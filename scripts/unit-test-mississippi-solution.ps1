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

Write-Host "Restoring packages for mississippi.slnx..." -ForegroundColor Cyan
dotnet restore "mississippi.slnx"

Write-Host "Running unit tests for mississippi.slnx (Configuration: $Configuration)..." -ForegroundColor Cyan
dotnet test "mississippi.slnx" --configuration $Configuration --no-restore --results-directory "./test-results" --logger "trx;LogFileName=test_results.trx" 