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

    It 'includes site and changed-project workflow checks' {
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
            [pscustomobject]@{ id = 1; user = [pscustomobject]@{ login = 'reviewer' }; state = 'APPROVED'; commit_id = 'head-end'; submitted_at = '2026-09-19T00:00:00Z' },
            [pscustomobject]@{ id = 2; user = [pscustomobject]@{ login = 'reviewer' }; state = 'COMMENTED'; submitted_at = '2026-09-19T00:01:00Z' },
            [pscustomobject]@{ id = 3; user = $null; state = 'CHANGES_REQUESTED'; body = 'Superseded deleted-account feedback'; submitted_at = '2026-09-19T00:02:00Z' },
            [pscustomobject]@{ id = 4; user = $null; state = 'APPROVED'; commit_id = 'head-end'; submitted_at = '2026-09-19T00:03:00Z' }
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
        $snapshot.Approvals | Should -Be 2
        $snapshot.ReviewFeedbackCount | Should -Be 0
        @($snapshot.ReviewThreads).Count | Should -Be 1
    }
}

Describe 'Unconditional Docusaurus readiness' -Tag 'DocusaurusStableReporter' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1') -Force

        function New-ReadinessStack {
            param([string]$Trunk)
            if (-not $Trunk) { return $null }
            return [pscustomobject]@{ number = 20; position = 2; size = 2; base = [pscustomobject]@{ ref = $Trunk; sha = ('a' * 40) } }
        }

        function New-SiteCheckRun {
            param([long]$Id, [long]$RunId, [string]$Conclusion = 'success', [string]$Status = 'completed', [long]$AppId = 15368, [string]$StartedAt = '2026-10-08T00:01:00Z', [long]$JobAttempt = 1)
            [pscustomobject]@{
                id = $Id
                name = 'Build Docusaurus Site'
                status = $Status
                conclusion = $Conclusion
                head_sha = 'head'
                started_at = $StartedAt
                FixtureJobAttempt = $JobAttempt
                app = [pscustomobject]@{ id = $AppId }
                details_url="https://github.com/Gibbs-Morris/mississippi/actions/runs/$RunId/job/$Id"
                pull_requests=@([pscustomobject]@{number=744;base=[pscustomobject]@{ref='main'}})
            }
        }

        function New-SiteWorkflowRun {
            param([long]$Id, [string]$CreatedAt, [long]$WorkflowId = 10)
            [pscustomobject]@{
                id = $Id
                workflow_id = $WorkflowId
                path = '.github/workflows/docusaurus.yml@refs/pull/744/merge'
                event = 'pull_request'
                head_sha = 'head'
                created_at = $CreatedAt
                run_attempt = 1
                run_started_at = $CreatedAt
                updated_at = $CreatedAt
                status = 'completed'
                conclusion = 'success'
                repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}
            }
        }

        function Get-SiteReadinessJob {
            param([object]$Fixture, [string]$JobId)
            if ($null -ne $Fixture.JobOverrides -and $Fixture.JobOverrides.ContainsKey($JobId)) { return $Fixture.JobOverrides[$JobId] }
            $page = if ($Fixture.CheckResponses.Count -eq 0) { $Fixture.FinalCheckPage } else { $Fixture.CheckPage }
            $jobCheck = @($page.check_runs | Where-Object { $null -ne $_.PSObject.Properties['id'] -and [long]$_.id -eq [long]$JobId })
            if ($jobCheck.Count -ne 1) { throw 'Expected exact fixture job identity.' }
            $runId = [long]([regex]::Match($jobCheck[0].details_url, '/actions/runs/(\d+)/').Groups[1].Value)
            return [pscustomobject]@{ id=$jobCheck[0].id;run_id=$runId;run_attempt=$jobCheck[0].FixtureJobAttempt;head_sha='head';check_run_url="https://api.github.com/repos/Gibbs-Morris/mississippi/check-runs/$($jobCheck[0].id)" }
        }

        function Get-SiteReadinessResponse {
            param([object]$Fixture, [string[]]$Arguments, [scriptblock]$JobReader)
            $query = $Arguments -join ' '
            $jobMatch = [regex]::Match($query, 'actions/jobs/(\d+)$')
            if ($jobMatch.Success) { return & $JobReader -Fixture $Fixture -JobId $jobMatch.Groups[1].Value }
            $workflowMatch = [regex]::Match($query, 'actions/runs/(\d+)$')
            if ($workflowMatch.Success) {
                if ($null -ne $Fixture.FinalWorkflowRuns -and $Fixture.CheckResponses.Count -eq 0) { return $Fixture.FinalWorkflowRuns[$workflowMatch.Groups[1].Value] }
                return $Fixture.WorkflowRuns[$workflowMatch.Groups[1].Value]
            }
            if ($query -match 'pulls/744$') { return $Fixture.PullResponses.Dequeue() }
            if ($query -match 'check-runs') { return $Fixture.CheckResponses.Dequeue() }
            if ($query -match 'statuses|issues/744/comments') { return @() }
            if ($query -match 'reviews') { return $Fixture.ReviewPage }
            if ($query -match 'pulls/744/files') { return @([pscustomobject]@{ filename = 'README.md' }) }
            if ($query -match 'graphql') { return $Fixture.GraphPage }
            throw "Unexpected readiness request: $query"
        }
        function New-SiteReadinessSnapshot {
            param([string]$BaseRef = 'main', [object]$Stack, [object]$FinalStack, [switch]$IncludeSiteCheck, [object[]]$SiteChecks, [object[]]$FinalSiteChecks, [hashtable]$WorkflowRuns, [hashtable]$FinalWorkflowRuns, [hashtable]$JobOverrides)
            if ($null -eq $WorkflowRuns) { $WorkflowRuns = @{ '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z' } }
            $workflowBase = if ($null -ne $Stack) { [string]$Stack.base.ref } else { $BaseRef }
            $pull = [pscustomobject]@{
                number = 744; head = [pscustomobject]@{ sha = 'head' }; base = [pscustomobject]@{ sha = 'base'; ref = $BaseRef }
                state = 'open'; draft = $false; mergeable_state = 'clean'; html_url = 'https://github.com/Gibbs-Morris/mississippi/pull/744'; stack = $Stack
            }
            $finalPull = $pull.PSObject.Copy()
            if ($PSBoundParameters.ContainsKey('FinalStack')) { $finalPull.stack = $FinalStack }
            $pullResponses = [System.Collections.Generic.Queue[object]]::new()
            $pullResponses.Enqueue($pull)
            $pullResponses.Enqueue($finalPull)
            $checkNames = @(Get-PrReadinessExpectedCheckPatterns -BaseRef $workflowBase -ChangedPaths @('README.md') |
                Where-Object { $_ -ne '^Build Docusaurus Site$' } | ForEach-Object { [regex]::Unescape($_.Trim('^', '$')) })
            if ($IncludeSiteCheck -and -not $PSBoundParameters.ContainsKey('SiteChecks')) {
                $SiteChecks = @((New-SiteCheckRun -Id 1 -RunId 501))
                $SiteChecks[0].pull_requests[0].base.ref = $BaseRef
            }
            $checkPage = [pscustomobject]@{ check_runs = @($checkNames | ForEach-Object {
                [pscustomobject]@{ name = $_; status = 'completed'; conclusion = 'success'; pull_requests = @([pscustomobject]@{ number = 744; base = [pscustomobject]@{ ref = $BaseRef } }) }
            }) }
            if ($IncludeSiteCheck -or $PSBoundParameters.ContainsKey('SiteChecks')) { $checkPage.check_runs += @($SiteChecks) }
            $finalCheckPage = [pscustomobject]@{check_runs=@($checkPage.check_runs)}
            if ($PSBoundParameters.ContainsKey('FinalSiteChecks')) {
                $finalCheckPage.check_runs = @($checkPage.check_runs | Where-Object name -NE 'Build Docusaurus Site') + @($FinalSiteChecks)
            }
            $checkResponses = [System.Collections.Generic.Queue[object]]::new()
            $checkResponses.Enqueue($checkPage)
            $checkResponses.Enqueue($finalCheckPage)
            $reviewPage = @([pscustomobject]@{ id = 1; user = [pscustomobject]@{ login = 'reviewer' }; state = 'APPROVED'; commit_id = 'head'; submitted_at = '2026-10-08T00:00:00Z'; body = '' })
            $graphPage = [pscustomobject]@{ data = [pscustomobject]@{ repository = [pscustomobject]@{ pullRequest = [pscustomobject]@{
                reviewDecision = 'APPROVED'; reviewThreads = [pscustomobject]@{ nodes = @(); pageInfo = [pscustomobject]@{ hasNextPage = $false; endCursor = $null } }
            } } } }
            $fixture = [pscustomobject]@{
                CheckPage=$checkPage;FinalCheckPage=$finalCheckPage;CheckResponses=$checkResponses;PullResponses=$pullResponses
                WorkflowRuns=$WorkflowRuns;FinalWorkflowRuns=$FinalWorkflowRuns;JobOverrides=$JobOverrides;ReviewPage=$reviewPage;GraphPage=$graphPage
            }
            $responseReader = ${function:Get-SiteReadinessResponse}
            $jobReader = ${function:Get-SiteReadinessJob}
            $provider = {
                param([string[]]$Arguments)
                & $responseReader -Fixture $fixture -Arguments $Arguments -JobReader $jobReader
            }.GetNewClosure()
            $snapshot = Get-PrReadinessSnapshot -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -PullRequestNumber 744 -GhJsonProvider $provider -PollingSeconds 300
            $snapshot.IssueReferenceVerified = $true
            $snapshot.DescriptionReviewed = $true
            return $snapshot
        }
    }

    BeforeEach { Mock Start-Sleep {} -ModuleName RepositoryAutomation }

    It 'blocks an otherwise ready unrelated-file PR on <BaseRef> when its site check is missing' -TestCases @(
        @{ BaseRef = 'main'; Trunk = '' }
        @{ BaseRef = 'feature/example'; Trunk = '' }
        @{ BaseRef = 'topic/example'; Trunk = '' }
        @{ BaseRef = 'codex/parent'; Trunk = 'main' }
    ) {
        param($BaseRef, $Trunk)
        $snapshot = New-SiteReadinessSnapshot -BaseRef $BaseRef -Stack (New-ReadinessStack -Trunk $Trunk)
        $result = Get-PrReadinessReport -Snapshot $snapshot
        $result.Status | Should -Be 'INCOMPLETE'
        ($result.Blockers -join ' ') | Should -Match 'Build Docusaurus Site.*missing'
    }

    It 'accepts the current successful site check for <BaseRef>' -TestCases @(
        @{ BaseRef = 'main'; Trunk = '' }
        @{ BaseRef = 'codex/parent'; Trunk = 'main' }
    ) {
        param($BaseRef, $Trunk)
        $snapshot = New-SiteReadinessSnapshot -BaseRef $BaseRef -Stack (New-ReadinessStack -Trunk $Trunk) -IncludeSiteCheck
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and $_.ExpectedIdentity -and $_.Required }).Count | Should -Be 1
    }

    It 'accepts an authenticated site workflow path with suffix <Suffix>' -TestCases @(
        @{ Suffix = 'main' }
        @{ Suffix = 'refs/pull/744/merge' }
        @{ Suffix = 'refs/heads/topic/name@segment' }
    ) {
        param($Suffix)
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $run.path = '.github/workflows/docusaurus.yml@' + $Suffix
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @((New-SiteCheckRun -Id 1 -RunId 501)) -WorkflowRuns @{'501'=$run}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }
    It 'rejects a misleading site workflow path <Path>' -TestCases @(
        @{ Path = '.github/workflows/docusaurus.yml@' }
        @{ Path = '.github/workflows/docusaurus.yml.extra@main' }
        @{ Path = '.github/workflows/another.yml@refs/pull/744/merge' }
    ) {
        param($Path)
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $run.path = $Path
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @((New-SiteCheckRun -Id 1 -RunId 501)) -WorkflowRuns @{'501'=$run}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
    }
    It 'accepts the unsuffixed path returned by existing workflow runs' {
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $run.path = '.github/workflows/docusaurus.yml'
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @((New-SiteCheckRun -Id 1 -RunId 501)) -WorkflowRuns @{'501'=$run}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }
    It 'blocks a same-named site result from <ForeignIdentity> when the required producer is missing' -TestCases @(
        @{ ForeignIdentity = 'another App' }
        @{ ForeignIdentity = 'another workflow' }
        @{ ForeignIdentity = 'a manual event' }
        @{ ForeignIdentity = 'missing workflow path' }
    ) {
        param($ForeignIdentity)
        $site = New-SiteCheckRun -Id 1 -RunId 501
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        switch ($ForeignIdentity) {
            'another App' { $site.app.id = 999 }
            'another workflow' { $run.path = '.github/workflows/another.yml' }
            'a manual event' { $run.event = 'workflow_dispatch' }
            'missing workflow path' { $run.PSObject.Properties.Remove('path') }
        }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($site) -WorkflowRuns @{'501'=$run}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'required:^Build Docusaurus Site$' -and $_.State -eq 'missing' }).Count | Should -Be 1
    }
    It 'preserves an unrelated non-native target without a site workflow' {
        $snapshot = New-SiteReadinessSnapshot -BaseRef 'release/example'
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }

    It 'rejects malformed native trunk metadata for <Case>' -TestCases @(
        @{ Case = 'EmptyRef' }
        @{ Case = 'InvalidSha' }
        @{ Case = 'ZeroNumber' }
        @{ Case = 'MissingNumber' }
    ) {
        param($Case)
        $stack = New-ReadinessStack -Trunk 'main'
        switch ($Case) {
            'EmptyRef' { $stack.base.ref = '' }
            'InvalidSha' { $stack.base.sha = 'invalid' }
            'ZeroNumber' { $stack.number = 0 }
            'MissingNumber' { $stack.PSObject.Properties.Remove('number') }
        }
        { New-SiteReadinessSnapshot -BaseRef 'codex/parent' -Stack $stack } | Should -Throw '*native*'
    }

    It 'blocks a native applicability change during collection' -TestCases @(
        @{ FinalTrunk = 'topic/other' }
        @{ FinalTrunk = '' }
    ) {
        param($FinalTrunk)
        $snapshot = New-SiteReadinessSnapshot -BaseRef 'codex/parent' -Stack (New-ReadinessStack -Trunk 'main') -FinalStack (New-ReadinessStack -Trunk $FinalTrunk) -IncludeSiteCheck
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        $snapshot.EvidenceStable | Should -BeFalse
    }

    It 'blocks a changed native trunk revision even when the branch name is unchanged' {
        $finalStack = New-ReadinessStack -Trunk 'main'
        $finalStack.base.sha = ('b' * 40)
        $snapshot = New-SiteReadinessSnapshot -BaseRef 'codex/parent' -Stack (New-ReadinessStack -Trunk 'main') -FinalStack $finalStack -IncludeSiteCheck
        $snapshot.EvidenceStable | Should -BeFalse
    }

    It 'blocks a changed native stack number during collection' {
        $finalStack = New-ReadinessStack -Trunk 'main'
        $finalStack.number = 21
        $snapshot = New-SiteReadinessSnapshot -BaseRef 'codex/parent' -Stack (New-ReadinessStack -Trunk 'main') -FinalStack $finalStack -IncludeSiteCheck
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        $snapshot.EvidenceStable | Should -BeFalse
    }

    It 'uses the current site run when the earlier run is <OldState> and the replacement is <NewState>' -TestCases @(
        @{OldState='cancelled';NewState='success';Expected='READY'}
        @{OldState='failure';NewState='success';Expected='READY'}
        @{OldState='success';NewState='queued';Expected='INCOMPLETE'}
        @{OldState='success';NewState='failure';Expected='INCOMPLETE'}
        @{OldState='success';NewState='cancelled';Expected='INCOMPLETE'}
    ) {
        param($OldState,$NewState,$Expected)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion $OldState
        $newer = New-SiteCheckRun -Id 2 -RunId 502 -Conclusion $NewState
        if ($NewState -eq 'queued') {
            $newer.status = 'queued'
            $newer.conclusion = $null
            $newer.started_at = $null
        }
        $runs = @{
            '501'=(New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z')
            '502'=(New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z')
        }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($older,$newer) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be $Expected
        @($snapshot.Checks | Where-Object name -EQ 'Build Docusaurus Site').Count | Should -Be 1
    }

    It 'retains a failed same-name check from another <Identity>' -TestCases @(
        @{Identity='workflow'}
        @{Identity='provider'}
        @{Identity='event'}
    ) {
        param($Identity)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502
        $olderRun = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        switch ($Identity) {
            'workflow' { $olderRun.workflow_id=11 }
            'provider' { $older.app.id=999 }
            'event' { $olderRun.event='push' }
        }
        $runs = @{'501'=$olderRun;'502'=(New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z')}
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($older,$newer) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object name -EQ 'Build Docusaurus Site').Count | Should -Be 2
    }

    It 'blocks a replaced site check identity even when both snapshots are successful' {
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $newer = New-SiteCheckRun -Id 2 -RunId 502
        $replacement = New-SiteCheckRun -Id 3 -RunId 503
        $runs = @{
            '501'=(New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z')
            '502'=(New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z')
            '503'=(New-SiteWorkflowRun -Id 503 -CreatedAt '2026-10-08T00:02:00Z')
        }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($older,$newer) -FinalSiteChecks @($older,$replacement) -WorkflowRuns $runs
        $snapshot.EvidenceStable | Should -BeFalse
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
    }

    It 'rejects duplicate Actions evidence with <Fault>' -TestCases @(
        @{ Fault = 'foreign URL' }
        @{ Fault = 'wrong check URL ID' }
        @{ Fault = 'missing provider' }
        @{ Fault = 'invalid check ID' }
        @{ Fault = 'missing workflow metadata' }
        @{ Fault = 'missing workflow identity' }
        @{ Fault = 'missing workflow attempt' }
        @{ Fault = 'wrong workflow run' }
        @{ Fault = 'foreign workflow repository' }
        @{ Fault = 'different workflow head' }
        @{ Fault = 'empty workflow event' }
        @{ Fault = 'missing workflow state' }
        @{ Fault = 'unknown workflow state' }
        @{ Fault = 'missing completed conclusion' }
        @{ Fault = 'invalid workflow timestamp' }
        @{ Fault = 'timestamp without a timezone' }
        @{ Fault = 'indeterminate workflow order' }
    ) {
        param($Fault)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        switch ($Fault) {
            'foreign URL' { $newer.details_url = 'https://github.com/other/repo/actions/runs/502/job/2' }
            'wrong check URL ID' { $newer.details_url = 'https://github.com/Gibbs-Morris/mississippi/actions/runs/502/job/3' }
            'missing provider' { $older.PSObject.Properties.Remove('app'); $newer.PSObject.Properties.Remove('app') }
            'invalid check ID' { $newer.id = 0 }
            'missing workflow metadata' { $runs['502'] = $null }
            'missing workflow identity' { $runs['502'].PSObject.Properties.Remove('workflow_id') }
            'missing workflow attempt' { $runs['502'].PSObject.Properties.Remove('run_attempt') }
            'wrong workflow run' { $runs['502'].id = 503 }
            'foreign workflow repository' { $runs['502'].repository.full_name = 'other/repo' }
            'different workflow head' { $runs['502'].head_sha = 'another-head' }
            'empty workflow event' { $runs['502'].event = '' }
            'missing workflow state' { $runs['502'].PSObject.Properties.Remove('status') }
            'unknown workflow state' { $runs['502'].status = 'unknown' }
            'missing completed conclusion' { $runs['502'].conclusion = $null }
            'invalid workflow timestamp' { $runs['502'].created_at = 'invalid' }
            'timestamp without a timezone' { $runs['502'].created_at = '2026-10-08T00:01:00' }
            'indeterminate workflow order' { $runs['502'].created_at = $runs['501'].created_at; $newer.started_at = $older.started_at }
        }
        { New-SiteReadinessSnapshot -SiteChecks @($older, $newer) -WorkflowRuns $runs } | Should -Throw
    }

    It 'orders timestamps after actual JSON deserialization and for <TimestampType>' -TestCases @(
        @{ TimestampType = 'DateTime' }
        @{ TimestampType = 'DateTimeOffset' }
    ) {
        param($TimestampType)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502
        $olderRun = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z' | ConvertTo-Json | ConvertFrom-Json
        $newerRun = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z' | ConvertTo-Json | ConvertFrom-Json
        if ($TimestampType -eq 'DateTime') {
            $olderRun.created_at = [DateTime]::Parse('2026-10-08T00:00:00Z').ToUniversalTime()
            $newerRun.created_at = [DateTime]::Parse('2026-10-08T00:01:00Z').ToUniversalTime()
        }
        else {
            $olderRun.created_at = [DateTimeOffset]::Parse('2026-10-08T01:00:00+01:00')
            $newerRun.created_at = [DateTimeOffset]::Parse('2026-10-08T00:01:00Z')
        }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($newer, $older) -WorkflowRuns @{'501' = $olderRun; '502' = $newerRun}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }

    It 'selects the <Conclusion> rerun within the same workflow run' -TestCases @(
        @{ Conclusion = 'failure'; Expected = 'INCOMPLETE' }
        @{ Conclusion = 'success'; Expected = 'READY' }
    ) {
        param($Conclusion, $Expected)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $newer = New-SiteCheckRun -Id 2 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion $Conclusion
        $older.started_at = '2026-10-08T00:01:00Z'
        $newer.started_at = '2026-10-08T00:02:00Z'
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $run.run_attempt = 2
        $run.run_started_at = '2026-10-08T00:02:00Z'
        $run.updated_at = '2026-10-08T00:03:00Z'
        $run.conclusion = $Conclusion
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($older, $newer) -WorkflowRuns @{'501'=$run}
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be $Expected
        @($snapshot.Checks | Where-Object name -EQ 'Build Docusaurus Site').Count | Should -Be 1
    }

    It 'rejects indeterminate rerun order' {
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        { New-SiteReadinessSnapshot -SiteChecks @($older, $newer) -WorkflowRuns @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        } } | Should -Throw '*rerun ordering*'
    }

    It 'selects the current external-provider check using its start time' {
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -AppId 999 -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502 -AppId 999
        $older.started_at = '2026-10-08T00:00:00Z'
        $newer.started_at = '2026-10-08T00:01:00Z'
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($older, $newer, (New-SiteCheckRun -Id 3 -RunId 501))
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and -not $_.SiteProducerVerified }).Count | Should -Be 1
    }

    It 'rejects indeterminate external-provider check order' {
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -AppId 999 -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502 -AppId 999
        $newer.started_at = $older.started_at
        { New-SiteReadinessSnapshot -SiteChecks @($older, $newer) } | Should -Throw '*ordering is ambiguous*'
    }

    It 'blocks a replaced check provider even when the check ID and state match' {
        $initial = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $replacement = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -AppId 999
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($initial) -FinalSiteChecks @($replacement)
        $snapshot.EvidenceStable | Should -BeFalse
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
    }

    It 'orders same-second Actions runs by their workflow run number for <LatestState>' -TestCases @(
        @{ LatestState = 'success'; Expected = 'READY' }
        @{ LatestState = 'failure'; Expected = 'INCOMPLETE' }
        @{ LatestState = 'queued'; Expected = 'INCOMPLETE' }
    ) {
        param($LatestState, $Expected)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'cancelled'
        $newer = New-SiteCheckRun -Id 2 -RunId 502 -Conclusion $LatestState
        if ($LatestState -eq 'queued') { $newer.status = 'queued'; $newer.conclusion = $null; $newer.started_at = $null }
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:00:00Z'
        }
        if ($null -ne $newer.started_at) { $newer.started_at = $older.started_at }
        $runs['501'] | Add-Member run_number 10
        $runs['502'] | Add-Member run_number 11
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($newer, $older) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be $Expected
        @($snapshot.Checks | Where-Object name -EQ 'Build Docusaurus Site').Count | Should -Be 1
    }

    It 'keeps equal-time workflow ordering closed for <Fault>' -TestCases @(
        @{ Fault = 'missing from one run' }
        @{ Fault = 'equal across runs' }
        @{ Fault = 'a same-second rerun' }
    ) {
        param($Fault)
        $older = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z' -Conclusion 'failure'
        $newer = New-SiteCheckRun -Id 2 -RunId 502
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:00:00Z'
        }
        $newer.started_at = $older.started_at
        $runs['502'] | Add-Member run_number 11
        if ($Fault -eq 'equal across runs') { $runs['501'] | Add-Member run_number 11 }
        if ($Fault -eq 'a same-second rerun') {
            $runs['501'] | Add-Member run_number 10
            $runs['501'].run_attempt = 2
        }
        { New-SiteReadinessSnapshot -SiteChecks @($older, $newer) -WorkflowRuns $runs } | Should -Throw '*ordering is ambiguous*'
    }

    It 'blocks an unmaterialized <RunState> rerun with prior successful checks (single=<Single>)' -TestCases @(
        @{ RunState = 'queued'; Single = $false }
        @{ RunState = 'requested'; Single = $false }
        @{ RunState = 'waiting'; Single = $false }
        @{ RunState = 'pending'; Single = $false }
        @{ RunState = 'in_progress'; Single = $false }
        @{ RunState = 'queued'; Single = $true }
        @{ RunState = 'requested'; Single = $true }
        @{ RunState = 'waiting'; Single = $true }
        @{ RunState = 'pending'; Single = $true }
        @{ RunState = 'in_progress'; Single = $true }
    ) {
        param($RunState, $Single)
        $prior = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $checks = @($prior)
        if (-not $Single) { $checks += New-SiteCheckRun -Id 2 -RunId 502 }
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].status = $RunState
        $runs['501'].conclusion = $null
        $runs['501'].updated_at = '2026-10-08T00:02:00Z'
        if ($RunState -eq 'in_progress') { $runs['501'].run_started_at = '2026-10-08T00:02:00Z' }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks $checks -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and $_.State -eq 'pending' }).Count | Should -Be 1
        $prior.status | Should -Be 'completed'
        $prior.conclusion | Should -Be 'success'
    }

    It 'keeps a completed <Conclusion> rerun blocking before its new check is visible' -TestCases @(
        @{ Conclusion = 'failure' }
        @{ Conclusion = 'cancelled' }
    ) {
        param($Conclusion)
        $prior = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].run_started_at = '2026-10-08T00:02:00Z'
        $runs['501'].updated_at = '2026-10-08T00:03:00Z'
        $runs['501'].conclusion = $Conclusion
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($prior, (New-SiteCheckRun -Id 2 -RunId 502)) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and $_.State -eq 'fail' }).Count | Should -Be 1
    }

    It 'blocks a completed <Conclusion> rerun canceled before starting while only its prior started check remains' -TestCases @(
        @{ Conclusion = 'cancelled' }
        @{ Conclusion = 'failure' }
    ) {
        param($Conclusion)
        $prior = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].status = 'completed'
        $runs['501'].conclusion = $Conclusion
        $runs['501'].updated_at = '2026-10-08T00:03:00Z'
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($prior, (New-SiteCheckRun -Id 2 -RunId 502)) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and $_.State -eq 'fail' }).Count | Should -Be 1
        $prior.conclusion | Should -Be 'success'
        $prior.started_at | Should -Be '2026-10-08T00:00:00Z'
    }
    It 'rejects untrusted rerun job identity with <Field>' -TestCases @(
        @{ Field = 'id'; Value = 9 }
        @{ Field = 'run_id'; Value = 999 }
        @{ Field = 'head_sha'; Value = 'different-head' }
        @{ Field = 'check_run_url'; Value = 'https://api.github.com/repos/other/repository/check-runs/1' }
        @{ Field = 'run_attempt'; Value = 0 }
        @{ Field = 'run_attempt'; Value = 3 }
        @{ Field = 'run_attempt'; Value = $null }
    ) {
        param($Field, $Value)
        $run = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $run.run_attempt = 2
        $run.run_started_at = '2026-10-08T00:02:00Z'
        $job = [pscustomobject]@{ id=1;run_id=501;run_attempt=1;head_sha='head';check_run_url='https://api.github.com/repos/Gibbs-Morris/mississippi/check-runs/1' }
        $job.$Field = $Value
        { New-SiteReadinessSnapshot -SiteChecks @((New-SiteCheckRun -Id 1 -RunId 501)) -WorkflowRuns @{'501'=$run} -JobOverrides @{'1'=$job} } | Should -Throw '*job attempt*'
    }
    It 'does not let a later completion reorder an earlier-started rerun' {
        $rerun = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:02:00Z' -Conclusion 'failure' -JobAttempt 2
        $success = New-SiteCheckRun -Id 2 -RunId 502 -StartedAt '2026-10-08T00:03:00Z'
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:03:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].run_started_at = '2026-10-08T00:02:00Z'
        $runs['501'].updated_at = '2026-10-08T00:04:00Z'
        $runs['501'].conclusion = 'failure'
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($rerun, $success) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }

    It 'accepts a preserved success after the current workflow attempt succeeds (single=<Single>)' -TestCases @(
        @{ Single = $false }
        @{ Single = $true }
    ) {
        param($Single)
        $prior = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:00:00Z'
        $checks = @($prior)
        if (-not $Single) { $checks += New-SiteCheckRun -Id 2 -RunId 502 }
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].run_started_at = '2026-10-08T00:02:00Z'
        $runs['501'].updated_at = '2026-10-08T00:03:00Z'
        $snapshot = New-SiteReadinessSnapshot -SiteChecks $checks -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'READY'
    }

    It 'blocks workflow attempt movement with an unchanged successful check (<FinalState>)' -TestCases @(
        @{ FinalState = 'queued' }
        @{ FinalState = 'completed' }
    ) {
        param($FinalState)
        $initial = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
        $final = $initial.PSObject.Copy()
        $final.run_attempt = 2
        $final.status = $FinalState
        $final.run_started_at = '2026-10-08T00:02:00Z'
        $final.updated_at = '2026-10-08T00:03:00Z'
        if ($FinalState -eq 'queued') { $final.conclusion = $null }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @((New-SiteCheckRun -Id 1 -RunId 501)) -WorkflowRuns @{'501'=$initial} -FinalWorkflowRuns @{'501'=$final}
        $snapshot.EvidenceStable | Should -BeFalse
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
    }

    It 'keeps a later attempt of an older workflow run current when it is <AttemptState>' -TestCases @(
        @{ AttemptState = 'failure' }
        @{ AttemptState = 'cancelled' }
        @{ AttemptState = 'queued' }
        @{ AttemptState = 'failure'; Unstarted = $true }
        @{ AttemptState = 'cancelled'; Unstarted = $true }
    ) {
        param($AttemptState, $Unstarted)
        $rerun = New-SiteCheckRun -Id 1 -RunId 501 -StartedAt '2026-10-08T00:02:00Z' -Conclusion $AttemptState -JobAttempt 2
        $success = New-SiteCheckRun -Id 2 -RunId 502
        $runs = @{
            '501' = New-SiteWorkflowRun -Id 501 -CreatedAt '2026-10-08T00:00:00Z'
            '502' = New-SiteWorkflowRun -Id 502 -CreatedAt '2026-10-08T00:01:00Z'
        }
        $runs['501'].run_attempt = 2
        $runs['501'].run_started_at = '2026-10-08T00:02:00Z'
        $runs['501'].updated_at = '2026-10-08T00:02:00Z'
        $runs['501'].status = 'completed'
        if ($Unstarted) {
            $rerun.started_at = $null
            $runs['501'].run_started_at = '2026-10-08T00:00:00Z'
        }
        if ($AttemptState -eq 'queued') {
            $rerun.status = 'queued'
            $rerun.conclusion = $null
            $rerun.started_at = $null
            $runs['501'].status = 'queued'
            $runs['501'].run_started_at = '2026-10-08T00:00:00Z'
        }
        $snapshot = New-SiteReadinessSnapshot -SiteChecks @($rerun, $success) -WorkflowRuns $runs
        (Get-PrReadinessReport -Snapshot $snapshot).Status | Should -Be 'INCOMPLETE'
        @($snapshot.Checks | Where-Object name -EQ 'Build Docusaurus Site').Count | Should -Be 1
        @($snapshot.Checks | Where-Object { $_.Name -eq 'Build Docusaurus Site' -and $_.State -ne 'pass' }).Count | Should -Be 1
    }
}

