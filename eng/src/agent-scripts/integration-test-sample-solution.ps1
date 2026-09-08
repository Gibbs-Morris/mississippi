#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs L2 functional API and infrastructure tests for the Samples solution.

.DESCRIPTION
    This script executes L2 tests that require deployed services or other infrastructure.
    Other levels can be selected explicitly with TestLevels. These tests are excluded
    from the standard unit-test-sample-solution.ps1 to keep PR gates fast.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER TestLevels
    Array of test levels to run (default: L2Tests).

.EXAMPLE
    pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1

.EXAMPLE
    pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1 -TestLevels @('L2Tests')
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$TestLevels = @('L2Tests')
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
