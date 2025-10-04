#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs ReSharper CleanupCode on one or more solution files to ensure consistent formatting.
.DESCRIPTION
    This script performs code cleanup using JetBrains ReSharper CLI tool with the "Built-in: Full Cleanup" profile.
    It generates temporary .sln files from .slnx format, runs cleanup on them, and optionally validates
    that no changes were made (useful in CI to ensure code is properly formatted).
.PARAMETER Solutions
    Array of solution files (.slnx) to clean. Defaults to both mississippi.slnx and samples.slnx.
.PARAMETER Check
    When specified, the script will fail if cleanup produces any modifications.
    This is useful in CI to enforce that all code is already properly formatted.
.EXAMPLE
    ./eng/cleanup.ps1
    Runs cleanup on both solutions (mississippi and samples).
.EXAMPLE
    ./eng/cleanup.ps1 -Solutions @("mississippi.slnx")
    Runs cleanup only on the mississippi solution.
.EXAMPLE
    ./eng/cleanup.ps1 -Check
    Runs cleanup on both solutions and fails if any files are modified.
#>

param(
    [string[]]$Solutions = @("mississippi.slnx", "samples.slnx"),
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    # Ensure we're in the repository root
    $repo = git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to determine repository root. Are you in a git repository?"
    }
    Set-Location $repo
    
    $DOTSETTINGS_PATH = Join-Path $repo "Directory.DotSettings"
    
    if (-not (Test-Path $DOTSETTINGS_PATH)) {
        throw "Directory.DotSettings not found at: $DOTSETTINGS_PATH"
    }

    Write-Host "=== RESHARPER CODE CLEANUP ===" -ForegroundColor Yellow
    Write-Host "Repository: $repo"
    Write-Host "Settings: $DOTSETTINGS_PATH"
    Write-Host "Solutions: $($Solutions -join ', ')"
    if ($Check) {
        Write-Host "Mode: CHECK (will fail if changes detected)" -ForegroundColor Cyan
    }
    Write-Host ""

    # Restore dotnet tools from manifest
    Write-Host "[1/3] Restoring dotnet tools from manifest..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dotnet tools"
    }
    Write-Host "SUCCESS: Dotnet tools restored" -ForegroundColor Green
    Write-Host ""

    # Process each solution
    $solutionCount = $Solutions.Count
    $currentSolution = 0
    
    foreach ($slnx in $Solutions) {
        $currentSolution++
        $sln = [IO.Path]::ChangeExtension($slnx, ".sln")
        
        Write-Host "[2/3] Processing solution $currentSolution of ${solutionCount}: $slnx" -ForegroundColor Cyan
        
        # Generate .sln from .slnx
        Write-Host "  → Generating $sln from $slnx using SlnGen..." -ForegroundColor Gray
        dotnet tool run slngen "$slnx" --solutionfile "$sln" --launch false | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to generate solution file: $sln"
        }
        Write-Host "  ✓ Solution file generated" -ForegroundColor Green
        
        # Run ReSharper cleanup
        Write-Host "  → Running ReSharper CleanupCode..." -ForegroundColor Gray
        Write-Host "    Profile: Built-in: Full Cleanup" -ForegroundColor Gray
        dotnet tool run jb cleanupcode --profile="Built-in: Full Cleanup" --settings="$DOTSETTINGS_PATH" "$sln" | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "ReSharper CleanupCode failed for: $sln"
        }
        Write-Host "  ✓ Cleanup completed" -ForegroundColor Green
        Write-Host ""
    }

    Write-Host "[3/3] Cleanup process completed" -ForegroundColor Green
    Write-Host ""

    # Check for modifications if requested
    if ($Check) {
        Write-Host "Checking for modifications..." -ForegroundColor Cyan
        $changes = git status --porcelain=v1 -uno
        if ($changes) {
            Write-Host "ERROR: Cleanup produced modifications" -ForegroundColor Red
            Write-Host ""
            Write-Host "Modified files:" -ForegroundColor Yellow
            Write-Host $changes
            Write-Host ""
            Write-Host "Diff:" -ForegroundColor Yellow
            git diff --color=always | Out-Host
            Write-Host ""
            Write-Host "To fix this locally, run:" -ForegroundColor Yellow
            Write-Host "  pwsh ./eng/cleanup.ps1" -ForegroundColor White
            Write-Host ""
            throw "Cleanup produced modifications. Please run cleanup locally and commit the changes."
        }
        Write-Host "✓ No modifications detected - code is properly formatted" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "=== CLEANUP COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
} catch {
    Write-Error "=== CLEANUP FAILED ===: $_"
    exit 1
}
