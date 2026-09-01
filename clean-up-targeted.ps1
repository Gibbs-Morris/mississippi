#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$BaseRef = 'main',
    [ValidateNotNullOrEmpty()]
    [string]$HeadRef = 'HEAD',
    [switch]$PlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'

try {
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        throw "Repository automation module not found: $modulePath"
    }

    Import-Module -Name $modulePath -Force

    $changedPaths = @(
        Get-CleanupChangedPaths `
            -RepoRoot $repoRoot `
            -BaseRef $BaseRef `
            -HeadRef $HeadRef
    )

    Write-Verbose "Discovered $($changedPaths.Count) changed path(s) for cleanup."

    if ($PlanOnly) {
        Get-CleanupPlan `
            -Paths $changedPaths `
            -RepoRoot $repoRoot |
            ConvertTo-Json -Depth 10 -Compress |
            Write-Output
        exit 0
    }

    $null = Invoke-RepositoryCleanup `
        -Mode Targeted `
        -RepoRoot $repoRoot `
        -Paths $changedPaths
    exit 0
}
catch {
    Write-Error "Targeted cleanup failed: $($_.Exception.Message)"
    exit 1
}
