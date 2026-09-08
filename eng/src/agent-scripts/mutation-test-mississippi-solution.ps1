#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-MississippiSolutionMutationTests -RepoRoot $repoRoot -Configuration $Configuration
}
catch {
    Write-Error "=== MISSISSIPPI SOLUTION MUTATION TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}

exit 0
