#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$activeRepositoryExecutionLeases = [System.Collections.Concurrent.ConcurrentDictionary[string, bool]]::new()
$sharedExecutionLeaseStreamStates = @{}
$sharedExecutionLeaseStreamGate = [object]::new()
$sharedExecutionLeaseDirectoryMode = 365 # 0555
$sharedExecutionLeaseWritableDirectoryMode = 493 # 0755
$sharedExecutionLeaseFileMode = 438 # 0666
$privateExecutionLeaseDirectoryMode = 448 # 0700
$privateExecutionLeaseFileMode = 384 # 0600
$coordinationTypeName = 'Mississippi.RepositoryExecutionLeaseCoordination'
if ($null -eq ($coordinationTypeName -as [type])) {
    try {
        Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mississippi
{
    public static class RepositoryExecutionLeaseCoordination
    {
        private sealed class Entry
        {
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public int References;
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public static SemaphoreSlim Acquire(string key)
        {
            lock (Gate)
            {
                Entry entry;
                if (!Entries.TryGetValue(key, out entry))
                {
                    entry = new Entry();
                    Entries.Add(key, entry);
                }
                entry.References++;
                return entry.Semaphore;
            }
        }

        public static void Release(string key, SemaphoreSlim semaphore, bool acquired)
        {
            lock (Gate)
            {
                if (acquired) semaphore.Release();
                Entry entry;
                if (!Entries.TryGetValue(key, out entry) || !ReferenceEquals(entry.Semaphore, semaphore)) return;
                entry.References--;
                if (entry.References == 0)
                {
                    Entries.Remove(key);
                    semaphore.Dispose();
                }
            }
        }
    }
}
'@ -ErrorAction Stop
    }
    catch {
        if ($null -eq ($coordinationTypeName -as [type])) { throw }
    }
}
$outputTypeName = 'Mississippi.BoundedProcessOutput'
if ($null -eq ($outputTypeName -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mississippi
{
    public sealed class BoundedProcessOutput
    {
        private readonly ConcurrentQueue<string> Lines = new ConcurrentQueue<string>();
        private readonly int MaxLines;
        private readonly int MaxLineLength;

        public BoundedProcessOutput(int maxLines, int maxLineLength)
        {
            MaxLines = maxLines;
            MaxLineLength = maxLineLength;
        }

        public bool Closed { get; private set; }
        public bool Truncated { get; private set; }
        public DataReceivedEventHandler Handler { get { return OnData; } }
        public string Text { get { return string.Join(Environment.NewLine, Lines.ToArray()).TrimEnd('\r', '\n'); } }

        private void OnData(object sender, DataReceivedEventArgs args)
        {
            if (args.Data == null)
            {
                Closed = true;
                return;
            }
            var line = args.Data;
            if (line.Length > MaxLineLength)
            {
                line = line.Substring(0, MaxLineLength);
                Truncated = true;
            }
            Lines.Enqueue(line);
            while (Lines.Count > MaxLines)
            {
                string ignored;
                Lines.TryDequeue(out ignored);
                Truncated = true;
            }
        }
    }
}
'@ -ErrorAction Stop
}

function Test-RepositoryExecutionLeaseSharedMode {
    [CmdletBinding()]
    param([string]$LeaseDirectory)

    return $env:MISSISSIPPI_SHARED_WORKTREE -eq 'true' -or -not [string]::IsNullOrWhiteSpace($LeaseDirectory)
}

function Register-RepositoryExecutionLeaseIdentity {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Identity)

    if (-not $activeRepositoryExecutionLeases.TryAdd($Identity, $true)) {
        throw "Repository execution lease is already held in this process for '$Identity'. Use -ExistingLease for supported reentrancy."
    }
}

function Unregister-RepositoryExecutionLeaseIdentity {
    [CmdletBinding()]
    param([string]$Identity)

    if (-not [string]::IsNullOrWhiteSpace($Identity)) {
        $removed = $false
        $activeRepositoryExecutionLeases.TryRemove($Identity, [ref]$removed) | Out-Null
    }
}

function Acquire-SharedRepositoryExecutionLeaseStream {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $key = (Get-Item -LiteralPath $Path -Force -ErrorAction Stop).FullName
    if ($IsWindows) { $key = $key.ToLowerInvariant() }
    [System.Threading.Monitor]::Enter($sharedExecutionLeaseStreamGate)
    try {
        if ($sharedExecutionLeaseStreamStates.ContainsKey($key)) {
            $state = $sharedExecutionLeaseStreamStates[$key]
            $state.RefCount++
            return $state
        }
        $stream = [System.IO.FileStream]::new($Path, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
        $state = [pscustomobject]@{ Key = $key; Stream = $stream; RefCount = 1; Gate = [object]::new(); Locked = $false }
        $sharedExecutionLeaseStreamStates[$key] = $state
        return $state
    }
    finally {
        [System.Threading.Monitor]::Exit($sharedExecutionLeaseStreamGate)
    }
}

function Release-SharedRepositoryExecutionLeaseStream {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$State)

    [System.Threading.Monitor]::Enter($sharedExecutionLeaseStreamGate)
    try {
        $State.RefCount--
        if ($State.RefCount -le 0) {
            try { $State.Stream.Dispose() }
            finally { $sharedExecutionLeaseStreamStates.Remove($State.Key) }
        }
    }
    finally {
        [System.Threading.Monitor]::Exit($sharedExecutionLeaseStreamGate)
    }
}

function Unlock-SharedRepositoryExecutionLeaseSlot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$State,
        [Parameter(Mandatory)][long]$Offset
    )

    [System.Threading.Monitor]::Enter($State.Gate)
    try {
        if ($State.Locked) {
            $State.Stream.Unlock($Offset, 1)
            $State.Locked = $false
        }
    }
    finally {
        [System.Threading.Monitor]::Exit($State.Gate)
    }
}

function Get-RepositoryExecutionLeaseHash {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$CanonicalRepoRoot)

    $keyRoot = Get-RepositoryExecutionLeaseIdentityKey -CanonicalRepoRoot $CanonicalRepoRoot
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($keyRoot)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($bytes)
    }
    finally {
        $sha256.Dispose()
    }
    return ,$hash
}

function Get-RepositoryExecutionUnixIdentity {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if ($IsWindows) { return $null }
    $statCommand = Get-Command -Name stat -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $statCommand) { return $null }
    foreach ($arguments in @(@('-c', '%d:%i'), @('-f', '%d:%i'))) {
        try {
            $identity = (& $statCommand.Source @arguments -- $Path 2>$null | Out-String).Trim()
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($identity)) { return $identity }
        }
        catch {
            Write-Verbose "Unable to read Unix filesystem identity for '$Path' with stat dialect '$($arguments[0])': $($_.Exception.Message)"
        }
    }
    return $null
}

function Get-RepositoryExecutionLeaseIdentityKey {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$CanonicalRepoRoot)

    $identity = Get-RepositoryExecutionUnixIdentity -Path $CanonicalRepoRoot
    if (-not [string]::IsNullOrWhiteSpace($identity)) { return "filesystem:$identity" }
    if ((Get-RepositoryPathComparison -RepoRoot $CanonicalRepoRoot) -eq [System.StringComparison]::OrdinalIgnoreCase) {
        return $CanonicalRepoRoot.ToLowerInvariant()
    }
    return $CanonicalRepoRoot
}

function Get-RepositoryExecutionLeaseFileIdentityKey {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $identity = Get-RepositoryExecutionUnixIdentity -Path $Path
    if (-not [string]::IsNullOrWhiteSpace($identity)) { return "filesystem-file:$identity" }
    return [System.IO.Path]::GetFullPath($Path).ToLowerInvariant()
}

function New-RepositoryExecutionLeaseProcessSemaphore {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Identity)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Identity)) }
    finally { $sha256.Dispose() }
    $key = ([System.BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant())
    return [pscustomobject]@{
        Key = $key
        Semaphore = [Mississippi.RepositoryExecutionLeaseCoordination]::Acquire($key)
        Acquired = $false
    }
}

function Set-RepositoryExecutionLeaseUnixMode {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$Mode
    )

    if ($IsWindows) { return }

    try {
        $method = [System.IO.File].GetMethods() |
            Where-Object { $_.Name -eq 'SetUnixFileMode' -and $_.GetParameters().Count -eq 2 -and $_.GetParameters()[0].ParameterType -eq [string] } |
            Select-Object -First 1
        if ($null -ne $method) {
            $modeValue = [Enum]::ToObject($method.GetParameters()[1].ParameterType, $Mode)
            $method.Invoke($null, @($Path, $modeValue)) | Out-Null
        }
        else {
            $modeText = switch ($Mode) {
                365 { '555'; break }
                438 { '666'; break }
                448 { '700'; break }
                384 { '600'; break }
                default { [Convert]::ToString($Mode, 8) }
            }
            & chmod $modeText -- $Path 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "chmod exited with code $LASTEXITCODE" }
        }
    }
    catch {
        throw "Unable to set shared execution lease permissions on '$Path': $($_.Exception.Message)"
    }

    Test-RepositoryExecutionLeaseUnixMode -Path $Path -Mode $Mode
}

function Test-RepositoryExecutionLeaseUnixMode {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$Mode
    )

    if ($IsWindows) { return }

    try {
        $method = [System.IO.File].GetMethods() |
            Where-Object { $_.Name -eq 'GetUnixFileMode' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType -eq [string] } |
            Select-Object -First 1
        if ($null -ne $method) {
            $actualMode = [int]$method.Invoke($null, @($Path))
        }
        else {
            $modeText = (& stat -c '%a' -- $Path 2>$null | Out-String).Trim()
            if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($modeText)) {
                $modeText = (& stat -f '%Lp' -- $Path 2>$null | Out-String).Trim()
            }
            if ([string]::IsNullOrWhiteSpace($modeText) -or $LASTEXITCODE -ne 0) { throw 'No compatible Unix mode API or stat command was available.' }
            $actualMode = [Convert]::ToInt32($modeText, 8)
        }
    }
    catch {
        throw "Unable to inspect shared execution lease permissions on '$Path': $($_.Exception.Message)"
    }

    if (([int]$actualMode -band [int]$Mode) -ne [int]$Mode) {
        throw "Shared execution lease permissions on '$Path' are insufficient for all participating accounts."
    }

    $writeBits = 146 # 0222
    if (($Mode -band $writeBits) -eq 0 -and ($actualMode -band $writeBits) -ne 0) {
        throw "Shared execution lease path '$Path' must not be writable by participating accounts."
    }
}

function Get-RepositoryRoot {
    [CmdletBinding()]
    param(
        [string]$StartPath = (Get-Location).Path
    )

    $resolved = Resolve-Path -LiteralPath $StartPath
    $current = $resolved.Path

    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current '.git')) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            break
        }
        $current = $parent
    }

    throw "Unable to locate repository root from '$StartPath'."
}

function Get-RepositoryPathComparison {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)

    if ($IsWindows) { return [System.StringComparison]::OrdinalIgnoreCase }
    if (-not $IsMacOS) { return [System.StringComparison]::Ordinal }

    $probeName = '.mississippi-case-probe-' + [guid]::NewGuid().ToString('N')
    $probePath = Join-Path $RepoRoot $probeName
    try {
        New-Item -ItemType Directory -LiteralPath $probePath -Force -ErrorAction Stop | Out-Null
        if (Test-Path -LiteralPath (Join-Path $RepoRoot $probeName.ToUpperInvariant())) {
            return [System.StringComparison]::OrdinalIgnoreCase
        }
    }
    catch {
        Write-Verbose "Unable to prove case-insensitive path comparison for '$RepoRoot': $($_.Exception.Message)"
    }
    finally {
        Remove-Item -LiteralPath $probePath -Recurse -Force -ErrorAction SilentlyContinue
    }

    return [System.StringComparison]::Ordinal
}

function Assert-RepositoryExecutionLeaseDirectoryAncestors {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetPathRoot($fullPath)
    $segments = @($fullPath.Substring($root.Length).Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) | Where-Object { $_ })
    $current = $root
    foreach ($segment in $segments) {
        $candidate = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $candidate)) { break }
        $item = Get-Item -LiteralPath $candidate -Force -ErrorAction Stop
        if ([bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Lease directory path contains a reparse-point ancestor: '$candidate'."
        }
        $current = $item.FullName
    }
}

function Resolve-RepositoryExecutionLeaseDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CanonicalRepoRoot,
        [string]$LeaseDirectory
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($LeaseDirectory)) {
        $userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
        if ([string]::IsNullOrWhiteSpace($userProfile) -or -not [System.IO.Path]::IsPathRooted($userProfile) -or -not (Test-Path -LiteralPath $userProfile -PathType Container)) {
            throw "Unable to resolve the default repository execution lease directory because the user profile directory is unavailable. Specify -LeaseDirectory with an absolute path."
        }
        Join-Path $userProfile '.mississippi/execution-leases'
    }
    elseif ([System.IO.Path]::IsPathRooted($LeaseDirectory)) {
        $LeaseDirectory
    }
    else {
        Join-Path $CanonicalRepoRoot $LeaseDirectory
    }
    $candidate = [System.IO.Path]::GetFullPath($candidate)
    $missingSegments = [System.Collections.Generic.List[string]]::new()
    $existingPath = $candidate
    while (-not (Test-Path -LiteralPath $existingPath)) {
        $segment = Split-Path -Leaf $existingPath
        if ([string]::IsNullOrWhiteSpace($segment)) { throw "Unable to resolve lease directory parent for '$candidate'." }
        $missingSegments.Add($segment)
        $parent = Split-Path -Parent $existingPath
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $existingPath) { throw "Unable to resolve lease directory parent for '$candidate'." }
        $existingPath = $parent
    }
    $resolved = (Resolve-Path -LiteralPath $existingPath -ErrorAction Stop).Path
    for ($index = $missingSegments.Count - 1; $index -ge 0; $index--) {
        $resolved = Join-Path $resolved $missingSegments[$index]
    }
    return [System.IO.Path]::GetFullPath($resolved)
}

function Ensure-RepositoryExecutionLeaseDirectory {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    Assert-RepositoryExecutionLeaseDirectoryAncestors -Path $Path
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Lease directory is not a trusted private directory: '$Path'."
        }
        Assert-RepositoryExecutionLeaseDirectoryAncestors -Path $Path
        return $false
    }

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    try {
        New-Item -ItemType Directory -Path $Path -ErrorAction Stop | Out-Null
        Assert-RepositoryExecutionLeaseDirectoryAncestors -Path $Path
        return $true
    }
    catch {
        if (Test-Path -LiteralPath $Path -PathType Container) {
            $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
            if ([bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                throw "Lease directory is not a trusted private directory: '$Path'."
            }
            Assert-RepositoryExecutionLeaseDirectoryAncestors -Path $Path
            return $false
        }
        throw
    }
}

function Wait-RepositoryExecutionLeaseInitialization {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LeasePath,
        [Parameter(Mandatory)][string]$MetadataPath
    )

    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        if ((Test-Path -LiteralPath $LeasePath -PathType Leaf) -and (Test-Path -LiteralPath $MetadataPath -PathType Leaf)) {
            try {
                if ($IsWindows) {
                    Test-RepositoryExecutionLeaseWindowsAccess -Path (Split-Path -Parent $LeasePath)
                    Test-RepositoryExecutionLeaseWindowsAccess -Path $LeasePath
                    Test-RepositoryExecutionLeaseWindowsAccess -Path $MetadataPath
                }
                else {
                    Test-RepositoryExecutionLeaseUnixMode -Path (Split-Path -Parent $LeasePath) -Mode $sharedExecutionLeaseDirectoryMode
                    Test-RepositoryExecutionLeaseUnixMode -Path $LeasePath -Mode $sharedExecutionLeaseFileMode
                    Test-RepositoryExecutionLeaseUnixMode -Path $MetadataPath -Mode $sharedExecutionLeaseFileMode
                }
                return $true
            }
            catch {
                Write-Verbose "Shared lease initialization is visible but its permissions are not fully published yet: $($_.Exception.Message)"
            }
        }
        Start-Sleep -Milliseconds 50
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

function Create-SharedRepositoryExecutionLeaseFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LeaseDirectory,
        [Parameter(Mandatory)][string]$LeasePath
    )

    $placeholder = [System.IO.FileStream]::new($LeasePath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
    try { $placeholder.SetLength(1); $placeholder.Flush($true) }
    finally { $placeholder.Dispose() }
    if (-not $IsWindows) { Set-RepositoryExecutionLeaseUnixMode -Path $LeasePath -Mode $sharedExecutionLeaseFileMode }
    if ($IsWindows) { Set-RepositoryExecutionLeaseWindowsAccess -Path $LeasePath }
}

function Initialize-SharedRepositoryExecutionLeasePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LeaseDirectory,
        [Parameter(Mandatory)][string]$LeasePath,
        [Parameter(Mandatory)][string]$MetadataPath,
        [Parameter(Mandatory)][bool]$LeaseDirectoryCreated
    )

    if (-not $LeaseDirectoryCreated) {
        $initialized = Wait-RepositoryExecutionLeaseInitialization -LeasePath $LeasePath -MetadataPath $MetadataPath
        if (-not $initialized) {
            throw "Shared lease directory '$LeaseDirectory' exists without its coordination file. Refusing to modify a caller-owned directory; pre-provision '$LeasePath' or use a new dedicated coordination directory."
        }
        return
    }
    Create-SharedRepositoryExecutionLeaseFile -LeaseDirectory $LeaseDirectory -LeasePath $LeasePath
    Ensure-RepositoryExecutionLeaseMetadataFile -Path $MetadataPath -SharedLease $true
    Set-RepositoryExecutionLeaseUnixMode -Path $LeaseDirectory -Mode $sharedExecutionLeaseDirectoryMode
}

