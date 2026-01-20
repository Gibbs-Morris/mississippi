#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipCleanup,
    [switch]$IncludeMutation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-SolutionsPipeline -Configuration $Configuration -RepoRoot $repoRoot -SkipCleanup:$SkipCleanup -IncludeMutation:$IncludeMutation
}
catch {
    Write-Error "=== PIPELINE FAILED ===: $($_.Exception.Message)"
    exit 1
}
