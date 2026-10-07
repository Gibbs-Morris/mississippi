#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Trusted Sonar source identity' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/MergeGroupIssueReference.psm1') -Force
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        $head = 'a' * 40; $merge = 'b' * 40; $target = 'c' * 40
        function Invoke-Source { Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main }
    }
    BeforeEach {
        $script:sourceRun = [pscustomobject]@{
            id=42; workflow_id=141036039; path='.github/workflows/sonar-cloud.yml'; status='completed'
            repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}
            head_repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}
            event='pull_request'; head_sha=$head; head_branch='codex/test'
            pull_requests=@([pscustomobject]@{number=5;head=[pscustomobject]@{sha=$head;ref='codex/test'}})
        }
        $script:sourcePr = [pscustomobject]@{
            state='open'; merge_commit_sha=$merge
            head=[pscustomobject]@{sha=$head;ref='codex/test';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
            base=[pscustomobject]@{sha=$target;ref='codex/parent';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
        }
        $script:sourceRef = [pscustomobject]@{object=[pscustomobject]@{sha=$head}}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if ($Path -like '*/actions/runs/*') { return $script:sourceRun }
            if ($Path -like '*/pulls/*') { return $script:sourcePr }
            if ($Path -like '*/git/ref/heads/*') { return $script:sourceRef }
            throw 'Unexpected metadata path.'
        }
        $script:queuePage = [pscustomobject]@{
            nameWithOwner='Gibbs-Morris/mississippi'; ref=[pscustomobject]@{target=[pscustomobject]@{oid=$target}}
            mergeQueue=[pscustomobject]@{
                id='queue-1'; entries=[pscustomobject]@{totalCount=1;pageInfo=[pscustomobject]@{hasNextPage=$false;endCursor=$null};nodes=@(
                    [pscustomobject]@{id='entry-5';position=1;baseCommit=[pscustomobject]@{oid=$target};headCommit=[pscustomobject]@{oid=$head};pullRequest=[pscustomobject]@{number=5;body='Refs #741';headRefOid=$merge;state='OPEN';repository=[pscustomobject]@{nameWithOwner='Gibbs-Morris/mississippi'}}}
                )}
            }
        }
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference { return $script:queuePage }
    }

    It 'builds the immutable PR merge while reporting its source head and immediate parent' {
        $source = Invoke-Source
        $source.Mode | Should -Be PullRequest
        $source.BuildSha | Should -Be $merge
        $source.HeadSha | Should -Be $head
        $source.TargetRef | Should -Be 'codex/parent'
        $arguments = @(Get-TrustedSonarAnalysisArguments -Source $source)
        $arguments | Should -Contain "/d:sonar.scm.revision=$head"
        $arguments | Should -Contain '/d:sonar.pullrequest.key=5'
        $arguments | Should -Contain '/d:sonar.pullrequest.base=codex/parent'
        ($arguments -join ' ') | Should -Not -Match 'sonar.branch.name'
    }
    It 'explicitly identifies a manually selected non-main branch' {
        $script:sourceRun.event = 'workflow_dispatch'
        $source = Invoke-Source
        $source.BuildSha | Should -Be $head
        $arguments = @(Get-TrustedSonarAnalysisArguments -Source $source)
        $arguments | Should -Contain '/d:sonar.branch.name=codex/test'
        $arguments | Should -Contain '/d:sonar.branch.target=main'
        Should -Invoke Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis -Times 1 -Exactly -ParameterFilter { $Path -like '*/heads/codex%2Ftest' }
    }
    It 'analyzes main without configuring itself as its own reference branch' -TestCases @(@{Event='push'},@{Event='workflow_dispatch'}) {
        param($Event)
        $script:sourceRun.event = $Event; $script:sourceRun.head_branch = 'main'
        $source = Invoke-Source
        $arguments = @(Get-TrustedSonarAnalysisArguments -Source $source)
        $arguments | Should -Contain '/d:sonar.branch.name=main'
        ($arguments -join ' ') | Should -Not -Match 'sonar.branch.target'
    }
    It 'resolves an exact live candidate through the existing complete queue resolver' -TestCases @(@{Event='merge_group'},@{Event='workflow_dispatch'}) {
        param($Event)
        $script:sourceRun.event = $Event; $script:sourceRun.head_branch = 'gh-readonly-queue/main/pr-5'
        $source = Invoke-Source
        $source.Mode | Should -Be Queue
        $source.BuildSha | Should -Be $head
        $source.TargetSha | Should -Be $target
        @($source.Queue.PullRequests.number) | Should -Be @(5)
        $arguments = @(Get-TrustedSonarAnalysisArguments -Source $source)
        $arguments | Should -Contain '/d:sonar.branch.name=gh-readonly-queue/main/pr-5'
        $arguments | Should -Contain '/d:sonar.branch.target=main'
        Should -Invoke Read-MergeQueuePage -ModuleName MergeGroupIssueReference -Times 2 -Exactly
    }
    It 'rejects invalid source <Case>' -TestCases @(
        @{Case='run ID'},@{Case='workflow ID'},@{Case='workflow path'},@{Case='repository'},@{Case='fork'},@{Case='unfinished'},@{Case='SHA'},@{Case='ref'},
        @{Case='event'},@{Case='non-main push'},@{Case='missing PR'},@{Case='ambiguous PR'},@{Case='PR number'},@{Case='closed PR'},@{Case='changed PR head'},
        @{Case='changed PR ref'},@{Case='foreign PR'},@{Case='foreign base'},@{Case='missing merge'},@{Case='missing base'},@{Case='changed branch'},@{Case='foreign queue'},@{Case='missing candidate'},@{Case='ambiguous candidate'}
    ) {
        param($Case)
        switch ($Case) {
            'run ID' {$script:sourceRun.id=43}
            'workflow ID' {$script:sourceRun.workflow_id=99}
            'workflow path' {$script:sourceRun.path='.github/workflows/forged.yml'}
            'repository' {$script:sourceRun.repository.full_name='other/repo'}
            'fork' {$script:sourceRun.head_repository.full_name='fork/repo'}
            'unfinished' {$script:sourceRun.status='in_progress'}
            'SHA' {$script:sourceRun.head_sha='bad'}
            'ref' {$script:sourceRun.head_branch=''}
            'event' {$script:sourceRun.event='issue_comment'}
            'non-main push' {$script:sourceRun.event='push'}
            'missing PR' {$script:sourceRun.pull_requests=@()}
            'ambiguous PR' {$script:sourceRun.pull_requests+= $script:sourceRun.pull_requests[0]}
            'PR number' {$script:sourceRun.pull_requests[0].number=0}
            'closed PR' {$script:sourcePr.state='closed'}
            'changed PR head' {$script:sourcePr.head.sha=$merge}
            'changed PR ref' {$script:sourcePr.head.ref='other'}
            'foreign PR' {$script:sourcePr.head.repo.full_name='other/repo'}
            'foreign base' {$script:sourcePr.base.repo.full_name='other/repo'}
            'missing merge' {$script:sourcePr.merge_commit_sha=$null}
            'missing base' {$script:sourcePr.base.sha=$null}
            'changed branch' {$script:sourceRun.event='workflow_dispatch';$script:sourceRef.object.sha=$merge}
            'foreign queue' {$script:sourceRun.event='merge_group';$script:sourceRun.head_branch='gh-readonly-queue/other/pr-5'}
            'missing candidate' {$script:sourceRun.event='merge_group';$script:sourceRun.head_branch='gh-readonly-queue/main/pr-5';$script:queuePage.mergeQueue.entries.nodes[0].headCommit.oid=$merge}
            'ambiguous candidate' {$script:sourceRun.event='merge_group';$script:sourceRun.head_branch='gh-readonly-queue/main/pr-5';$script:queuePage.mergeQueue.entries.nodes+= $script:queuePage.mergeQueue.entries.nodes[0]}
        }
        { Invoke-Source } | Should -Throw
    }
    It 'requires the immutable default-branch controller definition' {
        Assert-TrustedSonarControllerOrigin -Repository Gibbs-Morris/mississippi -DefaultBranch main -WorkflowRef 'Gibbs-Morris/mississippi/.github/workflows/sonar-trusted-analysis.yml@refs/heads/main' -WorkflowSha $head -CheckoutSha $head
    }
    It 'rejects changed controller <Field>' -TestCases @(@{Field='ref'},@{Field='workflow SHA'},@{Field='checkout'}) {
        param($Field)
        $ref='Gibbs-Morris/mississippi/.github/workflows/sonar-trusted-analysis.yml@refs/heads/main';$workflow=$head;$checkout=$head
        switch ($Field) {'ref' {$ref=$ref.Replace('heads/main','heads/codex/test')};'workflow SHA' {$workflow='bad'};'checkout' {$checkout=$merge}}
        { Assert-TrustedSonarControllerOrigin -Repository Gibbs-Morris/mississippi -DefaultBranch main -WorkflowRef $ref -WorkflowSha $workflow -CheckoutSha $checkout } | Should -Throw '*immutable default-branch*'
    }
    It 'accepts a recheck with identical source identity' {
        $before=Invoke-Source
        Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-Source)
    }
    It 'rejects a changed merge revision or target after analysis' -TestCases @(@{Field='merge'},@{Field='target'}) {
        param($Field)
        $before=Invoke-Source
        if ($Field -eq 'merge') {$script:sourcePr.merge_commit_sha='d'*40} else {$script:sourcePr.base.sha='d'*40}
        { Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-Source) } | Should -Throw '*changed during analysis*'
    }
    It 'delegates candidate metadata rechecks to the same queue membership assertion' {
        $script:sourceRun.event='merge_group';$script:sourceRun.head_branch='gh-readonly-queue/main/pr-5'
        $before=Invoke-Source
        Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-Source)
        $script:queuePage.mergeQueue.entries.nodes[0].pullRequest.body='Changed body'
        { Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-Source) } | Should -Throw '*changed during validation*'
    }
}
