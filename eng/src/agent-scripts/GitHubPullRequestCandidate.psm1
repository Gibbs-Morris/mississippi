#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CandidatePositiveInteger {
    param([AllowNull()][object]$Value)

    if (($Value -isnot [int] -and $Value -isnot [long]) -or $Value -le 0) { throw 'Candidate metadata requires a positive integer.' }
    return [long]$Value
}

function Assert-CandidateSha {
    param([AllowNull()][object]$Value)

    if ([string]$Value -cnotmatch '^[0-9a-f]{40}$') { throw 'Candidate metadata requires an immutable commit SHA.' }
}

function Get-CandidateStackMetadata {
    param([object]$PullRequest)

    $property = $PullRequest.PSObject.Properties['stack']
    if ($null -eq $property -or $null -eq $property.Value) { return $null }
    $stack = $property.Value
    $number = Get-CandidatePositiveInteger $stack.number
    $position = Get-CandidatePositiveInteger $stack.position
    $size = Get-CandidatePositiveInteger $stack.size
    Assert-CandidateSha $stack.base.sha
    if ($position -gt $size -or [string]::IsNullOrWhiteSpace([string]$stack.base.ref)) { throw 'Candidate native stack metadata is invalid.' }
    return [pscustomobject]@{Number=$number;Position=$position;Size=$size;TargetRef=[string]$stack.base.ref;TargetSha=[string]$stack.base.sha}
}

function Assert-CandidatePullRequest {
    param([object]$PullRequest,[long]$Number,[string]$Repository)

    if ((Get-CandidatePositiveInteger $PullRequest.number) -ne $Number -or $PullRequest.base.repo.full_name -ine $Repository) { throw 'Candidate pull request does not identify this repository member.' }
    foreach ($side in @('head','base')) {
        Assert-CandidateSha $PullRequest.$side.sha
        if ([string]::IsNullOrWhiteSpace([string]$PullRequest.$side.ref)) { throw 'Candidate pull request branch identity is incomplete.' }
    }
    Assert-CandidateSha $PullRequest.merge_commit_sha
    if ($PullRequest.state -cnotin @('open','closed')) { throw 'Candidate pull request state is unknown.' }
}

function Get-CandidatePullRequestSnapshotIdentity {
    param([object]$PullRequest)

    $stack = Get-CandidateStackMetadata $PullRequest
    return ConvertTo-Json -InputObject @(
        $PullRequest.number,$PullRequest.state,$PullRequest.head.sha,$PullRequest.head.ref,$PullRequest.head.repo.full_name,
        $PullRequest.base.sha,$PullRequest.base.ref,$PullRequest.base.repo.full_name,$PullRequest.merge_commit_sha,$stack
    ) -Depth 5 -Compress
}

function Assert-CandidateMergeCommit {
    param([object]$PullRequest,[string]$ParentSha,[string]$Repository,[scriptblock]$MetadataReader)

    if ($PullRequest.mergeable -isnot [bool] -or -not $PullRequest.mergeable) { throw 'Candidate merge revision is not confirmed mergeable.' }
    $commit = & $MetadataReader "repos/$Repository/git/commits/$($PullRequest.merge_commit_sha)"
    $parents = @($commit.parents)
    if ($commit.sha -cne $PullRequest.merge_commit_sha -or $parents.Count -ne 2 -or $parents[0].sha -cne $ParentSha -or $parents[1].sha -cne $PullRequest.head.sha) {
        throw 'Candidate merge revision does not have the exact verified base and head parents.'
    }
}

function Get-CandidateNativeStack {
    param([object]$Header,[long]$Number,[string]$Repository,[scriptblock]$MetadataReader)

    $stack = & $MetadataReader "repos/$Repository/stacks/$($Header.Number)"
    if ((Get-CandidatePositiveInteger $stack.number) -ne $Header.Number -or $stack.open -isnot [bool] -or -not $stack.open -or $stack.base.ref -cne $Header.TargetRef) { throw 'Candidate native stack identity is invalid.' }
    $members = @($stack.pull_requests)
    $numbers = @($members | ForEach-Object {Get-CandidatePositiveInteger $_.number})
    if ($members.Count -ne $Header.Size -or @($numbers | Select-Object -Unique).Count -ne $numbers.Count -or $numbers[$Header.Position-1] -ne $Number) { throw 'Candidate native membership is incomplete or inconsistent.' }
    return $stack
}

