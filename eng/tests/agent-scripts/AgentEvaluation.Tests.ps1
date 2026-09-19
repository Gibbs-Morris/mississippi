#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Issue delivery benchmark validation' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $validator = Join-Path $repoRoot 'eng/src/agent-scripts/validate-agent-evaluation.ps1'
        $pack = Join-Path $repoRoot '.github/agent-guidance/issue-delivery-benchmark.json'
        $results = Join-Path $repoRoot '.github/agent-guidance/issue-delivery-results.json'
        function Invoke-Evaluation {
            param([string]$ResultsPath = $results)
            $output = & $powerShellPath -NoProfile -File $validator -ScenarioPath $pack -ResultsPath $ResultsPath -Json 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = $output | ConvertFrom-Json }
        }
    }

    It 'validates the published unsupported baseline without counting it as pass' {
        $outcome = Invoke-Evaluation

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'VALIDATED_UNSUPPORTED_BASELINE'
        $outcome.Result.Errors | Should -HaveCount 0
    }

    It 'rejects a host category with fewer than three accounted trials' {
        $data = Get-Content -LiteralPath $results -Raw | ConvertFrom-Json
        $data.hosts[0].trialSummaries[0].unsupported = 2
        $path = Join-Path $TestDrive 'invalid-results.json'
        $data | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        $outcome.Result.Status | Should -Be 'INVALID'
    }

    It 'rejects unsupported trials counted as passes' {
        $data = Get-Content -LiteralPath $results -Raw | ConvertFrom-Json
        $data.hosts[0].trialSummaries[0].passed = 1
        $path = Join-Path $TestDrive 'pass-invalid-results.json'
        $data | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'counts unsupported trials as passes'
    }
}
