#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [string[]]$ContextPath = @(),
    [Parameter(DontShow)][string]$HashPath,
    [Parameter(DontShow)][string]$MetadataPath,
    [Parameter(DontShow)][string]$InspectionContext
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-ContextGit {
    param([string]$Root, [string[]]$Arguments, [int[]]$AcceptedExitCodes = @(0), [switch]$WithResult)
    $options = @('--no-replace-objects', '--no-optional-locks', '-c', 'core.fsmonitor=', '-c', 'core.trustctime=true', '-c', 'core.checkStat=default', '-c', 'core.ignoreStat=false', '-c', 'core.ignoreCase=false', '-C', $root)
    if (-not $IsWindows) { $options = @('-c', 'core.fileMode=true') + $options }
    $result = Invoke-ContextNativeOutput $gitApplication ($options + $Arguments) -AcceptedExitCodes $AcceptedExitCodes -WithResult
    if ($WithResult) { return $result }
    return $result.Output.Split([char]10, [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.TrimEnd([char]13) }
}

function Get-ContextFileMetadata {
    param([IO.FileSystemInfo]$Item, [string]$Relative, [IO.FileStream]$Stream)
    if ($IsWindows) {
        return Get-ContextWindowsFileMetadata $Item $Relative $Stream
    }
    return Get-ContextUnixFileMetadata $Item $Relative $Stream
}

function Get-ContextWindowsFileMetadata {
    param([IO.FileSystemInfo]$Item, [string]$Relative, [IO.FileStream]$Stream)
    $attributes = if ($null -eq $Stream) { $item.Attributes } else { [IO.File]::GetAttributes($Stream.SafeFileHandle) }
    if ($null -ne $Stream -and (-not $Stream.CanSeek -or ($attributes -band [IO.FileAttributes]::Device) -ne 0)) {
        throw "Non-regular context files require manual inspection: $Relative"
    }
    return [pscustomobject]@{ Type = 'File'; Mode = [int]$attributes }
}

function Get-ContextUnixFileMetadata {
    param([IO.FileSystemInfo]$Item, [string]$Relative, [IO.FileStream]$Stream)
    $stat = Get-Command stat -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $stat) { throw 'Unix stat metadata is unavailable; inspect selected files manually.' }
    $metadataPath = if ($null -eq $Stream) { $item.FullName } else { '/dev/fd/' + $Stream.SafeFileHandle.DangerousGetHandle().ToInt64() }
    $arguments = if ($IsMacOS) { @('-f', '%p', '--', $metadataPath) } else { @('-c', '%f', '--', $metadataPath) }
    if ($null -ne $Stream) { $arguments = @('-L') + $arguments }
    $radix = if ($IsMacOS) { 8 } else { 16 }
    $mode = [Convert]::ToInt32(([string](Invoke-ContextNativeOutput $stat.Source $arguments)).Trim(), $radix)
    if (($mode -band 61440) -ne 32768) {
        throw "Non-regular context files require manual inspection: $relative"
    }
    return [pscustomobject]@{ Type = 'File'; Mode = $mode }
}

