#!/usr/bin/env pwsh

<#
    Orchestrates the end-to-end pipeline for both solutions.
    Order:
      1. Mississippi  build, unit test, mutation test, cleanup.
      2. Sample       build, unit test, cleanup.
    The script stops immediately if any step returns a non-zero exit code.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$ScriptName,
        [string]$Description,
        [int]$StepNumber
    )
    Write-Host "=== STEP ${StepNumber}: ${Description} ===" -ForegroundColor Yellow
    Write-Host "Executing script: $ScriptName"
    
    try {
        & "$PSScriptRoot/$ScriptName"
        if ($LASTEXITCODE -ne 0) {
            throw "Step ${StepNumber} (${ScriptName}) failed with exit code $LASTEXITCODE"
        }
        Write-Host "SUCCESS: Step ${StepNumber} completed - ${Description}" -ForegroundColor Green
        Write-Host ""
    } catch {
        Write-Error "FAILURE: Step ${StepNumber} failed - ${Description} : $_"
        throw
    }
}

try {
    Write-Host "=== STARTING COMPLETE BUILD AND TEST PIPELINE ===" -ForegroundColor Magenta
    Write-Host "Pipeline will execute Mississippi solution followed by Sample solution"
    Write-Host "Each step must complete successfully before proceeding to the next"
    Write-Host ""
    
    Write-Host "=== MISSISSIPPI SOLUTION PIPELINE ===" -ForegroundColor Cyan
    
    # Mississippi pipeline
    Invoke-Step "build-mississippi-solution.ps1" "Build Mississippi Solution" 1
    Invoke-Step "unit-test-mississippi-solution.ps1" "Run Mississippi Unit Tests" 2
    Invoke-Step "mutation-test-mississippi-solution.ps1" "Run Mississippi Mutation Tests" 3
    Invoke-Step "clean-up-mississippi-solution.ps1" "Cleanup Mississippi Code Style" 4

    Write-Host "=== SAMPLE SOLUTION PIPELINE ===" -ForegroundColor Cyan
    
    # Sample pipeline
    Invoke-Step "build-sample-solution.ps1" "Build Sample Solution" 5
    Invoke-Step "unit-test-sample-solution.ps1" "Run Sample Unit Tests" 6
    Invoke-Step "clean-up-sample-solution.ps1" "Cleanup Sample Code Style" 7

    # Final build of both solutions with warnings treated as errors
    Invoke-Step "final-build-solutions.ps1" "Final Build with Warnings as Errors" 8
    
    Write-Host "=== PIPELINE COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "All 8 steps completed without errors. Solutions are ready for deployment."
} catch {
    Write-Error "=== PIPELINE FAILED ===: $_"
    exit 1
}

