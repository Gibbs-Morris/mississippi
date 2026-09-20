#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs code-style cleanup (ReSharper CleanupCode) for both solutions in the repository.
.DESCRIPTION
        This is a convenience wrapper that sequentially invokes:
            • eng/src/agent-scripts/clean-up-mississippi-solution.ps1
            • eng/src/agent-scripts/clean-up-sample-solution.ps1
    It ensures both the core and sample solutions are formatted with the same rules.
#>

[CmdletBinding()]
param(
    # Switch to skip cleaning the sample solution if desired.
    [switch]$SkipSamples,

    # Switch to skip cleaning the main Mississippi solution if desired.
    [switch]$SkipMississippi,

    # Shared coordination directory for cross-account worktree leases.
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })

# Determine repository root (this file lives there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1') -Force
$executionLease = $null

try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "cleanup-wrapper-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    $repoRoot = $executionLease.RepositoryRoot
    $mississippiCleanup = Join-Path $repoRoot 'eng' 'src' 'agent-scripts' 'clean-up-mississippi-solution.ps1'
    $sampleCleanup = Join-Path $repoRoot 'eng' 'src' 'agent-scripts' 'clean-up-sample-solution.ps1'
    $leaseArguments = @('-SkipLease')
    if (-not [string]::IsNullOrWhiteSpace($LeaseDirectory)) { $leaseArguments += @('-LeaseDirectory', $LeaseDirectory) }

    if (-not $SkipMississippi) {
        Write-Host "=== STEP 1: MISSISSIPPI SOLUTION CLEANUP ===" -ForegroundColor Yellow
        Write-Host "Running ReSharper CleanupCode on mississippi.slnx..."
        & $powerShellPath -NoProfile -File $mississippiCleanup @leaseArguments
        if ($LASTEXITCODE -ne 0) {
            throw "Mississippi solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "SUCCESS: Mississippi solution cleanup completed successfully" -ForegroundColor Green
        Write-Host ""
    }

    if (-not $SkipSamples) {
        Write-Host "=== STEP 2: SAMPLE SOLUTION CLEANUP ===" -ForegroundColor Yellow
        Write-Host "Running ReSharper CleanupCode on samples.slnx..."
        & $powerShellPath -NoProfile -File $sampleCleanup @leaseArguments
        if ($LASTEXITCODE -ne 0) {
            throw "Sample solution cleanup failed with exit code: $LASTEXITCODE"
        }
        Write-Host "SUCCESS: Sample solution cleanup completed successfully" -ForegroundColor Green
        Write-Host ""
    }

    Write-Host "=== ALL CLEANUP OPERATIONS COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host "Both mississippi.slnx and samples.slnx have been cleaned with ReSharper rules"
} catch {
    Write-Error "=== FAILURE: Cleanup operation failed: $_"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}

exit 0
