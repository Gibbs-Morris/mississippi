#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs the orchestrate-solutions.ps1 script for the repository.
#>

# Accept optional parameters forwarded from callers
param(
    [switch]$SkipCleanup,
    [switch]$IncludeMutation,
    [string]$Configuration = 'Release'
)

# Determine the repository root directory (this script resides there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Build the path to the orchestrate script
$orchestrateScript = Join-Path $repoRoot 'eng\src\agent-scripts\orchestrate-solutions.ps1'

Write-Host "=== STARTING MAIN PIPELINE ORCHESTRATION ===" -ForegroundColor Yellow
Write-Host "Executing orchestrate-solutions.ps1 script..."

try {
    # Execute the orchestrate script and wait for completion
    & $orchestrateScript -Configuration $Configuration -SkipCleanup:$SkipCleanup -IncludeMutation:$IncludeMutation
    if ($LASTEXITCODE -ne 0) {
        throw "orchestrate-solutions.ps1 failed with exit code $LASTEXITCODE"
    }
    Write-Host "=== SUCCESS: Main pipeline orchestration completed successfully ===" -ForegroundColor Green
} catch {
    Write-Error "=== FAILURE: Main pipeline orchestration failed: $_"
    exit 1
}
