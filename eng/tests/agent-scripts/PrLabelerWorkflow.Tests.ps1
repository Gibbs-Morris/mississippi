#!/usr/bin/env pwsh

#requires -Module Pester

[CmdletBinding()]
param(
    [string]$WorkflowPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR file-labeling event admission' {
    BeforeAll {
        if (-not $WorkflowPath) {
            $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
            $WorkflowPath = Join-Path $repoRoot '.github/workflows/pr-labeler.yml'
        }
        $workflow = Get-Content -LiteralPath $WorkflowPath -Raw
        $typesMatch = [regex]::Match($workflow, '(?m)^    types: \[([^\]]+)\]')
        if (-not $typesMatch.Success) { throw 'Unable to read the labeler event types.' }
        $eventTypes = @($typesMatch.Groups[1].Value.Split(',') | ForEach-Object { $_.Trim() })
        $jobMatch = [regex]::Match($workflow, '(?ms)^  label-by-files:\r?\n(?<body>.*?)(?=^  [a-zA-Z][\w-]*:|\z)')
        if (-not $jobMatch.Success) { throw 'Unable to read the labeling job.' }
        $job = $jobMatch.Groups['body'].Value
        $guardMatch = [regex]::Match($job, '(?m)^    if: (.+)')
        $guard = if ($guardMatch.Success) { $guardMatch.Groups[1].Value.Trim() } else { 'true' }
        # Interpret the small equality/Boolean subset used by this workflow, not a second admission predicate.
        $tokenPattern = "github\.event\.changes\.base|github\.event\.action|'[a-z_]+'|!=|==|\|\||&&|\(|\)|\bnull\b|\btrue\b|\bfalse\b"
        $tokens = @([regex]::Matches($guard, $tokenPattern) | ForEach-Object { $_.Value })
        if (($tokens -join '') -ne ($guard -replace '\s', '')) { throw 'Unsupported labeler expression syntax.' }
        $translated = foreach ($token in $tokens) {
            switch ($token) {
                'github.event.action' { '$Action' }
                'github.event.changes.base' { '$BaseChange' }
                '!=' { '-ne' }
                '==' { '-eq' }
                '||' { '-or' }
                '&&' { '-and' }
                'null' { '$null' }
                'true' { '$true' }
                'false' { '$false' }
                default { $token }
            }
        }
        $guardExpression = [scriptblock]::Create($translated -join ' ')
        $workflowConcurrency = $workflow -match '(?m)^concurrency:'
        $jobConcurrency = $job -match '(?m)^    concurrency:'
        $cancelsRunning = $workflow -match '(?m)^\s+cancel-in-progress: true\s*$'

        $groupSource = if ($jobConcurrency) { $job } else { $workflow }
        $groupMatch = [regex]::Match($groupSource, '(?m)^\s+group: (.+)')
        if (-not $groupMatch.Success) { throw 'Unable to read the labeling concurrency group.' }
        $groupTemplate = $groupMatch.Groups[1].Value.Trim()

        function Get-LabelingGroup {
            param([int]$PullRequestNumber, [int]$RunId, [string]$Ref, [string]$WorkflowName = 'PR Labeler')

            [regex]::Replace($groupTemplate, '\$\{\{\s*(.*?)\s*\}\}', {
                param($match)
                foreach ($operand in ($match.Groups[1].Value -split '\|\|')) {
                    $value = switch ($operand.Trim()) {
                        'github.workflow' { $WorkflowName }
                        'github.event.pull_request.number' { $PullRequestNumber }
                        'github.ref' { $Ref }
                        'github.run_id' { $RunId }
                        default { throw 'Unsupported concurrency expression syntax.' }
                    }
                    if ($value) { return [string]$value }
                }
                return ''
            })
        }

        function Get-LabelingOutcome {
            param([string]$Action, [hashtable]$Changes)

            $BaseChange = $Changes['base']
            $triggered = $Action -in $eventTypes
            $admitted = $triggered -and [bool](& $guardExpression)
            # A workflow enters its concurrency group even if its only job skips.
            $joinsGroup = $triggered -and ($workflowConcurrency -or ($admitted -and $jobConcurrency))
            [pscustomobject]@{
                LabelsFiles = $admitted
                ReplacesPendingLabeling = $joinsGroup
                CancelsRunningLabeling = $joinsGroup -and $cancelsRunning
            }
        }
    }

    It 'shares a group across different runs and refs for the same PR' {
        $first = Get-LabelingGroup -PullRequestNumber 15 -RunId 100 -Ref 'refs/pull/15/merge'
        $next = Get-LabelingGroup -PullRequestNumber 15 -RunId 101 -Ref 'refs/pull/15/head'

        $first | Should -Be $next
    }

    It 'keeps other PRs and workflows in separate groups' {
        $first = Get-LabelingGroup -PullRequestNumber 15 -RunId 100 -Ref 'refs/pull/15/merge'
        $otherPr = Get-LabelingGroup -PullRequestNumber 16 -RunId 100 -Ref 'refs/pull/15/merge'
        $otherWorkflow = Get-LabelingGroup -PullRequestNumber 15 -RunId 100 -Ref 'refs/pull/15/merge' -WorkflowName 'Another workflow'

        $first | Should -Not -Be $otherPr
        $first | Should -Not -Be $otherWorkflow
    }

    It '<Name>' -TestCases @(
        @{ Name = 'labels a newly opened PR'; Action = 'opened'; Changes = @{}; Labels = $true; Supersedes = $true }
        @{ Name = 'replaces stale work after a new commit'; Action = 'synchronize'; Changes = @{}; Labels = $true; Supersedes = $true }
        @{ Name = 'refreshes a reopened PR'; Action = 'reopened'; Changes = @{}; Labels = $true; Supersedes = $true }
        @{ Name = 'preserves work across a title edit'; Action = 'edited'; Changes = @{ title = @{ from = 'Old title' } }; Labels = $false; Supersedes = $false }
        @{ Name = 'preserves work across a body edit'; Action = 'edited'; Changes = @{ body = @{ from = 'Old body' } }; Labels = $false; Supersedes = $false }
        @{ Name = 'preserves work across an edit without file changes'; Action = 'edited'; Changes = @{}; Labels = $false; Supersedes = $false }
        @{ Name = 'refreshes labels after changing base branch'; Action = 'edited'; Changes = @{ base = @{ ref = @{ from = 'main' }; sha = @{ from = ('a' * 40) } } }; Labels = $true; Supersedes = $true }
        @{ Name = 'refreshes a base change combined with a body edit'; Action = 'edited'; Changes = @{ base = @{ ref = @{ from = 'main' } }; body = @{ from = 'Old body' } }; Labels = $true; Supersedes = $true }
        @{ Name = 'ignores an unrelated PR event'; Action = 'labeled'; Changes = @{}; Labels = $false; Supersedes = $false }
    ) {
        param($Action, $Changes, $Labels, $Supersedes)

        $outcome = Get-LabelingOutcome -Action $Action -Changes $Changes
        $outcome.LabelsFiles | Should -Be $Labels
        $outcome.ReplacesPendingLabeling | Should -Be $Supersedes
        $outcome.CancelsRunningLabeling | Should -Be $Supersedes
    }
}
