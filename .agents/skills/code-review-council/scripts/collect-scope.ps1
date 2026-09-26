#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('codebase','branch','worktree','pull-request')][string]$Mode,
    [string]$Repo = (Get-Location).Path,
    [string]$Base,
    [string]$Head = 'HEAD',
    [ValidateSet('staged','unstaged','all')][string]$Changes = 'all',
    [string]$PullRequestSnapshot,
    [Parameter(Mandatory)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

function Get-ChangedFiles {
    param([string]$Repository,[string[]]$Revisions,[switch]$Cached)
    $arguments = @('diff')
    if ($Cached) { $arguments += '--cached' }
    $arguments += @('--name-status','--find-renames','--ignore-submodules=none','--no-ext-diff','--no-textconv','-z')
    $arguments += $Revisions
    $records = @(Get-CrcGitNulRecords -Bytes (Invoke-CrcGitBytes -Repository $Repository -Arguments $arguments))
    $result = @()
    for ($index = 0; $index -lt $records.Count;) {
        $status = $records[$index++]
        if ($index -ge $records.Count) { throw 'Git returned an incomplete name-status record' }
        $firstPath = Get-CrcPath -Path $records[$index++]
        if ($status[0] -in @('R','C')) {
            if ($index -ge $records.Count) { throw 'Git returned an incomplete rename record' }
            $secondPath = Get-CrcPath -Path $records[$index++]
            $result += [ordered]@{ status = $status; old_path = $firstPath; path = $secondPath }
        }
        else {
            $result += [ordered]@{ status = $status; path = $firstPath }
        }
    }
    return @($result | Sort-Object path,status)
}

function Get-PatchComponent {
    param([string]$Repository,[string[]]$Revisions,[switch]$Cached)
    $arguments = @('diff')
    if ($Cached) { $arguments += '--cached' }
    $arguments += @('--binary','--find-renames','--default-prefix','--ignore-submodules=none','--no-ext-diff','--no-textconv')
    $arguments += $Revisions
    $bytes = Invoke-CrcGitBytes -Repository $Repository -Arguments $arguments
    return [ordered]@{
        sha256 = Get-CrcSha256 -Bytes $bytes
        bytes = $bytes.Length
        encoding = 'base64'
        content_base64 = [Convert]::ToBase64String($bytes)
    }
}

function Get-TreeFiles {
    param([string]$Repository,[string]$Revision)
    $records = @(Get-CrcGitNulRecords -Bytes (Invoke-CrcGitBytes -Repository $Repository -Arguments @('ls-tree','-r','-l','-z','--full-tree',$Revision)))
    $result = @()
    foreach ($record in $records) {
        $parts = $record -split "`t",2
        if ($parts.Count -ne 2) { continue }
        $header = $parts[0] -split '\s+'
        if ($header.Count -lt 4) { continue }
        $path = Get-CrcPath -Path $parts[1]
        if ($header[1] -eq 'commit') {
            $result += [ordered]@{ path = $path; gitlink_sha = $header[2]; type = 'gitlink' }
        }
        elseif ($header[1] -eq 'blob') {
            $size = $null
            if ($header[3] -ne '-') { $size = [int64]$header[3] }
            $result += [ordered]@{ path = $path; blob_sha = $header[2]; size = $size }
        }
    }
    return @($result | Sort-Object path)
}

function Get-WorktreeStatus {
    param([string]$Repository)
    $records = @(Get-CrcGitNulRecords -Bytes (Invoke-CrcGitBytes -Repository $Repository -Arguments @('status','--porcelain=v1','--untracked-files=all','-z')))
    $result = @()
    for ($index = 0; $index -lt $records.Count;) {
        $record = $records[$index++]
        if ($record.Length -lt 4) { continue }
        $code = $record.Substring(0,2)
        $path = Get-CrcPath -Path $record.Substring(3)
        $entry = [ordered]@{ index = $code[0].ToString(); worktree = $code[1].ToString(); path = $path }
        if ($code[0] -in @('R','C') -and $index -lt $records.Count) { $entry.old_path = Get-CrcPath -Path $records[$index++] }
        $result += $entry
    }
    return $result
}

function Get-CrcUnixFileType {
    param([string]$Path)

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = '/usr/bin/stat'
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.Environment['LC_ALL'] = 'C'
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
        $startInfo.ArgumentList.Add('-c')
        $startInfo.ArgumentList.Add('%F')
        $startInfo.ArgumentList.Add('--')
    }
    else {
        $startInfo.ArgumentList.Add('-f')
        $startInfo.ArgumentList.Add('%HT')
    }
    $startInfo.ArgumentList.Add($Path)
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $output = $process.StandardOutput.ReadToEnd()
        $null = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Unable to determine untracked file type: $Path" }
        return $output.Trim().ToLowerInvariant()
    }
    finally { $process.Dispose() }
}

