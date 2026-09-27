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
        Copy-Item (Join-Path $sourceRoot '../src/agent-scripts/ValidationEvidence.psm1') $fixtureModules
        $orchestrator = Join-Path $fixtureTests 'orchestrate-powershell-tests.ps1'
        $pesterRunners = @(
            'run-repository-automation-tests.ps1',
            'run-spring-validation-tests.ps1',
            'run-scratchpad-task-tests.ps1',
            'run-summarize-coverage-gaps-tests.ps1',
            'run-pr-issue-reference-tests.ps1',
            'run-task-automation-tests.ps1',
            'run-validation-plan-tests.ps1',
            'run-issue-spec-tests.ps1',
            'run-agent-doctor-tests.ps1',
            'run-agent-context-tests.ps1',
            'run-decompose-delivery-tests.ps1'
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

    It 'runs every applicable suite successfully and reports capability skips' {
        $results = & $orchestrator -PassThru 6>$null
        $results.Count | Should -Be 12
        $optional = @($results | Where-Object Name -EQ 'run-decompose-delivery-tests.ps1')
        $optional.Count | Should -Be 1
        $available = $PSVersionTable.PSVersion -ge [version]'7.4' -and ($IsWindows -or $IsLinux)
        if ($IsLinux) {
            foreach ($command in @('stat', 'prlimit')) {
                if ($null -eq (Get-Command $command -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)) { $available = $false }
            }
        }
        $optional[0].Status | Should -Be $(if ($available) { 'Passed' } else { 'Skipped' })
        if (-not $available) { $optional[0].Reason | Should -Not -BeNullOrEmpty }
        @($results | Where-Object { $_.Name -ne 'run-decompose-delivery-tests.ps1' -and $_.Status -ne 'Passed' }).Count | Should -Be 0
    }

    It 'skips the optional runner while executing unrelated suites for simulated <Capability>' -ForEach @(
        @{ Capability = 'PowerShell 7.3'; Reason = '*PowerShell 7.4*'; Find = '$PSVersionTable.PSVersion'; Replacement = "([version]'7.3')" },
        @{ Capability = 'unsupported platform'; Reason = '*Supported platforms*'; Find = "if (`$IsWindows) { 'Windows' } elseif (`$IsLinux) { 'Linux' } else { 'Other' }"; Replacement = "'Other'" },
        @{ Capability = 'missing Linux prlimit'; Reason = '*prlimit*'; Find = '$null -eq $reason -and $IsLinux -and $runner.ContainsKey(''LinuxCommands'')'; Replacement = '$null -eq $reason -and $runner.ContainsKey(''LinuxCommands'')' }
    ) {
        # These are capability-routing simulations on the current engine.
        $copy = Join-Path $fixtureTests 'capability-orchestrator.ps1'
        $source = [IO.File]::ReadAllText($orchestrator)
        $source.Contains($Find) | Should -BeTrue
        $source = $source.Replace($Find, $Replacement)
        if ($Capability -eq 'missing Linux prlimit') {
            $probe = '$missing = @($runner.LinuxCommands | Where-Object { $null -eq (Get-Command $_ -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1) })'
            $source.Contains($probe) | Should -BeTrue
            $source = $source.Replace($probe, '$missing = @(''prlimit'')')
        }
        [IO.File]::WriteAllText($copy, $source)
        $marker = Join-Path $fixtureRoot 'optional-runner.marker'
        Set-Content (Join-Path $fixtureRunners 'run-decompose-delivery-tests.ps1') "param([switch]`$PassThru); [IO.File]::WriteAllText('$($marker.Replace("'", "''"))', 'executed'); throw 'A skipped runner executed'"
        $results = & $copy -PassThru 6>$null
        $results.Count | Should -Be 12
        $optional = @($results | Where-Object Name -EQ 'run-decompose-delivery-tests.ps1')
        $optional.Count | Should -Be 1
        $optional[0].Status | Should -Be 'Skipped'
        $optional[0].Reason | Should -BeLike $Reason
        Test-Path -LiteralPath $marker | Should -BeFalse
        @($results | Where-Object { $_.Name -ne 'run-decompose-delivery-tests.ps1' -and $_.Status -ne 'Passed' }).Count | Should -Be 0
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
            @{ Runner = 'run-task-automation-tests.ps1'; TestFile = 'TaskAutomation.Tests.ps1' },
            @{ Runner = 'run-validation-plan-tests.ps1'; TestFile = 'ValidationPlan.Tests.ps1' },
            @{ Runner = 'run-issue-spec-tests.ps1'; TestFile = 'IssueSpec.Tests.ps1' },
            @{ Runner = 'run-agent-doctor-tests.ps1'; TestFile = 'AgentDoctor.Tests.ps1' }
            @{ Runner = 'run-agent-doctor-tests.ps1'; TestFile = 'AgentDoctor.Tests.ps1' },
            @{ Runner = 'run-agent-context-tests.ps1'; TestFile = 'AgentContext.Tests.ps1' },
            @{ Runner = 'run-decompose-delivery-tests.ps1'; TestFile = 'DecomposeDelivery.Tests.ps1'; MinimumVersion = [version]'7.4' }
        )) {
            foreach ($scenario in @(
                @{ Case = 'passing'; Body = "Describe 'Suite' { It 'passes' { 1 | Should -Be 1 } }"; ExitCode = 0 },
                @{ Case = 'discovery failure'; Body = "throw 'discovery failure'"; ExitCode = 1 },
                @{ Case = 'empty discovery'; Body = ''; ExitCode = 1 },
                @{ Case = 'failed test'; Body = "Describe 'Suite' { It 'fails' { 1 | Should -Be 2 } }"; ExitCode = 1 }
            )) {
                @{ Runner = $suite.Runner; TestFile = $suite.TestFile; Case = $scenario.Case; Body = $scenario.Body; ExitCode = $scenario.ExitCode; MinimumVersion = $(if ($suite.ContainsKey('MinimumVersion')) { $suite.MinimumVersion } else { [version]'7.0' }) }
            }
        }
    ) {
        if ($PSVersionTable.PSVersion -lt $MinimumVersion) {
            Set-ItResult -Skipped -Because "This runner requires PowerShell $MinimumVersion or later."
            return
        }
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
        Set-Content (Join-Path $scripts 'orchestrate-solutions.ps1') "param([string]`$Configuration, [switch]`$SkipCleanup, [switch]`$IncludeMutation, [string]`$LeaseDirectory); Write-Output ([string]::Join('|', `$Configuration, `$SkipCleanup, `$IncludeMutation, `$LeaseDirectory)); exit $ExitCode"
        $output = & $powerShellPath -NoProfile -File $portableHostScript (Join-Path $fixture 'go.ps1') -Configuration Debug -SkipCleanup -IncludeMutation -LeaseDirectory shared-leases 2>&1 | Out-String
        $LASTEXITCODE | Should -Be $(if ($ExitCode -eq 0) { 0 } else { 1 })
        $output | Should -Match 'Debug\|True\|True\|shared-leases'
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
        Set-Content (Join-Path $scripts 'final-build-solutions.ps1') "param([string]`$Configuration, [string]`$LeaseDirectory); Write-Output ('FINAL:' + `$Configuration + '|' + `$LeaseDirectory); exit $ExitCode"
        $output = & $powerShellPath -NoProfile -File $portableHostScript (Join-Path $fixture 'quick-build.ps1') -Configuration Debug -LeaseDirectory shared-leases 2>&1 | Out-String
        $LASTEXITCODE | Should -Be $WrapperExit
        $output | Should -Match 'FINAL:Debug\|shared-leases'
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
        if ($Prefix -in @('build', 'clean-up')) {
            $failureStatement = if ($ExitCode -eq 0) { '' } else { "throw 'CORE failed with exit code: $ExitCode'" }
            @"
function Get-RepositoryRoot { return (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent `$PSScriptRoot))) }
function Enter-RepositoryExecutionLease { param([string]`$RepoRoot, [string]`$OperationId, [string]`$LeaseDirectory); [pscustomobject]@{ RepositoryRoot = `$RepoRoot; OwnsStream = `$false } }
function Exit-RepositoryExecutionLease { param([object]`$Lease) }
function Invoke-MississippiSolutionBuild { param([string]`$Configuration, [string]`$RepoRoot); Write-Output ('CORE:' + `$Configuration); $failureStatement }
function Invoke-SampleSolutionBuild { param([string]`$Configuration, [string]`$RepoRoot); Write-Output ('SAMPLES:' + `$Configuration) }
function Invoke-MississippiSolutionCleanup { param([string]`$RepoRoot); Write-Output 'CORE:'; $failureStatement }
function Invoke-SampleSolutionCleanup { param([string]`$RepoRoot); Write-Output 'SAMPLES:' }
Export-ModuleMember -Function Get-RepositoryRoot, Enter-RepositoryExecutionLease, Exit-RepositoryExecutionLease, Invoke-MississippiSolutionBuild, Invoke-SampleSolutionBuild, Invoke-MississippiSolutionCleanup, Invoke-SampleSolutionCleanup
"@ | Set-Content (Join-Path $scripts 'RepositoryAutomation.psm1')
        }
        else {
            Set-Content (Join-Path $scripts "$Prefix-mississippi-solution.ps1") "param([string]`$Configuration); Write-Output ('CORE:' + `$Configuration); exit $ExitCode"
            Set-Content (Join-Path $scripts "$Prefix-sample-solution.ps1") "param([string]`$Configuration); Write-Output ('SAMPLES:' + `$Configuration); exit 0"
        }
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
            $output | Should -Match 'CORE failed[\s|]+with[\s|]+exit[\s|]+code:?[\s|]+7'
        }
        $output | Should -Match "CORE:$expectedConfiguration"
    }
}
