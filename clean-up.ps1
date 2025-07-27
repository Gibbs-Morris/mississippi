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
        Write-Host "Running cleanup for mississippi.slnx…" -ForegroundColor Cyan
        & $mississippiCleanup
        if ($LASTEXITCODE -ne 0) {
            throw "Mississippi solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "✓ Mississippi solution cleanup completed successfully" -ForegroundColor Green
    }

    if (-not $SkipSamples) {
        Write-Host "Running cleanup for samples.slnx…" -ForegroundColor Cyan
        & $sampleCleanup
        if ($LASTEXITCODE -ne 0) {
            throw "Sample solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "✓ Sample solution cleanup completed successfully" -ForegroundColor Green
    }

    Write-Host "🎉 All cleanup operations completed successfully!" -ForegroundColor Green
} catch {
    Write-Error "Cleanup failed: $_"
    exit 1
} 