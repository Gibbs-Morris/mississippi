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
    $arguments += @('--name-status','--find-renames','--no-ext-diff','--no-textconv','-z')
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
    $arguments += @('--binary','--find-renames','--no-ext-diff','--no-textconv')
    $arguments += $Revisions
    $text = Invoke-CrcGit -Repository $Repository -Arguments $arguments
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
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
        if ($item.PSIsContainer) { throw "Untracked path is a directory, not a file: $relative" }
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $result += [ordered]@{ path = $relative; type = 'file'; sha256 = Get-CrcSha256 -Bytes $bytes; size = $bytes.Length }
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

function Invoke-Collection {
    $repository = Resolve-CrcRepository -Path $Repo
    if ($Mode -eq 'codebase') {
        $revision = Resolve-CrcCommit -Repository $repository -Revision $Head
        $files = @(Get-TreeFiles -Repository $repository -Revision $revision)
        $dirtyStatus = @(Get-WorktreeStatus -Repository $repository)
        $dirtyWorktree = $dirtyStatus.Count -gt 0
        $warnings = if ($dirtyWorktree) { @('working tree is dirty; codebase snapshot is pinned to the requested commit') } else { @() }
        $material = [ordered]@{ mode = 'codebase'; status = 'READY'; revision = $revision; files = $files; dirty_worktree = $dirtyWorktree; dirty_status = $dirtyStatus; warnings = $warnings }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.status = 'READY'; $manifest.revision = $revision; $manifest.files = $files; $manifest.changed_files = @(); $manifest.dirty_worktree = $dirtyWorktree; $manifest.dirty_status = $dirtyStatus; $manifest.warnings = $warnings
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if ($Mode -eq 'branch') {
        if (-not $Base) { throw 'branch mode requires --Base' }
        $baseCommit = Resolve-CrcCommit -Repository $repository -Revision $Base
        $headCommit = Resolve-CrcCommit -Repository $repository -Revision $Head
        $mergeBase = (Invoke-CrcGit -Repository $repository -Arguments @('merge-base',$baseCommit,$headCommit)).Trim()
        $changed = @(Get-ChangedFiles -Repository $repository -Revisions @($mergeBase,$headCommit))
        $patch = Get-PatchComponent -Repository $repository -Revisions @($mergeBase,$headCommit)
        $manifestStatus = if ($changed.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
        $material = [ordered]@{ mode = 'branch'; status = $manifestStatus; base = $baseCommit; head = $headCommit; merge_base = $mergeBase; changed_files = $changed; patch = $patch }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.status = $manifestStatus
        $manifest.base = $baseCommit; $manifest.head = $headCommit; $manifest.merge_base = $mergeBase; $manifest.changed_files = $changed; $manifest.patch = $patch
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if ($Mode -eq 'worktree') {
        $startState = Get-WorktreeState -Repository $repository
        $revision = $startState.revision
        $statuses = $startState.statuses
        $staged = @(Get-ChangedFiles -Repository $repository -Revisions @() -Cached)
        $unstaged = @(Get-ChangedFiles -Repository $repository -Revisions @())
        $selected = [ordered]@{ selection = $Changes }
        if ($Changes -in @('staged','all')) { $selected.staged = [ordered]@{ files = $staged; patch = Get-PatchComponent -Repository $repository -Revisions @() -Cached } }
        if ($Changes -in @('unstaged','all')) { $selected.unstaged = [ordered]@{ files = $unstaged; patch = Get-PatchComponent -Repository $repository -Revisions @() }; $selected.untracked = @(Get-UntrackedFiles -Repository $repository -Statuses $statuses) }
        $unresolved = [bool]((Invoke-CrcGit -Repository $repository -Arguments @('ls-files','-u')).Trim())
        $endState = Get-WorktreeState -Repository $repository
        if ((ConvertTo-CrcJson -Value $startState) -ne (ConvertTo-CrcJson -Value $endState)) { throw 'worktree changed while the immutable snapshot was being collected' }
        $selectedFiles = @()
        foreach ($stream in @($selected.staged,$selected.unstaged)) { if ($null -ne $stream) { $selectedFiles += @($stream.files) } }
        if ($null -ne $selected.untracked) {
            $selectedFiles += @($selected.untracked | ForEach-Object {
                $entry = [ordered]@{ status = '??'; path = $_.path; type = $_.type }
                if ($_.type -eq 'symlink') { $entry.target = @($_.target) }
                else { $entry.sha256 = $_.sha256; $entry.size = $_.size }
                $entry
            })
        }
        $manifestStatus = if ($unresolved) { 'BLOCKED' } elseif ($selectedFiles.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
        $material = [ordered]@{ mode = 'worktree'; status = $manifestStatus; revision = $revision; selection = $Changes; statuses = $statuses; selected = $selected; unresolved_index = $unresolved }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.revision = $revision; $manifest.status = $manifestStatus
        $manifest.statuses = $statuses; $manifest.selected = $selected; $manifest.changed_files = @($selectedFiles | Sort-Object path,status); $manifest.unresolved_index = $unresolved
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if (-not $PullRequestSnapshot) { throw 'pull-request mode requires --PullRequestSnapshot' }
    $snapshotPath = Resolve-CrcSafePath -Path $PullRequestSnapshot -Label 'pull-request snapshot' -MustExist
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
    foreach ($field in @('repository','number','base_sha','head_sha','changed_files','diff','discussion','checks')) {
        $property = $snapshot.PSObject.Properties[$field]
        if ($null -eq $property -or $null -eq $property.Value) { throw "pull-request snapshot is missing $field" }
    }
    if ([string]::IsNullOrWhiteSpace([string]$snapshot.repository)) { throw 'pull-request repository evidence is empty' }
    if ([int]$snapshot.number -lt 1) { throw 'pull-request number evidence is invalid' }
    foreach ($field in @('changed_files','discussion','checks')) {
        if ($snapshot.$field -is [string] -or $snapshot.$field -isnot [System.Collections.IEnumerable]) { throw "pull-request $field evidence must be a collection" }
    }
    if ($snapshot.diff -isnot [string]) { throw 'pull-request diff evidence must be a string' }
    if ($snapshot.base_sha -notmatch '^[0-9a-f]{40}$' -or $snapshot.head_sha -notmatch '^[0-9a-f]{40}$') { throw 'pull-request base_sha/head_sha must be full SHAs' }
    $changed = @($snapshot.changed_files)
    $hasFiles = $changed.Count -gt 0; $hasDiff = -not [string]::IsNullOrWhiteSpace([string]$snapshot.diff)
    if ($hasFiles -ne $hasDiff) { throw 'pull-request changed_files and diff disagree about whether changes exist' }
    $diffHeaderPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in ($snapshot.diff -split "`r?`n")) {
        if ($line -match '^diff --git a/(.*) b/(.*)$') {
            [void]$diffHeaderPaths.Add((Get-CrcPath -Path $Matches[1]))
            [void]$diffHeaderPaths.Add((Get-CrcPath -Path $Matches[2]))
        }
    }
    foreach ($file in $changed) {
        if ($null -eq $file.PSObject.Properties['status'] -or $file.status -isnot [string] -or [string]::IsNullOrWhiteSpace($file.status)) { throw 'pull-request changed_files contains an invalid status' }
        if ($null -eq $file.PSObject.Properties['path'] -or $file.path -isnot [string] -or [string]::IsNullOrWhiteSpace($file.path)) { throw 'pull-request changed_files contains an invalid path' }
        $path = Get-CrcPath -Path $file.path
        if (-not $diffHeaderPaths.Contains($path)) { throw "pull-request diff headers do not contain changed path $path" }
        $oldPathProperty = $file.PSObject.Properties['old_path']
        if ($null -ne $oldPathProperty -and $null -ne $oldPathProperty.Value) {
            if ($file.old_path -isnot [string] -or [string]::IsNullOrWhiteSpace($file.old_path)) { throw "pull-request changed_files contains an invalid old_path for $path" }
            $oldPath = Get-CrcPath -Path $file.old_path
            if (-not $diffHeaderPaths.Contains($oldPath)) { throw "pull-request diff headers do not contain old path $oldPath" }
        }
    }
    $manifestStatus = if ($hasFiles) { 'READY' } else { 'NO_CHANGES' }
    $material = [ordered]@{ mode = 'pull-request'; status = $manifestStatus; snapshot = $snapshot }
    $manifest = New-Manifest -Repository $repository -Mode $Mode
    $manifest.status = $manifestStatus; $manifest.base = $snapshot.base_sha; $manifest.head = $snapshot.head_sha; $manifest.pull_request = $snapshot; $manifest.changed_files = $changed; $manifest.diff_sha256 = "sha256:$(Get-CrcHashText -Text ([string]$snapshot.diff))"
    return Complete-Manifest -Manifest $manifest -Material $material
}

$outputSafePath = $null
$manifest = New-Manifest -Repository ([System.IO.Path]::GetFullPath($Repo)) -Mode $Mode
try {
    $outputCandidatePath = Resolve-CrcSafePath -Path $Output -Label 'output'
    $repository = Resolve-CrcRepository -Path $Repo
    if ($Mode -eq 'worktree' -and (Test-CrcPathWithin -Path $outputCandidatePath -Root $repository)) { throw 'worktree output must be outside the reviewed repository' }
    $outputSafePath = $outputCandidatePath
    $manifest = Invoke-Collection
    Write-CrcJson -Path $outputSafePath -Value $manifest
    Write-Output $manifest.status
    exit 0
}
catch {
    $manifest.error = $_.Exception.Message
    $manifest.snapshot_id = "sha256:$(Get-CrcHashText -Text $_.Exception.Message)"
    if ($null -ne $outputSafePath) { Write-CrcJson -Path $outputSafePath -Value $manifest }
    Write-Error "BLOCKED: $($_.Exception.Message)"
    exit 2
}
