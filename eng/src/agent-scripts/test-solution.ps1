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
$resolvedSolutionPath = (Resolve-Path -LiteralPath $SolutionPath -ErrorAction Stop).Path
$lexicalSolutionRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedSolutionPath)
$relativeSolutionPath = [System.IO.Path]::GetRelativePath($lexicalSolutionRoot, $resolvedSolutionPath)
$executionLease = $null

try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $lexicalSolutionRoot -OperationId "test-solution-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    $resolvedSolutionPath = Join-Path $executionLease.RepositoryRoot $relativeSolutionPath
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