function Get-RepositoryExecutionLeaseCandidatePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CanonicalRepoRoot,
        [string]$LeaseDirectory
    )

    $hash = Get-RepositoryExecutionLeaseHash -CanonicalRepoRoot $CanonicalRepoRoot
    $sharedLease = Test-RepositoryExecutionLeaseSharedMode -LeaseDirectory $LeaseDirectory
    $fileName = if ($sharedLease) { 'shared.lease' } else { ([System.BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()) + '.lease' }
    $resolvedLeaseDirectory = Resolve-RepositoryExecutionLeaseDirectory -CanonicalRepoRoot $CanonicalRepoRoot -LeaseDirectory $LeaseDirectory
    return Join-Path $resolvedLeaseDirectory $fileName
}

function Get-RepositoryExecutionLeasePathForRoot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CanonicalRepoRoot,
        [string]$LeaseDirectory
    )

    $sharedLease = Test-RepositoryExecutionLeaseSharedMode -LeaseDirectory $LeaseDirectory
    $defaultLeaseDirectory = [string]::IsNullOrWhiteSpace($LeaseDirectory)
    $leasePath = Get-RepositoryExecutionLeaseCandidatePath -CanonicalRepoRoot $CanonicalRepoRoot -LeaseDirectory $LeaseDirectory
    $resolvedLeaseDirectory = Split-Path -Parent $leasePath
    $leaseDirectoryCreated = Ensure-RepositoryExecutionLeaseDirectory -Path $resolvedLeaseDirectory
    $metadataPath = "$leasePath.metadata"

    if ($sharedLease) {
        if ($IsWindows) {
            if ($leaseDirectoryCreated) { Set-RepositoryExecutionLeaseWindowsAccess -Path $resolvedLeaseDirectory }
        }
        Initialize-SharedRepositoryExecutionLeasePath -LeaseDirectory $resolvedLeaseDirectory -LeasePath $leasePath -MetadataPath $metadataPath -LeaseDirectoryCreated $leaseDirectoryCreated
    }
    elseif ($defaultLeaseDirectory) {
        Set-RepositoryExecutionLeaseUnixMode -Path $resolvedLeaseDirectory -Mode $privateExecutionLeaseDirectoryMode
    }
    return $leasePath
}

function Get-RepositoryExecutionLeasePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [string]$LeaseDirectory
    )

    $canonicalRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
    return Get-RepositoryExecutionLeasePathForRoot -CanonicalRepoRoot $canonicalRoot -LeaseDirectory $LeaseDirectory
}

function Resolve-ReparseTargetPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Item,
        [Parameter(Mandatory)][string]$Candidate
    )

    $target = @($Item.Target | Select-Object -First 1)[0]
    if ([string]::IsNullOrWhiteSpace([string]$target)) {
        throw "Unable to resolve worktree path component '$Candidate'."
    }
    if (-not [System.IO.Path]::IsPathRooted([string]$target)) {
        $target = Join-Path (Split-Path -Parent $Candidate) ([string]$target)
    }
    return [System.IO.Path]::GetFullPath([string]$target)
}

function Resolve-ReparsePathComponent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [System.Collections.Generic.HashSet[string]]$SeenTargets
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetPathRoot($fullPath)
    $segments = @($fullPath.Substring($root.Length).Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) | Where-Object { $_ })
    $current = $root
    for ($index = 0; $index -lt $segments.Count; $index++) {
        $candidate = Join-Path $current $segments[$index]
        $item = Get-Item -LiteralPath $candidate -Force -ErrorAction Stop
        if (-not [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            $current = $item.FullName
            continue
        }

        if (-not $SeenTargets.Add([System.IO.Path]::GetFullPath($candidate))) {
            throw "Worktree path resolution loop detected at '$candidate'."
        }
        $resolvedTarget = Resolve-ReparseTargetPath -Item $item -Candidate $candidate
        if ($index -lt ($segments.Count - 1)) {
            $remaining = $segments[($index + 1)..($segments.Count - 1)] -join [System.IO.Path]::DirectorySeparatorChar
            $resolvedTarget = Join-Path $resolvedTarget $remaining
        }
        return Resolve-ReparsePathComponent -Path $resolvedTarget -SeenTargets $SeenTargets
    }

    return $current
}

function Resolve-RepositoryExecutionRoot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)

    return Resolve-RepositoryExecutionPath -Path $RepoRoot
}

function Resolve-RepositoryExecutionPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Execution path does not exist: '$Path'."
    }

    $comparisonRoot = Split-Path -Parent $fullPath
    if ([string]::IsNullOrWhiteSpace($comparisonRoot)) { $comparisonRoot = [System.IO.Path]::GetPathRoot($fullPath) }
    $comparison = Get-RepositoryPathComparison -RepoRoot $comparisonRoot
    $comparer = if ($comparison -eq [System.StringComparison]::OrdinalIgnoreCase) { [System.StringComparer]::OrdinalIgnoreCase } else { [System.StringComparer]::Ordinal }
    $seenTargets = [System.Collections.Generic.HashSet[string]]::new($comparer)
    return Resolve-ReparsePathComponent -Path $fullPath -SeenTargets $seenTargets
}

function Get-ReentrantRepositoryExecutionLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][object]$ExistingLease,
        [string]$LeaseDirectory
    )

    if ($null -eq $ExistingLease.Stream -or $ExistingLease.Stream.SafeFileHandle.IsClosed -or -not $ExistingLease.Stream.CanRead) {
        throw 'Existing repository execution lease handle is closed or unavailable.'
    }
    $requestedRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
    $existingRoot = Resolve-RepositoryExecutionRoot -RepoRoot ([string]$ExistingLease.RepositoryRoot)
    $comparison = Get-RepositoryPathComparison -RepoRoot $requestedRoot
    if (-not [string]::Equals($requestedRoot, $existingRoot, $comparison)) {
        throw "Existing lease belongs to '$existingRoot', not requested worktree '$requestedRoot'."
    }
    $requestedLeasePath = Get-RepositoryExecutionLeaseCandidatePath -CanonicalRepoRoot $requestedRoot -LeaseDirectory $LeaseDirectory
    $leaseComparison = Get-RepositoryPathComparison -RepoRoot (Split-Path -Parent $requestedLeasePath)
    if (-not [string]::Equals([System.IO.Path]::GetFullPath($requestedLeasePath), [System.IO.Path]::GetFullPath([string]$ExistingLease.Path), $leaseComparison)) {
        throw "Existing lease belongs to coordination path '$($ExistingLease.Path)', not requested path '$requestedLeasePath'."
    }
    return [pscustomobject]@{
        Path = $ExistingLease.Path
        OperationId = $ExistingLease.OperationId
        RepositoryRoot = $ExistingLease.RepositoryRoot
        Stream = $ExistingLease.Stream
        OwnsStream = $false
    }
}

function Test-RepositoryExecutionLeaseWindowsAccess {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not $IsWindows) { return }
    try {
        $sid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
        $acl = Get-Acl -LiteralPath $Path -ErrorAction Stop
        $requiredRights = [System.Security.AccessControl.FileSystemRights]::Modify
        $accessRules = $acl.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier])
        $hasAccess = @(
            $accessRules | Where-Object {
                try {
                    $identity = $_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier])
                    $identity.Value -eq $sid.Value -and $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
                        (([System.Security.AccessControl.FileSystemRights]$_.FileSystemRights -band $requiredRights) -eq $requiredRights)
                }
                catch { $false }
            }
        ).Count -gt 0
        if (-not $hasAccess) { throw 'BUILTIN\Users does not have Modify access.' }
    }
    catch {
        throw "Shared Windows lease path '$Path' is not accessible to all participating users: $($_.Exception.Message)"
    }
}

function Set-RepositoryExecutionLeaseWindowsAccess {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not $IsWindows) { return }
    try {
        $sid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
        $acl = Get-Acl -LiteralPath $Path -ErrorAction Stop
        $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
        $inheritance = if ($item.PSIsContainer) {
            [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
        }
        else {
            [System.Security.AccessControl.InheritanceFlags]::None
        }
        $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
            $sid,
            [System.Security.AccessControl.FileSystemRights]::Modify,
            $inheritance,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow)
        $acl.SetAccessRule($rule)
        Set-Acl -LiteralPath $Path -AclObject $acl -ErrorAction Stop
        Test-RepositoryExecutionLeaseWindowsAccess -Path $Path
    }
    catch {
        throw "Unable to provision shared Windows lease access on '$Path': $($_.Exception.Message)"
    }
}

function Assert-RepositoryExecutionLeaseFile {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
        throw "Lease path is not a regular file: '$Path'."
    }
}

function Get-RepositoryExecutionProcessStartUtc {
    [CmdletBinding()]
    param()

    try { return (Get-Process -Id $PID -ErrorAction Stop).StartTime.ToUniversalTime() }
    catch { return $null }
}

function New-RepositoryExecutionLeaseContext {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$OperationId,
        [string]$LeaseDirectory
    )

    $sharedLease = Test-RepositoryExecutionLeaseSharedMode -LeaseDirectory $LeaseDirectory
    if ([string]::IsNullOrWhiteSpace($LeaseDirectory) -and $env:MISSISSIPPI_SHARED_WORKTREE -eq 'true') {
        throw 'Cross-account shared worktrees require an explicit trusted -LeaseDirectory.'
    }
    $canonicalRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
    $leasePath = Get-RepositoryExecutionLeasePathForRoot -CanonicalRepoRoot $canonicalRoot -LeaseDirectory $LeaseDirectory
    $metadataPath = "$leasePath.metadata"
    Assert-RepositoryExecutionLeaseFile -Path $leasePath
    Assert-RepositoryExecutionLeaseFile -Path $metadataPath
    $repositoryKey = [System.BitConverter]::ToString((Get-RepositoryExecutionLeaseHash -CanonicalRepoRoot $canonicalRoot)).Replace('-', '').ToLowerInvariant()
    $comparison = Get-RepositoryPathComparison -RepoRoot $canonicalRoot
    $identityLeasePath = if ($comparison -eq [System.StringComparison]::OrdinalIgnoreCase) { $leasePath.ToLowerInvariant() } else { $leasePath }
    $processStartUtc = Get-RepositoryExecutionProcessStartUtc
    $metadata = [ordered]@{
        operationId = $OperationId
        repositoryKey = $repositoryKey
        repositoryRoot = if ($canonicalRoot.Length -gt 256) { $canonicalRoot.Substring(0, 256) } else { $canonicalRoot }
        processId = $PID
        processStartUtc = if ($null -eq $processStartUtc) { $null } else { $processStartUtc.ToString('o') }
        startedUtc = (Get-Date).ToUniversalTime().ToString('o')
    } | ConvertTo-Json -Compress
    return [pscustomobject]@{
        SharedLease = $sharedLease
        CanonicalRoot = $canonicalRoot
        LeasePath = $leasePath
        MetadataPath = $metadataPath
        LeaseIdentity = "$repositoryKey|$identityLeasePath"
        ProcessSynchronizationKey = if ($sharedLease) { Get-RepositoryExecutionLeaseFileIdentityKey -Path $leasePath } else { "$repositoryKey|$identityLeasePath" }
        OperationId = $OperationId
        Metadata = $metadata
    }
}

function Lock-SharedRepositoryExecutionLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$State,
        [Parameter(Mandatory)][string]$Path
    )

    [System.Threading.Monitor]::Enter($State.Gate)
    try {
        if ($State.Locked) { throw "Shared coordination file is already held in this process. Use separate coordination directories for concurrent worktrees." }
        $State.Stream.Lock(0, 1)
        $State.Locked = $true
        Test-RepositoryExecutionLeaseUnixMode -Path $Path -Mode $sharedExecutionLeaseFileMode
        return [long]0
    }
    finally {
        [System.Threading.Monitor]::Exit($State.Gate)
    }
}

function Lock-PrivateRepositoryExecutionLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Stream,
        [Parameter(Mandatory)][string]$Path
    )

    Set-RepositoryExecutionLeaseUnixMode -Path $Path -Mode $privateExecutionLeaseFileMode
    $Stream.Lock(0, 1)
    return [long]0
}

function Release-RepositoryExecutionLeaseProcessSemaphore {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Lease)

    if ($null -eq $Lease.ProcessSemaphore) { return }
    try {
        [Mississippi.RepositoryExecutionLeaseCoordination]::Release(
            $Lease.ProcessSemaphore.Key,
            $Lease.ProcessSemaphore.Semaphore,
            [bool]$Lease.ProcessSemaphore.Acquired)
    }
    finally { $Lease.ProcessSemaphore.Acquired = $false }
}

function Release-RepositoryExecutionLeaseResources {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Resources)

    try {
        if ($null -ne $Resources.StreamState) {
            try {
                if ($null -ne $Resources.LeaseOffset) { Unlock-SharedRepositoryExecutionLeaseSlot -State $Resources.StreamState -Offset ([long]$Resources.LeaseOffset) }
            }
            finally { Release-SharedRepositoryExecutionLeaseStream -State $Resources.StreamState }
        }
        elseif ($null -ne $Resources.Stream) {
            $Resources.Stream.Dispose()
        }
    }
    finally {
        Release-RepositoryExecutionLeaseProcessSemaphore -Lease $Resources
        if ($Resources.LeaseRegistered) { Unregister-RepositoryExecutionLeaseIdentity -Identity $Resources.LeaseIdentity }
    }
}

function Assert-RepositoryExecutionLeaseNotHeldByCurrentProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][bool]$SharedLease
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $owner = $null
    try { $owner = Get-Content -LiteralPath $Path -Raw -ErrorAction Stop | ConvertFrom-Json }
    catch { return }
    if ($null -eq $owner -or [string]$owner.processId -ne [string]$PID) { return }
    $recordedStart = $null
    $processStartProperty = $owner.PSObject.Properties['processStartUtc']
    if ($null -ne $processStartProperty) { $recordedStart = $processStartProperty.Value }
    $currentStart = Get-RepositoryExecutionProcessStartUtc
    if ([string]::IsNullOrWhiteSpace([string]$recordedStart) -or $null -eq $currentStart) { return }
    try {
        $recordedStartTime = [DateTime]::Parse([string]$recordedStart).ToUniversalTime()
        if ([Math]::Abs(($recordedStartTime - $currentStart).TotalSeconds) -gt 1) { return }
    }
    catch { return }
    if ($SharedLease) {
        throw 'Shared coordination file is already held in this process; repository execution lease is already held. Use separate coordination directories for concurrent worktrees.'
    }
    throw "Repository execution lease is already held in this process for '$Path'. Use -ExistingLease for supported reentrancy."
}

function Test-RepositoryExecutionLeaseLockConflict {
    [CmdletBinding()]
    param([Parameter(Mandatory)][Exception]$Exception)

    $current = $Exception
    while ($null -ne $current) {
        $errorCode = $current.HResult -band 0xFFFF
        if ($errorCode -in @(11, 32, 33)) { return $true }
        if ($current.Message -match '(?i)(resource temporarily unavailable|would block|sharing violation|lock violation|already locked)') { return $true }
        $current = $current.InnerException
    }
    return $false
}

function Get-RepositoryExecutionLeaseOwner {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    try { return (Get-Content -LiteralPath $Path -Raw -ErrorAction Stop).Trim() }
    catch {
        Write-Verbose "Unable to read the current lease owner from '$Path': $($_.Exception.Message)"
        return ''
    }
}

function Throw-RepositoryExecutionLeaseOpenFailure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Context,
        [Parameter(Mandatory)][object]$Resources,
        [Parameter(Mandatory)][Exception]$Exception,
        [switch]$Unauthorized
    )

    $lockConflict = Test-RepositoryExecutionLeaseLockConflict -Exception $Exception
    Release-RepositoryExecutionLeaseResources -Resources $Resources
    if ($Unauthorized) {
        if ($Context.SharedLease) {
            throw "Shared execution lease '$($Context.LeasePath)' is not writable by this account. Ensure existing lease files in the shared directory are writable by all participating accounts."
        }
        throw $Exception
    }
    if (-not $lockConflict) { throw $Exception }
    if ($Context.SharedLease) {
        throw "Worktree execution lease is held for '$($Context.CanonicalRoot)' in shared coordination slot. Use a separate worktree or wait for the active operation."
    }
    $owner = Get-RepositoryExecutionLeaseOwner -Path $Context.MetadataPath
    throw "Worktree execution lease is held for '$($Context.CanonicalRoot)'. Current owner: $owner. Use a separate worktree or wait for the active operation."
}

