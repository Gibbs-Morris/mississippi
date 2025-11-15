#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Performs a fast, strict build of both solutions by invoking eng\\src\\agent-scripts\\final-build-solutions.ps1.
#>

# Determine the script directory (repo root)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Build the path to the final-build script
$finalBuildScript = Join-Path $scriptDir 'eng\\src\\agent-scripts\\final-build-solutions.ps1'

Write-Host "=== QUICK BUILD MODE ===" -ForegroundColor Yellow
Write-Host "Fast build with warnings as errors (bypasses tests and cleanup)"
Write-Host "Executing final-build-solutions.ps1..."
Write-Host ""

try {
    # Execute the final-build script and wait for completion
    & $finalBuildScript @args
    if ($LASTEXITCODE -ne 0) {
        throw "final-build-solutions.ps1 failed with exit code $LASTEXITCODE"
    }
    Write-Host "=== QUICK BUILD COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "Both solutions built successfully with zero warnings"
} catch {
    Write-Error "=== QUICK BUILD FAILED ===: $_"
    exit 1
}

