#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Write-Host "=== MISSISSIPPI SOLUTION UNIT TESTING PROCESS ===" -ForegroundColor Yellow
    Write-Host "Running comprehensive unit and integration tests"
    Write-Host ""
    
    Write-Host "[1/3] Restoring dotnet tools..." -ForegroundColor Cyan
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

    Write-Host "[3/3] Executing unit tests for mississippi.slnx..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "Test flags: --no-restore"
    Write-Host "Results directory: ./test-results"
    Write-Host "Logger: TRX format (test_results.trx)"
    dotnet test "mississippi.slnx" --configuration $Configuration --no-restore --results-directory "./test-results" --logger "trx;LogFileName=test_results.trx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to run unit tests for mississippi.slnx"
    }
    Write-Host "SUCCESS: All unit tests passed" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== MISSISSIPPI SOLUTION UNIT TESTING COMPLETED ===" -ForegroundColor Green
    Write-Host "All tests passed | Results saved to: ./test-results/test_results.trx"
} catch {
    Write-Error "=== MISSISSIPPI SOLUTION UNIT TESTING FAILED ===: $_"
    exit 1
}
