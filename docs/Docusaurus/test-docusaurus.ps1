#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local build and test script for Docusaurus site
.DESCRIPTION
    Mirrors the GitHub Actions workflow steps for building and testing the Docusaurus site locally.
    Follows repository PowerShell script standards with strict mode and explicit exit codes.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    Write-Host "=== Docusaurus Build and Test Script ===" -ForegroundColor Cyan
    Write-Host ""

    $DocusaurusPath = $PSScriptRoot
    Push-Location $DocusaurusPath

    # Step 1: Install dependencies
    Write-Host "[1/4] Installing npm dependencies..." -ForegroundColor Yellow
    npm ci --ignore-scripts
    if ($LASTEXITCODE -ne 0) {
        throw "npm ci failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Dependencies installed" -ForegroundColor Green
    Write-Host ""

    # Step 2: Build Docusaurus site
    Write-Host "[2/4] Building Docusaurus site..." -ForegroundColor Yellow
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Build completed" -ForegroundColor Green
    Write-Host ""

    # Step 3: Install Playwright browsers
    Write-Host "[3/4] Installing Playwright browsers..." -ForegroundColor Yellow
    npx playwright install --with-deps chromium
    if ($LASTEXITCODE -ne 0) {
        throw "Playwright install failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Playwright browsers installed" -ForegroundColor Green
    Write-Host ""

    # Step 4: Run Playwright tests
    Write-Host "[4/4] Running Playwright tests..." -ForegroundColor Yellow
    npm test
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Tests passed" -ForegroundColor Green
    Write-Host ""

    Write-Host "=== All steps completed successfully ===" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
