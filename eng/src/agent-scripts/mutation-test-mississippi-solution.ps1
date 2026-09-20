#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$LeaseDirectory,
    [switch]$ReportOnly,
    [switch]$SkipLease
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$executionLease = $null

try {
    if (-not $SkipLease) {
        $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "mutation-mississippi-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
        $repoRoot = $executionLease.RepositoryRoot
    }
    Invoke-MississippiSolutionMutationTests -RepoRoot $repoRoot -Configuration $Configuration -ReportOnly:$ReportOnly
}
catch {
    Write-Error "=== MISSISSIPPI SOLUTION MUTATION TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}

exit 0
