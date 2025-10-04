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
        [int]$StepNumber,
        [hashtable]$ScriptParameters
    )
    Write-Host "=== STEP ${StepNumber}: ${Description} ===" -ForegroundColor Yellow
    $commandDisplay = if ($ScriptParameters -and $ScriptParameters.Count -gt 0) {
        $paramDisplay = $ScriptParameters.GetEnumerator() | ForEach-Object { "-$($_.Key)=$($_.Value)" }
        "$ScriptName $($paramDisplay -join ' ')"
    } else {
        $ScriptName
    }
    Write-Host "Executing script: $commandDisplay"
    
    try {
        if ($ScriptParameters) {
            & "$PSScriptRoot/$ScriptName" @ScriptParameters
        } else {
            & "$PSScriptRoot/$ScriptName"
        }
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
    Invoke-Step -ScriptName "build-mississippi-solution.ps1" -Description "Build Mississippi Solution" -StepNumber 1
    Invoke-Step -ScriptName "unit-test-mississippi-solution.ps1" -Description "Run Mississippi Unit Tests" -StepNumber 2
    Invoke-Step -ScriptName "summarize-coverage-gaps.ps1" -Description "Summarize Coverage Gaps" -StepNumber 3 -ScriptParameters @{ EmitTasks = $true }
    Invoke-Step -ScriptName "mutation-test-mississippi-solution.ps1" -Description "Run Mississippi Mutation Tests" -StepNumber 4
    Invoke-Step -ScriptName "summarize-mutation-survivors.ps1" -Description "Summarize Mutation Survivors" -StepNumber 5 -ScriptParameters @{ GenerateTasks = $true; SkipMutationRun = $true }
    Invoke-Step -ScriptName "clean-up-mississippi-solution.ps1" -Description "Cleanup Mississippi Code Style" -StepNumber 6

    Write-Host "=== SAMPLE SOLUTION PIPELINE ===" -ForegroundColor Cyan
    
    # Sample pipeline
    Invoke-Step -ScriptName "build-sample-solution.ps1" -Description "Build Sample Solution" -StepNumber 7
    Invoke-Step -ScriptName "unit-test-sample-solution.ps1" -Description "Run Sample Unit Tests" -StepNumber 8
    Invoke-Step -ScriptName "clean-up-sample-solution.ps1" -Description "Cleanup Sample Code Style" -StepNumber 9

    # Final build of both solutions with warnings treated as errors
    Invoke-Step -ScriptName "final-build-solutions.ps1" -Description "Final Build with Warnings as Errors" -StepNumber 10
    
    Write-Host "=== PIPELINE COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "All 8 steps completed without errors. Solutions are ready for deployment."
} catch {
    Write-Error "=== PIPELINE FAILED ===: $_"
    exit 1
}

