#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-MergeQueuePage {
    param([string]$Owner, [string]$Name, [string]$Branch, [string]$After)

    $query = 'query($owner:String!,$name:String!,$branch:String!,$ref:String!,$after:String) { repository(owner:$owner,name:$name) { nameWithOwner ref(qualifiedName:$ref) { target { oid } } mergeQueue(branch:$branch) { id entries(first:100,after:$after) { totalCount pageInfo { hasNextPage endCursor } nodes { id position baseCommit { oid } headCommit { oid } pullRequest { number body headRefOid state repository { nameWithOwner } } } } } } }'
    $request = @('api', 'graphql', '-f', "query=$query", '-f', "owner=$Owner", '-f', "name=$Name", '-f', "branch=$($Branch.Substring(11))", '-f', "ref=$Branch")
    if ($After) { $request += @('-f', "after=$After") }
    $output = & gh @request 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw 'GitHub merge-queue query failed.' }
    $page = $output | ConvertFrom-Json
    if ($page.PSObject.Properties['errors'] -and @($page.errors).Count -gt 0) { throw 'GitHub returned merge-queue query errors.' }
    return $page.data.repository
}

function Get-MergeQueueIdentity {
    param([object]$RepositoryData, [string]$Repository)

    if ($null -eq $RepositoryData.mergeQueue -or $null -eq $RepositoryData.ref) { throw 'Merge queue or target ref was not found.' }
    if ($RepositoryData.nameWithOwner -ine $Repository) { throw 'Merge queue repository does not match the event repository.' }
    $current = [ordered]@{ QueueId = [string]$RepositoryData.mergeQueue.id; TargetSha = [string]$RepositoryData.ref.target.oid; TotalCount = $RepositoryData.mergeQueue.entries.totalCount }
    if (-not $current.QueueId -or $current.TargetSha -cnotmatch '^[0-9a-f]{40}$' -or ($current.TotalCount -isnot [long] -and $current.TotalCount -isnot [int]) -or $current.TotalCount -lt 0) { throw 'Invalid merge-queue snapshot identity.' }
    return $current
}

function Add-MergeQueuePageEntries {
    param([Collections.Generic.List[object]]$Entries, [object[]]$PageEntries)

    foreach ($entry in $PageEntries) {
        if ($null -eq $entry) { throw 'Merge queue contained an empty entry.' }
        $Entries.Add($entry)
    }
}

function Read-MergeQueueSnapshotAttempt {
    param([string]$Repository, [string]$Branch)

    $owner, $name = $Repository.Split('/')
    $entries = [Collections.Generic.List[object]]::new()
    $cursors = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $cursor = ''
    $identity = $null
    do {
        $repositoryData = Read-MergeQueuePage -Owner $owner -Name $name -Branch $Branch -After $cursor
        $current = Get-MergeQueueIdentity -RepositoryData $repositoryData -Repository $Repository
        if ($null -eq $identity) { $identity = $current }
        elseif (($identity | ConvertTo-Json -Compress) -cne ($current | ConvertTo-Json -Compress)) { return $null }
        $connection = $repositoryData.mergeQueue.entries
        Add-MergeQueuePageEntries -Entries $entries -PageEntries $connection.nodes
        if ($connection.pageInfo.hasNextPage -isnot [bool]) { throw 'Missing merge-queue pagination state.' }
        $cursor = [string]$connection.pageInfo.endCursor
        if ($connection.pageInfo.hasNextPage -and (-not $cursor -or -not $cursors.Add($cursor))) { throw 'Incomplete or repeating merge-queue pagination.' }
    } while ($connection.pageInfo.hasNextPage)
    if ($entries.Count -ne $identity.TotalCount) { throw 'Merge-queue pagination did not return every entry.' }
    return [pscustomobject]@{ QueueId = $identity.QueueId; TargetSha = $identity.TargetSha; Entries = @($entries.ToArray()) }
}
function Get-MergeQueueSnapshot {
    param([string]$Repository, [string]$Branch)

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $snapshot = Read-MergeQueueSnapshotAttempt -Repository $Repository -Branch $Branch
        if ($null -ne $snapshot) { return $snapshot }
    }
    throw 'Merge queue changed during pagination after three snapshot attempts.'
}
function Assert-MergeQueuePositions {
    param([object[]]$Entries)

    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $positions = [Collections.Generic.HashSet[long]]::new()
    foreach ($entry in $Entries) {
        if (-not $entry.id -or -not $ids.Add([string]$entry.id)) { throw 'Merge queue has missing or duplicate entry IDs.' }
        if (($entry.position -isnot [long] -and $entry.position -isnot [int]) -or $entry.position -lt 0 -or -not $positions.Add([long]$entry.position)) { throw 'Merge queue has invalid or duplicate positions.' }
    }
}