Describe 'Docusaurus workflow reporting contract' {
    BeforeAll {
        $workflowRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $workflow = Get-Content -LiteralPath (Join-Path $workflowRoot '.github/workflows/docusaurus.yml') -Raw
        $pullRequestTrigger = [regex]::Match(
            $workflow,
            '(?ms)^  pull_request:\r?\n(?<trigger>.*?)(?=^\S|\z)'
        ).Groups['trigger'].Value
    }

    It 'reports for opened, synchronized, reopened, edited and native stacked PR activity' {
        ([regex]::Matches($workflow, '(?m)^  pull_request:\r?$')).Count | Should -Be 1
        $pullRequestTrigger | Should -Not -BeNullOrEmpty
        $activityList = [regex]::Match(
            $pullRequestTrigger,
            '(?m)^    types: \[(?<types>[^\]]+)\]\r?$'
        ).Groups['types'].Value
        $activities = @($activityList.Split(',') | ForEach-Object { $_.Trim() })
        ($activities | Sort-Object) -join ',' | Should -Be 'edited,opened,reopened,stacked,synchronize'
    }

    It 'preserves the applicable main, feature and topic targets' {
        $branchList = [regex]::Match(
            $pullRequestTrigger,
            '(?m)^    branches:\r?\n(?<branches>(?:      - [^\r\n]+\r?\n)+)'
        ).Groups['branches'].Value
        $branches = @([regex]::Matches($branchList, '(?m)^      - (?<branch>.+?)\r?$') |
            ForEach-Object { $_.Groups['branch'].Value })
        ($branches | Sort-Object) -join ',' | Should -Be 'feature/**,main,topic/**'
    }

    It 'cannot omit the required PR check through paths or paths-ignore filtering' {
        $pullRequestTrigger | Should -Not -Match '(?m)^    paths(?:-ignore)?:'
    }
}
