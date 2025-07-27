#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SLNX_PATH = "samples.slnx"
$SLN_PATH  = "samples.sln"
$DOTSETTINGS_PATH = "Directory.DotSettings"

try {
    Write-Host "Restoring dotnet tools (including ReSharper)..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dotnet tools"
    }

    Write-Host "Generating $SLN_PATH from $SLNX_PATH..." -ForegroundColor Cyan
    dotnet tool run slngen "$SLNX_PATH" --solutionfile "$SLN_PATH" --launch false
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate solution file"
    }

    Write-Host "Running ReSharper CleanupCode..." -ForegroundColor Cyan
    dotnet tool run jb cleanupcode --profile="Built-in: Full Cleanup" --settings="$DOTSETTINGS_PATH" "$SLN_PATH"
    if ($LASTEXITCODE -ne 0) {
        throw "ReSharper CleanupCode failed"
    }

    Write-Host "Sample solution cleanup completed successfully" -ForegroundColor Green
} catch {
    Write-Error "Sample solution cleanup failed: $_"
    exit 1
} 