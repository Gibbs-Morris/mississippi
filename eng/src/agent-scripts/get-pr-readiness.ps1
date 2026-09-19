#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [Parameter(Mandatory)][int]$PullRequestNumber,
    [string]$SnapshotJson,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GhJson {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $output = & gh @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "GitHub read failed: $($output.Trim())" }
    return ConvertFrom-Json -InputObject $output
}

function Get-LiveReadinessSnapshot {
    $pull = Get-GhJson -Arguments @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber")
    $head = [string]$pull.head.sha
    $base = [string]$pull.base.sha
    $checksPages = @(Get-GhJson -Arguments @('api', "repos/$RepositoryOwner/$RepositoryName/commits/$head/check-runs", '--paginate', '--slurp'))
    $checks = @($checksPages | ForEach-Object { $_.check_runs } | ForEach-Object {
        [pscustomobject]@{ Name = [string]$_.name; State = if ([string]$_.conclusion -eq 'success') { 'pass' } elseif ([string]$_.status -eq 'completed') { 'fail' } else { 'pending' }; Required = $true }
    })
    $reviewsPages = @(Get-GhJson -Arguments @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber/reviews", '--paginate', '--slurp'))
    $approvals = @($reviewsPages | ForEach-Object { $_ } | Where-Object { $_.state -eq 'APPROVED' }).Count
    [pscustomobject][ordered]@{
        DataComplete = $true
        HeadAtStart = $head
        HeadAtEnd = [string](Get-GhJson -Arguments @('api', "repos/$RepositoryOwner/$RepositoryName/pulls/$PullRequestNumber") | ForEach-Object { $_.head.sha })
        BaseAtStart = $base
        BaseAtEnd = $base
        Checks = @($checks)
        ReviewThreads = @()
        Approvals = $approvals
        IssueReferenceVerified = $false
        DescriptionReviewed = $false
        PullRequestUrl = [string]$pull.html_url
    }
}

function Get-PrReadinessReport {
    param([Parameter(Mandatory)][object]$Snapshot)

    $blockers = [System.Collections.Generic.List[string]]::new()
    if (-not $Snapshot.DataComplete) { $blockers.Add('Required GitHub data is incomplete or inaccessible.') }
    if ($Snapshot.HeadAtStart -ne $Snapshot.HeadAtEnd) { $blockers.Add('PR head changed during collection; snapshot is stale.') }
    if ($Snapshot.BaseAtStart -ne $Snapshot.BaseAtEnd) { $blockers.Add('PR base changed during collection; snapshot is stale.') }
    foreach ($check in @($Snapshot.Checks | Where-Object { $_.Required -and $_.State -ne 'pass' })) { $blockers.Add("Required check '$($check.Name)' is $($check.State).") }
    foreach ($thread in @($Snapshot.ReviewThreads | Where-Object { -not $_.IsResolved -or $_.IsOutdated })) { $blockers.Add('An unresolved or outdated review thread remains.') }
    if ([int]$Snapshot.Approvals -lt 1) { $blockers.Add('Required review approval evidence is missing.') }
    $mechanicalReady = $blockers.Count -eq 0
    $semanticReady = [bool]$Snapshot.IssueReferenceVerified -and [bool]$Snapshot.DescriptionReviewed
    [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Status = if ($mechanicalReady -and $semanticReady) { 'READY' } elseif ($mechanicalReady) { 'MECHANICALLY_READY_SEMANTIC_REVIEW_REQUIRED' } else { 'INCOMPLETE' }
        MechanicalGateReady = $mechanicalReady
        SemanticReviewRequired = -not $semanticReady
        Head = $Snapshot.HeadAtEnd
        Base = $Snapshot.BaseAtEnd
        PullRequestUrl = $Snapshot.PullRequestUrl
        Blockers = @($blockers)
        Checks = @($Snapshot.Checks)
        Approvals = [int]$Snapshot.Approvals
        ReviewThreads = @($Snapshot.ReviewThreads)
    }
}

try {
    $snapshot = if ([string]::IsNullOrWhiteSpace($SnapshotJson)) { Get-LiveReadinessSnapshot } else { ConvertFrom-Json -InputObject $SnapshotJson }
    $result = Get-PrReadinessReport -Snapshot $snapshot
    if ($Json) { $result | ConvertTo-Json -Depth 10 -Compress } else {
        Write-Output "PR_READINESS: $($result.Status)"
        Write-Output "HEAD: $($result.Head)"
        Write-Output "BASE: $($result.Base)"
        foreach ($blocker in @($result.Blockers)) { Write-Output "BLOCKER: $blocker" }
        if ($result.SemanticReviewRequired) { Write-Output 'SEMANTIC_REVIEW: REQUIRED' }
    }
    if ($result.Status -eq 'READY' -or $result.Status -eq 'MECHANICALLY_READY_SEMANTIC_REVIEW_REQUIRED') { exit 0 }
    exit 1
}
catch {
    Write-Error "PR readiness failed: $($_.Exception.Message)"
    exit 1
}
