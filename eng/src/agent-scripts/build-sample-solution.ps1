#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Write-Host "=== SAMPLE SOLUTION BUILD PROCESS ===" -ForegroundColor Yellow
    Write-Host "Building samples.slnx in $Configuration mode"
    Write-Host ""
    
    Write-Host "[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)..." -ForegroundColor Cyan
    if (Test-Path ".config/dotnet-tools.json") {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to restore dotnet tools"
        }
        Write-Host "SUCCESS: Dotnet tools restored successfully" -ForegroundColor Green
    }

    Write-Host "[2/3] Restoring NuGet packages for samples.slnx..." -ForegroundColor Cyan
    dotnet restore "samples.slnx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages for samples.slnx"
    }
    Write-Host "SUCCESS: NuGet packages restored successfully" -ForegroundColor Green

    Write-Host "[3/3] Compiling samples.slnx (Configuration: $Configuration)..." -ForegroundColor Cyan
    Write-Host "Build flags: --no-restore --no-incremental"
    dotnet build "samples.slnx" --configuration $Configuration --no-restore --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build samples.slnx"
    }
    Write-Host "SUCCESS: Sample solution compiled successfully" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== SAMPLE SOLUTION BUILD COMPLETED ===" -ForegroundColor Green
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
} catch {
    Write-Error "=== SAMPLE SOLUTION BUILD FAILED ===: $_"
    exit 1
}
