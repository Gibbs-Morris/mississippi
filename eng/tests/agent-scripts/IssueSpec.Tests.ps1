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
            param(
                [Parameter(Mandatory)][string]$IssuePath,
                [string]$WorkingDirectory
            )

            $pushed = $false
            try {
                if ($WorkingDirectory) {
                    Push-Location -LiteralPath $WorkingDirectory
                    $pushed = $true
                }
                $output = & $powerShellPath -NoProfile -File $validator -Path $IssuePath -RepositoryRoot $repoRoot -Json 2>&1 | Out-String
                $exitCode = $LASTEXITCODE
            }
            finally {
                if ($pushed) { Pop-Location }
            }
            [pscustomobject]@{
                ExitCode = $exitCode
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

    It 'preserves nested headings inside required sections' {
        $nestedBody = $validBug -replace '(?m)^## Problem', "## Problem`r`n### Current behavior`r`nThe current parser accepts empty input."
        $nestedBody = $nestedBody -replace '(?m)^## Acceptance criteria', "## Acceptance criteria`r`n### Observable cases"
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $nestedBody)

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

    It 'rejects duplicate canonical section headings' {
        $content = $validBug + [Environment]::NewLine + '## Problem' + [Environment]::NewLine + 'Conflicting duplicate.'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Duplicate required section heading: '## Problem'."
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

    It 'rejects duplicate and unknown validation evidence IDs' {
        $content = $validBug + [Environment]::NewLine + '- [AC1] Duplicate mapping.' + [Environment]::NewLine + '- [AC99] Unknown mapping.'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Duplicate validation evidence mapping ID: 'AC1'."
        $outcome.Result.Errors | Should -Contain "Validation evidence map contains unknown acceptance criterion ID: 'AC99'."
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

    It 'accepts existing repository directories as source boundaries' {
        $content = $validBug -replace '`README.md`', '`eng/src/agent-scripts`'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'validates ordered source-path entries instead of ignoring them' {
        $content = $validBug -replace '(?m)^## Decisions and non-goals', "1. ``missing/not-found.cs`` — an ordered source entry.`r`n`r`n## Decisions and non-goals"
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Referenced repository-relative path does not exist: 'missing/not-found.cs'."
    }

    It 'accepts ordered acceptance and evidence lists' {
        $content = $validBug -replace '(?m)^- \[AC', '1. [AC'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'treats commands and external instructions as data' {
        $sentinel = Join-Path $fixtureRoot 'must-remain.txt'
        Set-Content -LiteralPath $sentinel -Value 'untouched'
        $outcome = Invoke-Validator -IssuePath (Join-Path $fixtureRoot 'issue-spec-malicious-text.md') -WorkingDirectory $fixtureRoot

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
        $form | Should -Match 'label: Contract version'
        $form | Should -Match 'id: contract-version'
        $form | Should -Match 'value:\s*["'']?1\.0["'']?'
        foreach ($label in $requiredLabels) {
            $contract | Should -Match ([regex]::Escape("- ``## $label``"))
        }
        foreach ($label in @('Problem', 'Observable outcome', 'Scope', 'Relevant source and contracts', 'Decisions and non-goals', 'Acceptance criteria', 'Implementation outline', 'Validation plan', 'Risks and delivery boundary')) {
            $form | Should -Match ([regex]::Escape("label: $label"))
        }
        $form | Should -Match '## Dependencies and readiness'
        $form | Should -Match '## Validation evidence map'
        [regex]::Matches($form, '(?m)^\s*- type: ').Count | Should -BeLessOrEqual 10
    }

    It 'retains repository paths and validation detail in the issue refiner' {
        $refiner = Get-Content -LiteralPath (Join-Path $repoRoot '.github/agents/issue-refiner.agent.md') -Raw

        $refiner | Should -Match 'repository-relative paths'
        $refiner | Should -Match 'exact validation commands'
        $refiner | Should -Match 'Contract version: 1\.0'
        $refiner | Should -Match '1\. `## Problem`'
        $refiner | Should -Match '11\. `## Validation evidence map`'
        $refiner | Should -Not -Match '1\. `## Summary`'
        $refiner | Should -Not -Match '^-\s+implementation step-by-step instructions'
        $refiner | Should -Not -Match '^-\s+build/test command lists'
    }
}
