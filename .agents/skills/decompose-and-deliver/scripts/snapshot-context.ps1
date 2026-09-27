#!/usr/bin/env pwsh
#requires -Version 7.4

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [string[]]$ContextPath = @(),
    [Parameter(DontShow)][string]$HashPath,
    [Parameter(DontShow)][string]$MetadataPath,
    [Parameter(DontShow)][string]$InspectionContext,
    [Parameter(DontShow)][switch]$WaitForInspectionOwner
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Enable-ContextOwnershipType {
    if ($null -ne ('PortableDelivery.InspectionOwnership' -as [type])) { return }
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PortableDelivery
{
    /// <summary>Expose native ownership operations publicly for PowerShell Add-Type binding.</summary>
    public static class InspectionOwnership
    {
        /// <summary>Create a private Windows job that terminates members when its handle closes.</summary>
        /// <returns>The owned job handle.</returns>
        public static IntPtr CreateWindowsJob()
        {
            IntPtr job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
            int size = IntPtr.Size == 8 ? 144 : 112;
            IntPtr limits = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(new byte[size], 0, limits, size);
                // JOBOBJECT_EXTENDED_LIMIT_INFORMATION begins with two LARGE_INTEGER values.
                Marshal.WriteInt32(limits, 16, 0x2200); // KILL_ON_JOB_CLOSE and JOB_MEMORY.
                Marshal.WriteIntPtr(limits, IntPtr.Size == 8 ? 120 : 100, new IntPtr(536870912)); // 512 MiB for the whole job.
                if (!SetInformationJobObject(job, 9, limits, size))
                {
                    int error = Marshal.GetLastPInvokeError();
                    CloseHandle(job);
                    throw new Win32Exception(error);
                }
                return job;
            }
            finally { Marshal.FreeHGlobal(limits); }
        }

        /// <summary>Attach a gated inspection process before it launches native children.</summary>
        /// <param name="job">The owned job handle.</param>
        /// <param name="process">The gated process handle.</param>
        public static void AttachWindowsProcess(IntPtr job, IntPtr process)
        {
            if (!AssignProcessToJobObject(job, process)) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        }

        /// <summary>Terminate every remaining member of the owned job.</summary>
        /// <param name="job">The owned job handle.</param>
        public static void TerminateWindowsJob(IntPtr job)
        {
            if (!TerminateJobObject(job, 1)) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        }

        /// <summary>Read the active process count from the owned job.</summary>
        /// <param name="job">The owned job handle.</param>
        /// <returns>The active process count.</returns>
        public static int GetWindowsActiveProcesses(IntPtr job)
        {
            IntPtr accounting = Marshal.AllocHGlobal(48);
            try
            {
                if (!QueryInformationJobObject(job, 1, accounting, 48, IntPtr.Zero)) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
                return Marshal.ReadInt32(accounting, 40);
            }
            finally { Marshal.FreeHGlobal(accounting); }
        }

        /// <summary>Release the owned job handle.</summary>
        /// <param name="job">The owned job handle.</param>
        public static void CloseWindowsJob(IntPtr job)
        {
            if (!CloseHandle(job)) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        }

        /// <summary>Create a Unix session and process group before inspecting the target.</summary>
        /// <returns>The session identifier.</returns>
        public static int CreateUnixSession()
        {
            int session = SetSid();
            if (session < 0) { throw new Win32Exception(Marshal.GetLastPInvokeError()); }
            return session;
        }

        /// <summary>Signal an already verified owned Unix process group.</summary>
        /// <param name="group">The verified process group.</param>
        public static void TerminateUnixGroup(int group)
        {
            if (Kill(-group, 9) != 0)
            {
                int error = Marshal.GetLastPInvokeError();
                if (error != 3) { throw new Win32Exception(error); }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, int informationClass, IntPtr information, int length);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateJobObject(IntPtr job, uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryInformationJobObject(IntPtr job, int informationClass, IntPtr information, int length, IntPtr returnedLength);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
        [DllImport("libc", EntryPoint = "setsid", SetLastError = true)]
        private static extern int SetSid();
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int process, int signal);
    }
}
'@
}

function Get-ContextUnixGroupMembers {
    param([int]$Group, [string]$Nonce, [Collections.Generic.Dictionary[int, string]]$KnownMembers)
    $members = @()
    foreach ($directory in [IO.Directory]::EnumerateDirectories('/proc')) {
        if ([IO.Path]::GetFileName($directory) -notmatch '^\d+$') { continue }
        try {
            $value = [IO.File]::ReadAllText([IO.Path]::Combine($directory, 'stat'))
            $fields = $value.Substring($value.LastIndexOf(')') + 2).Split(' ')
            if ([int]$fields[2] -ne $Group -or $fields[0] -in @('Z', 'X')) { continue }
            $environment = [IO.File]::ReadAllText([IO.Path]::Combine($directory, 'environ'))
        }
        catch [IO.FileNotFoundException] { continue }
        catch [IO.DirectoryNotFoundException] { continue }
        $processId = [int][IO.Path]::GetFileName($directory)
        if ($environment.Split([char]0).Contains('DECOMPOSE_INSPECTION_OWNER=' + $Nonce)) { $KnownMembers[$processId] = $fields[19] }
        elseif (-not $KnownMembers.ContainsKey($processId) -or $KnownMembers[$processId] -cne $fields[19]) {
            throw 'Inspection process-group ownership is unconfirmed; reconcile manually before retrying.'
        }
        $members += $processId
    }
    return $members
}