function Ensure-RepositoryExecutionLeaseMetadataFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][bool]$SharedLease
    )

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        Assert-RepositoryExecutionLeaseFile -Path $Path
        return
    }
    $directoryRelaxed = $false
    if ($SharedLease -and -not $IsWindows) {
        Set-RepositoryExecutionLeaseUnixMode -Path (Split-Path -Parent $Path) -Mode $sharedExecutionLeaseWritableDirectoryMode
        $directoryRelaxed = $true
    }
    try {
        $stream = [System.IO.FileStream]::new($Path, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
        try { $stream.SetLength(0); $stream.Flush($true) }
        finally { $stream.Dispose() }
        if ($IsWindows -and $SharedLease) { Set-RepositoryExecutionLeaseWindowsAccess -Path $Path }
        elseif (-not $IsWindows -and $SharedLease) { Set-RepositoryExecutionLeaseUnixMode -Path $Path -Mode $sharedExecutionLeaseFileMode }
        elseif (-not $IsWindows) { Set-RepositoryExecutionLeaseUnixMode -Path $Path -Mode $privateExecutionLeaseFileMode }
    }
    finally {
        if ($directoryRelaxed) { Set-RepositoryExecutionLeaseUnixMode -Path (Split-Path -Parent $Path) -Mode $sharedExecutionLeaseDirectoryMode }
    }
}

function Open-RepositoryExecutionLeaseResources {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Context)

    $resources = [pscustomobject]@{
        LeaseIdentity = $Context.LeaseIdentity
        LeaseRegistered = $false
        Stream = $null
        StreamState = $null
        LeaseOffset = $null
        ProcessSemaphore = $null
        ProcessSemaphoreAcquired = $false
    }
    try {
        $resources.ProcessSemaphore = New-RepositoryExecutionLeaseProcessSemaphore -Identity $Context.ProcessSynchronizationKey
        if (-not $resources.ProcessSemaphore.Semaphore.Wait(0)) {
            if ($Context.SharedLease) {
                throw 'Shared coordination file is already held in this process; repository execution lease is already held. Use separate coordination directories for concurrent worktrees.'
            }
            throw 'Repository execution lease is already held in this process. Use -ExistingLease for supported reentrancy.'
        }
        $resources.ProcessSemaphore.Acquired = $true
        Register-RepositoryExecutionLeaseIdentity -Identity $Context.LeaseIdentity
        $resources.LeaseRegistered = $true
        if ($Context.SharedLease) {
            $resources.StreamState = Acquire-SharedRepositoryExecutionLeaseStream -Path $Context.LeasePath
            $resources.Stream = $resources.StreamState.Stream
            $resources.LeaseOffset = Lock-SharedRepositoryExecutionLease -State $resources.StreamState -Path $Context.LeasePath
        }
        else {
            $resources.Stream = [System.IO.FileStream]::new($Context.LeasePath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::Read)
            $resources.LeaseOffset = Lock-PrivateRepositoryExecutionLease -Stream $resources.Stream -Path $Context.LeasePath
        }
        try {
            Assert-RepositoryExecutionLeaseNotHeldByCurrentProcess -Path $Context.MetadataPath -SharedLease $Context.SharedLease
        }
        catch {
            Clear-RepositoryExecutionLeaseMetadata -Lease ([pscustomobject]@{
                Path = $Context.LeasePath
                MetadataPath = $Context.MetadataPath
                SharedStreamState = $resources.StreamState
            })
        }
        return $resources
    }
    catch [System.UnauthorizedAccessException] {
        Throw-RepositoryExecutionLeaseOpenFailure -Context $Context -Resources $resources -Exception $_.Exception -Unauthorized
    }
    catch [System.IO.IOException] {
        Throw-RepositoryExecutionLeaseOpenFailure -Context $Context -Resources $resources -Exception $_.Exception
    }
    catch {
        $exception = $_.Exception
        $lockConflict = Test-RepositoryExecutionLeaseLockConflict -Exception $exception
        if ($lockConflict) {
            Throw-RepositoryExecutionLeaseOpenFailure -Context $Context -Resources $resources -Exception $exception
        }
        Release-RepositoryExecutionLeaseResources -Resources $resources
        throw
    }
}

function Write-RepositoryExecutionLeaseMetadata {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Context,
        [Parameter(Mandatory)][object]$Resources
    )

    $metadataBytes = [System.Text.Encoding]::UTF8.GetBytes($Context.Metadata)
    Ensure-RepositoryExecutionLeaseMetadataFile -Path $Context.MetadataPath -SharedLease $Context.SharedLease
    $writeMetadata = {
        $metadataStream = [System.IO.FileStream]::new($Context.MetadataPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
        try {
            $metadataStream.SetLength(0)
            $metadataStream.Write($metadataBytes, 0, $metadataBytes.Length)
            $metadataStream.Flush($true)
        }
        finally { $metadataStream.Dispose() }
    }
    if ($Context.SharedLease) {
        [System.Threading.Monitor]::Enter($Resources.StreamState.Gate)
        try {
            & $writeMetadata
        }
        finally {
            [System.Threading.Monitor]::Exit($Resources.StreamState.Gate)
        }
    }
    else { & $writeMetadata }
}

function Clear-RepositoryExecutionLeaseMetadata {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Lease)

    try {
        $metadataPath = [string]$Lease.MetadataPath
        if ($null -ne $Lease.SharedStreamState) {
            [System.Threading.Monitor]::Enter($Lease.SharedStreamState.Gate)
            try {
                $metadataStream = [System.IO.FileStream]::new($metadataPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
                try { $metadataStream.SetLength(0); $metadataStream.Flush($true) }
                finally { $metadataStream.Dispose() }
            }
            finally {
                [System.Threading.Monitor]::Exit($Lease.SharedStreamState.Gate)
            }
        }
        else {
            $metadataStream = [System.IO.FileStream]::new($metadataPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
            try { $metadataStream.SetLength(0); $metadataStream.Flush($true) }
            finally { $metadataStream.Dispose() }
        }
    }
    catch {
        throw "Unable to clear repository execution lease metadata from '$($Lease.Path)': $($_.Exception.Message)"
    }
}

function Enter-RepositoryExecutionLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [string]$OperationId = ([guid]::NewGuid().ToString('N')),
        [object]$ExistingLease,
        [string]$LeaseDirectory
    )

    if ($null -ne $ExistingLease) {
        return Get-ReentrantRepositoryExecutionLease -RepoRoot $RepoRoot -ExistingLease $ExistingLease -LeaseDirectory $LeaseDirectory
    }
    $context = New-RepositoryExecutionLeaseContext -RepoRoot $RepoRoot -OperationId $OperationId -LeaseDirectory $LeaseDirectory
    $resources = $null
    try {
        $resources = Open-RepositoryExecutionLeaseResources -Context $context
        Write-RepositoryExecutionLeaseMetadata -Context $context -Resources $resources
        return [pscustomobject]@{
            Path = $context.LeasePath
            MetadataPath = $context.MetadataPath
            LeaseOffset = $resources.LeaseOffset
            LeaseIdentity = $context.LeaseIdentity
            OperationId = $context.OperationId
            RepositoryRoot = $context.CanonicalRoot
            Stream = $resources.Stream
            SharedStreamState = $resources.StreamState
            ProcessSemaphore = $resources.ProcessSemaphore
            ProcessSemaphoreAcquired = $resources.ProcessSemaphoreAcquired
            OwnsStream = $true
        }
    }
    catch {
        if ($null -ne $resources) {
            if ($null -ne $resources.LeaseOffset) {
                try {
                    Clear-RepositoryExecutionLeaseMetadata -Lease ([pscustomobject]@{
                        Path = $context.LeasePath
                        MetadataPath = $context.MetadataPath
                        SharedStreamState = $resources.StreamState
                    })
                }
                catch {
                    Write-Verbose "Unable to clear failed lease metadata from '$($context.MetadataPath)': $($_.Exception.Message)"
                }
            }
            Release-RepositoryExecutionLeaseResources -Resources $resources
        }
        throw
    }
}

function Exit-RepositoryExecutionLease {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Lease)

    if ($Lease.OwnsStream -and $null -ne $Lease.Stream) {
        $metadataFailure = $null
        try { Clear-RepositoryExecutionLeaseMetadata -Lease $Lease }
        catch { $metadataFailure = $_.Exception }
        try {
            if ($null -ne $Lease.SharedStreamState) {
                try {
                    if ($null -ne $Lease.LeaseOffset) { Unlock-SharedRepositoryExecutionLeaseSlot -State $Lease.SharedStreamState -Offset ([long]$Lease.LeaseOffset) }
                }
                finally { Release-SharedRepositoryExecutionLeaseStream -State $Lease.SharedStreamState }
            }
            else {
                try {
                    if ($null -ne $Lease.LeaseOffset) { $Lease.Stream.Unlock([long]$Lease.LeaseOffset, 1) }
                }
                finally { $Lease.Stream.Dispose() }
            }
        }
        finally {
            Release-RepositoryExecutionLeaseProcessSemaphore -Lease $Lease
            Unregister-RepositoryExecutionLeaseIdentity -Identity ([string]$Lease.LeaseIdentity)
        }
        if ($null -ne $metadataFailure) { throw $metadataFailure }
    }
}

function ConvertTo-ConsoleColor {
    param(
        [object]$Value,
        [ConsoleColor]$Default = [ConsoleColor]::Cyan
    )

    if ($Value -is [ConsoleColor]) { return $Value }

    if ($Value -is [string]) {
        $name = $Value
        if ($name -match '::') {
            $name = $name.Split('::')[-1]
        }
        if ([Enum]::IsDefined([ConsoleColor], $name)) {
            return [Enum]::Parse([ConsoleColor], $name, $true)
        }
    }

    return $Default
}


function Write-AutomationBanner {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Message,
        [object]$ForegroundColor = [ConsoleColor]::Cyan,
        [switch]$InsertBlankLine
    )

    if ($InsertBlankLine) {
        Write-Host
    }

    $color = ConvertTo-ConsoleColor -Value $ForegroundColor -Default ([ConsoleColor]::Cyan)
    Write-Host $Message -ForegroundColor $color
}

function Invoke-AutomationStep {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$Name,
        [string]$Description,
        [int]$StepNumber,
        [object]$BannerColor = [ConsoleColor]::Yellow,
        [object]$SuccessColor = [ConsoleColor]::Green,
        [switch]$SilentSuccess
    )

    $bannerText = if ($PSBoundParameters.ContainsKey('StepNumber')) {
        "=== STEP ${StepNumber}: $Name ==="
    } else {
        "=== $Name ==="
    }

    $resolvedBannerColor = ConvertTo-ConsoleColor -Value $BannerColor -Default ([ConsoleColor]::Yellow)
    $resolvedSuccessColor = ConvertTo-ConsoleColor -Value $SuccessColor -Default ([ConsoleColor]::Green)
    Write-AutomationBanner -Message $bannerText -ForegroundColor $resolvedBannerColor -InsertBlankLine
    if ($Description) {
        Write-Host $Description
    }

    try {
        $result = & $Action
        if (-not $SilentSuccess) {
            Write-Host "SUCCESS: $Name" -ForegroundColor $resolvedSuccessColor
            Write-Host
        }
        return $result
    }
    catch {
        Write-Error "FAILURE: $Name : $($_.Exception.Message)" -ErrorAction Continue
        throw
    }
}

function Get-RepositoryProcessDescendantIds { # NOSONAR - bounded process-tree ownership snapshot intentionally traverses platform process metadata.
    [CmdletBinding()]
    param([Parameter(Mandatory)][int]$RootProcessId)

    $parents = @{}
    try {
        if ($IsWindows) {
            foreach ($processInfo in @(Get-CimInstance -ClassName Win32_Process -ErrorAction Stop)) {
                $parents[[int]$processInfo.ProcessId] = [int]$processInfo.ParentProcessId
            }
        }
        else {
            foreach ($line in @(& ps -eo 'pid=,ppid=' 2>$null)) {
                if ($line -match '^\s*(?<pid>\d+)\s+(?<parent>\d+)\s*$') {
                    $parents[[int]$Matches.pid] = [int]$Matches.parent
                }
            }
        }
    }
    catch {
        Write-Verbose "Unable to snapshot native process descendants: $($_.Exception.Message)"
        return @()
    }

    $descendants = [System.Collections.Generic.HashSet[int]]::new()
    $pending = [System.Collections.Generic.Queue[int]]::new()
    $pending.Enqueue($RootProcessId)
    while ($pending.Count -gt 0) {
        $parentId = $pending.Dequeue()
        foreach ($entry in $parents.GetEnumerator() | Where-Object { $_.Value -eq $parentId }) {
            $childId = [int]$entry.Key
            if ($descendants.Add($childId)) { $pending.Enqueue($childId) }
        }
    }
    return @($descendants | ForEach-Object {
        $process = Get-Process -Id ([int]$_) -ErrorAction SilentlyContinue
        [pscustomobject]@{ Id = [int]$_; StartTime = if ($null -ne $process) { $process.StartTime.ToUniversalTime() } else { $null } }
    })
}

function Stop-RepositoryProcessIds { # NOSONAR - termination helper deliberately handles platform-specific process cleanup branches.
    [CmdletBinding()]
    param([Parameter(Mandatory)][object[]]$ProcessRecords)

    $errors = [System.Collections.Generic.List[string]]::new()
    foreach ($record in @($ProcessRecords | Sort-Object Id -Descending -Unique)) {
        $processId = [int]$record.Id
        try {
            $current = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($null -eq $current -or ($null -ne $record.StartTime -and [Math]::Abs(($current.StartTime.ToUniversalTime() - $record.StartTime).TotalSeconds) -gt 1)) { continue }
            if ($IsWindows) { Stop-Process -Id $processId -Force -ErrorAction Stop }
            else { Stop-Process -Id $processId -Force -ErrorAction Stop }
        }
        catch { $errors.Add("${processId}: $($_.Exception.Message)") }
    }
    Start-Sleep -Milliseconds 100
    foreach ($record in @($ProcessRecords | Sort-Object Id -Descending -Unique)) {
        $processId = [int]$record.Id
        try {
            $current = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($null -eq $current -or ($null -ne $record.StartTime -and [Math]::Abs(($current.StartTime.ToUniversalTime() - $record.StartTime).TotalSeconds) -gt 1)) { continue }
            if ($IsWindows) {
                if (Get-Process -Id $processId -ErrorAction SilentlyContinue) { Stop-Process -Id $processId -Force -ErrorAction Stop }
            }
            else {
                Stop-Process -Id $processId -Force -ErrorAction Stop
            }
        }
        catch { $errors.Add("${processId}: $($_.Exception.Message)") }
    }
    return ,$errors
}

