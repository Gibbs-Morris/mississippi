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
    $output = @(& git -C $Root @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Git context inspection failed: $($Arguments[0])" }
    return $output
}

try {
    foreach ($selector in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_NAMESPACE')) {
        if ($null -ne [Environment]::GetEnvironmentVariable($selector)) {
            throw "Ambient Git override $selector prevents reliable target inspection; use a clean process or manual inspection."
        }
    }
    $requestedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $root = [IO.Path]::GetFullPath(([string](Invoke-ContextGit $requestedRoot @('rev-parse', '--show-toplevel'))).Trim())
    $head = [string](Invoke-ContextGit $root @('rev-parse', '--verify', 'HEAD'))
    $branch = [string](Invoke-ContextGit $root @('branch', '--show-current'))
    $status = @(Invoke-ContextGit $root @('status', '--porcelain=v1', '--untracked-files=all'))
    $paths = @(Invoke-ContextGit $root @('-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard') | Sort-Object -Unique)
    $selected = @()
    foreach ($relative in $ContextPath) {
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
        $selected += [pscustomobject]@{
            Path = [IO.Path]::GetRelativePath($root, $fullPath).Replace('\', '/')
            Sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $finalHead = [string](Invoke-ContextGit $root @('rev-parse', '--verify', 'HEAD'))
    if ($head -ne $finalHead) { throw 'HEAD changed during context inspection; reconcile and retry.' }
    [pscustomobject]@{
        SchemaVersion = 1
        RepositoryRoot = $root
        Head = $head
        Branch = $branch
        Dirty = $status.Count -gt 0
        Paths = $paths
        SelectedInputs = @($selected)
        InstructionSelectionRequired = $true
    } | ConvertTo-Json -Depth 8
    exit 0
}
catch {
    Write-Error -Message $_.Exception.Message
    exit 1
}
