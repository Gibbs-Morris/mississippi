#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Restoring dotnet tools..." -ForegroundColor Cyan
# Ensure any local tools (e.g., GitVersion, slngen) are available
if (Test-Path ".config/dotnet-tools.json") {
    dotnet tool restore | Out-Null
}

Write-Host "Restoring packages for mississippi.slnx..." -ForegroundColor Cyan
dotnet restore "mississippi.slnx"

Write-Host "Building mississippi.slnx (Configuration: $Configuration)..." -ForegroundColor Cyan
dotnet build "mississippi.slnx" --configuration $Configuration --no-restore --no-incremental 