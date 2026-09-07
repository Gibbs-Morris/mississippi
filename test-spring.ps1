#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds Spring and verifies its Aspire stack through a headless browser.
.DESCRIPTION
    Smoke runs the banking journey; Full runs all Spring L2 tests. Doctor checks
    SDK and Docker access without installing tools, building, or starting services.
    Results are written to a unique directory under artifacts/spring.
.EXAMPLE
    pwsh ./test-spring.ps1
.EXAMPLE
    pwsh ./test-spring.ps1 -Suite Full -InstallBrowserDependencies
#>
[CmdletBinding()]
param(
    [ValidateSet('Smoke', 'Full')][string]$Suite = 'Smoke',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$Doctor,
    [switch]$InstallBrowserDependencies
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1') -Force
try {
    Invoke-SpringValidation -RepoRoot $PSScriptRoot -Suite $Suite -Configuration $Configuration `
        -Doctor:$Doctor -InstallBrowserDependencies:$InstallBrowserDependencies
    exit 0
}
catch {
    Write-Error "Spring validation failed: $($_.Exception.Message)"
    exit 1
}
