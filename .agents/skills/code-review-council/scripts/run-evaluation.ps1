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

function Get-Fixture { param([string]$Path)
    $fixture=Get-Content -LiteralPath (Resolve-CrcSafePath -Path $Path -Label 'fixture' -MustExist) -Raw | ConvertFrom-Json
    if($fixture.schema_version -ne $script:CrcSchemaVersion -or @($fixture.cases).Count -eq 0){throw 'fixture schema is invalid'}
    $ids=@($fixture.cases | ForEach-Object id)
    if ($ids.Count -ne @($ids | Sort-Object -Unique).Count) { throw 'fixture contains duplicate case IDs' }
    if (@($ids | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0) { throw 'fixture contains an empty case ID' }
    foreach($required in $script:RequiredCases){if($required -notin $ids){throw "required fixture missing: $required"}}
    if(@($fixture.cases | Where-Object set -eq 'development').Count -eq 0 -or @($fixture.cases | Where-Object set -eq 'held-out').Count -eq 0){throw 'fixture requires development and held-out cases'}
    foreach($name in $script:Approaches){$defaultApproach=$fixture.defaults.$name; foreach($booleanName in @('triggered','policy_compliant','scope_complete')){Assert-EvalBoolean -Object $defaultApproach -Name $booleanName -Location "defaults.$name"}; if(Test-EvalProperty $defaultApproach 'findings'){if($defaultApproach.findings -isnot [System.Array]){throw "defaults.$name.findings must be an array"}; $findingIndex=0; foreach($finding in $defaultApproach.findings){$findingIndex++; foreach($booleanName in @('valid_anchor','blocked','validated')){Assert-EvalBoolean -Object $finding -Name $booleanName -Location "defaults.$name.findings[$findingIndex]"}}}}
    foreach($case in @($fixture.cases)){
        if($case.set -notin @('development','held-out')){throw "unknown evaluation set: $($case.set)"}
        if($case.truth -isnot [System.Collections.IEnumerable] -or $case.truth -is [string]){throw "truth must be a list: $($case.id)"}
        $high=if(Test-EvalProperty $case 'high_severity_truth'){@($case.high_severity_truth)}else{@()}; if(@($high | Where-Object { $_ -notin @($case.truth) }).Count -gt 0){throw "high severity truth is invalid: $($case.id)"}; if((Test-EvalProperty $case 'trigger_expected') -and $case.trigger_expected -isnot [bool]){throw "trigger_expected is invalid: $($case.id)"}
        foreach($name in $script:Approaches){
            $approach = if(Test-EvalProperty $case.approaches $name){$case.approaches.$name}else{[pscustomobject]@{}}
            foreach($booleanName in @('triggered','policy_compliant','scope_complete')){Assert-EvalBoolean -Object $approach -Name $booleanName -Location "$($case.id).$name"}
            if(Test-EvalProperty $approach 'findings'){
                if($approach.findings -isnot [System.Array]){throw "$($case.id).$name.findings must be an array"}
                $findingIndex=0; foreach($finding in $approach.findings){$findingIndex++; foreach($booleanName in @('valid_anchor','blocked','validated')){Assert-EvalBoolean -Object $finding -Name $booleanName -Location "$($case.id).$name.findings[$findingIndex]"}}
            }
        }
    }
    return $fixture
}

function Get-Approach { param([object]$Fixture,[object]$Case,[string]$Name)
    $base=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.PSObject.Properties){$base[$property.Name]=$property.Value}; if((Test-EvalProperty $Fixture.defaults.$Name 'publication') -and (Test-EvalProperty $Case.approaches.$Name 'publication')){$base.publication=[ordered]@{}; foreach($property in $Fixture.defaults.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value}; foreach($property in $Case.approaches.$Name.publication.PSObject.Properties){$base.publication[$property.Name]=$property.Value} }; return [pscustomobject]$base
}

