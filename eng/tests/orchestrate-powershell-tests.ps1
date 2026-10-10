#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path $PSScriptRoot '..\src\agent-scripts\RepositoryAutomation.psm1'
$automationModulePath = [System.IO.Path]::GetFullPath($automationModulePath)
Import-Module -Name $automationModulePath -Force



$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$testsRoot = Join-Path $repoRoot 'eng/tests/agent-scripts'

$testRunners = @(
    'run-repository-automation-tests.ps1',
    'run-spring-validation-tests.ps1',
    'run-scratchpad-task-tests.ps1',
    'run-summarize-coverage-gaps-tests.ps1',
    'run-task-automation-tests.ps1',
    'run-validation-plan-tests.ps1',
    'run-issue-spec-tests.ps1',
    'run-agent-doctor-tests.ps1',
    'run-agent-context-tests.ps1'
)

try {
    Import-Module Pester -MinimumVersion 5.0.0 -Force -ErrorAction Stop | Out-Null
}
catch {
    Write-Host 'Pester v5+ is required to run PowerShell tests.' -ForegroundColor Red
    if ($env:CI) {
        Write-Host 'Install Pester v5 in your CI image or a pre-step. Example:' -ForegroundColor Yellow
        Write-Host '  pwsh -Command "Install-Module -Name Pester -Scope CurrentUser -Force -MinimumVersion 5.0.0"' -ForegroundColor Yellow
        exit 1
    }
    else {
        Write-Host 'Install Pester locally:' -ForegroundColor Yellow
        Write-Host '  Install-Module -Name Pester -Scope CurrentUser -Force -MinimumVersion 5.0.0' -ForegroundColor Yellow
        throw
    }
}

$results = @()
$failureCount = 0

foreach ($runner in $testRunners) {
    $path = Join-Path $testsRoot $runner
    Write-Host "Executing: $runner" -ForegroundColor Cyan
    try {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Test runner not found: $runner"
        }

        $result = & $path -PassThru
        if ($null -eq $result -or $result.TotalCount -lt 1) {
            throw "Pester runner returned no tests: $runner"
        }

        # Result also covers discovery and container failures with no failed tests.
        $status = if ($result.Result -eq 'Passed' -and $result.FailedCount -eq 0) { 'Passed' } else { 'Failed' }
        $failedCount = if ($status -eq 'Failed') { [Math]::Max(1, $result.FailedCount) } else { 0 }
        if ($status -eq 'Failed') { $failureCount++ }
        $results += [pscustomobject]@{ Name = $runner; Type = 'Pester'; Status = $status; Failed = $failedCount }
    }
    catch {
        $failureCount++
        Write-Host "FAILED: ${runner}: $($_.Exception.Message)" -ForegroundColor Red
        $results += [pscustomobject]@{ Name = $runner; Type = 'Pester'; Status = 'Failed'; Failed = 1; Error = $_.Exception.Message }
    }
}

Write-Host ''
Write-Host '=== PowerShell Test Summary ===' -ForegroundColor White
$table = $results | Select-Object Name, Type, Status, Failed
$table | Format-Table -AutoSize | Out-String | Write-Host

Write-Host ''
if ($failureCount -gt 0) {
    Write-Host 'RESULT: FAIL' -ForegroundColor Red
$failedSuites = @($results | Where-Object { $_.Status -eq 'Failed' } | Select-Object -ExpandProperty Name)
if ($failedSuites -and $failedSuites.Count -gt 0) {
    Write-Host ("Failed suites: {0}" -f ($failedSuites -join ', ')) -ForegroundColor Red
}
}
else {
    Write-Host 'RESULT: SUCCESS' -ForegroundColor Green
    Write-Host 'All PowerShell test suites passed.' -ForegroundColor Green
}

if ($PassThru) { return $results }

if ($failureCount -gt 0) { exit 1 } else { exit 0 }



