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
            state='open'; merge_commit_sha=$merge; mergeable=$true
            head=[pscustomobject]@{sha=$head;ref='codex/test';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
            base=[pscustomobject]@{sha=$target;ref='codex/parent';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
        }
        $script:sourceMerge = [pscustomobject]@{sha=$merge;parents=@([pscustomobject]@{sha=$target},[pscustomobject]@{sha=$head})}
        $script:sourceRef = [pscustomobject]@{object=[pscustomobject]@{sha=$head}}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if ($Path -like '*/actions/runs/*') { return $script:sourceRun }
            if ($Path -like '*/pulls/*') { return $script:sourcePr }
            if ($Path -like '*/git/commits/*') { return $script:sourceMerge }
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
    It 'rejects an unconfirmed or inconsistent PR merge revision <Case>' -TestCases @(
        @{Case='pending mergeability'}, @{Case='conflicted mergeability'}, @{Case='nonboolean mergeability'},
        @{Case='stale base parent'}, @{Case='stale source parent'}, @{Case='single parent'},
        @{Case='extra parent'}, @{Case='different commit'}, @{Case='unavailable commit metadata'}
    ) {
        param($Case)
        switch ($Case) {
            'pending mergeability' {$script:sourcePr.mergeable=$null}
            'conflicted mergeability' {$script:sourcePr.mergeable=$false}
            'nonboolean mergeability' {$script:sourcePr.mergeable='true'}
            'stale base parent' {$script:sourceMerge.parents[0].sha='d'*40}
            'stale source parent' {$script:sourceMerge.parents[1].sha='d'*40}
            'single parent' {$script:sourceMerge.parents=@($script:sourceMerge.parents[0])}
            'extra parent' {$script:sourceMerge.parents+=[pscustomobject]@{sha=('d'*40)}}
            'different commit' {$script:sourceMerge.sha='d'*40}
            'unavailable commit metadata' {
                Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {throw 'Merge commit lookup failed.'} -ParameterFilter {$Path -like '*/git/commits/*'}
            }
        }
        {Invoke-Source} | Should -Throw
    }

    It 'accepts a source-qualified workflow path <Suffix>' -TestCases @(
        @{Suffix='codex/test'},@{Suffix='refs/heads/codex/test'},@{Suffix=('a'*40)},@{Suffix='refs/pull/5/merge'}
    ) {
        param($Suffix)
        $script:sourceRun.path=".github/workflows/sonar-cloud.yml@$Suffix"
        (Invoke-Source).HeadSha | Should -Be $head
    }
    It 'accepts the documented main-qualified workflow path' {
        $script:sourceRun.event='push';$script:sourceRun.head_branch='main';$script:sourceRun.path='.github/workflows/sonar-cloud.yml@main'
        (Invoke-Source).HeadRef | Should -Be main
    }
    It 'rejects an unrelated or malformed workflow qualifier <Suffix>' -TestCases @(
        @{Suffix='other'},@{Suffix='refs/pull/6/merge'},@{Suffix=''},@{Suffix='codex/test@main'}
    ) {
        param($Suffix)
        $script:sourceRun.path=".github/workflows/sonar-cloud.yml@$Suffix"
        { Invoke-Source } | Should -Throw '*approved repository workflow*'
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
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis { [pscustomobject]@{ref='refs/heads/main';object=[pscustomobject]@{type='commit';sha=$head}} }
        Assert-TrustedSonarControllerOrigin -Repository Gibbs-Morris/mississippi -DefaultBranch main -WorkflowRef 'Gibbs-Morris/mississippi/.github/workflows/sonar-trusted-analysis.yml@refs/heads/main' -WorkflowSha $head -CheckoutSha $head
    }
    It 'rejects preserved controller identity when the live default ref has <Change>' -TestCases @(
        @{Change='advanced SHA'}, @{Change='tag ref'}, @{Change='non-commit object'}, @{Change='malformed SHA'}
    ) {
        param($Change)
        $script:controllerRef=[pscustomobject]@{ref='refs/heads/main';object=[pscustomobject]@{type='commit';sha=$head}}
        switch ($Change) {
            'advanced SHA' {$script:controllerRef.object.sha=$target}
            'tag ref' {$script:controllerRef.ref='refs/tags/main'}
            'non-commit object' {$script:controllerRef.object.type='tag'}
            'malformed SHA' {$script:controllerRef.object.sha='invalid'}
        }
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {$script:controllerRef}
        { Assert-TrustedSonarControllerOrigin -Repository Gibbs-Morris/mississippi -DefaultBranch main -WorkflowRef 'Gibbs-Morris/mississippi/.github/workflows/sonar-trusted-analysis.yml@refs/heads/main' -WorkflowSha $head -CheckoutSha $head } | Should -Throw '*current default-branch tip*'
    }
    It 'rejects controller intake when the current default ref cannot be read' {
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {throw 'Current default-ref API unavailable'}
        { Assert-TrustedSonarControllerOrigin -Repository Gibbs-Morris/mississippi -DefaultBranch main -WorkflowRef 'Gibbs-Morris/mississippi/.github/workflows/sonar-trusted-analysis.yml@refs/heads/main' -WorkflowSha $head -CheckoutSha $head } | Should -Throw '*Current default-ref API unavailable*'
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
        if ($Field -eq 'merge') {$script:sourcePr.merge_commit_sha='d'*40}
        else {$script:sourcePr.base.sha='d'*40;$script:sourcePr.merge_commit_sha='e'*40}
        $script:sourceMerge.sha=$script:sourcePr.merge_commit_sha
        $script:sourceMerge.parents=@([pscustomobject]@{sha=$script:sourcePr.base.sha},[pscustomobject]@{sha=$script:sourcePr.head.sha})
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

Describe 'Trusted Sonar post-upload completion' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/MergeGroupIssueReference.psm1') -Force
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        $tokens=$null;$errors=$null
        $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repoRoot 'eng/src/agent-scripts/invoke-trusted-sonar-analysis.ps1'),[ref]$tokens,[ref]$errors)
        if($errors.Count){throw 'Controller syntax failed.'}
        $body=@(($ast.EndBlock.Statements | Where-Object {$_ -is [Management.Automation.Language.TryStatementAst]}).Body.Statements)
        $end=-1;$finish=-1
        for($i=0;$i -lt $body.Count;$i++){
            if($body[$i].Extent.Text -match '^Invoke-SonarContainer .* -Phase End$'){$end=$i}
            if($body[$i].Extent.Text -match '^Write-Host "Sonar analysis completed'){$finish=$i}
        }
        if($end -lt 0 -or $finish -le $end){throw 'Cannot locate the actual post-upload acceptance block.'}
        $script:completionBlock=[scriptblock]::Create(($body[($end+1)..($finish-1)] | ForEach-Object {$_.Extent.Text}) -join [Environment]::NewLine)
        function Invoke-Completion {
            InModuleScope TrustedSonarAnalysis -Parameters @{Completion=$script:completionBlock;Original=$script:originalSource;OriginalPolicy=$script:completionPolicy} {
                param($Completion,$Original,$OriginalPolicy)
                $source=$Original;$latest=$Original;$policy=$OriginalPolicy
                $Repository='Gibbs-Morris/mississippi';$SourceRunId=42;$DefaultBranch='main'
                $startedAt=[datetimeoffset]'2026-10-07T12:00:00Z'
                & $Completion
            }
        }
    }
    BeforeEach {
        $script:completionBranches=[pscustomobject]@{total_count=1;branch_policies=@([pscustomobject]@{id=1;name='main';type='branch'})}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if($Path -like '*/deployment-branch-policies?*'){return $script:completionBranches}
            if($Path -like '*/environments/sonar-analysis'){return [pscustomobject]@{deployment_branch_policy=[pscustomobject]@{protected_branches=$false;custom_branch_policies=$true}}}
            throw 'Unexpected completion metadata path.'
        }
        $script:originalSource=[pscustomobject]@{RunId=42;Mode='Branch';HeadSha=('a'*40);BuildSha=('a'*40);HeadRef='main';TargetRef='main';TargetSha=$null;PullRequest=$null;Queue=$null}
        $script:currentSource=$script:originalSource | ConvertTo-Json | ConvertFrom-Json
        $script:completionPolicy=[pscustomobject]@{GateId=1;Conditions='reviewed';LongLivedPattern='(branch|release)-.*'}
        Mock Get-TrustedSonarSource -ModuleName TrustedSonarAnalysis {return ($script:currentSource | ConvertTo-Json -Depth 8 | ConvertFrom-Json)}
        Mock Assert-SonarPublishedAnalysis -ModuleName TrustedSonarAnalysis {}
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {return $script:completionPolicy}
    }
    It 'rejects a credential policy widened <Stage>' -TestCases @(
        @{Stage='during upload'},@{Stage='during provider wait'},@{Stage='during Sonar policy verification'}
    ) {
        param($Stage)
        $widened=[pscustomobject]@{total_count=2;branch_policies=@(
            [pscustomobject]@{id=1;name='main';type='branch'},
            [pscustomobject]@{id=2;name='codex/*';type='branch'}
        )}
        switch($Stage){
            'during upload' {$script:completionBranches=$widened}
            'during provider wait' {Mock Assert-SonarPublishedAnalysis -ModuleName TrustedSonarAnalysis {$script:completionBranches=$widened}}
            'during Sonar policy verification' {Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {$script:completionBranches=$widened;return $script:completionPolicy}}
        }
        {Invoke-Completion} | Should -Throw '*permit only the exact default branch*'
    }
    It 'accepts an unchanged source after a successful upload' {
        {Invoke-Completion} | Should -Not -Throw
    }
    It 'rejects a source changed during upload: <Mode>' -TestCases @(@{Mode='Branch'},@{Mode='Manual'},@{Mode='PullRequest'},@{Mode='Queue'}) {
        param($Mode)
        $resolvedMode=if($Mode -eq 'Manual'){'Branch'}else{$Mode}
        $script:originalSource.Mode=$resolvedMode;$script:currentSource.Mode=$resolvedMode
        if($Mode -ne 'Branch'){$branch=if($Mode -eq 'Queue'){'gh-readonly-queue/main/pr-5'}else{'codex/test'};$script:originalSource.HeadRef=$branch;$script:currentSource.HeadRef=$branch}
        $script:currentSource.HeadSha='d'*40
        {Invoke-Completion} | Should -Throw '*changed during analysis*'
    }
    It 'rejects a source changed while waiting for its provider check' {
        Mock Assert-SonarPublishedAnalysis -ModuleName TrustedSonarAnalysis {$script:currentSource.HeadSha='d'*40}
        {Invoke-Completion} | Should -Throw '*changed during analysis*'
    }
    It 'rejects a source changed while rechecking Sonar policy' {
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {$script:currentSource.HeadSha='d'*40;return $script:completionPolicy}
        {Invoke-Completion} | Should -Throw '*changed during analysis*'
    }
    It 'propagates a post-upload verification failure: <Stage>' -TestCases @(@{Stage='provider'},@{Stage='policy'}) {
        param($Stage)
        if($Stage -eq 'provider'){
            Mock Assert-SonarPublishedAnalysis -ModuleName TrustedSonarAnalysis {throw 'Provider verification failed.'}
            {Invoke-Completion} | Should -Throw '*Provider verification failed*'
        }else{
            Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {return [pscustomobject]@{GateId=2;Conditions='reviewed';LongLivedPattern='(branch|release)-.*'}}
            {Invoke-Completion} | Should -Throw '*policy changed*'
        }
    }
    It 'rejects unavailable post-upload source metadata' {
        Mock Get-TrustedSonarSource -ModuleName TrustedSonarAnalysis {throw 'Post-upload metadata unavailable.'}
        {Invoke-Completion} | Should -Throw '*metadata unavailable*'
    }
}
