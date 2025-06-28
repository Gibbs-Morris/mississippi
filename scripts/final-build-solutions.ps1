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

Write-Host "Restoring dotnet tools..." -ForegroundColor Cyan
if (Test-Path ".config/dotnet-tools.json") {
    dotnet tool restore | Out-Null
}

Write-Host "Restoring packages for mississippi.slnx..." -ForegroundColor Cyan
dotnet restore "mississippi.slnx"

Write-Host "Restoring packages for samples.slnx..." -ForegroundColor Cyan
dotnet restore "samples.slnx"

Write-Host "Building mississippi.slnx with warnings as errors (Configuration: $Configuration)..." -ForegroundColor Cyan
dotnet build "mississippi.slnx" --configuration $Configuration --no-restore --no-incremental --warnaserror

Write-Host "Building samples.slnx with warnings as errors (Configuration: $Configuration)..." -ForegroundColor Cyan
dotnet build "samples.slnx" --configuration $Configuration --no-restore --no-incremental --warnaserror 