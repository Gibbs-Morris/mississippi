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
            number=5; state='open'; merge_commit_sha=$merge; mergeable=$true
            head=[pscustomobject]@{sha=$head;ref='codex/test';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
            base=[pscustomobject]@{sha=$target;ref='codex/parent';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
        }
        $script:sourceMerge = [pscustomobject]@{sha=$merge;parents=@([pscustomobject]@{sha=$target},[pscustomobject]@{sha=$head})}
        $script:sourceRef = [pscustomobject]@{object=[pscustomobject]@{sha=$head}}
        $script:defaultRef = [pscustomobject]@{ref='refs/heads/main';object=[pscustomobject]@{type='commit';sha=$target}}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if ($Path -like '*/actions/runs/*') { return $script:sourceRun }
            if ($Path -like '*/pulls/*') { return $script:sourcePr }
            if ($Path -like '*/git/commits/*') { return $script:sourceMerge }
            if ($Path -like '*/git/ref/heads/main' -and $script:sourceRun.head_branch -cne 'main') { return $script:defaultRef }
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
    It 'retains the current default-branch target for manual branch analysis' {
        $script:sourceRun.event='workflow_dispatch'
        (Invoke-Source).TargetSha | Should -Be $target
    }
    It 'rejects default-branch movement during manual branch analysis' {
        $script:sourceRun.event='workflow_dispatch'
        $before=Invoke-Source
        $script:defaultRef.object.sha='d'*40
        {Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-Source)} | Should -Throw '*target changed during analysis*'
    }
    It 'rejects invalid manual target metadata <Change>' -TestCases @(
        @{Change='tag ref'}, @{Change='non-commit object'}, @{Change='malformed SHA'}
    ) {
        param($Change)
        $script:sourceRun.event='workflow_dispatch'
        switch($Change){
            'tag ref' {$script:defaultRef.ref='refs/tags/main'}
            'non-commit object' {$script:defaultRef.object.type='tag'}
            'malformed SHA' {$script:defaultRef.object.sha='bad'}
        }
        {Invoke-Source} | Should -Throw
    }
    It 'rejects manual analysis when default-branch metadata is unavailable' {
        $script:sourceRun.event='workflow_dispatch'
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {throw 'Default target lookup failed.'} -ParameterFilter {$Path -like '*/git/ref/heads/main'}
        {Invoke-Source} | Should -Throw '*Default target lookup failed*'
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

Describe 'Trusted Sonar native candidate identity' -Tag NativeCandidate {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        $nativeHead = 'a' * 40; $nativeMerge = 'b' * 40; $nativeRoot = 'c' * 40
        $parentMerge = 'd' * 40; $parentHead = 'e' * 40; $landing = 'f' * 40
        function New-NativeLightMember {
            param($PullRequest)
            $row = [ordered]@{number=$PullRequest.number;state=$PullRequest.state;merged_at=$null}
            foreach ($kind in @('head','base')) {
                $side = $PullRequest.$kind
                $row[$kind] = [pscustomobject]@{sha=$side.sha;ref=$side.ref;repo=[pscustomobject]@{id=$side.repo.id;name='mississippi';url='https://api.github.com/repos/Gibbs-Morris/mississippi'}}
            }
            return [pscustomobject]$row
        }
        function Invoke-NativeSource { Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main }
        function Set-NativeMergedPrefix {
            $script:nativePulls['4'].state = 'closed'
            $script:nativePulls['4'].merged = $true
            $script:nativePulls['4'].merge_commit_sha = $landing
            $script:nativePulls['4'].stack = $null
            $script:nativePulls['5'].base.ref = 'main'
            $script:nativePulls['5'].base.sha = $nativeRoot
            $script:nativeStack.pull_requests = @((New-NativeLightMember $script:nativePulls['4']), (New-NativeLightMember $script:nativePulls['5']))
            $script:nativeStack.pull_requests[0].merged_at = '2026-10-08T00:00:00Z'
            $script:nativeCommits[$nativeMerge].parents[0].sha = $nativeRoot
        }
    }
    BeforeEach {
        $script:nativeRun = [pscustomobject]@{
            id=42;workflow_id=141036039;path='.github/workflows/sonar-cloud.yml';status='completed'
            repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}
            head_repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}
            event='pull_request';head_sha=$nativeHead;head_branch='codex/test'
            pull_requests=@([pscustomobject]@{number=5;head=[pscustomobject]@{sha=$nativeHead;ref='codex/test'}})
        }
        $script:nativePulls = @{}
        foreach ($number in @(4,5)) {
            $isParent = $number -eq 4
            $script:nativePulls[[string]$number] = [pscustomobject]@{
                number=$number;state='open';merged=$false;mergeable=$true
                merge_commit_sha=$(if ($isParent) {$parentMerge} else {$nativeMerge})
                head=[pscustomobject]@{sha=$(if ($isParent) {$parentHead} else {$nativeHead});ref=$(if ($isParent) {'codex/parent'} else {'codex/test'});repo=[pscustomobject]@{id=924331982;full_name='Gibbs-Morris/mississippi'}}
                base=[pscustomobject]@{sha=$(if ($isParent) {$nativeRoot} else {$parentHead});ref=$(if ($isParent) {'main'} else {'codex/parent'});repo=[pscustomobject]@{id=924331982;full_name='Gibbs-Morris/mississippi'}}
                stack=[pscustomobject]@{number=1052;position=($number-3);size=2;base=[pscustomobject]@{ref='main';sha=$nativeRoot}}
            }
        }
        $script:nativeStack = [pscustomobject]@{number=1052;open=$true;base=[pscustomobject]@{ref='main'};pull_requests=@((New-NativeLightMember $script:nativePulls['4']), (New-NativeLightMember $script:nativePulls['5']))}
        $script:nativeCommits = @{}
        $script:nativeCommits[$parentMerge] = [pscustomobject]@{sha=$parentMerge;parents=@([pscustomobject]@{sha=$nativeRoot},[pscustomobject]@{sha=$parentHead})}
        $script:nativeCommits[$nativeMerge] = [pscustomobject]@{sha=$nativeMerge;parents=@([pscustomobject]@{sha=$parentMerge},[pscustomobject]@{sha=$nativeHead})}
        $script:nativeRef = [pscustomobject]@{ref='refs/heads/main';object=[pscustomobject]@{type='commit';sha=$nativeRoot}}
        $script:nativeComparison = [pscustomobject]@{base_commit=[pscustomobject]@{sha=$landing};merge_base_commit=[pscustomobject]@{sha=$landing};behind_by=0;status='ahead'}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if ($Path -match '/actions/runs/42$') { return $script:nativeRun }
            if ($Path -match '/pulls/([0-9]+)$') { return $script:nativePulls[$Matches[1]] }
            if ($Path -match '/stacks/105[23]$') { return $script:nativeStack }
            if ($Path -match '/git/ref/heads/main$') { return $script:nativeRef }
            if ($Path -match '/git/commits/([a-f0-9]{40})$') { return $script:nativeCommits[$Matches[1]] }
            if ($Path -match '/compare/[a-f0-9]{40}\.\.\.[a-f0-9]{40}$') { return $script:nativeComparison }
            throw "Unexpected native metadata path: $Path"
        }
    }

    It 'accepts the verified synthetic prefix and uses its actual trunk baseline without an opaque stack ID' {
        $source = Invoke-NativeSource
        $source.BuildSha | Should -Be $nativeMerge
        $source.HeadSha | Should -Be $nativeHead
        $source.TargetRef | Should -Be 'main'
        $source.TargetSha | Should -Be $nativeRoot
        $source.NativeIdentity | Should -Not -BeNullOrEmpty
        @(Get-TrustedSonarAnalysisArguments -Source $source) | Should -Contain '/d:sonar.pullrequest.base=main'
        @(Get-TrustedSonarAnalysisArguments -Source $source) | Should -Contain "/d:sonar.scm.revision=$nativeHead"
    }
    It 'accepts a contained merged prefix only after the remaining PR targets the fresh trunk' {
        Set-NativeMergedPrefix
        $source = Invoke-NativeSource
        $source.BuildSha | Should -Be $nativeMerge
        $source.TargetSha | Should -Be $nativeRoot
        $source.NativeIdentity | Should -Not -BeNullOrEmpty
        Should -Invoke Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis -Times 1 -Exactly -ParameterFilter {$Path -like '*/compare/*'}
    }
    It 'keeps unrelated upper-layer growth out of completion identity' {
        $before = Invoke-NativeSource
        $script:nativeStack.pull_requests += [pscustomobject]@{number=6;state='open';head=[pscustomobject]@{sha=('1'*40);ref='codex/above'}}
        foreach ($pr in $script:nativePulls.Values) {$pr.stack.size=3}
        Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-NativeSource)
        Should -Invoke Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis -Times 0 -Exactly -ParameterFilter {$Path -match '/pulls/6$'}
    }
    It 'rejects a changed prefix membership even when all selected source and commit SHAs match' {
        $before = Invoke-NativeSource
        $script:nativePulls['4'].number = 6
        $script:nativePulls['6'] = $script:nativePulls['4']
        $script:nativePulls.Remove('4')
        $script:nativeStack.pull_requests[0] = New-NativeLightMember $script:nativePulls['6']
        $after = Invoke-NativeSource
        $after.BuildSha | Should -Be $before.BuildSha
        {Assert-TrustedSonarSourceUnchanged -Before $before -After $after} | Should -Throw '*identity changed*'
    }
    It 'rejects a changed stack association with unchanged source code' {
        $before = Invoke-NativeSource
        $script:nativeStack.number = 1053
        foreach ($pr in $script:nativePulls.Values) {$pr.stack.number=1053}
        {Assert-TrustedSonarSourceUnchanged -Before $before -After (Invoke-NativeSource)} | Should -Throw '*identity changed*'
    }
    It 'rejects selected metadata movement during intake: <Change>' -TestCases @(
        @{Change='head'},@{Change='base'},@{Change='merge'},@{Change='membership'},@{Change='closed'}
    ) {
        param($Change)
        $script:initialNativePull = $script:nativePulls['5'] | ConvertTo-Json -Depth 8 | ConvertFrom-Json
        $script:selectedNativeReads = 0
        switch ($Change) {
            'head' {$script:nativePulls['5'].head.sha='2'*40}
            'base' {$script:nativePulls['5'].base.sha='2'*40}
            'merge' {$script:nativePulls['5'].merge_commit_sha='2'*40}
            'membership' {$script:nativePulls['5'].stack=$null}
            'closed' {$script:nativePulls['5'].state='closed'}
        }
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            $script:selectedNativeReads++
            if ($script:selectedNativeReads -eq 1) { return $script:initialNativePull }
            return $script:nativePulls['5']
        } -ParameterFilter {$Path -match '/pulls/5$'}
        {Invoke-NativeSource} | Should -Throw
    }
    It 'rejects invalid native evidence: <Fault>' -TestCases @(
        @{Fault='invalid number'},@{Fault='invalid position'},@{Fault='invalid size'},@{Fault='missing trunk'},@{Fault='stale trunk'},@{Fault='wrong stack'},@{Fault='closed stack'},
        @{Fault='incomplete membership'},@{Fault='duplicate member'},@{Fault='reordered membership'},@{Fault='wrong selected position'},
        @{Fault='foreign prefix repository'},@{Fault='foreign lightweight repository'},@{Fault='changed prefix head'},@{Fault='changed prefix base'},@{Fault='changed prefix stack'},
        @{Fault='changed prefix state'},@{Fault='missing prefix stack'},@{Fault='unknown prefix state'},@{Fault='empty prefix branch'},@{Fault='wrong prefix number'},@{Fault='foreign prefix base'},
        @{Fault='wrong prefix branch chain'},@{Fault='unconfirmed prefix merge'},@{Fault='inconsistent active merge'},@{Fault='nonboolean active merge'},@{Fault='wrong prefix merge commit'},@{Fault='wrong prefix merge parent'},@{Fault='extra prefix merge parent'},
        @{Fault='raw branch parent'},@{Fault='wrong selected source parent'},@{Fault='wrong root ref'},@{Fault='noncommit root'},@{Fault='closed unmerged prefix'},@{Fault='merged after active'}
    ) {
        param($Fault)
        switch ($Fault) {
            'invalid number' {$script:nativePulls['5'].stack.number=0}
            'invalid position' {$script:nativePulls['5'].stack.position=0}
            'invalid size' {$script:nativePulls['5'].stack.size=0}
            'missing trunk' {$script:nativePulls['5'].stack.base.ref=''}
            'stale trunk' {$script:nativeRef.object.sha='2'*40}
            'wrong stack' {$script:nativeStack.number=1053}
            'closed stack' {$script:nativeStack.open=$false}
            'incomplete membership' {$script:nativeStack.pull_requests=@($script:nativeStack.pull_requests[1])}
            'duplicate member' {$script:nativeStack.pull_requests[0].number=5}
            'reordered membership' {$script:nativeStack.pull_requests=@($script:nativeStack.pull_requests[1],$script:nativeStack.pull_requests[0])}
            'wrong selected position' {$script:nativePulls['5'].stack.position=1}
            'foreign prefix repository' {$script:nativePulls['4'].head.repo.full_name='other/repo'}
            'foreign lightweight repository' {$script:nativeStack.pull_requests[0].head.repo.id=1}
            'changed prefix head' {$script:nativePulls['4'].head.sha='2'*40}
            'changed prefix base' {$script:nativePulls['4'].base.sha='2'*40}
            'changed prefix stack' {$script:nativePulls['4'].stack.number=1053}
            'changed prefix state' {$script:nativePulls['4'].state='closed'}
            'missing prefix stack' {$script:nativePulls['4'].stack=$null}
            'unknown prefix state' {$script:nativePulls['4'].state='unknown'}
            'empty prefix branch' {$script:nativePulls['4'].head.ref=''}
            'wrong prefix number' {$script:nativePulls['4'].number=99}
            'foreign prefix base' {$script:nativePulls['4'].base.repo.full_name='other/repo'}
            'wrong prefix branch chain' {$script:nativePulls['4'].base.ref='other';$script:nativeStack.pull_requests[0].base.ref='other'}
            'unconfirmed prefix merge' {$script:nativePulls['4'].mergeable=$null}
            'inconsistent active merge' {$script:nativePulls['4'].merged=$true}
            'nonboolean active merge' {$script:nativePulls['4'].merged='false'}
            'wrong prefix merge commit' {$script:nativeCommits[$parentMerge].sha='2'*40}
            'wrong prefix merge parent' {$script:nativeCommits[$parentMerge].parents[0].sha='2'*40}
            'extra prefix merge parent' {$script:nativeCommits[$parentMerge].parents+=[pscustomobject]@{sha=('2'*40)}}
            'raw branch parent' {$script:nativeCommits[$nativeMerge].parents[0].sha=$parentHead}
            'wrong selected source parent' {$script:nativeCommits[$nativeMerge].parents[1].sha='2'*40}
            'wrong root ref' {$script:nativeRef.ref='refs/tags/main'}
            'noncommit root' {$script:nativeRef.object.type='tag'}
            'closed unmerged prefix' {$script:nativePulls['4'].state='closed';$script:nativeStack.pull_requests[0].state='closed';$script:nativeStack.pull_requests[0].merged_at='2026-10-08T00:00:00Z'}
            'merged after active' {
                $middle = $script:nativePulls['4'] | ConvertTo-Json -Depth 8 | ConvertFrom-Json
                $middle.number=6;$middle.state='closed';$middle.merged=$true;$middle.merge_commit_sha=$landing;$middle.stack=$null
                $middle.head.sha='2'*40;$middle.head.ref='codex/middle'
                $script:nativePulls['6']=$middle
                $script:nativePulls['4'].stack.size=3
                $script:nativePulls['5'].stack.size=3;$script:nativePulls['5'].stack.position=3
                $script:nativeStack.pull_requests=@((New-NativeLightMember $script:nativePulls['4']),(New-NativeLightMember $middle),(New-NativeLightMember $script:nativePulls['5']))
                $script:nativeStack.pull_requests[1].merged_at='2026-10-08T00:00:00Z'
            }
        }
        {Invoke-NativeSource} | Should -Throw
    }
    It 'rejects an unproven merged prefix: <Fault>' -TestCases @(
        @{Fault='missing landing'},@{Fault='missing merged timestamp'},@{Fault='noninteger comparison count'},@{Fault='not contained'},@{Fault='wrong comparison base'},@{Fault='unknown comparison'},@{Fault='not rebased to trunk'}
    ) {
        param($Fault)
        Set-NativeMergedPrefix
        switch ($Fault) {
            'missing landing' {$script:nativePulls['4'].merge_commit_sha=$null}
            'missing merged timestamp' {$script:nativeStack.pull_requests[0].merged_at=$null}
            'noninteger comparison count' {$script:nativeComparison.behind_by='0'}
            'not contained' {$script:nativeComparison.merge_base_commit.sha='2'*40;$script:nativeComparison.behind_by=1;$script:nativeComparison.status='diverged'}
            'wrong comparison base' {$script:nativeComparison.base_commit.sha='2'*40}
            'unknown comparison' {$script:nativeComparison.status='unknown'}
            'not rebased to trunk' {$script:nativePulls['5'].base.ref='codex/parent';$script:nativeStack.pull_requests[1].base.ref='codex/parent'}
        }
        {Invoke-NativeSource} | Should -Throw
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
