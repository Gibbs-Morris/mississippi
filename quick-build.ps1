#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Performs a fast, strict build of both solutions by invoking scripts\final-build-solutions.ps1.
#>

# Determine the script directory (repo root)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Build the path to the final-build script
$finalBuildScript = Join-Path $scriptDir 'scripts\final-build-solutions.ps1'

Write-Host "Running final-build-solutions.ps1..."

# Execute the final-build script
& $finalBuildScript @args
