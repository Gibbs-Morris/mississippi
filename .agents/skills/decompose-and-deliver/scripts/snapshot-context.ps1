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
    $output = @(& git --no-replace-objects --no-optional-locks -c core.fsmonitor= -c core.trustctime=true -c core.checkStat=default -c core.ignoreStat=false -C $Root @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Git context inspection failed: $($Arguments[0])" }
    return $output
}

function Get-ContextFileMetadata {
    param([IO.FileSystemInfo]$Item, [string]$Relative)
    if ($IsWindows) { return [pscustomobject]@{ Type = 'File'; Mode = [int]$item.Attributes } }
    $stat = Get-Command stat -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $stat) { throw 'Unix stat metadata is unavailable; inspect selected files manually.' }
    $arguments = if ($IsMacOS) { @('-f', '%p', '--', $item.FullName) } else { @('-c', '%f', '--', $item.FullName) }
    $radix = if ($IsMacOS) { 8 } else { 16 }
    $mode = [Convert]::ToInt32(([string](Invoke-ContextNativeOutput $stat.Source $arguments)).Trim(), $radix)
    if (($mode -band 61440) -ne 32768) {
        throw "Non-regular context files require manual inspection: $relative"
    }
    return [pscustomobject]@{ Type = 'File'; Mode = $mode }
}

function Get-ContextFileHash {
    param([string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
        $hasher.Dispose()
    }
}

function Get-ContextInput {
    param([string]$Root, [string]$Relative)
    if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative)) {
        throw 'Context paths must be nonempty and repository-relative.'
    }
    $fullPath = [IO.Path]::GetFullPath([IO.Path]::Combine($root, $relative))
    if (-not $fullPath.StartsWith($root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
        throw "Context path escapes the target repository: $relative"
    }
    if ([IO.Directory]::Exists($fullPath)) { throw "Context path is not a file: $relative" }
    $item = [IO.FileInfo]::new($fullPath)
    if (-not $item.Exists) { throw "Context file is absent or inaccessible: $relative" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Linked context paths require explicit manual inspection: $relative"
    }
    $ancestor = $item.Directory
    while ($null -ne $ancestor -and $ancestor.FullName -cne $root) {
        if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Linked context paths require explicit manual inspection: $relative"
        }
        $ancestor = $ancestor.Parent
    }
    $metadata = Get-ContextFileMetadata $item $relative
    $relativePath = [IO.Path]::GetRelativePath($root, $fullPath)
    if ($IsWindows) { $relativePath = $relativePath.Replace('\', '/') }
    return [pscustomobject]@{
        Path = $relativePath
        Type = $metadata.Type
        Mode = $metadata.Mode
        Sha256 = Get-ContextFileHash $fullPath
    }
}

function Invoke-ContextNativeOutput {
    param([string]$Application, [string[]]$Arguments)
    $start = [Diagnostics.ProcessStartInfo]::new($Application)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false, $true)
    foreach ($argument in $Arguments) {
        $start.ArgumentList.Add($argument)
    }
    $child = [Diagnostics.Process]::Start($start)
    try {
        $output = $child.StandardOutput.ReadToEndAsync()
        $errorOutput = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(10000)) { throw 'Native context inspection timed out; inspect the target manually.' }
        if ($child.ExitCode -ne 0) { throw "Native context inspection failed: $($errorOutput.GetAwaiter().GetResult())" }
        return $output.GetAwaiter().GetResult()
    }
    finally {
        if (-not $child.HasExited) { $child.Kill($true) }
        $child.Dispose()
    }
}

function Get-ContextRawPaths {
    param([string]$Root)
    $git = (Get-Command git -CommandType Application | Select-Object -First 1).Source
    $arguments = @('--no-replace-objects', '--no-optional-locks', '-c', 'core.fsmonitor=', '-C', $root, 'ls-files', '-z', '--cached', '--others', '--exclude-standard')
    return ([string](Invoke-ContextNativeOutput $git $arguments)).Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
}

function Get-ContextPaths {
    param([string]$Root)
    $paths = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($relative in (Get-ContextRawPaths $root)) {
        $fullPath = [IO.Path]::Combine($root, $relative)
        if ([IO.File]::Exists($fullPath) -or [IO.Directory]::Exists($fullPath)) { $null = $paths.Add($relative) }
    }
    return @($paths)
}

