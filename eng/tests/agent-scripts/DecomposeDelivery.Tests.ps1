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
        param([string[]]$Paths = @('AGENTS.md', 'tools/verify.sh'), [string]$Prelude = '')
        $quotedPaths = @($Paths | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ','
        $command = $Prelude + "& '" + $snapshotScript.Replace("'", "''") + "' -RepositoryRoot '" +
            $fixture.Replace("'", "''") + "' -ContextPath @(" + $quotedPaths + ')'
        $result = & $shell -NoProfile -Command $command 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw $result }
        return $result | ConvertFrom-Json
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
            $output = & $shell -NoProfile -File $snapshotScript -RepositoryRoot . -ContextPath AGENTS.md | ConvertFrom-Json
            $LASTEXITCODE | Should -Be 0
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
        $after.Dirty | Should -BeTrue
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
