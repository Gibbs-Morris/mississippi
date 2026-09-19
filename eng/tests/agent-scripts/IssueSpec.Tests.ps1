#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Implementation-ready issue contract' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $validator = Join-Path $repoRoot 'eng/src/agent-scripts/test-issue-spec.ps1'
        $fixtureRoot = Join-Path $PSScriptRoot 'fixtures'

        function Invoke-Validator {
            param([Parameter(Mandatory)][string]$IssuePath)

            $output = & $powerShellPath -NoProfile -File $validator -Path $IssuePath -RepositoryRoot $repoRoot -Json 2>&1 | Out-String
            [pscustomobject]@{
                ExitCode = $LASTEXITCODE
                Output = $output
                Result = $output | ConvertFrom-Json
            }
        }

        function New-TemporaryIssue {
            param([Parameter(Mandatory)][string]$Content)

            $path = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '.md')
            Set-Content -LiteralPath $path -Value $Content
            return $path
        }

        $validBug = Get-Content -LiteralPath (Join-Path $fixtureRoot 'issue-spec-bug-fix.md') -Raw
    }

    It 'accepts the bug-fix specimen' {
        $outcome = Invoke-Validator -IssuePath (Join-Path $fixtureRoot 'issue-spec-bug-fix.md')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
        @($outcome.Result.AcceptanceCriteria) | Should -HaveCount 3
    }

    It 'accepts the harness-change specimen' {
        $outcome = Invoke-Validator -IssuePath (Join-Path $fixtureRoot 'issue-spec-harness-change.md')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'accepts headings emitted by the GitHub issue form' {
        $formBody = $validBug -replace '(?m)^## ', '### '
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $formBody)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'accepts the submitted contract version field from the GitHub issue form' {
        $formBody = $validBug -replace '(?m)^Contract version:\s*1\.0\r?\n', "### Contract version`r`n`r`n1.0`r`n`r`n"
        $formBody = $formBody -replace '(?m)^## ', '### '
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $formBody)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'rejects required sections in the wrong order' {
        $reordered = $validBug -replace '(?m)^## Problem', '## Temporary problem'
        $reordered = $reordered -replace '(?m)^## Observable outcome', '## Problem'
        $reordered = $reordered -replace '(?m)^## Temporary problem', '## Observable outcome'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $reordered)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Required sections must appear in the contract order.'
    }

    It 'rejects a missing validation section' {
        $content = $validBug -replace '(?ms)^## Validation plan.*?(?=^## Risks and delivery boundary)', ''
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Validation plan'."
    }

    It 'rejects duplicate acceptance IDs' {
        $content = $validBug -replace '(- \[AC3\].*)', ('$1' + [Environment]::NewLine + '- [AC1] Repeat the first criterion.')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Duplicate acceptance criterion ID: 'AC1'."
    }

    It 'rejects unresolved blocking decisions' {
        $content = $validBug -replace '(The parser project and its L0 test project are available in the current solution\.)', ('$1' + [Environment]::NewLine + 'TBD (blocking): choose an unapproved deployment target.')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Unresolved blocking TBD/TODO marker is not allowed.'
    }

    It 'rejects a missing repository-relative path' {
        $content = $validBug -replace '`README.md`', '`missing/not-found.cs`'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Referenced repository-relative path does not exist: 'missing/not-found.cs'."
    }

    It 'treats commands and external instructions as data' {
        $sentinel = Join-Path $TestDrive 'must-remain.txt'
        Set-Content -LiteralPath $sentinel -Value 'untouched'
        $outcome = Invoke-Validator -IssuePath (Join-Path $fixtureRoot 'issue-spec-malicious-text.md')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
        Test-Path -LiteralPath $sentinel | Should -BeTrue
    }

    It 'keeps the contract version and required fields aligned with the issue form' {
        $contract = Get-Content -LiteralPath (Join-Path $repoRoot '.github/agent-guidance/issue-contract.md') -Raw
        $form = Get-Content -LiteralPath (Join-Path $repoRoot '.github/ISSUE_TEMPLATE/agent-task.yml') -Raw
        $requiredLabels = @(
            'Problem',
            'Observable outcome',
            'Scope',
            'Relevant source and contracts',
            'Decisions and non-goals',
            'Dependencies and readiness',
            'Acceptance criteria',
            'Implementation outline',
            'Validation plan',
            'Risks and delivery boundary',
            'Validation evidence map'
        )

        $contract | Should -Match 'Contract version: 1\.0'
        $form | Should -Match 'Contract version: 1\.0'
        $form | Should -Match 'id: contract-version'
        $form | Should -Match 'value: 1\.0'
        foreach ($label in $requiredLabels) {
            $contract | Should -Match ([regex]::Escape("- ``## $label``"))
            $form | Should -Match ([regex]::Escape("label: $label"))
        }
    }

    It 'retains repository paths and validation detail in the issue refiner' {
        $refiner = Get-Content -LiteralPath (Join-Path $repoRoot '.github/agents/issue-refiner.agent.md') -Raw

        $refiner | Should -Match 'repository-relative paths'
        $refiner | Should -Match 'exact validation commands'
        $refiner | Should -Match '1\. `## Problem`'
        $refiner | Should -Match '11\. `## Validation evidence map`'
        $refiner | Should -Not -Match '1\. `## Summary`'
        $refiner | Should -Not -Match '^-\s+implementation step-by-step instructions'
        $refiner | Should -Not -Match '^-\s+build/test command lists'
    }
}
