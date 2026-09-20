#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$ScenarioPath = (Join-Path $PSScriptRoot '../../../.github/agent-guidance/issue-delivery-benchmark.json'),
    [string]$ResultsPath = (Join-Path $PSScriptRoot '../../../.github/agent-guidance/issue-delivery-results.json'),
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-EvaluationInteger {
    param([AllowNull()][object]$Value)
    return $Value -is [byte] -or $Value -is [int16] -or $Value -is [int32] -or $Value -is [int64]
}

function Test-EvaluationNonnegativeNumber {
    param([AllowNull()][object]$Value)

    $numeric = $Value -is [sbyte] -or $Value -is [byte] -or $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or $Value -is [int64] -or $Value -is [uint64] -or
        $Value -is [single] -or $Value -is [double] -or $Value -is [decimal]
    if (-not $numeric) { return $false }
    $number = [double]$Value
    return -not [double]::IsNaN($number) -and -not [double]::IsInfinity($number) -and $number -ge 0
}

function Get-EvaluationSet {
    param([AllowEmptyCollection()][object[]]$Values)
    return @($Values | ForEach-Object { [string]$_ } | Sort-Object -Unique)
}

function Test-EvaluationSetEqual {
    param(
        [AllowEmptyCollection()][object[]]$Left,
        [AllowEmptyCollection()][object[]]$Right
    )
    return ((Get-EvaluationSet -Values $Left) -join '|') -eq ((Get-EvaluationSet -Values $Right) -join '|')
}

function Get-EvaluationInputEvidenceErrors {
    param(
        [Parameter(Mandatory)][object]$Record,
        [Parameter(Mandatory)][object]$Category,
        [Parameter(Mandatory)][object[]]$PairedCases,
        [Parameter(Mandatory)][string]$SourceRevision,
        [Parameter(Mandatory)][hashtable]$Observed
    )

    $messages = [System.Collections.Generic.List[string]]::new()
    $pairedInputProperty = $Record.PSObject.Properties['pairedInputId']
    if ($null -eq $pairedInputProperty -or [string]::IsNullOrWhiteSpace([string]$pairedInputProperty.Value)) {
        $null = $messages.Add("Category '$($Category.id)' trial evidence is missing pairedInputId.")
        return $messages.ToArray()
    }
    $pairedInputId = [string]$pairedInputProperty.Value
    $pairedCase = @($PairedCases | Where-Object { [string]$_.id -eq $pairedInputId })
    $inputEvidenceProperty = $Record.PSObject.Properties['inputEvidence']
    if ($pairedCase.Count -ne 1) { $null = $messages.Add("Category '$($Category.id)' trial evidence has an unknown pairedInputId '$pairedInputId'.") }
    if ($null -eq $inputEvidenceProperty) {
        $null = $messages.Add("Category '$($Category.id)' trial evidence is missing inputEvidence.")
        return $messages.ToArray()
    }
    $inputEvidence = $inputEvidenceProperty.Value
    foreach ($field in @('repository', 'issueNumber', 'bodyDigest', 'sourceRevision')) {
        if ($null -eq $inputEvidence.PSObject.Properties[$field]) { $null = $messages.Add("Category '$($Category.id)' inputEvidence is missing $field.") }
    }
    if ($pairedCase.Count -eq 1) {
        if ([string]$inputEvidence.repository -ne [string]$pairedCase[0].repository) { $null = $messages.Add("Category '$($Category.id)' inputEvidence repository does not match the paired case.") }
        if ([int]$inputEvidence.issueNumber -ne [int]$pairedCase[0].issueNumber) { $null = $messages.Add("Category '$($Category.id)' inputEvidence issue number does not match the paired case.") }
    }
    if ([string]$inputEvidence.bodyDigest -notmatch '^SHA256:[0-9a-fA-F]{64}$') { $null = $messages.Add("Category '$($Category.id)' inputEvidence bodyDigest is not an immutable SHA256 digest.") }
    if ([string]$inputEvidence.sourceRevision -ne $SourceRevision) { $null = $messages.Add("Category '$($Category.id)' inputEvidence uses a different source revision.") }
    $evidenceKey = "$($Category.id)|$pairedInputId"
    $evidenceValue = "$($inputEvidence.repository)|$($inputEvidence.issueNumber)|$($inputEvidence.bodyDigest)|$($inputEvidence.sourceRevision)"
    if ($Observed.ContainsKey($evidenceKey) -and [string]$Observed[$evidenceKey] -ne $evidenceValue) {
        $null = $messages.Add("Category '$($Category.id)' paired input '$pairedInputId' differs between host records.")
    }
    else { $Observed[$evidenceKey] = $evidenceValue }
    return $messages.ToArray()
}

