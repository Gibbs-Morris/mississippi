#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs L2+ integration and end-to-end tests for the Samples solution.

.DESCRIPTION
    This script executes higher-level tests (L2, L3, L4) that require deployed services,
    Playwright browser automation, or other infrastructure. These tests are excluded
    from the standard unit-test-sample-solution.ps1 to keep PR gates fast.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER TestLevels
    Array of test levels to run (default: L2Tests, L3Tests, L4Tests).

.EXAMPLE
    pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1

.EXAMPLE
    pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1 -TestLevels @('L2Tests')
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$TestLevels = @('L2Tests', 'L3Tests', 'L4Tests')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $repoRoot -TestLevels $TestLevels
}
catch {
    Write-Error "=== SAMPLE SOLUTION INTEGRATION TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}