function Get-Metrics { param([object]$Fixture,[object[]]$Cases,[string]$Name)
    [double]$predicted=0; [double]$truePredicted=0; [double]$truthCount=0; [double]$highTruth=0; [double]$trueHigh=0; [double]$falseBlockers=0; [double]$clean=0; [double]$invalid=0; [double]$anchored=0; [double]$scopePresentTotal=0; [double]$scopeRequiredTotal=0; [double]$policyPass=0; [double]$triggerPass=0; [double]$triggerCases=0; [double]$duplicates=0; [double]$publicationAttempts=0; [double]$latency=0; [double]$tokens=0; $council=@{}; $baselines=@{}
    foreach($case in $Cases){$approach=Get-Approach $Fixture $case $Name; $findings=@($approach.findings); if($Name -eq 'council'){$findings=@($findings | Where-Object { $_.validated -eq $true })}; $truth=@($case.truth); $high=@(); if(Test-EvalProperty $case 'high_severity_truth'){$high=@($case.high_severity_truth)}; $ids=@($findings | ForEach-Object fingerprint | Sort-Object -Unique); $hits=@($ids | Where-Object {$_ -in $truth}); $highHits=@($ids | Where-Object {$_ -in $high}); $predicted+=$ids.Count; $truePredicted+=$hits.Count; $truthCount+=$truth.Count; $highTruth+=$high.Count; $trueHigh+=$highHits.Count; $invalid+=@($findings | Where-Object { $_.valid_anchor -eq $false }).Count; $anchored+=$findings.Count; if($truth.Count -eq 0){$clean++; if($approach.result_status -eq 'BLOCKED' -or @($findings | Where-Object { $_.blocked -eq $true }).Count -gt 0){$falseBlockers++}}; $required=[int]$approach.scope_elements_required; $present=[int]$approach.scope_elements_present; $policyPass += [int][bool]$approach.policy_compliant; $triggerCases++; $expected=if((Test-EvalProperty $case 'trigger_expected')){[bool]$case.trigger_expected}else{$true}; $triggerPass += [int]($approach.triggered -eq $expected); $publication=if(Test-EvalProperty $approach 'publication'){$approach.publication}else{[pscustomobject]@{attempts=0;duplicate_attempts=0}}; $duplicates += [int]$publication.duplicate_attempts; $publicationAttempts += [int]$publication.attempts; $latency += [int]$approach.latency_ms; $tokens += [int]$approach.tokens; if($Name -eq 'council'){foreach($f in @($findings)){$council[$f.fingerprint]=$true}; foreach($baselineName in @('single-reviewer','all-lenses')){$baselineApproach=Get-Approach $Fixture $case $baselineName; foreach($f in @($baselineApproach.findings)){$baselines[$f.fingerprint]=$true}}} }
    return [ordered]@{ cases=$Cases.Count; precision=if($predicted){[math]::Round($truePredicted/$predicted,4)}else{1}; recall=if($truthCount){[math]::Round($truePredicted/$truthCount,4)}else{1}; p0_p1_recall=if($highTruth){[math]::Round($trueHigh/$highTruth,4)}else{1}; false_blocker_rate=if($clean){[math]::Round($falseBlockers/$clean,4)}else{0}; duplicate_publication_rate=if($publicationAttempts){[math]::Round($duplicates/$publicationAttempts,4)}else{0}; invalid_anchor_rate=if($anchored){[math]::Round($invalid/$anchored,4)}else{0}; scope_completeness=if($scopeRequired){[math]::Round($scopePresent/$scopeRequired,4)}else{0}; policy_compliance=[math]::Round($policyPass/$Cases.Count,4); skill_trigger_accuracy=[math]::Round($triggerPass/$triggerCases,4); latency_ms_total=$latency; tokens_total=$tokens; unique_validated_contribution=if($Name -eq 'council'){(@($council.Keys | Where-Object { -not $baselines.ContainsKey($_) }).Count)}else{0}}
}

