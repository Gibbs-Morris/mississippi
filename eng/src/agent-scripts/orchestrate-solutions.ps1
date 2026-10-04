#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipCleanup,
    [switch]$IncludeMutation,
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

try {
    Invoke-SolutionsPipeline -Configuration $Configuration -RepoRoot $repoRoot -SkipCleanup:$SkipCleanup -IncludeMutation:$IncludeMutation -LeaseDirectory $LeaseDirectory
}
catch {
    Write-Error "=== PIPELINE FAILED ===: $($_.Exception.Message)"
    exit 1
}

exit 0
