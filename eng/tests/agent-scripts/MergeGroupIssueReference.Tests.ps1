#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Exact merge-group issue membership' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/MergeGroupIssueReference.psm1') -Force
        $target = 'a' * 40
        $first = 'b' * 40
        $candidate = 'c' * 40
        $branch = 'refs/heads/codex/merge-queue/pilot-20261007'
        function New-Entry {
            param([int]$Position, [int]$Number, [string]$Base, [string]$Head)
            [pscustomobject]@{
                id = "entry-$Number"; position = $Position
                baseCommit = [pscustomobject]@{ oid = $Base }; headCommit = [pscustomobject]@{ oid = $Head }
                pullRequest = [pscustomobject]@{
                    number = $Number; body = 'Refs #741'; headRefOid = ('d' * 40); state = 'OPEN'
                    repository = [pscustomobject]@{ nameWithOwner = 'Gibbs-Morris/mississippi' }
                }
            }
        }
        function Invoke-Resolver {
            Resolve-MergeGroupIssueMembers -MergeGroup $group -Repository Gibbs-Morris/mississippi -CandidateSha $candidate
        }
    }
    BeforeEach {
        $group = [pscustomobject]@{ base_ref = $branch; base_sha = $first; head_sha = $candidate }
        $script:repositoryData = [pscustomobject]@{
            nameWithOwner = 'Gibbs-Morris/mississippi'
            ref = [pscustomobject]@{ target = [pscustomobject]@{ oid = $target } }
            mergeQueue = [pscustomobject]@{
                id = 'queue-1'
                entries = [pscustomobject]@{
                    totalCount = 2
                    pageInfo = [pscustomobject]@{ hasNextPage = $false; endCursor = $null }
                    nodes = @((New-Entry 1 101 $target $first), (New-Entry 2 102 $first $candidate))
                }
            }
        }
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { return $script:repositoryData }
    }

    It 'includes the code predecessor when the follower itself changes only documentation' {
        $result = Invoke-Resolver
        @($result.PullRequests.number) | Should -Be @(101, 102)
        $result.CandidateSha | Should -Be $candidate
        $result.TargetSha | Should -Be $target
    }
    It 'does not include a later queue entry in an earlier candidate' {
        $group.head_sha = $first
        $group.base_sha = $target
        $result = Resolve-MergeGroupIssueMembers -MergeGroup $group -Repository Gibbs-Morris/mississippi -CandidateSha $first
        @($result.PullRequests.number) | Should -Be @(101)
    }
    It 'excludes a predecessor after it has landed on the target' {
        $script:repositoryData.ref.target.oid = $first
        $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 1 102 $first $candidate)
        $script:repositoryData.mergeQueue.entries.totalCount = 1
        @((Invoke-Resolver).PullRequests.number) | Should -Be @(102)
    }
    It 'accepts a zero-based contiguous queue position' {
        $script:repositoryData.mergeQueue.entries.nodes[0].position = 0
        $script:repositoryData.mergeQueue.entries.nodes[1].position = 1
        @((Invoke-Resolver).PullRequests.number) | Should -Be @(101, 102)
    }
    It 'sorts entries by their explicit queue position' {
        $script:repositoryData.mergeQueue.entries.nodes = @($script:repositoryData.mergeQueue.entries.nodes[1], $script:repositoryData.mergeQueue.entries.nodes[0])
        @((Invoke-Resolver).PullRequests.number) | Should -Be @(101, 102)
    }
    It 'allows an unrelated waiting follower with no candidate yet' {
        $later = New-Entry 3 103 $candidate ('e' * 40)
        $later.headCommit = $null
        $script:repositoryData.mergeQueue.entries.nodes += $later
        $script:repositoryData.mergeQueue.entries.totalCount = 3
        @((Invoke-Resolver).PullRequests.number) | Should -Be @(101, 102)
    }

    It 'rejects <Case>' -TestCases @(
        @{ Case = 'event head mismatch'; Message = 'event and candidate SHA do not agree' }, @{ Case = 'malformed event base'; Message = 'event and candidate SHA do not agree' }, @{ Case = 'non-branch base'; Message = 'base must be a branch ref' },
        @{ Case = 'event base mismatch'; Message = 'entry base does not match' }, @{ Case = 'missing queue'; Message = 'queue or target ref was not found' }, @{ Case = 'missing target'; Message = 'queue or target ref was not found' },
        @{ Case = 'foreign queue repository'; Message = 'repository does not match' }, @{ Case = 'candidate absent'; Message = 'exactly one live' }, @{ Case = 'ambiguous candidate'; Message = 'exactly one live' },
        @{ Case = 'duplicate entry ID'; Message = 'duplicate entry IDs' }, @{ Case = 'duplicate position'; Message = 'duplicate positions' }, @{ Case = 'position gap'; Message = 'position gap' },
        @{ Case = 'missing prefix'; Message = 'prefix is incomplete' }, @{ Case = 'broken predecessor chain'; Message = 'predecessor chain' }, @{ Case = 'missing base commit'; Message = 'predecessor chain' },
        @{ Case = 'missing PR'; Message = 'pull request identity is incomplete' }, @{ Case = 'closed PR'; Message = 'pull request identity is incomplete' }, @{ Case = 'foreign PR repository'; Message = 'pull request identity is incomplete' },
        @{ Case = 'invalid source head'; Message = 'pull request identity is incomplete' }, @{ Case = 'duplicate PR number'; Message = 'duplicate PR numbers' }, @{ Case = 'invalid PR number'; Message = 'duplicate PR numbers' },
        @{ Case = 'truncated pagination'; Message = 'pagination did not return every entry' }, @{ Case = 'missing cursor'; Message = 'Incomplete or repeating' }, @{ Case = 'invalid page state'; Message = 'pagination state' },
        @{ Case = 'null entry'; Message = 'empty entry' }
    ) {
        param($Case, $Message)
        $entries = $script:repositoryData.mergeQueue.entries
        switch ($Case) {
            'event head mismatch' { $group.head_sha = $first }
            'malformed event base' { $group.base_sha = 'invalid' }
            'non-branch base' { $group.base_ref = 'refs/tags/pilot' }
            'event base mismatch' { $group.base_sha = $target }
            'missing queue' { $script:repositoryData.mergeQueue = $null }
            'missing target' { $script:repositoryData.ref = $null }
            'foreign queue repository' { $script:repositoryData.nameWithOwner = 'other/repo' }
            'candidate absent' { $entries.nodes[1].headCommit.oid = 'e' * 40 }
            'ambiguous candidate' { $entries.nodes[0].headCommit.oid = $candidate }
            'duplicate entry ID' { $entries.nodes[1].id = $entries.nodes[0].id }
            'duplicate position' { $entries.nodes[1].position = 1 }
            'position gap' { $entries.nodes[1].position = 3 }
            'missing prefix' { $entries.nodes[0].position = 2; $entries.nodes[1].position = 3 }
            'broken predecessor chain' { $entries.nodes[1].baseCommit.oid = $target }
            'missing base commit' { $entries.nodes[0].baseCommit = $null }
            'missing PR' { $entries.nodes[0].pullRequest = $null }
            'closed PR' { $entries.nodes[0].pullRequest.state = 'CLOSED' }
            'foreign PR repository' { $entries.nodes[0].pullRequest.repository.nameWithOwner = 'other/repo' }
            'invalid source head' { $entries.nodes[0].pullRequest.headRefOid = 'not-a-sha' }
            'duplicate PR number' { $entries.nodes[1].pullRequest.number = 101 }
            'invalid PR number' { $entries.nodes[0].pullRequest.number = 0 }
            'truncated pagination' { $entries.totalCount = 3 }
            'missing cursor' { $entries.pageInfo.hasNextPage = $true }
            'invalid page state' { $entries.pageInfo.hasNextPage = 'false' }
            'null entry' { $entries.nodes[0] = $null }
        }
        { Invoke-Resolver } | Should -Throw "*$Message*"
    }

    It 'reads all pages and passes the cursor and exact target to the API reader' {
        $script:responses = [Collections.Generic.Queue[object]]::new()
        $page1 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes[0])
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'next-page'
        $page2 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes[1])
        $script:responses.Enqueue($page1)
        $script:responses.Enqueue($page2)
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        @((Invoke-Resolver).PullRequests.number) | Should -Be @(101, 102)
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 1 -Exactly -ParameterFilter { $After -eq 'next-page' -and $Branch -eq 'refs/heads/codex/merge-queue/pilot-20261007' -and $Owner -eq 'Gibbs-Morris' -and $Name -eq 'mississippi' }
    }
    It 'restarts a snapshot when a <Change> races with pagination' -TestCases @(@{Change='follower'}, @{Change='predecessor'}) {
        param($Change)
        $script:responses = [Collections.Generic.Queue[object]]::new()
        $page1 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes[0])
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'old-next-page'
        $page2 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes[1])
        $stable = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        if ($Change -eq 'follower') {
            $page2.mergeQueue.entries.totalCount = 3
            $later = New-Entry 3 103 $candidate ('e' * 40)
            $later.headCommit = $null
            $stable.mergeQueue.entries.nodes += $later
            $stable.mergeQueue.entries.totalCount = 3
        }
        else {
            $page2.ref.target.oid = $first
            $stable.ref.target.oid = $first
            $stable.mergeQueue.entries.nodes = @(New-Entry 1 102 $first $candidate)
            $stable.mergeQueue.entries.totalCount = 1
        }
        $script:responses.Enqueue($page1)
        $script:responses.Enqueue($page2)
        $script:responses.Enqueue($stable)
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        $result = Invoke-Resolver
        $expected = if ($Change -eq 'follower') { @(101,102) } else { @(102) }
        @($result.PullRequests.number) | Should -Be $expected
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 2 -Exactly -ParameterFilter { $After -eq '' }
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 1 -Exactly -ParameterFilter { $After -eq 'old-next-page' }
        $script:responses.Count | Should -Be 0
    }
    It 'retries a same-count follower replacement with a cross-page <Race>' -TestCases @(
        @{ Race = 'count mismatch' }, @{ Race = 'duplicate ID' }, @{ Race = 'duplicate position' }, @{ Race = 'position gap' }
    ) {
        param($Race)
        $stable = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        for ($position = 3; $position -le 103; $position++) {
            $entry = New-Entry $position (1000 + $position) $candidate ('e' * 40)
            $entry.headCommit = $null
            $stable.mergeQueue.entries.nodes += $entry
        }
        $stable.mergeQueue.entries.totalCount = 103
        $page1 = $stable | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes | Select-Object -First 100)
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'follower-cursor'
        $page2 = $stable | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes | Select-Object -Last 3)
        switch ($Race) {
            'count mismatch' { $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes | Select-Object -First 2) }
            'duplicate ID' { $page2.mergeQueue.entries.nodes[0].id = $page1.mergeQueue.entries.nodes[-1].id }
            'duplicate position' { $page2.mergeQueue.entries.nodes[0].position = 100 }
            'position gap' { foreach ($entry in $page2.mergeQueue.entries.nodes) { $entry.position++ } }
        }
        $script:responses = [Collections.Generic.Queue[object]]::new()
        $script:responses.Enqueue($page1)
        $script:responses.Enqueue($page2)
        $script:responses.Enqueue($stable)
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        $result = Invoke-Resolver
        @($result.PullRequests.number) | Should -Be @(101, 102)
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 2 -Exactly -ParameterFilter { $After -eq '' }
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 1 -Exactly -ParameterFilter { $After -eq 'follower-cursor' }
        $script:responses.Count | Should -Be 0
    }
    It 'bounds retries for persistent cross-page <Race>' -TestCases @(
        @{ Race = 'count mismatch' }, @{ Race = 'duplicate ID' }, @{ Race = 'duplicate position' }, @{ Race = 'position gap' }
    ) {
        param($Race)
        $page1 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes[0])
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'unstable-cursor'
        $page2 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes[1])
        switch ($Race) {
            'count mismatch' { $page2.mergeQueue.entries.nodes = @() }
            'duplicate ID' { $page2.mergeQueue.entries.nodes[0].id = $page1.mergeQueue.entries.nodes[0].id }
            'duplicate position' { $page2.mergeQueue.entries.nodes[0].position = 1 }
            'position gap' { $page2.mergeQueue.entries.nodes[0].position = 3 }
        }
        $script:responses = [Collections.Generic.Queue[object]]::new()
        for ($attempt = 0; $attempt -lt 3; $attempt++) {
            $script:responses.Enqueue($page1)
            $script:responses.Enqueue($page2)
        }
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        { Invoke-Resolver } | Should -Throw '*changed during pagination after three snapshot attempts*'
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 6 -Exactly
        $script:responses.Count | Should -Be 0
    }

    It 'fails without another retry when a restarted snapshot has <Failure>' -TestCases @(
        @{Failure='an API error'; Message='forced API failure'}, @{Failure='a missing candidate'; Message='exactly one live'}
    ) {
        param($Failure,$Message)
        $script:responses = [Collections.Generic.Queue[object]]::new()
        $page1 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes[0])
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'old-next-page'
        $page2 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.totalCount = 3
        $stable = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $stable.mergeQueue.entries.nodes[1].headCommit.oid = 'e' * 40
        $script:responses.Enqueue($page1)
        $script:responses.Enqueue($page2)
        $script:responses.Enqueue($stable)
        $script:apiFailure = $Failure -eq 'an API error'
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference {
            if ($script:apiFailure -and $script:responses.Count -eq 1) { throw 'forced API failure' }
            $script:responses.Dequeue()
        }
        { Invoke-Resolver } | Should -Throw "*$Message*"
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 3 -Exactly
    }
    It 'rejects <Change> during pagination' -TestCases @(@{Change='target';Message='changed during pagination'}, @{Change='queue';Message='changed during pagination'}, @{Change='count';Message='changed during pagination'}, @{Change='cursor';Message='Incomplete or repeating'}) {
        param($Change, $Message)
        $script:responses = [Collections.Generic.Queue[object]]::new()
        $page1 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page1.mergeQueue.entries.nodes = @($page1.mergeQueue.entries.nodes[0])
        $page1.mergeQueue.entries.pageInfo.hasNextPage = $true
        $page1.mergeQueue.entries.pageInfo.endCursor = 'next-page'
        $page2 = $script:repositoryData | ConvertTo-Json -Depth 15 | ConvertFrom-Json
        $page2.mergeQueue.entries.nodes = @($page2.mergeQueue.entries.nodes[1])
        switch ($Change) {
            'target' { $page2.ref.target.oid = 'f' * 40 }
            'queue' { $page2.mergeQueue.id = 'queue-2' }
            'count' { $page2.mergeQueue.entries.totalCount = 3 }
            'cursor' { $page2.mergeQueue.entries.pageInfo.hasNextPage = $true; $page2.mergeQueue.entries.pageInfo.endCursor = 'next-page' }
        }
        $attempts = if ($Change -eq 'cursor') { 1 } else { 3 }
        for ($attempt = 0; $attempt -lt $attempts; $attempt++) {
            $script:responses.Enqueue($page1)
            $script:responses.Enqueue($page2)
        }
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        { Invoke-Resolver } | Should -Throw "*$Message*"
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times (2 * $attempts) -Exactly
        $script:responses.Count | Should -Be 0
    }

    It 'accepts an identical recheck' {
        $before = Invoke-Resolver
        Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver)
    }
    It 'accepts a landed predecessor without revalidating changed candidate contents' {
        $before = Invoke-Resolver
        $script:repositoryData.ref.target.oid = $first
        $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 1 102 $first $candidate)
        $script:repositoryData.mergeQueue.entries.totalCount = 1
        Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver)
    }
    It 'accepts multiple landed predecessors as one contiguous prefix' {
        $second = 'e' * 40
        $script:repositoryData.mergeQueue.entries.nodes = @((New-Entry 1 101 $target $first), (New-Entry 2 102 $first $second), (New-Entry 3 103 $second $candidate))
        $script:repositoryData.mergeQueue.entries.totalCount = 3
        $group.base_sha = $second
        $before = Invoke-Resolver
        $script:repositoryData.ref.target.oid = $second
        $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 1 103 $second $candidate)
        $script:repositoryData.mergeQueue.entries.totalCount = 1
        Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver)
    }
    It 'accepts landed predecessors with zero-based positions' {
        $script:repositoryData.mergeQueue.entries.nodes[0].position = 0
        $script:repositoryData.mergeQueue.entries.nodes[1].position = 1
        $before = Invoke-Resolver
        $script:repositoryData.ref.target.oid = $first
        $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 0 102 $first $candidate)
        $script:repositoryData.mergeQueue.entries.totalCount = 1
        Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver)
    }
    It 'rejects changed <Field> alongside legitimate target advancement' -TestCases @(
        @{Field='body'}, @{Field='source head'}, @{Field='target'}, @{Field='queue'}, @{Field='candidate'},
        @{Field='entry'}, @{Field='number'}, @{Field='base'}, @{Field='member candidate'}, @{Field='position'}, @{Field='empty'}, @{Field='addition'}
    ) {
        param($Field)
        $before = Invoke-Resolver
        $script:repositoryData.ref.target.oid = $first
        $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 1 102 $first $candidate)
        $script:repositoryData.mergeQueue.entries.totalCount = 1
        $after = Invoke-Resolver
        switch ($Field) {
            'body' { $after.PullRequests[0].body = 'No reference.' }
            'source head' { $after.PullRequests[0].head_sha = 'e' * 40 }
            'target' { $after.TargetSha = 'f' * 40 }
            'queue' { $after.QueueId = 'queue-2' }
            'candidate' { $after.CandidateSha = 'f' * 40 }
            'entry' { $after.PullRequests[0].entry_id = 'replacement-entry' }
            'number' { $after.PullRequests[0].number = 999 }
            'base' { $after.PullRequests[0].base_sha = 'f' * 40 }
            'member candidate' { $after.PullRequests[0].candidate_sha = 'f' * 40 }
            'position' { $after.PullRequests[0].position = 2 }
            'empty' { $after.PullRequests = @() }
            'addition' { $after.PullRequests = @($before.PullRequests) + @($after.PullRequests) }
        }
        { Assert-MergeGroupIssueMembersUnchanged -Before $before -After $after } | Should -Throw '*changed during validation*'
    }
    It 'rejects changed <Field> after body validation' -TestCases @(@{Field='body'}, @{Field='source head'}, @{Field='target'}, @{Field='queue'}) {
        param($Field)
        $before = Invoke-Resolver
        switch ($Field) {
            'body' { $script:repositoryData.mergeQueue.entries.nodes[0].pullRequest.body = 'No reference.' }
            'source head' { $script:repositoryData.mergeQueue.entries.nodes[0].pullRequest.headRefOid = 'e' * 40 }
            'target' { $script:repositoryData.ref.target.oid = 'f' * 40; $script:repositoryData.mergeQueue.entries.nodes[0].baseCommit.oid = 'f' * 40 }
            'queue' { $script:repositoryData.mergeQueue.id = 'queue-2' }
        }
        { Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver) } | Should -Throw '*changed during validation*'
    }


}
