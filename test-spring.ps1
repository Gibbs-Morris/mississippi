#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds Spring and runs its selected L2 API or L3 browser suite.
.DESCRIPTION
    L3 Smoke runs the banking journey; Full runs all tests at the selected L2 or L3 level. Doctor checks
    SDK and Docker access without installing tools, building, or starting services.
    Results are written to a unique directory under artifacts/spring.
.EXAMPLE
    pwsh ./test-spring.ps1
.EXAMPLE
    pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full
#>
[CmdletBinding()]
param(
    [ValidateSet('L2', 'L3')][string]$TestLevel = 'L3',
    [ValidateSet('Smoke', 'Full')][string]$Suite = 'Smoke',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$Doctor,
    [switch]$InstallBrowserDependencies,
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1') -Force
$executionLease = $null
$repoRoot = $PSScriptRoot
try {
    if (-not $Doctor) {
        $executionLease = Enter-RepositoryExecutionLease -RepoRoot $PSScriptRoot -OperationId "spring-validation-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
        $repoRoot = $executionLease.RepositoryRoot
    }
    Invoke-SpringValidation -RepoRoot $repoRoot -TestLevel $TestLevel -Suite $Suite -Configuration $Configuration `
        -Doctor:$Doctor -InstallBrowserDependencies:$InstallBrowserDependencies
    exit 0
}
catch {
    Write-Error "Spring validation failed: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}