function Invoke-RepositoryProcess { # NOSONAR - native process lifecycle, bounded capture, and descendant cleanup intentionally remain coordinated here.
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments,
        [string]$ErrorMessage,
        [switch]$SuppressCommandEcho,
        [ValidateRange(0, 86400)][int]$TimeoutSeconds = 0,
        [string]$WorkingDirectory,
        [switch]$PassThru
    )

    $escapedArgs = if ($Arguments) {
        $Arguments | ForEach-Object {
            if ($_ -and $_.Contains(' ')) { '"{0}"' -f $_ } else { $_ }
        }
    } else {
        @()
    }

    if (-not $SuppressCommandEcho) {
        $commandLine = ($escapedArgs -join ' ').Trim()
        if ($commandLine) {
            Write-Host "Executing: $FilePath $commandLine" -ForegroundColor ([ConsoleColor]::DarkGray)
        } else {
            Write-Host "Executing: $FilePath" -ForegroundColor ([ConsoleColor]::DarkGray)
        }
    }

    if ($TimeoutSeconds -le 0 -and -not $PassThru -and [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        & $FilePath @Arguments
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            $message = if ($ErrorMessage) { $ErrorMessage } else { "Command '$FilePath' failed with exit code $exitCode." }
            throw $message
        }
        return
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processFilePath = $FilePath
    $processArguments = @($Arguments)
    if ($IsWindows -and [System.IO.Path]::GetExtension($FilePath) -iin @('.cmd', '.bat')) {
        $processFilePath = $env:ComSpec
        $processArguments = @('/d', '/c', $FilePath) + @($Arguments)
    }
    $startInfo.FileName = $processFilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    if ($WorkingDirectory) { $startInfo.WorkingDirectory = $WorkingDirectory }
    foreach ($argument in $processArguments) { $null = $startInfo.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $startedUtc = (Get-Date).ToUniversalTime().ToString('o')
    $trackedDescendants = @{}
    $stdoutBuffer = [Mississippi.BoundedProcessOutput]::new(200, 4096)
    $stderrBuffer = [Mississippi.BoundedProcessOutput]::new(200, 4096)
    $stdoutHandler = $stdoutBuffer.Handler
    $stderrHandler = $stderrBuffer.Handler
    try {
        if (-not $process.Start()) { throw "Command '$FilePath' could not be started." }
        $process.add_OutputDataReceived($stdoutHandler)
        $process.add_ErrorDataReceived($stderrHandler)
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()
        $deadline = if ($TimeoutSeconds -gt 0) { [DateTime]::UtcNow.AddSeconds($TimeoutSeconds) } else { $null }
        $finished = $false
        $nextDescendantScan = [DateTime]::UtcNow
        while (-not $process.HasExited) {
            if ([DateTime]::UtcNow -ge $nextDescendantScan) {
                foreach ($descendant in @(Get-RepositoryProcessDescendantIds -RootProcessId $process.Id)) { $trackedDescendants[[int]$descendant.Id] = $descendant }
                $nextDescendantScan = [DateTime]::UtcNow.AddMilliseconds(250)
            }
            if ($null -ne $deadline -and [DateTime]::UtcNow -ge $deadline) { break }
            Start-Sleep -Milliseconds 50
        }
        $finished = $process.HasExited
        $timedOut = $null -ne $deadline -and [DateTime]::UtcNow -ge $deadline
        $terminationErrors = [System.Collections.Generic.List[string]]::new()
        $terminationNotes = [System.Collections.Generic.List[string]]::new()
        $terminated = $true
        $captureDeadline = [DateTime]::UtcNow.AddMilliseconds(1000)
        while (-not ($stdoutBuffer.Closed -and $stderrBuffer.Closed) -and [DateTime]::UtcNow -lt $captureDeadline) {
            Start-Sleep -Milliseconds 25
        }
        $captureIncomplete = -not ($stdoutBuffer.Closed -and $stderrBuffer.Closed)
        if ($timedOut -or $captureIncomplete) {
            if ($timedOut) { $terminationNotes.Add("Command exceeded the $TimeoutSeconds second timeout.") }
            if ($captureIncomplete) { $terminationNotes.Add('Native output streams remained open after the root process exited.') }
            $terminationRecords = @($trackedDescendants.Values)
            if (-not $process.HasExited) { $terminationRecords += [pscustomobject]@{ Id = $process.Id; StartTime = $process.StartTime.ToUniversalTime() } }
            if ($terminationRecords.Count -gt 0) {
                foreach ($terminationError in @(Stop-RepositoryProcessIds -ProcessRecords $terminationRecords)) { $terminationErrors.Add($terminationError) }
            }
            if (-not $process.HasExited) {
                try { $process.Kill() } catch { $terminationErrors.Add($_.Exception.Message) }
            }
            $terminated = $process.WaitForExit(1000) -and $terminationErrors.Count -eq 0
        }
        $stdout = $stdoutBuffer.Text
        $stderr = $stderrBuffer.Text
        $result = [pscustomobject][ordered]@{
            FilePath = $FilePath
            Arguments = @($Arguments)
            StartedUtc = $startedUtc
            EndedUtc = (Get-Date).ToUniversalTime().ToString('o')
            ExitCode = if ($timedOut) { 124 } else { $process.ExitCode }
            TimedOut = $timedOut
            Cancelled = $false
            TerminationFailed = ($timedOut -or $captureIncomplete) -and (-not $terminated -or $terminationErrors.Count -gt 0)
            TerminationErrors = @($terminationErrors)
            TerminationNotes = @($terminationNotes)
            CaptureIncomplete = $captureIncomplete
            OutputTruncated = $stdoutBuffer.Truncated -or $stderrBuffer.Truncated
            StdOut = $stdout
            StdErr = $stderr
            Success = -not $timedOut -and -not $captureIncomplete -and $process.ExitCode -eq 0
        }
        if ($PassThru) { return $result }
        if (-not $result.Success) {
            $message = if ($timedOut) { "Command '$FilePath' timed out after $TimeoutSeconds seconds." } elseif ($result.CaptureIncomplete) { "Command '$FilePath' exited before native output capture completed." } elseif ($ErrorMessage) { $ErrorMessage } else { "Command '$FilePath' failed with exit code $($result.ExitCode)." }
            if ($stdout) { $message += " Native output: $stdout" }
            if ($stderr) { $message += " Native error output: $stderr" }
            if ($terminationNotes.Count -gt 0) { $message += " Process cleanup: $($terminationNotes -join '; ')" }
            if ($result.TerminationFailed) { $message += " Process termination was not verified: $($result.TerminationErrors -join '; ')" }
            throw $message
        }
        if ($stdout) { Write-Output $stdout }
        if ($stderr) { Write-Output $stderr }
    }
    catch {
        if ($PassThru) {
            return [pscustomobject][ordered]@{
                FilePath = $FilePath; Arguments = @($Arguments); StartedUtc = $startedUtc
                EndedUtc = (Get-Date).ToUniversalTime().ToString('o'); ExitCode = -1
                TimedOut = $false; Cancelled = $false; TerminationFailed = $false; CaptureIncomplete = $false
                TerminationErrors = @(); TerminationNotes = @(); OutputTruncated = $false; StdOut = ''; StdErr = $_.Exception.Message; Success = $false
            }
        }
        throw
    }
    finally {
        try { $process.CancelOutputRead() } catch { Write-Verbose 'Native stdout reader was already closed.' }
        try { $process.CancelErrorRead() } catch { Write-Verbose 'Native stderr reader was already closed.' }
        try { $process.remove_OutputDataReceived($stdoutHandler) } catch { Write-Verbose 'Native stdout handler was already detached.' }
        try { $process.remove_ErrorDataReceived($stderrHandler) } catch { Write-Verbose 'Native stderr handler was already detached.' }
        $process.Dispose()
    }
}

function Invoke-DotnetToolRestore {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [switch]$Force
    )

    $manifestPath = Join-Path $RepoRoot '.config/dotnet-tools.json'
    if (-not $Force -and -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        Write-Verbose "Dotnet tool manifest not found at '$manifestPath'; skipping restore."
        return
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','restore') -ErrorMessage 'Failed to restore dotnet tools.' | Out-Host
}

function Invoke-SolutionRestore {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Description = 'Restoring solution',
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    if (-not $Quiet) {
        Write-Host "Restoring ${Description}: $($resolved.Path)" -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('restore', $resolved.Path) -ErrorMessage "Failed to restore packages for $($resolved.Path)." | Out-Host
}

function Invoke-SolutionBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Configuration = 'Release',
        [switch]$WarnAsError,
        [switch]$NoRestore,
        [switch]$NoIncremental,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    $args = @('build', $resolved.Path, '--configuration', $Configuration)
    if ($NoRestore) { $args += '--no-restore' }
    if ($NoIncremental) { $args += '--no-incremental' }
    if ($WarnAsError) { $args += '--warnaserror' }
    if ($AdditionalArguments) { $args += $AdditionalArguments }

    if (-not $Quiet) {
        Write-Host "Building solution: $($resolved.Path)" -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to build $($resolved.Path)."
}

function New-AutomationRunDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [string]$Prefix
    )

    if (-not (Test-Path -LiteralPath $Root)) {
        $null = New-Item -ItemType Directory -Path $Root -Force
    }

    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $runId = [guid]::NewGuid().ToString('N')
    $folderName = if ($Prefix) { "$Prefix-$stamp-$runId" } else { "$stamp-$runId" }
    $runDirectory = Join-Path $Root $folderName
    if (-not (Test-Path -LiteralPath $runDirectory)) {
        $null = New-Item -ItemType Directory -Path $runDirectory -Force
    }

    return $runDirectory
}

function Get-PowerShellExecutable {
    [CmdletBinding()]
    param()

    return Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
}

function Get-TestExecutionCount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ResultsDirectory
    )

    $reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter '*.trx' -File -ErrorAction Stop)
    if ($reports.Count -eq 0) {
        throw "No TRX reports were produced in '$ResultsDirectory'. Check the test logger configuration."
    }
    $executed = 0
    foreach ($report in $reports) {
        [xml]$trx = Get-Content -LiteralPath $report.FullName -Raw
        $executed += [int]$trx.TestRun.ResultSummary.Counters.executed
    }
    return $executed
}

function Invoke-TestModule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ResultsDirectory,
        [string]$Configuration = 'Release',
        [switch]$CollectCoverage,
        [string[]]$TestLevels,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $moduleDirectory = Join-Path $ResultsDirectory $projectName
    $null = New-Item -ItemType Directory -Path $moduleDirectory -Force
    $arguments = @('test', '--project', $ProjectPath, '--configuration', $Configuration, '--no-restore',
        '--report-xunit-trx', '--report-xunit-trx-filename', "test_results_$projectName.trx",
        '--results-directory', $moduleDirectory)
    # These existing SDK facade projects have no test implementations; all other modules must execute tests.
    $emptyFacade = $projectName -in @('Sdk.Client.L0Tests', 'Sdk.Gateway.L0Tests', 'Sdk.Runtime.L0Tests')
    if ($emptyFacade) { $arguments += @('--ignore-exit-code', '8') }
    if ($CollectCoverage) { $arguments += @('--coverlet', '--coverlet-output-format', 'cobertura') }
    if ($TestLevels) {
        $filter = ($TestLevels | ForEach-Object { "FullyQualifiedName~.$($_)." }) -join '|'
        $arguments += @('--filter', $filter)
    }
    if ($AdditionalArguments) { $arguments += $AdditionalArguments }
    if (-not $Quiet) { Write-Host "Executing tests: $ProjectPath" -ForegroundColor Cyan }
    Invoke-RepositoryProcess -FilePath dotnet -Arguments $arguments -ErrorMessage "Failed to run tests for $ProjectPath." | Out-Host
    $executed = Get-TestExecutionCount -ResultsDirectory $moduleDirectory
    if ($executed -lt 1 -and -not $emptyFacade) { throw "No tests executed for '$ProjectPath'. Reports: $moduleDirectory" }
    if (@(Get-ChildItem -LiteralPath $moduleDirectory -Recurse -Filter '*.trx' -File).Count -ne 1) {
        throw "Expected one TRX report for '$ProjectPath'. Reports: $moduleDirectory"
    }
}

function Invoke-SolutionTests {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Configuration = 'Release',
        [string]$ResultsRoot,
        [switch]$CollectCoverage,
        [string[]]$TestLevels,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    $projects = @(Get-TestProjects -SolutionPath $resolved.Path)
    if ($TestLevels) {
        $levels = ($TestLevels | ForEach-Object { [regex]::Escape($_) }) -join '|'
        $projects = @($projects | Where-Object { $_ -match "\.($levels)\.csproj$" })
    }
    if ($projects.Count -eq 0) { throw "No tests executed: no projects match levels '$($TestLevels -join ',')'." }
    if (-not $ResultsRoot) { $ResultsRoot = Join-Path (Get-RepositoryRoot) '.scratchpad/coverage-test-results' }
    $resultsDirectory = New-AutomationRunDirectory -Root $ResultsRoot
    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($project in $projects) {
        try {
            Invoke-TestModule -ProjectPath $project -ResultsDirectory $resultsDirectory -Configuration $Configuration `
                -CollectCoverage:$CollectCoverage -TestLevels $TestLevels -AdditionalArguments $AdditionalArguments -Quiet:$Quiet
        }
        catch {
            $failures.Add($_.Exception.Message)
        }
    }
    if ($failures.Count -gt 0) { throw "Test modules failed. Reports: $resultsDirectory`n$($failures -join "`n")" }
    if ((Get-TestExecutionCount -ResultsDirectory $resultsDirectory) -lt 1) {
        throw "No tests executed for '$($resolved.Path)'. Reports: $resultsDirectory"
    }
    return [pscustomobject]@{ SolutionPath = $resolved.Path; ResultsDirectory = $resultsDirectory }
}
function Invoke-SlnGeneration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $source = Resolve-Path -LiteralPath $SolutionPath
    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    $outputDirectory = Split-Path -Parent $outputFullPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        $null = New-Item -ItemType Directory -Path $outputDirectory -Force
    }

    $args = @('tool','run','slngen', $source.Path, '--solutionfile', $outputFullPath, '--launch','false')
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to generate solution '$outputFullPath' using SlnGen." -SuppressCommandEcho | Out-Host
    return $outputFullPath
}

function Invoke-ReSharperCleanup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string[]]$IncludePaths,
        [string[]]$ExcludePaths
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $resolvedSettings = Resolve-Path -LiteralPath $SettingsPath
    $cacheRoot = Join-Path (Split-Path -Parent $resolvedSolution.Path) '.scratchpad/cleanup-caches'
    $cacheDirectory = New-AutomationRunDirectory -Root $cacheRoot
    Write-Host "Cleanup cache: $cacheDirectory" -ForegroundColor DarkGray
    $sdkVersion = @(Invoke-RepositoryProcess -FilePath dotnet -Arguments @('--version') -SuppressCommandEcho)
    if ($sdkVersion.Count -ne 1 -or $sdkVersion[0] -notmatch '^\d+\.\d+\.\d+(?:-[\w.-]+)?$') {
        throw 'Expected one SDK version selected by global.json.'
    }
    $cleanupArguments = @('tool','run','jb','cleanupcode', "--profile=$Profile", "--settings=$($resolvedSettings.Path)",
        "--caches-home=$cacheDirectory", "--dotnetcoresdk=$($sdkVersion[0])")

    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        $cleanupArguments += "--include=$($IncludePaths -join ';')"
    }

    if ($ExcludePaths -and $ExcludePaths.Count -gt 0) {
        $cleanupArguments += "--exclude=$($ExcludePaths -join ';')"
    }

    $cleanupArguments += $resolvedSolution.Path
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $cleanupArguments -ErrorMessage "ReSharper cleanup failed for $($resolvedSolution.Path)." -SuppressCommandEcho | Out-Host
}

function Get-SolutionProjectPaths {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath
    )

    # Generated legacy solutions share their membership with the canonical .slnx.
    $canonicalPath = [System.IO.Path]::ChangeExtension($SolutionPath, '.slnx')
    [xml]$solution = Get-Content -LiteralPath $canonicalPath -Raw
    $solutionDir = Split-Path -Parent (Resolve-Path -LiteralPath $canonicalPath).Path
    foreach ($project in $solution.SelectNodes('//Project')) {
        $relativePath = $project.GetAttribute('Path') -replace '\\', '/'
        (Resolve-Path -LiteralPath (Join-Path $solutionDir $relativePath)).Path
    }
}

function Get-TestProjects {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$SolutionPath)

    Get-SolutionProjectPaths -SolutionPath $SolutionPath | Where-Object { $_ -like '*Tests.csproj' }
}

function Read-MutationReport {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ReportPath)

    $report = Get-Content -LiteralPath $ReportPath -Raw -ErrorAction Stop | ConvertFrom-Json -AsHashtable
    if ($report -isnot [System.Collections.IDictionary] -or $report['files'] -isnot [System.Collections.IDictionary]) {
        throw "Stryker report has no valid files collection: $ReportPath"
    }
    $completedStatuses = @('Killed', 'Survived', 'NoCoverage', 'CompileError', 'RuntimeError', 'Timeout', 'Ignored')
    foreach ($fileResult in $report['files'].Values) {
        if ($fileResult -isnot [System.Collections.IDictionary] -or $fileResult['mutants'] -isnot [array]) {
            throw "Stryker report has no valid mutants collection: $ReportPath"
        }
        foreach ($mutant in $fileResult['mutants']) {
            if ($mutant -isnot [System.Collections.IDictionary] -or $mutant['status'] -isnot [string] -or $mutant['status'] -cnotin $completedStatuses) {
                throw "Stryker report contains an incomplete or invalid mutant result: $ReportPath"
            }
        }
    }
    return $report
}

function Get-MutationReportPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$OutputPath)

    $reports = @(Get-ChildItem -LiteralPath $OutputPath -Recurse -Filter 'mutation-report.json' -File -ErrorAction Stop)
    if ($reports.Count -ne 1) {
        throw "Expected one mutation-report.json; found $($reports.Count). Reports: $OutputPath"
    }
    Read-MutationReport -ReportPath $reports[0].FullName | Out-Null
    return $reports[0].FullName
}

function Get-StrykerBreakThreshold {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    $config = Get-Content -LiteralPath $ConfigPath -Raw -ErrorAction Stop | ConvertFrom-Json -AsHashtable
    if ($config -isnot [System.Collections.IDictionary]) {
        throw "Stryker configuration is not a JSON object: $ConfigPath"
    }

    $strykerConfig = $config['stryker-config']
    $thresholds = if ($strykerConfig -is [System.Collections.IDictionary]) { $strykerConfig['thresholds'] } else { $null }
    $breakValue = if ($thresholds -is [System.Collections.IDictionary]) { $thresholds['break'] } else { $null }
    if ($null -eq $breakValue) {
        return [double]0
    }

    try {
        return [double]$breakValue
    }
    catch {
        throw "Stryker break threshold is not numeric in configuration: $ConfigPath"
    }
}

function Get-MutationReportMetrics {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ReportPath)

    $report = Read-MutationReport -ReportPath $ReportPath
    $mutants = @(
        foreach ($fileResult in $report['files'].Values) {
            @($fileResult['mutants'])
        }
    )
    $statuses = @('Killed', 'Timeout', 'Survived', 'NoCoverage', 'RuntimeError', 'CompileError', 'Ignored', 'Pending')
    $counts = @{}
    foreach ($status in $statuses) {
        $counts[$status] = @($mutants | Where-Object { $_['status'] -eq $status }).Count
    }

    $detected = $counts['Killed'] + $counts['Timeout']
    $valid = $detected + $counts['Survived'] + $counts['NoCoverage']
    $rawScore = if ($valid -gt 0) { $detected / $valid * 100 } else { $null }
    $score = if ($null -ne $rawScore) { [Math]::Round($rawScore, 2) } else { $null }
    return [pscustomobject]@{
        Score = $score
        RawScore = $rawScore
        Valid = $valid
        Detected = $detected
        Killed = $counts['Killed']
        Timeout = $counts['Timeout']
        Survived = $counts['Survived']
        NoCoverage = $counts['NoCoverage']
        RuntimeError = $counts['RuntimeError']
        CompileError = $counts['CompileError']
        Ignored = $counts['Ignored']
        Pending = $counts['Pending']
    }
}

