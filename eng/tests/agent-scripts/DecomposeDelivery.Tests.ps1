#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$contextSuiteSkipReason = $null
if ($PSVersionTable.PSVersion -lt [version]'7.4') { $contextSuiteSkipReason = 'Requires PowerShell 7.4 or later.' }
elseif (-not ($IsWindows -or $IsLinux)) { $contextSuiteSkipReason = 'Supports Windows and Linux; use manual inspection on other platforms.' }
elseif ($IsLinux) {
    $missing = @(@('stat', 'prlimit') | Where-Object { $null -eq (Get-Command $_ -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1) })
    if ($missing.Count -gt 0) { $contextSuiteSkipReason = "Optional helper prerequisites are unavailable: $($missing -join ', ')." }
}
if ($null -ne $contextSuiteSkipReason) { Write-Host "Skipping portable delivery context snapshot suite: $contextSuiteSkipReason" }

BeforeAll {
    $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
    $package = Join-Path $repositoryRoot '.agents/skills/decompose-and-deliver'
    $snapshotScript = Join-Path $package 'scripts/snapshot-context.ps1'
    $shell = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
    $probeName = [guid]::NewGuid().ToString('N') + '-case-probe'
    $probePath = [IO.Path]::Combine($TestDrive, $probeName)
    [IO.File]::WriteAllText($probePath, 'case probe')
    $caseSensitiveFileSystem = -not [IO.File]::Exists([IO.Path]::Combine($TestDrive, $probeName.ToUpperInvariant()))
    [IO.File]::Delete($probePath)

    function Invoke-FixtureGit {
        param([string[]]$Arguments)
        & git -c "safe.directory=$($fixture.Replace('\', '/'))" -C $fixture @Arguments | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Fixture Git operation failed.' }
    }

    function Get-FixtureSnapshot {
        param([string[]]$Paths = @('AGENTS.md', 'tools/verify.sh'), [string]$Prelude = '', [string]$Root = $fixture)
        $quotedPaths = @($Paths | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ','
        $configuration = "`$env:GIT_CONFIG_NOSYSTEM = '1'; `$env:GIT_CONFIG_GLOBAL = '" +
            (Join-Path $fixture 'missing-global').Replace("'", "''") + "'; "
        $command = $configuration + "`$snapshotInvocationPath = '" + $snapshotScript.Replace("'", "''") + "'; " + $Prelude + "& `$snapshotInvocationPath -RepositoryRoot '" +
            $Root.Replace("'", "''") + "' -ContextPath @(" + $quotedPaths + ')'
        $result = & $shell -NoProfile -Command $command 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw $result }
        return $result | ConvertFrom-Json
    }

    function Get-SnapshotMutationPrelude {
        param([string]$Mutation, [string]$Marker)
        $realGit = (Get-Command git -CommandType Application | Select-Object -First 1).Source.Replace("'", "''")
        $markerPath = $Marker.Replace("'", "''")
        $injected = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-snapshot.ps1')
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$after = Get-ContextObservation $root $ContextPath'
        if (-not $source.Contains($boundary)) { throw 'Snapshot observation boundary was not found.' }
        $definitions = @"
`$global:snapshotRealGit = '$realGit'
`$global:snapshotMutation = { Set-Content -LiteralPath '$markerPath' -Value 'mutated'; $Mutation }
"@ + "`n"
        $workerBoundary = '$gitApplication = (Get-Command git -CommandType Application'
        if (-not $source.Contains($workerBoundary)) { throw 'Snapshot worker boundary was not found.' }
        $source = $source.Replace($workerBoundary, $definitions + $workerBoundary)
        [IO.File]::WriteAllText($injected, $source.Replace($boundary, "& `$global:snapshotMutation`n    " + $boundary))
        return "`$snapshotInvocationPath = '$($injected.Replace("'", "''"))'`n"
    }

    function Invoke-BoundedSnapshot {
        param([string]$Root = $fixture, [string]$Path = 'AGENTS.md', [switch]$Unprivileged, [string]$ScriptPath = $snapshotScript)
        $application = if ($Unprivileged) { (Get-Command setpriv -CommandType Application | Select-Object -First 1).Source } else { $shell }
        $start = [Diagnostics.ProcessStartInfo]::new($application)
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        if ($Unprivileged) {
            foreach ($argument in @('--reuid=65534', '--regid=65534', '--clear-groups', $shell)) { $start.ArgumentList.Add($argument) }
        }
        foreach ($argument in @('-NoProfile', '-File', $ScriptPath, '-RepositoryRoot', $Root, '-ContextPath', $Path)) { $start.ArgumentList.Add($argument) }
        $start.Environment['GIT_CONFIG_NOSYSTEM'] = '1'
        $start.Environment['GIT_CONFIG_GLOBAL'] = Join-Path $fixture 'missing-global'
        $start.Environment['XDG_CONFIG_HOME'] = Join-Path $fixture 'missing-xdg'
        $child = [Diagnostics.Process]::Start($start)
        try {
            $output = $child.StandardOutput.ReadToEndAsync()
            $errorOutput = $child.StandardError.ReadToEndAsync()
            if (-not $child.WaitForExit(45000)) { throw 'Snapshot exceeded its outer fixture timeout.' }
            return [pscustomobject]@{ ExitCode = $child.ExitCode; Output = $output.GetAwaiter().GetResult(); Error = $errorOutput.GetAwaiter().GetResult() }
        }
        finally {
            if (-not $child.HasExited) { $child.Kill($true) }
            $child.Dispose()
        }
    }

    function Set-FixtureCachedCTime {
        param([string]$Relative)
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $indexPath = Join-Path $fixture '.git/index'
        $bytes = [IO.File]::ReadAllBytes($indexPath)
        # The fixture uses SHA-1 index v2: https://git-scm.com/docs/gitformat-index.
        [Convert]::ToInt32([BitConverter]::ToString($bytes, 4, 4).Replace('-', ''), 16) | Should -Be 2
        $count = [Convert]::ToInt32([BitConverter]::ToString($bytes, 8, 4).Replace('-', ''), 16)
        $cursor = 12
        for ($entry = 0; $entry -lt $count; $entry++) {
            $pathStart = $cursor + 62
            $end = $pathStart
            while ($bytes[$end] -ne 0) { $end++ }
            $name = [Text.Encoding]::UTF8.GetString($bytes, $pathStart, $end - $pathStart)
            if ($name -ceq $Relative) {
                [Array]::Clear($bytes, $cursor, 4)
                $checksum = [Security.Cryptography.SHA1]::HashData([byte[]]$bytes[0..($bytes.Length - 21)])
                [Array]::Copy($checksum, 0, $bytes, $bytes.Length - 20, 20)
                [IO.File]::WriteAllBytes($indexPath, $bytes)
                return
            }
            $cursor += [int]([Math]::Ceiling(($end + 1 - $cursor) / 8.0) * 8)
        }
        throw 'Fixture index entry was not found.'
    }

    function Set-FixtureStagedObject {
        param([string]$Relative, [string]$Object)
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $indexPath = Join-Path $fixture '.git/index'
        $bytes = [IO.File]::ReadAllBytes($indexPath)
        $count = [Convert]::ToInt32([BitConverter]::ToString($bytes, 8, 4).Replace('-', ''), 16)
        $cursor = 12
        for ($entry = 0; $entry -lt $count; $entry++) {
            $pathStart = $cursor + 62
            $end = $pathStart
            while ($bytes[$end] -ne 0) { $end++ }
            if ([Text.Encoding]::UTF8.GetString($bytes, $pathStart, $end - $pathStart) -ceq $Relative) {
                # Preserve genuine Git-written stat fields while substituting the staged blob identity.
                [Array]::Copy([Convert]::FromHexString($Object), 0, $bytes, $cursor + 40, 20)
                $checksum = [Security.Cryptography.SHA1]::HashData([byte[]]$bytes[0..($bytes.Length - 21)])
                [Array]::Copy($checksum, 0, $bytes, $bytes.Length - 20, 20)
                [IO.File]::WriteAllBytes($indexPath, $bytes)
                [IO.File]::SetLastWriteTimeUtc($indexPath, [datetime]::UtcNow.AddMinutes(1))
                return
            }
            $cursor += [int]([Math]::Ceiling(($end + 1 - $cursor) / 8.0) * 8)
        }
        throw 'Fixture index entry was not found.'
    }

    function Test-FixtureLiveProcess {
        param([int]$ProcessId)
        if ($IsLinux) {
            $statPath = '/proc/' + $ProcessId + '/stat'
            if (-not [IO.File]::Exists($statPath)) { return $false }
            $stat = [IO.File]::ReadAllText($statPath)
            return $stat.Substring($stat.LastIndexOf(')') + 2).Split(' ')[0] -notin @('Z', 'X')
        }
        $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        return $null -ne $process -and -not $process.HasExited
    }

    function New-FixtureTree {
        param([string[]]$Entries)
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Command git -CommandType Application | Select-Object -First 1).Source)
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardInput = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @('-C', $fixture, 'mktree')) { $start.ArgumentList.Add($argument) }
        $child = [Diagnostics.Process]::Start($start)
        try {
            $output = $child.StandardOutput.ReadToEndAsync()
            $errorOutput = $child.StandardError.ReadToEndAsync()
            # Git's input records require LF, including when the fixture runs on Windows.
            $child.StandardInput.NewLine = "`n"
            foreach ($entry in $Entries) { $child.StandardInput.WriteLine($entry) }
            $child.StandardInput.Close()
            if (-not $child.WaitForExit(45000)) { throw 'Fixture tree creation timed out.' }
            if ($child.ExitCode -ne 0) { throw $errorOutput.GetAwaiter().GetResult() }
            return $output.GetAwaiter().GetResult().Trim()
        }
        finally {
            if (-not $child.HasExited) { $child.Kill($true) }
            $child.Dispose()
        }
    }

    function Set-FixtureCachedTree {
        param([string]$Original, [string]$Replacement)
        $indexPath = Join-Path $fixture '.git/index'
        $bytes = [IO.File]::ReadAllBytes($indexPath)
        $hex = [Convert]::ToHexString($bytes)
        $offset = $hex.IndexOf($Original.ToUpperInvariant(), [StringComparison]::Ordinal)
        $offset | Should -BeGreaterThan 0
        $hex.IndexOf($Original.ToUpperInvariant(), $offset + 1, [StringComparison]::Ordinal) | Should -Be -1
        ($offset % 2) | Should -Be 0
        [Array]::Copy([Convert]::FromHexString($Replacement), 0, $bytes, $offset / 2, 20)
        $checksum = [Security.Cryptography.SHA1]::HashData([byte[]]$bytes[0..($bytes.Length - 21)])
        [Array]::Copy($checksum, 0, $bytes, $bytes.Length - 20, 20)
        [IO.File]::WriteAllBytes($indexPath, $bytes)
    }
}

