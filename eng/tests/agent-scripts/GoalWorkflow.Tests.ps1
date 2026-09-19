#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Issue-driven goal workflow' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/invoke-github-issue-goal.ps1'
        $checkpoint = Join-Path $TestDrive 'goals/741/checkpoint.json'
        function New-IssueJson {
            param([string]$Body = "## Problem`nA safe problem.`n`n## Acceptance criteria`n- [AC1] Resume safely.")
            [pscustomobject]@{
                number = 741
                title = '[Task] Goal route fixture'
                state = 'open'
                body = $Body
                html_url = 'https://github.com/Gibbs-Morris/mississippi/issues/741'
            } | ConvertTo-Json -Depth 6 -Compress
        }
        function Invoke-Goal {
            param(
                [ValidateSet('start', 'resume')][string]$Action = 'start',
                [string]$Body = "## Problem`nA safe problem.`n`n## Acceptance criteria`n- [AC1] Resume safely.",
                [string]$Head = 'head-fixture',
                [string]$Base = 'base-fixture',
                [string]$Operation = ''
            )
            $arguments = @('-NoProfile', '-File', $scriptPath, '-Action', $Action,
                '-RepositoryOwner', 'Gibbs-Morris', '-RepositoryName', 'mississippi',
                '-IssueNumber', '741', '-IssueJson', (New-IssueJson -Body $Body),
                '-CheckpointPath', $checkpoint, '-RepositoryRoot', $repoRoot,
                '-HeadRevision', $Head, '-BaseRevision', $Base, '-Json')
            if ($Operation) { $arguments += @('-OperationStateJson', $Operation) }
            $output = & $powerShellPath @arguments 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output; Result = $output | ConvertFrom-Json }
        }
    }

    It 'starts from one issue and records a non-authorizing checkpoint' {
        $outcome = Invoke-Goal

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'started'
        Test-Path -LiteralPath $checkpoint -PathType Leaf | Should -BeTrue
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
        $saved.Contract.IssueTextIsData | Should -BeTrue
        $saved.Contract.CommandsExecutedFromIssueText | Should -BeFalse
        $saved.MergeBoundary | Should -Be 'PR_READY_NOT_MERGED'
    }

    It 'resumes without claiming evidence is fresh before revalidation' {
        $null = Invoke-Goal
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'resumed'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'detects edited issue scope before implementation continues' {
        $null = Invoke-Goal
        $outcome = Invoke-Goal -Action resume -Body "## Problem`nChanged scope.`n`n## Acceptance criteria`n- [AC1] Different."

        $outcome.ExitCode | Should -Be 2
        $outcome.Result.Status | Should -Be 'scope-changed'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'invalidates evidence when the source revision changes' {
        $null = Invoke-Goal
        $outcome = Invoke-Goal -Action resume -Head 'new-head-fixture'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'evidence-stale'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'waits on a running operation handle instead of duplicating it' {
        $operation = '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $null = Invoke-Goal -Operation $operation
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 2
        $outcome.Result.Status | Should -Be 'operation-running'
        $outcome.Result.NextAction | Should -Match 'job-123'
    }

    It 'returns a structured JSON error for a missing resume checkpoint' {
        Remove-Item -LiteralPath $checkpoint -Force -ErrorAction SilentlyContinue
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
    }
}
