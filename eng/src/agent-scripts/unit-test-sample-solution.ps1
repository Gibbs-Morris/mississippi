#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Write-Host "=== SAMPLE SOLUTION UNIT TESTING PROCESS ===" -ForegroundColor Yellow
    Write-Host "Running sample application tests (minimal coverage for demonstration)"
    Write-Host ""
    
    Write-Host "[1/3] Restoring dotnet tools..." -ForegroundColor Cyan
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

    Write-Host "[3/3] Executing unit tests for samples.slnx..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "Test flags: --no-restore"
    $resultsDirectory = Join-Path ".scratchpad" "coverage-test-results"
    if (-not (Test-Path -LiteralPath $resultsDirectory)) {
        New-Item -ItemType Directory -Path $resultsDirectory | Out-Null
    }
    Write-Host "Results directory: $resultsDirectory"
    Write-Host "Logger: TRX format (test_results.trx)"
    Write-Host "NOTE: Sample tests are for demonstration purposes only"
    dotnet test "samples.slnx" --configuration $Configuration --no-restore --results-directory $resultsDirectory --logger "trx;LogFileName=test_results.trx"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to run unit tests for samples.slnx"
    }
    Write-Host "SUCCESS: All sample tests passed" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== SAMPLE SOLUTION UNIT TESTING COMPLETED ===" -ForegroundColor Green
    $resultsFile = Join-Path $resultsDirectory "test_results.trx"
    Write-Host "Sample tests passed | Results saved to: $resultsFile"
} catch {
    Write-Error "=== SAMPLE SOLUTION UNIT TESTING FAILED ===: $_"
    exit 1
}