function Get-MutationProjectStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ExecutionStatus,
        [Parameter(Mandatory)][bool]$ReportValid,
        [Nullable[double]]$Score,
        [double]$BreakThreshold
    )

    if ($ExecutionStatus -eq 'Skipped') { return 'SKIPPED' }
    if (-not $ReportValid) { return 'NO_REPORT' }
    if ($null -eq $Score) { return 'NO_SCORE' }
    if ($BreakThreshold -gt 0 -and $Score -lt $BreakThreshold) { return 'BELOW_BREAK' }
    return 'AT_OR_ABOVE_BREAK'
}

function Get-MutationProjectSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [double]$BreakThreshold
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension([string]$ProjectResult.Project)
    $reportValid = $false
    $reportError = $ProjectResult.ReportError
    $metrics = $null
    if ($ProjectResult.ReportPath -and (Test-Path -LiteralPath $ProjectResult.ReportPath -PathType Leaf)) {
        try {
            $metrics = Get-MutationReportMetrics -ReportPath $ProjectResult.ReportPath
            $reportValid = $true
        }
        catch {
            $reportError = $_.Exception.Message
        }
    }

    $score = if ($metrics) { $metrics.Score } else { $null }
    $rawScore = if ($metrics) { $metrics.RawScore } else { $null }
    return [pscustomobject]@{
        Project = $projectName
        ExecutionStatus = switch ($ProjectResult.Status) {
            'Failed' { 'FAILED' }
            'ThresholdFailed' { 'COMPLETED_WITH_THRESHOLD_FAILURE' }
            default { 'COMPLETED' }
        }
        Status = Get-MutationProjectStatus -ExecutionStatus $ProjectResult.Status -ReportValid $reportValid -Score $rawScore -BreakThreshold $BreakThreshold
        Score = $score
        RawScore = $rawScore
        ValidMutants = if ($metrics) { $metrics.Valid } else { 0 }
        DetectedMutants = if ($metrics) { $metrics.Detected } else { 0 }
        ReportPath = $ProjectResult.ReportPath
        ReportValid = $reportValid
        Error = $ProjectResult.Error
        ReportError = $reportError
    }
}

function Get-MutationProjectMessage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Project,
        [Parameter(Mandatory)][string]$Fallback
    )

    if ($Project.Error) { return $Project.Error }
    if ($Project.ReportError) { return $Project.ReportError }
    return $Fallback
}

function Get-GitHubMutationSummaryDetailLines {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Summary)

    $detailLines = @()
    if ($Summary.BelowBreakThresholdCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects below the advisory threshold'
        foreach ($project in $Summary.BelowBreakThresholdProjects) {
            $detailLines += "- $($project.Project): $($project.Score)%"
        }
    }
    if ($Summary.NoScoreProjectCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects without a mutation score'
        foreach ($project in $Summary.NoScoreProjects) {
            $detailLines += "- $($project.Project): $(Get-MutationProjectMessage -Project $project -Fallback 'No valid mutants were scored')"
        }
    }
    if ($Summary.FailedProjectCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects with execution failures'
        foreach ($project in $Summary.FailedProjects) {
            $detailLines += "- $($project.Project): $(Get-MutationProjectMessage -Project $project -Fallback $project.Status)"
        }
    }
    return $detailLines
}

function Write-GitHubMutationSummary {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Summary)

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) { return }

    $heading = switch ($Summary.MutationResult) {
        'FAIL' { 'failed'; break }
        'WARN' { 'completed with warnings'; break }
        default { 'completed' }
    }
    $summaryLines = @(
        "## Mutation testing: $heading"
        ''
        "- Execution: **$($Summary.ExecutionStatus)**"
        "- Result: **$($Summary.MutationResult)**"
        "- Complete reports: **$($Summary.CompleteReportCount)/$($Summary.ProjectCount)**"
        "- Skipped projects: **$($Summary.SkippedProjectCount)**"
        "- Threshold-only exits: **$($Summary.ThresholdFailureCount)**"
        "- Scored projects: **$($Summary.ScoredProjectCount)**"
        "- Unscored projects: **$($Summary.NoScoreProjectCount)**"
        "- Below break threshold ($($Summary.BreakThreshold)%): **$($Summary.BelowBreakThresholdCount)**"
    )
    $summaryLines += Get-GitHubMutationSummaryDetailLines -Summary $Summary
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summaryLines -Encoding utf8
}

function Show-MutationRunSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][object[]]$ProjectResults,
        [double]$BreakThreshold = 0
    )

    $projectSummaries = @($ProjectResults | ForEach-Object {
        Get-MutationProjectSummary -ProjectResult $_ -BreakThreshold $BreakThreshold
    })

    $failedProjects = @($projectSummaries | Where-Object { $_.ExecutionStatus -eq 'FAILED' })
    $thresholdFailures = @($projectSummaries | Where-Object { $_.ExecutionStatus -eq 'COMPLETED_WITH_THRESHOLD_FAILURE' })
    $belowBreak = @($projectSummaries | Where-Object { $_.Status -eq 'BELOW_BREAK' })
    $noScoreProjects = @($projectSummaries | Where-Object { $_.Status -eq 'NO_SCORE' })
    $skippedProjects = @($projectSummaries | Where-Object { $_.Status -eq 'SKIPPED' })
    $reportEligibleProjects = @($projectSummaries | Where-Object { $_.Status -ne 'SKIPPED' })
    $completeReports = @($projectSummaries | Where-Object { $_.ReportValid })
    $scoredReports = @($projectSummaries | Where-Object { $null -ne $_.Score })
    $hasWarnings = $thresholdFailures.Count -gt 0 -or $belowBreak.Count -gt 0 -or $noScoreProjects.Count -gt 0
    $executionStatus = if ($failedProjects.Count -gt 0) { 'FAILED' } elseif ($thresholdFailures.Count -gt 0 -or $noScoreProjects.Count -gt 0) { 'COMPLETED_WITH_WARNINGS' } else { 'COMPLETED' }
    $mutationResult = if ($executionStatus -eq 'FAILED') { 'FAIL' } elseif ($hasWarnings) { 'WARN' } else { 'PASS' }

    $summary = [ordered]@{
        SchemaVersion = 1
        ExecutionStatus = $executionStatus
        MutationResult = $mutationResult
        BreakThreshold = $BreakThreshold
        ProjectCount = $reportEligibleProjects.Count
        TargetProjectCount = $ProjectResults.Count
        SkippedProjectCount = $skippedProjects.Count
        CompleteReportCount = $completeReports.Count
        ScoredProjectCount = $scoredReports.Count
        BelowBreakThresholdCount = $belowBreak.Count
        FailedProjectCount = $failedProjects.Count
        ThresholdFailureCount = $thresholdFailures.Count
        NoScoreProjectCount = $noScoreProjects.Count
        BelowBreakThresholdProjects = @($belowBreak | Select-Object Project, Score)
        FailedProjects = @($failedProjects | Select-Object Project, Status, ReportValid, Error, ReportError)
        ThresholdFailureProjects = @($thresholdFailures | Select-Object Project, Score, RawScore)
        NoScoreProjects = @($noScoreProjects | Select-Object Project, Error, ReportError)
        Projects = $projectSummaries
    }
    $summaryPath = Join-Path $OutputPath 'mutation-summary.json'
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath

    Write-Host "MUTATION_EXECUTION: $executionStatus"
    Write-Host "MUTATION_RESULT: $mutationResult"
    Write-Host "MUTATION_REPORTS: $($completeReports.Count)/$($reportEligibleProjects.Count) complete (skipped: $($skippedProjects.Count))"
    Write-Host "MUTATION_SCORED_PROJECTS: $($scoredReports.Count)"
    Write-Host "MUTATION_UNSCORED_PROJECTS: $($noScoreProjects.Count)"
    Write-Host "MUTATION_BELOW_BREAK: $($belowBreak.Count) (threshold $BreakThreshold%)"
    Write-Host "MUTATION_THRESHOLD_FAILURES: $($thresholdFailures.Count)"
    if ($belowBreak.Count -gt 0) {
        $warningMessage = "Mutation analysis completed, but $($belowBreak.Count) project(s) scored below the configured break threshold."
        Write-Warning "$warningMessage Threshold: $BreakThreshold%."
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::warning title=Mutation score warning::$warningMessage"
        }
    }
    if ($noScoreProjects.Count -gt 0) {
        $warningMessage = "Mutation analysis completed, but $($noScoreProjects.Count) project(s) produced no mutation score."
        Write-Warning $warningMessage
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::warning title=Mutation score unavailable::$warningMessage"
        }
    }
    if ($failedProjects.Count -gt 0) {
        Write-Warning "Mutation analysis did not complete successfully for $($failedProjects.Count) project(s)."
    }

    Write-GitHubMutationSummary -Summary ([pscustomobject]$summary)

    return [pscustomobject]$summary
}

function Invoke-StrykerMutationTestPerProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string[]]$TestProjects,
        [string]$Configuration = 'Release',
        [switch]$ReportOnly,
        [switch]$ApplyThresholdAfterReport
    )

    $resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedProject)
    $projectOutputPath = New-AutomationRunDirectory -Root ([System.IO.Path]::GetFullPath((Join-Path $OutputPath $projectName)))
    $repoRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedProject)
    $configPath = Join-Path $repoRoot 'stryker-config.json'
    
    Write-Host "  Running Stryker for project: $projectName" -ForegroundColor ([ConsoleColor]::Cyan)
    # MTP executes xUnit v3 test applications; complete reports remain mandatory after any tool exit status.
    $arguments = @('stryker', '--project', [System.IO.Path]::GetFileName($resolvedProject), '--config-file', $configPath, '--configuration', $Configuration, '--output', $projectOutputPath, '--test-runner', 'mtp', '--disable-bail', '--break-on-initial-test-failure')
    foreach ($testProject in $TestProjects) {
        $arguments += @('--test-project', $testProject)
    }
    if ($ReportOnly -or $ApplyThresholdAfterReport) {
        # Keep full-solution/report-only exits focused on execution/report generation; apply thresholds after validating the report.
        $arguments += @('--break-at', '0')
    }
    # MTP reuses test servers; serialize mutants to isolate process-global state and integration fixtures.
    $arguments += @('--concurrency', '1')
    # Stryker's multiple-test-project mode runs from the source project directory.
    Push-Location -LiteralPath (Split-Path -Parent $resolvedProject)
    try {
        # Use the SDK selected by global.json instead of an older Visual Studio MSBuild.
        $msBuildDirectories = @(Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('msbuild', $resolvedProject, '-getProperty:MSBuildBinPath', '-nologo') -SuppressCommandEcho)
        if ($msBuildDirectories.Count -ne 1 -or [string]::IsNullOrWhiteSpace($msBuildDirectories[0])) {
            throw 'Expected one MSBuildBinPath value from the selected SDK.'
        }
        $msBuildPath = Join-Path $msBuildDirectories[0] 'MSBuild.dll'
        if (-not (Test-Path -LiteralPath $msBuildPath -PathType Leaf)) { throw "Selected SDK MSBuild.dll was not found: $msBuildPath" }
        $arguments += @('--msbuild-path', $msBuildPath)
        $processError = $null
        try {
            Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $arguments -ErrorMessage "Stryker mutation testing failed for project $projectName. Reports: $projectOutputPath" -SuppressCommandEcho | Out-Host
        }
        catch {
            $processError = $_
        }
        try {
            $reportPath = Get-MutationReportPath -OutputPath $projectOutputPath
        }
        catch {
            if ($processError) {
                $processError.Exception.Data['ReportError'] = $_.Exception.Message
                throw $processError
            }
            throw
        }
        if ($processError) {
            $processError.Exception.Data['ReportPath'] = $reportPath
            throw $processError
        }
    }
    catch {
        $_.Exception.Data['OutputPath'] = $projectOutputPath
        throw
    }
    finally {
        Pop-Location
    }
    
    return $projectOutputPath
}

function Get-MutationTargets {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$SolutionPath)

    $projects = @(Get-SolutionProjectPaths -SolutionPath $SolutionPath)
    $sourceRoot = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $SolutionPath) 'src')) + [System.IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $targets = @{}
    foreach ($project in $projects) {
        if ($project.StartsWith($sourceRoot, $comparison)) {
            $targets[$project] = [pscustomobject]@{ Project = $project; Tests = @(); HasSource = $false }
        }
    }
    foreach ($testProject in @($projects | Where-Object { $_ -like '*Tests.csproj' })) {
        [xml]$definition = Get-Content -LiteralPath $testProject -Raw
        foreach ($reference in $definition.SelectNodes('//ProjectReference')) {
            $relativePath = $reference.GetAttribute('Include').Replace('\', '/')
            $path = (Resolve-Path -LiteralPath (Join-Path (Split-Path -Parent $testProject) $relativePath)).Path
            if ($targets.ContainsKey($path)) { $targets[$path].Tests += $testProject }
        }
    }
    foreach ($target in $targets.Values) {
        $source = @(Get-ChildItem -LiteralPath (Split-Path -Parent $target.Project) -Recurse -File -Filter '*.cs' -ErrorAction Stop |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Select-Object -First 1)
        $target.HasSource = $source.Count -gt 0
        $target.Tests = @($target.Tests | Sort-Object -Unique)
    }
    return @($targets.Values | Sort-Object Project)
}

function Set-MutationResultMetrics {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][string]$ReportPath
    )

    $metrics = Get-MutationReportMetrics -ReportPath $ReportPath
    $ProjectResult.MutationScore = $metrics.Score
    $ProjectResult.RawMutationScore = $metrics.RawScore
    $ProjectResult.ValidMutants = $metrics.Valid
    $ProjectResult.DetectedMutants = $metrics.Detected
}

function Set-MutationFailureResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][object]$Failure
    )

    $ProjectResult.Output = $Failure.Exception.Data['OutputPath']
    $ProjectResult.ReportPath = $Failure.Exception.Data['ReportPath']
    $ProjectResult.Status = 'Failed'
    $ProjectResult.Error = $Failure.Exception.Message
    $ProjectResult.ReportError = $Failure.Exception.Data['ReportError']
    if ($ProjectResult.ReportPath -and (Test-Path -LiteralPath $ProjectResult.ReportPath -PathType Leaf)) {
        try {
            Set-MutationResultMetrics -ProjectResult $ProjectResult -ReportPath $ProjectResult.ReportPath
        }
        catch {
            if (-not $ProjectResult.ReportError) { $ProjectResult.ReportError = $_.Exception.Message }
        }
    }
}

function Invoke-MutationTarget {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Target,
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string]$Configuration,
        [double]$BreakThreshold,
        [switch]$ReportOnly
    )

    if (-not $Target.HasSource) {
        $ProjectResult.Status = 'Skipped'
        $ProjectResult.Success = $true
        $ProjectResult.Reason = 'No authored C# source'
        return
    }

    try {
        if ($Target.Tests.Count -eq 0) { throw "Authored source project has no declared test mapping: $($Target.Project)" }
        $projectOutput = Invoke-StrykerMutationTestPerProject -ProjectPath $Target.Project -TestProjects $Target.Tests -OutputPath $OutputPath -Configuration $Configuration -ReportOnly:$ReportOnly -ApplyThresholdAfterReport
        $reportPath = Get-MutationReportPath -OutputPath $projectOutput
        Set-MutationResultMetrics -ProjectResult $ProjectResult -ReportPath $reportPath
        $ProjectResult.Output = $projectOutput
        $ProjectResult.ReportPath = $reportPath
        if (-not $ReportOnly -and $BreakThreshold -gt 0 -and $null -ne $ProjectResult.RawMutationScore -and
            $ProjectResult.RawMutationScore -lt $BreakThreshold) {
            $ProjectResult.Status = 'ThresholdFailed'
            $ProjectResult.ThresholdFailure = $true
            $ProjectResult.Success = $false
            $ProjectResult.Error = "Mutation score $($ProjectResult.RawMutationScore)% is below the configured break threshold $BreakThreshold%."
            Write-Warning "  ! Threshold not met: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project)) - score $($ProjectResult.RawMutationScore)% (threshold $BreakThreshold%)"
            return
        }
        $ProjectResult.Success = $true
        $ProjectResult.Status = 'Completed'
        Write-Host "  ✓ Completed: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project))" -ForegroundColor ([ConsoleColor]::Green)
    }
    catch {
        Write-Warning "  ✗ Failed: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project)) - $($_.Exception.Message)"
        Set-MutationFailureResult -ProjectResult $ProjectResult -Failure $_
    }
}