function Assert-CrcRegularUntrackedFile {
    param([System.IO.FileSystemInfo]$Item,[string]$Path)

    if ($Item.PSIsContainer) { throw "Untracked path is a directory, not a file: $Path" }
    if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $fileType = Get-CrcUnixFileType -Path $Item.FullName
        if ($fileType -notin @('regular file','regular')) { throw "Untracked path is not a regular file: $Path ($fileType)" }
        return
    }
    if (($Item.Attributes -band [System.IO.FileAttributes]::Device) -ne 0) { throw "Untracked path is not a regular file: $Path" }
}

function Get-UntrackedFiles {
    param([string]$Repository,[object[]]$Statuses)
    $result = @()
    foreach ($status in $Statuses | Where-Object { $_.index -eq '?' -and $_.worktree -eq '?' }) {
        $relative = Get-CrcPath -Path $status.path
        $full = [System.IO.Path]::GetFullPath((Join-Path $Repository ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
        $repoResolved = [System.IO.Path]::GetFullPath($Repository).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-CrcPathWithin -Path $full -Root $repoResolved)) { throw "Untracked path escapes repository: $($status.path)" }
        $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            $result += [ordered]@{ path = $relative; type = 'symlink'; target = @($item.Target) }
            continue
        }
        Assert-CrcRegularUntrackedFile -Item $item -Path $relative
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $result += [ordered]@{ path = $relative; type = 'file'; sha256 = Get-CrcSha256 -Bytes $bytes; size = $bytes.Length; content_base64 = [Convert]::ToBase64String($bytes) }
    }
    return @($result | Sort-Object path)
}

function Get-WorktreeState {
    param([string]$Repository)
    $revision = Resolve-CrcCommit -Repository $Repository -Revision 'HEAD'
    $statuses = @(Get-WorktreeStatus -Repository $Repository)
    $stagedPatch = Get-PatchComponent -Repository $Repository -Revisions @() -Cached
    $unstagedPatch = Get-PatchComponent -Repository $Repository -Revisions @()
    $untracked = Get-UntrackedFiles -Repository $Repository -Statuses $statuses
    $unresolved = [bool]((Invoke-CrcGit -Repository $Repository -Arguments @('ls-files','-u')).Trim())
    return [ordered]@{
        revision = $revision
        statuses = $statuses
        staged_patch_sha256 = $stagedPatch.sha256
        unstaged_patch_sha256 = $unstagedPatch.sha256
        untracked = $untracked
        unresolved_index = $unresolved
    }
}

function New-Manifest {
    param([string]$Repository,[string]$Mode)
    return [ordered]@{
        schema_version = $script:CrcSchemaVersion
        mode = $Mode
        status = 'BLOCKED'
        snapshot_id = "sha256:$('0' * 64)"
        repository = [ordered]@{ root = $Repository }
        captured_at_utc = Get-CrcUtcNow
    }
}

function Complete-Manifest {
    param([System.Collections.IDictionary]$Manifest,[object]$Material)
    $Manifest.snapshot_id = Get-CrcHashJson -Value $Material
    $Manifest.snapshot_material = $Material
    return $Manifest
}

function Complete-BlockedManifest { param([System.Collections.IDictionary]$Manifest,[string]$ErrorMessage)
    $Manifest.status = 'BLOCKED'
    $Manifest.error = $ErrorMessage
    $material = [ordered]@{ mode=$Manifest.mode; status='BLOCKED'; error=$ErrorMessage }
    return Complete-Manifest -Manifest $Manifest -Material $material
}