function Stop-ContextNativeProcess {
    param([Diagnostics.Process]$Child)
    if ($Child.HasExited) { return }
    try { $Child.Kill($true) }
    catch [InvalidOperationException] { if (-not $Child.HasExited) { throw } }
}

function Stop-ContextInspectionOwner {
    param([Diagnostics.Process]$Child, [IntPtr]$Job, [string]$Nonce)
    $elapsed = [Diagnostics.Stopwatch]::StartNew()
    $knownMembers = [Collections.Generic.Dictionary[int, string]]::new()
    try {
        if ($IsWindows) { [PortableDelivery.InspectionOwnership]::TerminateWindowsJob($Job) }
        elseif (@(Get-ContextUnixGroupMembers $Child.Id $Nonce $knownMembers).Count -gt 0) { [PortableDelivery.InspectionOwnership]::TerminateUnixGroup($Child.Id) }
        Stop-ContextNativeProcess $Child
        $remaining = [Math]::Max(0, 2000 - [int]$elapsed.ElapsedMilliseconds)
        if (-not $Child.WaitForExit($remaining)) { throw 'Inspection termination is unconfirmed; reconcile manually before retrying.' }
        $pause = [Threading.ManualResetEventSlim]::new($false)
        try {
            do {
                $active = if ($IsWindows) { [PortableDelivery.InspectionOwnership]::GetWindowsActiveProcesses($Job) } else { @(Get-ContextUnixGroupMembers $Child.Id $Nonce $knownMembers).Count }
                if ($active -eq 0) { return }
                if ($elapsed.ElapsedMilliseconds -ge 2000) { throw 'Inspection descendant termination is unconfirmed; reconcile manually before retrying.' }
                $null = $pause.Wait(10)
            } while ($true)
        }
        finally { $pause.Dispose() }
    }
    finally { if ($IsWindows) { [PortableDelivery.InspectionOwnership]::CloseWindowsJob($Job) } }
}

