#!/usr/bin/env pwsh

[CmdletBinding()]
param([string]$Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-MississippiSolutionMutationTests -RepoRoot $repoRoot -Configuration $Configuration
}
catch {
    Write-Host "=== MISSISSIPPI SOLUTION MUTATION TESTING FAILED ===: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

exit 0
