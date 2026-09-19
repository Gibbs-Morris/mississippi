#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR readiness snapshot' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/get-pr-readiness.ps1'
        function Invoke-Readiness {
            param([Parameter(Mandatory)][object]$Snapshot)
            $json = $Snapshot | ConvertTo-Json -Depth 10 -Compress
            $output = & $powerShellPath -NoProfile -File $scriptPath -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -PullRequestNumber 744 -SnapshotJson $json -Json 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $output | ConvertFrom-Json }
        }
        $readySnapshot = [pscustomobject]@{
            DataComplete = $true; HeadAtStart = 'head'; HeadAtEnd = 'head'; BaseAtStart = 'base'; BaseAtEnd = 'base';
            Checks = @([pscustomobject]@{Name='build';State='pass';Required=$true}); ReviewThreads=@(); Approvals=1;
            IssueReferenceVerified=$true; DescriptionReviewed=$true; PullRequestUrl='https://github.com/Gibbs-Morris/mississippi/pull/744'
        }
    }

    It 'reports ready only when mechanical and semantic evidence is complete' {
        $outcome = Invoke-Readiness -Snapshot $readySnapshot
        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'READY'
        $outcome.Result.MechanicalGateReady | Should -BeTrue
    }

    It 'distinguishes mechanical readiness from semantic review' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.IssueReferenceVerified = $false
        $outcome = Invoke-Readiness -Snapshot $snapshot
        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'MECHANICALLY_READY_SEMANTIC_REVIEW_REQUIRED'
        $outcome.Result.SemanticReviewRequired | Should -BeTrue
    }

    It 'fails closed for pending checks, stale heads and unresolved threads' {
        $snapshot = $readySnapshot.PSObject.Copy()
        $snapshot.HeadAtEnd = 'new-head'
        $snapshot.Checks = @([pscustomobject]@{Name='build';State='pending';Required=$true})
        $snapshot.ReviewThreads = @([pscustomobject]@{IsResolved=$false;IsOutdated=$false})
        $snapshot.Approvals = 0
        $outcome = Invoke-Readiness -Snapshot $snapshot
        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Status | Should -Be 'INCOMPLETE'
        @($outcome.Result.Blockers).Count | Should -BeGreaterThan 2
    }
}
