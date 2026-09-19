#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'PR issue reference validator' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/validate-pr-issue-reference.ps1'
        $mergeScriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/validate-merge-group-pr-issue-reference.ps1'
        $knownIssues = @(
            [pscustomobject]@{ number = 741; title = 'Coverage binding'; state = 'open' }
            [pscustomobject]@{ number = 742; title = 'Worktree lease'; state = 'closed'; type = 'issue' }
            [pscustomobject]@{ number = 743; title = 'Native command bounds'; state = 'open'; type = 'pull_request' }
        ) | ConvertTo-Json -Compress

        function Invoke-ReferenceValidator {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $output = & $powerShellPath -NoProfile -File $scriptPath -Body $Body -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $knownIssues -Json 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $output | ConvertFrom-Json; Output = $output }
        }

        function Invoke-MergeGroupValidator {
            param([AllowEmptyCollection()][object[]]$PullRequests)
            $pullRequestsJson = if (@($PullRequests).Count -eq 0) { '[]' } else { ConvertTo-Json -InputObject @($PullRequests) -Depth 10 -Compress }
            $output = & $powerShellPath -NoProfile -File $mergeScriptPath -PullRequestsJson $pullRequestsJson -RepositoryOwner Gibbs-Morris -RepositoryName mississippi -KnownIssuesJson $knownIssues 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
        }

        function Assert-ValidReferenceBody {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $outcome = Invoke-ReferenceValidator -Body $Body
            $outcome.ExitCode | Should -Be 0
            $outcome.Result.Valid | Should -BeTrue
            return $outcome
        }

        function Assert-NoReferenceBody {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
            $outcome = Invoke-ReferenceValidator -Body $Body
            $outcome.ExitCode | Should -Not -Be 0
            $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
            return $outcome
        }
    }

    It 'accepts a local shorthand and same-repository issue URL' {
        $outcome = Assert-ValidReferenceBody -Body 'Refs #741 and https://github.com/Gibbs-Morris/mississippi/issues/741.'
        @($outcome.Result.ResolvedIssues).Count | Should -Be 1
    }

    It 'accepts a qualified same-repository issue reference' {
        Assert-ValidReferenceBody -Body 'Refs Gibbs-Morris/mississippi#741.'
    }

    It 'ignores a same-repository pull request link when an issue is present' {
        Assert-ValidReferenceBody -Body 'Refs #741; parent PR: https://github.com/Gibbs-Morris/mississippi/pull/743.'
    }

    It 'accepts a Markdown URI autolink to a same-repository issue' {
        Assert-ValidReferenceBody -Body '<https://github.com/Gibbs-Morris/mississippi/issues/741>'
    }

    It 'rejects a full issue URL with an invalid numeric boundary' {
        Assert-NoReferenceBody -Body 'https://github.com/Gibbs-Morris/mississippi/issues/741abc'
    }

    It 'accepts a used Markdown reference definition' {
        Assert-ValidReferenceBody -Body "[tracking issue][work]`r`n`r`n[work]: https://github.com/Gibbs-Morris/mississippi/issues/741"
    }

    It 'accepts collapsed reference-style issue links' {
        $body = '[tracking issue][]' + [Environment]::NewLine + [Environment]::NewLine + '[tracking issue]: https://github.com/Gibbs-Morris/mississippi/issues/741'
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores balanced Markdown link destinations while scanning shorthand' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/a(b)#741)'
    }

    It 'ignores escaped parentheses inside Markdown link destinations' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/a\)#741)'
    }

    It 'ignores issue URLs nested inside an external URL' {
        Assert-NoReferenceBody -Body 'Context: https://example.test/?next=https://github.com/Gibbs-Morris/mississippi/issues/741'
    }

    It 'does not recover a local shorthand from an external issue URL fragment' {
        Assert-NoReferenceBody -Body 'Context: https://github.com/other/repo/issues/999#741'
    }

    It 'ignores repository issue URLs nested after an external URL fragment' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/#https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores repository issue URLs nested after an external URI colon' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test/redirect:https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores repository issue URLs in Markdown link titles' {
        Assert-NoReferenceBody -Body '[tracking](https://example.test "See https://github.com/Gibbs-Morris/mississippi/issues/741.")'
    }

    It 'ignores quoted indented code blocks' {
        Assert-NoReferenceBody -Body ">     Refs #741"
    }

    It 'ignores nested quoted indented code blocks' {
        Assert-NoReferenceBody -Body '> >     Refs #741'
    }

    It 'accepts an issue URL in an ordinary Markdown link' {
        Assert-ValidReferenceBody -Body '[tracking issue](https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'accepts a rendered HTML anchor to a same-repository issue' {
        Assert-ValidReferenceBody -Body '<a href="https://github.com/Gibbs-Morris/mississippi/issues/741">tracking issue</a>'
    }

    It 'preserves a reference after inline comment opener code' {
        Assert-ValidReferenceBody -Body 'The token `<!--` is code. Refs #741.'
    }

    It 'ignores an unused Markdown reference definition' {
        Assert-NoReferenceBody -Body '[tracking]: https://github.com/Gibbs-Morris/mississippi/issues/741'
    }

    It 'ignores an unused multiline Markdown reference definition' {
        $body = "[unused]:`n  https://github.com/Gibbs-Morris/mississippi/issues/741"
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores a hidden continuation-line reference title' {
        $body = "[work]: https://example.test`n  (See https://github.com/Gibbs-Morris/mississippi/issues/741)"
        Assert-NoReferenceBody -Body $body
    }

    It 'allows a closed ancillary issue when an open issue is present' {
        Assert-ValidReferenceBody -Body 'Refs #741; supersedes #742.'
    }

    It 'returns structured output for an empty body' {
        $outcome = Invoke-ReferenceValidator -Body ''

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Valid | Should -BeFalse
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
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

    It 'rejects missing references while ignoring cross-repository context' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #999; see https://github.com/other/repo/issues/741.'

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'does not exist'
    }

    It 'rejects external-only context without a local issue' {
        Assert-NoReferenceBody -Body 'Context: https://github.com/other/repo/issues/741.'
    }

    It 'ignores variable-length Markdown fences' {
        $body = @'
````md
Refs #741
`````
'@
        Assert-NoReferenceBody -Body $body
    }

    It 'does not close a fence indented beyond three spaces' {
        $backtick = [char]96
        $fence = [string]::new($backtick, 3)
        $body = $fence + 'md' + [Environment]::NewLine + 'Refs #741' + [Environment]::NewLine + '    ' + $fence
        Assert-NoReferenceBody -Body $body
        return
    }

    It 'does not treat backticks in fence info strings as a code fence opener' {
        $backtick = [char]96
        $fence = [string]::new($backtick, 3)
        $body = $fence + 'md' + $backtick + [Environment]::NewLine + 'Refs #741' + [Environment]::NewLine + $fence
        Assert-ValidReferenceBody -Body $body
    }

    It 'ignores variable-width inline code spans' {
        Assert-NoReferenceBody -Body '``Refs #741``'
    }

    It 'does not hide text when the inline-code closing run length differs' {
        Assert-ValidReferenceBody -Body '``Refs #741```'
    }

    It 'preserves references after escaped backticks' {
        $backtick = [char]96
        $body = '\' + $backtick + 'Refs #741' + $backtick
        Assert-ValidReferenceBody -Body $body
    }

    It 'ignores indented Markdown code blocks' {
        Assert-NoReferenceBody -Body "    Refs #741"
    }

    It 'ignores issue-looking tokens in raw HTML attributes' {
        Assert-NoReferenceBody -Body '<span title="#741">tracking</span>'
    }

    It 'ignores shorthand tokens in Markdown link destinations' {
        Assert-NoReferenceBody -Body '[tracking details](#741)'
    }

    It 'does not treat task-list markers as shortcut references' {
        $body = "Checklist:`n- [x] done`n`n[x]: https://github.com/Gibbs-Morris/mississippi/issues/741"
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores escaped shortcut labels' {
        $body = '\[work]' + [Environment]::NewLine + '[work]: https://github.com/Gibbs-Morris/mississippi/issues/741'
        Assert-NoReferenceBody -Body $body
    }

    It 'ignores issue URLs used as Markdown image destinations' {
        Assert-NoReferenceBody -Body '![tracking](https://github.com/Gibbs-Morris/mississippi/issues/741)'
    }

    It 'ignores issue URLs in angle-enclosed Markdown image destinations' {
        Assert-NoReferenceBody -Body '![tracking](<https://github.com/Gibbs-Morris/mississippi/issues/741>)'
    }

    It 'preserves references after escaped closing brackets' {
        Assert-ValidReferenceBody -Body '\](Refs #741)'
    }

    It 'ignores shorthand tokens embedded in bare URLs' {
        Assert-NoReferenceBody -Body 'https://example.test/?issue=#741'
    }

    It 'caps API resolution work for excessive references' {
        $body = (1..25 | ForEach-Object { "Refs #$($_)" }) -join ' '
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'maximum supported is 20'
    }

    It 'preserves references after exhausting the Markdown link scan budget' {
        $body = ((1..100 | ForEach-Object { '[broken](' }) -join '') + ' Refs #741'
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
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

    It 'validates every constituent pull request in a merge-group fixture' {
        $fixture = @(
            [pscustomobject]@{ number = 101; body = 'Refs #741' },
            [pscustomobject]@{ number = 102; body = 'Refs Gibbs-Morris/mississippi#741' }
        )
        $outcome = Invoke-MergeGroupValidator -PullRequests $fixture

        $outcome.ExitCode | Should -Be 0
        (@($outcome.Output -split "`r?`n" | Select-String 'Validating merge-group pull request')).Count | Should -Be 2
    }

    It 'fails closed for an empty merge group' {
        $outcome = Invoke-MergeGroupValidator -PullRequests @()

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Output | Should -Match 'could not resolve any constituent[\s|]+pull requests'
    }

    It 'publishes PR-isolated status contexts' {
        $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/pr-issue-reference.yml') -Raw

        $workflow | Should -Match '\$statusContext = ''PR Issue Reference'''
        $workflow | Should -Match 'PR_NUMBER'
        $workflow | Should -Match 'DEFAULT_BRANCH'
        $workflow | Should -Match 'commits/\$env:DEFAULT_BRANCH'
        $workflow | Should -Not -Match 'actions/checkout'
    }

    It 'fails the merge group when one constituent PR has no open issue' {
        $fixture = @(
            [pscustomobject]@{ number = 101; body = 'Refs #741' },
            [pscustomobject]@{ number = 102; body = 'Refs #742' }
        )
        $outcome = Invoke-MergeGroupValidator -PullRequests $fixture

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Output | Should -Match 'failed for #102'
    }
}
