#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SLNX_PATH = "samples.slnx"
$SLN_PATH  = "samples.sln"
$DOTSETTINGS_PATH = "Directory.DotSettings"

Write-Host "Restoring dotnet tools (including ReSharper)..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "Generating $SLN_PATH from $SLNX_PATH..." -ForegroundColor Cyan
dotnet tool run slngen "$SLNX_PATH" --solutionfile "$SLN_PATH" --launch false

Write-Host "Running ReSharper CleanupCode..." -ForegroundColor Cyan
dotnet tool run jb cleanupcode --profile="Built-in: Full Cleanup" --settings="$DOTSETTINGS_PATH" "$SLN_PATH" 