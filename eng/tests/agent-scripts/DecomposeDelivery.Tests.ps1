#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
    $package = Join-Path $repositoryRoot '.agents/skills/decompose-and-deliver'
    $snapshotScript = Join-Path $package 'scripts/snapshot-context.ps1'
    $shell = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })

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
        [IO.File]::WriteAllText($injected, $source.Replace($boundary, "& `$global:snapshotMutation`n    " + $boundary))
        return @"
`$global:snapshotRealGit = '$realGit'
`$global:snapshotMutation = { Set-Content -LiteralPath '$markerPath' -Value 'mutated'; $Mutation }
`$snapshotInvocationPath = '$($injected.Replace("'", "''"))'
"@ + "`n"
    }

    function Invoke-BoundedSnapshot {
        param([string]$Root = $fixture, [string]$Path = 'AGENTS.md')
        $start = [Diagnostics.ProcessStartInfo]::new($shell)
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @('-NoProfile', '-File', $snapshotScript, '-RepositoryRoot', $Root, '-ContextPath', $Path)) { $start.ArgumentList.Add($argument) }
        $start.Environment['GIT_CONFIG_NOSYSTEM'] = '1'
        $start.Environment['GIT_CONFIG_GLOBAL'] = Join-Path $fixture 'missing-global'
        $child = [Diagnostics.Process]::Start($start)
        try {
            $output = $child.StandardOutput.ReadToEndAsync()
            $errorOutput = $child.StandardError.ReadToEndAsync()
            if (-not $child.WaitForExit(15000)) { throw 'Snapshot exceeded its outer fixture timeout.' }
            return [pscustomobject]@{ ExitCode = $child.ExitCode; Output = $output.GetAwaiter().GetResult(); Error = $errorOutput.GetAwaiter().GetResult() }
        }
        finally {
            if (-not $child.HasExited) { $child.Kill($true) }
            $child.Dispose()
        }
    }
}

Describe 'Portable delivery context snapshots' {
    BeforeEach {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $fixture 'tools'),(Join-Path $fixture 'packages/widget') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $fixture 'AGENTS.md') -Value '# Target instructions: bash tools/verify.sh; branch release/trunk'
        Set-Content -LiteralPath (Join-Path $fixture 'tools/verify.sh') -Value 'test -f packages/widget/model.txt'
        Set-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'baseline'
        Invoke-FixtureGit @('init', '-b', 'release/trunk')
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

    It 'resolves a nested target independently of the current directory and package directory' {
        Push-Location (Join-Path $fixture 'packages/widget')
        try {
            $output = Get-FixtureSnapshot -Root . -Paths @('AGENTS.md')
            $output.RepositoryRoot | Should -BeExactly ([IO.Path]::GetFullPath($fixture))
        }
        finally { Pop-Location }
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

    It 'preserves case-distinct paths on a case-sensitive filesystem' -Skip:$IsWindows {
        Set-Content -LiteralPath (Join-Path $fixture 'tools/Foo.md') -Value 'uppercase'
        Set-Content -LiteralPath (Join-Path $fixture 'tools/foo.md') -Value 'lowercase'
        Invoke-FixtureGit @('add', 'tools/Foo.md', 'tools/foo.md')
        $snapshot = Get-FixtureSnapshot
        @($snapshot.Paths | Where-Object { $_ -ceq 'tools/Foo.md' }).Count | Should -Be 1
        @($snapshot.Paths | Where-Object { $_ -ceq 'tools/foo.md' }).Count | Should -Be 1
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
        if (-not $IsWindows) {
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
        [IO.File]::SetLastWriteTimeUtc($path, [DateTime]::UtcNow.AddSeconds(-10))
        Invoke-FixtureGit @('update-index', '--refresh')
        $stamp = (Get-Item -LiteralPath $path).LastWriteTimeUtc
        # Git builds can compare ctime at whole-second precision.
        Start-Sleep -Milliseconds 1200
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

    It 'records a new head rather than reusing evidence from the baseline' {
        $before = Get-FixtureSnapshot
        Add-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'changed'
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'change')
        (Get-FixtureSnapshot).Head | Should -Not -BeExactly $before.Head
    }

    It 'binds dirty status to the reported commit despite replacement refs' {
        $original = & git -C $fixture rev-parse HEAD
        Set-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'replacement tree'
        Invoke-FixtureGit @('add', '.')
        $tree = & git -C $fixture write-tree
        $replacement = & git -C $fixture -c user.name=Fixture -c user.email=fixture@example.invalid commit-tree $tree -m replacement
        Invoke-FixtureGit @('replace', $original, $replacement)
        @(& git -C $fixture status --porcelain=v1).Count | Should -Be 0
        $snapshot = Get-FixtureSnapshot
        $snapshot.Head | Should -BeExactly $original
        $snapshot.Dirty | Should -BeTrue
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

    It 'changes selected mode identity when Git ignores execute-bit changes' -Skip:$IsWindows {
        Invoke-FixtureGit @('config', 'core.fileMode', 'false')
        $before = Get-FixtureSnapshot
        [IO.File]::SetUnixFileMode((Join-Path $fixture 'tools/verify.sh'), [IO.UnixFileMode]493)
        $after = Get-FixtureSnapshot
        $after.Dirty | Should -BeFalse
        $after.SelectedInputs[1].Type | Should -BeExactly 'File'
        $after.SelectedInputs[1].Sha256 | Should -BeExactly $before.SelectedInputs[1].Sha256
        $after.SelectedInputs[1].Mode | Should -Not -Be $before.SelectedInputs[1].Mode
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

    It 'rejects an ambient <Selector> instead of silently inspecting another target' -ForEach @(
        @{ Selector = 'GIT_DIR' },
        @{ Selector = 'GIT_WORK_TREE' },
        @{ Selector = 'GIT_COMMON_DIR' },
        @{ Selector = 'GIT_INDEX_FILE' }
    ) {
        $previous = [Environment]::GetEnvironmentVariable($Selector)
        try {
            [Environment]::SetEnvironmentVariable($Selector, $repositoryRoot)
            { Get-FixtureSnapshot } | Should -Throw '*Ambient Git override*'
        }
        finally {
            if ($null -eq $previous) { Remove-Item -LiteralPath "Env:$Selector" -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable($Selector, $previous) }
        }
    }
}
