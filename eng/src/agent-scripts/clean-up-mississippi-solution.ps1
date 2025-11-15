#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-MississippiSolutionCleanup -RepoRoot $repoRoot
}
catch {
    Write-Error "=== MISSISSIPPI SOLUTION CLEANUP FAILED ===: $($_.Exception.Message)"
    exit 1
}