function ConvertTo-MergeQueueIssueMember {
    param([object]$Entry, [string]$ExpectedBase, [string]$Repository, [Collections.Generic.HashSet[long]]$Numbers)

    if ($null -eq $Entry.baseCommit -or $Entry.baseCommit.oid -cne $ExpectedBase -or $null -eq $Entry.headCommit -or $Entry.headCommit.oid -cnotmatch '^[0-9a-f]{40}$') { throw 'Merge-queue predecessor chain does not prove candidate membership.' }
    $pr = $Entry.pullRequest
    if ($null -eq $pr -or $pr.state -cne 'OPEN' -or $pr.repository.nameWithOwner -ine $Repository -or $pr.headRefOid -cnotmatch '^[0-9a-f]{40}$') { throw 'Merge-queue pull request identity is incomplete.' }
    if (($pr.number -isnot [long] -and $pr.number -isnot [int]) -or $pr.number -le 0 -or -not $Numbers.Add([long]$pr.number)) { throw 'Merge queue has invalid or duplicate PR numbers.' }
    return [pscustomobject][ordered]@{
        number = $pr.number
        body = $pr.body
        head_sha = $pr.headRefOid
        entry_id = $Entry.id
        position = $Entry.position
        base_sha = $Entry.baseCommit.oid
        candidate_sha = $Entry.headCommit.oid
    }
}

function Get-MergeQueueIssueMembers {
    param([object]$Snapshot, [object]$MergeGroup, [string]$CandidateSha, [string]$Repository)

    $ordered = @($Snapshot.Entries | Sort-Object position)
    $candidateEntries = @($ordered | Where-Object { $null -ne $_.headCommit -and $_.headCommit.oid -ceq $CandidateSha })
    if ($candidateEntries.Count -ne 1) { throw 'Candidate does not identify exactly one live merge-queue entry.' }
    $members = @($ordered | Where-Object position -LE $candidateEntries[0].position)
    if ($members[0].position -notin @(0, 1)) { throw 'Merge-queue prefix is incomplete.' }
    $previous = $Snapshot.TargetSha
    $nextPosition = $members[0].position
    $numbers = [Collections.Generic.HashSet[long]]::new()
    $result = [Collections.Generic.List[object]]::new()
    foreach ($entry in $members) {
        if ($entry.position -ne $nextPosition) { throw 'Merge-queue prefix has a position gap.' }
        $nextPosition++
        $member = ConvertTo-MergeQueueIssueMember -Entry $entry -ExpectedBase $previous -Repository $Repository -Numbers $numbers
        $result.Add($member)
        $previous = $member.candidate_sha
    }
    if ($candidateEntries[0].baseCommit.oid -cne $MergeGroup.base_sha) { throw 'Candidate entry base does not match the merge-group event.' }
    return $result.ToArray()
}

function Resolve-MergeGroupIssueMembers {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$MergeGroup,
        [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$Repository,
        [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$CandidateSha
    )

    if ($MergeGroup.head_sha -cne $CandidateSha -or $MergeGroup.base_sha -cnotmatch '^[0-9a-f]{40}$') { throw 'Merge-group event and candidate SHA do not agree.' }
    $branch = [string]$MergeGroup.base_ref
    if (-not $branch.StartsWith('refs/heads/', [StringComparison]::Ordinal) -or $branch.Length -le 11) { throw 'Merge-group base must be a branch ref.' }
    $snapshot = Get-MergeQueueSnapshot -Repository $Repository -Branch $branch
    Assert-MergeQueuePositions -Entries $snapshot.Entries
    $pullRequests = @(Get-MergeQueueIssueMembers -Snapshot $snapshot -MergeGroup $MergeGroup -CandidateSha $CandidateSha -Repository $Repository)
    return [pscustomobject][ordered]@{ QueueId = $snapshot.QueueId; TargetSha = $snapshot.TargetSha; CandidateSha = $CandidateSha; PullRequests = $pullRequests }
}
function Assert-MergeGroupIssueMembersUnchanged {
    param([Parameter(Mandatory)][object]$Before, [Parameter(Mandatory)][object]$After)

    $beforeMembers = @($Before.PullRequests)
    $afterMembers = @($After.PullRequests)
    $removed = $beforeMembers.Count - $afterMembers.Count
    $failure = 'Merge-group membership or PR metadata changed during validation.'
    if ($Before.QueueId -cne $After.QueueId -or $Before.CandidateSha -cne $After.CandidateSha -or $afterMembers.Count -eq 0 -or $removed -lt 0) { throw $failure }

    $expectedTarget = $Before.TargetSha
    if ($removed -gt 0) { $expectedTarget = $beforeMembers[$removed - 1].candidate_sha }
    if ($expectedTarget -cne $After.TargetSha) { throw $failure }

    # Only an already-validated contiguous prefix may land while this candidate remains live.
    $fields = @('number', 'body', 'head_sha', 'entry_id', 'base_sha', 'candidate_sha')
    for ($index = 0; $index -lt $afterMembers.Count; $index++) {
        $expected = $beforeMembers[$index + $removed]
        $actual = $afterMembers[$index]
        if ($actual.position -ne ($expected.position - $removed)) { throw $failure }
        $expectedMetadata = $expected | Select-Object -Property $fields | ConvertTo-Json -Compress
        $actualMetadata = $actual | Select-Object -Property $fields | ConvertTo-Json -Compress
        if ($expectedMetadata -cne $actualMetadata) { throw $failure }
    }
}

Export-ModuleMember -Function Resolve-MergeGroupIssueMembers, Assert-MergeGroupIssueMembersUnchanged
