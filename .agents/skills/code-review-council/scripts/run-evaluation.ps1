#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][Alias('Fixtures')][string]$FixturesPath,
    [Parameter(Mandatory)][Alias('Output')][string]$OutputPath,
    [Alias('MarkdownOutput')][string]$MarkdownPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

$script:Approaches = @('single-reviewer','all-lenses','council')
$script:RequiredCases = @('unchanged-codebase-defect','branch-advanced-base','stacked-pull-request','staged-only','unstaged-only','untracked-only','staged-hidden-by-unstaged','rename-delete','no-change','unresolved-index','conditional-distributed-system','protected-non-defect','duplicate-findings','distinct-same-location','stale-pr-comments','partial-publication','revision-change','truncated-diff','reviewer-failure','unsupported-model','unavailable-parallelism','malicious-instructions','clean-zero-findings','adjacent-unrelated-request')
function Test-EvalProperty { param([object]$Object,[string]$Name) return $null -ne $Object.PSObject.Properties[$Name] }

function Get-Fixture { param([string]$Path)
    $fixture=Get-Content -LiteralPath (Resolve-CrcSafePath -Path $Path -Label 'fixture' -MustExist) -Raw | ConvertFrom-Json
    if($fixture.schema_version -ne $script:CrcSchemaVersion -or @($fixture.cases).Count -eq 0){throw 'fixture schema is invalid'}
    $ids=@($fixture.cases | ForEach-Object id)
    if ($ids.Count -ne @($ids | Sort-Object -Unique).Count) { throw 'fixture contains duplicate case IDs' }
    if (@($ids | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0) { throw 'fixture contains an empty case ID' }
    foreach($required in $script:RequiredCases){if($required -notin $ids){throw "required fixture missing: $required"}}
    if(@($fixture.cases | Where-Object set -eq 'development').Count -eq 0 -or @($fixture.cases | Where-Object set -eq 'held-out').Count -eq 0){throw 'fixture requires development and held-out cases'}
    foreach($case in @($fixture.cases)){if($case.truth -isnot [System.Collections.IEnumerable] -or $case.truth -is [string]){throw "truth must be a list: $($case.id)"}; $high=if(Test-EvalProperty $case 'high_severity_truth'){@($case.high_severity_truth)}else{@()}; if(@($high | Where-Object { $_ -notin @($case.truth) }).Count -gt 0){throw "high severity truth is invalid: $($case.id)"}; if((Test-EvalProperty $case 'trigger_expected') -and $case.trigger_expected -isnot [bool]){throw "trigger_expected is invalid: $($case.id)"}}
    return $fixture
}

function Get-Approach { param([object]$Fixture,[object]$Case,[string]$Name)
    $base=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; if((Test-EvalProperty $Fixture.defaults.$Name 'publication') -and (Test-EvalProperty $Case.approaches.$Name 'publication')){$base.publication=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value} }; return [pscustomobject]$base
}

