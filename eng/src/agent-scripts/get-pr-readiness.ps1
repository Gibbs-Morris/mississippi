#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [Parameter(Mandatory)][int]$PullRequestNumber,
    [Parameter(Mandatory)][string]$TrustedReviewQueryPath,
    [Parameter(Mandatory)][string]$TrustedAutomationModulePath,
    [Parameter(Mandatory)][ValidatePattern('^SHA256:[0-9a-fA-F]{64}$')][string]$TrustedAutomationModuleSha256,
    [ValidateRange(0, 86400)][int]$PollingSeconds = 0,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $trustedModulePath = (Resolve-Path -LiteralPath $TrustedAutomationModulePath -ErrorAction Stop).Path
    $checkoutRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..') -ErrorAction Stop).Path
    $checkoutPrefix = $checkoutRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($trustedModulePath.StartsWith($checkoutPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'TrustedAutomationModulePath must be outside the reviewed checkout.'
    }
    $actualModuleSha256 = 'SHA256:' + (Get-FileHash -LiteralPath $trustedModulePath -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    if ($actualModuleSha256 -ne $TrustedAutomationModuleSha256.ToUpperInvariant()) {
        throw "Trusted automation module integrity mismatch: expected $TrustedAutomationModuleSha256, got $actualModuleSha256."
    }
    Import-Module $trustedModulePath -Force
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
