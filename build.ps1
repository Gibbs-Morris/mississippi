#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds the Mississippi solution and (optionally) the Samples solution.
.DESCRIPTION
    Convenience wrapper that invokes:
        • eng/src/agent-scripts/build-mississippi-solution.ps1
        • eng/src/agent-scripts/build-sample-solution.ps1
    in a predictable sequence with consistent logging. By default both
    solutions are built in Release configuration with warnings treated as
    errors (enforced inside the underlying scripts / MSBuild settings).

    This script intentionally performs only compilation (no cleanup, tests,
    or mutation analysis). For the full pipeline including cleanup, tests,
    coverage, and mutation testing use ./go.ps1. For the strict final build
    of both solutions (warnings-as-errors) use quick-build.ps1 or
    eng/src/agent-scripts/final-build-solutions.ps1 directly.

.PARAMETER Configuration
    The build configuration to use (defaults to Release).

.PARAMETER SkipSamples
    When present, skips building the Samples solution.

.PARAMETER SkipMississippi
    When present, skips building the core Mississippi solution.

.EXAMPLE
    pwsh ./build.ps1
        Builds both solutions in Release.

.EXAMPLE
    pwsh ./build.ps1 -Configuration Debug -SkipSamples
        Builds only the Mississippi solution in Debug.

.NOTES
    Follows repository build rules: zero warnings policy. Any non‑zero
    exit code from underlying scripts causes this wrapper to fail.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipSamples,
    [switch]$SkipMississippi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Determine repository root (this script resides there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Underlying scripts
$buildMississippi = Join-Path $repoRoot 'eng\src\agent-scripts\build-mississippi-solution.ps1'
$buildSamples     = Join-Path $repoRoot 'eng\src\agent-scripts\build-sample-solution.ps1'

function Invoke-BuildStep {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][scriptblock]$Action
    )
    Write-Host "=== $Title ===" -ForegroundColor Yellow
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Title failed with exit code $LASTEXITCODE"
    }
    Write-Host "SUCCESS: $Title completed" -ForegroundColor Green
    Write-Host ''
}

try {
    if ($SkipMississippi -and $SkipSamples) {
        Write-Warning 'Both -SkipMississippi and -SkipSamples specified; nothing to build.'
        return
    }

    if (-not $SkipMississippi) {
        Invoke-BuildStep -Title 'STEP 1: Build Mississippi Solution' -Action { & $buildMississippi -Configuration $Configuration }
    }

    if (-not $SkipSamples) {
        Invoke-BuildStep -Title 'STEP 2: Build Samples Solution' -Action { & $buildSamples -Configuration $Configuration }
    }

    Write-Host '=== ALL REQUESTED BUILDS COMPLETED SUCCESSFULLY ===' -ForegroundColor Green
}
catch {
    Write-Error "=== BUILD FAILED === $_"
    exit 1
}
