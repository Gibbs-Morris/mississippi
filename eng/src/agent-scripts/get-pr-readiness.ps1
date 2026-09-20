#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [Parameter(Mandatory)][int]$PullRequestNumber,
    [Parameter(Mandatory)][string]$TrustedReviewQueryPath,
    [ValidateRange(0, 86400)][int]$PollingSeconds = 0,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'RepositoryAutomation.psm1') -Force

try {
    $snapshot = Get-PrReadinessSnapshot -RepositoryOwner $RepositoryOwner -RepositoryName $RepositoryName -PullRequestNumber $PullRequestNumber -TrustedReviewQueryPath $TrustedReviewQueryPath -PollingSeconds $PollingSeconds
    $result = Get-PrReadinessReport -Snapshot $snapshot
    if ($Json) {
        $result | ConvertTo-Json -Depth 10 -Compress
    }
    else {
        Write-Output "PR_READINESS: $($result.Status)"
        Write-Output "HEAD: $($result.Head)"
        Write-Output "BASE: $($result.Base)"
        foreach ($blocker in @($result.Blockers)) { Write-Output "BLOCKER: $blocker" }
        if ($result.SemanticReviewRequired) { Write-Output 'SEMANTIC_REVIEW: REQUIRED' }
    }
    if ($result.Status -eq 'READY') { exit 0 }
    exit 1
}
catch {
    Write-Error "PR readiness failed: $($_.Exception.Message)"
    exit 1
}
