#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][Alias('Review')][string]$ReviewPath,
    [ValidateSet('mock','github')][string]$Provider = 'mock',
    [Alias('Ledger')][string]$LedgerPath,
    [string]$Repo,
    [int]$Pr,
    [string]$ExpectedBase,
    [string]$ExpectedHead,
    [string]$Markdown,
    [switch]$Execute,
    [Alias('Output')][string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

function Invoke-GhApiJson { param([string[]]$Arguments,[object]$Body)
    $all = @('api','--hostname','github.com') + $Arguments
    $errorPath = [System.IO.Path]::GetTempFileName()
    try {
        if ($null -ne $Body) { $json = $Body | ConvertTo-Json -Depth 100 -Compress; $result = $json | & gh @all 2> $errorPath } else { $result = & gh @all 2> $errorPath }
        if ($LASTEXITCODE -ne 0) {
            $detail = Get-Content -LiteralPath $errorPath -Raw -ErrorAction SilentlyContinue
            throw "gh api failed: $($detail.Trim())"
        }
    }
    finally {
        if (Test-Path -LiteralPath $errorPath) { Remove-Item -LiteralPath $errorPath -Force }
    }
    return ($result -join "`n" | ConvertFrom-Json)
}

function Get-Marker { param([object]$Review)
    $material = [ordered]@{ schema_version=$script:CrcSchemaVersion; snapshot_id=$Review.snapshot_id; status=$Review.status; findings=@($Review.findings | ForEach-Object { [ordered]@{ fingerprint=$_.fingerprint; disposition=$_.disposition } } | Sort-Object fingerprint) }
    $hash = Get-CrcHashJson -Value $material
    return "<!-- code-review-council:v1:$($hash -replace '^sha256:','') -->"
}

function Escape-Markdown { param([object]$Value)
    $text = [string]$Value
    $text = $text.Replace('\','\\').Replace("`r",'').Replace("`n",' ')
    foreach ($character in @('`','*','_','[',']','<','>','@')) { $text = $text.Replace($character,"\$character") }
    return $text
}

function Get-Body { param([object]$Review,[string]$Marker,[string]$MarkdownPath)
    if ($MarkdownPath) { return (Get-Content -LiteralPath (Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown input' -MustExist) -Raw).Trim() + "`n$Marker`n" }
    $statusText = Escape-Markdown $Review.status
    $snapshotText = Escape-Markdown $Review.snapshot_id
    $lines = @('## Code Review Council','',"Status: **$statusText**","Snapshot: $snapshotText",'')
    if (@($Review.findings).Count -eq 0) { $lines += 'No findings were produced for this snapshot.' }
    else {
        $lines += '### Findings'; $lines += ''
        foreach ($finding in @($Review.findings)) {
            $severity = Escape-Markdown $finding.severity
            $pathText = Escape-Markdown $finding.path
            $scenario = Escape-Markdown $finding.scenario
            $disposition = Escape-Markdown $finding.disposition
            $lines += "- **$severity** (${pathText}:$($finding.line)) - $scenario"
            $lines += "  - Disposition: $disposition"
        }
    }
    return (($lines -join "`n") + "`n$Marker`n")
}

function Assert-Review { param([object]$Review)
    foreach ($name in @('schema_version','status','snapshot_id','scope_manifest','reviewers','findings','dispositions','execution','publication','errors')) { if ($null -eq $Review.PSObject.Properties[$name]) { throw "review.$name is required" } }
    if ($Review.schema_version -ne $script:CrcSchemaVersion) { throw 'review schema_version is invalid' }
    if ($Review.status -notin @('PASS','BLOCKED')) { throw 'only PASS or BLOCKED results can be published' }
    if ($Review.snapshot_id -notmatch '^sha256:[0-9a-f]{64}$') { throw 'review snapshot_id is invalid' }
    if ($Review.errors -isnot [System.Array] -or @($Review.errors).Count -gt 0) { throw 'review contains validation errors or errors is not an array' }
    foreach ($name in @('snapshot_id','snapshot_material')) { if ($null -eq $Review.scope_manifest.PSObject.Properties[$name]) { throw "review.scope_manifest.$name is required" } }
    if ($Review.scope_manifest.snapshot_id -ne $Review.snapshot_id) { throw 'review snapshot_id does not match nested scope' }
    if ($Review.reviewers -isnot [System.Array] -or $Review.reviewers.Count -ne $script:CrcPersonas.Count) { throw 'review must contain all ten reviewer records' }
    $personas = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($reviewer in $Review.reviewers) {
        foreach ($name in @('review_id','persona_id','snapshot_id','status','requested_model','effective_model','requested_concurrency','effective_concurrency','completed_at_utc')) { if ($null -eq $reviewer.PSObject.Properties[$name] -or ($name -notin @('requested_model','effective_model','requested_concurrency','effective_concurrency') -and [string]::IsNullOrWhiteSpace([string]$reviewer.$name))) { throw "reviewer.$name is required" } }
        if ($reviewer.persona_id -notin $script:CrcPersonas -or -not $personas.Add([string]$reviewer.persona_id)) { throw 'reviewer personas are invalid or duplicated' }
        if ($reviewer.snapshot_id -ne $Review.snapshot_id) { throw "reviewer snapshot differs from review: $($reviewer.review_id)" }
        if ($reviewer.status -notin @('complete','not_applicable')) { throw "reviewer is not complete: $($reviewer.review_id)" }
        if ($reviewer.findings -isnot [System.Array]) { throw "reviewer findings must be an array: $($reviewer.review_id)" }
        foreach ($name in @('requested_concurrency','effective_concurrency')) { if ($null -ne $reviewer.$name -and (($reviewer.$name -isnot [int] -and $reviewer.$name -isnot [long]) -or $reviewer.$name -lt 1)) { throw "reviewer concurrency is invalid: $($reviewer.review_id)" } }
    }
    if ($Review.findings -isnot [System.Array] -or $Review.dispositions -isnot [System.Array]) { throw 'review findings and dispositions must be arrays' }
    $findingIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($finding in $Review.findings) { if ($finding.snapshot_id -ne $Review.snapshot_id -or $finding.fingerprint -notmatch '^sha256:[0-9a-f]{64}$' -or -not $finding.disposition -or -not $findingIds.Add([string]$finding.fingerprint)) { throw 'review findings are incomplete or duplicated' } }
    $dispositionIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in $Review.dispositions) { if ($entry.snapshot_id -ne $Review.snapshot_id -or $entry.disposition -notin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved') -or [string]::IsNullOrWhiteSpace([string]$entry.rationale) -or -not $findingIds.Contains([string]$entry.fingerprint) -or -not $dispositionIds.Add([string]$entry.fingerprint)) { throw 'review dispositions are incomplete or duplicated' } }
    if ($dispositionIds.Count -ne $findingIds.Count) { throw 'review does not have one disposition per finding' }
    foreach ($name in @('reviewer_count','required_reviewer_count','adjudicator','adjudicated_at_utc','adjudication_snapshot_id')) { if ($null -eq $Review.execution.PSObject.Properties[$name] -or [string]::IsNullOrWhiteSpace([string]$Review.execution.$name)) { throw "review.execution.$name is required" } }
    if ([int]$Review.execution.reviewer_count -ne $script:CrcPersonas.Count -or [int]$Review.execution.required_reviewer_count -ne $script:CrcPersonas.Count -or $Review.execution.adjudication_snapshot_id -ne $Review.snapshot_id) { throw 'review execution metadata is incomplete' }
}

function Assert-LivePullRequest { param([string]$Repository,[int]$Number,[string]$Base,[string]$Head)
    $live = Invoke-GhApiJson -Arguments @("repos/$Repository/pulls/$Number")
    if ($live.state -ne 'open' -or $live.base.sha -ne $Base -or $live.head.sha -ne $Head) { throw 'live PR state or base/head differs from reviewed scope' }
    return $live
}

function Test-LiveAnchors { param([object]$Review,[object[]]$Files)
    $byPath=@{}
    foreach($file in $Files){ if($file.filename){$byPath[$file.filename]=$file}; if($file.previous_filename){$byPath[$file.previous_filename]=$file} }
    foreach($finding in @($Review.findings)){
        $file=$byPath[[string]$finding.path]; if($null -eq $file){throw "finding path missing from live diff: $($finding.path):$($finding.line)"}
        if ($null -eq $file.PSObject.Properties['patch'] -or [string]::IsNullOrWhiteSpace([string]$file.patch)) { continue }
        $found=$false; foreach($match in [regex]::Matches([string]$file.patch,'@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@')){ foreach($start in @([int]$match.Groups[1].Value,[int]$match.Groups[3].Value)){ $count=if($start -eq [int]$match.Groups[1].Value){if($match.Groups[2].Success){[int]$match.Groups[2].Value}else{1}}else{if($match.Groups[4].Success){[int]$match.Groups[4].Value}else{1}}; if([int]$finding.line -ge $start -and [int]$finding.line -lt ($start+$count)){$found=$true} } }; if(-not $found){throw "finding line missing from live diff: $($finding.path):$($finding.line)"}
    }
}

 $outputSafePath = $null
try {
    if ($OutputPath) { $outputSafePath = Resolve-CrcSafePath -Path $OutputPath -Label 'publication output' }
    $review = Get-Content -LiteralPath (Resolve-CrcSafePath -Path $ReviewPath -Label 'review input' -MustExist) -Raw | ConvertFrom-Json
    Assert-Review -Review $review
    $marker=Get-Marker -Review $review
    $body=Get-Body -Review $review -Marker $marker -MarkdownPath $Markdown
    if ($body.Length -gt 60000) { throw 'publication body exceeds 60,000 characters' }
    if ($Provider -eq 'mock') {
        if ($Execute) { throw '--Execute is only valid for the GitHub provider' }
        if (-not $LedgerPath) { throw 'mock provider requires --Ledger' }
        $ledgerPath=Resolve-CrcSafePath -Path $LedgerPath -Label 'ledger'
        $ledger=if(Test-Path -LiteralPath $ledgerPath){Get-Content -LiteralPath $ledgerPath -Raw | ConvertFrom-Json}else{[pscustomobject]@{schema_version=$script:CrcSchemaVersion;published=@()}}
        $existing=@($ledger.published | Where-Object marker -eq $marker)
        if($existing.Count -gt 0){$result=[ordered]@{status='already-published';provider='mock';idempotency_key=($marker -replace '^.*v1:([0-9a-f]+).*$','$1')}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
        $ledger.published=@($ledger.published)+([pscustomobject]@{marker=$marker;snapshot_id=$review.snapshot_id;status=$review.status;body=$body}); Write-CrcJson -Path $ledgerPath -Value $ledger
        $result=[ordered]@{status='published';provider='mock';idempotency_key=($marker -replace '^.*v1:([0-9a-f]+).*$','$1')}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0
    }
    if ($Repo -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$' -or $Pr -lt 1) { throw 'GitHub provider requires a valid --Repo and positive --Pr' }
    $scope=$review.scope_manifest; if($scope.base -notmatch '^[0-9a-f]{40}$' -or $scope.head -notmatch '^[0-9a-f]{40}$'){throw 'scope lacks full base/head SHAs'}
    if($ExpectedBase -and $ExpectedBase -ne $scope.base){throw '--ExpectedBase differs from reviewed scope'}; if($ExpectedHead -and $ExpectedHead -ne $scope.head){throw '--ExpectedHead differs from reviewed scope'}
    if(-not $Execute){$result=[ordered]@{status='dry-run';provider='github';requires_revalidation=$true;marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
    $live=Assert-LivePullRequest -Repository $Repo -Number $Pr -Base $scope.base -Head $scope.head
    $files=@(Invoke-GhApiJson -Arguments @("repos/$Repo/pulls/$Pr/files",'--paginate','--slurp') | ForEach-Object { $_ }); Test-LiveAnchors -Review $review -Files $files
    $publisher=(Invoke-GhApiJson -Arguments @('user')).login; $comments=@(Invoke-GhApiJson -Arguments @("repos/$Repo/issues/$Pr/comments",'--paginate','--slurp') | ForEach-Object { $_ }); if(@($comments | Where-Object { $_.user.login -eq $publisher -and $_.body -like "*$marker*" }).Count -gt 0){$result=[ordered]@{status='already-published';provider='github';marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
    $null=Assert-LivePullRequest -Repository $Repo -Number $Pr -Base $scope.base -Head $scope.head
    $null=Invoke-GhApiJson -Arguments @("repos/$Repo/issues/$Pr/comments",'--method','POST','--input','-') -Body ([ordered]@{body=$body}); $result=[ordered]@{status='published';provider='github';marker=$marker}; if($OutputPath){Write-CrcJson -Path $OutputPath -Value $result}; $result|ConvertTo-Json -Compress; exit 0
}
catch { $result=[ordered]@{status='blocked';error=$_.Exception.Message}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 2 }
