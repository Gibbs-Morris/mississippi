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
function Assert-EvalBoolean { param([object]$Object,[string]$Name,[string]$Location) if ((Test-EvalProperty $Object $Name) -and $Object.$Name -isnot [bool]) { throw "$Location.$Name must be a boolean" } }

function Assert-EvaluationApproach { param([object]$Approach,[string]$Location)
    foreach($name in @('triggered','policy_compliant','scope_complete')){Assert-EvalBoolean -Object $Approach -Name $name -Location $Location}
    if(-not (Test-EvalProperty $Approach 'findings')){return}
    if($Approach.findings -isnot [System.Array]){throw "$Location.findings must be an array"}
    $index=0
    foreach($finding in $Approach.findings){
        $index++
        foreach($name in @('valid_anchor','blocked','validated')){Assert-EvalBoolean -Object $finding -Name $name -Location "$Location.findings[$index]"}
    }
}

function Assert-EvaluationDefaults { param([object]$Fixture)
    foreach($name in $script:Approaches){Assert-EvaluationApproach -Approach $Fixture.defaults.$name -Location "defaults.$name"}
}

function Assert-EvaluationFingerprints { param([object[]]$Values,[string]$Name,[string]$CaseId)
    $unique = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($value in $Values) {
        if ($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value)) { throw "$Name contains an empty or non-string fingerprint: $CaseId" }
        if (-not $unique.Add($value)) { throw "$Name contains duplicate fingerprints: $CaseId" }
    }
}

function Assert-EvaluationCase { param([object]$Case)
    if($Case.set -notin @('development','held-out')){throw "unknown evaluation set: $($Case.set)"}
    if($Case.truth -isnot [System.Array]){throw "truth must be an array: $($Case.id)"}
    Assert-EvaluationFingerprints -Values $Case.truth -Name 'truth' -CaseId $Case.id
    if(Test-EvalProperty $Case 'high_severity_truth'){
        if($Case.high_severity_truth -isnot [System.Array]){throw "high_severity_truth must be an array: $($Case.id)"}
        Assert-EvaluationFingerprints -Values $Case.high_severity_truth -Name 'high_severity_truth' -CaseId $Case.id
        if(@($Case.high_severity_truth | Where-Object { $_ -cnotin $Case.truth }).Count -gt 0){throw "high severity truth is invalid: $($Case.id)"}
    }
    Assert-EvalBoolean -Object $Case -Name 'trigger_expected' -Location $Case.id
    if(-not (Test-EvalProperty $Case 'approaches')){throw "approaches are missing: $($Case.id)"}
    foreach($name in $script:Approaches){Assert-EvaluationApproach -Approach $Case.approaches.$name -Location "$($Case.id).$name"}
}

function Assert-RequiredEvaluationCoverage { param([object[]]$Cases,[string[]]$Ids)
    if(@($Cases | Where-Object set -eq 'development').Count -eq 0 -or @($Cases | Where-Object set -eq 'held-out').Count -eq 0){throw 'fixture requires development and held-out cases'}
    foreach($required in $script:RequiredCases){if($required -notin $Ids){throw "required fixture missing: $required"}}
}

function Get-Fixture { param([string]$Path)
    $fixture=Get-Content -LiteralPath (Resolve-CrcSafePath -Path $Path -Label 'fixture' -MustExist) -Raw | ConvertFrom-Json
    $cases=@($fixture.cases)
    if($fixture.schema_version -ne $script:CrcSchemaVersion -or $cases.Count -eq 0){throw 'fixture schema is invalid'}
    $ids=@($cases | ForEach-Object id)
    if($ids.Count -ne @($ids | Sort-Object -Unique).Count){throw 'fixture contains duplicate case IDs'}
    if(@($ids | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0){throw 'fixture contains an empty case ID'}
    Assert-RequiredEvaluationCoverage -Cases $cases -Ids $ids
    Assert-EvaluationDefaults -Fixture $fixture
    foreach($case in $cases){Assert-EvaluationCase -Case $case}
    return $fixture
}

function Get-Approach { param([object]$Fixture,[object]$Case,[string]$Name)
    $base=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; if((Test-EvalProperty $Fixture.defaults.$Name 'publication') -and (Test-EvalProperty $Case.approaches.$Name 'publication')){$base.publication=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value} }; return [pscustomobject]$base
}

function Get-MetricRatio { param([double]$Numerator,[double]$Denominator,[double]$EmptyValue=1)
    if($Denominator -gt 0){return [math]::Round($Numerator/$Denominator,4)}
    return $EmptyValue
}

function New-MetricTotals {
    return [pscustomobject]@{predicted=0.0;true_predicted=0.0;truth=0.0;high_truth=0.0;true_high=0.0;false_blockers=0.0;clean=0.0;invalid=0.0;anchored=0.0;scope_present=0.0;scope_required=0.0;policy_pass=0.0;trigger_pass=0.0;trigger_cases=0.0;duplicates=0.0;publication_attempts=0.0;latency=0.0;tokens=0.0;council=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal);baselines=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)}
}