function Invoke-StrykerMutationTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$OutputPath,
        [string]$Configuration = 'Release',
        [switch]$ReportOnly
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    if (-not (Test-Path -LiteralPath $outputFullPath)) {
        $null = New-Item -ItemType Directory -Path $outputFullPath -Force
    }

    $repoRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedSolution.Path)
    $configPath = Join-Path $repoRoot 'stryker-config.json'
    $breakThreshold = Get-StrykerBreakThreshold -ConfigPath $configPath
    $targets = @(Get-MutationTargets -SolutionPath $resolvedSolution.Path)
    if (@($targets | Where-Object HasSource).Count -eq 0) {
        throw "No source projects with tests were found in '$SolutionPath'."
    }

    $projectResults = @($targets | ForEach-Object {
        @{ Project = $_.Project; Output = $null; ReportPath = $null; Success = $false;
           Status = 'Pending'; Error = $null; ReportError = $null; Reason = $null;
           MutationScore = $null; RawMutationScore = $null; ValidMutants = 0; DetectedMutants = 0;
           ThresholdFailure = $false }
    })
    $manifestPath = Join-Path $outputFullPath 'project-results.json'
    $manifest = @{ Scope = 'Solution'; Solution = $resolvedSolution.Path; Projects = $projectResults }
    ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath
    for ($index = 0; $index -lt $targets.Count; $index++) {
        $target = $targets[$index]
        $result = $projectResults[$index]
        Invoke-MutationTarget -Target $target -ProjectResult $result -OutputPath $outputFullPath -Configuration $Configuration -BreakThreshold $breakThreshold -ReportOnly:$ReportOnly
        ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath
        Write-Host
    }
    ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath

    $null = Show-MutationRunSummary -OutputPath $outputFullPath -ProjectResults $projectResults -BreakThreshold $breakThreshold

    # Check if any projects failed
    $failedProjects = @($projectResults | Where-Object { -not $_.Success })
    if ($failedProjects.Count -gt 0) {
        $thresholdFailures = @($failedProjects | Where-Object { $_.Status -eq 'ThresholdFailed' })
        $executionFailures = @($failedProjects | Where-Object { $_.Status -ne 'ThresholdFailed' })
        if ($thresholdFailures.Count -gt 0) {
            Write-Warning "$($thresholdFailures.Count) project(s) completed but did not meet the configured mutation score threshold."
            foreach ($failed in $thresholdFailures) {
                Write-Host "  - $([System.IO.Path]::GetFileNameWithoutExtension($failed.Project)): $($failed.MutationScore)%" -ForegroundColor ([ConsoleColor]::Yellow)
            }
        }
        if ($executionFailures.Count -gt 0) {
            Write-Warning "$($executionFailures.Count) project(s) failed mutation execution."
            foreach ($failed in $executionFailures) {
                Write-Host "  - $([System.IO.Path]::GetFileNameWithoutExtension($failed.Project)): $($failed.Error)" -ForegroundColor ([ConsoleColor]::Yellow)
            }
        }
        throw "Stryker mutation testing failed for $($failedProjects.Count) project(s). Reports: $outputFullPath"
    }

    return $outputFullPath
}

function Invoke-MississippiSolutionBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $solutionPath = Join-Path $RepoRoot 'mississippi.slnx'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION BUILD PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Building mississippi.slnx in $Configuration mode"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Compiling mississippi.slnx (Configuration: $Configuration)..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: Mississippi solution compiled successfully' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION BUILD COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
}

function Invoke-SampleSolutionBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $solutionPath = Join-Path $RepoRoot 'samples.slnx'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION BUILD PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Building samples.slnx in $Configuration mode"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Compiling samples.slnx (Configuration: $Configuration)..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: Sample solution compiled successfully' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION BUILD COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
}

function Invoke-FinalSolutionsBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $mississippi = Join-Path $RepoRoot 'mississippi.slnx'
    $samples = Join-Path $RepoRoot 'samples.slnx'

    Write-AutomationBanner -Message '=== FINAL BUILD WITH WARNINGS AS ERRORS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Zero-tolerance build: All warnings will be treated as compilation errors'
    Write-Host 'This is the final quality gate before code can be merged'
    Write-Host

    Write-Host '[1/5] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/5] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $mississippi -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: Mississippi packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/5] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $samples -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: Sample packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[4/5] Building mississippi.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $mississippi -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: Mississippi solution built with zero warnings' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[5/5] Building samples.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $samples -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: Sample solution built with zero warnings' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== FINAL BUILD COMPLETED SUCCESSFULLY ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'ZERO WARNINGS DETECTED | Both solutions meet production quality standards'
    Write-Host 'Code is ready for merge to main branch'
}

function Invoke-MississippiSolutionUnitTests {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$TestLevels = @('L0Tests', 'L1Tests'),
        [switch]$PassThru
    )

    $solutionPath = Join-Path $RepoRoot 'mississippi.slnx'
    $resultsRoot = Join-Path $RepoRoot '.scratchpad/coverage-test-results'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION UNIT TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running comprehensive unit and integration tests'
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Executing unit tests for mississippi.slnx..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host "Test levels: $($TestLevels -join ', ')"
    Write-Host 'Test flags: --no-restore --coverlet --report-xunit-trx'
    $testResult = (Invoke-SolutionTests -SolutionPath $solutionPath -Configuration $Configuration -ResultsRoot $resultsRoot -CollectCoverage -TestLevels $TestLevels -Quiet | Select-Object -Last 1)
    $runDirectory = $testResult.ResultsDirectory
    Write-Host "Results directory: $runDirectory"
    Write-Host 'Logger: xUnit TRX reports, one per test module'

    $coverageFiles = @(Get-ChildItem -Path $runDirectory -Recurse -Filter '*cobertura*.xml' -ErrorAction SilentlyContinue)
    if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
        throw "Unit tests completed but no coverage reports were produced in '$runDirectory'."
    }

    $reportsArg = ($coverageFiles | ForEach-Object { '"{0}"' -f $_.FullName }) -join ';'
    $mergedOutputDir = Join-Path $runDirectory 'coverage-merged'
    if (Test-Path -LiteralPath $mergedOutputDir) {
        Remove-Item -LiteralPath $mergedOutputDir -Recurse -Force
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','run','reportgenerator', "-reports:$reportsArg", "-targetdir:$mergedOutputDir", '-reporttypes:Cobertura') -ErrorMessage 'ReportGenerator failed while merging coverage reports.' -SuppressCommandEcho | Out-Host

    $mergedCobertura = Join-Path $mergedOutputDir 'Cobertura.xml'
    if (-not (Test-Path -LiteralPath $mergedCobertura)) {
        throw 'ReportGenerator did not create an aggregated Cobertura.xml report.'
    }

    $finalCoveragePath = Join-Path $runDirectory 'coverage.cobertura.xml'
    Copy-Item -LiteralPath $mergedCobertura -Destination $finalCoveragePath -Force

    Write-Host 'SUCCESS: All unit tests passed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Aggregated coverage report: $finalCoveragePath" -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION UNIT TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    $resultsFile = Join-Path $runDirectory '*/test_results*.trx'
    Write-Host "All tests passed | Results saved to: $resultsFile"
    Write-Host 'Coverage report ready for summarize-coverage-gaps.ps1' -ForegroundColor ([ConsoleColor]::Green)
    if ($PassThru) {
        return [pscustomobject][ordered]@{
            ResultsDirectory = $runDirectory
            CoverageReportPath = $finalCoveragePath
            TestLevels = @($TestLevels)
            Configuration = $Configuration
        }
    }
}

function Invoke-SampleSolutionUnitTests {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$TestLevels = @('L0Tests', 'L1Tests')
    )

    $solutionPath = Join-Path $RepoRoot 'samples.slnx'
    $resultsRoot = Join-Path $RepoRoot '.scratchpad/coverage-test-results'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION UNIT TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running sample application tests (minimal coverage for demonstration)'
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Executing unit tests for samples.slnx..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host "Test levels: $($TestLevels -join ', ')"
    Write-Host 'Test flags: --no-restore'
    $testResult = (Invoke-SolutionTests -SolutionPath $solutionPath -Configuration $Configuration -ResultsRoot $resultsRoot -TestLevels $TestLevels -Quiet | Select-Object -Last 1)
    Write-Host "Results directory: $($testResult.ResultsDirectory)"
    Write-Host 'Logger: TRX format (unique test_results prefix per project/framework)'
    Write-Host 'NOTE: Sample tests are for demonstration purposes only'

    Write-Host 'SUCCESS: All sample tests passed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION UNIT TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    $resultsFile = Join-Path $testResult.ResultsDirectory 'test_results*.trx'
    Write-Host "Sample tests passed | Results saved to: $resultsFile"
}

function Invoke-MississippiSolutionCleanup {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$IncludePaths
    )

    $slnxPath = Join-Path $RepoRoot 'mississippi.slnx'
    $slnPath = Join-Path $RepoRoot 'mississippi.sln'
    $settingsPath = Join-Path $RepoRoot 'Directory.DotSettings'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with 'Built-in: Full Cleanup' profile"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/3] Generating mississippi.sln from mississippi.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: 'Built-in: Full Cleanup'"
    Write-Host "Settings file: $settingsPath"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPath -IncludePaths $IncludePaths
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-SampleSolutionCleanup {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$IncludePaths
    )

    $slnxPath = Join-Path $RepoRoot 'samples.slnx'
    $slnPath = Join-Path $RepoRoot 'samples.sln'
    $settingsPath = Join-Path $RepoRoot 'Directory.DotSettings'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with 'Built-in: Full Cleanup' profile"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/3] Generating samples.sln from samples.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: 'Built-in: Full Cleanup'"
    Write-Host "Settings file: $settingsPath"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPath -IncludePaths $IncludePaths
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-MississippiSolutionMutationTests {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string]$Configuration = 'Release',
        [switch]$ReportOnly
    )

    $slnxPath = Join-Path $RepoRoot 'mississippi.slnx'
    $slnPath = Join-Path $RepoRoot 'mississippi.sln'
    $mutationRoot = Join-Path $RepoRoot '.scratchpad/mutation-test-results'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION MUTATION TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running Stryker.NET mutation testing to validate test quality'
    Write-Host 'Mutation testing ensures tests can detect code changes'
    Write-Host

    Write-Host '[1/4] Restoring dotnet tools (including Stryker.NET)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, Stryker.NET available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/4] Generating mississippi.sln from mississippi.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for Stryker compatibility'
    $generatedSln = (Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Select-Object -Last 1)
    Write-Host 'SUCCESS: Solution file generated for Stryker processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/4] Restoring NuGet packages for generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $generatedSln -Description 'mississippi.sln' -Quiet
    Invoke-SolutionBuild -SolutionPath $generatedSln -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: NuGet packages restored for mutation testing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[4/4] Executing Stryker.NET mutation testing...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Stryker will inject mutations into source code and verify tests detect them'
    Write-Host 'This process validates the effectiveness of the test suite'
    Write-Host "Target solution: $generatedSln"

    $outputDirectory = New-AutomationRunDirectory -Root $mutationRoot
    Invoke-StrykerMutationTest -SolutionPath $generatedSln -OutputPath $outputDirectory -Configuration $Configuration -ReportOnly:$ReportOnly | Out-Null

    $summaryPath = Join-Path $outputDirectory 'mutation-summary.json'
    $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
    if ($summary.MutationResult -eq 'WARN') {
        Write-Host 'COMPLETED WITH WARNINGS: Mutation analysis finished; score thresholds were not met for some projects.' -ForegroundColor ([ConsoleColor]::Yellow)
    }
    else {
        Write-Host 'SUCCESS: Mutation analysis completed.' -ForegroundColor ([ConsoleColor]::Green)
    }
    Write-Host "Reports: $($summary.CompleteReportCount)/$($summary.ProjectCount) complete (skipped: $($summary.SkippedProjectCount)) | Below break threshold: $($summary.BelowBreakThresholdCount)"
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION MUTATION ANALYSIS COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
}

