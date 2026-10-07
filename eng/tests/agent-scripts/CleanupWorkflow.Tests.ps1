#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Cleanup workflow candidate scope' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/cleanup.yml') -Raw
        $match = [regex]::Match($workflow, '(?ms)      - name: Run ReSharper Cleanup Code\r?\n        shell: pwsh\r?\n        run: \|\r?\n(?<script>.*?)(?=\r?\n      - name:)')
        if (-not $match.Success) { throw 'Unable to extract the actual cleanup workflow script.' }
        $cleanupScript = ($match.Groups['script'].Value -split '\r?\n' | ForEach-Object { $_ -replace '^          ', '' }) -join "`n"
        $cleanupScript = $cleanupScript.Replace('${{ github.workspace }}', '$env:GITHUB_WORKSPACE').Replace('${{ matrix.solution }}', 'fixture.slnx').Replace('${{ env.DOTSETTINGS_PATH }}', 'Directory.DotSettings').Replace('${{ runner.temp }}', '$env:RUNNER_TEMP')
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })

        function Invoke-FixtureGit {
            param([Parameter(Mandatory)][string[]]$Arguments)
            $output = & git -c "safe.directory=$($fixtureRoot.Replace('\', '/'))" -C $fixtureRoot @Arguments 2>&1
            if ($LASTEXITCODE -ne 0) { throw "Fixture git failed: $output" }
            return $output
        }

        function Add-FixtureCommit {
            param([Parameter(Mandatory)][string]$Path, [string]$Content = 'fixture')
            $destination = Join-Path $fixtureRoot $Path
            New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
            Set-Content -LiteralPath $destination -Value $Content -Encoding utf8
            $null = Invoke-FixtureGit -Arguments @('add', '--', $Path)
            $null = Invoke-FixtureGit -Arguments @('commit', '--quiet', '-m', "Change $Path")
            return (Invoke-FixtureGit -Arguments @('rev-parse', 'HEAD')).Trim()
        }

        function Invoke-CleanupFixture {
            param(
                [ValidateSet('merge_group', 'pull_request', 'workflow_dispatch')][string]$Event = 'merge_group',
                [string]$Base = $baseSha,
                [string]$Head = $candidateSha,
                [string]$EventSha = $candidateSha,
                [string]$BaseRef = 'refs/heads/main',
                [int]$ToolExitCode = 0
            )
            $payload = if ($Event -eq 'pull_request') {
                @{ pull_request = @{ base = @{ sha = $Base }; head = @{ sha = $Head } } }
            } else {
                @{ merge_group = @{ base_sha = $Base; head_sha = $Head; base_ref = $BaseRef } }
            }
            $eventPath = Join-Path $fixtureRoot 'event.json'
            $payload | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $eventPath -Encoding utf8
            $logPath = Join-Path $fixtureRoot 'tool-arguments.json'
            Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
            $wrapperPath = Join-Path $fixtureRoot 'invoke-cleanup.ps1'
            $wrapper = @'
param([string]$Workspace, [string]$EventName, [string]$EventSha, [int]$ToolExitCode)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$env:GITHUB_WORKSPACE = $Workspace
$env:RUNNER_TEMP = $Workspace
$env:GITHUB_EVENT_NAME = $EventName
$env:GITHUB_SHA = $EventSha
$env:GITHUB_EVENT_PATH = Join-Path $Workspace 'event.json'
$env:GITHUB_ENV = Join-Path $Workspace 'github-env.txt'
$env:FIXTURE_TOOL_EXIT_CODE = $ToolExitCode
Set-Location -LiteralPath $Workspace
function dotnet {
    ConvertTo-Json -InputObject ([object[]]$args) -Compress | Set-Content -LiteralPath (Join-Path $env:GITHUB_WORKSPACE 'tool-arguments.json')
    $global:LASTEXITCODE = [int]$env:FIXTURE_TOOL_EXIT_CODE
}
'@
            $wrapper += "`n$cleanupScript`nif (Test-Path -LiteralPath variable:/LASTEXITCODE) { exit `$LASTEXITCODE }`nexit 0"
            Set-Content -LiteralPath $wrapperPath -Value $wrapper -Encoding utf8
            $output = & $powerShellPath -NoProfile -File $wrapperPath -Workspace $fixtureRoot -EventName $Event -EventSha $EventSha -ToolExitCode $ToolExitCode 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
            $arguments = @(if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw | ConvertFrom-Json })
            [pscustomobject]@{ ExitCode = $exitCode; Output = $output; Arguments = $arguments }
        }
    }

    BeforeEach {
        $fixtureRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        $null = Invoke-FixtureGit -Arguments @('init', '--quiet', '--initial-branch=main')
        $null = Invoke-FixtureGit -Arguments @('config', 'user.name', 'Cleanup fixture')
        $null = Invoke-FixtureGit -Arguments @('config', 'user.email', 'cleanup@example.invalid')
        $null = Invoke-FixtureGit -Arguments @('config', 'commit.gpgsign', 'false')
        $baseSha = Add-FixtureCommit -Path 'base.txt'
        $null = Invoke-FixtureGit -Arguments @('update-ref', 'refs/remotes/origin/main', $baseSha)
        $predecessorSha = Add-FixtureCommit -Path 'src/predecessor.cs'
        $candidateSha = Add-FixtureCommit -Path 'docs/follower.md'
    }

    It 'requests both required cleanup checks for main merge groups without cancelling candidates' {
        $workflow | Should -Match '(?ms)^  merge_group:\r?\n    types: \[checks_requested\]\r?\n    branches: \[main, ''codex/merge-queue/pilot-20261007''\]'
        $workflow | Should -Match "cancel-in-progress: \$\{\{ github.event_name == 'pull_request' \}\}"
        $workflow | Should -Match 'solution: \[ mississippi.slnx, samples.slnx \]'
    }

    It 'checks predecessor code when the last queue entry changes only documentation' {
        $result = Invoke-CleanupFixture -Base $predecessorSha
        $result.ExitCode | Should -Be 0
        $result.Arguments | Should -Contain '--include=src/predecessor.cs'
    }

    It 'includes every existing changed code path across multiple queue entries' {
        $candidateSha = Add-FixtureCommit -Path 'src/with space café.cs'
        $result = Invoke-CleanupFixture -Base $predecessorSha
        $result.ExitCode | Should -Be 0
        $include = @($result.Arguments | Where-Object { $_ -like '--include=*' })
        $include.Count | Should -Be 1
        ($include[0] -replace '^--include=', '' -split ';') | Should -Contain 'src/predecessor.cs'
        ($include[0] -replace '^--include=', '' -split ';') | Should -Contain 'src/with space café.cs'
    }

    It 'rejects a payload candidate different from the checked out commit' {
        $result = Invoke-CleanupFixture -Head $predecessorSha -EventSha $predecessorSha
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects a GitHub event SHA different from the payload candidate' {
        $result = Invoke-CleanupFixture -EventSha $predecessorSha
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects an empty candidate base before skipping cleanup' {
        $result = Invoke-CleanupFixture -Base ''
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects an unavailable base object' {
        $result = Invoke-CleanupFixture -Base ('1' * 40)
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects a base that is not an ancestor of the candidate' {
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', '-b', 'unrelated-base', $baseSha)
        $unrelatedSha = Add-FixtureCommit -Path 'unrelated.cs'
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', 'main')
        $result = Invoke-CleanupFixture -Base $unrelatedSha
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'preserves PR three-dot scope when the target branch has moved' {
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', '-b', 'feature', $baseSha)
        $prHead = Add-FixtureCommit -Path 'src/own.cs'
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', 'main')
        $prBase = (Invoke-FixtureGit -Arguments @('rev-parse', 'HEAD')).Trim()
        $null = Invoke-FixtureGit -Arguments @('merge', '--quiet', '--no-ff', 'feature', '-m', 'Synthetic PR merge')
        $result = Invoke-CleanupFixture -Event pull_request -Base $prBase -Head $prHead
        $result.ExitCode | Should -Be 0
        $result.Arguments | Should -Contain '--include=src/own.cs'
        $result.Arguments | Should -Not -Contain '--include=src/predecessor.cs'
    }

    It 'preserves full cleanup for manual runs' {
        $result = Invoke-CleanupFixture -Event workflow_dispatch
        $result.ExitCode | Should -Be 0
        @($result.Arguments | Where-Object { $_ -like '--include=*' }).Count | Should -Be 0
        $result.Arguments | Should -Contain 'cleanupcode'
    }

    It 'allows a proven documentation-only candidate without running CleanupCode' {
        $null = Invoke-FixtureGit -Arguments @('update-ref', 'refs/remotes/origin/main', $predecessorSha)
        $result = Invoke-CleanupFixture -Base $predecessorSha
        $result.ExitCode | Should -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'keeps predecessor scope when the target branch advances on a different line' {
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', '-b', 'target-advanced', $baseSha)
        $advancedTarget = Add-FixtureCommit -Path 'src/main-only.cs'
        $null = Invoke-FixtureGit -Arguments @('update-ref', 'refs/remotes/origin/main', $advancedTarget)
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', 'main')
        $result = Invoke-CleanupFixture -Base $predecessorSha
        $result.ExitCode | Should -Be 0
        $result.Arguments | Should -Contain '--include=src/predecessor.cs'
        $result.Arguments | Should -Not -Contain '--include=src/main-only.cs'
    }

    It 'rejects an unavailable target branch instead of assuming its scope' {
        $null = Invoke-FixtureGit -Arguments @('update-ref', '-d', 'refs/remotes/origin/main')
        $result = Invoke-CleanupFixture
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects an invalid target branch ref' {
        $result = Invoke-CleanupFixture -BaseRef 'refs/heads/main~1'
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects a target outside the branch namespace' {
        $result = Invoke-CleanupFixture -BaseRef 'refs/tags/main'
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects an unrelated target branch history' {
        $tree = (Invoke-FixtureGit -Arguments @('rev-parse', 'HEAD^{tree}')).Trim()
        $unrelatedTarget = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-m', 'Unrelated target')).Trim()
        $null = Invoke-FixtureGit -Arguments @('update-ref', 'refs/remotes/origin/main', $unrelatedTarget)
        $result = Invoke-CleanupFixture
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects ambiguous common ancestors instead of choosing a partial scope' {
        $tree = (Invoke-FixtureGit -Arguments @('rev-parse', 'HEAD^{tree}')).Trim()
        $left = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-p', $baseSha, '-m', 'Left')).Trim()
        $right = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-p', $baseSha, '-m', 'Right')).Trim()
        $parent = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-p', $left, '-p', $right, '-m', 'Queue parent')).Trim()
        $target = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-p', $right, '-p', $left, '-m', 'Target')).Trim()
        $candidateSha = (Invoke-FixtureGit -Arguments @('commit-tree', $tree, '-p', $parent, '-m', 'Candidate')).Trim()
        $null = Invoke-FixtureGit -Arguments @('checkout', '--quiet', '-B', 'main', $candidateSha)
        $null = Invoke-FixtureGit -Arguments @('update-ref', 'refs/remotes/origin/main', $target)
        @(Invoke-FixtureGit -Arguments @('merge-base', '--all', $target, $parent)).Count | Should -Be 2
        $result = Invoke-CleanupFixture -Base $parent
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'rejects paths that cannot be represented as exact CleanupCode includes' {
        $candidateSha = Add-FixtureCommit -Path 'src/unsafe;path.cs'
        $result = Invoke-CleanupFixture
        $result.ExitCode | Should -Not -Be 0
        $result.Arguments.Count | Should -Be 0
    }

    It 'propagates CleanupCode failures on candidates' {
        $result = Invoke-CleanupFixture -ToolExitCode 5
        $result.ExitCode | Should -Be 5
    }

    It 'accepts no files matching the selected solution on a candidate' {
        $result = Invoke-CleanupFixture -ToolExitCode 3
        $result.ExitCode | Should -Be 0
    }
}
