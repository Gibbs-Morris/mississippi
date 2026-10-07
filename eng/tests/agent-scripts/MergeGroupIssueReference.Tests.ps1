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
        $script:responses.Enqueue($page1)
        $script:responses.Enqueue($page2)
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { $script:responses.Dequeue() }
        { Invoke-Resolver } | Should -Throw "*$Message*"
    }

    It 'accepts an identical recheck' {
        $before = Invoke-Resolver
        Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver)
    }
    It 'rejects changed <Field> after body validation' -TestCases @(@{Field='body'}, @{Field='source head'}, @{Field='target'}, @{Field='queue'}) {
        param($Field)
        $before = Invoke-Resolver
        switch ($Field) {
            'body' { $script:repositoryData.mergeQueue.entries.nodes[0].pullRequest.body = 'No reference.' }
            'source head' { $script:repositoryData.mergeQueue.entries.nodes[0].pullRequest.headRefOid = 'e' * 40 }
            'target' { $script:repositoryData.ref.target.oid = $first; $script:repositoryData.mergeQueue.entries.nodes = @(New-Entry 1 102 $first $candidate); $script:repositoryData.mergeQueue.entries.totalCount = 1 }
            'queue' { $script:repositoryData.mergeQueue.id = 'queue-2' }
        }
        { Assert-MergeGroupIssueMembersUnchanged -Before $before -After (Invoke-Resolver) } | Should -Throw '*changed during validation*'
    }
    It 'runs the existing body validator for every resolved member, including an invalid predecessor' {
        $script:repositoryData.mergeQueue.entries.nodes[0].pullRequest.body = 'Missing issue reference.'
        $members = Invoke-Resolver
        $file = Join-Path $TestDrive 'members.json'
        ConvertTo-Json -InputObject @($members.PullRequests) -Depth 10 | Set-Content -LiteralPath $file -Encoding utf8
        $known = '[{"number":741,"title":"Queue support","state":"open"}]'
        $validator = Join-Path $repoRoot 'eng/src/agent-scripts/validate-merge-group-pr-issue-reference.ps1'
        $output = & pwsh -NoProfile -File $validator -PullRequestsPath $file -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $known 2>&1 | Out-String
        $LASTEXITCODE | Should -Not -Be 0
        $output | Should -Match 'failed for #101'
    }
    It 'does not infer PR membership from synthetic commit associations' {
        $workflow = Get-Content (Join-Path $repoRoot '.github/workflows/pr-issue-reference.yml') -Raw
        $workflow | Should -Not -Match 'commits/\$env:GITHUB_SHA/pulls'
        $workflow | Should -Match "Save-TrustedScript -Path 'eng/src/agent-scripts/MergeGroupIssueReference.psm1'"
        ([regex]::Matches($workflow, 'Resolve-MergeGroupIssueMembers -MergeGroup')).Count | Should -Be 2
        $workflow | Should -Match 'Assert-MergeGroupIssueMembersUnchanged -Before \$before -After \$after'
    }
}
