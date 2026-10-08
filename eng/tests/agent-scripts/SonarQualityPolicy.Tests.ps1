#!/usr/bin/env pwsh

#requires -Module Pester
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Sonar baseline and authentic provider verification' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/TrustedSonarAnalysis.psm1') -Force
        function Invoke-Policy { Get-SonarQualityPolicySnapshot -Source $source }
        function Invoke-Published { Assert-SonarPublishedAnalysis -Source $source -Repository Gibbs-Morris/mississippi -StartedAt ([datetimeoffset]'2026-10-07T12:00:00Z') }
    }
    BeforeEach {
        $source = [pscustomobject]@{Mode='Queue';HeadSha=('a'*40);HeadRef='gh-readonly-queue/main/pr-5';TargetRef='main';TargetSha=('b'*40)}
        $script:assignment = [pscustomobject]@{qualityGate=[pscustomobject]@{id=126237}}
        $script:definition = [pscustomobject]@{conditions=@(
            [pscustomobject]@{metric='new_security_rating';op='GT';error='1'},[pscustomobject]@{metric='new_reliability_rating';op='GT';error='1'},
            [pscustomobject]@{metric='new_maintainability_rating';op='GT';error='1'},[pscustomobject]@{metric='new_security_hotspots_reviewed';op='LT';error='100'},
            [pscustomobject]@{metric='new_code_smells';op='GT';error='0'},[pscustomobject]@{metric='new_violations';op='GT';error='0'},
            [pscustomobject]@{metric='new_duplicated_lines_density';op='GT';error='6'},[pscustomobject]@{metric='new_coverage';op='LT';error='60'}
        )}
        $script:classification = [pscustomobject]@{settings=@([pscustomobject]@{key='sonar.branch.longLivedBranches.regex';value='(branch|release)-.*'})}
        $script:branches = [pscustomobject]@{branches=@(
            [pscustomobject]@{name='main';isMain=$true;type='LONG';commit=[pscustomobject]@{sha=('b'*40)};status=[pscustomobject]@{qualityGateStatus='OK'}},
            [pscustomobject]@{name='gh-readonly-queue/main/pr-5';isMain=$false;type='SHORT';commit=[pscustomobject]@{sha=('a'*40)};status=[pscustomobject]@{qualityGateStatus='OK'}}
        )}
        $script:checks = [pscustomobject]@{total_count=1;check_runs=@([pscustomobject]@{app=[pscustomobject]@{id=12526};head_sha=('a'*40);status='completed';conclusion='success';started_at='2026-10-07T12:01:00Z'})}
        Mock Read-SonarServiceMetadata -ModuleName TrustedSonarAnalysis {
            if ($Endpoint -like 'qualitygates/get*') { return $script:assignment }
            if ($Endpoint -like 'qualitygates/show*') { return $script:definition }
            if ($Endpoint -like 'settings/*') { return $script:classification }
            if ($Endpoint -like 'project_branches/*') { return $script:branches }
            throw 'Unexpected Sonar endpoint.'
        }
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis { return $script:checks }
        Mock Start-Sleep -ModuleName TrustedSonarAnalysis {}
    }
    It 'accepts the exact target baseline, reviewed gate and distinct SHORT candidate' {
        $policy = Invoke-Policy
        $policy.GateId | Should -Be 126237
        { Assert-SonarQualityPolicyUnchanged -Before $policy -After (Invoke-Policy) } | Should -Not -Throw
        { Invoke-Published } | Should -Not -Throw
    }
    It 'checks classification before first candidate analysis' {
        $script:branches.branches=@($script:branches.branches[0])
        { Invoke-Policy } | Should -Not -Throw
        $script:classification.settings[0].value='.*'
        { Invoke-Policy } | Should -Throw '*classification*'
    }
    It 'requires default-branch uploads to remain LONG Sonar main: <Main>/<Type>' -TestCases @(
        @{Main=$true;Type='LONG';Accepted=$true},@{Main=$false;Type='LONG';Accepted=$false},
        @{Main=$true;Type='SHORT';Accepted=$false},@{Main=$false;Type='SHORT';Accepted=$false}
    ) {
        param($Main,$Type,$Accepted)
        $source.Mode='Branch';$source.HeadRef='main';$source.HeadSha='b'*40
        $script:checks.check_runs[0].head_sha='b'*40
        $script:branches.branches[0].isMain=$Main;$script:branches.branches[0].type=$Type
        if($Accepted){{Invoke-Published} | Should -Not -Throw}else{{Invoke-Published} | Should -Throw '*LONG Sonar main*'}
    }
    It 'rejects an unsafe queue baseline or policy: <Case>' -TestCases @(@{Case='stale target'},@{Case='missing target'},@{Case='long candidate'},@{Case='candidate is main'},@{Case='weak security'},@{Case='weak hotspot review'},@{Case='missing condition'},@{Case='duplicate condition'},@{Case='unknown classification'}) {
        param($Case)
        switch ($Case) {
            'stale target' { $script:branches.branches[0].commit.sha='c'*40 }
            'missing target' { $script:branches.branches=@($script:branches.branches[1]) }
            'long candidate' { $script:branches.branches[1].type='LONG' }
            'candidate is main' { $script:branches.branches[1].isMain=$true }
            'weak security' { $script:definition.conditions[0].error='2' }
            'weak hotspot review' { $script:definition.conditions[3].error='80' }
            'missing condition' { $script:definition.conditions=@($script:definition.conditions | Where-Object metric -ne new_violations) }
            'duplicate condition' { $script:definition.conditions+=@($script:definition.conditions[0]) }
            'unknown classification' { $script:classification.settings=@() }
        }
        { Invoke-Policy } | Should -Throw
    }

    It 'accepts targeted manual <Type> branches only with the exact LONG main baseline' -TestCases @(@{Type='SHORT'},@{Type='LONG'}) {
        param($Type)
        $source.Mode='Branch';$source.HeadRef='branch/manual'
        $script:branches.branches[1].name=$source.HeadRef
        $script:branches.branches[1].type=$Type
        {Invoke-Policy} | Should -Not -Throw
    }
    It 'rejects an invalid targeted manual baseline: <Case>' -TestCases @(
        @{Case='stale target'},@{Case='missing target'},@{Case='duplicate target'},
        @{Case='SHORT target'},@{Case='target is not main'},@{Case='malformed backend revision'},@{Case='malformed source target'}
    ) {
        param($Case)
        $source.Mode='Branch';$source.HeadRef='branch/manual'
        switch($Case){
            'stale target' {$script:branches.branches[0].commit.sha='c'*40}
            'missing target' {$script:branches.branches=@($script:branches.branches[1])}
            'duplicate target' {$script:branches.branches+=$script:branches.branches[0]}
            'SHORT target' {$script:branches.branches[0].type='SHORT'}
            'target is not main' {$script:branches.branches[0].isMain=$false}
            'malformed backend revision' {$script:branches.branches[0].commit.sha='invalid'}
            'malformed source target' {$source.TargetSha='invalid'}
        }
        {Invoke-Policy} | Should -Throw
    }
    It 'rejects baseline movement at the manual completion policy recheck' {
        $source.Mode='Branch';$source.HeadRef='branch/manual'
        $before=Invoke-Policy
        $script:branches.branches[0].commit.sha='c'*40
        {Assert-SonarQualityPolicyUnchanged -Before $before -After (Invoke-Policy)} | Should -Throw
    }
    It 'allows main refresh without requiring its prior Sonar baseline revision' {
        $source.Mode='Branch';$source.HeadRef='main';$source.TargetSha=$null
        $script:branches.branches[0].commit.sha='c'*40
        {Invoke-Policy} | Should -Not -Throw
    }

    It 'rejects gate reassignment or changed conditions during analysis' {
        $before = Invoke-Policy
        $script:assignment.qualityGate.id=99
        { Assert-SonarQualityPolicyUnchanged -Before $before -After (Invoke-Policy) } | Should -Throw '*policy changed*'
        $script:assignment.qualityGate.id=126237
        $script:definition.conditions+=@([pscustomobject]@{metric='new_bugs';op='GT';error='0'})
        { Assert-SonarQualityPolicyUnchanged -Before $before -After (Invoke-Policy) } | Should -Throw '*policy changed*'
    }
    It 'rejects incorrect backend or provider identity: <Case>' -TestCases @(@{Case='wrong branch revision'},@{Case='failed gate'},@{Case='LONG upload'},@{Case='main upload'},@{Case='missing branch'},@{Case='wrong app'},@{Case='wrong check revision'},@{Case='stale check'},@{Case='failed check'},@{Case='pending check'},@{Case='incomplete checks'}) {
        param($Case)
        switch ($Case) {
            'wrong branch revision' { $script:branches.branches[1].commit.sha='c'*40 }
            'failed gate' { $script:branches.branches[1].status.qualityGateStatus='ERROR' }
            'LONG upload' { $script:branches.branches[1].type='LONG' }
            'main upload' { $script:branches.branches[1].isMain=$true }
            'missing branch' { $script:branches.branches=@($script:branches.branches[0]) }
            'wrong app' { $script:checks.check_runs[0].app.id=15368 }
            'wrong check revision' { $script:checks.check_runs[0].head_sha='c'*40 }
            'stale check' { $script:checks.check_runs[0].started_at='2026-10-07T11:59:59Z' }
            'failed check' { $script:checks.check_runs[0].conclusion='failure' }
            'pending check' { $script:checks.check_runs[0].status='in_progress' }
            'incomplete checks' { $script:checks.total_count=101 }
        }
        { Invoke-Published } | Should -Throw
    }
    It 'permits delayed genuine provider delivery within the bounded wait' {
        $script:readCount=0
        Mock Read-SonarGitHubMetadata -ModuleName TrustedSonarAnalysis {
            $script:readCount++
            if($script:readCount -eq 1){return [pscustomobject]@{total_count=0;check_runs=@()}}
            return $script:checks
        }
        { Invoke-Published } | Should -Not -Throw
        Should -Invoke Start-Sleep -ModuleName TrustedSonarAnalysis -Times 1 -Exactly
    }
    It 'verifies PR provider checks without treating the PR as a branch analysis' {
        $source.Mode='PullRequest'
        { Invoke-Published } | Should -Not -Throw
        Should -Invoke Read-SonarServiceMetadata -ModuleName TrustedSonarAnalysis -Times 0 -Exactly
    }
}