function Get-Metrics { param([object]$Fixture,[object[]]$Cases,[string]$Name)
    [double]$predicted=0; [double]$truePredicted=0; [double]$truthCount=0; [double]$highTruth=0; [double]$trueHigh=0; [double]$falseBlockers=0; [double]$clean=0; [double]$invalid=0; [double]$anchored=0; [double]$scopePresentTotal=0; [double]$scopeRequiredTotal=0; [double]$policyPass=0; [double]$triggerPass=0; [double]$triggerCases=0; [double]$duplicates=0; [double]$publicationAttempts=0; [double]$latency=0; [double]$tokens=0; $council=@{}; $baselines=@{}
    foreach($case in $Cases) {
        $approach=Get-Approach $Fixture $case $Name
        $findings=@($approach.findings)
        if($Name -eq 'council') { $findings=@($findings | Where-Object { $_.validated -eq $true }) }
        $truth=@($case.truth); $high=@(); if(Test-EvalProperty $case 'high_severity_truth'){$high=@($case.high_severity_truth)}
        $ids=@($findings | ForEach-Object fingerprint | Sort-Object -Unique); $hits=@($ids | Where-Object {$_ -in $truth}); $highHits=@($ids | Where-Object {$_ -in $high})
        $predicted += $ids.Count; $truePredicted += $hits.Count; $truthCount += $truth.Count; $highTruth += $high.Count; $trueHigh += $highHits.Count
        $invalid += @($findings | Where-Object { $_.valid_anchor -eq $false }).Count; $anchored += $findings.Count
        if($truth.Count -eq 0) { $clean++; if($approach.result_status -eq 'BLOCKED' -or @($findings | Where-Object { $_.blocked -eq $true }).Count -gt 0) { $falseBlockers++ } }
        $scopeRequiredTotal += [double]$approach.scope_elements_required; $scopePresentTotal += [double]$approach.scope_elements_present
        $policyPass += [int]([bool]$approach.policy_compliant); $triggerCases++; $expected=if(Test-EvalProperty $case 'trigger_expected'){[bool]$case.trigger_expected}else{$true}; $triggerPass += [int]($approach.triggered -eq $expected)
        $publication=if(Test-EvalProperty $approach 'publication'){$approach.publication}else{[pscustomobject]@{attempts=0;duplicate_attempts=0}}; $duplicates += [int]$publication.duplicate_attempts; $publicationAttempts += [int]$publication.attempts; $latency += [int]$approach.latency_ms; $tokens += [int]$approach.tokens
        if($Name -eq 'council') { foreach($f in @($findings)){$council[$f.fingerprint]=$true}; foreach($baselineName in @('single-reviewer','all-lenses')) { $baselineApproach=Get-Approach $Fixture $case $baselineName; foreach($f in @($baselineApproach.findings)){$baselines[$f.fingerprint]=$true} } }
    }
    return [pscustomobject]@{ cases=$Cases.Count; precision=if($predicted -gt 0){[math]::Round($truePredicted/$predicted,4)}else{1}; recall=if($truthCount -gt 0){[math]::Round($truePredicted/$truthCount,4)}else{1}; p0_p1_recall=if($highTruth -gt 0){[math]::Round($trueHigh/$highTruth,4)}else{1}; false_blocker_rate=if($clean -gt 0){[math]::Round($falseBlockers/$clean,4)}else{0}; duplicate_publication_rate=if($publicationAttempts -gt 0){[math]::Round($duplicates/$publicationAttempts,4)}else{0}; invalid_anchor_rate=if($anchored -gt 0){[math]::Round($invalid/$anchored,4)}else{0}; scope_completeness=if($scopeRequiredTotal -gt 0){[math]::Round($scopePresentTotal/$scopeRequiredTotal,4)}else{0}; policy_compliance=[math]::Round($policyPass/$Cases.Count,4); skill_trigger_accuracy=[math]::Round($triggerPass/$triggerCases,4); latency_ms_total=$latency; tokens_total=$tokens; unique_validated_contribution=if($Name -eq 'council'){(@($council.Keys | Where-Object { -not $baselines.ContainsKey($_) }).Count)}else{0} }
}

try {
    $fixtureSafePath=Resolve-CrcSafePath -Path $FixturesPath -Label 'fixture input' -MustExist
    $requestedOutputPath=Resolve-CrcSafePath -Path $OutputPath -Label 'evaluation output'
    $requestedMarkdownPath=$null; if($MarkdownPath){$requestedMarkdownPath=Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown output'}
    if(Test-CrcPathEqual -Left $fixtureSafePath -Right $requestedOutputPath){throw 'evaluation output must differ from fixture input'}
    if($requestedMarkdownPath -and (Test-CrcPathEqual -Left $fixtureSafePath -Right $requestedMarkdownPath)){throw 'Markdown output must differ from fixture input'}
    if($requestedMarkdownPath -and (Test-CrcPathEqual -Left $requestedOutputPath -Right $requestedMarkdownPath)){throw 'Markdown output must differ from evaluation output'}
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
