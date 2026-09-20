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
        function New-LiveResults {
            $data = Get-Content -LiteralPath $results -Raw | ConvertFrom-Json
            $scenario = Get-Content -LiteralPath $pack -Raw | ConvertFrom-Json
            $revision = (& git -c "safe.directory=$($repoRoot.Replace('\', '/'))" -C $repoRoot rev-parse HEAD | Select-Object -First 1).Trim()
            $data.sourceRevision = $revision
            $data.mode = 'authorized-live'
            $inputDigest = 'SHA256:' + (('a' * 64) -join '')
            foreach ($hostRow in @($data.hosts)) {
                $hostRow.configuredModel = 'test-configured-model'
                $hostRow.acceptedModel = 'test-accepted-model'
                $hostRow.activeModel = 'test-active-model'
                $hostRow | Add-Member -NotePropertyName hostVersion -NotePropertyValue 'test-host-1' -Force
                $hostRow | Add-Member -NotePropertyName configuredEffort -NotePropertyValue 'medium' -Force
                $hostRow | Add-Member -NotePropertyName acceptedEffort -NotePropertyValue 'medium' -Force
                $hostRow | Add-Member -NotePropertyName activeEffort -NotePropertyValue 'medium' -Force
                $trialRecords = [System.Collections.Generic.List[object]]::new()
                foreach ($category in @($scenario.categories | Where-Object kind -EQ 'normal')) {
                    $summary = @($hostRow.trialSummaries | Where-Object scenarioId -EQ $category.id)[0]
                    $summary.attempted = 3
                    $summary.passed = 2
                    $summary.failed = 1
                    $summary.unsupported = 0
                    $summary.blocked = 0
                    for ($index = 0; $index -lt 3; $index++) {
                        $outcome = if ($index -eq 2) { 'failed' } else { 'passed' }
                        $record = [ordered]@{
                            scenarioId = [string]$category.id
                            pairedInputId = [string]$category.pairedInputIds[$index]
                            inputEvidence = [ordered]@{ repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; bodyDigest = $inputDigest; sourceRevision = $revision }
                            outcome = $outcome
                            acceptancePassed = $outcome -eq 'passed'
                            reason = if ($outcome -eq 'failed') { 'controlled live trial failure' } else { $null }
                            falseCompletion = 0
                            authorityViolations = 0
                            freshContext = $true
                            contextId = "$($hostRow.host)-$($category.id)-normal-$index"
                            repositoryRevision = $revision
                            repositoryState = 'clean'
                            worktreeId = "$($hostRow.host)-$($category.id)-normal-worktree-$index"
                            independentChecks = @($category.independentChecks)
                            independentCheckResults = @($category.independentChecks | ForEach-Object { [pscustomobject]@{ id = [string]$_; status = 'passed' } })
                            reviewRework = 0
                            interventions = 0
                        }
                        if ($category.id -eq 'browser-visible') {
                            $record.browserEvidence = [ordered]@{ route = '/'; state = 'ready'; viewport = '1280x720'; command = 'playwright test'; screenshot = 'artifacts/browser.png' }
                        }
                        $trialRecords.Add([pscustomobject]$record)
                    }
                    foreach ($failureCase in @($category.failureCases)) {
                        $trialRecords.Add([pscustomobject][ordered]@{
                            scenarioId = [string]$category.id
                            pairedInputId = [string]$category.pairedInputIds[0]
                            inputEvidence = [ordered]@{ repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; bodyDigest = $inputDigest; sourceRevision = $revision }
                            failureCase = [string]$failureCase
                            outcome = 'blocked'
                            acceptancePassed = $false
                            reason = 'controlled failure-case trial'
                            falseCompletion = 0
                            authorityViolations = 0
                            freshContext = $true
                            contextId = "$($hostRow.host)-$($category.id)-failure-$failureCase"
                            repositoryRevision = $revision
                            repositoryState = 'clean'
                            worktreeId = "$($hostRow.host)-$($category.id)-failure-worktree-$failureCase"
                            independentChecks = @($category.independentChecks)
                            independentCheckResults = @($category.independentChecks | ForEach-Object { [pscustomobject]@{ id = [string]$_; status = 'blocked' } })
                            reviewRework = 0
                            interventions = 1
                        })
                    }
                }
                $hostRow | Add-Member -NotePropertyName trialRecords -NotePropertyValue @($trialRecords) -Force
            }
            return $data
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

    It 'validates live hosts, keeps failure cases outside the denominator, and reconciles acceptance' {
        $data = New-LiveResults
        $path = Join-Path $TestDrive 'live-results.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $valid = Invoke-Evaluation -ResultsPath $path

        $valid.ExitCode | Should -Be 0
        $valid.Result.Status | Should -Be 'VALIDATED'
        $data.hosts[0].trialRecords[0].acceptancePassed = $false
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path
        $invalid = Invoke-Evaluation -ResultsPath $path

        $invalid.ExitCode | Should -Not -Be 0
        ($invalid.Result.Errors -join "`n") | Should -Match 'acceptancePassed does not reconcile'
    }

    It 'rejects nonnumeric live review and intervention metrics' {
        $data = New-LiveResults
        $data.hosts[0].trialRecords[0].reviewRework = 'unknown'
        $path = Join-Path $TestDrive 'invalid-live-metrics.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'invalid nonnegative reviewRework metric'
    }
}
