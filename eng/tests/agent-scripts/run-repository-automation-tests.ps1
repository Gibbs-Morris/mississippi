#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$testPaths = @(
    (Join-Path $PSScriptRoot 'RepositoryAutomation.Tests.ps1'),
    (Join-Path $PSScriptRoot 'CleanupWorkflow.Tests.ps1'),
    (Join-Path $PSScriptRoot 'MutationAutomation.Tests.ps1'),
    (Join-Path $PSScriptRoot 'SourceProjectResolution.Tests.ps1'),
    (Join-Path $PSScriptRoot 'MutationSummary.Tests.ps1'),
    (Join-Path $PSScriptRoot 'PowerShellTestHarness.Tests.ps1'),
    (Join-Path $PSScriptRoot 'PrReadiness.Tests.ps1'),
    (Join-Path $PSScriptRoot 'ValidationEvidence.Tests.ps1'),
    (Join-Path $PSScriptRoot 'Setup.Tests.ps1'),
    (Join-Path $PSScriptRoot 'GoalWorkflow.Tests.ps1'),
    (Join-Path $PSScriptRoot 'AgentEvaluation.Tests.ps1'),
    (Join-Path $PSScriptRoot 'PrFeedbackSkill.Tests.ps1')
)
foreach ($testPath in $testPaths) {
    if (-not (Test-Path -LiteralPath $testPath -PathType Leaf)) { throw "Test file not found: $testPath" }
}

$runner = Join-Path $PSScriptRoot 'run-pester-suite.ps1'
$runs = foreach ($testPath in $testPaths) {
    $run = & $runner -TestPath $testPath -PassThru
    if ($null -eq $run -or $run.TotalCount -lt 1) {
        throw "Pester runner returned no tests for $testPath"
    }

    $run
}

$failedRuns = @($runs | Where-Object { $_.Result -ne 'Passed' })
$result = [pscustomobject]@{
    Result = if ($failedRuns.Count -eq 0) { 'Passed' } else { 'Failed' }
    TotalCount = ($runs | Measure-Object -Property TotalCount -Sum).Sum
    FailedCount = ($runs | Measure-Object -Property FailedCount -Sum).Sum
    PassedCount = ($runs | Measure-Object -Property PassedCount -Sum).Sum
    SkippedCount = ($runs | Measure-Object -Property SkippedCount -Sum).Sum
    Containers = @($runs | ForEach-Object { $_.Containers })
    # Keep the native runs for test details, timings, and configuration.
    Runs = $runs
}

if ($PassThru) { return $result }
if ($result.Result -ne 'Passed') { exit 1 }
exit 0
