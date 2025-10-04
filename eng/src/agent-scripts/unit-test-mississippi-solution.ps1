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
    Write-Host "Test flags: --no-restore --collect:XPlat Code Coverage"
    $resultsDirectory = Join-Path ".scratchpad" "coverage-test-results"
    if (-not (Test-Path -LiteralPath $resultsDirectory)) {
        New-Item -ItemType Directory -Path $resultsDirectory | Out-Null
    }

    $runIdentifier = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $runDirectory = Join-Path $resultsDirectory $runIdentifier
    if (-not (Test-Path -LiteralPath $runDirectory)) {
        New-Item -ItemType Directory -Path $runDirectory | Out-Null
    }

    Write-Host "Results directory: $runDirectory"
    Write-Host "Logger: TRX format (test_results.trx)"
    dotnet test "mississippi.slnx" --configuration $Configuration --no-restore --results-directory $runDirectory --logger "trx;LogFileName=test_results.trx" --collect "XPlat Code Coverage"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to run unit tests for mississippi.slnx"
    }

    $coverageFiles = Get-ChildItem -Path $runDirectory -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
        throw "Unit tests completed but no coverage reports were produced in '$runDirectory'."
    }

    $reportsArg = ($coverageFiles | ForEach-Object { '"{0}"' -f $_.FullName }) -join ';'
    $mergedOutputDir = Join-Path $runDirectory 'coverage-merged'
    if (Test-Path -LiteralPath $mergedOutputDir) {
        Remove-Item -LiteralPath $mergedOutputDir -Recurse -Force
    }
    Write-Host "Merging coverage reports with ReportGenerator (found $($coverageFiles.Count))" -ForegroundColor Cyan
    dotnet tool run reportgenerator "-reports:$reportsArg" "-targetdir:$mergedOutputDir" "-reporttypes:Cobertura" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator failed while merging coverage reports."
    }

    $mergedCobertura = Join-Path $mergedOutputDir 'Cobertura.xml'
    if (-not (Test-Path -LiteralPath $mergedCobertura)) {
        throw "ReportGenerator did not create an aggregated Cobertura.xml report."
    }

    $finalCoveragePath = Join-Path $runDirectory 'coverage.cobertura.xml'
    Copy-Item -LiteralPath $mergedCobertura -Destination $finalCoveragePath -Force

    Write-Host "SUCCESS: All unit tests passed" -ForegroundColor Green
    Write-Host "Aggregated coverage report: $finalCoveragePath" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== MISSISSIPPI SOLUTION UNIT TESTING COMPLETED ===" -ForegroundColor Green
    $resultsFile = Join-Path $runDirectory "test_results.trx"
    Write-Host "All tests passed | Results saved to: $resultsFile"
    Write-Host "Coverage report ready for summarize-coverage-gaps.ps1" -ForegroundColor Green
} catch {
    Write-Error "=== MISSISSIPPI SOLUTION UNIT TESTING FAILED ===: $_"
    exit 1
}
