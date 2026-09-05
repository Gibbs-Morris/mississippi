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
    @{ Name = 'run-scratchpad-task-tests.ps1';   Type = 'Pester' },
    @{ Name = 'run-summarize-coverage-gaps-tests.ps1'; Type = 'Pester' },
    @{ Name = 'run-task-automation-tests.ps1';   Type = 'Pester' },
    @{ Name = 'verify-scratchpad-task-scripts.ps1';    Type = 'Script' }
)

# Ensure Pester v5+ is available when any Pester runners are present
$needsPester = $testRunners | Where-Object { $_.Type -eq 'Pester' }
if ($needsPester.Count -gt 0) {
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
}

$results = @()
$failureCount = 0

foreach ($runner in $testRunners) {
    $path = Join-Path $testsRoot $runner.Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Host "Skipping missing test runner: $($runner.Name)" -ForegroundColor DarkGray
        continue
    }

    Write-Host "Executing: $($runner.Name)" -ForegroundColor Cyan
    try {
        if ($runner.Type -eq 'Pester') {
            $result = & $path -PassThru

            # Determine failures across Pester versions
            $failedCount = 0
            if ($null -ne $result) {
                if ($result.PSObject.Properties.Name -contains 'FailedCount') {
                    $failedCount = [int]$result.FailedCount
                }
                elseif ($result.PSObject.Properties.Name -contains 'Failed') {
                    $failedCount = [int]$result.Failed
                }
                elseif ($result.PSObject.Properties.Name -contains 'TestResult') {
                    $failedCount = @($result.TestResult | Where-Object { $_.Result -eq 'Failed' -or $_.Outcome -eq 'Failed' }).Count
                }
            }

            $status = if ($failedCount -gt 0) { 'Failed' } else { 'Passed' }
            if ($failedCount -gt 0) { $failureCount++ }
            $results += [pscustomobject]@{ Name = $runner.Name; Type = 'Pester'; Status = $status; Failed = $failedCount }
        }
        else {
            & $path
            $results += [pscustomobject]@{ Name = $runner.Name; Type = 'Script'; Status = 'Passed'; Failed = 0 }
        }
    }
    catch {
        $failureCount++
        $results += [pscustomobject]@{ Name = $runner.Name; Type = $runner.Type; Status = 'Failed'; Failed = 1; Error = $_.Exception.Message }
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



