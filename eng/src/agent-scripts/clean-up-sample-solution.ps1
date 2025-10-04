#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Wrapper script for cleaning up the Sample solution.
.DESCRIPTION
    This script delegates to the main clean-up.ps1 script at the repository root,
    processing only the samples.slnx solution. Maintained for backward compatibility
    with existing workflows and documentation.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    # Determine repository root
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $repoRoot = Resolve-Path (Join-Path $scriptDir "../../..")
    
    # Delegate to main cleanup script
    $mainCleanupScript = Join-Path $repoRoot "clean-up.ps1"
    
    Write-Host "=== SAMPLE SOLUTION CLEANUP (via unified script) ===" -ForegroundColor Yellow
    & $mainCleanupScript -Solutions @("samples.slnx")
    
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
} catch {
    Write-Error "Failed to run Sample solution cleanup: $_"
    exit 1
} 