function Get-ContextFileHash {
    param([string]$Path)
    $shell = [IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
    return Invoke-ContextNativeOutput $shell @('-NoProfile', '-File', $PSCommandPath, '-RepositoryRoot', $root, '-HashPath', $Path) | ConvertFrom-Json
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
    $null = Get-ContextFileMetadata $item $relative
    $metadata = Get-ContextFileHash $fullPath
    $relativePath = [IO.Path]::GetRelativePath($root, $fullPath)
    if ($IsWindows) { $relativePath = $relativePath.Replace('\', '/') }
    return [pscustomobject]@{
        Path = $relativePath
        Type = $metadata.Type
        Mode = $metadata.Mode
        Sha256 = $metadata.Sha256
    }
}

function Invoke-ContextNativeOutput {
    param([string]$Application, [string[]]$Arguments, [int[]]$AcceptedExitCodes = @(0), [switch]$WithResult, [ValidateRange(1, 30000)][int]$TimeoutMilliseconds = 10000)
    $start = [Diagnostics.ProcessStartInfo]::new($Application)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false, $true)
    $start.Environment['GIT_NO_LAZY_FETCH'] = '1'
    $start.Environment['GIT_ALLOW_PROTOCOL'] = ''
    foreach ($argument in $Arguments) {
        $start.ArgumentList.Add($argument)
    }
    $child = [Diagnostics.Process]::Start($start)
    try {
        $output = $child.StandardOutput.ReadToEndAsync()
        $errorOutput = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit($TimeoutMilliseconds)) { throw 'Native context inspection timed out; inspect the target manually.' }
        $diagnostic = $errorOutput.GetAwaiter().GetResult()
        if ($child.ExitCode -notin $AcceptedExitCodes) { throw "Native context inspection failed: $diagnostic" }
        if (-not [string]::IsNullOrWhiteSpace($diagnostic)) {
            throw "Native context inspection reported diagnostics; inspect the target manually: $diagnostic"
        }
        if ($WithResult) { return [pscustomobject]@{ ExitCode = $child.ExitCode; Output = $output.GetAwaiter().GetResult() } }
        return $output.GetAwaiter().GetResult()
    }
    finally {
        if (-not $child.HasExited) { $child.Kill($true) }
        $child.Dispose()
    }
}

function Get-ContextRawPaths {
    param([string]$Root, [string[]]$ContextPaths)
    $visible = Invoke-ContextGit $root @('ls-files', '-z', '--cached', '--others', '--exclude-standard') -WithResult
    $instructionCandidates = @(':(glob)**/AGENTS.md', ':(glob)**/*.instructions.md', ':(glob)**/CLAUDE.md', '.github/copilot-instructions.md')
    $selected = @($ContextPaths | ForEach-Object { ':(literal)' + $_ })
    $ignored = Invoke-ContextGit $root (@('ls-files', '-z', '--others', '--ignored', '--exclude-standard', '--') + $instructionCandidates + $selected) -WithResult
    return ($visible.Output + $ignored.Output).Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
}

function Get-ContextPaths {
    param([string]$Root, [string[]]$ContextPaths)
    $paths = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($relative in (Get-ContextRawPaths $root $ContextPaths)) {
        $fullPath = [IO.Path]::Combine($root, $relative)
        $entry = [IO.FileInfo]::new($fullPath)
        if ([IO.File]::Exists($fullPath) -or [IO.Directory]::Exists($fullPath) -or $null -ne $entry.LinkTarget) { $null = $paths.Add($relative) }
    }
    return @($paths)
}

function Assert-ContextGitMetadataEntry {
    param([IO.DirectoryInfo]$Directory, [IO.FileSystemInfo]$Item)
    if ($null -ne $item.LinkTarget -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Linked Git metadata entries require manual inspection; refs, objects and index must belong to the selected repository.'
    }
    $metadataRelative = [IO.Path]::GetRelativePath($Directory.FullName, $item.FullName).Replace([IO.Path]::DirectorySeparatorChar, [char]'/')
    if ($metadataRelative.Equals('objects/info/alternates', [StringComparison]::OrdinalIgnoreCase) -or $metadataRelative.Equals('objects/info/http-alternates', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Alternate Git object stores require manual inspection; objects must belong to the selected repository.'
    }
    if ($metadataRelative.Equals('info/grafts', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Legacy Git grafts require manual inspection; synthetic ancestry can publish unrelated objects.'
    }
}

function Assert-ContextGitMetadata {
    param([IO.DirectoryInfo]$Directory, [switch]$InChild)
    if (-not $InChild) {
        $shell = [IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
        $null = Invoke-ContextNativeOutput $shell @('-NoProfile', '-File', $PSCommandPath, '-RepositoryRoot', $Directory.Parent.FullName, '-MetadataPath', $Directory.FullName)
        return
    }
    if (($Directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Linked Git metadata directories require manual inspection.' }
    $pending = [Collections.Generic.Queue[IO.DirectoryInfo]]::new()
    $pending.Enqueue($Directory)
    $entries = 0
    $elapsed = [Diagnostics.Stopwatch]::StartNew()
    while ($pending.Count -gt 0) {
        foreach ($item in $pending.Dequeue().EnumerateFileSystemInfos()) {
            $entries++
            if ($entries -gt 100000 -or $elapsed.Elapsed.TotalSeconds -gt 10) {
                throw 'Git metadata inspection exceeded its bounds; inspect this target manually.'
            }
            Assert-ContextGitMetadataEntry $Directory $item
            if ($item -is [IO.DirectoryInfo]) { $pending.Enqueue($item) }
        }
    }
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
            Assert-ContextGitMetadata $markerDirectory
            return $directory.FullName
        }
        $directory = $directory.Parent
    }
    throw 'An embedded Git directory was not found; inspect this target manually.'
}

function Get-ContextGitPath {
    param([string]$Root, [string[]]$Arguments)
    $output = (Invoke-ContextGit $Root $Arguments -WithResult).Output
    if (-not $output.EndsWith("`n", [StringComparison]::Ordinal)) { throw 'Git path output lacks its line terminator; inspect this target manually.' }
    $terminatorLength = if ($IsWindows -and $output.EndsWith("`r`n", [StringComparison]::Ordinal)) { 2 } else { 1 }
    return [IO.Path]::GetFullPath($output.Substring(0, $output.Length - $terminatorLength))
}

function Get-ContextGitRoot {
    param([string]$Root)
    $expectedRoot = Get-ContextEmbeddedRoot $root
    $worktreeSetting = Invoke-ContextGit $root @('config', '--get', 'core.worktree') -AcceptedExitCodes @(0, 1) -WithResult
    if ($worktreeSetting.ExitCode -eq 0) {
        throw 'Configured core.worktree requires manual inspection; it can redirect the selected repository.'
    }
    $partialClone = Invoke-ContextGit $root @('config', '--name-only', '--get-regexp', '^(extensions\.partialclone|remote\..*\.promisor)$') -AcceptedExitCodes @(0, 1) -WithResult
    if ($partialClone.ExitCode -eq 0) { throw 'Partial/promisor repositories require manual inspection; missing objects can trigger remote helpers.' }
    $actualRoot = Get-ContextGitPath $root @('rev-parse', '--show-toplevel')
    $gitDirectory = Get-ContextGitPath $root @('rev-parse', '--absolute-git-dir')
    $commonDirectory = Get-ContextGitPath $root @('rev-parse', '--path-format=absolute', '--git-common-dir')
    if ($actualRoot -cne $expectedRoot -or $gitDirectory -cne [IO.Path]::Combine($expectedRoot, '.git')) {
        throw 'Git directory identity differs from the selected working copy; inspect this target manually.'
    }
    if ($commonDirectory -cne $gitDirectory) { throw 'Git common directory differs from the embedded metadata; inspect this target manually.' }
    return $actualRoot
}

function Get-ContextObservation {
    param([string]$Root, [string[]]$ContextPaths)
    if ((Get-ContextGitRoot $root) -cne $root) { throw 'Repository root changed during context inspection.' }
    $headResult = Invoke-ContextGit $root @('rev-parse', '--verify', '--quiet', 'HEAD') -AcceptedExitCodes @(0, 1) -WithResult
    $head = if ($headResult.ExitCode -eq 0) { $headResult.Output.Trim() } else { $null }
    $branch = [string](Invoke-ContextGit $root @('branch', '--show-current'))
    if ($null -eq $head -and [string]::IsNullOrWhiteSpace($branch)) { throw 'Missing detached HEAD requires manual inspection.' }
    $index = @(Invoke-ContextGit $root @('ls-files', '--stage'))
    if (@($index | Where-Object { $_ -match '^160000 ' }).Count -gt 0) {
        throw 'Submodule entries require manual inspection; status can execute submodule-local commands.'
    }
    if (@($index | Where-Object { $_ -match '^120000 ' }).Count -gt 0) {
        $symlinks = Invoke-ContextGit $root @('config', '--type=bool', '--get', 'core.symlinks') -AcceptedExitCodes @(0, 1) -WithResult
        if ($symlinks.ExitCode -eq 0 -and $symlinks.Output.Trim() -eq 'false') {
            throw 'Tracked symlinks with core.symlinks=false require manual inspection; plain-file substitutions can look clean.'
        }
    }
    $flags = @(Invoke-ContextGit $root @('ls-files', '-v'))
    if (@($flags | Where-Object { $_ -cmatch '^[a-zS] ' }).Count -gt 0) {
        throw 'Hidden index flags require manual inspection: assume-unchanged or skip-worktree can conceal changes.'
    }
    $filters = Invoke-ContextGit $root @('config', '--name-only', '--get-regexp', '^filter\..*\.(clean|process)$') -AcceptedExitCodes @(0, 1) -WithResult
    if ($filters.ExitCode -eq 0) {
        throw 'Configured clean/process filters require manual inspection; status may execute repository-controlled commands.'
    }
    $status = @(Invoke-ContextGit $root @('status', '--porcelain=v1', '--untracked-files=all'))
    $selected = @(foreach ($relative in $ContextPaths) { Get-ContextInput $root $relative })
    $paths = @(Get-ContextPaths $root $ContextPaths)
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
    if ([string]::IsNullOrEmpty($InspectionContext) -and [string]::IsNullOrEmpty($MetadataPath) -and [string]::IsNullOrEmpty($HashPath)) {
        $inspectionRoot = if ([IO.Path]::IsPathRooted($RepositoryRoot)) { [IO.Path]::GetFullPath($RepositoryRoot) }
        else { [IO.Path]::GetFullPath([IO.Path]::Combine((Get-Location).Path, $RepositoryRoot)) }
        $shell = [IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
        $contextJson = ConvertTo-Json -InputObject @($ContextPath) -Compress
        $output = Invoke-ContextNativeOutput $shell @('-NoProfile', '-File', $PSCommandPath, '-RepositoryRoot', $inspectionRoot, '-InspectionContext', $contextJson) -TimeoutMilliseconds 30000
        [Console]::Write($output)
        exit 0
    }
    if (-not [string]::IsNullOrEmpty($InspectionContext)) { $ContextPath = @(ConvertFrom-Json -InputObject $InspectionContext) }
    if (-not [string]::IsNullOrEmpty($MetadataPath)) {
        Assert-ContextGitMetadata ([IO.DirectoryInfo]::new($MetadataPath)) -InChild
        exit 0
    }
    if (-not [string]::IsNullOrEmpty($HashPath)) {
        $stream = [IO.FileStream]::new($HashPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::Read -bor [IO.FileShare]::Inheritable))
        try {
            $metadata = Get-ContextFileMetadata ([IO.FileInfo]::new($HashPath)) $HashPath $stream
            $hasher = [Security.Cryptography.SHA256]::Create()
            try {
                [pscustomobject]@{
                    Type = $metadata.Type
                    Mode = $metadata.Mode
                    Sha256 = [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
                } | ConvertTo-Json
            }
            finally { $hasher.Dispose() }
        }
        finally { $stream.Dispose() }
        exit 0
    }
    $gitApplication = (Get-Command git -CommandType Application | Select-Object -First 1).Source
    foreach ($selector in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_NAMESPACE')) {
        if ($null -ne [Environment]::GetEnvironmentVariable($selector)) {
            throw "Ambient Git override $selector prevents reliable target inspection; use a clean process or manual inspection."
        }
    }
    $requestedRoot = if ([IO.Path]::IsPathRooted($RepositoryRoot)) {
        [IO.Path]::GetFullPath($RepositoryRoot)
    } else { [IO.Path]::GetFullPath([IO.Path]::Combine((Get-Location).Path, $RepositoryRoot)) }
    $requestedRoot = [IO.Path]::TrimEndingDirectorySeparator($requestedRoot)
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
