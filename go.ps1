#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs the orchestrate-solutions.ps1 script for the repository.
#>

# Determine the repository root directory (this script resides there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Build the path to the orchestrate script
$orchestrateScript = Join-Path $repoRoot 'scripts\orchestrate-solutions.ps1'

Write-Host "Running orchestrate-solutions.ps1..."

# Execute the orchestrate script
& $orchestrateScript @args