function Get-Metrics { param([object]$Fixture,[object[]]$Cases,[string]$Name)
    [double]$predicted=0; [double]$truePredicted=0; [double]$truthCount=0; [double]$highTruth=0; [double]$trueHigh=0; [double]$falseBlockers=0; [double]$clean=0; [double]$invalid=0; [double]$anchored=0; [double]$scopePresent=0; [double]$scopeRequired=0; [double]$policyPass=0; [double]$triggerPass=0; [double]$triggerCases=0; [double]$duplicates=0; [double]$publicationAttempts=0; [double]$latency=0; [double]$tokens=0; $council=@{}; $baselines=@{}
    foreach($case in $Cases){$approach=Get-Approach $Fixture $case $Name; $findings=@($approach.findings); $truth=@($case.truth); $high=@(); if(Test-EvalProperty $case 'high_severity_truth'){$high=@($case.high_severity_truth)}; $ids=@($findings | ForEach-Object fingerprint | Sort-Object -Unique); $hits=@($ids | Where-Object {$_ -in $truth}); $highHits=@($ids | Where-Object {$_ -in $high}); $predicted+=$ids.Count; $truePredicted+=$hits.Count; $truthCount+=$truth.Count; $highTruth+=$high.Count; $trueHigh+=$highHits.Count; $invalid+=@($findings | Where-Object { -not $_.valid_anchor }).Count; $anchored+=$findings.Count; if($truth.Count -eq 0){$clean++; if($approach.result_status -eq 'BLOCKED' -or @($findings | Where-Object blocked).Count -gt 0){$falseBlockers++}}; $required=[int]$approach.scope_elements_required; $present=[int]$approach.scope_elements_present; $scopeRequired+=$required; $scopePresent+=$present; $policyPass += [int][bool]$approach.policy_compliant; $triggerCases++; $expected=if((Test-EvalProperty $case 'trigger_expected')){[bool]$case.trigger_expected}else{$true}; $triggerPass += [int]($approach.triggered -eq $expected); $publication=if(Test-EvalProperty $approach 'publication'){$approach.publication}else{[pscustomobject]@{attempts=0;duplicate_attempts=0}}; $duplicates += [int]$publication.duplicate_attempts; $publicationAttempts += [int]$publication.attempts; $latency += [int]$approach.latency_ms; $tokens += [int]$approach.tokens; if($Name -eq 'council'){foreach($f in @($findings | Where-Object validated)){$council[$f.fingerprint]=$true}; foreach($baselineName in @('single-reviewer','all-lenses')){$baselineApproach=Get-Approach $Fixture $case $baselineName; foreach($f in @($baselineApproach.findings)){$baselines[$f.fingerprint]=$true}}} }
    return [ordered]@{ cases=$Cases.Count; precision=if($predicted){[math]::Round($truePredicted/$predicted,4)}else{1}; recall=if($truthCount){[math]::Round($truePredicted/$truthCount,4)}else{1}; p0_p1_recall=if($highTruth){[math]::Round($trueHigh/$highTruth,4)}else{1}; false_blocker_rate=if($clean){[math]::Round($falseBlockers/$clean,4)}else{0}; duplicate_publication_rate=if($publicationAttempts){[math]::Round($duplicates/$publicationAttempts,4)}else{0}; invalid_anchor_rate=if($anchored){[math]::Round($invalid/$anchored,4)}else{0}; scope_completeness=if($scopeRequired){[math]::Round($scopePresent/$scopeRequired,4)}else{0}; policy_compliance=[math]::Round($policyPass/$Cases.Count,4); skill_trigger_accuracy=[math]::Round($triggerPass/$triggerCases,4); latency_ms_total=$latency; tokens_total=$tokens; unique_validated_contribution=if($Name -eq 'council'){(@($council.Keys | Where-Object { -not $baselines.ContainsKey($_) }).Count)}else{0}}
}

try {
    $fixture=Get-Fixture -Path $FixturesPath; $cases=@($fixture.cases); $setCounts=[ordered]@{}; foreach($set in @($cases.set)){if(-not $setCounts.Contains($set)){$setCounts[$set]=0};$setCounts[$set]++}
    $overall=[ordered]@{}; foreach($name in $script:Approaches){$overall[$name]=Get-Metrics $fixture $cases $name}
    $bySet=[ordered]@{}; foreach($set in $setCounts.Keys){$bySet[$set]=[ordered]@{}; $setCases=@($cases | Where-Object set -eq $set); foreach($name in $script:Approaches){$bySet[$set][$name]=Get-Metrics $fixture $setCases $name}}
    $overallObject=[pscustomobject]@{'single-reviewer'=$overall['single-reviewer'];'all-lenses'=$overall['all-lenses'];council=$overall.council}
    $setCountsObject=[pscustomobject]@{development=$setCounts['development'];'held-out'=$setCounts['held-out']}
    $result=[ordered]@{schema_version=$script:CrcSchemaVersion;evaluation='offline-deterministic';case_count=$cases.Count;set_counts=$setCountsObject;overall=$overallObject;by_set=[pscustomobject]@{};required_cases=$script:RequiredCases;limitations=@('No live model execution was performed by this runner.','No live GitHub publication was performed by this runner.','Latency and token values are fixture observations, not production measurements.')}
    Write-CrcJson -Path $OutputPath -Value $result
    if($MarkdownPath){$markdownSafePath=Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown output';$lines=@('# Code Review Council evaluation','', 'This is an offline deterministic fixture evaluation. It does not measure live model precision, recall, or provider latency.','',"- Cases: ``$($cases.Count)``","- Development cases: ``$($setCounts.development)``","- Held-out cases: ``$($setCounts.'held-out')``",'', '## Overall results','', '| Approach | Precision | Recall | P0/P1 recall | False blockers | Scope | Policy |','| --- | ---: | ---: | ---: | ---: | ---: | ---: |'); foreach($name in $script:Approaches){$m=$overall[$name];$lines += "| ``$name`` | $($m.precision) | $($m.recall) | $($m.p0_p1_recall) | $($m.false_blocker_rate) | $($m.scope_completeness) | $($m.policy_compliance) |"}; $lines += @('', '## Limitations','', '- No live Codex reviewer or GitHub publication was executed by this offline runner.','- Fixture latency and token values are simulated observations for comparison only.','- A live promotion decision requires real traces, host capability metadata, and current CI/review evidence.'); Set-Content -LiteralPath $markdownSafePath -Value ($lines -join "`n") -Encoding utf8 }
    $overall | ConvertTo-Json -Depth 100 -Compress; exit 0
}
catch { Write-Error $_.Exception.Message; exit 2 }
