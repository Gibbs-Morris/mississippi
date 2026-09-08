#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
    $portableHostScript = Join-Path $TestDrive 'portable-host.ps1'
    Set-Content $portableHostScript @'
$env:PATH = ''
$childArguments = @($args | Select-Object -Skip 1)
& $args[0] @childArguments
exit $LASTEXITCODE
'@
}

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
            'run-spring-validation-tests.ps1',
            'run-scratchpad-task-tests.ps1',
            'run-summarize-coverage-gaps-tests.ps1',
            'run-task-automation-tests.ps1'
        )
        $targetRunner = Join-Path $fixtureRunners $pesterRunners[0]
    }

    AfterAll {
        $fixtureModule = Join-Path $fixtureModules 'RepositoryAutomation.psm1'
        Get-Module RepositoryAutomation -All |
            Where-Object Path -EQ $fixtureModule |
            Remove-Module -Force
    }

    BeforeEach {
        foreach ($runner in $pesterRunners) {
            Set-Content (Join-Path $fixtureRunners $runner) 'param([switch]$PassThru); [pscustomobject]@{ Result = "Passed"; TotalCount = 1; FailedCount = 0 }'
        }
        Set-Content (Join-Path $fixtureRunners 'verify-scratchpad-task-scripts.ps1') 'exit 0'
    }

    It 'runs every required suite successfully' {
        $results = & $orchestrator -PassThru 6>$null
        $results.Count | Should -Be 6
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
        & $powerShellPath -NoProfile -File $orchestrator | Out-Null
        $LASTEXITCODE | Should -Be $ExitCode
    }
}

Describe 'Standalone Pester runners' {
    It 'returns <ExitCode> for <Case> through <Runner>' -ForEach @(
        foreach ($suite in @(
            @{ Runner = 'run-scratchpad-task-tests.ps1'; TestFile = 'scratchpad-task-scripts.Tests.ps1' },
            @{ Runner = 'run-summarize-coverage-gaps-tests.ps1'; TestFile = 'summarize-coverage-gaps.Tests.ps1' },
            @{ Runner = 'run-task-automation-tests.ps1'; TestFile = 'TaskAutomation.Tests.ps1' }
        )) {
            foreach ($scenario in @(
                @{ Case = 'passing'; Body = "Describe 'Suite' { It 'passes' { 1 | Should -Be 1 } }"; ExitCode = 0 },
                @{ Case = 'discovery failure'; Body = "throw 'discovery failure'"; ExitCode = 1 },
                @{ Case = 'empty discovery'; Body = ''; ExitCode = 1 },
                @{ Case = 'failed test'; Body = "Describe 'Suite' { It 'fails' { 1 | Should -Be 2 } }"; ExitCode = 1 }
            )) {
                @{ Runner = $suite.Runner; TestFile = $suite.TestFile; Case = $scenario.Case; Body = $scenario.Body; ExitCode = $scenario.ExitCode }
            }
        }
    ) {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $fixture | Out-Null
        Copy-Item (Join-Path $PSScriptRoot $Runner) $fixture
        Copy-Item (Join-Path $PSScriptRoot 'run-pester-suite.ps1') $fixture
        Set-Content (Join-Path $fixture $TestFile) $Body
        & $powerShellPath -NoProfile -File (Join-Path $fixture $Runner) | Out-Null
        $LASTEXITCODE | Should -Be $ExitCode
    }
}

