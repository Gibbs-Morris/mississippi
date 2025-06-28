#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Restoring dotnet tools..." -ForegroundColor Cyan
if (Test-Path ".config/dotnet-tools.json") {
    dotnet tool restore | Out-Null
}

Write-Host "Restoring packages for samples.slnx..." -ForegroundColor Cyan
dotnet restore "samples.slnx"

Write-Host "Running unit tests for samples.slnx (Configuration: $Configuration)..." -ForegroundColor Cyan
dotnet test "samples.slnx" --configuration $Configuration --no-restore --results-directory "./test-results" --logger "trx;LogFileName=test_results.trx" 