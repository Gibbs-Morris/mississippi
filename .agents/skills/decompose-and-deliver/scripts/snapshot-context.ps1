#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [string[]]$ContextPath = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-ContextGit {
    param([string]$Root, [string[]]$Arguments)
    $output = @(& git --no-optional-locks -c core.fsmonitor= -C $Root @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Git context inspection failed: $($Arguments[0])" }
    return $output
}

function Get-ContextFileType {
    param([IO.FileSystemInfo]$Item, [string]$Relative)
    if ($IsWindows) { return 'File' }
    if ($null -eq $item.PSObject.Properties['UnixStat']) {
        throw 'Unix file-type inspection is unavailable; inspect selected files manually.'
    }
    if ($item.UnixStat.ItemType -ne 'File') {
        throw "Non-regular context files require manual inspection: $relative"
    }
    return 'File'
}

function Get-ContextInput {
    param([string]$Root, [string]$Relative)
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative)) {
        throw 'Context paths must be nonempty and repository-relative.'
    }
    $fullPath = [IO.Path]::GetFullPath((Join-Path $root $relative))
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (-not $fullPath.StartsWith($root.TrimEnd('/', '\') + [IO.Path]::DirectorySeparatorChar, $comparison)) {
        throw "Context path escapes the target repository: $relative"
    }
    $item = Get-Item -LiteralPath $fullPath -Force
    if ($item.PSIsContainer) { throw "Context path is not a file: $relative" }
    $ancestor = $item
    while ($null -ne $ancestor -and $ancestor.FullName -ne $root) {
        if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Linked context paths require explicit manual inspection: $relative"
        }
        $ancestor = Get-Item -LiteralPath (Split-Path -Parent $ancestor.FullName) -Force
    }
    $type = Get-ContextFileType $item $relative
    return [pscustomobject]@{
        Path = [IO.Path]::GetRelativePath($root, $fullPath).Replace('\', '/')
        Type = $type
        Sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Get-ContextObservation {
    param([string]$Root, [string[]]$ContextPaths)
    $head = [string](Invoke-ContextGit $root @('rev-parse', '--verify', 'HEAD'))
    $branch = [string](Invoke-ContextGit $root @('branch', '--show-current'))
    $filters = @(& git --no-optional-locks -c core.fsmonitor= -C $root config --name-only --get-regexp '^filter\..*\.(clean|process)$')
    if ($LASTEXITCODE -notin @(0, 1)) { throw 'Git filter configuration inspection failed.' }
    if ($filters.Count -gt 0) {
        throw 'Configured clean/process filters require manual inspection; status may execute repository-controlled commands.'
    }
    $status = @(Invoke-ContextGit $root @('status', '--porcelain=v1', '--untracked-files=all'))
    $paths = @(Invoke-ContextGit $root @('-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard') |
        Where-Object { Test-Path -LiteralPath (Join-Path $root $_) } | Sort-Object -Unique)
    $index = @(Invoke-ContextGit $root @('ls-files', '--stage'))
    $selected = @(foreach ($relative in $ContextPaths) { Get-ContextInput $root $relative })
    return [pscustomobject]@{
        Head = $head
        Branch = $branch
        Status = $status
        Paths = $paths
        Index = $index
        SelectedInputs = @($selected)
    }
}

try {
    foreach ($selector in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_NAMESPACE')) {
        if ($null -ne [Environment]::GetEnvironmentVariable($selector)) {
            throw "Ambient Git override $selector prevents reliable target inspection; use a clean process or manual inspection."
        }
    }
    $requestedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $root = [IO.Path]::GetFullPath(([string](Invoke-ContextGit $requestedRoot @('rev-parse', '--show-toplevel'))).Trim())
    $before = Get-ContextObservation $root $ContextPath
    $after = Get-ContextObservation $root $ContextPath
    if (($before | ConvertTo-Json -Depth 8 -Compress) -cne ($after | ConvertTo-Json -Depth 8 -Compress)) {
        throw 'Repository or selected inputs changed during context inspection; reconcile and retry.'
    }
    [pscustomobject]@{
        SchemaVersion = 1
        RepositoryRoot = $root
        Head = $after.Head
        Branch = $after.Branch
        Dirty = $after.Status.Count -gt 0
        Paths = $after.Paths
        SelectedInputs = @($after.SelectedInputs)
        InstructionSelectionRequired = $true
    } | ConvertTo-Json -Depth 8
    exit 0
}
catch {
    Write-Error -Message $_.Exception.Message
    exit 1
}
