#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Deterministic validation plan' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/get-validation-plan.ps1'
        $fixtureRoot = Join-Path $TestDrive 'validation-repository'
        New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

        function Invoke-Plan {
            param([string[]]$Paths, [string[]]$RiskHints = @(), [AllowEmptyString()][string]$Base = 'base-sha', [AllowEmptyString()][string]$Head = 'head-sha')
            $arguments = @('-NoProfile', '-File', $scriptPath, '-RepositoryRoot', $repoRoot, '-BaseRevision', $Base, '-HeadRevision', $Head, '-OutputFormat', 'Json')
            if (@($Paths).Count -gt 0) {
                $changedJson = ConvertTo-Json -InputObject ([string[]]@($Paths)) -Compress
                $arguments += @('-ChangedPathJson', $changedJson)
            }
            if (@($RiskHints).Count -gt 0) {
                $riskJson = ConvertTo-Json -InputObject ([string[]]@($RiskHints)) -Compress
                $arguments += @('-RiskHintJson', $riskJson)
            }
            $json = & $powerShellPath @arguments 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $json | ConvertFrom-Json; Output = $json }
        }
    }

    It 'selects the PowerShell gate for PowerShell changes' {
        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/RepositoryAutomation.psm1')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.SelectedChecks.Id | Should -Contain 'powershell-tests'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'core-final'
    }

    It 'selects Markdown lint for docs and keeps command arrays structured' {
        $outcome = Invoke-Plan -Paths @('docs/Docusaurus/docs/guide.md')

        $outcome.ExitCode | Should -Be 0
        $markdown = @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint')[0]
        $markdown.Arguments | Should -Contain 'docs/Docusaurus/docs/guide.md'
        $markdown.Executable | Should -Be 'npx'
        $markdown.Arguments | Should -Contain 'markdownlint-cli2@0.17.2'
    }

    It 'selects the Docusaurus gate for Docusaurus changes' {
        $outcome = Invoke-Plan -Paths @('docs/Docusaurus/docs/guide.md')

        $outcome.Result.SelectedChecks.Id | Should -Contain 'docusaurus-final'
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'docusaurus-final').Arguments | Should -Contain './docs/Docusaurus/test-docusaurus.ps1'
    }

    It 'does not report a browser gap when the Docusaurus gate covers CSS' {
        $outcome = Invoke-Plan -Paths @('docs/Docusaurus/src/pages/index.module.css')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.SelectedChecks.Id | Should -Contain 'docusaurus-final'
        $outcome.Result.Unresolved | Should -Not -Match 'No application-specific browser validation gate'
    }

    It 'selects Spring doctor and smoke for browser-facing changes' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Client/Pages/Index.razor')

        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-smoke'
    }

    It 'selects full Spring L2 and L3 suites for every Spring change' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Domain/Account.cs')

        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-l2-full'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-l3-full'
    }

    It 'does not route non-Spring browser changes to Spring validation' {
        $outcome = Invoke-Plan -Paths @('samples/LightSpeed/LightSpeed.Client/Pages/Index.razor')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-smoke'
        $outcome.Result.Unresolved | Should -Match 'No application-specific browser validation gate'
    }

    It 'treats non-Spring HTML and JavaScript assets as browser changes' {
        $outcome = Invoke-Plan -Paths @('samples/LightSpeed/LightSpeed.Client/wwwroot/index.html', 'src/Reservoir.Client/wwwroot/mississippi.reservoir.devtools.js')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-smoke'
        $outcome.Result.Unresolved | Should -Match 'No application-specific browser validation gate'
    }

    It 'selects broad checks conservatively for unknown paths' {
        $outcome = Invoke-Plan -Paths @('new-tool/generated.surface')

        $outcome.ExitCode | Should -Be 0
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'core-iteration').Reasons | Should -Match 'Unknown mapping'
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'full-cleanup'
    }

    It 'retains unknown handling when mapped and unmapped paths are mixed' {
        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/RepositoryAutomation.psm1', 'new-tool/generated.surface')

        $outcome.ExitCode | Should -Be 0
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'core-iteration').Reasons | Should -Match 'new-tool/generated.surface'
    }

    It 'selects PowerShell tests for catalog-only changes' {
        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/validation-command-catalog.json')

        $outcome.Result.SelectedChecks.Id | Should -Contain 'powershell-tests'
    }

    It 'covers directly orchestrated PowerShell runner paths' {
        $outcome = Invoke-Plan -Paths @('eng/tests/agent-scripts/run-spring-validation-tests.ps1')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Unresolved | Should -Not -Match 'outside the maintained parser/test gate'
    }

    It 'reports an explicit gap for arbitrary PowerShell tooling paths' {
        $outcome = Invoke-Plan -Paths @('tools/foo.ps1')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Contain 'powershell-tests'
        $outcome.Result.Unresolved | Should -Match 'outside the maintained parser/test gate'
    }

    It 'recognizes planner files as covered PowerShell paths' {
        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/get-validation-plan.ps1', 'eng/tests/agent-scripts/ValidationPlan.Tests.ps1')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Unresolved | Should -Not -Match 'outside the maintained parser/test gate'
    }

    It 'recognizes the PowerShell harness suite as a covered path' {
        $outcome = Invoke-Plan -Paths @('eng/tests/agent-scripts/PowerShellTestHarness.Tests.ps1')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Unresolved | Should -Not -Match 'outside the maintained parser/test gate'
    }

    It 'selects Markdown lint for Markdown-lint configuration changes' {
        $outcome = Invoke-Plan -Paths @('.markdownlintignore')

        $outcome.ExitCode | Should -Be 0
        $markdown = @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint')[0]
        $markdown.Arguments | Should -Contain '.'
    }

    It 'uses repository-wide Markdown lint for mixed config and document changes' {
        $outcome = Invoke-Plan -Paths @('.markdownlintignore', 'docs/guide.md')

        $markdown = @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint')[0]
        $markdown.Arguments | Should -Contain '.'
        $markdown.Arguments | Should -Not -Contain 'docs/guide.md'
    }

    It 'derives the repository root from the planner script when omitted' {
        Push-Location -LiteralPath (Join-Path $repoRoot 'eng')
        try {
            $json = & $powerShellPath -NoProfile -File $scriptPath -BaseRevision base-sha -HeadRevision head-sha -ChangedPath README.md -OutputFormat Json 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }

        $exitCode | Should -Be 0
        ($json | ConvertFrom-Json).RepositoryRoot | Should -Be ([System.IO.Path]::GetFullPath($repoRoot))
    }

    It 'applies supported risk hints to check selection' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Client/Pages/Index.razor') -RiskHints @('browser', 'infrastructure')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-smoke'
        ((@($outcome.Result.SelectedChecks | Where-Object Id -EQ 'spring-doctor')[0].Reasons) -join "`n") | Should -Match "Risk hint 'browser'"
    }

    It 'does not map a generic browser risk hint to unrelated Spring' {
        $outcome = Invoke-Plan -Paths @('src/Reservoir/State.cs') -RiskHints @('browser')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-smoke'
        $outcome.Result.Unresolved | Should -Match 'requires an application-specific browser context'
    }

    It 'fails closed for browser risk across mixed Spring and non-Spring applications' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Client/Pages/Index.razor', 'src/Reservoir/State.cs') -RiskHints @('browser')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'ambiguous across Spring and non-Spring application paths'
    }

    It 'does not treat repository documentation as another browser application' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Client/Pages/Index.razor', 'README.md') -RiskHints @('browser')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Unresolved | Should -Not -Match 'ambiguous across Spring'
    }

    It 'fails closed for infrastructure risk across mixed applications' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Domain/Account.cs', 'src/Reservoir/State.cs') -RiskHints @('infrastructure')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'ambiguous across Spring and non-Spring'
    }

    It 'rejects case-only application path matches on case-sensitive hosts' {
        if ($IsWindows) {
            Set-ItResult -Skipped -Because 'Case-only path distinctions are not applicable on Windows.'
            return
        }
        $outcome = Invoke-Plan -Paths @('samples/spring/config.json')

        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'core-iteration').Reasons | Should -Match 'Unknown mapping'
    }

    It 'emits fast iteration before Spring prerequisites and final gates' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Domain/Account.cs')

        $outcome.Result.SelectedChecks[0].Id | Should -Be 'core-iteration'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-doctor'
    }

    It 'treats Razor code-behind as browser-facing' {
        $outcome = Invoke-Plan -Paths @('src/Refraction.Client/Components/Molecules/CommandOrbit.razor.cs')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'No application-specific browser validation gate'
    }

    It 'treats client-project C# as browser-facing' {
        $outcome = Invoke-Plan -Paths @('src/Reservoir.Client/BuiltIn/Navigation/Effects/NavigationEffect.cs')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'No application-specific browser validation gate'
    }

    It 'does not map generic infrastructure risk to an unrelated L2 gate' {
        $outcome = Invoke-Plan -Paths @('src/Reservoir/State.cs') -RiskHints @('infrastructure')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-doctor'
        $outcome.Result.Unresolved | Should -Match 'requires an application-specific L2 gate'
    }

    It 'fails closed for unsupported risk hints' {
        $outcome = Invoke-Plan -Paths @('README.txt') -RiskHints @('unbounded-risk')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Contain "Unsupported risk hint 'unbounded-risk'."
    }

    It 'uses a repository-wide Markdown target for documentation risk without Markdown paths' {
        $outcome = Invoke-Plan -Paths @('src/Reservoir/State.cs') -RiskHints @('documentation')

        $outcome.ExitCode | Should -Be 0
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint').Arguments | Should -Contain '.'
    }

    It 'normalizes renamed or deleted paths without executing commands' {
        $sentinel = Join-Path $TestDrive 'must-remain.txt'
        Set-Content -LiteralPath $sentinel -Value 'unchanged'
        $outcome = Invoke-Plan -Paths @('old/removed.ps1', 'eng/tests/orchestrate-powershell-tests.ps1')

        $outcome.ExitCode | Should -Be 1
        (Get-Content -LiteralPath $sentinel -Raw).Trim() | Should -Be 'unchanged'
        $outcome.Result.ChangedPaths | Should -Contain 'old/removed.ps1'
    }

    It 'preserves commas in literal changed paths' {
        $outcome = Invoke-Plan -Paths @('docs/api,legacy.md')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.ChangedPaths | Should -Contain 'docs/api,legacy.md'
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint').Arguments | Should -Contain 'docs/api,legacy.md'
    }

    It 'uses a literal-safe repository target for Markdown glob characters' {
        $outcome = Invoke-Plan -Paths @('docs/guide[1].md')

        $outcome.ExitCode | Should -Be 0
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'markdown-lint').Arguments | Should -Contain '.'
    }

    It 'returns structured output for an empty changed path entry' {
        $outcome = Invoke-Plan -Paths @('')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Contain 'Changed path is empty or invalid.'
    }

    It 'rejects non-array changed-path JSON' {
        $json = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $repoRoot -BaseRevision base-sha -HeadRevision head-sha -ChangedPathJson '123' -OutputFormat Json 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
        $result = $json | ConvertFrom-Json

        $exitCode | Should -Be 1
        ($result.Unresolved -join [Environment]::NewLine) | Should -Match 'ChangedPathJson must be a JSON array of strings'
    }

    It 'keeps workflow changes incomplete without a workflow-specific gate' {
        $outcome = Invoke-Plan -Paths @('.github/workflows/l2-tests.yml')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'Workflow paths have no catalog-specific validation gate'
    }

    It 'preserves case-sensitive PowerShell allowlists on Unix' {
        if ($IsWindows) {
            Set-ItResult -Skipped -Because 'Case-only path distinctions are not applicable on Windows.'
            return
        }

        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/repositoryautomation.psm1')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Match 'outside the maintained parser/test gate'
    }

    It 'rejects a rooted cross-volume path on Windows' {
        if (-not $IsWindows) {
            Set-ItResult -Skipped -Because 'Cross-volume rooted paths are Windows-specific.'
            return
        }

        $currentDrive = ([System.IO.Path]::GetPathRoot($repoRoot)).Substring(0, 1).ToUpperInvariant()
        $otherDrive = if ($currentDrive -eq 'Z') { 'Y' } else { 'Z' }
        $outcome = Invoke-Plan -Paths @("${otherDrive}:\outside.md")

        $outcome.ExitCode | Should -Be 1
        ($outcome.Result.Unresolved -join "`n") | Should -Match 'outside the repository or invalid'
    }

    It 'quotes spaced arguments in text output' {
        $json = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $repoRoot -BaseRevision base-sha -HeadRevision head-sha -ChangedPathJson '["docs/My Guide.md"]' -OutputFormat Text 2>&1 | Out-String

        $json | Should -Match "'docs/My Guide.md'"
    }

    It 'fails closed when base, head, or changed paths are omitted' {
        $outcome = Invoke-Plan -Paths @() -Base ' ' -Head ' '

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Complete | Should -BeFalse
        $outcome.Result.Unresolved | Should -Contain 'BaseRevision is required; no default base is assumed.'
        $outcome.Result.Unresolved | Should -Contain 'HeadRevision is required; no default head is assumed.'
    }

    It 'keeps catalog script references valid' {
        $catalog = Get-Content -LiteralPath (Join-Path $repoRoot 'eng/src/agent-scripts/validation-command-catalog.json') -Raw | ConvertFrom-Json
        foreach ($check in @($catalog.checks)) {
            foreach ($scriptPath in @($check.scriptPaths)) {
                if ($scriptPath) { Test-Path -LiteralPath (Join-Path $repoRoot $scriptPath) | Should -BeTrue }
            }
        }
    }
}
