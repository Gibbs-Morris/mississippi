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
        $command = $configuration + $Prelude + "& '" + $snapshotScript.Replace("'", "''") + "' -RepositoryRoot '" +
            $Root.Replace("'", "''") + "' -ContextPath @(" + $quotedPaths + ')'
        $result = & $shell -NoProfile -Command $command 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw $result }
        return $result | ConvertFrom-Json
    }

    function Get-SnapshotMutationPrelude {
        param([string]$Mutation, [string]$Marker)
        $realGit = (Get-Command git -CommandType Application | Select-Object -First 1).Source.Replace("'", "''")
        $markerPath = $Marker.Replace("'", "''")
        return @"
`$global:snapshotRealGit = '$realGit'
`$global:snapshotHeadReads = 0
`$global:snapshotMutation = { Set-Content -LiteralPath '$markerPath' -Value 'mutated'; $Mutation }
function global:git {
    `$gitArguments = @(`$args)
    if (`$gitArguments -contains 'rev-parse' -and `$gitArguments -contains '--verify' -and `$gitArguments -contains 'HEAD') {
        `$global:snapshotHeadReads++
        if (`$global:snapshotHeadReads -eq 2) { & `$global:snapshotMutation }
    }
    `$result = @(& `$global:snapshotRealGit @gitArguments)
    `$global:LASTEXITCODE = `$LASTEXITCODE
    `$result
}
"@ + "`n"
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

    It 'records a new head rather than reusing evidence from the baseline' {
        $before = Get-FixtureSnapshot
        Add-Content -LiteralPath (Join-Path $fixture 'packages/widget/model.txt') -Value 'changed'
        Invoke-FixtureGit @('add', '.')
        Invoke-FixtureGit @('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-m', 'change')
        (Get-FixtureSnapshot).Head | Should -Not -BeExactly $before.Head
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

    It 'preserves Git rejection of an untrusted repository owner' {
        $prelude = "`$env:GIT_TEST_ASSUME_DIFFERENT_OWNER = '1'; `$env:GIT_CONFIG_NOSYSTEM = '1'; " +
            "`$env:GIT_CONFIG_GLOBAL = '" + (Join-Path $fixture 'missing-global').Replace("'", "''") + "'; "
        { Get-FixtureSnapshot -Prelude $prelude } | Should -Throw '*dubious ownership*'
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
