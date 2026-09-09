#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeDiscovery {
    $queryRegressions = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'fixtures/pr-feedback-query-regressions.json') -Raw |
        ConvertFrom-Json -AsHashtable
}

BeforeAll {
    $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
    $skillPath = '.agents/skills/address-pull-request-feedback'
    $query = (Get-Content -LiteralPath (Join-Path $repositoryRoot "$skillPath/scripts/list-review-threads.graphql") -Raw).Replace("`r`n", "`n")

    function Assert-ReviewQueryContract {
        param([Parameter(Mandatory)][string]$Query)

        $requirements = @{
            'read-only operation' = '\A\s*query\s+ReviewThreads\s*\('
            'cursor variable' = '\$endCursor\s*:\s*String\b'
            'cursor binding' = 'reviewThreads\s*\([^)]*\bafter\s*:\s*\$endCursor\s*\)'
            'continuation fields' = 'pageInfo\s*\{(?=[^{}]*\bhasNextPage\b)(?=[^{}]*\bendCursor\b)'
            'comment anchor' = 'comments\s*\(\s*first\s*:\s*1\s*\)\s*\{\s*nodes\s*\{(?=[^{}]*\bid\b)(?=[^{}]*\burl\b)'
            'thread identity and state' = 'reviewThreads\s*\([^)]*\)\s*\{\s*nodes\s*\{(?=[^{}]*\bid\b)(?=[^{}]*\bisResolved\b)(?=[^{}]*\bisOutdated\b)'
        }
        foreach ($requirement in $requirements.GetEnumerator()) {
            if ($Query -cnotmatch $requirement.Value) { throw "Missing query contract: $($requirement.Key)" }
        }
        if ($Query -cmatch '\b(mutation|subscription)\b') { throw 'Unexpected non-query operation' }
    }

    function Assert-FeedbackSkillRoute {
        param([Parameter(Mandatory)][string]$ConsumerPath)

        $content = Get-Content -LiteralPath $ConsumerPath -Raw
        $links = [regex]::Matches($content, '\]\(([^)]+address-pull-request-feedback/SKILL\.md)\)')
        if ($links.Count -eq 0) { throw 'Missing feedback skill route' }
        foreach ($link in $links) {
            $target = Join-Path (Split-Path -Parent $ConsumerPath) $link.Groups[1].Value
            if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw 'Broken feedback skill route' }
        }
    }
}

Describe 'PR feedback skill query contract' {
    It 'ships the cursor, continuation, identity, and anchor fields needed for complete collection' {
        { Assert-ReviewQueryContract -Query $query } | Should -Not -Throw
    }

    It 'rejects the fixture regression: <name>' -ForEach $queryRegressions {
        $regressedQuery = $query.Replace($before, $after)
        $regressedQuery | Should -Not -BeExactly $query
        { Assert-ReviewQueryContract -Query $regressedQuery } | Should -Throw
    }
}

Describe 'PR feedback skill consumer routes' {
    It 'resolves the route from <consumer>' -ForEach @(
        @{ consumer = '.github/instructions/pr-review-polling.instructions.md' },
        @{ consumer = '.github/agents/CoV-mississippi-pr-review-mcp.agent.md' },
        @{ consumer = '.github/agents/cs-pr-manager.agent.md' },
        @{ consumer = '.github/clean-squad/WORKFLOW.md' }
    ) {
        { Assert-FeedbackSkillRoute -ConsumerPath (Join-Path $repositoryRoot $consumer) } | Should -Not -Throw
    }

    It 'rejects a missing or broken consumer route: <content>' -ForEach @(
        @{ content = 'No route remains.' },
        @{ content = '[Feedback](missing/address-pull-request-feedback/SKILL.md)' }
    ) {
        $consumerPath = Join-Path $TestDrive 'consumer.md'
        Set-Content -LiteralPath $consumerPath -Value $content
        { Assert-FeedbackSkillRoute -ConsumerPath $consumerPath } | Should -Throw '*feedback skill route*'
    }
}