Describe 'Portable delivery context snapshots' -Skip:($null -ne $contextSuiteSkipReason) {
    BeforeEach {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $fixture 'tools'),(Join-Path $fixture 'packages/widget') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $fixture 'AGENTS.md') -Value '# Target instructions: bash tools/verify.sh; branch release/trunk'
        Set-Content -LiteralPath (Join-Path $fixture 'tools/verify.sh') -Value 'test -f packages/widget/model.txt'
        Set-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'baseline'
        Invoke-FixtureGit @('init', '--object-format=sha1', '-b', 'release/trunk')
        Invoke-FixtureGit @('config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'baseline')
    }

    It 'uses the explicit target, its branch, selected instructions and current layout' {
        $snapshot = Get-FixtureSnapshot
        $snapshot.RepositoryRoot | Should -BeExactly ([IO.Path]::GetFullPath($fixture))
        $snapshot.Branch | Should -BeExactly 'release/trunk'
        $snapshot.Paths | Should -Contain 'packages/widget/model.txt'
        $snapshot.SelectedInputs.Path | Should -Contain 'tools/verify.sh'
        $snapshot.InstructionSelectionRequired | Should -BeTrue
        $snapshot.Dirty | Should -BeFalse
    }

    It 'represents an unborn branch and then discovers its initial commit' {
        $newRoot = Join-Path $TestDrive 'greenfield'
        $null = [IO.Directory]::CreateDirectory($newRoot)
        [IO.File]::WriteAllText((Join-Path $newRoot 'AGENTS.md'), 'Greenfield instructions')
        Invoke-FixtureGit @('-C', $newRoot, 'init', '-b', 'greenfield')
        $before = Get-FixtureSnapshot -Root $newRoot -Paths @('AGENTS.md')
        $before.Head | Should -BeNullOrEmpty
        $before.Branch | Should -BeExactly 'greenfield'
        $before.Paths | Should -Contain 'AGENTS.md'
        $before.Dirty | Should -BeTrue
        Invoke-FixtureGit @('-C', $newRoot, 'add', '.')
        Invoke-FixtureGit @('-C', $newRoot, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'initial commit')
        $after = Get-FixtureSnapshot -Root $newRoot -Paths @('AGENTS.md')
        $after.Head | Should -Match '^[a-f0-9]{40}$'
        $after.Dirty | Should -BeFalse
    }

    It 'resolves a nested target independently of the current directory and package directory' {
        Push-Location (Join-Path $fixture 'packages/widget')
        try {
            $output = Get-FixtureSnapshot -Root . -Paths @('AGENTS.md')
            $output.RepositoryRoot | Should -BeExactly ([IO.Path]::GetFullPath($fixture))
        }
        finally { Pop-Location }
    }

    It 'normalizes trailing root separators while preserving file identity' {
        $snapshot = Get-FixtureSnapshot -Root ($fixture + [IO.Path]::DirectorySeparatorChar)
        $snapshot.RepositoryRoot | Should -BeExactly $fixture
        $snapshot.SelectedInputs[0].Path | Should -BeExactly 'AGENTS.md'
        $nested = Get-FixtureSnapshot -Root ((Join-Path $fixture 'packages/widget') + [IO.Path]::DirectorySeparatorChar)
        $nested.RepositoryRoot | Should -BeExactly $fixture
    }

    It 'changes input identity after uncommitted instruction edits and discovers a changed layout' {
        $before = Get-FixtureSnapshot
        Add-Content -LiteralPath (Join-Path $fixture 'AGENTS.md') -Value 'New decision: validate moved layout.'
        Move-Item -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Destination (Join-Path $fixture 'tools/model.txt')
        $after = Get-FixtureSnapshot
        $after.SelectedInputs[0].Sha256 | Should -Not -BeExactly $before.SelectedInputs[0].Sha256
        $after.Paths | Should -Contain 'tools/model.txt'
        $after.Paths | Should -Not -Contain 'packages/widget/model.txt'
        $after.Dirty | Should -BeTrue
    }

    It 'excludes unstaged deleted files from the current inventory' {
        Remove-Item -LiteralPath (Join-Path $fixture 'packages/widget/model.txt')
        $snapshot = Get-FixtureSnapshot
        $snapshot.Paths | Should -Not -Contain 'packages/widget/model.txt'
        $snapshot.Dirty | Should -BeTrue
    }

    It 'includes ignored instruction candidates and explicitly selected context' {
        [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), "packages/widget/AGENTS.md`n.github/instructions/`ntools/private-policy.txt`n")
        Invoke-FixtureGit @('add', '.gitignore')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'ignored guidance setup')
        $null = [IO.Directory]::CreateDirectory((Join-Path $fixture '.github/instructions'))
        [IO.File]::WriteAllText((Join-Path $fixture 'packages/widget/AGENTS.md'), 'Ignored scoped guidance')
        [IO.File]::WriteAllText((Join-Path $fixture '.github/instructions/hidden.instructions.md'), 'Ignored global guidance')
        [IO.File]::WriteAllText((Join-Path $fixture 'tools/private-policy.txt'), 'Selected private guidance')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        $snapshot = Get-FixtureSnapshot -Paths @('AGENTS.md', 'tools/private-policy.txt')
        $snapshot.Dirty | Should -BeFalse
        $snapshot.Paths | Should -Contain 'packages/widget/AGENTS.md'
        $snapshot.Paths | Should -Contain '.github/instructions/hidden.instructions.md'
        $snapshot.Paths | Should -Contain 'tools/private-policy.txt'
        $snapshot.SelectedInputs.Path | Should -Contain 'tools/private-policy.txt'
    }

    It 'rejects private Git excludes that conceal an automatically imported build input' {
        $excludePath = Join-Path $fixture '.git/info/exclude'
        [IO.File]::WriteAllText($excludePath, "Directory.Build.targets`n")
        [IO.File]::WriteAllText((Join-Path $fixture 'Directory.Build.targets'), '<Project><Target Name="OwnedFixtureTarget" /></Project>')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        @(& git -C $fixture ls-files --cached --others --exclude-standard) | Should -Not -Contain 'Directory.Build.targets'
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $excludeHash = (Get-FileHash -LiteralPath $excludePath).Hash
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Private Git exclude patterns require manual inspection'
        $result.Output | Should -BeNullOrEmpty
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        (Get-FileHash -LiteralPath $excludePath).Hash | Should -BeExactly $excludeHash
    }

    It 'accepts comment-only private Git excludes with CRLF line endings' {
        [IO.File]::WriteAllText((Join-Path $fixture '.git/info/exclude'), "# Local comments`r`n`r`n# No active patterns`r`n")
        (Get-FixtureSnapshot).Dirty | Should -BeFalse
    }

    It 'bounds oversized private Git excludes before parsing them' {
        [IO.File]::WriteAllText((Join-Path $fixture '.git/info/exclude'), '#' + ('x' * 65536))
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Git exclude inspection exceeded its bounds'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'rejects a non-regular private Git excludes file without blocking' -Skip:(-not $IsLinux) {
        $excludePath = Join-Path $fixture '.git/info/exclude'
        [IO.File]::Delete($excludePath)
        & mkfifo -- $excludePath
        $LASTEXITCODE | Should -Be 0
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Non-regular context files require manual inspection'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'reveals build inputs hidden by a configured Git excludes file without changing configuration' {
        $excludePath = Join-Path $fixture '.git/configured-ignore'
        [IO.File]::WriteAllText($excludePath, "Directory.Build.targets`n")
        Invoke-FixtureGit @('config', 'core.excludesFile', $excludePath)
        [IO.File]::WriteAllText((Join-Path $fixture 'Directory.Build.targets'), '<Project />')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        $configHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/config')).Hash
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $snapshot = Get-FixtureSnapshot
        $snapshot.Dirty | Should -BeTrue
        $snapshot.Paths | Should -Contain 'Directory.Build.targets'
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/config')).Hash | Should -BeExactly $configHash
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
    }

    It 'rejects ignored embedded repositories that hide scoped guidance behind a directory entry' {
        [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), "nested/`n")
        Invoke-FixtureGit @('add', '.gitignore')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'opaque ignored directory')
        $nested = Join-Path $fixture 'nested'
        [IO.Directory]::CreateDirectory($nested) | Out-Null
        Invoke-FixtureGit @('-C', $nested, 'init', '-b', 'nested')
        [IO.File]::WriteAllText((Join-Path $nested 'AGENTS.md'), 'Scoped instructions inside an ignored embedded repository')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        $opaque = @(& git -C $fixture ls-files --others --ignored --exclude-standard)
        @($opaque | Where-Object { $_ -ceq 'nested/' }).Count | Should -Be 1
        @($opaque | Where-Object { $_ -ceq 'nested/AGENTS.md' }).Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Opaque directory inventory requires manual instruction discovery*'
    }

    It 'bounds stalled worktree probes during inventory filtering' {
        $copy = Join-Path $TestDrive 'inventory-stall-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = 'if ([IO.Directory]::Exists($fullPath)) { throw "Opaque directory inventory requires manual instruction discovery: $relative" }'
        $source.Contains($boundary) | Should -BeTrue
        # Deterministically simulate a blocked filesystem probe; no network or FUSE setup is implied.
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, 'while ($true) { }; ' + $boundary))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context inspection timed out'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'caps captured <Stream> before processing oversized inventory output' -ForEach @(
        @{ Stream = 'Out' }, @{ Stream = 'Error' }
    ) {
        $copy = Join-Path $TestDrive ('output-cap-' + $Stream + '-snapshot.ps1')
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$start = [Diagnostics.ProcessStartInfo]::new($Application)'
        $source.Contains($boundary) | Should -BeTrue
        $emitter = '[Console]::' + $Stream + '.Write((''x'' * 2097152))'
        $replacement = @'
if ($Arguments -contains 'ls-files') {
        $Application = [IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
        $Arguments = @('-NoProfile', '-Command', '__EMITTER__')
    }

'@
        # Inject only the command stream; this does not claim a millions-of-paths index fixture.
        $replacement = $replacement.Replace('__EMITTER__', $emitter.Replace("'", "''"))
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, $replacement + $boundary))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context output exceeded its one-MiB stream limit'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'bounds stalled selected-path preflight probes before hashing' {
        $copy = Join-Path $TestDrive 'preflight-stall-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = 'if ([IO.Directory]::Exists($fullPath)) { throw "Context path is not a file: $relative" }'
        $source.Contains($boundary) | Should -BeTrue
        # Simulate a blocked preflight probe before the separately bounded hash child starts.
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, 'while ($true) { }; ' + $boundary))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context inspection timed out'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'confirms owned descendants terminate after <Failure> while preserving an unrelated process' -ForEach @(
        @{ Failure = 'root exit' }, @{ Failure = 'timeout' }, @{ Failure = 'output cap' }
    ) {
        $marker = Join-Path $TestDrive ('descendant-' + $Failure + '.pid')
        $descendant = Join-Path $TestDrive ('descendant-' + $Failure + '.ps1')
        [IO.File]::WriteAllText($descendant, "[IO.File]::WriteAllText('$($marker.Replace("'", "''"))', [string]`$PID); `$pause = [Threading.ManualResetEventSlim]::new(`$false); `$null = `$pause.Wait(60000)")
        $unrelatedStart = [Diagnostics.ProcessStartInfo]::new($shell)
        $unrelatedStart.UseShellExecute = $false
        $unrelatedStart.CreateNoWindow = $true
        foreach ($argument in @('-NoProfile', '-Command', '$pause = [Threading.ManualResetEventSlim]::new($false); $null = $pause.Wait(60000)')) { $unrelatedStart.ArgumentList.Add($argument) }
        $unrelated = [Diagnostics.Process]::Start($unrelatedStart)
        $descendantId = 0
        try {
            $copy = Join-Path $TestDrive ('owner-' + $Failure + '-snapshot.ps1')
            $source = [IO.File]::ReadAllText($snapshotScript)
            $boundary = '$gitApplication = (Get-Command git -CommandType Application'
            $source.Contains($boundary) | Should -BeTrue
            $termination = switch ($Failure) {
                'root exit' { 'exit 0' }
                'timeout' { '$pause = [Threading.ManualResetEventSlim]::new($false); $null = $pause.Wait(60000)' }
                'output cap' { '[Console]::Out.Write((''x'' * 2097152)); $pause = [Threading.ManualResetEventSlim]::new($false); $null = $pause.Wait(60000)' }
            }
            $injection = @"
`$descendantStart = [Diagnostics.ProcessStartInfo]::new('$($shell.Replace("'", "''"))')
`$descendantStart.UseShellExecute = `$false
`$descendantStart.CreateNoWindow = `$true
foreach (`$argument in @('-NoProfile', '-File', '$($descendant.Replace("'", "''"))')) { `$descendantStart.ArgumentList.Add(`$argument) }
`$null = [Diagnostics.Process]::Start(`$descendantStart)
`$ready = [Diagnostics.Stopwatch]::StartNew()
`$pause = [Threading.ManualResetEventSlim]::new(`$false)
while (-not [IO.File]::Exists('$($marker.Replace("'", "''"))')) {
    if (`$ready.ElapsedMilliseconds -gt 4000) { throw 'Owned descendant did not start.' }
    `$null = `$pause.Wait(10)
}
$termination
"@ + "`n"
            # Shorten only this deterministic ownership fixture's outer deadline.
            $source = $source.Replace('-TimeoutMilliseconds 30000 -OwnInspection', '-TimeoutMilliseconds 6000 -OwnInspection')
            [IO.File]::WriteAllText($copy, $source.Replace($boundary, $injection + $boundary))
            $result = Invoke-BoundedSnapshot -ScriptPath $copy
            $result.ExitCode | Should -Be 1
            $result.Output | Should -BeNullOrEmpty
            $result.Error | Should -Match $(if ($Failure -eq 'output cap') { 'one-MiB stream limit' } else { 'inspection timed out' })
            [IO.File]::Exists($marker) | Should -BeTrue
            $descendantId = [int][IO.File]::ReadAllText($marker)
            (Test-FixtureLiveProcess $descendantId) | Should -BeFalse
            $unrelated.HasExited | Should -BeFalse
        }
        finally {
            if ($descendantId -gt 0 -and (Test-FixtureLiveProcess $descendantId)) { Stop-Process -Id $descendantId -Force }
            if (-not $unrelated.HasExited) { $unrelated.Kill($true); $null = $unrelated.WaitForExit(2000) }
            $unrelated.Dispose()
        }
    }

    It 'retains verified Linux process identity through exit-time environment clearing and rejects PID reuse' -Skip:$IsWindows {
        $tokens = $null
        $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($snapshotScript, [ref]$tokens, [ref]$errors)
        $definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-ContextUnixGroupMembers' }, $true)
        $proc = Join-Path $TestDrive 'owned-proc-records'
        $record = Join-Path $proc '123456'
        $null = [IO.Directory]::CreateDirectory($record)
        $statFields = @('R', '1', '123456') + (@('0') * 16) + @('900')
        [IO.File]::WriteAllText((Join-Path $record 'stat'), '123456 (owned child) ' + ($statFields -join ' '))
        [IO.File]::WriteAllText((Join-Path $record 'environ'), 'DECOMPOSE_INSPECTION_OWNER=fixture-nonce' + [char]0)
        # Controlled proc records make the exit-time clearing race deterministic;
        # actual group termination is covered separately by the descendant fixtures.
        $body = $definition.Extent.Text.Replace("[IO.Directory]::EnumerateDirectories('/proc')", "[IO.Directory]::EnumerateDirectories('$($proc.Replace("'", "''"))')")
        . ([scriptblock]::Create($body))
        $known = [Collections.Generic.Dictionary[int, string]]::new()
        @(Get-ContextUnixGroupMembers 123456 'fixture-nonce' $known) | Should -Contain 123456
        [IO.File]::WriteAllText((Join-Path $record 'environ'), '')
        @(Get-ContextUnixGroupMembers 123456 'fixture-nonce' $known) | Should -Contain 123456
        $statFields[19] = '901'
        [IO.File]::WriteAllText((Join-Path $record 'stat'), '123456 (owned child) ' + ($statFields -join ' '))
        { Get-ContextUnixGroupMembers 123456 'fixture-nonce' $known } | Should -Throw '*ownership is unconfirmed*'
    }

    It 'bounds native Git allocation from a tiny loose object with a hostile declared size' {
        $header = [Text.Encoding]::ASCII.GetBytes('tree 1073741824' + [char]0)
        $objectId = [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData($header)).ToLowerInvariant()
        $directory = Join-Path $fixture ('.git/objects/' + $objectId.Substring(0, 2))
        $null = [IO.Directory]::CreateDirectory($directory)
        $objectPath = Join-Path $directory $objectId.Substring(2)
        $stream = [IO.File]::Create($objectPath)
        try {
            $compressed = [IO.Compression.ZLibStream]::new($stream, [IO.Compression.CompressionLevel]::Optimal, $true)
            try { $compressed.Write($header, 0, $header.Length) }
            finally { $compressed.Dispose() }
        }
        finally { $stream.Dispose() }
        (Get-Item -LiteralPath $objectPath).Length | Should -BeLessThan 100
        $parent = ([string](& git -C $fixture rev-parse HEAD)).Trim()
        $commitPath = Join-Path $TestDrive 'hostile-tree-commit.txt'
        [IO.File]::WriteAllText($commitPath, "tree $objectId`nparent $parent`nauthor Fixture <fixture@example.invalid> 946684800 +0000`ncommitter Fixture <fixture@example.invalid> 946684800 +0000`n`nOwned allocation control`n")
        $commit = ([string](& git -C $fixture hash-object -t commit -w -- $commitPath)).Trim()
        $LASTEXITCODE | Should -Be 0
        Invoke-FixtureGit @('update-ref', 'HEAD', $commit)
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        # Never run an unbounded allocation positive control on the host.
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match '(?i)memory|allocate'
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
    }

    It 'requires manual inspection when Linux Git memory limiting is unavailable' -Skip:$IsWindows {
        $copy = Join-Path $TestDrive 'missing-limiter-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $probe = 'Get-Command prlimit -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1'
        $source.Contains($probe) | Should -BeTrue
        [IO.File]::WriteAllText($copy, $source.Replace($probe, '$null'))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'Git memory limiting requires Linux prlimit'
    }

    It 'rejects a Unix <Kind> from the instruction inventory before returning paths' -Skip:$IsWindows -ForEach @(
        @{ Kind = 'named pipe'; Relative = '.github/instructions/special.instructions.md' },
        @{ Kind = 'socket'; Relative = 'scoped/AGENTS.md' }
    ) {
        $path = [IO.Path]::Combine($fixture, $Relative)
        $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))
        [IO.File]::WriteAllText($path, 'Tracked scoped guidance')
        Invoke-FixtureGit @('add', '--', $Relative)
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'scoped guidance')
        [IO.File]::Delete($path)
        $socket = $null
        try {
            if ($Kind -eq 'named pipe') {
                & mkfifo -- $path
                $LASTEXITCODE | Should -Be 0
            }
            else {
                $socket = [Net.Sockets.Socket]::new([Net.Sockets.AddressFamily]::Unix, [Net.Sockets.SocketType]::Stream, [Net.Sockets.ProtocolType]::Unspecified)
                $socket.Bind([Net.Sockets.UnixDomainSocketEndPoint]::new($path))
            }
            @(& git -C $fixture ls-files --cached) | Should -Contain $Relative
            $result = Invoke-BoundedSnapshot
            $result.ExitCode | Should -Be 1
            $result.Output | Should -BeNullOrEmpty
            $result.Error | Should -Match 'Non-regular inventory entries require manual instruction discovery'
        }
        finally { if ($null -ne $socket) { $socket.Dispose() } }
    }

    It 'requires manual inspection of tracked dangling symlink content' -Skip:$IsWindows {
        $path = Join-Path $fixture 'tools/dangling.md'
        $null = [IO.File]::CreateSymbolicLink($path, 'missing-target.md')
        [IO.File]::Exists((Join-Path $fixture 'tools/missing-target.md')) | Should -BeFalse
        ([IO.FileInfo]::new($path)).LinkTarget | Should -BeExactly 'missing-target.md'
        $inventory = Get-FixtureSnapshot
        $inventory.Dirty | Should -BeTrue
        $inventory.Paths | Should -Contain 'tools/dangling.md'
        Invoke-FixtureGit @('add', 'tools/dangling.md')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'tracked dangling link')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Tracked symlink content requires manual inspection*'
        ([IO.FileInfo]::new($path)).LinkTarget | Should -BeExactly 'missing-target.md'
    }

    It 'rejects a forged clean stat cache for a changed tracked dangling symlink' -Skip:$IsWindows {
        $path = Join-Path $fixture 'tools/dangling.md'
        $null = [IO.File]::CreateSymbolicLink($path, 'missing-one.md')
        Invoke-FixtureGit @('add', 'tools/dangling.md')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'dangling link baseline')
        $original = ([string](& git -C $fixture rev-parse HEAD:tools/dangling.md)).Trim()
        [IO.File]::Delete($path)
        $null = [IO.File]::CreateSymbolicLink($path, 'missing-two.md')
        Invoke-FixtureGit @('add', 'tools/dangling.md')
        Set-FixtureStagedObject 'tools/dangling.md' $original
        @(& git --no-optional-locks -c core.fsmonitor= -c core.trustctime=true -c core.checkStat=default -C $fixture status --porcelain=v1).Count | Should -Be 0
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        { Get-FixtureSnapshot } | Should -Throw '*Tracked symlink content requires manual inspection*'
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        ([IO.FileInfo]::new($path)).LinkTarget | Should -BeExactly 'missing-two.md'
    }

    It 'preserves containment at the filesystem root of an owned tracked file' {
        $tokens = $null
        $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($snapshotScript, [ref]$tokens, [ref]$errors)
        $definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-ContextTrackedFile' }, $true)
        $definition | Should -Not -BeNullOrEmpty
        . ([scriptblock]::Create($definition.Extent.Text))
        $path = Join-Path $fixture 'AGENTS.md'
        $filesystemRoot = [IO.Path]::GetPathRoot($path)
        $relative = [IO.Path]::GetRelativePath($filesystemRoot, $path)
        (Get-ContextTrackedFile $filesystemRoot $relative).FullName | Should -BeExactly $path
    }

    It 'rejects external shallow history overrides before attributing a clean target' {
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-m', 'second commit')
        $boundary = Join-Path $TestDrive 'external-shallow'
        [IO.File]::WriteAllText($boundary, ([string](& git -C $fixture rev-parse HEAD)).Trim() + "`n")
        $previous = [Environment]::GetEnvironmentVariable('GIT_SHALLOW_FILE')
        try {
            [int](& git -C $fixture rev-list --count HEAD) | Should -Be 2
            $env:GIT_SHALLOW_FILE = $boundary
            [int](& git -C $fixture rev-list --count HEAD) | Should -Be 1
            @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
            { Get-FixtureSnapshot } | Should -Throw '*Ambient Git override GIT_SHALLOW_FILE*'
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath Env:GIT_SHALLOW_FILE -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable('GIT_SHALLOW_FILE', $previous) }
        }
    }

    It 'preserves case-distinct paths on a case-sensitive filesystem' {
        if (-not $caseSensitiveFileSystem) { Set-ItResult -Skipped -Because 'The fixture filesystem is case-insensitive.'; return }
        Set-Content -LiteralPath (Join-Path $fixture 'tools/Foo.md') -Value 'uppercase'
        Set-Content -LiteralPath (Join-Path $fixture 'tools/foo.md') -Value 'lowercase'
        Invoke-FixtureGit @('add', 'tools/Foo.md', 'tools/foo.md')
        $snapshot = Get-FixtureSnapshot
        @($snapshot.Paths | Where-Object { $_ -ceq 'tools/Foo.md' }).Count | Should -Be 1
        @($snapshot.Paths | Where-Object { $_ -ceq 'tools/foo.md' }).Count | Should -Be 1
    }

    It 'discovers case-distinct instructions concealed by core.ignoreCase' {
        if (-not $caseSensitiveFileSystem) { Set-ItResult -Skipped -Because 'The fixture filesystem is case-insensitive.'; return }
        [IO.File]::WriteAllText((Join-Path $fixture 'Foo.instructions.md'), 'Tracked uppercase guidance')
        Invoke-FixtureGit @('add', 'Foo.instructions.md')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'case-folding fixture')
        Invoke-FixtureGit @('config', 'core.ignoreCase', 'true')
        [IO.File]::WriteAllText((Join-Path $fixture 'foo.instructions.md'), 'Untracked lowercase guidance')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        @(& git -C $fixture ls-files --cached --others --exclude-standard | Where-Object { $_ -ceq 'foo.instructions.md' }).Count | Should -Be 0
        $snapshot = Get-FixtureSnapshot -Paths @('AGENTS.md', 'foo.instructions.md')
        $snapshot.Dirty | Should -BeTrue
        @($snapshot.Paths | Where-Object { $_ -ceq 'Foo.instructions.md' }).Count | Should -Be 1
        @($snapshot.Paths | Where-Object { $_ -ceq 'foo.instructions.md' }).Count | Should -Be 1
        @($snapshot.SelectedInputs.Path | Where-Object { $_ -ceq 'foo.instructions.md' }).Count | Should -Be 1
        [string](& git -C $fixture config --get core.ignoreCase) | Should -BeExactly 'true'
    }

    It 'preserves a <Label> in tracked and untracked filenames' -Skip:$IsWindows -ForEach @(
        @{ Label = 'newline'; Character = "`n" },
        @{ Label = 'tab'; Character = "`t" },
        @{ Label = 'quote'; Character = '"' }
    ) {
        $tracked = 'tools/tracked' + $Character + 'name.md'
        $untracked = 'tools/untracked' + $Character + 'name.md'
        Set-Content -LiteralPath (Join-Path $fixture $tracked) -Value 'tracked'
        Set-Content -LiteralPath (Join-Path $fixture $untracked) -Value 'untracked'
        Invoke-FixtureGit @('add', '--', $tracked)
        $snapshot = Get-FixtureSnapshot
        @($snapshot.Paths | Where-Object { $_ -ceq $tracked }).Count | Should -Be 1
        @($snapshot.Paths | Where-Object { $_ -ceq $untracked }).Count | Should -Be 1
    }

    It 'rejects <Flag> index flags that conceal changed source files' -ForEach @(
        @{ Flag = 'assume-unchanged' }, @{ Flag = 'skip-worktree' }
    ) {
        Invoke-FixtureGit @('update-index', "--$Flag", 'packages/widget/model.txt')
        Set-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'changed source'
        $status = @(& git -C $fixture status --porcelain=v1)
        $LASTEXITCODE | Should -Be 0
        $status.Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Hidden index flags require manual inspection*'
    }

    It 'rejects a repository-local worktree redirect to another repository' {
        $other = Join-Path $TestDrive 'other-repository'
        New-Item -ItemType Directory -Path $other | Out-Null
        Set-Content -LiteralPath (Join-Path $other 'AGENTS.md') -Value 'Different repository instructions'
        Invoke-FixtureGit @('-C', $other, 'init', '-b', 'other')
        Invoke-FixtureGit @('-C', $other, 'config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('-C', $other, 'add', '.')
        Invoke-FixtureGit @('-C', $other, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'other baseline')
        Invoke-FixtureGit @('config', 'core.worktree', $other)
        $redirected = [string](& git -C $fixture rev-parse --show-toplevel)
        [IO.Path]::GetFullPath($redirected) | Should -BeExactly $other
        { Get-FixtureSnapshot } | Should -Throw '*Configured core.worktree requires manual inspection*'
    }

    It 'rejects a Git file that redirects the selected directory to another repository' {
        $selected = Join-Path $TestDrive 'redirected-working-copy'
        New-Item -ItemType Directory -Path $selected | Out-Null
        Set-Content -LiteralPath (Join-Path $selected '.git') -Value ('gitdir: ' + (Join-Path $fixture '.git').Replace('\', '/'))
        Set-Content -LiteralPath (Join-Path $selected 'AGENTS.md') -Value 'Different directory'
        $foreignHead = [string](& git -C $selected rev-parse HEAD)
        $foreignHead | Should -BeExactly ([string](& git -C $fixture rev-parse HEAD))
        { Get-FixtureSnapshot -Root $selected -Paths @('AGENTS.md') } | Should -Throw '*Git directory indirection requires manual inspection*'
    }

    It 'rejects an embedded commondir redirect despite matching worktree and gitdir roots' {
        $other = Join-Path $TestDrive 'common-target'
        New-Item -ItemType Directory -Path $other | Out-Null
        Set-Content -LiteralPath (Join-Path $other 'AGENTS.md') -Value 'Foreign common metadata'
        Invoke-FixtureGit @('-C', $other, 'init', '-b', 'release/trunk')
        Invoke-FixtureGit @('-C', $other, 'add', '.')
        Invoke-FixtureGit @('-C', $other, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'foreign baseline')
        Set-Content -LiteralPath (Join-Path $fixture '.git/commondir') -Value ((Join-Path $other '.git').Replace('\', '/'))
        [IO.Path]::GetFullPath([string](& git -C $fixture rev-parse --show-toplevel)) | Should -BeExactly $fixture
        [IO.Path]::GetFullPath([string](& git -C $fixture rev-parse --absolute-git-dir)) | Should -BeExactly (Join-Path $fixture '.git')
        [string](& git -C $fixture rev-parse HEAD) | Should -BeExactly ([string](& git -C $other rev-parse HEAD))
        { Get-FixtureSnapshot } | Should -Throw '*Git common directory differs*'
    }

    It 'rejects a clean shared clone whose HEAD and ancestry use an alternate object store' {
        [IO.File]::WriteAllText((Join-Path $fixture 'borrowed-parent.txt'), 'Synthetic parent content for the fixture')
        Invoke-FixtureGit @('add', 'borrowed-parent.txt')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'synthetic borrowed parent')
        [IO.File]::Delete((Join-Path $fixture 'borrowed-parent.txt'))
        Invoke-FixtureGit @('add', '-u')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'remove parent fixture content')
        $borrowed = Join-Path $TestDrive 'borrowed-objects'
        Invoke-FixtureGit @('clone', '--shared', '--', $fixture, $borrowed)
        $head = [string](& git -C $borrowed rev-parse HEAD)
        $head | Should -BeExactly ([string](& git -C $fixture rev-parse HEAD))
        @(& git -C $borrowed status --porcelain=v1).Count | Should -Be 0
        [IO.Path]::GetFullPath([string](& git -C $borrowed rev-parse --show-toplevel)) | Should -BeExactly $borrowed
        [IO.Path]::GetFullPath([string](& git -C $borrowed rev-parse --absolute-git-dir)) | Should -BeExactly (Join-Path $borrowed '.git')
        $alternate = [IO.FileInfo]::new((Join-Path $borrowed '.git/objects/info/alternates'))
        $alternate.Exists | Should -BeTrue
        $alternate.LinkTarget | Should -BeNullOrEmpty
        [IO.File]::Exists((Join-Path $borrowed ('.git/objects/' + $head.Substring(0, 2) + '/' + $head.Substring(2)))) | Should -BeFalse
        [string](& git -C $borrowed show 'HEAD~1:borrowed-parent.txt') | Should -BeExactly 'Synthetic parent content for the fixture'
        { Get-FixtureSnapshot -Root $borrowed } | Should -Throw '*Alternate Git object stores require manual inspection*'
    }

    It 'rejects regular graft metadata even when advice is suppressed and replacement refs are disabled' {
        $head=[string](& git -C $fixture rev-parse HEAD)
        $tree=[string](& git -C $fixture write-tree)
        $parent=[string](& git -C $fixture -c user.name=Fixture -c user.email=fixture@example.invalid commit-tree $tree -m 'synthetic unrelated parent')
        [IO.File]::WriteAllText((Join-Path $fixture '.git/info/grafts'), "$head $parent`n")
        Invoke-FixtureGit @('config', 'advice.graftFileDeprecated', 'false')
        @(& git --no-replace-objects -C $fixture status --porcelain=v1).Count | Should -Be 0
        [string](& git --no-replace-objects -C $fixture log -1 '--format=%H %P') | Should -BeExactly "$head $parent"
        { Get-FixtureSnapshot } | Should -Throw '*Legacy Git grafts require manual inspection*'
    }

    It 'preserves plain nested failure diagnostics under forced ANSI rendering' {
        $copy = Join-Path $TestDrive 'ansi-diagnostics-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$ErrorActionPreference = ''Stop'''
        $source.Contains($boundary) | Should -BeTrue
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, $boundary + "`n`$PSStyle.OutputRendering = 'Ansi'"))
        [IO.File]::WriteAllText((Join-Path $fixture '.git/objects/info/alternates'), 'synthetic alternate entry')
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'Alternate Git object stores require manual inspection'
        $result.Error | Should -Not -Match ([regex]::Escape([string][char]27))
    }

    It 'rejects a regular HTTP alternates metadata file before reading objects' {
        [IO.File]::WriteAllText((Join-Path $fixture '.git/objects/info/http-alternates'), 'https://example.invalid/objects')
        { Get-FixtureSnapshot } | Should -Throw '*Alternate Git object stores require manual inspection*'
    }

    It 'rejects a linked .git/<Metadata> despite matching worktree and gitdir roots' -ForEach @(
        @{ Metadata = 'refs' }, @{ Metadata = 'objects' }, @{ Metadata = 'refs/heads' }
    ) {
        $metadataPath = [IO.Path]::GetFullPath((Join-Path $fixture ".git/$Metadata"))
        $outside = [IO.Path]::GetFullPath((Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-metadata')))
        $ownedRoot = [IO.Path]::GetFullPath($TestDrive).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $metadataPath.StartsWith($ownedRoot, [StringComparison]::Ordinal) | Should -BeTrue
        $outside.StartsWith($ownedRoot, [StringComparison]::Ordinal) | Should -BeTrue
        Move-Item -LiteralPath $metadataPath -Destination $outside
        $linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
        New-Item -ItemType $linkType -Path $metadataPath -Target $outside | Out-Null
        [IO.Path]::GetFullPath([string](& git -C $fixture rev-parse --show-toplevel)) | Should -BeExactly $fixture
        [IO.Path]::GetFullPath([string](& git -C $fixture rev-parse --absolute-git-dir)) | Should -BeExactly (Join-Path $fixture '.git')
        { Get-FixtureSnapshot } | Should -Throw '*Linked Git metadata entries require manual inspection*'
    }

    It 'bounds a blocked Git metadata enumerator in its owned child' {
        $copy = Join-Path $TestDrive 'metadata-stall-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$pending.Dequeue().EnumerateFileSystemInfos()'
        $source.Contains($boundary) | Should -BeTrue
        # Deterministically simulate stalled enumerator advancement without requiring FUSE or a network filesystem.
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, '$(while ($true) { })'))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context inspection timed out'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'rejects a linked Git index' -Skip:$IsWindows {
        $index = Join-Path $fixture '.git/index'
        $outside = Join-Path $TestDrive 'foreign-index'
        [IO.File]::Move($index, $outside)
        $null = [IO.File]::CreateSymbolicLink($index, $outside)
        { Get-FixtureSnapshot } | Should -Throw '*Linked Git metadata entries require manual inspection*'
    }

    It 'takes the declared manual fallback for legitimate linked Git worktrees' {
        $linked = Join-Path $TestDrive 'linked-working-copy'
        Invoke-FixtureGit @('worktree', 'add', '-b', 'linked', $linked)
        { Get-FixtureSnapshot -Root $linked } | Should -Throw '*Git directory indirection requires manual inspection*'
    }

    It 'rejects case aliases of the root without assuming Windows case folding' {
        $caseRoot = Join-Path $TestDrive 'case-root'
        New-Item -ItemType Directory -Path $caseRoot | Out-Null
        Set-Content -LiteralPath (Join-Path $caseRoot 'AGENTS.md') -Value 'Selected instructions'
        Invoke-FixtureGit @('-C', $caseRoot, 'init', '-b', 'case-fixture')
        Invoke-FixtureGit @('-C', $caseRoot, 'config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('-C', $caseRoot, 'add', '.')
        Invoke-FixtureGit @('-C', $caseRoot, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'case baseline')
        if ($caseSensitiveFileSystem) {
            $sibling = Join-Path $TestDrive 'CASE-ROOT'
            New-Item -ItemType Directory -Path $sibling | Out-Null
            Set-Content -LiteralPath (Join-Path $sibling 'AGENTS.md') -Value 'Foreign instructions'
        }
        { Get-FixtureSnapshot -Root $caseRoot -Paths @('../CASE-ROOT/AGENTS.md') } | Should -Throw '*Context path escapes*'
    }

    It 'overrides reduced stat settings that conceal a same-size source rewrite' -Skip:$IsWindows {
        Invoke-FixtureGit @('config', 'core.trustctime', 'false')
        Invoke-FixtureGit @('config', 'core.checkStat', 'minimal')
        $path = Join-Path $fixture 'packages/widget/model.txt'
        # Keep the cached mtime older than the index so racy-Git rehashing cannot mask the reduced-stat case.
        [IO.File]::SetLastWriteTimeUtc($path, [DateTime]::new(2000, 1, 1, 0, 0, 0, [DateTimeKind]::Utc))
        Invoke-FixtureGit @('update-index', '--refresh')
        Set-FixtureCachedCTime -Relative 'packages/widget/model.txt'
        $stamp = (Get-Item -LiteralPath $path).LastWriteTimeUtc
        [IO.File]::WriteAllText($path, "modified`n")
        [IO.File]::SetLastWriteTimeUtc($path, $stamp)
        $status = @(& git -C $fixture status --porcelain=v1)
        $LASTEXITCODE | Should -Be 0
        $status.Count | Should -Be 0
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        ([string](& git -C $fixture config --get core.trustctime)) | Should -BeExactly 'false'
        ([string](& git -C $fixture config --get core.checkStat)) | Should -BeExactly 'minimal'
    }

    It 'detects unselected tracked content concealed by a forged matching stat cache without writes' {
        $relative = 'packages/widget/model.txt'
        $original = ([string](& git -C $fixture rev-parse ('HEAD:' + $relative))).Trim()
        [IO.File]::WriteAllText((Join-Path $fixture $relative), "modified`n")
        Invoke-FixtureGit @('add', '--', $relative)
        Set-FixtureStagedObject $relative $original
        @(& git --no-optional-locks -c core.fsmonitor= -c core.trustctime=true -c core.checkStat=default -c core.ignoreStat=false -C $fixture status --porcelain=v1).Count | Should -Be 0
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $configHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/config')).Hash
        $objects = @([IO.Directory]::EnumerateFiles((Join-Path $fixture '.git/objects'), '*', [IO.SearchOption]::AllDirectories) | Sort-Object)
        $snapshot = Get-FixtureSnapshot
        $snapshot.SelectedInputs.Path | Should -Not -Contain $relative
        $snapshot.Dirty | Should -BeTrue
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/config')).Hash | Should -BeExactly $configHash
        (@([IO.Directory]::EnumerateFiles((Join-Path $fixture '.git/objects'), '*', [IO.SearchOption]::AllDirectories) | Sort-Object) -join "`n") | Should -BeExactly ($objects -join "`n")
    }

    It 'preserves built-in CRLF normalization when checking tracked content' {
        [IO.File]::WriteAllText((Join-Path $fixture '.gitattributes'), "*.txt text eol=crlf`n")
        Invoke-FixtureGit @('add', '.gitattributes')
        Invoke-FixtureGit @('add', '--renormalize', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'CRLF checkout policy')
        $path = Join-Path $fixture 'packages/widget/model.txt'
        [IO.File]::Delete($path)
        Invoke-FixtureGit @('checkout', '--', 'packages/widget/model.txt')
        [IO.File]::ReadAllText($path).Contains("`r`n") | Should -BeTrue
        (Get-FixtureSnapshot).Dirty | Should -BeFalse
    }

    It 'hashes quoted Unicode and control-character tracked filenames literally' -Skip:$IsWindows {
        $paths = @('models/é"name.txt', "models/line`nbreak.txt", 'models/back\slash.txt')
        $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'models'))
        foreach ($path in $paths) { [IO.File]::WriteAllText([IO.Path]::Combine($fixture, $path), 'Tracked content') }
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'literal tracked names')
        (Get-FixtureSnapshot).Dirty | Should -BeFalse
        [IO.File]::WriteAllText([IO.Path]::Combine($fixture, $paths[1]), 'Changed content')
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
    }

    It 'preserves literal Unix backslashes separately from directory separators' -Skip:$IsWindows {
        $literal = 'policies\team.md'
        $nested = 'policies/team.md'
        New-Item -ItemType Directory -Path (Join-Path $fixture 'policies') | Out-Null
        [IO.File]::WriteAllText([IO.Path]::Combine($fixture, $literal), 'Literal filename')
        [IO.File]::WriteAllText([IO.Path]::Combine($fixture, $nested), 'Nested filename')
        $snapshot = Get-FixtureSnapshot -Paths @($literal, $nested)
        $snapshot.Paths | Should -Contain $literal
        $snapshot.Paths | Should -Contain $nested
        $snapshot.SelectedInputs[0].Path | Should -BeExactly $literal
        $snapshot.SelectedInputs[1].Path | Should -BeExactly $nested
        $snapshot.SelectedInputs[0].Sha256 | Should -Not -BeExactly $snapshot.SelectedInputs[1].Sha256
    }

    It 'supports a Unix root ending in a literal backslash' -Skip:$IsWindows {
        $backslashRoot = [IO.Path]::Combine($TestDrive, 'repo\')
        $null = [IO.Directory]::CreateDirectory($backslashRoot)
        [IO.File]::WriteAllText([IO.Path]::Combine($backslashRoot, 'AGENTS.md'), 'Backslash root')
        Invoke-FixtureGit @('-C', $backslashRoot, 'init', '-b', 'backslash-fixture')
        Invoke-FixtureGit @('-C', $backslashRoot, 'config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('-C', $backslashRoot, 'add', '.')
        Invoke-FixtureGit @('-C', $backslashRoot, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'backslash baseline')
        $snapshot = Get-FixtureSnapshot -Root $backslashRoot -Paths @('AGENTS.md')
        $snapshot.RepositoryRoot | Should -BeExactly $backslashRoot
        $snapshot.SelectedInputs[0].Path | Should -BeExactly 'AGENTS.md'
    }

    It 'preserves a trailing <Label> in a Unix repository root' -Skip:$IsWindows -ForEach @(
        @{ Label = 'space'; Suffix = ' ' },
        @{ Label = 'tab'; Suffix = "`t" },
        @{ Label = 'newline'; Suffix = "`n" },
        @{ Label = 'carriage return'; Suffix = "`r" }
    ) {
        $whitespaceRoot = [IO.Path]::Combine($TestDrive, 'whitespace-' + $Label + $Suffix)
        $null = [IO.Directory]::CreateDirectory($whitespaceRoot)
        [IO.File]::WriteAllText([IO.Path]::Combine($whitespaceRoot, 'AGENTS.md'), 'Whitespace root instructions')
        Invoke-FixtureGit @('-C', $whitespaceRoot, 'init', '-b', 'whitespace')
        Invoke-FixtureGit @('-C', $whitespaceRoot, 'config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('-C', $whitespaceRoot, 'add', '.')
        Invoke-FixtureGit @('-C', $whitespaceRoot, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'whitespace baseline')
        $snapshot = Get-FixtureSnapshot -Root $whitespaceRoot -Paths @('AGENTS.md')
        $snapshot.RepositoryRoot | Should -BeExactly $whitespaceRoot
        $snapshot.SelectedInputs[0].Path | Should -BeExactly 'AGENTS.md'
    }

    It 'rejects a content-free pending merge despite clean porcelain status' {
        Invoke-FixtureGit @('checkout', '-b', 'pending-merge')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-m', 'other empty commit')
        Invoke-FixtureGit @('checkout', 'release/trunk')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-m', 'local empty commit')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'merge', '--no-commit', '--no-ff', 'pending-merge')
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        [IO.File]::Exists((Join-Path $fixture '.git/MERGE_HEAD')) | Should -BeTrue
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'In-progress Git operations require manual recovery'
    }

    It 'rejects pending Git state at <Marker> before inspecting commits' -ForEach @(
        @{ Marker = 'CHERRY_PICK_HEAD'; Directory = $false },
        @{ Marker = 'REVERT_HEAD'; Directory = $false },
        @{ Marker = 'REBASE_HEAD'; Directory = $false },
        @{ Marker = 'rebase-apply'; Directory = $true },
        @{ Marker = 'rebase-merge'; Directory = $true },
        @{ Marker = 'sequencer'; Directory = $true }
    ) {
        $path = Join-Path $fixture ('.git/' + $Marker)
        if ($Directory) { $null = [IO.Directory]::CreateDirectory($path) }
        else { [IO.File]::WriteAllText($path, 'Pending operation fixture') }
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'In-progress Git operations require manual recovery'
    }

    It 'rejects a pending merge introduced between observations' {
        $path = (Join-Path $fixture '.git/MERGE_HEAD').Replace("'", "''")
        $marker = Join-Path $TestDrive 'pending-operation-mutated'
        $prelude = Get-SnapshotMutationPrelude -Marker $marker -Mutation "[IO.File]::WriteAllText('$path', 'Pending operation fixture')"
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*In-progress Git operations require manual recovery*'
        [IO.File]::Exists($marker) | Should -BeTrue
    }

    It 'does not mistake an unrelated nested metadata filename for pending Git state' {
        [IO.File]::WriteAllText((Join-Path $fixture '.git/info/MERGE_HEAD'), 'Unrelated fixture metadata')
        (Get-FixtureSnapshot).Dirty | Should -BeFalse
    }

    It 'records a new head rather than reusing evidence from the baseline' {
        $before = Get-FixtureSnapshot
        Add-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'changed'
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'change')
        (Get-FixtureSnapshot).Head | Should -Not -BeExactly $before.Head
    }

    It 'takes a manual fallback for <Setting> metadata' -ForEach @(
        @{ Setting = 'extensions.partialclone'; Value = 'origin' },
        @{ Setting = 'remote.origin.promisor'; Value = 'true' }
    ) {
        Invoke-FixtureGit @('config', $Setting, $Value)
        { Get-FixtureSnapshot } | Should -Throw '*Partial/promisor repositories require manual inspection*'
    }

    It 'rejects a promisor remote before a missing tree can execute its transport helper' -Skip:$IsWindows {
        $marker = Join-Path $fixture 'partial-fetch-marker'
        $driver = Join-Path $fixture 'partial-fetch-hook'
        [IO.File]::WriteAllText($driver, "#!/bin/sh`nprintf executed > partial-fetch-marker`nexit 1`n")
        [IO.File]::SetUnixFileMode($driver, [IO.UnixFileMode]493)
        Invoke-FixtureGit @('config', 'remote.origin.promisor', 'true')
        Invoke-FixtureGit @('config', 'remote.origin.url', 'ext::./partial-fetch-hook')
        Invoke-FixtureGit @('config', 'protocol.ext.allow', 'always')
        $content = "tree 1111111111111111111111111111111111111111`nauthor Fixture <fixture@example.invalid> 1000000000 +0000`ncommitter Fixture <fixture@example.invalid> 1000000000 +0000`n`nMissing tree fixture`n"
        $commit = $content | & git -C $fixture hash-object -t commit -w --stdin
        $LASTEXITCODE | Should -Be 0
        Invoke-FixtureGit @('update-ref', 'HEAD', $commit)
        { Get-FixtureSnapshot } | Should -Throw '*Partial/promisor repositories require manual inspection*'
        Test-Path -LiteralPath $marker | Should -BeFalse
        & git --no-optional-locks -c core.fsmonitor= -C $fixture status --porcelain=v1 2>&1 | Out-Null
        Test-Path -LiteralPath $marker | Should -BeTrue
    }

    It 'rejects symlink conversions concealed by core.symlinks false' {
        Invoke-FixtureGit @('config', 'core.symlinks', 'false')
        $path = Join-Path $fixture 'tools/link.md'
        [IO.File]::WriteAllText($path, 'missing-target.md')
        $blob = & git -C $fixture hash-object -w $path
        $LASTEXITCODE | Should -Be 0
        Invoke-FixtureGit @('update-index', '--add', '--cacheinfo', "120000,$blob,tools/link.md")
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'symlink conversion fixture')
        ([IO.FileInfo]::new($path)).LinkTarget | Should -BeNullOrEmpty
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Tracked symlinks with core.symlinks=false require manual inspection*'
    }

    It 'rejects a forged cached index root despite clean status and benign staged entries' {
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $originalTree = [string](& git -C $fixture rev-parse 'HEAD^{tree}')
        $blob = [string](& git -C $fixture rev-parse 'HEAD:AGENTS.md')
        $payloadTree = New-FixtureTree @("100644 blob $blob`tprivate-payload")
        Set-FixtureCachedTree $originalTree $payloadTree
        @(& git --no-optional-locks -c core.fsmonitor= -c core.commitGraph=false -C $fixture status --porcelain=v1).Count | Should -Be 0
        (& git -C $fixture ls-files --stage | Out-String) | Should -Not -Match 'private-payload'
        [string](& git --no-optional-locks -C $fixture write-tree) | Should -BeExactly $payloadTree
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $objectsBefore = @(& git -C $fixture count-objects -v) -join "`n"
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'Cached Git index tree disagrees with staged entries'
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        (@(& git -C $fixture count-objects -v) -join "`n") | Should -BeExactly $objectsBefore
    }

    It 'rejects a forged valid child beneath an <RootState> cached index root' -ForEach @(
        @{ RootState = 'invalidated'; StageRootEdit = $true },
        @{ RootState = 'intact'; StageRootEdit = $false }
    ) {
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $originalTree = [string](& git -C $fixture rev-parse 'HEAD:tools')
        $blob = [string](& git -C $fixture rev-parse 'HEAD:tools/verify.sh')
        $payloadTree = New-FixtureTree @("100644 blob $blob`tprivate-payload")
        if ($StageRootEdit) {
            [IO.File]::AppendAllText((Join-Path $fixture 'AGENTS.md'), 'Staged root edit')
            Invoke-FixtureGit @('add', 'AGENTS.md')
        }
        Set-FixtureCachedTree $originalTree $payloadTree
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'Cached Git index tree disagrees with staged entries'
        if (-not $StageRootEdit) {
            [IO.File]::AppendAllText((Join-Path $fixture 'AGENTS.md'), 'Later coordinator edit')
            Invoke-FixtureGit @('add', 'AGENTS.md')
        }
        $publishedTree = [string](& git -C $fixture write-tree)
        @(& git -C $fixture ls-tree -r --name-only $publishedTree) | Should -Contain 'tools/private-payload'
        @(& git -C $fixture ls-files) | Should -Contain 'tools/verify.sh'
    }

    It 'accepts a genuine SHA256 version-four cached index tree' {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-sha256')
        $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'nested'))
        [IO.File]::WriteAllText((Join-Path $fixture 'AGENTS.md'), 'SHA256 instructions')
        [IO.File]::WriteAllText((Join-Path $fixture 'nested/a'), 'First file')
        [IO.File]::WriteAllText((Join-Path $fixture 'nested/ab'), 'Second file')
        Invoke-FixtureGit @('init', '--object-format=sha256', '-b', 'sha256-fixture')
        Invoke-FixtureGit @('config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'SHA256 baseline')
        Invoke-FixtureGit @('update-index', '--index-version=4')
        $snapshot = Get-FixtureSnapshot -Paths @('AGENTS.md')
        $snapshot.Dirty | Should -BeFalse
        $snapshot.Head.Length | Should -Be 64
        $snapshot.Paths | Should -Contain 'nested/ab'
    }

    It 'accepts genuine cached index trees with directory prefixes and distinct Unicode names' {
        $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'same'))
        foreach ($name in @('same.file', 'same/leaf', [string][char]0xe000, [char]::ConvertFromUtf32(0x10000))) {
            [IO.File]::WriteAllText([IO.Path]::Combine($fixture, $name), $name)
        }
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'tree names fixture')
        (Get-FixtureSnapshot).Dirty | Should -BeFalse
    }

    It 'accepts a genuinely invalidated cached index root with valid children' {
        [IO.File]::AppendAllText((Join-Path $fixture 'AGENTS.md'), 'Real staged edit')
        Invoke-FixtureGit @('add', 'AGENTS.md')
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
    }

    It 'accepts a genuine version-three cached index with intent-to-add' {
        [IO.File]::WriteAllText((Join-Path $fixture 'new-file'), 'Intent to add')
        Invoke-FixtureGit @('add', '--intent-to-add', 'new-file')
        Invoke-FixtureGit @('update-index', '--index-version=3')
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
    }

    It 'rejects a cached index tree containing an extra empty directory' {
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $originalTree = [string](& git -C $fixture rev-parse 'HEAD^{tree}')
        $emptyTree = New-FixtureTree @()
        $entries = @(& git -C $fixture ls-tree HEAD) + "040000 tree $emptyTree`textra-empty"
        $payloadTree = New-FixtureTree $entries
        Set-FixtureCachedTree $originalTree $payloadTree
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Cached Git index tree disagrees with staged entries'
    }

    It 'requires manual inspection for split cached index storage' {
        Invoke-FixtureGit @('update-index', '--split-index')
        { Get-FixtureSnapshot } | Should -Throw '*Mandatory Git index extensions require manual inspection*'
    }

    It 'exposes untracked files concealed by a forged untracked cache' {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-untracked-cache')
        $null = [IO.Directory]::CreateDirectory($fixture)
        [IO.File]::WriteAllText((Join-Path $fixture 'AGENTS.md'), 'Untracked cache fixture')
        Invoke-FixtureGit @('init', '--object-format=sha1', '-b', 'cache-fixture')
        Invoke-FixtureGit @('config', 'status.showUntrackedFiles', 'all')
        Invoke-FixtureGit @('config', 'core.untrackedCache', 'true')
        Invoke-FixtureGit @('config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('add', 'AGENTS.md')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'cache baseline')
        Invoke-FixtureGit @('update-index', '--index-version=2')
        $hiddenName = 'hidden-payload.txt'
        [IO.File]::WriteAllText((Join-Path $fixture $hiddenName), 'Untracked payload')
        Invoke-FixtureGit @('status', '--porcelain=v1', '--untracked-files=all')
        $indexPath = Join-Path $fixture '.git/index'
        $bytes = [IO.File]::ReadAllBytes($indexPath)
        # SHA-1 index v2 and UNTR layout: https://git-scm.com/docs/gitformat-index.
        [Convert]::ToInt32([BitConverter]::ToString($bytes, 4, 4).Replace('-', ''), 16) | Should -Be 2
        [Convert]::ToInt32([BitConverter]::ToString($bytes, 8, 4).Replace('-', ''), 16) | Should -Be 1
        $entryEnd = 12 + 62
        while ($bytes[$entryEnd] -ne 0) { $entryEnd++ }
        $extensions = 12 + [int]([Math]::Ceiling(($entryEnd + 1 - 12) / 8.0) * 8)
        $cursor = $extensions
        while ([Text.Encoding]::ASCII.GetString($bytes, $cursor, 4) -cne 'UNTR') {
            $cursor += 8 + [Convert]::ToInt32([BitConverter]::ToString($bytes, $cursor + 4, 4).Replace('-', ''), 16)
            if ($cursor -ge $bytes.Length - 20) { throw 'Fixture untracked cache was not found.' }
        }
        $size = [Convert]::ToInt32([BitConverter]::ToString($bytes, $cursor + 4, 4).Replace('-', ''), 16)
        $cache = [byte[]]$bytes[($cursor + 8)..($cursor + 7 + $size)]
        $offset = 0
        $encoded = [int]$cache[$offset++]
        $environmentLength = $encoded -band 127
        while (($encoded -band 128) -ne 0) {
            $encoded = [int]$cache[$offset++]
            $environmentLength = ($environmentLength + 1) * 128 + ($encoded -band 127)
        }
        # Git's UNTR stat_data has nine uint32 fields (no index-entry mode field).
        $offset += $environmentLength + 72 + 4 + 40
        $ignoreEnd = [Array]::IndexOf($cache, [byte]0, $offset)
        $ignoreEnd | Should -BeGreaterOrEqual $offset
        $offset = $ignoreEnd + 1
        $cache[$offset++] | Should -Be 1 # One cached directory: the root.
        $countOffset = $offset++
        $cache[$countOffset] | Should -Be 1
        $cache[$offset++] | Should -Be 0 # No subdirectories.
        $cache[$offset++] | Should -Be 0 # Empty root name.
        $nameStart = $offset
        $nameEnd = [Array]::IndexOf($cache, [byte]0, $offset)
        $nameEnd | Should -BeGreaterOrEqual $offset
        $offset = $nameEnd + 1
        [Text.Encoding]::UTF8.GetString($cache, $nameStart, $offset - $nameStart - 1) | Should -BeExactly $hiddenName
        $cache[$countOffset] = 0
        $forged = [byte[]]($cache[0..($nameStart - 1)] + $cache[$offset..($cache.Length - 1)])
        $lengthBytes = [BitConverter]::GetBytes([int]$forged.Length)
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($lengthBytes) }
        # Preserve entries and current root stat data; discard other optional extensions.
        $body = [byte[]]($bytes[0..($extensions - 1)] + [Text.Encoding]::ASCII.GetBytes('UNTR') + $lengthBytes + $forged)
        [IO.File]::WriteAllBytes($indexPath, [byte[]]($body + [Security.Cryptography.SHA1]::HashData($body)))
        # Make the index strictly newer than the cached root without a wall-clock delay.
        [IO.File]::SetLastWriteTimeUtc($indexPath, [IO.Directory]::GetLastWriteTimeUtc($fixture).AddDays(1))
        $indexHash = (Get-FileHash -LiteralPath $indexPath).Hash
        @(& git --no-optional-locks -c core.fsmonitor= -c core.untrackedCache=true -C $fixture status --porcelain=v1 --untracked-files=all).Count | Should -Be 0
        @(& git --no-optional-locks -c core.untrackedCache=false -C $fixture status --porcelain=v1 --untracked-files=all) | Should -Contain ('?? ' + $hiddenName)
        $snapshot = Get-FixtureSnapshot -Paths @('AGENTS.md')
        $snapshot.Dirty | Should -BeTrue
        $snapshot.Paths | Should -Contain $hiddenName
        [string](& git -C $fixture config --get core.untrackedCache) | Should -BeExactly 'true'
        (Get-FileHash -LiteralPath $indexPath).Hash | Should -BeExactly $indexHash
    }

    It 'exposes staged changes concealed by a forged commit-graph root tree' {
        Invoke-FixtureGit @('config', 'core.commitGraph', 'true')
        Invoke-FixtureGit @('commit-graph', 'write', '--reachable')
        [IO.File]::WriteAllText((Join-Path $fixture 'packages/widget/model.txt'), 'synthetic staged payload')
        Invoke-FixtureGit @('add', 'packages/widget/model.txt')
        $stagedTree=[string](& git -C $fixture write-tree)
        $graphPath=Join-Path $fixture '.git/objects/info/commit-graph'
        $bytes=[IO.File]::ReadAllBytes($graphPath)
        # SHA-1 graph format: https://git-scm.com/docs/gitformat-commit-graph.
        [Text.Encoding]::ASCII.GetString($bytes, 0, 4) | Should -BeExactly 'CGPH'
        $bytes[4] | Should -Be 1
        $bytes[5] | Should -Be 1
        $dataOffset=-1
        for ($chunk=0; $chunk -lt $bytes[6]; $chunk++) {
            $tableOffset=8 + (12 * $chunk)
            if ([Text.Encoding]::ASCII.GetString($bytes, $tableOffset, 4) -ceq 'CDAT') {
                $dataOffset=[Convert]::ToInt64([BitConverter]::ToString($bytes, $tableOffset + 4, 8).Replace('-', ''), 16)
            }
        }
        $dataOffset | Should -BeGreaterThan 0
        [Array]::Copy([Convert]::FromHexString($stagedTree), 0, $bytes, $dataOffset, 20)
        $checksum=[Security.Cryptography.SHA1]::HashData([byte[]]$bytes[0..($bytes.Length - 21)])
        [Array]::Copy($checksum, 0, $bytes, $bytes.Length - 20, 20)
        [IO.File]::SetAttributes($graphPath, ([IO.File]::GetAttributes($graphPath) -band (-bnot [IO.FileAttributes]::ReadOnly)))
        [IO.File]::WriteAllBytes($graphPath, $bytes)
        @(& git --no-replace-objects --no-optional-locks -c core.fsmonitor= -c core.commitGraph=true -C $fixture status --porcelain=v1).Count | Should -Be 0
        @(& git -c core.commitGraph=false -C $fixture status --porcelain=v1).Count | Should -BeGreaterThan 0
        $snapshot=Get-FixtureSnapshot
        $snapshot.Dirty | Should -BeTrue
        $snapshot.Head | Should -BeExactly ([string](& git -C $fixture rev-parse HEAD))
        [string](& git -C $fixture config --get core.commitGraph) | Should -BeExactly 'true'
    }

    It 'rejects <Storage> replacement refs that conceal the ordinary Git view' -ForEach @(
        @{ Storage = 'loose'; Packed = $false },
        @{ Storage = 'packed'; Packed = $true }
    ) {
        $original = & git -C $fixture rev-parse HEAD
        Set-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'replacement tree'
        Invoke-FixtureGit @('add', '.')
        $tree = & git -C $fixture write-tree
        $replacement = & git -C $fixture -c user.name=Fixture -c user.email=fixture@example.invalid commit-tree $tree -m replacement
        Invoke-FixtureGit @('replace', $original, $replacement)
        if ($Packed) {
            Invoke-FixtureGit @('pack-refs', '--all')
            (Get-Content -Raw -LiteralPath (Join-Path $fixture '.git/packed-refs')) | Should -Match 'refs/replace/'
        }
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Git replacement refs require manual inspection*'
    }

    It 'rejects a shallow boundary that conceals existing parent ancestry' {
        $parent = [string](& git -C $fixture rev-parse HEAD)
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-m', 'child')
        $head = [string](& git -C $fixture rev-parse HEAD)
        [IO.File]::WriteAllText((Join-Path $fixture '.git/shallow'), $head + "`n")
        & git -C $fixture merge-base --is-ancestor $parent HEAD
        $LASTEXITCODE | Should -Be 1
        $emptyBoundary = Join-Path $TestDrive 'empty-shallow'
        [IO.File]::WriteAllText($emptyBoundary, '')
        & git --shallow-file $emptyBoundary -C $fixture merge-base --is-ancestor $parent HEAD
        $LASTEXITCODE | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*Shallow Git boundaries require manual inspection*'
        [IO.File]::ReadAllText((Join-Path $fixture '.git/shallow')) | Should -BeExactly ($head + "`n")
    }

    It 'rejects an active bisect despite empty status and a detached HEAD' {
        foreach ($number in 1..3) { Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '-m', "revision $number") }
        Invoke-FixtureGit @('bisect', 'start', 'HEAD', 'HEAD~3')
        @(& git --no-optional-locks -C $fixture status --porcelain=v1).Count | Should -Be 0
        [string](& git -C $fixture branch --show-current) | Should -BeNullOrEmpty
        { Get-FixtureSnapshot } | Should -Throw '*In-progress Git operations require manual recovery*BISECT_START*'
        Test-Path -LiteralPath (Join-Path $fixture '.git/BISECT_START') | Should -BeTrue
    }

    It 'rejects an index lock that prevents the next ordinary mutation' {
        $lock = Join-Path $fixture '.git/index.lock'
        [IO.File]::WriteAllText($lock, '')
        @(& git --no-optional-locks -C $fixture status --porcelain=v1).Count | Should -Be 0
        { Get-FixtureSnapshot } | Should -Throw '*In-progress Git operations require manual recovery*index.lock*'
        & git -C $fixture add AGENTS.md 2>&1 | Out-Null
        $LASTEXITCODE | Should -Not -Be 0
        Test-Path -LiteralPath $lock | Should -BeTrue
    }

    It 'rejects a <Kind> lock before reporting mutation readiness' -ForEach @(
        @{ Kind = 'branch ref'; Relative = 'refs/heads/release/trunk.lock' },
        @{ Kind = 'detached HEAD'; Relative = 'HEAD.lock' },
        @{ Kind = 'symbolic-chain target'; Relative = 'refs/heads/release/trunk.lock' },
        @{ Kind = 'packed refs'; Relative = 'packed-refs.lock' }
    ) {
        if ($Kind -eq 'detached HEAD') { Invoke-FixtureGit @('checkout', '--detach', 'HEAD') }
        if ($Kind -eq 'symbolic-chain target') {
            Invoke-FixtureGit @('symbolic-ref', 'refs/heads/alias', 'refs/heads/release/trunk')
            Invoke-FixtureGit @('symbolic-ref', 'HEAD', 'refs/heads/alias')
        }
        $lock = Join-Path $fixture ('.git/' + $Relative)
        [IO.File]::WriteAllText($lock, '')
        @(& git --no-optional-locks -C $fixture status --porcelain=v1).Count | Should -Be 0
        $head = ([string](& git -C $fixture rev-parse HEAD)).Trim()
        if ($Kind -eq 'packed refs') { & git -C $fixture pack-refs --all --prune 2>&1 | Out-Null }
        else { & git -C $fixture -c user.name=Fixture -c user.email=fixture@example.invalid commit --allow-empty -m 'owned lock control' 2>&1 | Out-Null }
        $LASTEXITCODE | Should -Not -Be 0
        ([string](& git -C $fixture rev-parse HEAD)).Trim() | Should -BeExactly $head
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Output | Should -BeNullOrEmpty
        $result.Error | Should -Match 'manual recovery before inspection'
        $result.Error | Should -Match ([regex]::Escape($Relative))
        Test-Path -LiteralPath $lock | Should -BeTrue
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
    }

    It 'rejects a live untracked instruction symlink to a <Kind> before returning inventory' -ForEach @(
        @{ Kind = 'FIFO' },
        @{ Kind = 'socket' }
    ) -Skip:$IsWindows {
        $target = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-special')
        $socket = $null
        if ($Kind -eq 'FIFO') { & mkfifo $target; $LASTEXITCODE | Should -Be 0 }
        else {
            $socket = [Net.Sockets.Socket]::new([Net.Sockets.AddressFamily]::Unix, [Net.Sockets.SocketType]::Stream, [Net.Sockets.ProtocolType]::Unspecified)
            $socket.Bind([Net.Sockets.UnixDomainSocketEndPoint]::new($target))
        }
        try {
            $instructions = Join-Path $fixture 'linked.instructions.md'
            $null = [IO.File]::CreateSymbolicLink($instructions, $target)
            { Get-FixtureSnapshot -Paths @('tools/verify.sh') } | Should -Throw '*Live linked inventory entries require manual discovery*'
        }
        finally { if ($null -ne $socket) { $socket.Dispose() } }
    }

    It 'rejects a missing file rather than reporting absent guidance as success' {
        { Get-FixtureSnapshot @('missing-policy.md') } | Should -Throw
    }

    It 'rejects paths outside the target repository' {
        { Get-FixtureSnapshot @('../outside.md') } | Should -Throw
        { Get-FixtureSnapshot @($snapshotScript) } | Should -Throw
    }

    It 'rejects a directory as selected instruction content' {
        { Get-FixtureSnapshot @('tools') } | Should -Throw
    }

    It 'rejects a Unix <Kind> before hashing without hanging' -Skip:$IsWindows -ForEach @(
        @{ Kind = 'named pipe' }, @{ Kind = 'socket' }
    ) {
        $path = Join-Path $fixture 'special'
        $socket = $null
        if ($Kind -eq 'named pipe') {
            & mkfifo $path
            $LASTEXITCODE | Should -Be 0
        }
        else {
            $socket = [Net.Sockets.Socket]::new([Net.Sockets.AddressFamily]::Unix, [Net.Sockets.SocketType]::Stream, [Net.Sockets.ProtocolType]::Unspecified)
            $socket.Bind([Net.Sockets.UnixDomainSocketEndPoint]::new($path))
        }
        $start = [Diagnostics.ProcessStartInfo]::new($shell)
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @('-NoProfile', '-File', $snapshotScript, '-RepositoryRoot', $fixture, '-ContextPath', 'special')) {
            $start.ArgumentList.Add($argument)
        }
        $start.Environment['GIT_CONFIG_NOSYSTEM'] = '1'
        $start.Environment['GIT_CONFIG_GLOBAL'] = Join-Path $fixture 'missing-global'
        $child = [Diagnostics.Process]::Start($start)
        try {
            $child.WaitForExit(10000) | Should -BeTrue
            $child.ExitCode | Should -Be 1
            $child.StandardError.ReadToEnd() | Should -Match 'Non-regular context files'
            $child.StandardOutput.ReadToEnd() | Should -BeNullOrEmpty
        }
        finally {
            if (-not $child.HasExited) { $child.Kill($true) }
            $child.Dispose()
            if ($null -ne $socket) { $socket.Dispose() }
        }
    }

    It 'bounds hashing when a regular file becomes a FIFO after path inspection' -Skip:$IsWindows {
        $copy = Join-Path $TestDrive 'fifo-race-snapshot.ps1'
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$metadata = Get-ContextFileHash $fullPath'
        $source.Contains($boundary) | Should -BeTrue
        $replacement = '[IO.File]::Delete($fullPath); & mkfifo -- $fullPath; ' + $boundary
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, $replacement))
        $result = Invoke-BoundedSnapshot -ScriptPath $copy
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context inspection timed out'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'hashes the validated open handle after its path is replaced' -Skip:$IsWindows {
        $copy = Join-Path $TestDrive 'handle-race-snapshot.ps1'
        $path = Join-Path $fixture 'AGENTS.md'
        $originalHash = (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()
        $source = [IO.File]::ReadAllText($snapshotScript)
        $boundary = '$hasher = [Security.Cryptography.SHA256]::Create()'
        $source.Contains($boundary) | Should -BeTrue
        $replacement = '[IO.File]::Move($HashPath, $HashPath + ''.opened''); [IO.File]::WriteAllText($HashPath, ''replacement after validation''); ' + $boundary
        [IO.File]::WriteAllText($copy, $source.Replace($boundary, $replacement))
        $output = & $shell -NoProfile -File $copy -RepositoryRoot $fixture -HashPath $path 2>&1 | Out-String
        $LASTEXITCODE | Should -Be 0
        ($output | ConvertFrom-Json).Sha256 | Should -BeExactly $originalHash
        (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant() | Should -Not -BeExactly $originalHash
    }

    It 'bounds Git reads of a FIFO at .git/<Metadata>' -Skip:$IsWindows -ForEach @(
        @{ Metadata = 'index' }, @{ Metadata = 'config' }
    ) {
        $path = Join-Path $fixture ".git/$Metadata"
        Remove-Item -LiteralPath $path
        & mkfifo $path
        $LASTEXITCODE | Should -Be 0
        $result = Invoke-BoundedSnapshot
        $result.ExitCode | Should -Be 1
        $result.Error | Should -Match 'Native context inspection timed out'
        $result.Output | Should -BeNullOrEmpty
    }

    It 'rejects successful Git traversal warnings from an unreadable directory' -Skip:$IsWindows {
        $permissionRoot = [IO.Path]::GetFullPath([IO.Path]::Combine([IO.Path]::GetTempPath(), [guid]::NewGuid().ToString('N') + '-permissions'))
        $null = [IO.Directory]::CreateDirectory($permissionRoot)
        $unreadable = Join-Path $permissionRoot 'unreadable'
        $null = [IO.Directory]::CreateDirectory($unreadable)
        [IO.File]::WriteAllText((Join-Path $permissionRoot 'AGENTS.md'), 'Permission fixture')
        [IO.File]::WriteAllText((Join-Path $unreadable 'hidden.md'), 'Untracked content')
        $rootUser = ([string](& id -u)).Trim() -eq '0'
        if ($rootUser -and $null -eq (Get-Command setpriv -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)) {
            Set-ItResult -Skipped -Because 'The root permission fixture requires the optional setpriv utility.'
            return
        }
        try {
            Invoke-FixtureGit @('-C', $permissionRoot, 'init', '-b', 'permissions')
            Invoke-FixtureGit @('-C', $permissionRoot, 'config', 'core.excludesFile', (Join-Path $permissionRoot 'missing-exclude'))
            Invoke-FixtureGit @('-C', $permissionRoot, 'add', 'AGENTS.md')
            Invoke-FixtureGit @('-C', $permissionRoot, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'permission baseline')
            if ($rootUser) {
                & chown -R 65534:65534 -- $permissionRoot
                $LASTEXITCODE | Should -Be 0
            }
            [IO.File]::SetUnixFileMode($unreadable, [IO.UnixFileMode]0)
            if ($rootUser) { $warning = & setpriv --reuid=65534 --regid=65534 --clear-groups git -C $permissionRoot status --porcelain=v1 --untracked-files=all 2>&1 | Out-String }
            else { $warning = & git -C $permissionRoot status --porcelain=v1 --untracked-files=all 2>&1 | Out-String }
            $LASTEXITCODE | Should -Be 0
            $warning | Should -Match 'Permission denied'
            $result = Invoke-BoundedSnapshot -Root $permissionRoot -Unprivileged:$rootUser
            $result.ExitCode | Should -Be 1
            $result.Error | Should -Match 'reported diagnostics'
            $result.Error | Should -Match 'unreadable/'
            $result.Error | Should -Match 'Permission denied'
            $result.Output | Should -BeNullOrEmpty
        }
        finally {
            [IO.File]::SetUnixFileMode($unreadable, [IO.UnixFileMode]493)
            [IO.Directory]::Delete($permissionRoot, $true)
        }
    }

    It 'rejects submodule entries before status can execute nested filters' {
        $source = Join-Path $TestDrive 'submodule-source'
        New-Item -ItemType Directory -Path $source | Out-Null
        Set-Content -LiteralPath (Join-Path $source 'probe.txt') -Value 'original'
        Set-Content -LiteralPath (Join-Path $source '.gitattributes') -Value '*.txt filter=probe'
        Invoke-FixtureGit @('-C', $source, 'init', '-b', 'fixture')
        Invoke-FixtureGit @('-C', $source, 'config', 'core.autocrlf', 'false')
        Invoke-FixtureGit @('-C', $source, 'add', '.')
        Invoke-FixtureGit @('-C', $source, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'submodule baseline')
        Invoke-FixtureGit @('-c', 'protocol.file.allow=always', 'submodule', 'add', $source, 'nested')
        $nested = Join-Path $fixture 'nested'
        $marker = Join-Path $fixture 'nested-filter-marker'
        $driver = Join-Path $TestDrive 'nested-filter.ps1'
        Set-Content -LiteralPath $driver -Value ("[IO.File]::WriteAllText('" + $marker.Replace("'", "''") + "', 'executed'); exit 1")
        $driverCommand = '"' + $shell + '" -NoProfile -File "' + $driver + '"'
        Invoke-FixtureGit @('-C', $nested, 'config', 'filter.probe.clean', $driverCommand)
        $probe = Join-Path $nested 'probe.txt'
        Set-Content -LiteralPath $probe -Value 'modified'
        [IO.File]::SetLastWriteTimeUtc($probe, [DateTime]::UtcNow.AddSeconds(5))
        { Get-FixtureSnapshot } | Should -Throw '*Submodule entries require manual inspection*'
        Test-Path -LiteralPath $marker | Should -BeFalse
        Invoke-FixtureGit @('--no-optional-locks', '-c', 'core.fsmonitor=', 'status', '--porcelain=v1')
        Test-Path -LiteralPath $marker | Should -BeTrue
    }

    It 'changes selected mode identity and overrides ignored execute-bit changes' -Skip:$IsWindows {
        Invoke-FixtureGit @('config', 'core.fileMode', 'false')
        $before = Get-FixtureSnapshot
        [IO.File]::SetUnixFileMode((Join-Path $fixture 'tools/verify.sh'), [IO.UnixFileMode]493)
        $after = Get-FixtureSnapshot
        $after.Dirty | Should -BeTrue
        $after.SelectedInputs[1].Type | Should -BeExactly 'File'
        $after.SelectedInputs[1].Sha256 | Should -BeExactly $before.SelectedInputs[1].Sha256
        $after.SelectedInputs[1].Mode | Should -Not -Be $before.SelectedInputs[1].Mode
    }

    It 'detects executable-bit changes outside selected context despite core.fileMode false' -Skip:$IsWindows {
        Invoke-FixtureGit @('config', 'core.fileMode', 'false')
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        [IO.File]::SetUnixFileMode((Join-Path $fixture 'packages/widget/model.txt'), [IO.UnixFileMode]493)
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
        ([string](& git -C $fixture config --get core.fileMode)) | Should -BeExactly 'false'
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
    }

    It 'rejects a selected mode change between observations' -Skip:$IsWindows {
        Invoke-FixtureGit @('config', 'core.fileMode', 'false')
        $scriptPath = (Join-Path $fixture 'tools/verify.sh').Replace("'", "''")
        $marker = Join-Path $TestDrive 'mode-mutation'
        $prelude = Get-SnapshotMutationPrelude -Marker $marker -Mutation "[IO.File]::SetUnixFileMode('$scriptPath', [IO.UnixFileMode]493)"
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*changed during context inspection*'
        Test-Path -LiteralPath $marker | Should -BeTrue
    }

    It 'preserves Git rejection of an untrusted repository owner' {
        $prelude = "`$env:GIT_TEST_ASSUME_DIFFERENT_OWNER = '1'; `$env:GIT_CONFIG_NOSYSTEM = '1'; " +
            "`$env:GIT_CONFIG_GLOBAL = '" + (Join-Path $fixture 'missing-global').Replace("'", "''") + "'; "
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*dubious ownership*'
    }

    It 'ignores a PowerShell function shadowing native Git' {
        $marker = (Join-Path $TestDrive 'git-wrapper-marker').Replace("'", "''")
        $prelude = "function global:git { [IO.File]::WriteAllText('$marker', 'executed'); throw 'Shadowed Git executed' }; "
        (Get-FixtureSnapshot -Prelude $prelude).Dirty | Should -BeFalse
        Test-Path -LiteralPath $marker | Should -BeFalse
    }

    It 'never executes commands embedded in target instructions' {
        Set-Content -LiteralPath (Join-Path $fixture 'AGENTS.md') -Value 'New-Item escaped-execution.txt'
        $null = Get-FixtureSnapshot
        Test-Path -LiteralPath (Join-Path $fixture 'escaped-execution.txt') | Should -BeFalse
    }

    It 'does not run a configured filesystem-monitor hook or refresh the index' {
        $hook = Join-Path $fixture 'fsmonitor-hook'
        [IO.File]::WriteAllText($hook, "#!/bin/sh`nprintf invoked > escaped-hook.txt`nprintf 'fixture\0/\0'`n")
        if (-not $IsWindows) { [IO.File]::SetUnixFileMode($hook, [IO.UnixFileMode]493) }
        Invoke-FixtureGit @('config', 'core.fsmonitor', $hook.Replace('\', '/'))
        $indexHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash
        $null = Get-FixtureSnapshot
        Test-Path -LiteralPath (Join-Path $fixture 'escaped-hook.txt') | Should -BeFalse
        (Get-FileHash -LiteralPath (Join-Path $fixture '.git/index')).Hash | Should -BeExactly $indexHash
        Invoke-FixtureGit @('status', '--porcelain=v1')
        Test-Path -LiteralPath (Join-Path $fixture 'escaped-hook.txt') | Should -BeTrue
    }

    It 'rejects <Scope> <Kind> filters before executing content drivers' -ForEach @(
        @{ Kind = 'clean'; Scope = 'local' },
        @{ Kind = 'process'; Scope = 'local' },
        @{ Kind = 'clean'; Scope = 'inherited' }
    ) {
        Set-Content -LiteralPath (Join-Path $fixture '.gitattributes') -Value '* filter=probe'
        Invoke-FixtureGit @('add', '.gitattributes')
        $marker = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-filter-executed.txt')
        $driver = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-filter.ps1')
        Set-Content -LiteralPath $driver -Value ("Set-Content -LiteralPath '" + $marker.Replace("'", "''") + "' -Value 'executed'; exit 1")
        $filterCommand = '"' + $shell.Replace('\', '/') + '" -NoProfile -File "' + $driver.Replace('\', '/') + '"'
        $key = "filter.probe.$Kind"
        $prelude = ''
        if ($Scope -eq 'local') { Invoke-FixtureGit @('config', $key, $filterCommand) }
        else {
            $configPath = Join-Path $TestDrive 'inherited-filter.cfg'
            Invoke-FixtureGit @('config', '--file', $configPath, $key, $filterCommand)
            $prelude = "`$env:GIT_CONFIG_GLOBAL = '" + $configPath.Replace("'", "''") + "'; "
        }
        $inputPath = Join-Path $fixture 'AGENTS.md'
        $inputText = [IO.File]::ReadAllText($inputPath)
        [IO.File]::WriteAllText($inputPath, '!' + $inputText.Substring(1))
        (Get-Item -LiteralPath $inputPath).LastWriteTimeUtc = [datetime]::UtcNow.AddSeconds(3)
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*filters require manual inspection*'
        Test-Path -LiteralPath $marker | Should -BeFalse
        & git --no-optional-locks -c core.fsmonitor= -c "$key=$filterCommand" -C $fixture status --porcelain=v1 2>&1 | Out-Null
        Test-Path -LiteralPath $marker | Should -BeTrue
    }

    It 'rejects GIT_CONFIG before a concealed local filter can execute' {
        [IO.File]::WriteAllText((Join-Path $fixture '.gitattributes'), "*.txt filter=probe`n")
        Invoke-FixtureGit @('add', '.gitattributes')
        $marker = Join-Path $TestDrive 'concealed-filter.marker'
        $driver = Join-Path $TestDrive 'concealed-filter.ps1'
        [IO.File]::WriteAllText($driver, "[IO.File]::WriteAllText('$($marker.Replace("'", "''"))', 'executed'); exit 1")
        $command = '"' + $shell.Replace('\', '/') + '" -NoProfile -File "' + $driver.Replace('\', '/') + '"'
        Invoke-FixtureGit @('config', 'filter.probe.clean', $command)
        $modelPath = Join-Path $fixture 'packages/widget/model.txt'
        $modelText = [IO.File]::ReadAllText($modelPath)
        [IO.File]::WriteAllText($modelPath, '!' + $modelText.Substring(1))
        [IO.File]::SetLastWriteTimeUtc($modelPath, [datetime]::UtcNow.AddSeconds(3))
        $alternate = Join-Path $TestDrive 'empty-alternate-config'
        [IO.File]::WriteAllText($alternate, '')
        $prelude = "`$env:GIT_CONFIG = '$($alternate.Replace("'", "''"))'; "
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*Ambient Git override GIT_CONFIG*'
        [IO.File]::Exists($marker) | Should -BeFalse
        $previous = [Environment]::GetEnvironmentVariable('GIT_CONFIG')
        try {
            $env:GIT_CONFIG = $alternate
            & git -C $fixture config --name-only --get-regexp '^filter\..*\.(clean|process)$' | Out-Null
            $LASTEXITCODE | Should -Be 1
            $controlOutput = & git --no-optional-locks -c core.fsmonitor= -C $fixture status --porcelain=v1 2>&1 | Out-String
            if (-not [IO.File]::Exists($marker)) { throw "Concealed filter positive control did not execute: $controlOutput" }
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath Env:GIT_CONFIG -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable('GIT_CONFIG', $previous) }
        }
    }

    It 'rejects concurrent <MutationCase> with an unchanged HEAD' -ForEach @(
        @{ MutationCase = 'clean input edit' },
        @{ MutationCase = 'already dirty input edit' },
        @{ MutationCase = 'branch change' },
        @{ MutationCase = 'index-only change' }
    ) {
        $inputPath = Join-Path $fixture 'AGENTS.md'
        $modelPath = Join-Path $fixture 'packages/widget/model.txt'
        $quotedRoot = $fixture.Replace("'", "''")
        $mutation = "Set-Content -LiteralPath '" + $inputPath.Replace("'", "''") + "' -Value 'changed during inspection'"
        if ($MutationCase -eq 'already dirty input edit') {
            Set-Content -LiteralPath $inputPath -Value 'dirty before inspection'
            (Get-FixtureSnapshot).Dirty | Should -BeTrue
        }
        elseif ($MutationCase -eq 'branch change') {
            Invoke-FixtureGit @('branch', 'case/alternate')
            $mutation = "& `$global:snapshotRealGit -C '$quotedRoot' symbolic-ref HEAD refs/heads/case/alternate | Out-Null"
        }
        elseif ($MutationCase -eq 'index-only change') {
            Set-Content -LiteralPath $modelPath -Value 'staged before inspection'
            Invoke-FixtureGit @('add', 'packages/widget/model.txt')
            Set-Content -LiteralPath $modelPath -Value 'working content stays unchanged'
            $replacementPath = Join-Path $TestDrive 'replacement-blob.txt'
            Set-Content -LiteralPath $replacementPath -Value 'replacement staged content'
            $blob = & git -C $fixture hash-object -w $replacementPath
            $LASTEXITCODE | Should -Be 0
            $mutation = "& `$global:snapshotRealGit -C '$quotedRoot' update-index --cacheinfo '100644,$blob,packages/widget/model.txt' | Out-Null"
        }
        $headBefore = & git -C $fixture rev-parse HEAD
        $statusBefore = @(& git -C $fixture status --porcelain=v1) -join "`n"
        $indexBefore = @(& git -C $fixture ls-files --stage) -join "`n"
        $inputHashBefore = (Get-FileHash -LiteralPath $inputPath).Hash
        $modelHashBefore = (Get-FileHash -LiteralPath $modelPath).Hash
        $marker = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '-snapshot-mutated.txt')
        $prelude = Get-SnapshotMutationPrelude -Mutation $mutation -Marker $marker
        $failure = ''
        try { $null = Get-FixtureSnapshot -Prelude $prelude }
        catch { $failure = $_.Exception.Message }
        $failure | Should -BeLike '*changed during context inspection*'
        $failure | Should -Not -Match '"SchemaVersion"'
        Test-Path -LiteralPath $marker | Should -BeTrue
        (& git -C $fixture rev-parse HEAD) | Should -BeExactly $headBefore
        $statusAfter = @(& git -C $fixture status --porcelain=v1) -join "`n"
        if ($MutationCase -eq 'clean input edit') {
            $statusAfter | Should -Not -BeExactly $statusBefore
        }
        else { $statusAfter | Should -BeExactly $statusBefore }
        if ($MutationCase -in @('clean input edit', 'already dirty input edit')) {
            (Get-FileHash -LiteralPath $inputPath).Hash | Should -Not -BeExactly $inputHashBefore
        }
        elseif ($MutationCase -eq 'branch change') {
            (& git -C $fixture branch --show-current) | Should -BeExactly 'case/alternate'
        }
        else {
            (@(& git -C $fixture ls-files --stage) -join "`n") | Should -Not -BeExactly $indexBefore
            (Get-FileHash -LiteralPath $inputPath).Hash | Should -BeExactly $inputHashBefore
            (Get-FileHash -LiteralPath $modelPath).Hash | Should -BeExactly $modelHashBefore
        }
    }

    It 'rejects linked directories instead of reading context from another repository' {
        $outside = Join-Path $TestDrive 'outside'
        New-Item -ItemType Directory -Path $outside -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $outside 'policy.md') -Value 'Foreign context'
        $linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
        New-Item -ItemType $linkType -Path (Join-Path $fixture 'linked') -Target $outside | Out-Null
        { Get-FixtureSnapshot @('linked/policy.md') } | Should -Throw
    }

    It 'rejects a literal-pathspec override that hides ignored scoped guidance' {
        $ignored = Join-Path $fixture 'ignored'
        $null = [IO.Directory]::CreateDirectory($ignored)
        [IO.File]::WriteAllText((Join-Path $ignored 'AGENTS.md'), 'Ignored scoped guidance')
        [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), "ignored/`n")
        Invoke-FixtureGit @('add', '.gitignore')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'owned ignored guidance')
        $arguments = @('ls-files', '--others', '--ignored', '--exclude-standard', '--', ':(glob)**/AGENTS.md')
        @(& git -C $fixture @arguments) | Should -Contain 'ignored/AGENTS.md'
        (Get-FixtureSnapshot).Paths | Should -Contain 'ignored/AGENTS.md'
        $previous = [Environment]::GetEnvironmentVariable('GIT_LITERAL_PATHSPECS')
        try {
            [Environment]::SetEnvironmentVariable('GIT_LITERAL_PATHSPECS', '1')
            @(& git -C $fixture @arguments).Count | Should -Be 0
            { Get-FixtureSnapshot } | Should -Throw '*Ambient Git override GIT_LITERAL_PATHSPECS*'
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath Env:GIT_LITERAL_PATHSPECS -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable('GIT_LITERAL_PATHSPECS', $previous) }
        }
    }

    It 'rejects an attribute-source override that conceals a tracked CRLF difference' {
        $model = Join-Path $fixture 'packages/widget/model.txt'
        $attributes = Join-Path $fixture '.gitattributes'
        [IO.File]::WriteAllText($model, "baseline`r`n")
        [IO.File]::WriteAllText($attributes, "packages/widget/model.txt text eol=lf`n")
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('add', '--renormalize', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'owned normalization policy')
        $attributeSource = ([string](& git -C $fixture rev-parse HEAD)).Trim()
        $expected = ([string](& git -C $fixture rev-parse HEAD:packages/widget/model.txt)).Trim()
        [IO.File]::WriteAllText($attributes, "packages/widget/model.txt -text`n")
        Invoke-FixtureGit @('add', '.gitattributes')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'owned raw-content policy')
        [IO.File]::WriteAllText($model, "baseline`r`n")
        [IO.File]::SetLastWriteTimeUtc($model, [DateTime]::UtcNow.AddSeconds(2))
        ([string](& git -C $fixture hash-object -- packages/widget/model.txt)).Trim() | Should -Not -BeExactly $expected
        @(& git --no-optional-locks -C $fixture status --porcelain=v1).Count | Should -BeGreaterThan 0
        (Get-FixtureSnapshot).Dirty | Should -BeTrue
        $previous = [Environment]::GetEnvironmentVariable('GIT_ATTR_SOURCE')
        try {
            [Environment]::SetEnvironmentVariable('GIT_ATTR_SOURCE', $attributeSource)
            ([string](& git -C $fixture hash-object -- packages/widget/model.txt)).Trim() | Should -BeExactly $expected
            @(& git --no-optional-locks -C $fixture status --porcelain=v1).Count | Should -Be 0
            { Get-FixtureSnapshot } | Should -Throw '*Ambient Git override GIT_ATTR_SOURCE*'
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath Env:GIT_ATTR_SOURCE -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable('GIT_ATTR_SOURCE', $previous) }
        }
    }

    It 'rejects an ambient <Selector> instead of silently inspecting another target' -ForEach @(
        @{ Selector = 'GIT_DIR' },
        @{ Selector = 'GIT_WORK_TREE' },
        @{ Selector = 'GIT_COMMON_DIR' },
        @{ Selector = 'GIT_INDEX_FILE' },
        @{ Selector = 'GIT_CONFIG' },
        @{ Selector = 'GIT_REPLACE_REF_BASE' },
        @{ Selector = 'GIT_SHALLOW_FILE' },
        @{ Selector = 'GIT_GRAFT_FILE' },
        @{ Selector = 'GIT_LITERAL_PATHSPECS' },
        @{ Selector = 'GIT_GLOB_PATHSPECS' },
        @{ Selector = 'GIT_NOGLOB_PATHSPECS' },
        @{ Selector = 'GIT_ICASE_PATHSPECS' },
        @{ Selector = 'GIT_ATTR_SOURCE' }
    ) {
        $previous = [Environment]::GetEnvironmentVariable($Selector)
        try {
            [Environment]::SetEnvironmentVariable($Selector, $repositoryRoot)
            { Get-FixtureSnapshot } | Should -Throw "*Ambient Git override $Selector prevents reliable target inspection*"
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath "Env:$Selector" -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable($Selector, $previous) }
        }
    }
}
