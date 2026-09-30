#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SolutionPath,
    [string]$Configuration = 'Release',
    [string[]]$TestLevels = @('L0Tests', 'L1Tests'),
    [switch]$NoBuild,
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RepositoryAutomation.psm1')
$resolvedSolutionPath = Resolve-RepositoryExecutionPath -Path $SolutionPath
$solutionRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedSolutionPath)
$relativeSolutionPath = [System.IO.Path]::GetRelativePath($solutionRoot, $resolvedSolutionPath)
$executionLease = $null

try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $solutionRoot -OperationId "test-solution-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    $leasedSolutionPath = Join-Path $executionLease.RepositoryRoot $relativeSolutionPath
    $resolvedSolutionPath = Resolve-RepositoryExecutionPath -Path $leasedSolutionPath
    $resolvedSolutionRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedSolutionPath)
    $pathComparison = if ($IsWindows -or $IsMacOS) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    if (-not [string]::Equals($resolvedSolutionRoot, $executionLease.RepositoryRoot, $pathComparison)) {
        throw "Solution path '$SolutionPath' resolves outside the leased repository root '$($executionLease.RepositoryRoot)'."
    }
    $arguments = @()
    if ($NoBuild) { $arguments += '--no-build' }
    Invoke-SolutionTests -SolutionPath $resolvedSolutionPath -Configuration $Configuration -TestLevels $TestLevels `
        -AdditionalArguments $arguments | Out-Host
    exit 0
}
catch {
    [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}
