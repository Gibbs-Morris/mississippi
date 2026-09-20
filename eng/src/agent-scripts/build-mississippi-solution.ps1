#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$LeaseDirectory,
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
        $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "build-mississippi-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
        $repoRoot = $executionLease.RepositoryRoot
    }
    Invoke-MississippiSolutionBuild -Configuration $Configuration -RepoRoot $repoRoot
}
catch {
    Write-Error "=== MISSISSIPPI SOLUTION BUILD FAILED ===: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}

exit 0