function Assert-CandidateNativeTrunk {
    param([object]$Header,[string]$Repository,[scriptblock]$MetadataReader)

    $ref = & $MetadataReader "repos/$Repository/git/ref/heads/$([Uri]::EscapeDataString($Header.TargetRef))"
    if ($ref.ref -cne "refs/heads/$($Header.TargetRef)" -or $ref.object.type -cne 'commit' -or $ref.object.sha -cne $Header.TargetSha) { throw 'Candidate native trunk no longer identifies the current commit.' }
}

function Assert-CandidateNativeMemberBinding {
    param([object]$PullRequest,[object]$Member,[long]$RepositoryId,[string]$Repository)

    if ($PullRequest.state -cne $Member.state) { throw 'Candidate native member state changed.' }
    foreach ($side in @('head','base')) {
        $full = $PullRequest.$side
        $listed = $Member.$side
        $fullId = Get-CandidatePositiveInteger $full.repo.id
        $listedId = Get-CandidatePositiveInteger $listed.repo.id
        if ($full.repo.full_name -ine $Repository -or $fullId -ne $RepositoryId -or $listedId -ne $RepositoryId) { throw 'Candidate native member repository is foreign.' }
        if ($full.sha -cne $listed.sha -or $full.ref -cne $listed.ref) { throw 'Candidate native member branch identity changed.' }
    }
}

function Assert-CandidateNativeActiveMember {
    param([object]$PullRequest,[object]$Header,[int]$Position,[AllowNull()][object]$Previous)

    if ($PullRequest.merged -isnot [bool] -or $PullRequest.merged) { throw 'Candidate native active member has inconsistent merge state.' }
    $memberHeader = Get-CandidateStackMetadata $PullRequest
    if ($null -eq $memberHeader) { throw 'Candidate native active member lost stack membership.' }
    $expected = @($Header.Number,$Position,$Header.Size,$Header.TargetRef,$Header.TargetSha) | ConvertTo-Json -Compress
    $actual = @($memberHeader.Number,$memberHeader.Position,$memberHeader.Size,$memberHeader.TargetRef,$memberHeader.TargetSha) | ConvertTo-Json -Compress
    if ($actual -cne $expected) { throw 'Candidate native active member stack identity changed.' }
    $baseRef = $Header.TargetRef
    $baseSha = $Header.TargetSha
    if ($null -ne $Previous) { $baseRef=$Previous.head.ref; $baseSha=$Previous.head.sha }
    if ($PullRequest.base.ref -cne $baseRef -or $PullRequest.base.sha -cne $baseSha) { throw 'Candidate native branch chain does not identify the verified predecessor.' }
}

function Assert-CandidateNativeMergedMember {
    param([object]$PullRequest,[object]$Member,[AllowNull()][object]$Previous,[object]$Header,[string]$Repository,[scriptblock]$MetadataReader)

    if ($null -ne $Previous -or $PullRequest.merged -isnot [bool] -or -not $PullRequest.merged -or $null -eq $Member.merged_at) { throw 'Candidate native closed member is not a leading merged prefix.' }
    $landing = [string]$PullRequest.merge_commit_sha
    $comparison = & $MetadataReader "repos/$Repository/compare/$landing...$($Header.TargetSha)"
    if ($comparison.behind_by -isnot [int] -and $comparison.behind_by -isnot [long]) { throw 'Candidate native containment count is unknown.' }
    if ($comparison.base_commit.sha -cne $landing -or $comparison.merge_base_commit.sha -cne $landing -or $comparison.behind_by -ne 0 -or $comparison.status -cnotin @('ahead','identical')) { throw 'Candidate native merged landing is not proven contained in the trunk.' }
}

