#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PowerShell test orchestration' {
    BeforeAll {
        $sourceRoot = Split-Path -Parent $PSScriptRoot
        $fixtureRoot = Join-Path $TestDrive 'repository'
        $fixtureTests = Join-Path $fixtureRoot 'eng/tests'
        $fixtureRunners = Join-Path $fixtureTests 'agent-scripts'
        $fixtureModules = Join-Path $fixtureRoot 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $fixtureRunners, $fixtureModules, (Join-Path $fixtureRoot '.git') -Force | Out-Null
        Copy-Item (Join-Path $sourceRoot 'orchestrate-powershell-tests.ps1') $fixtureTests
        Copy-Item (Join-Path $sourceRoot '../src/agent-scripts/RepositoryAutomation.psm1') $fixtureModules
        $orchestrator = Join-Path $fixtureTests 'orchestrate-powershell-tests.ps1'
        $pesterRunners = @(
            'run-repository-automation-tests.ps1',
            'run-scratchpad-task-tests.ps1',
            'run-summarize-coverage-gaps-tests.ps1',
            'run-task-automation-tests.ps1'
        )
        $targetRunner = Join-Path $fixtureRunners $pesterRunners[0]
    }

    BeforeEach {
        foreach ($runner in $pesterRunners) {
            Set-Content (Join-Path $fixtureRunners $runner) 'param([switch]$PassThru); [pscustomobject]@{ Result = "Passed"; TotalCount = 1; FailedCount = 0 }'
        }
        Set-Content (Join-Path $fixtureRunners 'verify-scratchpad-task-scripts.ps1') 'exit 0'
    }

    It 'runs every required suite successfully' {
        $results = & $orchestrator -PassThru 6>$null
        $results.Count | Should -Be 5
        @($results | Where-Object Status -NE 'Passed').Count | Should -Be 0
    }

    It 'fails a missing runner' {
        Remove-Item -LiteralPath $targetRunner
        $results = & $orchestrator -PassThru 6>$null
        $results[0].Status | Should -Be 'Failed'
        $results[0].Error | Should -BeLike 'Test runner not found:*'
    }

    It 'fails invalid or unsuccessful Pester results: <Case>' -ForEach @(
        @{ Case = 'null'; Body = '' },
        @{ Case = 'no tests'; Body = '[pscustomobject]@{ Result = "Passed"; TotalCount = 0; FailedCount = 0 }' },
        @{ Case = 'container failure'; Body = '[pscustomobject]@{ Result = "Failed"; TotalCount = 1; FailedCount = 0 }' },
        @{ Case = 'test failure'; Body = '[pscustomobject]@{ Result = "Failed"; TotalCount = 1; FailedCount = 1 }' }
    ) {
        Set-Content $targetRunner "param([switch]`$PassThru); $Body"
        $results = & $orchestrator -PassThru 6>$null
        $results[0].Status | Should -Be 'Failed'
        $results[0].Failed | Should -BeGreaterThan 0
    }

    It 'fails a script that exits unsuccessfully' {
        Set-Content (Join-Path $fixtureRunners 'verify-scratchpad-task-scripts.ps1') 'exit 7'
        $results = & $orchestrator -PassThru 6>$null
        $results[-1].Status | Should -Be 'Failed'
        $results[-1].Error | Should -BeLike 'Test runner exited with code 7:*'
    }

    It 'returns process exit code <ExitCode> when the script runner exits <ExitCode>' -ForEach @(
        @{ ExitCode = 0 },
        @{ ExitCode = 1 }
    ) {
        Set-Content (Join-Path $fixtureRunners 'verify-scratchpad-task-scripts.ps1') "exit $ExitCode"
        & (Get-Process -Id $PID).Path -NoProfile -File $orchestrator | Out-Null
        $LASTEXITCODE | Should -Be $ExitCode
    }
}

Describe 'Standalone Pester runners' {
    It 'fails discovery errors in <Runner>' -ForEach @(
        @{ Runner = 'run-scratchpad-task-tests.ps1'; TestFile = 'scratchpad-task-scripts.Tests.ps1' },
        @{ Runner = 'run-summarize-coverage-gaps-tests.ps1'; TestFile = 'summarize-coverage-gaps.Tests.ps1' },
        @{ Runner = 'run-task-automation-tests.ps1'; TestFile = 'TaskAutomation.Tests.ps1' }
    ) {
        $fixture = Join-Path $TestDrive $Runner
        New-Item -ItemType Directory -Path $fixture | Out-Null
        Copy-Item (Join-Path $PSScriptRoot $Runner) $fixture
        Set-Content (Join-Path $fixture $TestFile) "throw 'discovery failure'"
        & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $fixture $Runner) | Out-Null
        $LASTEXITCODE | Should -Be 1
    }
}
