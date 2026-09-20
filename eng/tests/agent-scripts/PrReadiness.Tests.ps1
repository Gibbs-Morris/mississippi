#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR readiness snapshot' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1') -Force
        function Invoke-Readiness {
            param([Parameter(Mandatory)][object]$Snapshot)
            Get-PrReadinessReport -Snapshot $Snapshot
        }
        $readySnapshot = [pscustomobject]@{
            DataComplete = $true; HeadAtStart = 'head'; HeadAtEnd = 'head'; BaseAtStart = 'base'; BaseAtEnd = 'base';
            Checks = @([pscustomobject]@{Name='build';State='pass';Required=$true}); ReviewThreads=@(); Approvals=1;
            PullRequestState='open'; IsDraft=$false; MergeableState='clean'; ReviewDecision='APPROVED'; PollingCompleted=$true;
            IssueReferenceVerified=$true; DescriptionReviewed=$true; PullRequestUrl='https://github.com/Gibbs-Morris/mississippi/pull/744'
        }
    }

    It 'reports ready only when mechanical and semantic evidence is complete' {
        $outcome = Invoke-Readiness -Snapshot $readySnapshot
        $outcome.Status | Should -Be 'READY'
        $outcome.MechanicalGateReady | Should -BeTrue
    }

    It 'distinguishes mechanical readiness from semantic review' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.IssueReferenceVerified = $false
        $outcome = Invoke-Readiness -Snapshot $snapshot
        $outcome.Status | Should -Be 'MECHANICALLY_READY_SEMANTIC_REVIEW_REQUIRED'
        $outcome.SemanticReviewRequired | Should -BeTrue
    }

    It 'fails closed for pending checks, stale heads and unresolved threads' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.HeadAtEnd = 'new-head'
        $snapshot.Checks = @([pscustomobject]@{Name='build';State='pending';Required=$true})
        $snapshot.ReviewThreads = @([pscustomobject]@{IsResolved=$false;IsOutdated=$false})
        $snapshot.Approvals = 0
        $outcome = Invoke-Readiness -Snapshot $snapshot
        $outcome.Status | Should -Be 'INCOMPLETE'
        @($outcome.Blockers).Count | Should -BeGreaterThan 2
    }

    It 'rejects lifecycle, mergeability, current-review and polling failures' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.IsDraft = $true
        $snapshot.MergeableState = 'dirty'
        $snapshot.ReviewDecision = 'CHANGES_REQUESTED'
        $snapshot.PollingCompleted = $false

        $outcome = Invoke-Readiness -Snapshot $snapshot

        $outcome.Status | Should -Be 'INCOMPLETE'
        ($outcome.Blockers -join "`n") | Should -Match 'draft|mergeability|requests changes|polling'
    }

    It 'blocks aggregate review decisions that are not APPROVED' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.ReviewDecision = 'REVIEW_REQUIRED'

        $outcome = Invoke-Readiness -Snapshot $snapshot

        $outcome.Status | Should -Be 'INCOMPLETE'
        $outcome.Blockers | Should -Contain 'Aggregate review decision is REVIEW_REQUIRED.'
    }

    It 'accepts a resolved outdated thread as having a disposition' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.ReviewThreads = @([pscustomobject]@{ IsResolved = $true; IsOutdated = $true })

        $outcome = Invoke-Readiness -Snapshot $snapshot

        $outcome.Status | Should -Be 'READY'
    }

    It 'keeps matrix identities separate in the expected check catalog' {
        $patterns = @(Get-PrReadinessExpectedCheckPatterns)

        $patterns | Should -Contain '^Build \(ubuntu-latest, mississippi\.slnx\)$'
        $patterns | Should -Contain '^Build \(ubuntu-latest, samples\.slnx\)$'
    }

    It 'adds only path-applicable workflow checks' {
        $docsPatterns = @(Get-PrReadinessExpectedCheckPatterns -ChangedPaths @('docs/Docusaurus/docs/guide.md'))
        $projectPatterns = @(Get-PrReadinessExpectedCheckPatterns -ChangedPaths @('src/Example/Example.csproj'))

        $docsPatterns | Should -Contain '^Build Docusaurus Site$'
        $projectPatterns | Should -Contain '^Validate src csproj descriptions$'
    }

    It 'collects the live paths through an injectable GitHub provider and re-fetches the base and head' {
        $pullStart = [pscustomobject]@{ head = [pscustomobject]@{ sha = 'head-start' }; base = [pscustomobject]@{ sha = 'base-start' }; state = 'open'; draft = $false; mergeable_state = 'clean'; html_url = 'https://github.com/Gibbs-Morris/mississippi/pull/744' }
        $pullEnd = [pscustomobject]@{ head = [pscustomobject]@{ sha = 'head-end' }; base = [pscustomobject]@{ sha = 'base-end' }; state = 'open'; draft = $false; mergeable_state = 'clean'; html_url = $pullStart.html_url }
        $checkPage = [pscustomobject]@{ check_runs = @([pscustomobject]@{ name = 'CodeQL'; status = 'completed'; conclusion = 'success' }) }
        $statusPage = @()
        $reviewPage = @(
            [pscustomobject]@{ id = 1; user = [pscustomobject]@{ login = 'reviewer' }; state = 'APPROVED'; submitted_at = '2026-09-19T00:00:00Z' },
            [pscustomobject]@{ id = 2; user = [pscustomobject]@{ login = 'reviewer' }; state = 'COMMENTED'; submitted_at = '2026-09-19T00:01:00Z' }
        )
        $filesPage = @([pscustomobject]@{ filename = 'README.md' })
        $thread = [pscustomobject]@{ id = 'thread-1'; isResolved = $true; isOutdated = $false; comments = [pscustomobject]@{ nodes = @() } }
        $graphqlPage = [pscustomobject]@{ data = [pscustomobject]@{ repository = [pscustomobject]@{ pullRequest = [pscustomobject]@{ reviewDecision = 'APPROVED'; reviewThreads = [pscustomobject]@{ nodes = @($thread); pageInfo = [pscustomobject]@{ hasNextPage = $false; endCursor = $null } } } } } }
        $pullResponses = [System.Collections.Generic.Queue[object]]::new()
        $pullResponses.Enqueue($pullStart)
        $pullResponses.Enqueue($pullEnd)
        $fakeProvider = {
            param([string[]]$Arguments)
            $joined = $Arguments -join ' '
            if ($joined -match 'pulls/744$') {
                return $pullResponses.Dequeue()
            }
            if ($joined -match 'check-runs') { return $checkPage }
            if ($joined -match 'statuses') { return $statusPage }
            if ($joined -match 'reviews') { return $reviewPage }
            if ($joined -match 'pulls/744/files') { return $filesPage }
            if ($joined -match 'graphql') { return $graphqlPage }
            throw "Unexpected provider query: $joined"
        }.GetNewClosure()

        $snapshot = Get-PrReadinessSnapshot -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -PullRequestNumber 744 -GhJsonProvider $fakeProvider

        $snapshot.DataComplete | Should -BeTrue
        $snapshot.HeadAtStart | Should -Be 'head-start'
        $snapshot.HeadAtEnd | Should -Be 'head-end'
        $snapshot.BaseAtStart | Should -Be 'base-start'
        $snapshot.BaseAtEnd | Should -Be 'base-end'
        $snapshot.ReviewDecision | Should -Be 'APPROVED'
        $snapshot.Approvals | Should -Be 1
        @($snapshot.ReviewThreads).Count | Should -Be 1
    }
}