function Get-EvaluationFindings { param([object]$Approach,[string]$Name)
    $findings=@($Approach.findings)
    if($Name -eq 'council'){$findings=@($findings | Where-Object {$_.validated -eq $true})}
    return ,$findings
}

function Add-FindingMetrics { param([object]$Totals,[object[]]$Findings,[object[]]$Truth,[object[]]$HighTruth)
    $ids=@($Findings | ForEach-Object fingerprint | Sort-Object -Unique)
    $hits=@($ids | Where-Object {$_ -in $Truth})
    $highHits=@($ids | Where-Object {$_ -in $HighTruth})
    $Totals.predicted += $ids.Count; $Totals.true_predicted += $hits.Count; $Totals.truth += $Truth.Count; $Totals.high_truth += $HighTruth.Count; $Totals.true_high += $highHits.Count
    $Totals.invalid += @($Findings | Where-Object {$_.valid_anchor -eq $false}).Count
    $Totals.anchored += $Findings.Count
}

function Add-CaseSafetyMetrics { param([object]$Totals,[object]$Approach,[object[]]$Findings,[object]$Case)
    if($Case.truth.Count -eq 0){
        $Totals.clean++
        if($Approach.result_status -eq 'BLOCKED' -or @($Findings | Where-Object {$_.blocked -eq $true}).Count -gt 0){$Totals.false_blockers++}
    }
    $Totals.scope_required += [double]$Approach.scope_elements_required
    $Totals.scope_present += [double]$Approach.scope_elements_present
    $Totals.policy_pass += [int]([bool]$Approach.policy_compliant)
    $expected=if(Test-EvalProperty $Case 'trigger_expected'){[bool]$Case.trigger_expected}else{$true}
    $Totals.trigger_cases++
    $Totals.trigger_pass += [int]($Approach.triggered -eq $expected)
}

function Add-PublicationMetrics { param([object]$Totals,[object]$Approach)
    $publication=if(Test-EvalProperty $Approach 'publication'){$Approach.publication}else{[pscustomobject]@{attempts=0;duplicate_attempts=0}}
    $Totals.duplicates += [int]$publication.duplicate_attempts
    $Totals.publication_attempts += [int]$publication.attempts
    $Totals.latency += [int]$Approach.latency_ms
    $Totals.tokens += [int]$Approach.tokens
}

function Add-CouncilContributionMetrics { param([object]$Totals,[object]$Fixture,[object]$Case,[object[]]$Findings)
    foreach($finding in $Findings){[void]$Totals.council.Add([string]$finding.fingerprint)}
    foreach($baselineName in @('single-reviewer','all-lenses')){
        $baseline=Get-Approach -Fixture $Fixture -Case $Case -Name $baselineName
        foreach($finding in @($baseline.findings)){[void]$Totals.baselines.Add([string]$finding.fingerprint)}
    }
}

function Complete-MetricTotals { param([object]$Totals,[int]$CaseCount,[string]$Name)
    $uniqueContribution=0
    if($Name -eq 'council'){$uniqueContribution=@($Totals.council | Where-Object {-not $Totals.baselines.Contains($_)}).Count}
    return [pscustomobject]@{
        cases=$CaseCount
        precision=Get-MetricRatio $Totals.true_predicted $Totals.predicted
        recall=Get-MetricRatio $Totals.true_predicted $Totals.truth
        p0_p1_recall=Get-MetricRatio $Totals.true_high $Totals.high_truth
        false_blocker_rate=Get-MetricRatio $Totals.false_blockers $Totals.clean 0
        duplicate_publication_rate=Get-MetricRatio $Totals.duplicates $Totals.publication_attempts 0
        invalid_anchor_rate=Get-MetricRatio $Totals.invalid $Totals.anchored 0
        scope_completeness=Get-MetricRatio $Totals.scope_present $Totals.scope_required 0
        policy_compliance=Get-MetricRatio $Totals.policy_pass $CaseCount 0
        skill_trigger_accuracy=Get-MetricRatio $Totals.trigger_pass $Totals.trigger_cases 0
        latency_ms_total=$Totals.latency; tokens_total=$Totals.tokens; unique_validated_contribution=$uniqueContribution
    }
}