function Invoke-ContextGit {
    param([string]$Root, [string[]]$Arguments, [int[]]$AcceptedExitCodes = @(0), [switch]$WithResult, [string]$InputText)
    $options = @('--no-replace-objects', '--no-optional-locks', '-c', 'core.fsmonitor=', '-c', 'core.trustctime=true', '-c', 'core.checkStat=default', '-c', 'core.ignoreStat=false', '-c', 'core.ignoreCase=false', '-c', 'core.commitGraph=false', '-c', 'core.untrackedCache=false', '-C', $root)
    if (-not $IsWindows) { $options = @('-c', 'core.fileMode=true') + $options }
    $inputOption = if ($PSBoundParameters.ContainsKey('InputText')) { @{ InputText = $InputText } } else { @{} }
    $application = $gitApplication
    $nativeArguments = $options + $Arguments
    if ($IsLinux) {
        $application = $gitLimiter
        $nativeArguments = @('--as=268435456:268435456', '--', $gitApplication) + $nativeArguments
    }
    $result = Invoke-ContextNativeOutput $application $nativeArguments -AcceptedExitCodes $AcceptedExitCodes -WithResult @inputOption
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

function New-ContextOutputCapture {
    param([IO.Stream]$Stream)
    $buffer = [byte[]]::new(8192)
    return [pscustomobject]@{ Stream = $Stream; Buffer = $buffer; Content = [IO.MemoryStream]::new(); Read = $Stream.ReadAsync($buffer, 0, $buffer.Length) }
}

function Update-ContextOutputCapture {
    param([psobject]$Capture)
    if ($null -eq $Capture.Read -or -not $Capture.Read.IsCompleted) { return }
    $count = $Capture.Read.GetAwaiter().GetResult()
    if ($count -eq 0) { $Capture.Read = $null; return }
    if ($Capture.Content.Length + $count -gt 1048576) { throw 'Native context output exceeded its one-MiB stream limit; inspect the target manually.' }
    $Capture.Content.Write($Capture.Buffer, 0, $count)
    $Capture.Read = $Capture.Stream.ReadAsync($Capture.Buffer, 0, $Capture.Buffer.Length)
}

function Get-ContextBoundedOutput {
    param([Diagnostics.Process]$Child, [int]$TimeoutMilliseconds, [Threading.Tasks.Task]$InputWrite)
    $captures = @((New-ContextOutputCapture $Child.StandardOutput.BaseStream), (New-ContextOutputCapture $Child.StandardError.BaseStream))
    $elapsed = [Diagnostics.Stopwatch]::StartNew()
    try {
        while (-not $Child.HasExited -or $null -ne $captures[0].Read -or $null -ne $captures[1].Read -or $null -ne $InputWrite) {
            foreach ($capture in $captures) { Update-ContextOutputCapture $capture }
            if ($null -ne $InputWrite -and $InputWrite.IsCompleted) {
                $null = $InputWrite.GetAwaiter().GetResult()
                $Child.StandardInput.Close()
                $InputWrite = $null
            }
            if ($elapsed.ElapsedMilliseconds -ge $TimeoutMilliseconds) { throw 'Native context inspection timed out; inspect the target manually.' }
            $pending = @($captures | Where-Object { $null -ne $_.Read } | Select-Object -First 1)
            if ($pending.Count -eq 0) { $null = $Child.WaitForExit(10) }
            else { $null = $pending[0].Read.Wait(10) }
        }
        $encoding = [Text.UTF8Encoding]::new($false, $true)
        return [pscustomobject]@{
            Output = $encoding.GetString($captures[0].Content.GetBuffer(), 0, [int]$captures[0].Content.Length)
            Error = $encoding.GetString($captures[1].Content.GetBuffer(), 0, [int]$captures[1].Content.Length)
        }
    }
    finally { foreach ($capture in $captures) { $capture.Content.Dispose() } }
}

function New-ContextInspectionOwner {
    param([Diagnostics.ProcessStartInfo]$Start, [switch]$OwnInspection)
    $owner = [pscustomobject]@{ Job = [IntPtr]::Zero; Nonce = [guid]::NewGuid().ToString('N') }
    if (-not $OwnInspection) { return $owner }
    if (-not $IsWindows -and -not $IsLinux) { throw 'Inspection process ownership is unsupported; inspect the target manually.' }
    Enable-ContextOwnershipType
    $Start.Environment['DECOMPOSE_INSPECTION_OWNER'] = $owner.Nonce
    if ($IsWindows) { $owner.Job = [PortableDelivery.InspectionOwnership]::CreateWindowsJob() }
    return $owner
}

function Close-ContextNativeOutput {
    param([Diagnostics.Process]$Child, [psobject]$Owner, [switch]$OwnInspection)
    try {
        if ($null -eq $Child) {
            if ($Owner.Job -ne [IntPtr]::Zero) { [PortableDelivery.InspectionOwnership]::CloseWindowsJob($Owner.Job) }
        } elseif ($OwnInspection) { Stop-ContextInspectionOwner $Child $Owner.Job $Owner.Nonce }
        else {
            Stop-ContextNativeProcess $Child
            if (-not $Child.WaitForExit(2000)) { throw 'Native inspection termination is unconfirmed; reconcile manually before retrying.' }
        }
    }
    finally { if ($null -ne $Child) { $Child.Dispose() } }
}

function Invoke-ContextNativeOutput {
    param([string]$Application, [string[]]$Arguments, [int[]]$AcceptedExitCodes = @(0), [switch]$WithResult, [ValidateRange(1, 30000)][int]$TimeoutMilliseconds = 10000, [switch]$OwnInspection, [string]$InputText)
    $hasInput = $PSBoundParameters.ContainsKey('InputText')
    $inputBytes = if ($hasInput) { [Text.UTF8Encoding]::new($false, $true).GetBytes($InputText) } else { $null }
    if ($hasInput -and ($OwnInspection -or $inputBytes.Length -gt 1048576)) { throw 'Native inspection input is unsupported or exceeds its one-MiB limit; inspect the target manually.' }
    $start = [Diagnostics.ProcessStartInfo]::new($Application)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.RedirectStandardInput = $OwnInspection -or $hasInput
    $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false, $true)
    $start.Environment['GIT_NO_LAZY_FETCH'] = '1'
    $start.Environment['GIT_ALLOW_PROTOCOL'] = ''
    foreach ($argument in $Arguments) {
        $start.ArgumentList.Add($argument)
    }
    $owner = New-ContextInspectionOwner $start -OwnInspection:$OwnInspection
    $child = $null
    try {
        $child = [Diagnostics.Process]::Start($start)
        if ($OwnInspection) {
            if ($IsWindows) { [PortableDelivery.InspectionOwnership]::AttachWindowsProcess($owner.Job, $child.Handle) }
            $child.StandardInput.WriteLine('OWNED')
            $child.StandardInput.Close()
        }
        $inputWrite = if ($hasInput) { $child.StandardInput.BaseStream.WriteAsync($inputBytes, 0, $inputBytes.Length) } else { $null }
        $captured = Get-ContextBoundedOutput $child $TimeoutMilliseconds $inputWrite
        $diagnostic = $captured.Error
        if ($child.ExitCode -notin $AcceptedExitCodes) { throw "Native context inspection failed: $diagnostic" }
        if (-not [string]::IsNullOrWhiteSpace($diagnostic)) {
            throw "Native context inspection reported diagnostics; inspect the target manually: $diagnostic"
        }
        if ($WithResult) { return [pscustomobject]@{ ExitCode = $child.ExitCode; Output = $captured.Output } }
        return $captured.Output
    }
    finally { Close-ContextNativeOutput $child $owner -OwnInspection:$OwnInspection }
}

