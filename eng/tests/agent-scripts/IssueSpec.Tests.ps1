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

    It 'does not treat a heading split across lines as a canonical section' {
        $splitHeading = $validBug -replace '(?m)^## Problem', ('##' + [Environment]::NewLine + 'Problem')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $splitHeading)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'rejects mixed canonical heading levels' {
        $mixed = $validBug -replace '(?m)^## Observable outcome', '### Observable outcome'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $mixed)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Required sections must use one consistent Markdown heading level.'
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

    It 'stops required sections at unknown peer headings' {
        $content = $validBug -replace '(?ms)^## Acceptance criteria.*?(?=^## Implementation outline)', "## Acceptance criteria`r`n## Appendix`r`n- [AC1] This belongs to the appendix.`r`n`r`n## Implementation outline"
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Required section '## Acceptance criteria' is empty."
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

    It 'ignores headings inside fenced Markdown blocks' {
        $fencedOnly = '```md' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '```'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $fencedOnly)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'preserves inline HTML-comment tokens as code' {
        $content = $validBug -replace 'Keep the existing exception type\.', 'Keep the existing exception type. The parser token is `<!--`.'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'counts a fenced command as rendered section content' {
        $backtick = [char]96
        $fence = [string]::new($backtick, 3)
        $replacement = '## Validation plan' + [Environment]::NewLine + $fence + 'powershell' + [Environment]::NewLine + 'dotnet test tests/Example/Example.csproj' + [Environment]::NewLine + $fence + [Environment]::NewLine + [Environment]::NewLine
        $content = [regex]::Replace($validBug, '(?ms)^## Validation plan.*?(?=^## Risks and delivery boundary)', $replacement)
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'does not close a bare fence on its opening line' {
        $bareFence = '```' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '```'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $bareFence)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'requires an exact maximal inline-code delimiter run' {
        $backtick = [char]96
        $hidden = [string]::new($backtick, 2) + '<!--' + [Environment]::NewLine + $validBug + [Environment]::NewLine + [string]::new($backtick, 3)
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $hidden)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'tracks the fence character while ignoring structural examples' {
        $mixedFences = '```md' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '~~~' + [Environment]::NewLine + '## Still code' + [Environment]::NewLine + '```'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $mixedFences)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'does not treat a four-space-indented fence as a Markdown fence' {
        $indentedFence = '    ```md' + [Environment]::NewLine + $validBug
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $indentedFence)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores contract content inside HTML comments' {
        $commentedOnly = '<!--' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '-->'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $commentedOnly)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'ignores placeholders inside fenced Markdown examples' {
        $content = $validBug + [Environment]::NewLine + '```text' + [Environment]::NewLine + '{{name}}' + [Environment]::NewLine + '```'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores repository paths inside fenced source examples' {
        $content = $validBug -replace '(?m)^## Decisions and non-goals', ('```text' + [Environment]::NewLine + '`missing/not-found.cs` — example only.' + [Environment]::NewLine + '```' + [Environment]::NewLine + [Environment]::NewLine + '## Decisions and non-goals')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores acceptance and evidence entries inside indented code' {
        $content = $validBug -replace '(?m)^- \[AC1\]', ('    - [AC99] Code example only.' + [Environment]::NewLine + '- [AC1]')
        $content = $content -replace '(?m)^- \[AC1\] Test:', ('    - [AC99] Test: code example; expected: ignored.' + [Environment]::NewLine + '- [AC1] Test:')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'ignores headings inside raw HTML blocks' {
        $compact = $validBug -replace '\r?\n\r?\n', [Environment]::NewLine
        $content = '<div>' + [Environment]::NewLine + $compact + [Environment]::NewLine + '</div>'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'masks compact custom raw HTML blocks through the terminating blank line' {
        $compact = $validBug -replace '\r?\n\r?\n', [Environment]::NewLine
        $content = '<x-task>' + [Environment]::NewLine + $compact + [Environment]::NewLine + '</x-task>'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'caps total repeated source references before explanation processing' {
        $references = ((1..513 | ForEach-Object { '- `README.md` — repeated reference.' }) -join [Environment]::NewLine)
        $content = $validBug -replace '- `README.md` — public validation and test entry points\.', $references
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Relevant source and contracts may contain at most 512 total path references.'
    }

    It 'does not enter a fence for a backtick in its info string' {
        $invalidFenceInfo = '```text`' + [Environment]::NewLine + $validBug
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $invalidFenceInfo)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'resumes after token-terminated raw HTML declarations' {
        $content = $validBug -replace 'The input parser accepts an empty identifier', ('<?target?>' + [Environment]::NewLine + 'The input parser accepts an empty identifier')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'resumes after multiline token-terminated raw HTML declarations' {
        $tokens = '<?target' + [Environment]::NewLine + '?>' + [Environment]::NewLine + '<![CDATA[' + [Environment]::NewLine + ']]>' + [Environment]::NewLine + '<!DECL' + [Environment]::NewLine + '>' + [Environment]::NewLine
        $content = $validBug -replace 'The input parser accepts an empty identifier', ($tokens + 'The input parser accepts an empty identifier')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'does not classify lowercase declaration text as a raw HTML declaration' {
        $content = '<!doctype html>' + [Environment]::NewLine + [Environment]::NewLine + $validBug
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'does not let inline code spans cross raw HTML blocks' {
        $backtick = [char]96
        $content = [string]$backtick + [Environment]::NewLine + '<script>' + [Environment]::NewLine + 'ignored' + [Environment]::NewLine + '</script>' + [Environment]::NewLine + '<!--' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '-->' + [Environment]::NewLine + [string]$backtick
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Missing required section '## Problem'."
    }

    It 'ignores raw token-looking lines inside an HTML comment' {
        $comment = '<!--' + [Environment]::NewLine + '<?target' + [Environment]::NewLine + '?>' + [Environment]::NewLine + '-->' + [Environment]::NewLine
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content ($comment + $validBug))

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'does not classify lowercase cdata text as a raw token' {
        $content = '<![cdata[' + [Environment]::NewLine + [Environment]::NewLine + $validBug
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'does not let fenced token examples mask visible contract headings' {
        $fencedToken = '```html' + [Environment]::NewLine + '<?target' + [Environment]::NewLine + $validBug + [Environment]::NewLine + '```' + [Environment]::NewLine
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content ($fencedToken + $validBug))

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'treats a raw HTML-only required section as empty' {
        $content = $validBug -replace '(?ms)(?<=^## Problem\r?\n).*?(?=^## Observable outcome)', ('<script>' + [Environment]::NewLine + 'hidden' + [Environment]::NewLine + '</script>' + [Environment]::NewLine)
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Required section '## Problem' is empty."
    }

    It 'treats an empty fenced required section as empty' {
        $emptyFence = '```text' + [Environment]::NewLine + '   ' + [Environment]::NewLine + '```' + [Environment]::NewLine
        $content = $validBug -replace '(?ms)(?<=^## Problem\r?\n).*?(?=^## Observable outcome)', $emptyFence
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Required section '## Problem' is empty."
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
        $content = $validBug + [Environment]::NewLine + '- [AC1] Test: duplicate mapping; expected: duplicate.' + [Environment]::NewLine + '- [AC99] Test: unknown mapping; expected: unknown.'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Duplicate validation evidence mapping ID: 'AC1'."
        $outcome.Result.Errors | Should -Contain "Validation evidence map contains unknown acceptance criterion ID: 'AC99'."
    }

    It 'rejects evidence mappings without a kind and expected result' {
        $content = $validBug -replace '- \[AC1\] Test: parser invalid-input test; expected: validation error and no storage call\.', '- [AC1] pending'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Validation evidence entry for 'AC1' must include Command, Test, or Manual observation evidence and an expected result."
    }

    It 'rejects evidence mappings with whitespace-only values' {
        $content = $validBug -replace '- \[AC1\] Test: parser invalid-input test; expected: validation error and no storage call\.', '- [AC1] Test:    ; expected:    '
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Validation evidence entry for 'AC1' must include nonempty evidence and expected result."
    }

    It 'rejects required form sections that contain only their prefilled heading' {
        $content = $validBug -replace '(?ms)^## Decisions and non-goals.*?(?=^## Acceptance criteria)', "## Decisions and non-goals`r`n### Dependencies and readiness`r`n`r`n## Acceptance criteria"
        $content = $content -replace '(?ms)^## Risks and delivery boundary.*?(?=^## Validation evidence map)', "## Risks and delivery boundary`r`n`r`n## Validation evidence map"
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Required section '## Decisions and non-goals' is empty."
        $outcome.Result.Errors | Should -Contain "Required section '## Risks and delivery boundary' is empty."
    }

    It 'accepts semicolons inside command evidence' {
        $content = $validBug -replace 'focused test command; expected:', "pwsh -Command 'Build; Test'; expected:"
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
    }

    It 'requires the contract version before the first required section' {
        $content = $validBug -replace '(?m)^Contract version: 1\.0\r?\n', [string]::Empty
        $content = $content -replace '(?m)^## Validation plan', ('## Validation plan' + [Environment]::NewLine + 'Contract version: 1.0')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Missing Contract version: major.minor.'
    }

    It 'rejects duplicate contract version declarations' {
        $content = $validBug + [Environment]::NewLine + 'Contract version: 2.0'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Contract version is declared more than once with conflicting values.'
    }

    It 'rejects unresolved blocking decisions' {
        $content = $validBug -replace '(The parser project and its L0 test project are available in the current solution\.)', ('$1' + [Environment]::NewLine + 'TBD (blocking): choose an unapproved deployment target.')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain 'Unresolved blocking TBD/TODO marker is not allowed.'
    }

    It 'rejects colon-form blocking markers in either order' {
        $todoFirst = $validBug -replace '(The parser project and its L0 test project are available in the current solution\.)', ('$1' + [Environment]::NewLine + 'TODO: blocking - choose an unapproved deployment target.')
        $blockingFirst = $validBug -replace '(The parser project and its L0 test project are available in the current solution\.)', ('$1' + [Environment]::NewLine + 'BLOCKING: TODO - choose an unapproved deployment target.')

        $todoOutcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $todoFirst)
        $blockingOutcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $blockingFirst)

        $todoOutcome.Result.Errors | Should -Contain 'Unresolved blocking TBD/TODO marker is not allowed.'
        $blockingOutcome.Result.Errors | Should -Contain 'Unresolved blocking TBD/TODO marker is not allowed.'
    }

    It 'rejects a missing repository-relative path' {
        $content = $validBug -replace '`README.md`', '`missing/not-found.cs`'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Referenced repository-relative path does not exist: 'missing/not-found.cs'."
    }

    It 'requires an explanation for every referenced source path' {
        $content = $validBug -replace '`README.md` — public validation and test entry points\.', '`README.md`'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Referenced repository-relative path must include an explanation: 'README.md'."
    }

    It 'ignores blocker-looking text inside inline code' {
        $backtick = [char]96
        $inlineMarker = [string]::new($backtick, 1) + 'TODO: blocking' + [string]::new($backtick, 1)
        $content = $validBug -replace 'Keep the existing exception type.', ('Keep the existing exception type. The literal syntax ' + $inlineMarker + ' is data.')
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'allows an explicit no-blockers readiness statement' {
        $content = $validBug -replace 'Keep the existing exception type\.', 'Keep the existing exception type. There are no blocking TODOs.'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'rejects ordered source entries that contain only a path' {
        $backtick = [char]96
        $original = '- ' + $backtick + 'README.md' + $backtick + ' — public validation and test entry points.'
        $replacement = '1. ' + $backtick + 'README.md' + $backtick
        $content = $validBug.Replace($original, $replacement)
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Errors | Should -Contain "Referenced repository-relative path must include an explanation: 'README.md'."
    }

    It 'accepts existing repository directories as source boundaries' {
        $content = $validBug -replace '`README.md`', '`eng/src/agent-scripts`'
        $outcome = Invoke-Validator -IssuePath (New-TemporaryIssue -Content $content)

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Valid | Should -BeTrue
    }

    It 'accepts a normalized current-directory path segment' {
        $content = $validBug -replace '`README.md`', '`./README.md`'
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

    It 'validates prose-form source paths instead of ignoring them' {
        $content = $validBug -replace '(?m)^## Decisions and non-goals', "Also update ``missing/not-found.cs`` for the contract.`r`n`r`n## Decisions and non-goals"
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
        $workingDirectory = Join-Path $TestDrive 'malicious-working-directory'
        New-Item -ItemType Directory -Path $workingDirectory -Force | Out-Null
        $sentinel = Join-Path $workingDirectory 'must-remain.txt'
        Set-Content -LiteralPath $sentinel -Value 'untouched'
        try {
            $outcome = Invoke-Validator -IssuePath (Join-Path $fixtureRoot 'issue-spec-malicious-text.md') -WorkingDirectory $workingDirectory

            $outcome.ExitCode | Should -Be 0
            $outcome.Result.Valid | Should -BeTrue
            Test-Path -LiteralPath $sentinel | Should -BeTrue
        }
        finally {
            Remove-Item -LiteralPath $workingDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
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
        $form | Should -Match 'value:\s*\|\s*\r?\n\s*### Dependencies and readiness'
        $form | Should -Match 'value:\s*\|\s*\r?\n\s*### Validation evidence map'
        [regex]::Matches($form, '(?m)^\s*- type: ').Count | Should -BeLessOrEqual 10
        foreach ($description in [regex]::Matches($form, '(?m)^\s+description:\s*(?<Text>.+)$')) {
            $description.Groups['Text'].Value.Length | Should -BeLessOrEqual 200
        }
    }

    It 'retains repository paths and validation detail in the issue refiner' {
        $refiner = Get-Content -LiteralPath (Join-Path $repoRoot '.github/agents/issue-refiner.agent.md') -Raw

        $refiner | Should -Match 'repository-relative paths'
        $refiner | Should -Match 'exact validation commands'
        $refiner | Should -Match 'Contract version: 1\.0'
        $refiner | Should -Match '1\. `## Problem`'
        $refiner | Should -Match '11\. `## Validation evidence map`'
        $refiner | Should -Match 'test-issue-spec\.ps1.*ISSUE-BODY\.md'
        $refiner | Should -Match 'exit code `0` and JSON `Valid: true`'
        $refiner | Should -Match 'Do not update or overwrite the GitHub issue while validation is failing'
        $refiner | Should -Not -Match '1\. `## Summary`'
        $refiner | Should -Not -Match '^-\s+implementation step-by-step instructions'
        $refiner | Should -Not -Match '^-\s+build/test command lists'
    }
}
