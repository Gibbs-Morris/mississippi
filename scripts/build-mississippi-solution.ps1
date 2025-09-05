#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Write-Host "=== MISSISSIPPI SOLUTION BUILD PROCESS ===" -ForegroundColor Yellow
    Write-Host "Building mississippi.slnx in $Configuration mode"
    Write-Host ""
    
    Write-Host "[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)..." -ForegroundColor Cyan
    # Ensure any local tools (e.g., GitVersion, slngen) are available
    if (Test-Path ".config/dotnet-tools.json") {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to restore dotnet tools"
        }
        Write-Host "SUCCESS: Dotnet tools restored successfully" -ForegroundColor Green
    }

    Write-Host "[2/3] Restoring NuGet packages for mississippi.slnx..." -ForegroundColor Cyan
    dotnet restore "mississippi.slnx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages for mississippi.slnx"
    }
    Write-Host "SUCCESS: NuGet packages restored successfully" -ForegroundColor Green

    Write-Host "[3/3] Compiling mississippi.slnx (Configuration: $Configuration)..." -ForegroundColor Cyan
    Write-Host "Build flags: --no-restore --no-incremental"
    dotnet build "mississippi.slnx" --configuration $Configuration --no-restore --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build mississippi.slnx"
    }
    Write-Host "SUCCESS: Mississippi solution compiled successfully" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== MISSISSIPPI SOLUTION BUILD COMPLETED ===" -ForegroundColor Green
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
} catch {
    Write-Error "=== MISSISSIPPI SOLUTION BUILD FAILED ===: $_"
    exit 1
}
