#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SLNX_PATH = "mississippi.slnx"
$SLN_PATH  = "mississippi.sln"
$DOTSETTINGS_PATH = "Directory.DotSettings"

try {
    Write-Host "=== MISSISSIPPI SOLUTION CODE CLEANUP PROCESS ===" -ForegroundColor Yellow
    Write-Host "Running ReSharper CleanupCode with 'Built-in: Full Cleanup' profile"
    Write-Host ""
    
    Write-Host "[1/3] Restoring dotnet tools (including ReSharper CLI)..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dotnet tools"
    }
    Write-Host "SUCCESS: Dotnet tools restored, ReSharper CLI available" -ForegroundColor Green

    Write-Host "[2/3] Generating $SLN_PATH from $SLNX_PATH using SlnGen..." -ForegroundColor Cyan
    Write-Host "SlnGen converts .slnx format to .sln format for ReSharper compatibility"
    dotnet tool run slngen "$SLNX_PATH" --solutionfile "$SLN_PATH" --launch false
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate solution file"
    }
    Write-Host "SUCCESS: Solution file generated for ReSharper processing" -ForegroundColor Green

    Write-Host "[3/3] Running ReSharper CleanupCode on generated solution..." -ForegroundColor Cyan
    Write-Host "Cleanup profile: 'Built-in: Full Cleanup'"
    Write-Host "Settings file: $DOTSETTINGS_PATH"
    Write-Host "Target solution: $SLN_PATH"
    dotnet tool run jb cleanupcode --profile="Built-in: Full Cleanup" --settings="$DOTSETTINGS_PATH" "$SLN_PATH"
    if ($LASTEXITCODE -ne 0) {
        throw "ReSharper CleanupCode failed"
    }
    Write-Host "SUCCESS: ReSharper code cleanup completed" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== MISSISSIPPI SOLUTION CLEANUP COMPLETED ===" -ForegroundColor Green
    Write-Host "All code files have been formatted according to project standards"
} catch {
    Write-Error "=== MISSISSIPPI SOLUTION CLEANUP FAILED ===: $_"
    exit 1
} 