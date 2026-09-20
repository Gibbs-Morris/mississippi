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
            param([string]$ResultsPath = $results, [string]$ScenarioPath = $pack)
            $output = & $powerShellPath -NoProfile -File $validator -ScenarioPath $ScenarioPath -ResultsPath $ResultsPath -Json 2>&1 | Out-String
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
                        $pairedCase = @($scenario.pairedCases | Where-Object id -EQ $category.pairedInputIds[$index])[0]
                        $record = [ordered]@{
                            scenarioId = [string]$category.id
                            pairedInputId = [string]$category.pairedInputIds[$index]
                            inputEvidence = [ordered]@{ repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; inputProfile = [string]$pairedCase.inputProfile; bodyDigest = $inputDigest; sourceRevision = $revision }
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
                        $definition = @($category.failureCaseDefinitions | Where-Object id -EQ $failureCase)[0]
                        $pairedCase = @($scenario.pairedCases | Where-Object id -EQ $category.pairedInputIds[0])[0]
                        $definitionText = "$($definition.id)|$($definition.setup)|$($definition.trigger)|$($definition.expectedObservation)"
                        $definitionHash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($definitionText))
                        $trialRecords.Add([pscustomobject][ordered]@{
                            scenarioId = [string]$category.id
                            pairedInputId = [string]$category.pairedInputIds[0]
                            inputEvidence = [ordered]@{ repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; inputProfile = [string]$pairedCase.inputProfile; bodyDigest = $inputDigest; sourceRevision = $revision }
                            failureCase = [string]$failureCase
                            failureDefinitionId = [string]$failureCase
                            failureDefinitionDigest = 'SHA256:' + (($definitionHash | ForEach-Object { $_.ToString('x2') }) -join '')
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
            foreach ($contract in @($data.deterministicContractTrials)) {
                $contract | Add-Member -NotePropertyName evidenceStatus -NotePropertyValue 'PASS' -Force
                $contract | Add-Member -NotePropertyName evidenceRevision -NotePropertyValue $revision -Force
                $contract | Add-Member -NotePropertyName evidenceArtifacts -NotePropertyValue @($contract.evidenceChecks) -Force
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

    It 'rejects noncanonical category kinds' {
        $scenario = Get-Content -LiteralPath $pack -Raw | ConvertFrom-Json
        $scenario.categories = @($scenario.categories + [pscustomobject]@{ id = 'injected'; kind = 'experimental' })
        $scenarioPath = Join-Path $TestDrive 'noncanonical-category-pack.json'
        $scenario | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $scenarioPath

        $outcome = Invoke-Evaluation -ScenarioPath $scenarioPath

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'only normal benchmark categories'
    }

    It 'rejects extra result hosts' {
        $data = New-LiveResults
        $data.hosts = @($data.hosts + [pscustomobject]@{ host = 'UntrustedHost' })
        $path = Join-Path $TestDrive 'extra-host-results.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'no extra hosts'
    }

    It 'requires scalar-string context and worktree identities' {
        $data = New-LiveResults
        $normal = $data.hosts[0].trialRecords[0]
        $normal.contextId = 42
        $failure = @($data.hosts[0].trialRecords | Where-Object { $null -ne $_.PSObject.Properties['failureCase'] })[0]
        $failure.worktreeId = $false
        $path = Join-Path $TestDrive 'non-scalar-identities.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'trial evidence contextId must be a scalar string'
        ($outcome.Result.Errors -join "`n") | Should -Match 'failure-case evidence worktreeId must be a scalar string'
    }

    It 'binds trial evidence to the paired input profile' {
        $data = New-LiveResults
        $data.hosts[0].trialRecords[0].inputEvidence.inputProfile = 'wrong-profile'
        $path = Join-Path $TestDrive 'wrong-input-profile.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'inputProfile does not match the paired case'
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

    It 'rejects trial records for scenarios outside the canonical category set' {
        $data = New-LiveResults
        $data.hosts[0].trialRecords[0].scenarioId = 'unlisted-scenario'
        $path = Join-Path $TestDrive 'unknown-scenario-results.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match "unknown scenario 'unlisted-scenario'"
    }

    It 'validates optional measurements on failure-case records' {
        $data = New-LiveResults
        $failure = @($data.hosts[0].trialRecords | Where-Object { $null -ne $_.PSObject.Properties['failureCase'] })[0]
        $failure | Add-Member -NotePropertyName elapsedMilliseconds -NotePropertyValue -1 -Force
        $path = Join-Path $TestDrive 'invalid-failure-measurement-results.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'failure-case evidence has an invalid nonnegative elapsedMilliseconds measurement'
    }

    It 'rejects contradictory measurement aliases' {
        $data = New-LiveResults
        $data.hosts[0].trialRecords[0] | Add-Member -NotePropertyName tokenCount -NotePropertyValue 10 -Force
        $data.hosts[0].trialRecords[0] | Add-Member -NotePropertyName tokens -NotePropertyValue 100 -Force
        $data.hosts[0].trialRecords[0] | Add-Member -NotePropertyName measurementProvenance -NotePropertyValue direct -Force
        $path = Join-Path $TestDrive 'contradictory-measurements.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'contradictory measurement aliases'
    }

    It 'requires browser evidence for browser failure-case trials' {
        $data = New-LiveResults
        $failure = @($data.hosts[0].trialRecords | Where-Object { $_.scenarioId -eq 'browser-visible' -and $null -ne $_.PSObject.Properties['failureCase'] })[0]
        $failure.outcome = 'failed'
        $failure.acceptancePassed = $false
        $failure.reason = 'controlled browser failure'
        $path = Join-Path $TestDrive 'missing-browser-failure-evidence.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'browser failure-case trial is missing browserEvidence'
    }

    It 'rejects duplicate independent-check results' {
        $data = New-LiveResults
        $normal = $data.hosts[0].trialRecords[0]
        $normal.independentCheckResults = @($normal.independentCheckResults + $normal.independentCheckResults[0])
        $path = Join-Path $TestDrive 'duplicate-independent-checks.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'duplicate independent-check results'
    }

    It 'binds the same paired input identically across categories' {
        $data = New-LiveResults
        $documentationRecord = @($data.hosts[0].trialRecords | Where-Object scenarioId -EQ 'documentation')[0]
        $documentationRecord.inputEvidence.bodyDigest = 'SHA256:' + ('b' * 64)
        $path = Join-Path $TestDrive 'cross-category-input-mismatch.json'
        $data | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path

        $outcome = Invoke-Evaluation -ResultsPath $path

        $outcome.ExitCode | Should -Not -Be 0
        ($outcome.Result.Errors -join "`n") | Should -Match 'paired input.*differs'
    }
}