function Get-CodebaseCollection { param([string]$Repository,[string]$HeadRevision)
    $revision = Resolve-CrcCommit -Repository $Repository -Revision $HeadRevision
    $files = @(Get-TreeFiles -Repository $Repository -Revision $revision)
    $dirtyStatus = @(Get-WorktreeStatus -Repository $Repository)
    $dirtyWorktree = $dirtyStatus.Count -gt 0
    $warnings = if ($dirtyWorktree) { @('working tree is dirty; codebase snapshot is pinned to the requested commit') } else { @() }
    $material = [ordered]@{ mode = 'codebase'; status = 'READY'; revision = $revision; files = $files; dirty_worktree = $dirtyWorktree; dirty_status = $dirtyStatus; warnings = $warnings }
    $manifest = New-Manifest -Repository $Repository -Mode 'codebase'
    $manifest.status = 'READY'; $manifest.revision = $revision; $manifest.files = $files; $manifest.changed_files = @(); $manifest.dirty_worktree = $dirtyWorktree; $manifest.dirty_status = $dirtyStatus; $manifest.warnings = $warnings
    return Complete-Manifest -Manifest $manifest -Material $material
}

function Get-BranchCollection { param([string]$Repository,[string]$BaseRevision,[string]$HeadRevision)
    if (-not $BaseRevision) { throw 'branch mode requires --Base' }
    $baseCommit = Resolve-CrcCommit -Repository $Repository -Revision $BaseRevision
    $headCommit = Resolve-CrcCommit -Repository $Repository -Revision $HeadRevision
    $mergeBases = @(Get-CrcGitLines -Repository $Repository -Arguments @('merge-base','--all',$baseCommit,$headCommit))
    if ($mergeBases.Count -ne 1) { throw 'branch scope requires exactly one best merge base' }
    $mergeBase = $mergeBases[0]
    $changed = @(Get-ChangedFiles -Repository $Repository -Revisions @($mergeBase,$headCommit))
    $patch = Get-PatchComponent -Repository $Repository -Revisions @($mergeBase,$headCommit)
    $manifestStatus = if ($changed.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
    $material = [ordered]@{ mode = 'branch'; status = $manifestStatus; base = $baseCommit; head = $headCommit; merge_base = $mergeBase; changed_files = $changed; patch = $patch }
    $manifest = New-Manifest -Repository $Repository -Mode 'branch'
    $manifest.status = $manifestStatus; $manifest.base = $baseCommit; $manifest.head = $headCommit; $manifest.merge_base = $mergeBase; $manifest.changed_files = $changed; $manifest.patch = $patch
    return Complete-Manifest -Manifest $manifest -Material $material
}

function Get-SelectedWorktreeStreams { param([string]$Repository,[string]$Selection)
    $startState = Get-WorktreeState -Repository $Repository
    $selected = [ordered]@{ selection = $Selection }
    if ($Selection -in @('staged','all')) { $selected.staged = [ordered]@{ files = @(Get-ChangedFiles -Repository $Repository -Revisions @() -Cached); patch = Get-PatchComponent -Repository $Repository -Revisions @() -Cached } }
    if ($Selection -in @('unstaged','all')) { $selected.unstaged = [ordered]@{ files = @(Get-ChangedFiles -Repository $Repository -Revisions @()); patch = Get-PatchComponent -Repository $Repository -Revisions @() }; $selected.untracked = @(Get-UntrackedFiles -Repository $Repository -Statuses $startState.statuses) }
    $endState = Get-WorktreeState -Repository $Repository
    if ((ConvertTo-CrcJson -Value $startState) -ne (ConvertTo-CrcJson -Value $endState)) { throw 'worktree changed while the immutable snapshot was being collected' }
    return [pscustomobject]@{ revision=$startState.revision; statuses=$startState.statuses; unresolved_index=$startState.unresolved_index; selected=$selected }
}

function Get-SelectedWorktreeFiles { param([object]$Selected)
    $files = @()
    foreach ($streamName in @('staged','unstaged')) {
        $stream = if ($Selected.Contains($streamName)) { $Selected[$streamName] } else { $null }
        if ($null -ne $stream) { $files += @($stream.files) }
    }
    $untrackedFiles = if ($Selected.Contains('untracked')) { @($Selected['untracked']) } else { @() }
    foreach ($untracked in $untrackedFiles) {
        $entry = [ordered]@{ status='??'; path=$untracked.path; type=$untracked.type }
        if ($untracked.type -eq 'symlink') { $entry.target = @($untracked.target) }
        else { $entry.sha256 = $untracked.sha256; $entry.size = $untracked.size }
        $files += $entry
    }
    return @($files | Sort-Object path,status)
}

function Get-WorktreeCollection { param([string]$Repository,[string]$Selection)
    $snapshot = Get-SelectedWorktreeStreams -Repository $Repository -Selection $Selection
    $changedFiles = Get-SelectedWorktreeFiles -Selected $snapshot.selected
    $status = if ($snapshot.unresolved_index) { 'BLOCKED' } elseif ($changedFiles.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
    $material = [ordered]@{ mode='worktree'; status=$status; revision=$snapshot.revision; selection=$Selection; statuses=$snapshot.statuses; selected=$snapshot.selected; unresolved_index=$snapshot.unresolved_index }
    $manifest = New-Manifest -Repository $Repository -Mode 'worktree'
    $manifest.revision=$snapshot.revision; $manifest.status=$status; $manifest.statuses=$snapshot.statuses; $manifest.selected=$snapshot.selected; $manifest.changed_files=$changedFiles; $manifest.unresolved_index=$snapshot.unresolved_index
    return Complete-Manifest -Manifest $manifest -Material $material
}

function Assert-PullRequestSnapshotShape { param([object]$Snapshot,[int]$ObjectIdLength)
    foreach ($field in @('repository','number','base_sha','head_sha','changed_files','diff','discussion','checks')) {
        $property = $Snapshot.PSObject.Properties[$field]
        if ($null -eq $property -or $null -eq $property.Value) { throw "pull-request snapshot is missing $field" }
    }
    if ([string]::IsNullOrWhiteSpace([string]$Snapshot.repository)) { throw 'pull-request repository evidence is empty' }
    if ($Snapshot.number -isnot [int] -and $Snapshot.number -isnot [long]) { throw 'pull-request number must be an integer' }
    if ($Snapshot.number -lt 1) { throw 'pull-request number evidence is invalid' }
    foreach ($field in @('changed_files','discussion','checks')) {
        if ($Snapshot.$field -isnot [System.Array]) { throw "pull-request $field evidence must be an array" }
    }
    if ($Snapshot.diff -isnot [string]) { throw 'pull-request diff evidence must be a string' }
    $objectIdPattern = "^[0-9a-f]{$ObjectIdLength}$"
    if ($Snapshot.base_sha -notmatch $objectIdPattern -or $Snapshot.head_sha -notmatch $objectIdPattern) { throw 'pull-request base_sha/head_sha must match the repository object format' }
}

function Get-PullRequestDiffHeaderPaths { param([string]$Diff)
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in ($Diff -split "`r?`n")) {
        if ($line -match '^diff --git a/(.*) b/(.*)$') {
            [void]$paths.Add((Get-CrcPath -Path $Matches[1]))
            [void]$paths.Add((Get-CrcPath -Path $Matches[2]))
        }
    }
    return ,$paths
}

function Get-PullRequestRequiredText { param([object]$File,[string]$Name,[string]$ErrorMessage)
    $property = $File.PSObject.Properties[$Name]
    if ($null -eq $property -or $property.Value -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { throw $ErrorMessage }
    return [string]$property.Value
}

function Add-PullRequestDiffPath { param([string]$Path,[string]$Kind,[System.Collections.Generic.HashSet[string]]$Paths,[System.Collections.Generic.HashSet[string]]$DiffPaths)
    $normalizedPath = Get-CrcPath -Path $Path
    if (-not $DiffPaths.Contains($normalizedPath)) { throw "pull-request diff headers do not contain $Kind path $normalizedPath" }
    [void]$Paths.Add($normalizedPath)
}

function Add-PullRequestChangedFilePaths { param([object]$File,[System.Collections.Generic.HashSet[string]]$Paths,[System.Collections.Generic.HashSet[string]]$DiffPaths)
    $null = Get-PullRequestRequiredText -File $File -Name 'status' -ErrorMessage 'pull-request changed_files contains an invalid status'
    $path = Get-PullRequestRequiredText -File $File -Name 'path' -ErrorMessage 'pull-request changed_files contains an invalid path'
    Add-PullRequestDiffPath -Path $path -Kind 'changed' -Paths $Paths -DiffPaths $DiffPaths
    $oldPathProperty = $File.PSObject.Properties['old_path']
    if ($null -eq $oldPathProperty) { return }
    $oldPath = $oldPathProperty.Value
    if ($oldPath -isnot [string] -or [string]::IsNullOrWhiteSpace($oldPath)) { throw "pull-request changed_files contains an invalid old_path for $path" }
    Add-PullRequestDiffPath -Path $oldPath -Kind 'old' -Paths $Paths -DiffPaths $DiffPaths
}

function Assert-PullRequestChangedFiles { param([object[]]$ChangedFiles,[System.Collections.Generic.HashSet[string]]$DiffPaths)
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $ChangedFiles) { Add-PullRequestChangedFilePaths -File $file -Paths $paths -DiffPaths $DiffPaths }
    return ,$paths
}

function Get-PullRequestCollection { param([string]$Repository,[string]$SnapshotPath)
    if (-not $SnapshotPath) { throw 'pull-request mode requires --PullRequestSnapshot' }
    $safeSnapshotPath = Resolve-CrcSafePath -Path $SnapshotPath -Label 'pull-request snapshot' -MustExist
    $snapshot = Get-Content -LiteralPath $safeSnapshotPath -Raw | ConvertFrom-Json
    $objectIdLength = Get-CrcObjectIdLength -Repository $Repository
    Assert-PullRequestSnapshotShape -Snapshot $snapshot -ObjectIdLength $objectIdLength
    $null = Resolve-CrcCommit -Repository $Repository -Revision ([string]$snapshot.base_sha)
    $null = Resolve-CrcCommit -Repository $Repository -Revision ([string]$snapshot.head_sha)
    $changed = @($snapshot.changed_files)
    $hasDiff = -not [string]::IsNullOrWhiteSpace([string]$snapshot.diff)
    if (($changed.Count -gt 0) -ne $hasDiff) { throw 'pull-request changed_files and diff disagree about whether changes exist' }
    $diffPaths = Get-CrcDiffHeaderPaths -DiffText $snapshot.diff
    $changedPaths = Assert-PullRequestChangedFiles -ChangedFiles $changed -DiffPaths $diffPaths
    if ($changedPaths.Count -ne $diffPaths.Count) { throw 'pull-request diff headers and changed_files are incomplete or inconsistent' }
    foreach ($path in $diffPaths) { if (-not $changedPaths.Contains($path)) { throw "pull-request changed_files is missing diff header path $path" } }
    $status = if ($changed.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
    $material = [ordered]@{ mode='pull-request'; status=$status; snapshot=$snapshot }
    $manifest = New-Manifest -Repository $Repository -Mode 'pull-request'
    $manifest.status=$status; $manifest.base=$snapshot.base_sha; $manifest.head=$snapshot.head_sha; $manifest.pull_request=$snapshot; $manifest.changed_files=$changed; $manifest.diff_sha256="sha256:$(Get-CrcHashText -Text $snapshot.diff)"
    return Complete-Manifest -Manifest $manifest -Material $material
}

function Invoke-Collection {
    $repository = Resolve-CrcRepository -Path $Repo
    switch ($Mode) {
        'codebase' { return Get-CodebaseCollection -Repository $repository -HeadRevision $Head }
        'branch' { return Get-BranchCollection -Repository $repository -BaseRevision $Base -HeadRevision $Head }
        'worktree' { return Get-WorktreeCollection -Repository $repository -Selection $Changes }
        'pull-request' { return Get-PullRequestCollection -Repository $repository -SnapshotPath $PullRequestSnapshot }
        default { throw "unsupported collection mode: $Mode" }
    }
}

$outputSafePath = $null
$manifest = New-Manifest -Repository ([System.IO.Path]::GetFullPath($Repo)) -Mode $Mode
try {
    $outputCandidatePath = Resolve-CrcSafePath -Path $Output -Label 'output'
    $repository = Resolve-CrcRepository -Path $Repo
    if ($Mode -eq 'worktree' -and (Test-CrcPathWithin -Path $outputCandidatePath -Root $repository)) { throw 'worktree output must be outside the reviewed repository' }
    if ($Mode -eq 'pull-request') {
        $snapshotInputPath = Resolve-CrcSafePath -Path $PullRequestSnapshot -Label 'pull-request snapshot' -MustExist
        if (Test-CrcPathEqual -Left $outputCandidatePath -Right $snapshotInputPath) { throw 'output must differ from pull-request snapshot input' }
    }
    $outputSafePath = $outputCandidatePath
    $manifest = Invoke-Collection
    Write-CrcJson -Path $outputSafePath -Value $manifest
    Write-Output $manifest.status
    exit 0
}
catch {
    $manifest = Complete-BlockedManifest -Manifest $manifest -ErrorMessage $_.Exception.Message
    if ($null -ne $outputSafePath) { Write-CrcJson -Path $outputSafePath -Value $manifest }
    [Console]::Error.WriteLine("BLOCKED: $($_.Exception.Message)")
    [Console]::Error.WriteLine($_.InvocationInfo.PositionMessage)
    exit 2
}
