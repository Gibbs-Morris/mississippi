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
            [pscustomobject]@{ number = 741; title = 'Coverage binding'; state = 'open'; type = 'issue' }
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
    }

    It 'accepts a local shorthand and same-repository issue URL' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #741 and https://github.com/Gibbs-Morris/mississippi/issues/741.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
        @($outcome.Result.ResolvedIssues).Count | Should -Be 1
    }

    It 'accepts a qualified same-repository issue reference' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs Gibbs-Morris/mississippi#741.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores a same-repository pull request link when an issue is present' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #741; parent PR: https://github.com/Gibbs-Morris/mississippi/pull/743.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'accepts a Markdown URI autolink to a same-repository issue' {
        $outcome = Invoke-ReferenceValidator -Body '<https://github.com/Gibbs-Morris/mississippi/issues/741>'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'rejects a full issue URL with an invalid numeric boundary' {
        $outcome = Invoke-ReferenceValidator -Body 'https://github.com/Gibbs-Morris/mississippi/issues/741abc'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'accepts a used Markdown reference definition' {
        $outcome = Invoke-ReferenceValidator -Body "[tracking issue][work]`r`n`r`n[work]: https://github.com/Gibbs-Morris/mississippi/issues/741"

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'accepts collapsed reference-style issue links' {
        $body = '[tracking issue][]' + [Environment]::NewLine + [Environment]::NewLine + '[tracking issue]: https://github.com/Gibbs-Morris/mississippi/issues/741'
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores balanced Markdown link destinations while scanning shorthand' {
        $outcome = Invoke-ReferenceValidator -Body '[tracking](https://example.test/a(b)#741)'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores escaped parentheses inside Markdown link destinations' {
        $outcome = Invoke-ReferenceValidator -Body '[tracking](https://example.test/a\)#741)'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores issue URLs nested inside an external URL' {
        $outcome = Invoke-ReferenceValidator -Body 'Context: https://example.test/?next=https://github.com/Gibbs-Morris/mississippi/issues/741'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores quoted indented code blocks' {
        $outcome = Invoke-ReferenceValidator -Body ">     Refs #741"

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores nested quoted indented code blocks' {
        $outcome = Invoke-ReferenceValidator -Body '> >     Refs #741'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'accepts an issue URL in an ordinary Markdown link' {
        $outcome = Invoke-ReferenceValidator -Body '[tracking issue](https://github.com/Gibbs-Morris/mississippi/issues/741)'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'preserves a reference after inline comment opener code' {
        $outcome = Invoke-ReferenceValidator -Body 'The token `<!--` is code. Refs #741.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores an unused Markdown reference definition' {
        $outcome = Invoke-ReferenceValidator -Body '[tracking]: https://github.com/Gibbs-Morris/mississippi/issues/741'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'allows a closed ancillary issue when an open issue is present' {
        $outcome = Invoke-ReferenceValidator -Body 'Refs #741; supersedes #742.'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
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
        $outcome = Invoke-ReferenceValidator -Body 'Context: https://github.com/other/repo/issues/741.'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores variable-length Markdown fences' {
        $body = @'
````md
Refs #741
`````
'@
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'does not close a fence indented beyond three spaces' {
        $body = "```md`r`nRefs #741`r`n    ``` `r`nRefs #741"
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores variable-width inline code spans' {
        $outcome = Invoke-ReferenceValidator -Body '``Refs #741``'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'does not hide text when the inline-code closing run length differs' {
        $outcome = Invoke-ReferenceValidator -Body '``Refs #741```'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores indented Markdown code blocks' {
        $outcome = Invoke-ReferenceValidator -Body "    Refs #741"

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores issue-looking tokens in raw HTML attributes' {
        $outcome = Invoke-ReferenceValidator -Body '<span title="#741">tracking</span>'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores shorthand tokens in Markdown link destinations' {
        $outcome = Invoke-ReferenceValidator -Body '[tracking details](#741)'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'ignores shorthand tokens embedded in bare URLs' {
        $outcome = Invoke-ReferenceValidator -Body 'https://example.test/?issue=#741'

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Errors | Should -Contain 'No repository issue reference was found in the rendered pull request description.'
    }

    It 'caps API resolution work for excessive references' {
        $body = (1..25 | ForEach-Object { "Refs #$($_)" }) -join ' '
        $outcome = Invoke-ReferenceValidator -Body $body

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'maximum supported is 20'
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
        $outcome.Output | Should -Match 'could not resolve any constituent pull requests'
    }

    It 'publishes PR-isolated status contexts' {
        $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/pr-issue-reference.yml') -Raw

        $workflow | Should -Match 'PR Issue Reference / PR #'
        $workflow | Should -Match 'PR_NUMBER'
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
