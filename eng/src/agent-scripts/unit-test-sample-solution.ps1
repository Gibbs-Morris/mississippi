#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $repoRoot
}
catch {
    Write-Error "=== SAMPLE SOLUTION UNIT TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}
