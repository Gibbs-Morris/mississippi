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
Import-Module (Join-Path $PSScriptRoot 'RepositoryAutomation.psm1') -Force
$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$executionLease = $null

try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "test-solution-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    $arguments = @()
    if ($NoBuild) { $arguments += '--no-build' }
    Invoke-SolutionTests -SolutionPath $SolutionPath -Configuration $Configuration -TestLevels $TestLevels `
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
