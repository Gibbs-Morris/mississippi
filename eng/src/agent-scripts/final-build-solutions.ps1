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
    Invoke-FinalSolutionsBuild -Configuration $Configuration -RepoRoot $repoRoot
}
catch {
    Write-Error "=== FINAL BUILD FAILED ===: $($_.Exception.Message)"
    Write-Error 'Build failed due to warnings or errors. All issues must be resolved before merge.'
    exit 1
}
