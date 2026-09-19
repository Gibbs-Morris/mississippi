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
    $arguments += @('--name-status','--find-renames','--no-ext-diff')
    $arguments += $Revisions
    $lines = @(Invoke-CrcGit -Repository $Repository -Arguments $arguments | Where-Object { $_.Trim() })
    $result = @()
    foreach ($line in $lines) {
        $fields = $line -split "`t"
        if ($fields.Count -lt 2) { continue }
        if ($fields[0][0] -in @('R','C') -and $fields.Count -ge 3) {
            $result += [ordered]@{ status = $fields[0]; old_path = Get-CrcPath -Path $fields[1]; path = Get-CrcPath -Path $fields[2] }
        }
        else {
            $result += [ordered]@{ status = $fields[0]; path = Get-CrcPath -Path $fields[1] }
        }
    }
    return @($result | Sort-Object path,status)
}

function Get-PatchComponent {
    param([string]$Repository,[string[]]$Revisions,[switch]$Cached)
    $arguments = @('diff')
    if ($Cached) { $arguments += '--cached' }
    $arguments += @('--binary','--find-renames','--no-ext-diff')
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
    $lines = @(Invoke-CrcGit -Repository $Repository -Arguments @('ls-tree','-r','-l','--full-tree',$Revision) | Where-Object { $_.Trim() })
    $result = @()
    foreach ($line in $lines) {
        $parts = $line -split "`t",2
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
    $lines = @(Invoke-CrcGit -Repository $Repository -Arguments @('status','--porcelain=v1','--untracked-files=all') | Where-Object { $_.Trim() })
    $result = @()
    foreach ($line in $lines) {
        if ($line.Length -lt 4) { continue }
        $code = $line.Substring(0,2)
        $path = ($line.Substring(3) -replace ' -> .*$', '')
        $result += [ordered]@{ index = $code[0].ToString(); worktree = $code[1].ToString(); path = Get-CrcPath -Path $path }
    }
    return $result
}

function Get-UntrackedFiles {
    param([string]$Repository,[object[]]$Statuses)
    $result = @()
    foreach ($status in $Statuses | Where-Object { $_.index -eq '?' -and $_.worktree -eq '?' }) {
        $full = Join-Path $Repository ($status.path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        $resolved = (Resolve-Path -LiteralPath $full -ErrorAction Stop).Path
        $repoResolved = (Resolve-Path -LiteralPath $Repository).Path
        if (-not $resolved.StartsWith("$repoResolved$([System.IO.Path]::DirectorySeparatorChar)",[System.StringComparison]::OrdinalIgnoreCase)) { throw "Untracked path escapes repository: $($status.path)" }
        $bytes = [System.IO.File]::ReadAllBytes($resolved)
        $result += [ordered]@{ path = $status.path; sha256 = Get-CrcSha256 -Bytes $bytes; size = $bytes.Length }
    }
    return @($result | Sort-Object path)
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
        $files = Get-TreeFiles -Repository $repository -Revision $revision
        $material = [ordered]@{ mode = 'codebase'; revision = $revision; files = $files }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.status = 'READY'; $manifest.revision = $revision; $manifest.files = $files; $manifest.changed_files = @()
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if ($Mode -eq 'branch') {
        if (-not $Base) { throw 'branch mode requires --Base' }
        $baseCommit = Resolve-CrcCommit -Repository $repository -Revision $Base
        $headCommit = Resolve-CrcCommit -Repository $repository -Revision $Head
        $mergeBase = (Invoke-CrcGit -Repository $repository -Arguments @('merge-base',$baseCommit,$headCommit)).Trim()
        $changed = Get-ChangedFiles -Repository $repository -Revisions @($mergeBase,$headCommit)
        $patch = Get-PatchComponent -Repository $repository -Revisions @($mergeBase,$headCommit)
        $material = [ordered]@{ mode = 'branch'; base = $baseCommit; head = $headCommit; merge_base = $mergeBase; changed_files = $changed; patch = $patch }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.status = if ($changed.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
        $manifest.base = $baseCommit; $manifest.head = $headCommit; $manifest.merge_base = $mergeBase; $manifest.changed_files = $changed; $manifest.patch = $patch
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if ($Mode -eq 'worktree') {
        $revision = Resolve-CrcCommit -Repository $repository -Revision 'HEAD'
        $statuses = Get-WorktreeStatus -Repository $repository
        $staged = Get-ChangedFiles -Repository $repository -Revisions @() -Cached
        $unstaged = Get-ChangedFiles -Repository $repository -Revisions @()
        $selected = [ordered]@{ selection = $Changes }
        if ($Changes -in @('staged','all')) { $selected.staged = [ordered]@{ files = $staged; patch = Get-PatchComponent -Repository $repository -Revisions @() -Cached } }
        if ($Changes -in @('unstaged','all')) { $selected.unstaged = [ordered]@{ files = $unstaged; patch = Get-PatchComponent -Repository $repository -Revisions @() }; $selected.untracked = Get-UntrackedFiles -Repository $repository -Statuses $statuses }
        $unresolved = [bool]((Invoke-CrcGit -Repository $repository -Arguments @('ls-files','-u')).Trim())
        $selectedFiles = @()
        foreach ($stream in @($selected.staged,$selected.unstaged)) { if ($null -ne $stream) { $selectedFiles += @($stream.files) } }
        if ($null -ne $selected.untracked) { $selectedFiles += @($selected.untracked | ForEach-Object { [ordered]@{ status = '??'; path = $_.path; sha256 = $_.sha256; size = $_.size } }) }
        $material = [ordered]@{ mode = 'worktree'; revision = $revision; selection = $Changes; statuses = $statuses; selected = $selected; unresolved_index = $unresolved }
        $manifest = New-Manifest -Repository $repository -Mode $Mode
        $manifest.revision = $revision; $manifest.status = if ($unresolved) { 'BLOCKED' } elseif ($selectedFiles.Count -gt 0) { 'READY' } else { 'NO_CHANGES' }
        $manifest.statuses = $statuses; $manifest.selected = $selected; $manifest.changed_files = @($selectedFiles | Sort-Object path,status); $manifest.unresolved_index = $unresolved
        return Complete-Manifest -Manifest $manifest -Material $material
    }
    if (-not $PullRequestSnapshot) { throw 'pull-request mode requires --PullRequestSnapshot' }
    $snapshotPath = Resolve-CrcSafePath -Path $PullRequestSnapshot -Label 'pull-request snapshot' -MustExist
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
    foreach ($field in @('repository','number','base_sha','head_sha','changed_files','diff','discussion','checks')) { if ($null -eq $snapshot.$field) { throw "pull-request snapshot is missing $field" } }
    if ($snapshot.base_sha -notmatch '^[0-9a-f]{40}$' -or $snapshot.head_sha -notmatch '^[0-9a-f]{40}$') { throw 'pull-request base_sha/head_sha must be full SHAs' }
    $changed = @($snapshot.changed_files)
    $hasFiles = $changed.Count -gt 0; $hasDiff = -not [string]::IsNullOrWhiteSpace([string]$snapshot.diff)
    if ($hasFiles -ne $hasDiff) { throw 'pull-request changed_files and diff disagree about whether changes exist' }
    foreach ($file in $changed) { if (-not $file.path) { throw 'pull-request changed_files contains an invalid path' }; [void](Get-CrcPath -Path $file.path); if ($file.old_path) { [void](Get-CrcPath -Path $file.old_path) }; if (-not ([string]$snapshot.diff).Contains([string]$file.path)) { throw "pull-request diff does not contain changed path $($file.path)" } }
    $material = [ordered]@{ mode = 'pull-request'; snapshot = $snapshot }
    $manifest = New-Manifest -Repository $repository -Mode $Mode
    $manifest.status = if ($hasFiles) { 'READY' } else { 'NO_CHANGES' }; $manifest.base = $snapshot.base_sha; $manifest.head = $snapshot.head_sha; $manifest.pull_request = $snapshot; $manifest.changed_files = $changed; $manifest.diff_sha256 = "sha256:$(Get-CrcHashText -Text ([string]$snapshot.diff))"
    return Complete-Manifest -Manifest $manifest -Material $material
}

$manifest = New-Manifest -Repository ([System.IO.Path]::GetFullPath($Repo)) -Mode $Mode
try {
    $manifest = Invoke-Collection
    Write-CrcJson -Path $Output -Value $manifest
    Write-Output $manifest.status
    exit 0
}
catch {
    $manifest.error = $_.Exception.Message
    $manifest.snapshot_id = "sha256:$(Get-CrcHashText -Text $_.Exception.Message)"
    Write-CrcJson -Path $Output -Value $manifest
    Write-Error "BLOCKED: $($_.Exception.Message)"
    exit 2
}
