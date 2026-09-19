#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR issue reference validator' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/validate-pr-issue-reference.ps1'
        $knownIssues = @(
            [pscustomobject]@{ number = 741; title = 'Coverage binding'; state = 'open'; type = 'issue' }
            [pscustomobject]@{ number = 742; title = 'Worktree lease'; state = 'closed'; type = 'issue' }
            [pscustomobject]@{ number = 743; title = 'Native command bounds'; state = 'open'; type = 'pull_request' }
        ) | ConvertTo-Json -Compress

        function Invoke-ReferenceValidator {
            param([Parameter(Mandatory)][string]$Body)
            $output = & $powerShellPath -NoProfile -File $scriptPath -Body $Body -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $knownIssues -Json 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $output | ConvertFrom-Json; Output = $output }
        }
    }

    It 'accepts a local shorthand and same-repository issue URL' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #741 and https://github.com/Gibbs-Morris/mississippi/issues/741.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
        @($outcome.Result.ResolvedIssues).Count | Should -Be 1
    }

    It 'ignores fenced and HTML-comment examples' {
        $body = @'
```md
Refs #741
```
<!-- Refs #741 -->
'@
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'rejects missing, cross-repository and pull-request references' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #741 and #999; see https://github.com/other/repo/issues/741 and https://github.com/Gibbs-Morris/mississippi/pull/743.'

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'does not exist'
        ($outcome.Result.Errors -join "`n") | Should -Match 'Pull request URL'
    }

    It 'rejects external-only context without a local issue' {
        $outcome = Invoke-ReferenceValidator -Body 'Context: https://github.com/other/repo/issues/741.'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'rejects closed issues' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #742'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Match 'not open'
    }

    It 'rejects a PR number masquerading as an issue' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #743'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Match 'pull request, not an issue'
    }
}
