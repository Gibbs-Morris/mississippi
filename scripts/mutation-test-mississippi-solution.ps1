#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SLNX_PATH = "mississippi.slnx"
$SLN_PATH  = "mississippi.sln"

Write-Host "Restoring dotnet tools (including Stryker)..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "Generating $SLN_PATH from $SLNX_PATH..." -ForegroundColor Cyan
dotnet tool run slngen "$SLNX_PATH" --solutionfile "$SLN_PATH" --launch false

Write-Host "Restoring packages for $SLN_PATH..." -ForegroundColor Cyan
dotnet restore "$SLN_PATH"

Write-Host "Running Stryker mutation tests..." -ForegroundColor Cyan
dotnet stryker --solution "$SLN_PATH" 