function Get-ContextRawPaths {
    param([string]$Root, [string[]]$ContextPaths)
    $visible = Invoke-ContextGit $root @('ls-files', '-z', '--cached', '--others', '--exclude-standard') -WithResult
    $instructionCandidates = @(':(glob)**/AGENTS.md', ':(glob)**/*.instructions.md', ':(glob)**/CLAUDE.md', '.github/copilot-instructions.md')
    $selected = @($ContextPaths | ForEach-Object { ':(literal)' + $_ })
    $ignored = Invoke-ContextGit $root (@('ls-files', '-z', '--others', '--ignored', '--exclude-standard', '--') + $instructionCandidates + $selected) -WithResult
    return ($visible.Output + $ignored.Output).Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
}

function Assert-ContextInventoryTypes {
    param([string]$Root, [string[]]$Paths)
    if ($IsWindows -or $Paths.Count -eq 0) { return }
    $stat = Get-Command stat -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $stat) { throw 'Unix stat metadata is unavailable; inspect inventory entries manually.' }
    $arguments = if ($IsMacOS) { @('-f', '%p', '--') } else { @('-c', '%f', '--') }
    $radix = if ($IsMacOS) { 8 } else { 16 }
    $arguments += @($Paths | ForEach-Object { [IO.Path]::Combine($Root, $_) })
    $modes = (Invoke-ContextNativeOutput $stat.Source $arguments).Split([char]10, [StringSplitOptions]::RemoveEmptyEntries)
    if ($modes.Count -ne $Paths.Count) { throw 'Inventory entry metadata is incomplete; inspect instructions manually.' }
    for ($index = 0; $index -lt $Paths.Count; $index++) {
        $mode = [Convert]::ToInt32($modes[$index].Trim(), $radix) -band 61440
        if ($mode -notin @(32768, 40960)) {
            throw "Non-regular inventory entries require manual instruction discovery: $($Paths[$index])"
        }
    }
}

function Get-ContextPaths {
    param([string]$Root, [string[]]$ContextPaths)
    $paths = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($relative in (Get-ContextRawPaths $root $ContextPaths)) {
        $fullPath = [IO.Path]::Combine($root, $relative)
        if ([IO.Directory]::Exists($fullPath)) { throw "Opaque directory inventory requires manual instruction discovery: $relative" }
        $entry = [IO.FileInfo]::new($fullPath)
        if ($null -ne $entry.LinkTarget -and $entry.ResolveLinkTarget($true).Exists) { throw "Live linked inventory entries require manual discovery: $relative" }
        if ([IO.File]::Exists($fullPath) -or $null -ne $entry.LinkTarget) { $null = $paths.Add($relative) }
    }
    $inventory = @($paths)
    Assert-ContextInventoryTypes $Root $inventory
    return $inventory
}