Describe 'Build entry point process boundaries' {
    It 'forwards pipeline options and handles child exit <ExitCode>' -ForEach @(
        @{ ExitCode = 0 },
        @{ ExitCode = 7 }
    ) {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $scripts = Join-Path $fixture 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $scripts -Force | Out-Null
        Copy-Item (Join-Path $PSScriptRoot '../../../go.ps1') $fixture
        Set-Content (Join-Path $scripts 'orchestrate-solutions.ps1') "param([string]`$Configuration, [switch]`$SkipCleanup, [switch]`$IncludeMutation); Write-Output ([string]::Join('|', `$Configuration, `$SkipCleanup, `$IncludeMutation)); exit $ExitCode"
        $output = & $powerShellPath -NoProfile -File $portableHostScript (Join-Path $fixture 'go.ps1') -Configuration Debug -SkipCleanup -IncludeMutation 2>&1 | Out-String
        $LASTEXITCODE | Should -Be $(if ($ExitCode -eq 0) { 0 } else { 1 })
        $output | Should -Match 'Debug\|True\|True'
        $output.Contains('SUCCESS: Main pipeline orchestration completed successfully') | Should -Be ($ExitCode -eq 0)
    }

    It 'handles the final-build child exit code in quick-build: <ExitCode>' -ForEach @(
        @{ ExitCode = 0; WrapperExit = 0 },
        @{ ExitCode = 7; WrapperExit = 1 }
    ) {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $scripts = Join-Path $fixture 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $scripts -Force | Out-Null
        Copy-Item (Join-Path $PSScriptRoot '../../../quick-build.ps1') $fixture
        Set-Content (Join-Path $scripts 'final-build-solutions.ps1') "param([string]`$Configuration); Write-Output ('FINAL:' + `$Configuration); exit $ExitCode"
        $output = & $powerShellPath -NoProfile -File $portableHostScript (Join-Path $fixture 'quick-build.ps1') -Configuration Debug 2>&1 | Out-String
        $LASTEXITCODE | Should -Be $WrapperExit
        $output | Should -Match 'FINAL:Debug'
        $output.Contains('QUICK BUILD COMPLETED SUCCESSFULLY') | Should -Be ($ExitCode -eq 0)
        if ($ExitCode -ne 0) { $output | Should -Match 'failed[\s|]+with[\s|]+exit[\s|]+code:?[\s|]+7' }
    }

    It 'runs both steps after success and stops after a failed child: <EntryPoint> <ExitCode>' -ForEach @(
        @{ ExitCode = 0; EntryPoint = 'build.ps1'; Prefix = 'build'; Summary = 'ALL REQUESTED BUILDS COMPLETED SUCCESSFULLY' },
        @{ ExitCode = 7; EntryPoint = 'build.ps1'; Prefix = 'build'; Summary = 'ALL REQUESTED BUILDS COMPLETED SUCCESSFULLY' },
        @{ ExitCode = 0; EntryPoint = 'clean-up.ps1'; Prefix = 'clean-up'; Summary = 'ALL CLEANUP OPERATIONS COMPLETED SUCCESSFULLY' },
        @{ ExitCode = 7; EntryPoint = 'clean-up.ps1'; Prefix = 'clean-up'; Summary = 'ALL CLEANUP OPERATIONS COMPLETED SUCCESSFULLY' }
    ) {
        $fixture = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $scripts = Join-Path $fixture 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $scripts -Force | Out-Null
        Copy-Item (Join-Path $PSScriptRoot '../../../' $EntryPoint) $fixture
        Set-Content (Join-Path $scripts "$Prefix-mississippi-solution.ps1") "param([string]`$Configuration); Write-Output ('CORE:' + `$Configuration); exit $ExitCode"
        Set-Content (Join-Path $scripts "$Prefix-sample-solution.ps1") "param([string]`$Configuration); Write-Output ('SAMPLES:' + `$Configuration); exit 0"
        $options = if ($Prefix -eq 'build') { @('-Configuration', 'Debug') } else { @() }
        $expectedConfiguration = if ($Prefix -eq 'build') { 'Debug' } else { '' }
        $output = & $powerShellPath -NoProfile -File $portableHostScript (Join-Path $fixture $EntryPoint) @options 2>&1 | Out-String
        if ($ExitCode -eq 0) {
            $LASTEXITCODE | Should -Be 0
            $output | Should -Match "SAMPLES:$expectedConfiguration"
            $output | Should -Match $Summary
        }
        else {
            $LASTEXITCODE | Should -Be 1
            $output | Should -Not -Match 'SAMPLES:'
            $output | Should -Match 'failed[\s|]+with[\s|]+exit[\s|]+code:?[\s|]+7'
        }
        $output | Should -Match "CORE:$expectedConfiguration"
    }
}
