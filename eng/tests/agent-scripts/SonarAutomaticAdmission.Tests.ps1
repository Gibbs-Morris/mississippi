#!/usr/bin/env pwsh
#requires -Module Pester
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

Describe 'Trusted Sonar automatic event admission' {
    BeforeAll {
        $repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        function Invoke-Admission {Get-TrustedSonarAutomaticAdmission -EventName $script:eventName -Payload $script:payload -RunId 42 -Repository Gibbs-Morris/mississippi -Enabled $script:enabled}
    }
    BeforeEach {
        $script:eventName='workflow_run';$script:enabled='true'
        $script:run=[pscustomobject]@{id=42;run_attempt=1;name='SonarCloud';workflow_id=141036039;status='completed';conclusion='success';event='push';head_sha=('a'*40);head_branch='main';repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'};head_repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}
        $script:payload=[pscustomobject]@{action='completed';repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'};workflow_run=$script:run}
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if($Path -like '*/actions/runs/*'){return $script:run}
            return [pscustomobject]@{object=[pscustomobject]@{sha=('a'*40)}}
        }
        $script:run | Add-Member -NotePropertyName path -NotePropertyValue '.github/workflows/sonar-cloud.yml'
    }
    It 'correlates the successful event with the fresh approved source run' {
        $admission=Invoke-Admission
        $admission.RunAttempt | Should -Be 1
        (Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main -AutomaticAdmission $admission).HeadSha | Should -Be ('a'*40)
    }
    It 'accepts genuine 64-bit run IDs' {
        $script:run.id=[long]37706334660
        $admission=Get-TrustedSonarAutomaticAdmission -EventName workflow_run -Payload $script:payload -RunId 37706334660 -Repository Gibbs-Morris/mississippi -Enabled true
        (Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 37706334660 -DefaultBranch main -AutomaticAdmission $admission).RunId | Should -Be 37706334660
    }
    It 'keeps manual dispatch independent of automatic enablement' {
        $script:eventName='workflow_dispatch';$script:enabled='false';$script:payload=$null
        Invoke-Admission | Should -BeNullOrEmpty
        $script:run.conclusion='failure'
        {Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main} | Should -Not -Throw
    }
    It 'rejects malformed or unauthorized events: <Case>' -TestCases @(
        @{Case='disabled'},@{Case='unset'},@{Case='event'},@{Case='action'},@{Case='repository'},@{Case='run repository'},@{Case='fork'},@{Case='recursive workflow'},@{Case='workflow ID'},@{Case='run ID'},@{Case='string ID'},@{Case='unfinished'},@{Case='failed'},@{Case='cancelled'},@{Case='attempt zero'},@{Case='string attempt'},@{Case='SHA'},@{Case='ref'}
    ) {
        param($Case)
        switch($Case){
            'disabled' {$script:enabled='false'};'unset' {$script:enabled=''};'event' {$script:eventName='push'};'action' {$script:payload.action='requested'}
            'repository' {$script:payload.repository.full_name='other/repo'};'run repository' {$script:run.repository.full_name='other/repo'};'fork' {$script:run.head_repository.full_name='fork/repo'}
            'recursive workflow' {$script:run.name='Trusted Sonar Analysis'};'workflow ID' {$script:run.workflow_id=99};'run ID' {$script:run.id=43};'string ID' {$script:run.id='42'}
            'unfinished' {$script:run.status='in_progress'};'failed' {$script:run.conclusion='failure'};'cancelled' {$script:run.conclusion='cancelled'}
            'attempt zero' {$script:run.run_attempt=0};'string attempt' {$script:run.run_attempt='1'};'SHA' {$script:run.head_sha='bad'};'ref' {$script:run.head_branch=''}
        }
        {Invoke-Admission} | Should -Throw
    }
    It 'rejects fresh run metadata changed since the event: <Field>' -TestCases @(@{Field='attempt'},@{Field='SHA'},@{Field='ref'},@{Field='event'},@{Field='conclusion'},@{Field='name'}) {
        param($Field)
        $admission=Invoke-Admission
        switch($Field){'attempt' {$script:run.run_attempt=2};'SHA' {$script:run.head_sha='b'*40};'ref' {$script:run.head_branch='other'};'event' {$script:run.event='workflow_dispatch'};'conclusion' {$script:run.conclusion='failure'};'name' {$script:run.name='Other'}}
        {Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main -AutomaticAdmission $admission} | Should -Throw '*triggering attempt*'
    }
    It 'serializes the same branch across runs without colliding with case-distinct branches or PRs' {
        $first=Get-TrustedSonarAnalysisKey -Source ([pscustomobject]@{Mode='Branch';HeadRef='main';RunId=1})
        $first | Should -Be (Get-TrustedSonarAnalysisKey -Source ([pscustomobject]@{Mode='Branch';HeadRef='main';RunId=2}))
        $first | Should -Not -Be (Get-TrustedSonarAnalysisKey -Source ([pscustomobject]@{Mode='Branch';HeadRef='Main'}))
        $first | Should -Match '^branch-[a-f0-9]{64}$'
        Get-TrustedSonarAnalysisKey -Source ([pscustomobject]@{Mode='PullRequest';PullRequest=5}) | Should -Be 'pr-5'
    }
}

Describe 'Automatic Sonar exact-baseline coordination' {
    BeforeAll {
        $repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/MergeGroupIssueReference.psm1') -Force
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        function Invoke-Ready {
            $admission=Get-TrustedSonarAutomaticAdmission -EventName workflow_run -Payload ([pscustomobject]@{action='completed';repository=$script:run.repository;workflow_run=$script:run}) -RunId 42 -Repository Gibbs-Morris/mississippi -Enabled true
            $source=Get-TrustedSonarSource -Repository Gibbs-Morris/mississippi -RunId 42 -DefaultBranch main -AutomaticAdmission $admission
            Wait-TrustedSonarBaseline -Source $source -Policy $script:capturedPolicy -Repository Gibbs-Morris/mississippi -DefaultBranch main -AutomaticAdmission $admission -Attempts 3
        }
    }
    BeforeEach {
        $script:run=[pscustomobject]@{id=42;run_attempt=1;workflow_id=141036039;name='SonarCloud';path='.github/workflows/sonar-cloud.yml';status='completed';conclusion='success';event='merge_group';head_sha=('a'*40);head_branch='gh-readonly-queue/main/pr-5';repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'};head_repository=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'};pull_requests=@()}
        $script:queue=[pscustomobject]@{nameWithOwner='Gibbs-Morris/mississippi';ref=[pscustomobject]@{target=[pscustomobject]@{oid=('b'*40)}};mergeQueue=[pscustomobject]@{id='queue-1';entries=[pscustomobject]@{totalCount=1;pageInfo=[pscustomobject]@{hasNextPage=$false;endCursor=$null};nodes=@([pscustomobject]@{id='entry-5';position=1;baseCommit=[pscustomobject]@{oid=('b'*40)};headCommit=[pscustomobject]@{oid=('a'*40)};pullRequest=[pscustomobject]@{number=5;body='Refs #1030';headRefOid=('c'*40);state='OPEN';repository=[pscustomobject]@{nameWithOwner='Gibbs-Morris/mississippi'}}})}}}
        $script:waitPolicy=[pscustomobject]@{GateId=126237;Conditions='reviewed';LongLivedPattern='(branch|release)-.*';BaselinePending=$false}
        $script:capturedPolicy=$null
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            if($Path -like '*/actions/runs/*'){return ($script:run|ConvertTo-Json -Depth 8|ConvertFrom-Json)}
            if($Path -like '*/git/ref/*'){return [pscustomobject]@{object=[pscustomobject]@{sha=('a'*40)}}}
            if($Path -like '*/pulls/*'){return $script:pr}
            if($Path -like '*/git/commits/*'){return [pscustomobject]@{sha=$script:pr.merge_commit_sha;parents=@([pscustomobject]@{sha=$script:pr.base.sha},[pscustomobject]@{sha=$script:pr.head.sha})}}
            throw 'Unexpected source API request.'
        }
        Mock Read-MergeQueuePage -ModuleName MergeGroupIssueReference {return ($script:queue|ConvertTo-Json -Depth 15|ConvertFrom-Json)}
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {return ($script:waitPolicy|ConvertTo-Json|ConvertFrom-Json)}
        Mock Start-Sleep -ModuleName TrustedSonarAnalysis {}
    }
    It 'accepts an exact current baseline without sleeping' {
        (Invoke-Ready).Source.HeadSha | Should -Be ('a'*40)
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
    It 'waits for ordinary baseline lag and returns the fresh policy/source' {
        $script:reads=0
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {$script:reads++;$script:waitPolicy.BaselinePending=$script:reads -eq 1;return ($script:waitPolicy|ConvertTo-Json|ConvertFrom-Json)}
        $ready=Invoke-Ready
        $ready.Policy.BaselinePending | Should -BeFalse
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 1 -Exactly -ParameterFilter {$Seconds -eq 60}
    }
    It 'bounds persistent lag' {
        $script:waitPolicy.BaselinePending=$true
        {Invoke-Ready} | Should -Throw '*did not become ready*'
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 2 -Exactly
    }
    It 'rejects candidate or source changes during the wait: <Case>' -TestCases @(@{Case='withdrawn'},@{Case='candidate changed'},@{Case='attempt changed'},@{Case='member body changed'}) {
        param($Case)
        $script:waitPolicy.BaselinePending=$true;$script:waitChange=$Case
        Mock Start-Sleep -ModuleName TrustedSonarAnalysis {
            switch($script:waitChange){
                'withdrawn' {$script:queue.mergeQueue.entries.nodes=@();$script:queue.mergeQueue.entries.totalCount=0}
                'candidate changed' {$script:queue.mergeQueue.entries.nodes[0].headCommit.oid='d'*40}
                'attempt changed' {$script:run.run_attempt=2}
                'member body changed' {$script:queue.mergeQueue.entries.nodes[0].pullRequest.body='Changed body'}
            }
        }
        {Invoke-Ready} | Should -Throw
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 1 -Exactly
    }
    It 'rechecks the successful attempt after policy reads' {
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {$script:run.run_attempt=2;return $script:waitPolicy}
        {Invoke-Ready} | Should -Throw '*triggering attempt*'
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
    It 'rejects changed policy during lag' {
        $script:waitPolicy.BaselinePending=$true
        Mock Start-Sleep -ModuleName TrustedSonarAnalysis {$script:waitPolicy.GateId=99}
        {Invoke-Ready} | Should -Throw '*policy changed*'
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 1 -Exactly
    }
    It 'retains the pre-build policy during admission before upload' {
        $script:capturedPolicy=$script:waitPolicy|ConvertTo-Json|ConvertFrom-Json
        $script:waitPolicy.GateId=99
        {Invoke-Ready} | Should -Throw '*policy changed*'
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
    It 'propagates policy API failures immediately' {
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {throw 'Policy API unavailable.'}
        {Invoke-Ready} | Should -Throw '*API unavailable*'
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
    It 'refreshes an exact baseline when a verified predecessor lands during policy reads' {
        $second=$script:queue.mergeQueue.entries.nodes[0];$second.id='entry-6';$second.position=2;$second.baseCommit.oid='d'*40;$second.pullRequest.number=6
        $first=[pscustomobject]@{id='entry-5';position=1;baseCommit=[pscustomobject]@{oid=('b'*40)};headCommit=[pscustomobject]@{oid=('d'*40)};pullRequest=[pscustomobject]@{number=5;body='Refs #1030';headRefOid=('e'*40);state='OPEN';repository=[pscustomobject]@{nameWithOwner='Gibbs-Morris/mississippi'}}}
        $script:queue.mergeQueue.entries.nodes=@($first,$second);$script:queue.mergeQueue.entries.totalCount=2;$script:reads=0
        Mock Get-SonarQualityPolicySnapshot -ModuleName TrustedSonarAnalysis {
            $script:reads++
            if($script:reads -eq 1){$script:queue.ref.target.oid='d'*40;$remaining=$script:queue.mergeQueue.entries.nodes[1];$remaining.position=1;$script:queue.mergeQueue.entries.nodes=@($remaining);$script:queue.mergeQueue.entries.totalCount=1}
            return $script:waitPolicy
        }
        (Invoke-Ready).Source.TargetSha | Should -Be ('d'*40)
        $script:reads | Should -Be 2
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 1 -Exactly
    }
    It 'does not wait for nonqueue analysis: <Mode>' -TestCases @(@{Mode='Branch'},@{Mode='PullRequest'}) {
        param($Mode)
        if($Mode -eq 'Branch'){$script:run.event='push';$script:run.head_branch='main'}else{
            $script:run.event='pull_request';$script:run.head_branch='codex/test';$script:run.pull_requests=@([pscustomobject]@{number=5;head=[pscustomobject]@{sha=('a'*40);ref='codex/test'}})
            $script:pr=[pscustomobject]@{state='open';mergeable=$true;merge_commit_sha=('c'*40);head=[pscustomobject]@{sha=('a'*40);ref='codex/test';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}};base=[pscustomobject]@{sha=('b'*40);ref='main';repo=[pscustomobject]@{full_name='Gibbs-Morris/mississippi'}}}
        }
        (Invoke-Ready).Source.Mode | Should -Be $Mode
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
}

Describe 'Trusted Sonar automatic workflow contract' {
    It 'keeps default-branch execution, read permissions, unique credentials and both concurrency scopes' {
        $repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $yaml=Get-Content (Join-Path $repoRoot '.github/workflows/sonar-trusted-analysis.yml') -Raw
        $yaml | Should -Match 'workflow_run:\s+workflows: \[SonarCloud\]\s+types: \[completed\]'
        $yaml | Should -Match "vars.SONAR_TRUSTED_ANALYSIS_ENABLED == 'true'"
        $yaml | Should -Match "github.event_name == 'workflow_dispatch'"
        $yaml | Should -Match 'permissions: \{\}'
        $yaml | Should -Match 'trusted-sonar-upload-.*needs.intake.outputs.analysis-key'
        ([regex]::Matches($yaml,'cancel-in-progress: false')).Count | Should -Be 2
        ([regex]::Matches($yaml,'(?m)^\s+ref:')).Count | Should -Be 0
        ([regex]::Matches($yaml,'(?m)^\s+repository:')).Count | Should -Be 0
        ([regex]::Matches($yaml,'uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1')).Count | Should -Be 2
        ([regex]::Matches($yaml,'persist-credentials: false')).Count | Should -Be 2
        $yaml | Should -Match 'environment: sonar-analysis'
        $yaml | Should -Match 'SONAR_ANALYSIS_TOKEN: \$\{\{ secrets.SONAR_ANALYSIS_TOKEN \}\}'
    }
}