function Get-CandidateNativePrefix {
    param([object]$PullRequest,[object]$Header,[string]$Repository,[scriptblock]$MetadataReader)

    $stack = Get-CandidateNativeStack -Header $Header -Number $PullRequest.number -Repository $Repository -MetadataReader $MetadataReader
    Assert-CandidateNativeTrunk -Header $Header -Repository $Repository -MetadataReader $MetadataReader
    $repositoryId = Get-CandidatePositiveInteger $PullRequest.base.repo.id
    $prefix = [Collections.Generic.List[object]]::new()
    $previous = $null
    $parentSha = $Header.TargetSha
    for ($index=0; $index -lt $Header.Position; $index++) {
        $member = $stack.pull_requests[$index]
        $number = Get-CandidatePositiveInteger $member.number
        $current = $PullRequest
        if ($number -ne $PullRequest.number) { $current = & $MetadataReader "repos/$Repository/pulls/$number" }
        Assert-CandidatePullRequest -PullRequest $current -Number $number -Repository $Repository
        Assert-CandidateNativeMemberBinding -PullRequest $current -Member $member -RepositoryId $repositoryId -Repository $Repository
        if ($current.state -ceq 'closed') {
            Assert-CandidateNativeMergedMember -PullRequest $current -Member $member -Previous $previous -Header $Header -Repository $Repository -MetadataReader $MetadataReader
            $prefix.Add($current)
            continue
        }
        Assert-CandidateNativeActiveMember -PullRequest $current -Header $Header -Position ($index+1) -Previous $previous
        Assert-CandidateMergeCommit -PullRequest $current -ParentSha $parentSha -Repository $Repository -MetadataReader $MetadataReader
        $prefix.Add($current)
        $previous = $current
        $parentSha = $current.merge_commit_sha
    }
    return $prefix.ToArray()
}

function Get-CandidateNativeFingerprint {
    param([object]$Header,[object[]]$Prefix)

    $members = @($Prefix | ForEach-Object {
        [ordered]@{Number=$_.number;State=$_.state;HeadSha=$_.head.sha;HeadRef=$_.head.ref;BaseSha=$_.base.sha;BaseRef=$_.base.ref;MergeSha=$_.merge_commit_sha}
    })
    return ConvertTo-Json -InputObject @($Header.Number,$Header.TargetRef,$Header.TargetSha,$members) -Depth 5 -Compress
}

function Get-GitHubPullRequestCandidateIdentity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$Repository,
        [Parameter(Mandatory)][ValidateRange(1,[long]::MaxValue)][long]$Number,
        [Parameter(Mandatory)][object]$PullRequest,
        [Parameter(Mandatory)][scriptblock]$MetadataReader
    )

    Assert-CandidatePullRequest -PullRequest $PullRequest -Number $Number -Repository $Repository
    $fresh = & $MetadataReader "repos/$Repository/pulls/$Number"
    Assert-CandidatePullRequest -PullRequest $fresh -Number $Number -Repository $Repository
    if ($fresh.state -cne 'open') { throw 'Candidate selected pull request is not open.' }
    if ((Get-CandidatePullRequestSnapshotIdentity $fresh) -cne (Get-CandidatePullRequestSnapshotIdentity $PullRequest)) { throw 'Candidate selected pull request changed during intake.' }
    $header = Get-CandidateStackMetadata $fresh
    if ($null -eq $header) {
        Assert-CandidateMergeCommit -PullRequest $fresh -ParentSha $fresh.base.sha -Repository $Repository -MetadataReader $MetadataReader
        return [pscustomobject]@{BuildSha=$fresh.merge_commit_sha;TargetRef=$fresh.base.ref;TargetSha=$fresh.base.sha;NativeIdentity='';PullRequests=@($fresh)}
    }
    $prefix = @(Get-CandidateNativePrefix -PullRequest $fresh -Header $header -Repository $Repository -MetadataReader $MetadataReader)
    $identity = Get-CandidateNativeFingerprint -Header $header -Prefix $prefix
    return [pscustomobject]@{BuildSha=$fresh.merge_commit_sha;TargetRef=$header.TargetRef;TargetSha=$header.TargetSha;NativeIdentity=$identity;PullRequests=$prefix}
}

Export-ModuleMember -Function Get-GitHubPullRequestCandidateIdentity
