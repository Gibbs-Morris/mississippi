#!/usr/bin/env pwsh

<#
    Orchestrates the end-to-end pipeline for both solutions.
    Order:
      1. Mississippi – build, unit test, mutation test, cleanup.
      2. Sample      – build, unit test, cleanup.
    The script stops immediately if any step returns a non-zero exit code.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

function Invoke-Step {
    param(
        [string]$ScriptName
    )
    Write-Host "=== Running $ScriptName ===" -ForegroundColor Yellow
    & "$PSScriptRoot/$ScriptName"

}

# Mississippi pipeline
Invoke-Step "build-mississippi-solution.ps1"
Invoke-Step "unit-test-mississippi-solution.ps1"
Invoke-Step "mutation-test-mississippi-solution.ps1"
Invoke-Step "clean-up-mississippi-solution.ps1"

# Sample pipeline
Invoke-Step "build-sample-solution.ps1"
Invoke-Step "unit-test-sample-solution.ps1"
Invoke-Step "clean-up-sample-solution.ps1"

# Final build of both solutions with warnings treated as errors
Invoke-Step "final-build-solutions.ps1" 