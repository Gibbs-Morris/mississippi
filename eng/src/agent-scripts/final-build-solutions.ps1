#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

<#
    Performs a final, strict build of both solutions with **warnings treated as errors**.
    Intended as the last gate before merging changes to the main branch.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Write-Host "=== FINAL BUILD WITH WARNINGS AS ERRORS ===" -ForegroundColor Yellow
    Write-Host "Zero-tolerance build: All warnings will be treated as compilation errors"
    Write-Host "This is the final quality gate before code can be merged"
    Write-Host ""
    
    Write-Host "[1/5] Restoring dotnet tools..." -ForegroundColor Cyan
    if (Test-Path ".config/dotnet-tools.json") {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to restore dotnet tools"
        }
        Write-Host "SUCCESS: Dotnet tools restored successfully" -ForegroundColor Green
    }

    Write-Host "[2/5] Restoring NuGet packages for mississippi.slnx..." -ForegroundColor Cyan
    dotnet restore "mississippi.slnx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages for mississippi.slnx"
    }
    Write-Host "SUCCESS: Mississippi packages restored successfully" -ForegroundColor Green

    Write-Host "[3/5] Restoring NuGet packages for samples.slnx..." -ForegroundColor Cyan
    dotnet restore "samples.slnx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages for samples.slnx"
    }
    Write-Host "SUCCESS: Sample packages restored successfully" -ForegroundColor Green

    Write-Host "[4/5] Building mississippi.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "Build flags: --no-restore --no-incremental --warnaserror"
    Write-Host "WARNING: Any compiler warning will cause build failure"
    dotnet build "mississippi.slnx" --configuration $Configuration --no-restore --no-incremental --warnaserror
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build mississippi.slnx with warnings as errors"
    }
    Write-Host "SUCCESS: Mississippi solution built with zero warnings" -ForegroundColor Green

    Write-Host "[5/5] Building samples.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "Build flags: --no-restore --no-incremental --warnaserror"
    Write-Host "WARNING: Any compiler warning will cause build failure"
    dotnet build "samples.slnx" --configuration $Configuration --no-restore --no-incremental --warnaserror
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build samples.slnx with warnings as errors"
    }
    Write-Host "SUCCESS: Sample solution built with zero warnings" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== FINAL BUILD COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "ZERO WARNINGS DETECTED | Both solutions meet production quality standards"
    Write-Host "Code is ready for merge to main branch"
} catch {
    Write-Error "=== FINAL BUILD FAILED ===: $_"
    Write-Error "Build failed due to warnings or errors. All issues must be resolved before merge."
    exit 1
}
