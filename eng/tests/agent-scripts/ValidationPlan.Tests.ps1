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
            if (@($Paths).Count -gt 0) { $arguments += @('-ChangedPath', (@($Paths) -join ',')) }
            if (@($RiskHints).Count -gt 0) { $arguments += @('-RiskHint', (@($RiskHints) -join ',')) }
            $json = & $powerShellPath @arguments 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $json | ConvertFrom-Json; Output = $json }
        }
    }

    It 'selects the PowerShell gate for PowerShell changes' {
        $outcome = Invoke-Plan -Paths @('eng/src/agent-scripts/example.ps1')

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
    }

    It 'selects Spring doctor and smoke for browser-facing changes' {
        $outcome = Invoke-Plan -Paths @('samples/Spring/Spring.Client/Pages/Index.razor')

        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-smoke'
    }

    It 'does not route non-Spring browser changes to Spring validation' {
        $outcome = Invoke-Plan -Paths @('samples/LightSpeed/LightSpeed.Client/Pages/Index.razor')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Not -Contain 'spring-smoke'
        $outcome.Result.Unresolved | Should -Match 'No application-specific browser validation gate'
    }

    It 'selects broad checks conservatively for unknown paths' {
        $outcome = Invoke-Plan -Paths @('new-tool/generated.surface')

        $outcome.ExitCode | Should -Be 0
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'core-iteration').Reasons | Should -Match 'Unknown mapping'
    }

    It 'applies supported risk hints to check selection' {
        $outcome = Invoke-Plan -Paths @('README.txt') -RiskHints @('browser', 'infrastructure')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-doctor'
        $outcome.Result.SelectedChecks.Id | Should -Contain 'spring-smoke'
        @($outcome.Result.SelectedChecks | Where-Object Id -EQ 'spring-doctor').Reasons | Should -Match "Risk hint 'browser'"
    }

    It 'fails closed for unsupported risk hints' {
        $outcome = Invoke-Plan -Paths @('README.txt') -RiskHints @('unbounded-risk')

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Unresolved | Should -Contain "Unsupported risk hint 'unbounded-risk'."
    }

    It 'normalizes renamed or deleted paths without executing commands' {
        $sentinel = Join-Path $TestDrive 'must-remain.txt'
        Set-Content -LiteralPath $sentinel -Value 'unchanged'
        $outcome = Invoke-Plan -Paths @('old/removed.ps1', 'eng/tests/orchestrate-powershell-tests.ps1')

        $outcome.ExitCode | Should -Be 0
        (Get-Content -LiteralPath $sentinel -Raw).Trim() | Should -Be 'unchanged'
        $outcome.Result.ChangedPaths | Should -Contain 'old/removed.ps1'
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
