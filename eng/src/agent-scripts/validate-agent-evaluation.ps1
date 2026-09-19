#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$ScenarioPath = (Join-Path $PSScriptRoot '../../../.github/agent-guidance/issue-delivery-benchmark.json'),
    [string]$ResultsPath = (Join-Path $PSScriptRoot '../../../.github/agent-guidance/issue-delivery-results.json'),
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $pack = Get-Content -LiteralPath $ScenarioPath -Raw | ConvertFrom-Json
    $results = Get-Content -LiteralPath $ResultsPath -Raw | ConvertFrom-Json
    $errors = [System.Collections.Generic.List[string]]::new()
    if ([string]$pack.schemaVersion -ne '1.0' -or [string]$results.schemaVersion -ne '1.0') { $errors.Add('Scenario and result schemas must both be version 1.0.') }
    if ([string]$pack.packId -ne [string]$results.packId) { $errors.Add('Results refer to a different scenario pack.') }
    if ([string]::IsNullOrWhiteSpace([string]$results.sourceRevision)) { $errors.Add('Results must identify the source revision.') }
    if ([string]$results.sourceRevision -ne 'record-at-evaluation-run' -and [string]$results.sourceRevision -notmatch '^[0-9a-fA-F]{7,64}$') { $errors.Add('Results sourceRevision must be an immutable commit identifier or the initial-baseline sentinel.') }
    if ([string]$results.mode -ne 'initial-baseline' -and [string]$results.sourceRevision -eq 'record-at-evaluation-run') { $errors.Add('Live results cannot use the initial-baseline sourceRevision sentinel.') }
    $categories = @($pack.categories | Where-Object { $_.kind -eq 'normal' })
    $expectedTrials = [int]$pack.normalTrialCount
    $requiredCategoryIds = @('csharp-behavior', 'powershell-harness', 'documentation', 'browser-visible', 'multi-project-generator')
    $actualCategoryIds = @($categories.id)
    if (@($requiredCategoryIds | Where-Object { $actualCategoryIds -notcontains $_ }).Count -gt 0 -or
        @($actualCategoryIds | Where-Object { $requiredCategoryIds -notcontains $_ }).Count -gt 0) {
        $errors.Add('Scenario pack must contain exactly the required benchmark categories.')
    }
    $pairedCases = @($pack.pairedCases)
    if ($pairedCases.Count -ne $expectedTrials) { $errors.Add("Scenario pack must define exactly $expectedTrials paired cases.") }
    foreach ($category in $categories) {
        if ($null -eq $category.PSObject.Properties['pairedInputIds'] -or @($category.pairedInputIds).Count -ne $expectedTrials) {
            $errors.Add("Category '$($category.id)' must define one paired input for each trial.")
        }
    }
    $hostNames = @('Codex', 'Copilot')
    $primaryRows = @($results.hosts | Where-Object { $_.host -in $hostNames })
    if ($primaryRows.Count -ne $hostNames.Count) { $errors.Add('Results must contain exactly one row for each primary host.') }
    if ([string]$results.mode -eq 'initial-baseline' -and @($primaryRows | Where-Object { [string]$_.activeModel -notin @('unsupported', 'blocked', 'unknown') }).Count -gt 0) {
        $errors.Add('Live host results cannot retain initial-baseline mode.')
    }
    foreach ($hostName in $hostNames) {
        $hostResult = @($results.hosts | Where-Object host -EQ $hostName)[0]
        if ($null -eq $hostResult) { $errors.Add("Missing primary host result: $hostName."); continue }
        foreach ($property in @('configuredModel', 'acceptedModel', 'activeModel')) {
            if ($null -eq $hostResult.PSObject.Properties[$property] -or [string]::IsNullOrWhiteSpace([string]$hostResult.$property)) { $errors.Add("Host '$hostName' is missing $property evidence.") }
        }
            $activeSentinel = [string]$hostResult.activeModel -in @('unsupported', 'blocked', 'unknown')
            if (-not $activeSentinel -and [string]$hostResult.acceptedModel -in @('unknown', 'unsupported', 'blocked')) {
                $errors.Add("Host '$hostName' has live results without verifiable accepted and active model evidence.")
            }
        foreach ($category in $categories) {
            $summary = @($hostResult.trialSummaries | Where-Object scenarioId -EQ $category.id)[0]
            if ($null -eq $summary) { $errors.Add("Host '$hostName' is missing trial summary '$($category.id)'."); continue }
            $attempted = [int]$summary.attempted
            $unsupported = [int]$summary.unsupported
            $blocked = if ($null -eq $summary.PSObject.Properties['blocked']) { 0 } else { [int]$summary.blocked }
            foreach ($countName in @('attempted', 'passed', 'failed', 'unsupported', 'blocked', 'falseCompletion', 'authorityViolations')) {
                if ($null -eq $summary.PSObject.Properties[$countName]) { $errors.Add("Host '$hostName' category '$($category.id)' is missing $countName.") ; continue }
                if ($summary.$countName -isnot [byte] -and $summary.$countName -isnot [int16] -and $summary.$countName -isnot [int32] -and $summary.$countName -isnot [int64]) { $errors.Add("Host '$hostName' category '$($category.id)' has a non-integer $countName.") }
            }
            if ([string]$hostResult.activeModel -in @('unsupported', 'blocked', 'unknown') -and ($attempted -ne 0 -or [int]$summary.passed -ne 0 -or [int]$summary.failed -ne 0)) {
                $errors.Add("Host '$hostName' category '$($category.id)' has attempted or passed sentinel-host trials.")
            }
            foreach ($countName in @('attempted', 'passed', 'failed', 'unsupported', 'blocked', 'falseCompletion', 'authorityViolations')) {
                if ($null -ne $summary.PSObject.Properties[$countName] -and [int]$summary.$countName -lt 0) { $errors.Add("Host '$hostName' category '$($category.id)' has a negative $countName count.") }
            }
            if (($attempted + $unsupported + $blocked) -ne $expectedTrials) { $errors.Add("Host '$hostName' category '$($category.id)' does not account for exactly $expectedTrials trials.") }
            if ([int]$summary.passed + [int]$summary.failed -ne $attempted) { $errors.Add("Host '$hostName' category '$($category.id)' does not reconcile attempted, passed, and failed counts.") }
            if ([int]$summary.passed -gt $attempted) { $errors.Add("Host '$hostName' category '$($category.id)' counts unsupported trials as passes.") }
            if ([string]$hostResult.activeModel -notin @('unsupported', 'blocked', 'unknown')) {
                $records = @($hostResult.trialRecords | Where-Object scenarioId -EQ $category.id)
                if ($records.Count -ne $expectedTrials) { $errors.Add("Host '$hostName' category '$($category.id)' requires one evidence record per trial.") }
                $recordInputIds = [System.Collections.Generic.List[string]]::new()
                $recordPassed = 0
                $recordFailed = 0
                $recordBlocked = 0
                $recordUnsupported = 0
                foreach ($record in $records) {
                    foreach ($field in @('pairedInputId', 'outcome', 'acceptancePassed', 'independentChecks', 'reviewRework', 'interventions')) {
                        if ($null -eq $record.PSObject.Properties[$field]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing $field.") }
                    }
                    if ($record.PSObject.Properties['pairedInputId']) { $recordInputIds.Add([string]$record.pairedInputId) }
                    $outcome = [string]$record.outcome
                    if ($outcome -eq 'passed') { $recordPassed++ } elseif ($outcome -eq 'failed') { $recordFailed++ } elseif ($outcome -eq 'blocked') { $recordBlocked++ } elseif ($outcome -eq 'unsupported') { $recordUnsupported++ } else { $errors.Add("Host '$hostName' category '$($category.id)' has an invalid trial outcome.") }
                    if ($record.acceptancePassed -isnot [bool]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has a non-boolean acceptancePassed value.") }
                    if ($record.independentChecks -isnot [array] -or @($record.independentChecks).Count -eq 0) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has no independent checks.") }
                    if ($null -eq $record.reviewRework -or $null -eq $record.interventions) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has incomplete review/intervention evidence.") }
                    if ($category.id -eq 'browser-visible' -and ($outcome -eq 'passed' -or $outcome -eq 'failed')) {
                        if ($null -eq $record.PSObject.Properties['browserEvidence']) {
                            $errors.Add("Host '$hostName' browser trial is missing browserEvidence.")
                        }
                        else {
                            foreach ($field in @('route', 'state', 'viewport', 'command', 'screenshot')) {
                                if ($null -eq $record.browserEvidence.PSObject.Properties[$field] -or [string]::IsNullOrWhiteSpace([string]$record.browserEvidence.$field)) { $errors.Add("Host '$hostName' browser trial is missing browserEvidence.$field.") }
                            }
                        }
                    }
                }
                $pairedIds = @($category.pairedInputIds)
                if ((@($recordInputIds | Sort-Object -Unique) -join '|') -ne (@($pairedIds | Sort-Object -Unique) -join '|')) { $errors.Add("Host '$hostName' category '$($category.id)' trial records do not cover the required paired inputs.") }
                if ($recordPassed -ne [int]$summary.passed -or $recordFailed -ne [int]$summary.failed -or $recordBlocked -ne [int]$summary.blocked -or $recordUnsupported -ne [int]$summary.unsupported) {
                    $errors.Add("Host '$hostName' category '$($category.id)' trial records do not reconcile with aggregate outcomes.")
                }
                }
        }
    }
    foreach ($category in $categories) {
        $contract = @($results.deterministicContractTrials | Where-Object scenarioId -EQ $category.id)[0]
        if ($null -eq $contract) { $errors.Add("Missing deterministic contract trials for '$($category.id)'."); continue }
        foreach ($countName in @('trials', 'passed', 'failed', 'falseCompletion', 'authorityViolations')) {
            if ([int]$contract.$countName -lt 0) { $errors.Add("Deterministic trials for '$($category.id)' have a negative $countName count.") }
        }
        if ([int]$contract.trials -ne $expectedTrials -or [int]$contract.passed + [int]$contract.failed -ne $expectedTrials) { $errors.Add("Deterministic trials for '$($category.id)' have inconsistent denominators.") }
        if ($null -eq $contract.PSObject.Properties['evidenceChecks'] -or @($contract.evidenceChecks).Count -eq 0) { $errors.Add("Deterministic trials for '$($category.id)' do not identify executed evidence checks.") }
    }
    $primaryHosts = @($results.hosts | Where-Object { $_.host -in $hostNames })
    $liveUnsupported = $primaryHosts.Count -eq $hostNames.Count -and @($primaryHosts | Where-Object { [string]$_.activeModel -in @('unsupported', 'blocked', 'unknown') }).Count -eq $hostNames.Count
    if (@($primaryHosts | Where-Object { [string]$_.activeModel -in @('unsupported', 'blocked', 'unknown') }).Count -gt 0 -and -not $liveUnsupported) {
        $errors.Add('Primary host results must either all provide verifiable active models or all be classified as unsupported.')
    }
    $report = [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        PackId = [string]$pack.packId
        Status = if ($errors.Count -gt 0) { 'INVALID' } elseif ($liveUnsupported) { 'VALIDATED_UNSUPPORTED_BASELINE' } else { 'VALIDATED' }
        NormalTrialCount = $expectedTrials
        Hosts = @($hostNames)
        Categories = @($categories.id)
        Errors = @($errors)
        Limitations = @($results.limitations)
    }
    if ($Json) { $report | ConvertTo-Json -Depth 8 -Compress } else { $report | Format-List }
    if ($errors.Count -gt 0) { exit 1 }
    exit 0
}
catch {
    if ($Json) {
        [pscustomobject][ordered]@{
            SchemaVersion = '1.0'
            PackId = ''
            Status = 'INVALID'
            NormalTrialCount = 0
            Hosts = @()
            Categories = @()
            Errors = @("Evaluation validation failed: $($_.Exception.Message)")
            Limitations = @()
        } | ConvertTo-Json -Depth 8 -Compress
    }
    else { Write-Error "Evaluation validation failed: $($_.Exception.Message)" }
    exit 1
}