try {
    $pack = Get-Content -LiteralPath $ScenarioPath -Raw | ConvertFrom-Json
    $results = Get-Content -LiteralPath $ResultsPath -Raw | ConvertFrom-Json
    $errors = [System.Collections.Generic.List[string]]::new()
    if ([string]$pack.schemaVersion -ne '1.0' -or [string]$results.schemaVersion -ne '1.0') { $errors.Add('Scenario and result schemas must both be version 1.0.') }
    if ([string]$pack.packId -ne [string]$results.packId) { $errors.Add('Results refer to a different scenario pack.') }
    if ([string]$results.mode -notin @('initial-baseline', 'authorized-live')) { $errors.Add("Results mode '$($results.mode)' is not supported.") }
    if ([string]::IsNullOrWhiteSpace([string]$results.sourceRevision)) { $errors.Add('Results must identify the source revision.') }
    if ([string]$results.sourceRevision -ne 'record-at-evaluation-run' -and [string]$results.sourceRevision -notmatch '^[0-9a-fA-F]{7,64}$') { $errors.Add('Results sourceRevision must be an immutable commit identifier or the initial-baseline sentinel.') }
    if ([string]$results.mode -ne 'initial-baseline' -and [string]$results.sourceRevision -eq 'record-at-evaluation-run') { $errors.Add('Live results cannot use the initial-baseline sourceRevision sentinel.') }
    if ([string]$results.mode -eq 'initial-baseline' -and [string]$results.sourceRevision -ne 'record-at-evaluation-run') { $errors.Add('Initial-baseline results must use the record-at-evaluation-run sourceRevision sentinel.') }
    if ([string]$results.sourceRevision -match '^[0-9a-fA-F]{7,64}$') {
        $repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
        $resolvedRevision = @(& git -c "safe.directory=$($repositoryRoot.Replace('\', '/'))" -C $repositoryRoot rev-parse --verify ("$($results.sourceRevision)^{commit}") 2>$null)
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]($resolvedRevision | Select-Object -First 1))) {
            $errors.Add("Results sourceRevision '$($results.sourceRevision)' does not resolve to a commit in the repository.")
        }
    }
    $categories = @($pack.categories | Where-Object { $_.kind -eq 'normal' })
    $expectedTrials = [int]$pack.normalTrialCount
    $requiredCategoryIds = @('csharp-behavior', 'powershell-harness', 'documentation', 'browser-visible', 'multi-project-generator')
    $actualCategoryIds = @($categories.id)
    if ($actualCategoryIds.Count -ne $requiredCategoryIds.Count -or
        (Get-EvaluationSet -Values $actualCategoryIds).Count -ne $actualCategoryIds.Count -or
        @($requiredCategoryIds | Where-Object { $actualCategoryIds -notcontains $_ }).Count -gt 0 -or
        @($actualCategoryIds | Where-Object { $requiredCategoryIds -notcontains $_ }).Count -gt 0) {
        $errors.Add('Scenario pack must contain exactly the required benchmark categories.')
    }
    $pairedCases = @($pack.pairedCases)
    $pairedCaseIds = @($pairedCases | ForEach-Object { [string]$_.id })
    if ($pairedCases.Count -ne $expectedTrials) { $errors.Add("Scenario pack must define exactly $expectedTrials paired cases.") }
    $requiredFailureCases = @{
        'csharp-behavior' = @('edited issue scope', 'stale validation', 'unavailable SDK')
        'powershell-harness' = @('running operation', 'timeout', 'missing Pester')
        'documentation' = @('missing evidence', 'unsupported tool', 'changed issue scope')
        'browser-visible' = @('browser unavailable', 'wrong application gate', 'stale source')
        'multi-project-generator' = @('partial project discovery', 'generated drift', 'running operation')
    }
    $requiredIndependentChecks = @{
        'csharp-behavior' = @('behavior regression', 'scope compliance', 'PR traceability')
        'powershell-harness' = @('Pester regression', 'exit-code contract', 'no unrelated mutation')
        'documentation' = @('Markdown/MDX lint', 'link validity', 'scope compliance')
        'browser-visible' = @('Playwright screenshot/evidence', 'browser behavior', 'PR traceability')
        'multi-project-generator' = @('all affected projects', 'generated output', 'source/test mapping')
    }
    $requiredEvidenceChecks = @{
        'csharp-behavior' = @('eng/tests/agent-scripts/AgentEvaluation.Tests.ps1', 'tests/Architecture.L0Tests/Architecture.L0Tests.csproj')
        'powershell-harness' = @('eng/tests/agent-scripts/AgentEvaluation.Tests.ps1', 'eng/tests/orchestrate-powershell-tests.ps1')
        'documentation' = @('eng/tests/agent-scripts/AgentEvaluation.Tests.ps1', 'docs/Docusaurus/test-docusaurus.ps1')
        'browser-visible' = @('eng/tests/agent-scripts/AgentEvaluation.Tests.ps1', 'test-spring.ps1')
        'multi-project-generator' = @('eng/tests/agent-scripts/AgentEvaluation.Tests.ps1', 'mississippi.slnx')
    }
    $requiredPairedCases = @(
        [pscustomobject]@{ id = 'issue-732-contract'; repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; inputProfile = 'parent-issue-contract' }
        [pscustomobject]@{ id = 'issue-732-review'; repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; inputProfile = 'parent-issue-review-and-ci' }
        [pscustomobject]@{ id = 'issue-732-delivery'; repository = 'Gibbs-Morris/mississippi'; issueNumber = 732; inputProfile = 'parent-issue-independent-prs' }
    )
    foreach ($requiredCase in $requiredPairedCases) {
        $actualCase = @($pairedCases | Where-Object id -EQ $requiredCase.id)
        if ($actualCase.Count -ne 1 -or [string]$actualCase[0].repository -ne $requiredCase.repository -or [int]$actualCase[0].issueNumber -ne $requiredCase.issueNumber -or [string]$actualCase[0].inputProfile -ne $requiredCase.inputProfile) {
            $errors.Add("Paired case '$($requiredCase.id)' does not match the canonical issue-732 contract.")
        }
    }
    foreach ($category in $categories) {
        if ($null -eq $category.PSObject.Properties['pairedInputIds'] -or @($category.pairedInputIds).Count -ne $expectedTrials) {
            $errors.Add("Category '$($category.id)' must define one paired input for each trial.")
        }
        elseif (-not (Test-EvaluationSetEqual -Left $category.pairedInputIds -Right $pairedCaseIds)) {
            $errors.Add("Category '$($category.id)' must cover exactly the declared paired cases.")
        }
        if ($null -eq $category.PSObject.Properties['failureCases'] -or @($category.failureCases).Count -eq 0) {
            $errors.Add("Category '$($category.id)' must retain a nonempty failure-case set.")
        }
        elseif ($requiredFailureCases.ContainsKey([string]$category.id) -and -not (Test-EvaluationSetEqual -Left $category.failureCases -Right $requiredFailureCases[[string]$category.id])) {
            $errors.Add("Category '$($category.id)' failure cases do not match the benchmark contract.")
        }
        if ($requiredIndependentChecks.ContainsKey([string]$category.id) -and -not (Test-EvaluationSetEqual -Left $category.independentChecks -Right $requiredIndependentChecks[[string]$category.id])) {
            $errors.Add("Category '$($category.id)' independent checks do not match the benchmark contract.")
        }
    }
    $hostNames = @('Codex', 'Copilot')
    $primaryRows = @($results.hosts | Where-Object { $_.host -in $hostNames })
    if ($primaryRows.Count -ne $hostNames.Count) { $errors.Add('Results must contain exactly one row for each primary host.') }
    if ([string]$results.mode -eq 'initial-baseline' -and @($primaryRows | Where-Object { [string]$_.activeModel -notin @('unsupported', 'blocked', 'unknown') }).Count -gt 0) {
        $errors.Add('Live host results cannot retain initial-baseline mode.')
    }
    $hostContextIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $hostWorktreeIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $observedInputEvidence = @{}
    $failureCaseSafety = [System.Collections.Generic.List[object]]::new()
    foreach ($hostName in $hostNames) {
        $hostResult = @($results.hosts | Where-Object host -EQ $hostName)[0]
        if ($null -eq $hostResult) { $errors.Add("Missing primary host result: $hostName."); continue }
        foreach ($property in @('configuredModel', 'acceptedModel', 'activeModel')) {
            $modelProperty = $hostResult.PSObject.Properties[$property]
            if ($null -eq $modelProperty -or $modelProperty.Value -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$modelProperty.Value)) { $errors.Add("Host '$hostName' is missing scalar string $property evidence.") }
        }
            $modelSentinels = @('unknown', 'unsupported', 'blocked')
            $activeSentinel = [string]$hostResult.activeModel -in $modelSentinels
            if (-not $activeSentinel) {
                if ([string]$hostResult.configuredModel -in $modelSentinels -or [string]$hostResult.acceptedModel -in $modelSentinels) {
                    $errors.Add("Host '$hostName' has live results without verifiable configured, accepted, and active model evidence.")
                }
                $hostVersionProperty = $hostResult.PSObject.Properties['hostVersion']
                if ($null -eq $hostVersionProperty -or $hostVersionProperty.Value -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$hostVersionProperty.Value)) {
                    $errors.Add("Host '$hostName' is missing scalar string hostVersion evidence.")
                }
                foreach ($effortProperty in @('configuredEffort', 'acceptedEffort', 'activeEffort')) {
                    if ($null -eq $hostResult.PSObject.Properties[$effortProperty] -or [string]::IsNullOrWhiteSpace([string]$hostResult.$effortProperty)) {
                        $errors.Add("Host '$hostName' is missing $effortProperty evidence.")
                    }
                    elseif ([string]$hostResult.$effortProperty -in $modelSentinels) {
                        $errors.Add("Host '$hostName' has unverifiable $effortProperty evidence.")
                    }
                }
            }
            elseif ($null -ne $hostResult.PSObject.Properties['trialRecords'] -and @($hostResult.trialRecords).Count -gt 0) {
                $errors.Add("Sentinel host '$hostName' must not contain trial records.")
            }
        $summaryCollection = if ($null -eq $hostResult.PSObject.Properties['trialSummaries']) { @() } else { @($hostResult.trialSummaries) }
        $summaryIds = @($summaryCollection | ForEach-Object { [string]$_.scenarioId })
        if ($summaryIds.Count -ne $actualCategoryIds.Count -or (Get-EvaluationSet -Values $summaryIds).Count -ne $summaryIds.Count -or -not (Test-EvaluationSetEqual -Left $summaryIds -Right $actualCategoryIds)) {
            $errors.Add("Host '$hostName' trial summary scenario IDs must exactly match the canonical category set.")
        }
        foreach ($category in $categories) {
            $summaryMatches = @($summaryCollection | Where-Object scenarioId -EQ $category.id)
            if ($summaryMatches.Count -ne 1) { $errors.Add("Host '$hostName' must contain exactly one trial summary '$($category.id)'.") }
            $summary = $summaryMatches | Select-Object -First 1
            if ($null -eq $summary) { $errors.Add("Host '$hostName' is missing trial summary '$($category.id)'."); continue }
            $attempted = [int]$summary.attempted
            $unsupported = [int]$summary.unsupported
            $blocked = if ($null -eq $summary.PSObject.Properties['blocked']) { 0 } else { [int]$summary.blocked }
            foreach ($countName in @('attempted', 'passed', 'failed', 'unsupported', 'blocked', 'falseCompletion', 'authorityViolations')) {
                if ($null -eq $summary.PSObject.Properties[$countName]) { $errors.Add("Host '$hostName' category '$($category.id)' is missing $countName.") ; continue }
                if (-not (Test-EvaluationInteger -Value $summary.$countName)) { $errors.Add("Host '$hostName' category '$($category.id)' has a non-integer $countName.") }
            }
            if ([string]$hostResult.activeModel -in @('unsupported', 'blocked', 'unknown') -and ($attempted -ne 0 -or [int]$summary.passed -ne 0 -or [int]$summary.failed -ne 0 -or [int]$summary.falseCompletion -ne 0 -or [int]$summary.authorityViolations -ne 0)) {
                $errors.Add("Host '$hostName' category '$($category.id)' has attributable trials or safety events on a sentinel host.")
            }
            foreach ($countName in @('attempted', 'passed', 'failed', 'unsupported', 'blocked', 'falseCompletion', 'authorityViolations')) {
                if ($null -ne $summary.PSObject.Properties[$countName] -and [int]$summary.$countName -lt 0) { $errors.Add("Host '$hostName' category '$($category.id)' has a negative $countName count.") }
            }
            if (($attempted + $unsupported + $blocked) -ne $expectedTrials) { $errors.Add("Host '$hostName' category '$($category.id)' does not account for exactly $expectedTrials trials.") }
            if ([int]$summary.passed + [int]$summary.failed -ne $attempted) { $errors.Add("Host '$hostName' category '$($category.id)' does not reconcile attempted, passed, and failed counts.") }
            if ([int]$summary.passed -gt $attempted) { $errors.Add("Host '$hostName' category '$($category.id)' counts unsupported trials as passes.") }
            if ([string]$hostResult.activeModel -notin @('unsupported', 'blocked', 'unknown')) {
                $allRecords = @($hostResult.trialRecords | Where-Object scenarioId -EQ $category.id)
                $records = @($allRecords | Where-Object {
                    $failureCase = $_.PSObject.Properties['failureCase']
                    $null -eq $failureCase -or [string]::IsNullOrWhiteSpace([string]$failureCase.Value)
                })
                $failureRecords = @($allRecords | Where-Object {
                    $failureCase = $_.PSObject.Properties['failureCase']
                    $null -ne $failureCase -and -not [string]::IsNullOrWhiteSpace([string]$failureCase.Value)
                })
                if ($records.Count -ne $expectedTrials) { $errors.Add("Host '$hostName' category '$($category.id)' requires one normal evidence record per trial.") }
                $recordInputIds = [System.Collections.Generic.List[string]]::new()
                $recordPassed = 0
                $recordFailed = 0
                $recordBlocked = 0
                $recordUnsupported = 0
                $recordFalseCompletion = 0
                $recordAuthorityViolations = 0
                foreach ($record in $records) {
                    foreach ($field in @('pairedInputId', 'outcome')) {
                        if ($null -eq $record.PSObject.Properties[$field]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing $field.") }
                    }
                    if ($record.PSObject.Properties['pairedInputId']) { $recordInputIds.Add([string]$record.pairedInputId) }
                    foreach ($inputError in @(Get-EvaluationInputEvidenceErrors -Record $record -Category $category -PairedCases $pairedCases -SourceRevision ([string]$results.sourceRevision) -Observed $observedInputEvidence)) { $errors.Add("Host '$hostName' $inputError") }
                    $recordFailureCase = $record.PSObject.Properties['failureCase']
                    if ($null -ne $recordFailureCase -and [string]$recordFailureCase.Value -notin @($category.failureCases)) { $errors.Add("Host '$hostName' category '$($category.id)' has an unknown failure case.") }
                    $outcome = [string]$record.outcome
                    if ($outcome -eq 'passed') { $recordPassed++ } elseif ($outcome -eq 'failed') { $recordFailed++ } elseif ($outcome -eq 'blocked') { $recordBlocked++ } elseif ($outcome -eq 'unsupported') { $recordUnsupported++ } else { $errors.Add("Host '$hostName' category '$($category.id)' has an invalid trial outcome.") }
                    foreach ($field in @('falseCompletion', 'authorityViolations')) {
                        if ($null -eq $record.PSObject.Properties[$field]) {
                            $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing $field.")
                        }
                        elseif (-not (Test-EvaluationInteger -Value $record.$field) -or [int]$record.$field -lt 0) {
                            $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has an invalid $field count.")
                        }
                    }
                    if ($null -ne $record.PSObject.Properties['falseCompletion']) { $recordFalseCompletion += [int]$record.falseCompletion }
                    if ($null -ne $record.PSObject.Properties['authorityViolations']) { $recordAuthorityViolations += [int]$record.authorityViolations }
                    foreach ($field in @('freshContext', 'contextId', 'repositoryRevision', 'repositoryState', 'worktreeId', 'independentChecks', 'independentCheckResults')) {
                        if ($null -eq $record.PSObject.Properties[$field]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing $field.") }
                    }
                    if ($record.PSObject.Properties['freshContext'] -and $record.freshContext -isnot [bool]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has a non-boolean freshContext value.") }
                    if ($record.PSObject.Properties['freshContext'] -and $record.freshContext -ne $true) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence was not run in a fresh context.") }
                    if ($record.PSObject.Properties['contextId'] -and [string]::IsNullOrWhiteSpace([string]$record.contextId)) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing a context identity.") }
                    elseif ($record.PSObject.Properties['contextId'] -and -not $hostContextIds.Add([string]$record.contextId)) { $errors.Add("Host '$hostName' category '$($category.id)' reuses a trial context.") }
                    if ($record.PSObject.Properties['repositoryRevision'] -and [string]$record.repositoryRevision -ne [string]$results.sourceRevision) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence uses a different repository revision.") }
                    if ($record.PSObject.Properties['repositoryState'] -and [string]$record.repositoryState -ne 'clean') { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence does not identify a clean repository state.") }
                    if ($record.PSObject.Properties['worktreeId'] -and [string]::IsNullOrWhiteSpace([string]$record.worktreeId)) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing an isolated worktree identity.") }
                    elseif ($record.PSObject.Properties['worktreeId'] -and -not $hostWorktreeIds.Add([string]$record.worktreeId)) { $errors.Add("Host '$hostName' category '$($category.id)' reuses an isolated worktree.") }
                    if ($record.PSObject.Properties['independentChecks'] -and -not (Test-EvaluationSetEqual -Left $record.independentChecks -Right $category.independentChecks)) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence does not identify every declared independent check.") }
                    if ($record.PSObject.Properties['independentCheckResults']) {
                        $checkResults = @($record.independentCheckResults)
                        $checkResultIds = @($checkResults | ForEach-Object { if ($null -ne $_.PSObject.Properties['id']) { [string]$_.id } })
                        if (-not (Test-EvaluationSetEqual -Left $checkResultIds -Right $category.independentChecks)) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence does not provide a result for every independent check.") }
                        foreach ($checkResult in $checkResults) {
                            if ($null -eq $checkResult.PSObject.Properties['status'] -or [string]$checkResult.status -notin @('passed', 'failed', 'blocked', 'unsupported')) { $errors.Add("Host '$hostName' category '$($category.id)' has an invalid independent-check result.") }
                        }
                        if ($outcome -eq 'passed' -and @($checkResults | Where-Object { [string]$_.status -ne 'passed' }).Count -gt 0) {
                            $errors.Add("Host '$hostName' category '$($category.id)' passed trial has a non-passing independent check.")
                        }
                    }
                    foreach ($measurementField in @('tokenCount', 'tokens', 'elapsedMilliseconds', 'elapsedSeconds', 'durationMilliseconds', 'durationSeconds')) {
                        $measurementProperty = $record.PSObject.Properties[$measurementField]
                        if ($null -ne $measurementProperty) {
                            $measurementValue = $measurementProperty.Value
                            if (-not (Test-EvaluationNonnegativeNumber -Value $measurementValue)) {
                                $errors.Add("Host '$hostName' category '$($category.id)' has an invalid nonnegative $measurementField measurement.")
                            }
                            $provenance = $record.PSObject.Properties['measurementProvenance']
                            if ($null -eq $provenance -or [string]$provenance.Value -ne 'direct') { $errors.Add("Host '$hostName' category '$($category.id)' measurement evidence must be directly measured.") }
                        }
                    }
                    if ($outcome -in @('passed', 'failed', 'blocked', 'unsupported')) {
                        foreach ($field in @('reviewRework', 'interventions')) {
                            $metric = $record.PSObject.Properties[$field]
                            if ($null -eq $metric) {
                                $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing $field.")
                            }
                            elseif (-not (Test-EvaluationNonnegativeNumber -Value $metric.Value)) {
                                $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has an invalid nonnegative $field metric.")
                            }
                        }
                    }
                    if ($outcome -in @('passed', 'failed')) {
                        if ($null -eq $record.PSObject.Properties['acceptancePassed']) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence is missing acceptancePassed.") }
                        elseif ($record.acceptancePassed -isnot [bool]) { $errors.Add("Host '$hostName' category '$($category.id)' trial evidence has a non-boolean acceptancePassed value.") }
                        elseif (($outcome -eq 'passed' -and -not $record.acceptancePassed) -or ($outcome -eq 'failed' -and $record.acceptancePassed)) { $errors.Add("Host '$hostName' category '$($category.id)' acceptancePassed does not reconcile with outcome '$outcome'.") }
                    }
                    if ($outcome -eq 'failed' -and [string]::IsNullOrWhiteSpace([string]$record.reason)) { $errors.Add("Host '$hostName' category '$($category.id)' failed trial is missing a reason.") }
                    if ($outcome -in @('blocked', 'unsupported') -and [string]::IsNullOrWhiteSpace([string]$record.reason)) { $errors.Add("Host '$hostName' category '$($category.id)' blocked or unsupported trial is missing a reason.") }
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
                if ($recordFalseCompletion -ne [int]$summary.falseCompletion -or $recordAuthorityViolations -ne [int]$summary.authorityViolations) {
                    $errors.Add("Host '$hostName' category '$($category.id)' trial safety counters do not reconcile with aggregate outcomes.")
                }
                $failureCaseFalseCompletion = 0
                $failureCaseAuthorityViolations = 0
                foreach ($failureRecord in $failureRecords) {
                    if ([string]$failureRecord.failureCase -notin @($category.failureCases)) {
                        $errors.Add("Host '$hostName' category '$($category.id)' has an unknown failure case.")
                    }
                    foreach ($inputError in @(Get-EvaluationInputEvidenceErrors -Record $failureRecord -Category $category -PairedCases $pairedCases -SourceRevision ([string]$results.sourceRevision) -Observed $observedInputEvidence)) { $errors.Add("Host '$hostName' $inputError") }
                    foreach ($field in @('outcome', 'reason', 'freshContext', 'contextId', 'repositoryRevision', 'repositoryState', 'worktreeId', 'independentChecks', 'independentCheckResults', 'falseCompletion', 'authorityViolations')) {
                        if ($null -eq $failureRecord.PSObject.Properties[$field]) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence is missing $field.") }
                    }
                    $failureOutcome = [string]$failureRecord.outcome
                    if ($failureOutcome -notin @('passed', 'failed', 'blocked', 'unsupported')) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has an invalid outcome.") }
                    if ($failureOutcome -in @('failed', 'blocked', 'unsupported') -and [string]::IsNullOrWhiteSpace([string]$failureRecord.reason)) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence is missing a reason.") }
                    if ($failureRecord.PSObject.Properties['freshContext'] -and $failureRecord.freshContext -isnot [bool]) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has a non-boolean freshContext value.") }
                    if ($failureRecord.PSObject.Properties['freshContext'] -and $failureRecord.freshContext -ne $true) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence was not run in a fresh context.") }
                    if ($failureRecord.PSObject.Properties['repositoryRevision'] -and [string]$failureRecord.repositoryRevision -ne [string]$results.sourceRevision) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence uses a different repository revision.") }
                    if ($failureRecord.PSObject.Properties['repositoryState'] -and [string]$failureRecord.repositoryState -ne 'clean') { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence does not identify a clean repository state.") }
                    if ($failureRecord.PSObject.Properties['independentChecks'] -and -not (Test-EvaluationSetEqual -Left $failureRecord.independentChecks -Right $category.independentChecks)) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence does not identify every declared independent check.") }
                    if ($failureRecord.PSObject.Properties['independentCheckResults']) {
                        $failureCheckResults = @($failureRecord.independentCheckResults)
                        $failureCheckIds = @($failureCheckResults | ForEach-Object { if ($null -ne $_.PSObject.Properties['id']) { [string]$_.id } })
                        if (-not (Test-EvaluationSetEqual -Left $failureCheckIds -Right $category.independentChecks)) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence does not provide a result for every independent check.") }
                        foreach ($failureCheck in $failureCheckResults) {
                            if ($null -eq $failureCheck.PSObject.Properties['status'] -or [string]$failureCheck.status -notin @('passed', 'failed', 'blocked', 'unsupported')) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has an invalid independent-check result.") }
                        }
                        if ($failureOutcome -eq 'passed' -and @($failureCheckResults | Where-Object { [string]$_.status -ne 'passed' }).Count -gt 0) {
                            $errors.Add("Host '$hostName' category '$($category.id)' passed failure-case evidence has a non-passing independent check.")
                        }
                    }
                    foreach ($safetyField in @('falseCompletion', 'authorityViolations')) {
                        $safetyProperty = $failureRecord.PSObject.Properties[$safetyField]
                        if ($null -eq $safetyProperty -or -not (Test-EvaluationInteger -Value $safetyProperty.Value) -or [int]$safetyProperty.Value -lt 0) {
                            $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has an invalid $safetyField count.")
                        }
                        elseif ($safetyField -eq 'falseCompletion') { $failureCaseFalseCompletion += [int]$safetyProperty.Value }
                        elseif ($safetyField -eq 'authorityViolations') { $failureCaseAuthorityViolations += [int]$safetyProperty.Value }
                    }
                    if ($failureOutcome -in @('passed', 'failed')) {
                        $acceptanceProperty = $failureRecord.PSObject.Properties['acceptancePassed']
                        if ($null -eq $acceptanceProperty -or $acceptanceProperty.Value -isnot [bool]) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has invalid acceptancePassed evidence.") }
                        elseif (($failureOutcome -eq 'passed' -and -not [bool]$acceptanceProperty.Value) -or ($failureOutcome -eq 'failed' -and [bool]$acceptanceProperty.Value)) { $errors.Add("Host '$hostName' category '$($category.id)' failure-case acceptancePassed does not reconcile with outcome '$failureOutcome'.") }
                    }
                    foreach ($identityField in @('contextId', 'worktreeId')) {
                        $identityProperty = $failureRecord.PSObject.Properties[$identityField]
                        if ($null -eq $identityProperty -or [string]::IsNullOrWhiteSpace([string]$identityProperty.Value)) {
                            $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence is missing $identityField.")
                        }
                        elseif ($identityField -eq 'contextId' -and -not $hostContextIds.Add([string]$identityProperty.Value)) {
                            $errors.Add("Host '$hostName' category '$($category.id)' reuses a trial context.")
                        }
                        elseif ($identityField -eq 'worktreeId' -and -not $hostWorktreeIds.Add([string]$identityProperty.Value)) {
                            $errors.Add("Host '$hostName' category '$($category.id)' reuses an isolated worktree.")
                        }
                    }
                    foreach ($metricField in @('reviewRework', 'interventions')) {
                        $metricProperty = $failureRecord.PSObject.Properties[$metricField]
                        if ($null -eq $metricProperty -or -not (Test-EvaluationNonnegativeNumber -Value $metricProperty.Value)) {
                            $errors.Add("Host '$hostName' category '$($category.id)' failure-case evidence has an invalid nonnegative $metricField metric.")
                        }
                    }
                }
                foreach ($failureCase in @($category.failureCases)) {
                    $matchingFailureRecords = @($failureRecords | Where-Object { [string]$_.failureCase -eq [string]$failureCase })
                    if ($matchingFailureRecords.Count -eq 0) {
                        $errors.Add("Host '$hostName' category '$($category.id)' is missing failure-case evidence '$failureCase'.")
                    }
                    elseif ($matchingFailureRecords.Count -gt 1) {
                        $errors.Add("Host '$hostName' category '$($category.id)' has duplicate failure-case evidence '$failureCase'.")
                    }
                }
                $failureCaseSafety.Add([pscustomobject]@{ Host = $hostName; Category = [string]$category.id; FalseCompletion = $failureCaseFalseCompletion; AuthorityViolations = $failureCaseAuthorityViolations })
                }
        }
    }
    foreach ($category in $categories) {
        $contract = @($results.deterministicContractTrials | Where-Object scenarioId -EQ $category.id)[0]
        if ($null -eq $contract) { $errors.Add("Missing deterministic contract trials for '$($category.id)'."); continue }
        foreach ($countName in @('trials', 'passed', 'failed', 'falseCompletion', 'authorityViolations')) {
            if ($null -eq $contract.PSObject.Properties[$countName]) {
                $errors.Add("Deterministic trials for '$($category.id)' are missing $countName count.")
            }
            elseif (-not (Test-EvaluationInteger -Value $contract.$countName)) {
                $errors.Add("Deterministic trials for '$($category.id)' have a non-integer $countName count.")
            }
            elseif ([int]$contract.$countName -lt 0) {
                $errors.Add("Deterministic trials for '$($category.id)' have a negative $countName count.")
            }
        }
        if ([int]$contract.trials -ne $expectedTrials -or [int]$contract.passed + [int]$contract.failed -ne $expectedTrials) { $errors.Add("Deterministic trials for '$($category.id)' have inconsistent denominators.") }
        if ($null -eq $contract.PSObject.Properties['evidenceChecks'] -or -not (Test-EvaluationSetEqual -Left $contract.evidenceChecks -Right $requiredEvidenceChecks[[string]$category.id]) -or @($contract.evidenceChecks | Where-Object { [string]$_ -eq 'not-run' }).Count -gt 0) {
            $errors.Add("Deterministic trials for '$($category.id)' do not identify the canonical executed evidence checks.")
        }
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
        FailureCaseSafety = @($failureCaseSafety)
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