function Assert-ContextGitMetadataEntry {
    param([IO.DirectoryInfo]$Directory, [IO.FileSystemInfo]$Item)
    if ($null -ne $item.LinkTarget -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Linked Git metadata entries require manual inspection; refs, objects and index must belong to the selected repository.'
    }
    $metadataRelative = [IO.Path]::GetRelativePath($Directory.FullName, $item.FullName).Replace([IO.Path]::DirectorySeparatorChar, [char]'/')
    if ($metadataRelative.StartsWith('refs/', [StringComparison]::OrdinalIgnoreCase) -and $metadataRelative.EndsWith('.lock', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Git ref locks require manual recovery before inspection: $metadataRelative"
    }
    if ($metadataRelative.Equals('objects/info/alternates', [StringComparison]::OrdinalIgnoreCase) -or $metadataRelative.Equals('objects/info/http-alternates', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Alternate Git object stores require manual inspection; objects must belong to the selected repository.'
    }
    if ($metadataRelative.Equals('info/grafts', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Legacy Git grafts require manual inspection; synthetic ancestry can publish unrelated objects.'
    }
    if ($metadataRelative.Equals('shallow', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Shallow Git boundaries require manual inspection; ancestry cannot be attributed from a truncated view.'
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
    $replacements = Invoke-ContextGit $root @('for-each-ref', '--format=%(refname)', 'refs/replace/') -WithResult
    if (-not [string]::IsNullOrWhiteSpace($replacements.Output)) { throw 'Git replacement refs require manual inspection; ordinary review and publication can use different object views.' }
    $actualRoot = Get-ContextGitPath $root @('rev-parse', '--show-toplevel')
    $gitDirectory = Get-ContextGitPath $root @('rev-parse', '--absolute-git-dir')
    $commonDirectory = Get-ContextGitPath $root @('rev-parse', '--path-format=absolute', '--git-common-dir')
    if ($actualRoot -cne $expectedRoot -or $gitDirectory -cne [IO.Path]::Combine($expectedRoot, '.git')) {
        throw 'Git directory identity differs from the selected working copy; inspect this target manually.'
    }
    if ($commonDirectory -cne $gitDirectory) { throw 'Git common directory differs from the embedded metadata; inspect this target manually.' }
    return $actualRoot
}

function Read-ContextIndexBytes {
    param([psobject]$Buffer, [int]$Count)
    if ($Count -lt 0 -or $Count -gt $Buffer.Limit - $Buffer.Cursor) { throw 'Malformed Git index requires manual inspection.' }
    $value = [byte[]]::new($Count)
    [Array]::Copy($Buffer.Data, $Buffer.Cursor, $value, 0, $Count)
    $Buffer.Cursor += $Count
    return ,$value
}

function Read-ContextIndexNumber {
    param([psobject]$Buffer, [int]$Count = 4)
    return [Convert]::ToInt64([BitConverter]::ToString((Read-ContextIndexBytes $Buffer $Count)).Replace('-', ''), 16)
}

function Read-ContextIndexTerminated {
    param([psobject]$Buffer, [byte]$Terminator = 0)
    $end = [Array]::IndexOf($Buffer.Data, $Terminator, $Buffer.Cursor, $Buffer.Limit - $Buffer.Cursor)
    if ($end -lt 0) { throw 'Malformed Git index requires manual inspection.' }
    $value = Read-ContextIndexBytes $Buffer ($end - $Buffer.Cursor)
    $Buffer.Cursor++
    return ,$value
}

function Skip-ContextIndexEntry {
    param([psobject]$Buffer, [int]$Version, [int]$HashSize)
    $start = $Buffer.Cursor
    $null = Read-ContextIndexBytes $Buffer (40 + $HashSize)
    $flags = Read-ContextIndexNumber $Buffer 2
    if (($flags -band 16384) -ne 0) {
        if ($Version -eq 2) { throw 'Extended version-two Git index requires manual inspection.' }
        $null = Read-ContextIndexBytes $Buffer 2
    }
    if ($Version -eq 4) {
        do { $encoded = (Read-ContextIndexBytes $Buffer 1)[0] } while (($encoded -band 128) -ne 0)
    }
    $null = Read-ContextIndexTerminated $Buffer
    if ($Version -ne 4) { $null = Read-ContextIndexBytes $Buffer ((8 - (($Buffer.Cursor - $start) % 8)) % 8) }
}

function Read-ContextIndexFile {
    param([IO.FileStream]$Stream)
    $bytes = [byte[]]::new([int]$Stream.Length)
    $read = 0
    while ($read -lt $bytes.Length) {
        $count = $Stream.Read($bytes, $read, $bytes.Length - $read)
        if ($count -eq 0) { throw 'Git index ended during inspection; reconcile and retry.' }
        $read += $count
    }
    return ,$bytes
}

function Get-ContextCacheTree {
    param([string]$Root, [int]$HashSize)
    $path = [IO.Path]::Combine($Root, '.git', 'index')
    if (-not [IO.File]::Exists($path)) { return $null }
    $stream = [IO.FileStream]::new($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if (-not $stream.CanSeek -or $stream.Length -gt 1048576 -or $stream.Length -lt 12 + $HashSize) {
            throw 'Git index exceeds supported inspection bounds; inspect this target manually.'
        }
        $bytes = Read-ContextIndexFile $stream
    }
    finally { $stream.Dispose() }
    $buffer = [pscustomobject]@{ Data = $bytes; Cursor = 0; Limit = $bytes.Length - $HashSize }
    if ([Text.Encoding]::ASCII.GetString((Read-ContextIndexBytes $buffer 4)) -cne 'DIRC') { throw 'Unsupported Git index requires manual inspection.' }
    $version = Read-ContextIndexNumber $buffer
    if ($version -notin @(2, 3, 4)) { throw 'Unsupported Git index version requires manual inspection.' }
    $count = Read-ContextIndexNumber $buffer
    if ($count -gt $buffer.Limit / (43 + $HashSize)) { throw 'Malformed Git index requires manual inspection.' }
    for ($entry = 0; $entry -lt $count; $entry++) { Skip-ContextIndexEntry $buffer $version $HashSize }
    $tree = $null
    while ($buffer.Cursor -lt $buffer.Limit) {
        $signature = [Text.Encoding]::ASCII.GetString((Read-ContextIndexBytes $buffer 4))
        $size = Read-ContextIndexNumber $buffer
        $payload = Read-ContextIndexBytes $buffer $size
        if ($signature -cnotmatch '^[A-Z]') { throw 'Mandatory Git index extensions require manual inspection, including split indexes.' }
        if ($signature -ceq 'TREE') {
            if ($null -ne $tree) { throw 'Duplicate Git index cache trees require manual inspection.' }
            $tree = $payload
        }
    }
    return ,$tree
}

function Read-ContextCacheTreeNode {
    param([psobject]$Buffer, [int]$HashSize, [int]$Depth = 0)
    if ($Depth -gt 256) { throw 'Git cache tree exceeds inspection depth; inspect this target manually.' }
    $encoding = [Text.UTF8Encoding]::new($false, $true)
    $name = $encoding.GetString((Read-ContextIndexTerminated $Buffer))
    $header = $encoding.GetString((Read-ContextIndexTerminated $Buffer 10))
    if ($name.Contains('/') -or $header -cnotmatch '^(-?\d+) (\d+)$') { throw 'Malformed Git index cache tree requires manual inspection.' }
    $count = [int]::Parse($Matches[1])
    $childCount = [int]::Parse($Matches[2])
    $oid = if ($count -ge 0) { [BitConverter]::ToString((Read-ContextIndexBytes $Buffer $HashSize)).Replace('-', '').ToLowerInvariant() } else { $null }
    $children = @(for ($child = 0; $child -lt $childCount; $child++) { Read-ContextCacheTreeNode $Buffer $HashSize ($Depth + 1) })
    return [pscustomobject]@{ Name = $name; EntryCount = $count; Oid = $oid; Children = $children }
}

function Get-ContextStagedTree {
    param([string[]]$Index, [int]$HashSize)
    $files = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $counts = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
    $counts.Add('', 0)
    foreach ($record in $Index) {
        if ($record -cnotmatch '(?s)^(100644|100755|120000) ([a-f0-9]+) 0\t(.+)$' -or $Matches[2].Length -ne 2 * $HashSize) {
            throw 'Unsupported staged entries require manual cache-tree inspection.'
        }
        $path = $Matches[3]
        $files.Add($path, $Matches[1] + ' blob ' + $Matches[2] + "`t" + $path)
        $counts['']++
        $parts = $path.Split('/')
        $parent = ''
        for ($part = 0; $part -lt $parts.Length - 1; $part++) {
            $parent = if ($part -eq 0) { $parts[$part] } else { $parent + '/' + $parts[$part] }
            if (-not $counts.ContainsKey($parent)) { $counts.Add($parent, 0) }
            $counts[$parent]++
        }
    }
    return [pscustomobject]@{ Files = $files; Counts = $counts }
}

function Get-ContextTreeRecord {
    param([string]$Record, [string]$Prefix, [psobject]$Staged)
    if ($Record -cnotmatch '(?s)^(\d{6}) (blob|tree) ([a-f0-9]+)\t(.+)$') { throw 'Unsupported Git tree requires manual inspection.' }
    $path = if ($Prefix.Length -eq 0) { $Matches[4] } else { $Prefix + '/' + $Matches[4] }
    $value = $Matches[1] + ' ' + $Matches[2] + ' ' + $Matches[3] + "`t" + $path
    $entry = [pscustomobject]@{ Path = $path; Oid = $Matches[3]; IsDirectory = $Matches[1] -ceq '040000' -and $Matches[2] -ceq 'tree' }
    if ($entry.IsDirectory) {
        if (-not $Staged.Counts.ContainsKey($path)) { throw 'Cached Git index tree disagrees with staged entries.' }
    } elseif (-not $Staged.Files.ContainsKey($path) -or $Staged.Files[$path] -cne $value) {
        throw 'Cached Git index tree disagrees with staged entries.'
    }
    return $entry
}

function Get-ContextVerifiedTreeObjects {
    param([string]$Root, [string]$Oid, [string]$Prefix, [psobject]$Staged)
    $output = (Invoke-ContextGit $Root @('ls-tree', '-r', '-t', '-z', '--full-tree', $Oid) -WithResult).Output
    $trees = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $trees.Add($Prefix, $Oid)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $files = 0
    foreach ($record in $output.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
        $entry = Get-ContextTreeRecord $record $Prefix $Staged
        if (-not $seen.Add($entry.Path)) { throw 'Cached Git index tree disagrees with staged entries.' }
        if ($entry.IsDirectory) { $trees.Add($entry.Path, $entry.Oid) } else { $files++ }
    }
    $subtreePrefix = if ($Prefix.Length -eq 0) { '' } else { $Prefix + '/' }
    $directories = @($Staged.Counts.get_Keys() | Where-Object { $_ -cne $Prefix -and $_.StartsWith($subtreePrefix, [StringComparison]::Ordinal) }).Count
    if ($files -ne $Staged.Counts[$Prefix] -or $trees.get_Count() -ne $directories + 1) { throw 'Cached Git index tree disagrees with staged entries.' }
    return ,$trees
}

function Assert-ContextCachedTreeNode {
    param([string]$Root, [psobject]$Node, [string]$Prefix, [psobject]$Staged, [Collections.Generic.Dictionary[string, string]]$Trees)
    if ($Node.EntryCount -ge 0) {
        if (-not $Staged.Counts.ContainsKey($Prefix) -or $Node.EntryCount -ne $Staged.Counts[$Prefix]) { throw 'Cached Git index tree disagrees with staged entries.' }
        if ($null -eq $Trees) { $Trees = Get-ContextVerifiedTreeObjects $Root $Node.Oid $Prefix $Staged }
        if (-not $Trees.ContainsKey($Prefix) -or $Trees[$Prefix] -cne $Node.Oid) { throw 'Cached Git index tree disagrees with staged entries.' }
    }
    foreach ($child in $Node.Children) {
        if ($child.Name.Length -eq 0) { throw 'Malformed Git index cache tree requires manual inspection.' }
        $childPrefix = if ($Prefix.Length -eq 0) { $child.Name } else { $Prefix + '/' + $child.Name }
        Assert-ContextCachedTreeNode $Root $child $childPrefix $Staged $Trees
    }
}

function Assert-ContextCachedTree {
    param([string]$Root, [string[]]$Index)
    $format = [string](Invoke-ContextGit $Root @('rev-parse', '--show-object-format'))
    $hashSize = switch ($format) { 'sha1' { 20 } 'sha256' { 32 } default { throw 'Unsupported Git object format requires manual inspection.' } }
    $tree = Get-ContextCacheTree $Root $hashSize
    if ($null -eq $tree) { return }
    $buffer = [pscustomobject]@{ Data = $tree; Cursor = 0; Limit = $tree.Length }
    $node = Read-ContextCacheTreeNode $buffer $hashSize
    if ($node.Name.Length -ne 0 -or $buffer.Cursor -ne $buffer.Limit) { throw 'Malformed Git index cache tree requires manual inspection.' }
    $staged = Get-ContextStagedTree $Index $hashSize
    Assert-ContextCachedTreeNode $Root $node '' $staged $null
}

function Assert-ContextOperationState {
    param([string]$Root)
    foreach ($name in @('MERGE_HEAD', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'REBASE_HEAD', 'rebase-apply', 'rebase-merge', 'sequencer', 'BISECT_START', 'index.lock', 'HEAD.lock', 'packed-refs.lock')) {
        $path = [IO.Path]::Combine($Root, '.git', $name)
        $entry = [IO.FileInfo]::new($path)
        if ($entry.Exists -or [IO.Directory]::Exists($path) -or $null -ne $entry.LinkTarget) {
            throw "In-progress Git operations require manual recovery before inspection: $name"
        }
    }
}

function ConvertTo-ContextGitInputPath {
    param([string]$Path)
    $quoted = [Text.StringBuilder]::new('"')
    foreach ($character in $Path.ToCharArray()) {
        if ($character -eq [char]34 -or $character -eq [char]92) { $null = $quoted.Append([char]92).Append($character) }
        elseif ([int]$character -lt 32 -or [int]$character -eq 127) { $null = $quoted.Append([char]92).Append([Convert]::ToString([int]$character, 8).PadLeft(3, '0')) }
        else { $null = $quoted.Append($character) }
    }
    return $quoted.Append('"').ToString()
}

function Get-ContextTrackedFile {
    param([string]$Root, [string]$Relative)
    $fullPath = [IO.Path]::GetFullPath([IO.Path]::Combine($Root, $Relative))
    $prefix = $Root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($prefix, [StringComparison]::Ordinal)) { throw "Tracked path escapes the target repository: $Relative" }
    $item = [IO.FileInfo]::new($fullPath)
    if ($null -ne $item.LinkTarget -or ($item.Exists -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) { throw "Linked tracked content requires manual inspection: $Relative" }
    $ancestor = $item.Directory
    while ($null -ne $ancestor -and $ancestor.FullName -cne $Root) {
        if ($ancestor.Exists -and ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked tracked content requires manual inspection: $Relative" }
        $ancestor = $ancestor.Parent
    }
    if ([IO.Directory]::Exists($item.FullName)) { throw "Non-regular tracked content requires manual inspection: $Relative" }
    if ($item.Exists -and ($item.Attributes -band [IO.FileAttributes]::Device) -ne 0) { throw "Non-regular tracked content requires manual inspection: $Relative" }
    return $item
}

function Get-ContextTrackedContent {
    param([string]$Root, [string[]]$Index)
    $entries = @(foreach ($record in $Index) {
        if ($record -cnotmatch '(?s)^(100644|100755) ([a-f0-9]+) 0\t(.+)$') { continue }
        $expected = $Matches[2]
        $relative = $Matches[3]
        $item = Get-ContextTrackedFile $Root $relative
        [pscustomobject]@{ Path = $relative; Expected = $expected; Actual = $null; Present = $item.Exists }
    })
    $present = @($entries | Where-Object { $_.Present })
    if ($present.Count -gt 0) {
        Assert-ContextInventoryTypes $Root @($present.Path)
        $inputPaths = (@($present | ForEach-Object { ConvertTo-ContextGitInputPath $_.Path }) -join "`n") + "`n"
        $hashes = @(Invoke-ContextGit $Root @('hash-object', '--stdin-paths') -InputText $inputPaths)
        if ($hashes.Count -ne $present.Count) { throw 'Tracked content identities are incomplete; inspect the target manually.' }
        for ($entry = 0; $entry -lt $present.Count; $entry++) {
            if ($hashes[$entry] -cnotmatch '^[a-f0-9]+$' -or $hashes[$entry].Length -ne $present[$entry].Expected.Length) { throw 'Tracked content identity is malformed; inspect the target manually.' }
            $present[$entry].Actual = $hashes[$entry]
        }
    }
    return $entries
}

function Get-ContextObservation {
    param([string]$Root, [string[]]$ContextPaths)
    if ((Get-ContextGitRoot $root) -cne $root) { throw 'Repository root changed during context inspection.' }
    Assert-ContextOperationState $root
    $headResult = Invoke-ContextGit $root @('rev-parse', '--verify', '--quiet', 'HEAD') -AcceptedExitCodes @(0, 1) -WithResult
    $head = if ($headResult.ExitCode -eq 0) { $headResult.Output.Trim() } else { $null }
    $branch = [string](Invoke-ContextGit $root @('branch', '--show-current'))
    if ($null -eq $head -and [string]::IsNullOrWhiteSpace($branch)) { throw 'Missing detached HEAD requires manual inspection.' }
    $index = (Invoke-ContextGit $root @('ls-files', '--stage', '-z') -WithResult).Output.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
    if (@($index | Where-Object { $_ -match '^160000 ' }).Count -gt 0) {
        throw 'Submodule entries require manual inspection; status can execute submodule-local commands.'
    }
    if (@($index | Where-Object { $_ -match '^120000 ' }).Count -gt 0) {
        $symlinks = Invoke-ContextGit $root @('config', '--type=bool', '--get', 'core.symlinks') -AcceptedExitCodes @(0, 1) -WithResult
        if ($symlinks.ExitCode -eq 0 -and $symlinks.Output.Trim() -eq 'false') {
            throw 'Tracked symlinks with core.symlinks=false require manual inspection; plain-file substitutions can look clean.'
        }
        throw 'Tracked symlink content requires manual inspection; cached stat fields cannot establish link identity.'
    }
    $flags = @(Invoke-ContextGit $root @('ls-files', '-v'))
    if (@($flags | Where-Object { $_ -cmatch '^[a-zS] ' }).Count -gt 0) {
        throw 'Hidden index flags require manual inspection: assume-unchanged or skip-worktree can conceal changes.'
    }
    $filters = Invoke-ContextGit $root @('config', '--name-only', '--get-regexp', '^filter\..*\.(clean|process)$') -AcceptedExitCodes @(0, 1) -WithResult
    if ($filters.ExitCode -eq 0) {
        throw 'Configured clean/process filters require manual inspection; status may execute repository-controlled commands.'
    }
    Assert-ContextCachedTree $Root $index
    $content = @(Get-ContextTrackedContent $Root $index)
    $status = @(Invoke-ContextGit $root @('status', '--porcelain=v1', '--untracked-files=all'))
    $selected = @(foreach ($relative in $ContextPaths) { Get-ContextInput $root $relative })
    $paths = @(Get-ContextPaths $root $ContextPaths)
    return [pscustomobject]@{
        Head = $head
        Branch = $branch
        Status = $status
        Paths = $paths
        Index = $index
        TrackedContent = $content
        SelectedInputs = @($selected)
    }
}

try {
    if ($WaitForInspectionOwner) {
        if ($IsLinux) {
            Enable-ContextOwnershipType
            if ([PortableDelivery.InspectionOwnership]::CreateUnixSession() -ne $PID) { throw 'Inspection process group identity differs from its owner.' }
        }
        if ([Console]::In.ReadLine() -cne 'OWNED') { throw 'Inspection ownership handshake is absent.' }
    }
    if ([string]::IsNullOrEmpty($InspectionContext) -and [string]::IsNullOrEmpty($MetadataPath) -and [string]::IsNullOrEmpty($HashPath)) {
        $inspectionRoot = if ([IO.Path]::IsPathRooted($RepositoryRoot)) { [IO.Path]::GetFullPath($RepositoryRoot) }
        else { [IO.Path]::GetFullPath([IO.Path]::Combine((Get-Location).Path, $RepositoryRoot)) }
        $shell = [IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
        $contextJson = ConvertTo-Json -InputObject @($ContextPath) -Compress
        $output = Invoke-ContextNativeOutput $shell @('-NoProfile', '-File', $PSCommandPath, '-RepositoryRoot', $inspectionRoot, '-InspectionContext', $contextJson, '-WaitForInspectionOwner') -TimeoutMilliseconds 30000 -OwnInspection
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
    foreach ($selector in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_NAMESPACE', 'GIT_CONFIG', 'GIT_REPLACE_REF_BASE', 'GIT_SHALLOW_FILE', 'GIT_GRAFT_FILE', 'GIT_LITERAL_PATHSPECS', 'GIT_GLOB_PATHSPECS', 'GIT_NOGLOB_PATHSPECS', 'GIT_ICASE_PATHSPECS', 'GIT_ATTR_SOURCE')) {
        if ($null -ne [Environment]::GetEnvironmentVariable($selector)) {
            throw "Ambient Git override $selector prevents reliable target inspection; use a clean process or manual inspection."
        }
    }
    $gitLimiter = if ($IsLinux) { Get-Command prlimit -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 } else { $null }
    if ($IsLinux -and $null -eq $gitLimiter) { throw 'Git memory limiting requires Linux prlimit; inspect the target manually.' }
    if ($IsLinux) { $gitLimiter = $gitLimiter.Source }
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
        Dirty = $after.Status.Count -gt 0 -or @($after.TrackedContent | Where-Object { $_.Actual -cne $_.Expected }).Count -gt 0
        Paths = $after.Paths
        SelectedInputs = @($after.SelectedInputs)
        InstructionSelectionRequired = $true
    } | ConvertTo-Json -Depth 8
    exit 0
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
