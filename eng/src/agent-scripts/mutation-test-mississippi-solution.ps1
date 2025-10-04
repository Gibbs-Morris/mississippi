#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SLNX_PATH = "mississippi.slnx"
$SLN_PATH  = "mississippi.sln"

try {
    Write-Host "=== MISSISSIPPI SOLUTION MUTATION TESTING PROCESS ===" -ForegroundColor Yellow
    Write-Host "Running Stryker.NET mutation testing to validate test quality"
    Write-Host "Mutation testing ensures tests can detect code changes"
    Write-Host ""
    
    Write-Host "[1/4] Restoring dotnet tools (including Stryker.NET)..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dotnet tools"
    }
    Write-Host "SUCCESS: Dotnet tools restored, Stryker.NET available" -ForegroundColor Green

    Write-Host "[2/4] Generating $SLN_PATH from $SLNX_PATH using SlnGen..." -ForegroundColor Cyan
    Write-Host "SlnGen converts .slnx format to .sln format for Stryker compatibility"
    dotnet tool run slngen "$SLNX_PATH" --solutionfile "$SLN_PATH" --launch false
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate solution file"
    }
    Write-Host "SUCCESS: Solution file generated for Stryker processing" -ForegroundColor Green

    Write-Host "[3/4] Restoring NuGet packages for generated solution..." -ForegroundColor Cyan
    dotnet restore "$SLN_PATH"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages for $SLN_PATH"
    }
    Write-Host "SUCCESS: NuGet packages restored for mutation testing" -ForegroundColor Green

    Write-Host "[4/4] Executing Stryker.NET mutation testing..." -ForegroundColor Cyan
    Write-Host "Stryker will inject mutations into source code and verify tests detect them"
    Write-Host "This process validates the effectiveness of the test suite"
    Write-Host "Target solution: $SLN_PATH"
    $mutationRoot = Join-Path ".scratchpad" "mutation-test-results"
    if (-not (Test-Path -LiteralPath $mutationRoot)) {
        New-Item -ItemType Directory -Path $mutationRoot | Out-Null
    }
    $mutationOutput = Join-Path $mutationRoot (Get-Date -Format 'yyyy-MM-dd.HH-mm-ss')
    if (-not (Test-Path -LiteralPath $mutationOutput)) {
        New-Item -ItemType Directory -Path $mutationOutput | Out-Null
    }
    dotnet stryker --solution "$SLN_PATH" --output "$mutationOutput"
    if ($LASTEXITCODE -ne 0) {
        throw "Stryker mutation tests failed"
    }
    Write-Host "SUCCESS: Mutation testing completed with acceptable scores" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== MISSISSIPPI SOLUTION MUTATION TESTING COMPLETED ===" -ForegroundColor Green
    Write-Host "Test quality validated | Mutation score meets project standards"
} catch {
    Write-Error "=== MISSISSIPPI SOLUTION MUTATION TESTING FAILED ===: $_"
    exit 1
}