function Get-Metrics { param([object]$Fixture,[object[]]$Cases,[string]$Name)
    $totals=New-MetricTotals
    foreach($case in $Cases){
        $approach=Get-Approach -Fixture $Fixture -Case $case -Name $Name
        $findings=Get-EvaluationFindings -Approach $approach -Name $Name
        $highTruth=@(); if(Test-EvalProperty $case 'high_severity_truth'){$highTruth=@($case.high_severity_truth)}
        Add-FindingMetrics -Totals $totals -Findings $findings -Truth @($case.truth) -HighTruth $highTruth
        Add-CaseSafetyMetrics -Totals $totals -Approach $approach -Findings $findings -Case $case
        Add-PublicationMetrics -Totals $totals -Approach $approach
        if($Name -eq 'council'){Add-CouncilContributionMetrics -Totals $totals -Fixture $Fixture -Case $case -Findings $findings}
    }
    return Complete-MetricTotals -Totals $totals -CaseCount $Cases.Count -Name $Name
}

function Ensure-EvaluationOutputParent { param([string]$Path)
    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
}

try {
    $fixtureSafePath=Resolve-CrcSafePath -Path $FixturesPath -Label 'fixture input' -MustExist
    $requestedOutputPath=Resolve-CrcSafePath -Path $OutputPath -Label 'evaluation output'
    $requestedMarkdownPath=$null; if($MarkdownPath){$requestedMarkdownPath=Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown output'}
    if(Test-CrcPathEqual -Left $fixtureSafePath -Right $requestedOutputPath){throw 'evaluation output must differ from fixture input'}
    if($requestedMarkdownPath -and (Test-CrcPathEqual -Left $fixtureSafePath -Right $requestedMarkdownPath)){throw 'Markdown output must differ from fixture input'}
    if($requestedMarkdownPath -and (Test-CrcPathEqual -Left $requestedOutputPath -Right $requestedMarkdownPath)){throw 'Markdown output must differ from evaluation output'}
    if($requestedMarkdownPath){Ensure-EvaluationOutputParent -Path $requestedMarkdownPath}
    $fixture=Get-Fixture -Path $fixtureSafePath; $cases=@($fixture.cases); $setCounts=@{}; foreach($set in @($cases | ForEach-Object { $_.set })){if(-not $setCounts.ContainsKey($set)){$setCounts[$set]=0};$setCounts[$set]++}
    $overall=[ordered]@{}; foreach($name in $script:Approaches){$overall[$name]=Get-Metrics $fixture $cases $name}
    $bySet=[ordered]@{}; foreach($set in $setCounts.Keys){$setMetrics=[ordered]@{}; $setCases=@($cases | Where-Object set -eq $set); foreach($name in $script:Approaches){$setMetrics[$name]=[pscustomobject](Get-Metrics $fixture $setCases $name)}; $bySet[$set]=[pscustomobject]$setMetrics}; $bySet=[pscustomobject]$bySet
    $overallObject=[pscustomobject]@{'single-reviewer'=$overall['single-reviewer'];'all-lenses'=$overall['all-lenses'];council=$overall['council']}
    $setCountsObject=[pscustomobject]@{development=$setCounts['development'];'held-out'=$setCounts['held-out']}
    $setCounts=[pscustomobject]@{development=$setCounts['development'];'held-out'=$setCounts['held-out']}
    $result=[ordered]@{schema_version=$script:CrcSchemaVersion;evaluation='offline-deterministic';case_count=$cases.Count;set_counts=$setCountsObject;overall=$overallObject;by_set=$bySet;required_cases=$script:RequiredCases;limitations=@('No live model execution was performed by this runner.','No live GitHub publication was performed by this runner.','Latency and token values are fixture observations, not production measurements.')}
    Write-CrcJson -Path $requestedOutputPath -Value $result
    if($requestedMarkdownPath){$lines=@('# Code Review Council evaluation','', 'This is an offline deterministic fixture evaluation. It does not measure live model precision, recall, or provider latency.','',"- Cases: ``$($cases.Count)``","- Development cases: ``$($setCounts.development)``","- Held-out cases: ``$($setCounts.'held-out')``",'', '## Overall results','', '| Approach | Precision | Recall | P0/P1 recall | False blockers | Scope | Policy |','| --- | ---: | ---: | ---: | ---: | ---: | ---: |'); foreach($name in $script:Approaches){$m=$overall[$name];$lines += "| ``$name`` | $($m.precision) | $($m.recall) | $($m.p0_p1_recall) | $($m.false_blocker_rate) | $($m.scope_completeness) | $($m.policy_compliance) |"}; $lines += @('', '## Limitations','', '- No live Codex reviewer or GitHub publication was executed by this offline runner.','- Fixture latency and token values are simulated observations for comparison only.','- A live promotion decision requires real traces, host capability metadata, and current CI/review evidence.'); Set-Content -LiteralPath $requestedMarkdownPath -Value ($lines -join "`n") -Encoding utf8 }
    $overall | ConvertTo-Json -Depth 100 -Compress; exit 0
}
catch { [Console]::Error.WriteLine($_.Exception.ToString()); [Console]::Error.WriteLine($_.InvocationInfo.PositionMessage); exit 2 }
