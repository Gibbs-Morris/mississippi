#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs code-style cleanup (ReSharper CleanupCode) for both solutions in the repository.
.DESCRIPTION
    This is a convenience wrapper that sequentially invokes:
      • scripts/clean-up-mississippi-solution.ps1
      • scripts/clean-up-sample-solution.ps1
    It ensures both the core and sample solutions are formatted with the same rules.
#>

param(
    # Switch to skip cleaning the sample solution if desired.
    [switch]$SkipSamples,

    # Switch to skip cleaning the main Mississippi solution if desired.
    [switch]$SkipMississippi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Determine repository root (this file lives there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Paths to the per-solution cleanup scripts
$mississippiCleanup = Join-Path $repoRoot 'scripts\clean-up-mississippi-solution.ps1'
$sampleCleanup      = Join-Path $repoRoot 'scripts\clean-up-sample-solution.ps1'

try {
    if (-not $SkipMississippi) {
        Write-Host "=== STEP 1: MISSISSIPPI SOLUTION CLEANUP ===" -ForegroundColor Yellow
        Write-Host "Running ReSharper CleanupCode on mississippi.slnx..."
        & $mississippiCleanup
        if ($LASTEXITCODE -ne 0) {
            throw "Mississippi solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "SUCCESS: Mississippi solution cleanup completed successfully" -ForegroundColor Green
        Write-Host ""
    }

    if (-not $SkipSamples) {
        Write-Host "=== STEP 2: SAMPLE SOLUTION CLEANUP ===" -ForegroundColor Yellow
        Write-Host "Running ReSharper CleanupCode on samples.slnx..."
        & $sampleCleanup
        if ($LASTEXITCODE -ne 0) {
            throw "Sample solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "SUCCESS: Sample solution cleanup completed successfully" -ForegroundColor Green
        Write-Host ""
    }

    Write-Host "=== ALL CLEANUP OPERATIONS COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "Both mississippi.slnx and samples.slnx have been cleaned with ReSharper rules"
} catch {
    Write-Error "=== FAILURE: Cleanup operation failed: $_"
    exit 1
}
