#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs code-style cleanup (ReSharper CleanupCode) for solutions in the repository.
.DESCRIPTION
    Unified cleanup script that processes one or more .slnx solution files with ReSharper CleanupCode.
    Supports a check mode to verify that cleanup produces no changes, ensuring deterministic formatting
    across Windows and Linux platforms.
.PARAMETER Solutions
    Array of .slnx solution files to process. Defaults to mississippi.slnx and samples.slnx.
.PARAMETER Check
    When specified, runs cleanup and then verifies no changes were made. Exits with code 1 if changes detected.
.EXAMPLE
    pwsh ./clean-up.ps1
    Runs cleanup on both default solutions.
.EXAMPLE
    pwsh ./clean-up.ps1 -Check
    Runs cleanup and verifies no modifications occurred.
.EXAMPLE
    pwsh ./clean-up.ps1 -Solutions @("mississippi.slnx")
    Runs cleanup only on the Mississippi solution.
#>

param(
    [string[]]$Solutions = @("mississippi.slnx", "samples.slnx"),
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Force execution from repository root
try {
    $gitRoot = git rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Not in a git repository"
    }
    $repoRoot = $gitRoot
    Set-Location $repoRoot
    Write-Host "Repository root: $repoRoot" -ForegroundColor Cyan
} catch {
    Write-Error "Failed to determine repository root: $_"
    exit 1
}

$DOTSETTINGS_PATH = Join-Path $repoRoot "Directory.DotSettings"

try {
    Write-Host "=== CODE CLEANUP PROCESS ===" -ForegroundColor Yellow
    Write-Host "Solutions: $($Solutions -join ', ')"
    Write-Host "Check mode: $Check"
    Write-Host ""

    # Restore dotnet tools once
    Write-Host "[1/3] Restoring dotnet tools (including ReSharper CLI and SlnGen)..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dotnet tools"
    }
    Write-Host "SUCCESS: Dotnet tools restored" -ForegroundColor Green
    Write-Host ""

    # Process each solution
    $solutionIndex = 0
    foreach ($slnxFile in $Solutions) {
        $solutionIndex++
        $slnxPath = Join-Path $repoRoot $slnxFile
        
        if (-not (Test-Path $slnxPath)) {
            throw "Solution file not found: $slnxPath"
        }

        $slnPath = $slnxPath -replace '\.slnx$', '.sln'
        $slnName = Split-Path -Leaf $slnxFile

        Write-Host "=== PROCESSING SOLUTION $solutionIndex/$($Solutions.Count): $slnName ===" -ForegroundColor Yellow
        
        # Generate .sln from .slnx
        Write-Host "[2/3] Generating $slnPath from $slnxPath using SlnGen..." -ForegroundColor Cyan
        Write-Host "SlnGen converts .slnx format to .sln format for ReSharper compatibility"
        dotnet tool run slngen "$slnxPath" --solutionfile "$slnPath" --launch false
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to generate solution file: $slnPath"
        }
        Write-Host "SUCCESS: Solution file generated" -ForegroundColor Green

        # Run ReSharper CleanupCode
        Write-Host "[3/3] Running ReSharper CleanupCode..." -ForegroundColor Cyan
        Write-Host "Profile: 'Built-in: Full Cleanup'"
        Write-Host "Settings: $DOTSETTINGS_PATH"
        Write-Host "Target: $slnPath"
        dotnet tool run jb cleanupcode --profile="Built-in: Full Cleanup" --settings="$DOTSETTINGS_PATH" "$slnPath"
        if ($LASTEXITCODE -ne 0) {
            throw "ReSharper CleanupCode failed for $slnName"
        }
        Write-Host "SUCCESS: ReSharper cleanup completed for $slnName" -ForegroundColor Green
        Write-Host ""
    }

    # Check mode: verify no changes
    if ($Check) {
        Write-Host "=== CHECKING FOR MODIFICATIONS ===" -ForegroundColor Yellow
        $changes = git status --porcelain=v1 -uno
        if ($changes) {
            Write-Host "ERROR: Cleanup modified files. Changes detected:" -ForegroundColor Red
            Write-Host ""
            git diff --color=always
            Write-Host ""
            Write-Host "To fix locally, run: pwsh ./clean-up.ps1" -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host "No modifications detected. Cleanup check passed." -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "=== CLEANUP COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "All solutions have been formatted according to project standards"
} catch {
    Write-Error "=== CLEANUP FAILED ===: $_"
    exit 1
}