function Get-ContextEmbeddedRoot {
    param([string]$Root)
    $directory = [IO.DirectoryInfo]::new($root)
    if (-not $directory.Exists) { throw 'The selected directory is absent or inaccessible.' }
    while ($null -ne $directory) {
        $marker = [IO.Path]::Combine($directory.FullName, '.git')
        $markerDirectory = [IO.DirectoryInfo]::new($marker)
        if ([IO.File]::Exists($marker) -or $markerDirectory.Exists) {
            if (-not $markerDirectory.Exists -or ($markerDirectory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Git directory indirection requires manual inspection, including linked worktrees.'
            }
            return $directory.FullName
        }
        $directory = $directory.Parent
    }
    throw 'An embedded Git directory was not found; inspect this target manually.'
}

function Get-ContextGitRoot {
    param([string]$Root)
    $expectedRoot = Get-ContextEmbeddedRoot $root
    $null = & git --no-replace-objects --no-optional-locks -c core.fsmonitor= -C $root config --get core.worktree
    if ($LASTEXITCODE -notin @(0, 1)) { throw 'Git worktree configuration inspection failed.' }
    if ($LASTEXITCODE -eq 0) {
        throw 'Configured core.worktree requires manual inspection; it can redirect the selected repository.'
    }
    $actualRoot = [IO.Path]::GetFullPath(([string](Invoke-ContextGit $root @('rev-parse', '--show-toplevel'))).Trim())
    $gitDirectory = [IO.Path]::GetFullPath(([string](Invoke-ContextGit $root @('rev-parse', '--absolute-git-dir'))).Trim())
    if ($actualRoot -cne $expectedRoot -or $gitDirectory -cne [IO.Path]::Combine($expectedRoot, '.git')) {
        throw 'Git directory identity differs from the selected working copy; inspect this target manually.'
    }
    return $actualRoot
}

function Get-ContextObservation {
    param([string]$Root, [string[]]$ContextPaths)
    if ((Get-ContextGitRoot $root) -cne $root) { throw 'Repository root changed during context inspection.' }
    $head = [string](Invoke-ContextGit $root @('rev-parse', '--verify', 'HEAD'))
    $branch = [string](Invoke-ContextGit $root @('branch', '--show-current'))
    $index = @(Invoke-ContextGit $root @('ls-files', '--stage'))
    if (@($index | Where-Object { $_ -match '^160000 ' }).Count -gt 0) {
        throw 'Submodule entries require manual inspection; status can execute submodule-local commands.'
    }
    $flags = @(Invoke-ContextGit $root @('ls-files', '-v'))
    if (@($flags | Where-Object { $_ -cmatch '^[a-zS] ' }).Count -gt 0) {
        throw 'Hidden index flags require manual inspection: assume-unchanged or skip-worktree can conceal changes.'
    }
    $filters = @(& git --no-replace-objects --no-optional-locks -c core.fsmonitor= -C $root config --name-only --get-regexp '^filter\..*\.(clean|process)$')
    if ($LASTEXITCODE -notin @(0, 1)) { throw 'Git filter configuration inspection failed.' }
    if ($filters.Count -gt 0) {
        throw 'Configured clean/process filters require manual inspection; status may execute repository-controlled commands.'
    }
    $status = @(Invoke-ContextGit $root @('status', '--porcelain=v1', '--untracked-files=all'))
    $paths = @(Get-ContextPaths $root)
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
    $requestedRoot = if ([IO.Path]::IsPathRooted($RepositoryRoot)) {
        [IO.Path]::GetFullPath($RepositoryRoot)
    } else { [IO.Path]::GetFullPath([IO.Path]::Combine((Get-Location).Path, $RepositoryRoot)) }
    $root = Get-ContextGitRoot $requestedRoot
    $before = Get-ContextObservation $root $ContextPath
    $after = Get-ContextObservation $root $ContextPath
    if (($before | ConvertTo-Json -Depth 8 -Compress) -cne ($after | ConvertTo-Json -Depth 8 -Compress)) {
        throw 'Repository or selected inputs changed during context inspection; reconcile and retry.'
    }
    if ((Get-ContextGitRoot $requestedRoot) -cne $root) { throw 'Repository root changed during context inspection.' }
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
