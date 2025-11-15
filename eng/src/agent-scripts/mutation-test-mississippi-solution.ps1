#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-MississippiSolutionMutationTests -RepoRoot $repoRoot
}
catch {
    Write-Error "=== MISSISSIPPI SOLUTION MUTATION TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}