function Invoke-SolutionsPipeline {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [switch]$SkipCleanup,
        [switch]$IncludeMutation,
        [string]$LeaseDirectory
    )

    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $RepoRoot -OperationId "pipeline-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    $RepoRoot = $executionLease.RepositoryRoot
    try {
    $automationScriptsRoot = Join-Path (Join-Path (Join-Path $RepoRoot 'eng') 'src') 'agent-scripts'
    $coverageScript = Join-Path $automationScriptsRoot 'summarize-coverage-gaps.ps1'
    $mutationSummaryScript = Join-Path $automationScriptsRoot 'summarize-mutation-survivors.ps1'

    Write-AutomationBanner -Message '=== STARTING COMPLETE BUILD AND TEST PIPELINE ===' -ForegroundColor ([ConsoleColor]::Magenta) -InsertBlankLine
    Write-Host 'Pipeline will execute Mississippi solution followed by Sample solution'
    Write-Host 'Each step must complete successfully before proceeding to the next'
    if (-not $IncludeMutation) {
        Write-Host 'Mutation testing skipped (use -IncludeMutation to enable)'
    }
    if ($SkipCleanup) {
        Write-Host 'Cleanup skipped; validation results are provisional for this intermediate run.' -ForegroundColor ([ConsoleColor]::Yellow)
    }
    Write-Host

    $step = 1

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Mississippi Solution' -StepNumber ($step++) -Action { Invoke-MississippiSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Mississippi Code Style' -StepNumber ($step++) -Action { Invoke-MississippiSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }
    $mississippiTestResult = Invoke-AutomationStep -Name 'Run Mississippi Unit Tests' -StepNumber ($step++) -Action { Invoke-MississippiSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot -PassThru } -SilentSuccess
    if ($null -eq $mississippiTestResult -or [string]::IsNullOrWhiteSpace([string]$mississippiTestResult.CoverageReportPath)) {
        throw 'Mississippi unit-test operation did not return an aggregated coverage report path.'
    }
    Invoke-AutomationStep -Name 'Summarize Coverage Gaps' -StepNumber ($step++) -Action { Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments @('-NoProfile', '-File', $coverageScript, '-CoverageReportPath', $mississippiTestResult.CoverageReportPath, '-EmitTasks') | Out-Host }
    if ($IncludeMutation) {
        Invoke-AutomationStep -Name 'Run and Summarize Mississippi Mutation Tests' -StepNumber ($step++) -Action {
            $mutationArguments = @('-NoProfile', '-File', $mutationSummaryScript, '-Configuration', $Configuration, '-GenerateTasks', '-SkipLease')
            if (-not [string]::IsNullOrWhiteSpace($LeaseDirectory)) { $mutationArguments += @('-LeaseDirectory', $LeaseDirectory) }
            Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments $mutationArguments | Out-Host
        }
    }

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Sample Solution' -StepNumber ($step++) -Action { Invoke-SampleSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Sample Code Style' -StepNumber ($step++) -Action { Invoke-SampleSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }
    Invoke-AutomationStep -Name 'Run Sample Unit Tests' -StepNumber ($step++) -Action { Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess

    Invoke-AutomationStep -Name 'Final Build with Warnings as Errors' -StepNumber ($step++) -Action { Invoke-FinalSolutionsBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess

    Write-Host '=== PIPELINE COMPLETED SUCCESSFULLY ===' -ForegroundColor ([ConsoleColor]::Green)
    if ($SkipCleanup) {
        Write-Host 'Local build, test, coverage and final-build checks completed; cleanup was intentionally skipped, so this is not final handoff evidence.'
    }
    else {
        Write-Host 'Local build, test, coverage, cleanup and final-build checks completed. Deployment, browser and external CI checks are outside this command.'
    }
    }
    finally {
        Exit-RepositoryExecutionLease -Lease $executionLease
    }
}

function Get-SpringTestResult {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    [xml]$report = Get-Content -LiteralPath $Path -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ($report.TestRun.ResultSummary.outcome -ne 'Completed' -or
        [int]$counters.total -le 0 -or [int]$counters.passed -ne [int]$counters.total -or
        [int]$counters.executed -ne [int]$counters.total) {
        throw 'Spring validation requires a completed run with at least one test and every test passing.'
    }
    return [int]$counters.passed
}

function Install-SpringBrowser {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Project,
        [string]$Configuration = 'Release',
        [switch]$InstallBrowserDependencies
    )

    $targetDirectory = (Invoke-RepositoryProcess -FilePath dotnet -SuppressCommandEcho `
        -Arguments @('msbuild', $Project, "-property:Configuration=$Configuration", '-getProperty:TargetDir') | Out-String).Trim()
    $playwrightScript = Join-Path $targetDirectory 'playwright.ps1'
    if (-not (Test-Path -LiteralPath $playwrightScript -PathType Leaf)) { throw "Playwright installer missing: $playwrightScript" }
    $browserArguments = @('-NoProfile', '-File', $playwrightScript, 'install', 'chromium')
    if ($InstallBrowserDependencies) { $browserArguments += '--with-deps' }
    Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments $browserArguments
}

function Get-PrReadinessGhJson {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = & gh @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "GitHub read failed: $($output.Trim())" }
    return ConvertFrom-Json -InputObject $output
}

function Get-PrReadinessExpectedCheckPatterns {
    [CmdletBinding()]
    param([string[]]$ChangedPaths = @(), [AllowEmptyString()][string]$BaseRef = 'main')

    $patterns = [System.Collections.Generic.List[string]]::new()
    foreach ($pattern in @(
        '^CodeQL$',
        '^SonarCloud$',
        '^SonarCloud Code Analysis$',
        '^Build \(ubuntu-latest\)$',
        '^Build \(ubuntu-latest, mississippi\.slnx\)$',
        '^Build \(ubuntu-latest, samples\.slnx\)$',
        '^L0 Unit Tests \(ubuntu-latest, mississippi\.slnx\)$',
        '^L0 Unit Tests \(ubuntu-latest, samples\.slnx\)$',
        '^L1 Light Infrastructure Tests \(ubuntu-latest, mississippi\.slnx\)$',
        '^L1 Light Infrastructure Tests \(ubuntu-latest, samples\.slnx\)$',
        '^L2 Integration Tests \(Aspire\) \(ubuntu-latest, mississippi\.slnx\)$',
        '^L2 Integration Tests \(Aspire\) \(ubuntu-latest, samples\.slnx\)$',
        '^cleanup \(ubuntu-latest, mississippi\.slnx\)$',
        '^cleanup \(ubuntu-latest, samples\.slnx\)$',
        '^AppHost locked restore \(ubuntu-latest\)$',
        '^AppHost locked restore \(windows-latest\)$',
        '^pwsh-tests \(ubuntu-latest\)$',
        '^pwsh-tests \(windows-latest\)$',
        '^Markdown Lint$',
        '^L3 Spring E2E \(Smoke\)$',
        '^Validate repository issue reference$',
        '^label-by-files$',
        '^label-by-semver$',
        '^Analyze \(csharp\)$',
        '^Analyze \(actions\)$',
        '^Analyze \(javascript-typescript\)$',
        '^submit-nuget$'
    )) { $patterns.Add($pattern) }

    $standardWorkflowBase = $BaseRef -eq 'main' -or $BaseRef -match '^(?:feature|topic)/'
    if (-not $standardWorkflowBase) {
        $standardWorkflowPatterns = @(
            '^SonarCloud$', '^SonarCloud Code Analysis$', '^Build \(ubuntu-latest\)$',
            '^Build \(ubuntu-latest, mississippi\.slnx\)$', '^Build \(ubuntu-latest, samples\.slnx\)$',
            '^L0 Unit Tests \(ubuntu-latest, mississippi\.slnx\)$', '^L0 Unit Tests \(ubuntu-latest, samples\.slnx\)$',
            '^L1 Light Infrastructure Tests \(ubuntu-latest, mississippi\.slnx\)$', '^L1 Light Infrastructure Tests \(ubuntu-latest, samples\.slnx\)$',
            '^L2 Integration Tests \(Aspire\) \(ubuntu-latest, mississippi\.slnx\)$', '^L2 Integration Tests \(Aspire\) \(ubuntu-latest, samples\.slnx\)$',
            '^cleanup \(ubuntu-latest, mississippi\.slnx\)$', '^cleanup \(ubuntu-latest, samples\.slnx\)$',
            '^AppHost locked restore \(ubuntu-latest\)$', '^AppHost locked restore \(windows-latest\)$',
            '^pwsh-tests \(ubuntu-latest\)$', '^pwsh-tests \(windows-latest\)$', '^Markdown Lint$', '^L3 Spring E2E \(Smoke\)$'
        )
        foreach ($pattern in $standardWorkflowPatterns) { $null = $patterns.Remove($pattern) }
    }

    $docsApplicable = $standardWorkflowBase -and @($ChangedPaths | Where-Object { $_ -match '^(?:docs/|\.github/workflows/docusaurus\.yml$)' }).Count -gt 0
    if ($docsApplicable) { $patterns.Add('^Build Docusaurus Site$') }
    $csprojApplicable = @($ChangedPaths | Where-Object { $_ -match '^src/.+\.csproj$' }).Count -gt 0
    if ($csprojApplicable) { $patterns.Add('^Validate src csproj descriptions$') }
    $copilotSetupApplicable = @($ChangedPaths | Where-Object { $_ -eq '.github/workflows/copilot-setup-steps.yml' }).Count -gt 0
    if ($copilotSetupApplicable) { $patterns.Add('^copilot-setup-steps$') }
    return @($patterns)
}

function Get-PrReadinessCheckState {
    param([Parameter(Mandatory)][object]$CheckRun)

    if ([string]$CheckRun.conclusion -eq 'success') { return 'pass' }
    if ([string]$CheckRun.status -eq 'completed') { return 'fail' }
    return 'pending'
}

function Test-PrReadinessCheckRunBelongsToPullRequest {
    param([Parameter(Mandatory)][object]$CheckRun, [Parameter(Mandatory)][int]$PullRequestNumber, [AllowEmptyString()][string]$BaseRef)

    $pullRequests = $CheckRun.PSObject.Properties['pull_requests']
    if ($null -eq $pullRequests) { return $false }
    return @($pullRequests.Value | Where-Object {
        if ([int]$_.number -ne $PullRequestNumber) { return $false }
        $base = $_.PSObject.Properties['base']
        $null -eq $base -or [string]$base.Value.ref -eq $BaseRef
    }).Count -gt 0
}

function Get-PrReadinessCommitStatusState {
    param([Parameter(Mandatory)][object]$Status)

    switch ([string]$Status.state.ToLowerInvariant()) {
        'success' { return 'pass' }
        'pending' { return 'pending' }
        default { return 'fail' }
    }
}

function Get-PrReadinessBodyText {
    param([Parameter(Mandatory)][object]$Value)

    $property = $Value.PSObject.Properties['body']
    if ($null -eq $property) { return '' }
    return [string]$property.Value
}

function Get-PrReadinessReviewAuthor {
    param([Parameter(Mandatory)][object]$Value)

    $user = $Value.PSObject.Properties['user']
    if ($null -ne $user -and $null -ne $user.Value -and $null -ne $user.Value.PSObject.Properties['login']) {
        return [string]$user.Value.login
    }
    return "review-$([string]$Value.id)"
}

function Get-PrReadinessBodyFingerprint {
    param([Parameter(Mandatory)][object]$Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes((Get-PrReadinessBodyText -Value $Value))
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-PrReadinessSnapshot { # NOSONAR - readiness snapshot intentionally coordinates paginated GitHub checks, reviews, threads, and stability fingerprints.
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryOwner,
        [Parameter(Mandatory)][string]$RepositoryName,
        [Parameter(Mandatory)][int]$PullRequestNumber,
        [scriptblock]$GhJsonProvider,
        [ValidateRange(0, 86400)][int]$PollingSeconds = 0
    )

    $getJson = if ($null -ne $GhJsonProvider) {
        { param([string[]]$Arguments) & $GhJsonProvider $Arguments }
    }
    else {
        { param([string[]]$Arguments) Get-PrReadinessGhJson -Arguments $Arguments }
    }
    $pullPath = "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber"
    $pull = & $getJson @('api', $pullPath)
    $headAtStart = [string]$pull.head.sha
    $baseAtStart = [string]$pull.base.sha
    $baseRefAtStart = if ($null -ne $pull.base.PSObject.Properties['ref']) { [string]$pull.base.ref } else { '' }
    $filePages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber/files", '--paginate', '--slurp'))
    $changedPaths = @($filePages | ForEach-Object { if ($_ -is [array]) { @($_) } else { @($_) } } | ForEach-Object {
        if ($null -ne $_.PSObject.Properties['filename']) { [string]$_.filename }
        if ($null -ne $_.PSObject.Properties['previous_filename']) { [string]$_.previous_filename }
    } | Where-Object { $_ })
    $checkPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/commits/$headAtStart/check-runs", '--paginate', '--slurp'))
    $checkRuns = @($checkPages | ForEach-Object { if ($_ -is [array]) { @($_) } else { @($_) } } | ForEach-Object { @($_.check_runs) } | Where-Object { Test-PrReadinessCheckRunBelongsToPullRequest -CheckRun $_ -PullRequestNumber $PullRequestNumber -BaseRef $baseRefAtStart })
    $statusPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/commits/$headAtStart/statuses", '--paginate', '--slurp'))
    $statuses = @($statusPages | ForEach-Object { if ($_ -is [array]) { @($_) } else { @($_) } } | Group-Object context | ForEach-Object { $_.Group | Sort-Object created_at -Descending | Select-Object -First 1 })
    $checks = [System.Collections.Generic.List[object]]::new()
    foreach ($checkRun in $checkRuns) {
        $checks.Add([pscustomobject]@{
            Name = [string]$checkRun.name
            State = Get-PrReadinessCheckState -CheckRun $checkRun
            Required = $false
            ExpectedIdentity = $false
        })
    }
    foreach ($status in $statuses) {
        $checks.Add([pscustomobject]@{
            Name = [string]$status.context
            State = Get-PrReadinessCommitStatusState -Status $status
            Required = $false
            ExpectedIdentity = $false
        })
    }
    $expectedPatterns = @(Get-PrReadinessExpectedCheckPatterns -ChangedPaths $changedPaths -BaseRef $baseRefAtStart)
    foreach ($pattern in $expectedPatterns) {
        if (@($checks | Where-Object { $_.Name -match $pattern }).Count -eq 0) {
            $checks.Add([pscustomobject]@{ Name = "required:$pattern"; State = 'missing'; Required = $true; ExpectedIdentity = $true })
        }
        else {
            foreach ($check in @($checks | Where-Object { $_.Name -match $pattern })) { $check.Required = $true; $check.ExpectedIdentity = $true }
        }
    }

    $reviewsPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber/reviews", '--paginate', '--slurp'))
    $reviews = @($reviewsPages | ForEach-Object { if ($_ -is [array]) { @($_) } else { @($_) } })
    $latestReviewByAuthor = @{}
    foreach ($review in @($reviews | Where-Object { $_.state -in @('APPROVED', 'CHANGES_REQUESTED', 'DISMISSED') } | Sort-Object submitted_at)) {
        $author = Get-PrReadinessReviewAuthor -Value $review
        $latestReviewByAuthor[$author] = $review
    }
    $currentReviews = @($latestReviewByAuthor.Values)
    $approvals = @($currentReviews | Where-Object { $_.state -eq 'APPROVED' }).Count
    $reviewDecision = if (@($currentReviews | Where-Object { $_.state -eq 'CHANGES_REQUESTED' }).Count -gt 0) { 'CHANGES_REQUESTED' } elseif ($approvals -gt 0) { 'APPROVED' } else { '' }

    $threadQuery = 'query($owner:String!,$repo:String!,$number:Int!,$cursor:String){repository(owner:$owner,name:$repo){pullRequest(number:$number){reviewDecision reviewThreads(first:100,after:$cursor){nodes{id isResolved isOutdated comments(first:20){nodes{databaseId body author{login} path line url}}} pageInfo{hasNextPage endCursor}}}}}'
    $threadQueryExpectedDigest = 'SHA256:408a4e4a10fcf7a747ce76b4f2894f1933d62c01b8a51af6e0e9adc15ef45169'
    $threadQueryDigestBytes = [System.Text.Encoding]::UTF8.GetBytes($threadQuery)
    $threadQueryDigestHash = [System.Security.Cryptography.SHA256]::HashData($threadQueryDigestBytes)
    $threadQueryDigest = 'SHA256:' + (($threadQueryDigestHash | ForEach-Object { $_.ToString('x2') }) -join '')
    if ($threadQueryDigest -ne $threadQueryExpectedDigest) { throw 'Readiness GraphQL query integrity verification failed.' }
    $threads = [System.Collections.Generic.List[object]]::new()
    $cursor = $null
    $graphqlReviewDecision = ''
    do {
        $graphqlArguments = @('api', 'graphql', '-f', "query=$threadQuery", '-F', "owner=$RepositoryOwner", '-F', "repo=$RepositoryName", '-F', "number=$PullRequestNumber", '-F', $(if ($null -eq $cursor) { 'cursor=null' } else { "cursor=$cursor" }))
        $threadPage = & $getJson $graphqlArguments
        if ($null -ne $threadPage.PSObject.Properties['errors'] -and @($threadPage.errors).Count -gt 0) { throw "Readiness GraphQL thread query returned errors: $($threadPage.errors | ConvertTo-Json -Compress)" }
        $graphqlReviewDecision = [string]$threadPage.data.repository.pullRequest.reviewDecision
        foreach ($thread in @($threadPage.data.repository.pullRequest.reviewThreads.nodes)) { $threads.Add($thread) }
        $hasNextPage = [bool]$threadPage.data.repository.pullRequest.reviewThreads.pageInfo.hasNextPage
        $cursor = [string]$threadPage.data.repository.pullRequest.reviewThreads.pageInfo.endCursor
    } while ($hasNextPage)
    if (-not [string]::IsNullOrWhiteSpace($graphqlReviewDecision)) { $reviewDecision = $graphqlReviewDecision }

    if ($PollingSeconds -gt 0) { Start-Sleep -Seconds $PollingSeconds }
    $pullAtEnd = & $getJson @('api', $pullPath)
    $finalHead = [string]$pullAtEnd.head.sha
    $baseRefAtEnd = if ($null -ne $pullAtEnd.base.PSObject.Properties['ref']) { [string]$pullAtEnd.base.ref } else { '' }
    $finalCheckPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/commits/$finalHead/check-runs", '--paginate', '--slurp'))
    $finalCheckRuns = @($finalCheckPages | ForEach-Object { @($_.check_runs) } | Where-Object { Test-PrReadinessCheckRunBelongsToPullRequest -CheckRun $_ -PullRequestNumber $PullRequestNumber -BaseRef $baseRefAtEnd })
    $finalStatusPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/commits/$finalHead/statuses", '--paginate', '--slurp'))
    $finalStatuses = @($finalStatusPages | ForEach-Object { if ($_ -is [array]) { @($_) } else { @($_) } } | Group-Object context | ForEach-Object { $_.Group | Sort-Object created_at -Descending | Select-Object -First 1 })
    $statusFingerprintStart = (@($statuses | ForEach-Object { "$($_.context)=$([string](Get-PrReadinessCommitStatusState -Status $_))" } | Sort-Object) -join '|')
    $statusFingerprintEnd = (@($finalStatuses | ForEach-Object { "$($_.context)=$([string](Get-PrReadinessCommitStatusState -Status $_))" } | Sort-Object) -join '|')
    $checkFingerprintStart = (@($checkRuns | ForEach-Object { "$($_.name)=$([string](Get-PrReadinessCheckState -CheckRun $_))" } | Sort-Object) -join '|')
    $checkFingerprintEnd = (@($finalCheckRuns | ForEach-Object { "$($_.name)=$([string](Get-PrReadinessCheckState -CheckRun $_))" } | Sort-Object) -join '|')
    $checkFingerprintStart = "$checkFingerprintStart|$statusFingerprintStart"
    $checkFingerprintEnd = "$checkFingerprintEnd|$statusFingerprintEnd"

    $finalReviewsPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber/reviews", '--paginate', '--slurp'))
    $finalReviews = @($finalReviewsPages | ForEach-Object { @($_) })
    $reviewFingerprintStart = (@($reviews | Where-Object { $_.state -in @('APPROVED', 'CHANGES_REQUESTED', 'DISMISSED') } | Sort-Object state, id | ForEach-Object { "$(Get-PrReadinessReviewAuthor -Value $_)=$($_.state)#$($_.id)" }) -join '|')
    $reviewFingerprintEnd = (@($finalReviews | Where-Object { $_.state -in @('APPROVED', 'CHANGES_REQUESTED', 'DISMISSED') } | Sort-Object state, id | ForEach-Object { "$(Get-PrReadinessReviewAuthor -Value $_)=$($_.state)#$($_.id)" }) -join '|')
    $commentFingerprintStart = (@($reviews | Where-Object { $_.state -eq 'COMMENTED' -and -not [string]::IsNullOrWhiteSpace((Get-PrReadinessBodyText -Value $_)) } | Sort-Object id | ForEach-Object { "$($_.id)=$(Get-PrReadinessBodyFingerprint -Value $_)" }) -join '|')
    $commentFingerprintEnd = (@($finalReviews | Where-Object { $_.state -eq 'COMMENTED' -and -not [string]::IsNullOrWhiteSpace((Get-PrReadinessBodyText -Value $_)) } | Sort-Object id | ForEach-Object { "$($_.id)=$(Get-PrReadinessBodyFingerprint -Value $_)" }) -join '|')
    $finalLatestReviewByAuthor = @{}
    foreach ($review in @($finalReviews | Where-Object { $_.state -in @('APPROVED', 'CHANGES_REQUESTED', 'DISMISSED') } | Sort-Object submitted_at)) {
        $author = Get-PrReadinessReviewAuthor -Value $review
        $finalLatestReviewByAuthor[$author] = $review
    }
    $approvals = @($finalLatestReviewByAuthor.Values | Where-Object { $_.state -eq 'APPROVED' }).Count

    $finalThreads = [System.Collections.Generic.List[object]]::new()
    $finalCursor = $null
    $finalHasNextPage = $false
    do {
        $finalGraphqlArguments = @('api', 'graphql', '-f', "query=$threadQuery", '-F', "owner=$RepositoryOwner", '-F', "repo=$RepositoryName", '-F', "number=$PullRequestNumber", '-F', $(if ($null -eq $finalCursor) { 'cursor=null' } else { "cursor=$finalCursor" }))
        $finalThreadPage = & $getJson $finalGraphqlArguments
        if ($null -ne $finalThreadPage.PSObject.Properties['errors'] -and @($finalThreadPage.errors).Count -gt 0) { throw "Readiness GraphQL final thread query returned errors: $($finalThreadPage.errors | ConvertTo-Json -Compress)" }
        if (-not [string]::IsNullOrWhiteSpace([string]$finalThreadPage.data.repository.pullRequest.reviewDecision)) {
            $reviewDecision = [string]$finalThreadPage.data.repository.pullRequest.reviewDecision
        }
        foreach ($thread in @($finalThreadPage.data.repository.pullRequest.reviewThreads.nodes)) { $finalThreads.Add($thread) }
        $finalHasNextPage = [bool]$finalThreadPage.data.repository.pullRequest.reviewThreads.pageInfo.hasNextPage
        $finalCursor = [string]$finalThreadPage.data.repository.pullRequest.reviewThreads.pageInfo.endCursor
    } while ($finalHasNextPage)
    $reviewEventsByAuthor = @{}
    foreach ($review in @($finalReviews | Where-Object { $_.state -in @('APPROVED', 'CHANGES_REQUESTED', 'COMMENTED', 'DISMISSED') } | Sort-Object submitted_at)) {
        $author = Get-PrReadinessReviewAuthor -Value $review
        if (-not $reviewEventsByAuthor.ContainsKey($author)) { $reviewEventsByAuthor[$author] = [System.Collections.Generic.List[object]]::new() }
        $reviewEventsByAuthor[$author].Add($review)
    }
    $reviewDispositionList = [System.Collections.Generic.List[object]]::new()
    foreach ($reviewEvents in $reviewEventsByAuthor.Values) {
        $activeFeedback = [System.Collections.Generic.List[object]]::new()
        foreach ($review in $reviewEvents) {
            $body = Get-PrReadinessBodyText -Value $review
            if ([string]$review.state -in @('APPROVED', 'DISMISSED')) {
                $activeFeedback.Clear()
                if (-not [string]::IsNullOrWhiteSpace($body)) {
                    $reviewDispositionList.Add([pscustomobject]@{ Id = [string]$review.id; Author = Get-PrReadinessReviewAuthor -Value $review; State = [string]$review.state; Disposition = 'addressed' })
                }
            }
            elseif ([string]$review.state -eq 'CHANGES_REQUESTED') {
                $activeFeedback.Clear()
                if (-not [string]::IsNullOrWhiteSpace($body)) { $activeFeedback.Add($review) }
            }
            elseif (-not [string]::IsNullOrWhiteSpace($body)) {
                $activeFeedback.Add($review)
            }
        }
        foreach ($review in $activeFeedback) {
            $reviewDispositionList.Add([pscustomobject]@{ Id = [string]$review.id; Author = Get-PrReadinessReviewAuthor -Value $review; State = [string]$review.state; Disposition = 'pending' })
        }
    }
    $reviewDispositions = @($reviewDispositionList)
    $threadFingerprintStart = (@($threads | Sort-Object id | ForEach-Object { "$($_.id)=$($_.isResolved)/$($_.isOutdated):$(@($_.comments.nodes | ForEach-Object { $_.databaseId }) -join ',')" }) -join '|')
    $threadFingerprintEnd = (@($finalThreads | Sort-Object id | ForEach-Object { "$($_.id)=$($_.isResolved)/$($_.isOutdated):$(@($_.comments.nodes | ForEach-Object { $_.databaseId }) -join ',')" }) -join '|')
    $mutableEvidenceStable = $checkFingerprintStart -eq $checkFingerprintEnd -and
        $reviewFingerprintStart -eq $reviewFingerprintEnd -and
        $commentFingerprintStart -eq $commentFingerprintEnd -and
        $threadFingerprintStart -eq $threadFingerprintEnd
    $generalComments = @()
    try {
        $commentPages = @(& $getJson @('api', "repos/$RepositoryOwner/$RepositoryName/issues/$PullRequestNumber/comments", '--paginate', '--slurp'))
        $generalComments = @($commentPages | ForEach-Object { @($_) } | Where-Object { -not [string]::IsNullOrWhiteSpace((Get-PrReadinessBodyText -Value $_)) })
    }
    catch {
        # A provider that cannot expose discussion comments is incomplete.
        $mutableEvidenceStable = $false
    }
    $pollingCompleted = $PollingSeconds -ge 300
    [pscustomobject][ordered]@{
        DataComplete = $true
        HeadAtStart = $headAtStart
        HeadAtEnd = [string]$pullAtEnd.head.sha
        BaseAtStart = $baseAtStart
        BaseAtEnd = [string]$pullAtEnd.base.sha
        BaseRefAtStart = $baseRefAtStart
        BaseRefAtEnd = $baseRefAtEnd
        PullRequestState = [string]$pullAtEnd.state
        IsDraft = [bool]$pullAtEnd.draft
        MergeableState = [string]$pullAtEnd.mergeable_state
        ReviewDecision = $reviewDecision
        Checks = @($checks)
        ReviewThreads = @($finalThreads | ForEach-Object {
            $latestComment = @($_.comments.nodes | Sort-Object databaseId | Select-Object -Last 1)
            [pscustomobject]@{
                IsResolved = [bool]$_.isResolved
                IsOutdated = [bool]$_.isOutdated
                LatestCommentId = if ($latestComment.Count -gt 0) { [string]$latestComment[0].databaseId } else { '' }
                LatestCommentBody = if ($latestComment.Count -gt 0) { Get-PrReadinessBodyText -Value $latestComment[0] } else { '' }
                LatestCommentAuthor = if ($latestComment.Count -gt 0) { [string]$latestComment[0].author.login } else { '' }
                Disposition = if ([bool]$_.isResolved -or [bool]$_.isOutdated) { 'addressed' } else { 'pending' }
            }
        })
        Approvals = $approvals
        IssueReferenceVerified = $false
        DescriptionReviewed = $false
        PollingCompleted = $pollingCompleted
        EvidenceStable = $mutableEvidenceStable
        GeneralFeedbackCount = @($generalComments).Count
        ReviewDispositions = @($reviewDispositions)
        ReviewFeedbackCount = @($reviewDispositions | Where-Object Disposition -EQ 'pending').Count
        PullRequestUrl = [string]$pullAtEnd.html_url
    }
}

function Get-PrReadinessReport { # NOSONAR - readiness reporting intentionally evaluates the complete mechanical and disposition gate.
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Snapshot)

    $blockers = [System.Collections.Generic.List[string]]::new()
    if (-not $Snapshot.DataComplete) { $blockers.Add('Required GitHub data is incomplete or inaccessible.') }
    if ($Snapshot.HeadAtStart -ne $Snapshot.HeadAtEnd) { $blockers.Add('PR head changed during collection; snapshot is stale.') }
    if ($Snapshot.BaseAtStart -ne $Snapshot.BaseAtEnd -or ($null -ne $Snapshot.PSObject.Properties['BaseRefAtStart'] -and $Snapshot.BaseRefAtStart -ne $Snapshot.BaseRefAtEnd)) { $blockers.Add('PR base changed during collection; snapshot is stale.') }
    $state = if ($null -ne $Snapshot.PSObject.Properties['PullRequestState']) { [string]$Snapshot.PullRequestState } else { 'open' }
    $draft = if ($null -ne $Snapshot.PSObject.Properties['IsDraft']) { [bool]$Snapshot.IsDraft } else { $false }
    $mergeableState = if ($null -ne $Snapshot.PSObject.Properties['MergeableState']) { [string]$Snapshot.MergeableState } else { 'clean' }
    if ($state -ne 'open') { $blockers.Add("Pull request is not open (state: $state).") }
    if ($draft) { $blockers.Add('Pull request is still a draft.') }
    if ($mergeableState -ne 'clean') { $blockers.Add("Pull request cannot currently advance (mergeability: $mergeableState).") }
    if (-not [string]::IsNullOrWhiteSpace([string]$Snapshot.ReviewDecision) -and [string]$Snapshot.ReviewDecision -ne 'APPROVED') { $blockers.Add("Aggregate review decision is $($Snapshot.ReviewDecision).") }
    foreach ($check in @($Snapshot.Checks | Where-Object { $_.Required -and $_.State -ne 'pass' })) { $blockers.Add("Required check '$($check.Name)' is $($check.State).") }
    foreach ($thread in @($Snapshot.ReviewThreads)) {
        $disposition = $thread.PSObject.Properties['Disposition']
        $pendingDisposition = $null -ne $disposition -and [string]$disposition.Value -eq 'pending'
        if ($pendingDisposition -or -not [bool]$thread.IsResolved) {
            $blockers.Add('An unresolved review thread remains.')
        }
    }
    if ([int]$Snapshot.Approvals -lt 1) { $blockers.Add('Required current review approval evidence is missing.') }
    if (-not $Snapshot.PollingCompleted) { $blockers.Add('Required post-push review polling evidence is incomplete.') }
    if ($null -ne $Snapshot.PSObject.Properties['EvidenceStable'] -and -not [bool]$Snapshot.EvidenceStable) {
        $blockers.Add('Mutable checks, reviews, or threads changed during collection; rerun the readiness snapshot.')
    }
    if ($null -ne $Snapshot.PSObject.Properties['GeneralFeedbackCount'] -and [int]$Snapshot.GeneralFeedbackCount -gt 0) {
        $blockers.Add('General PR discussion comments require review disposition.')
    }
    if ($null -ne $Snapshot.PSObject.Properties['ReviewFeedbackCount'] -and [int]$Snapshot.ReviewFeedbackCount -gt 0) {
        $blockers.Add('Comment-only review feedback requires review disposition.')
    }
    $mechanicalReady = $blockers.Count -eq 0
    $semanticReady = [bool]$Snapshot.IssueReferenceVerified -and [bool]$Snapshot.DescriptionReviewed
    [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Status = if ($mechanicalReady -and $semanticReady) { 'READY' } elseif ($mechanicalReady) { 'MECHANICALLY_READY_SEMANTIC_REVIEW_REQUIRED' } else { 'INCOMPLETE' }
        MechanicalGateReady = $mechanicalReady
        SemanticReviewRequired = -not $semanticReady
        Head = $Snapshot.HeadAtEnd
        Base = $Snapshot.BaseAtEnd
        PullRequestUrl = $Snapshot.PullRequestUrl
        Blockers = @($blockers)
        Checks = @($Snapshot.Checks)
        Approvals = [int]$Snapshot.Approvals
        ReviewThreads = @($Snapshot.ReviewThreads)
    }
}

function Invoke-SpringValidation {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [ValidateSet('L2', 'L3')][string]$TestLevel = 'L3',
        [ValidateSet('Smoke', 'Full')][string]$Suite = 'Smoke',
        [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
        [switch]$Doctor,
        [switch]$InstallBrowserDependencies
    )

    $TestLevel = $TestLevel.ToUpperInvariant()
    $Suite = if ($Suite -eq 'Smoke') { 'Smoke' } else { 'Full' }
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
    $runDirectory = New-AutomationRunDirectory -Root (Join-Path $RepoRoot 'artifacts/spring') -Prefix "$TestLevel-$Suite-$([guid]::NewGuid().ToString('N'))"
    $project = Join-Path $RepoRoot "samples/Spring/Spring.${TestLevel}Tests/Spring.${TestLevel}Tests.csproj"
    $summary = [ordered]@{ schemaVersion = 1; status = 'FAIL'; phase = 'selection'; testLevel = $TestLevel; suite = $Suite; project = $project; passed = 0; artifacts = $runDirectory }
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $previousPath = $env:PATH
    $previousArtifacts = $env:SPRING_TEST_ARTIFACTS
    $previousBrowsers = $env:PLAYWRIGHT_BROWSERS_PATH
    Push-Location $RepoRoot
    try {
        if ($TestLevel -eq 'L2' -and $Suite -eq 'Smoke' -and -not $Doctor) {
            throw 'Spring smoke journeys are L3. Use -TestLevel L2 -Suite Full for API/infrastructure tests.'
        }
        $summary.phase = 'prerequisites'
        $summary.sdk = (Invoke-RepositoryProcess -FilePath dotnet -Arguments @('--version') `
            -ErrorMessage 'Install the SDK selected by global.json.' | Out-String).Trim()
        $dockerOs = (Invoke-RepositoryProcess -FilePath docker -Arguments @('info', '--format', '{{.OSType}}') `
            -ErrorMessage 'Start Docker with Linux containers and grant this user access to its daemon.' | Out-String).Trim()
        if ($dockerOs -ne 'linux') { throw 'Spring requires Docker running Linux containers.' }
        [xml]$packages = Get-Content -LiteralPath (Join-Path $RepoRoot 'Directory.Packages.props') -Raw
        $aspireVersion = ($packages.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Aspire.Hosting.AppHost').Version
        if ([string]::IsNullOrWhiteSpace($aspireVersion)) { throw 'Aspire.Hosting.AppHost version is missing from Directory.Packages.props.' }
        $summary.aspireVersion = $aspireVersion
        if ($Doctor) {
            $summary.status = 'READY'
            return
        }

        $summary.phase = 'tooling'
        $toolPath = Join-Path $RepoRoot "artifacts/tools/aspire-$aspireVersion"
        $aspire = Join-Path $toolPath $(if ($IsWindows) { 'aspire.cmd' } else { 'aspire' })
        if (-not (Test-Path -LiteralPath $aspire -PathType Leaf)) {
            Invoke-RepositoryProcess -FilePath dotnet -Arguments @('tool', 'install', 'Aspire.Cli', '--version', $aspireVersion, '--tool-path', $toolPath)
        }
        $env:PATH = $toolPath + [System.IO.Path]::PathSeparator + $previousPath
        $installedVersion = (Invoke-RepositoryProcess -FilePath $aspire -Arguments @('--version') | Out-String).Trim()
        if ($installedVersion.Split('+')[0] -ne $aspireVersion) { throw "Expected Aspire CLI $aspireVersion; found $installedVersion." }
        $summary.phase = 'restore'
        Invoke-RepositoryProcess -FilePath dotnet -Arguments @('restore', $project, '--locked-mode') |
            Tee-Object -FilePath (Join-Path $runDirectory 'restore.log') | Out-Host
        $summary.phase = 'build'
        Invoke-SolutionBuild -SolutionPath $project -Configuration $Configuration -WarnAsError -NoRestore |
            Tee-Object -FilePath (Join-Path $runDirectory 'build.log') | Out-Host
        if ($TestLevel -eq 'L3') {
            $summary.phase = 'browser'
            $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $RepoRoot 'artifacts/tools/playwright'
            Install-SpringBrowser -Project $project -Configuration $Configuration -InstallBrowserDependencies:$InstallBrowserDependencies
        }
        $env:SPRING_TEST_ARTIFACTS = $runDirectory
        $summary.phase = 'test'
        $testArguments = @('test', '--project', $project, '--configuration', $Configuration, '--no-build', '--no-restore',
            '--report-xunit-trx', '--report-xunit-trx-filename', 'spring.trx', '--results-directory', $runDirectory,
            '--minimum-expected-tests', '1', '--timeout', '15m')
        if ($Suite -eq 'Smoke') { $testArguments += @('--filter', 'Category=Smoke') }
        Invoke-RepositoryProcess -FilePath dotnet -Arguments $testArguments |
            Tee-Object -FilePath (Join-Path $runDirectory 'test.log') | Out-Host
        $summary.passed = Get-SpringTestResult -Path (Join-Path $runDirectory 'spring.trx')
        $summary.phase = 'complete'
        $summary.status = 'PASS'
    }
    catch {
        $summary.error = $_.Exception.Message
        throw
    }
    finally {
        $env:PATH = $previousPath
        $env:SPRING_TEST_ARTIFACTS = $previousArtifacts
        $env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowsers
        Pop-Location
        $summary.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)
        $summaryPath = Join-Path $runDirectory 'summary.json'
        $summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
        Write-Output "RESULT: $($summary.status) | LEVEL: $TestLevel | SUITE: $Suite | PHASE: $($summary.phase) | PASSED: $($summary.passed)"
        Write-Output "SUMMARY: $summaryPath"
    }
}

Export-ModuleMember -Function Get-RepositoryRoot, Resolve-RepositoryExecutionPath, Get-RepositoryExecutionLeasePath, Enter-RepositoryExecutionLease, Exit-RepositoryExecutionLease, Invoke-RepositoryProcess, Write-AutomationBanner, Invoke-AutomationStep, Invoke-DotnetToolRestore, Invoke-SolutionRestore, Invoke-SolutionBuild, New-AutomationRunDirectory, Invoke-SolutionTests, Invoke-SlnGeneration, Invoke-ReSharperCleanup, Get-TestProjects, Read-MutationReport, Get-MutationReportPath, Invoke-StrykerMutationTestPerProject, Invoke-StrykerMutationTest, Invoke-MississippiSolutionBuild, Invoke-SampleSolutionBuild, Invoke-FinalSolutionsBuild, Invoke-MississippiSolutionUnitTests, Invoke-SampleSolutionUnitTests, Invoke-MississippiSolutionCleanup, Invoke-SampleSolutionCleanup, Invoke-MississippiSolutionMutationTests, Invoke-SolutionsPipeline, Invoke-SpringValidation, Get-PrReadinessGhJson, Get-PrReadinessExpectedCheckPatterns, Get-PrReadinessSnapshot, Get-PrReadinessReport








