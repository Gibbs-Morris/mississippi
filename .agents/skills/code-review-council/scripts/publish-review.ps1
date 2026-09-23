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

function Assert-CrcWritableOutputPath { param([string]$Path)
    $safePath = Resolve-CrcSafePath -Path $Path -Label 'publication output'
    $parent = Split-Path -Parent $safePath
    if ($parent -and -not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    $existing = Get-Item -LiteralPath $safePath -Force -ErrorAction SilentlyContinue
    if ($null -ne $existing -and $existing.PSIsContainer) { throw 'publication output path is a directory' }
    if ($null -ne $existing -and ($existing.Attributes -band [System.IO.FileAttributes]::ReadOnly) -ne 0) { throw 'publication output path is read-only' }
    $created = $false
    $stream = $null
    try {
        if ($null -eq $existing) {
            $stream = [System.IO.File]::Open($safePath,[System.IO.FileMode]::CreateNew,[System.IO.FileAccess]::Write,[System.IO.FileShare]::None)
            $created = $true
        }
        else {
            $stream = [System.IO.File]::Open($safePath,[System.IO.FileMode]::Open,[System.IO.FileAccess]::Write,[System.IO.FileShare]::Read)
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
        if ($created -and (Test-Path -LiteralPath $safePath -PathType Leaf)) { Remove-Item -LiteralPath $safePath -Force }
    }
    return $safePath
}

function Assert-ReviewEnvelope { param([object]$Review)
    foreach ($name in @('schema_version','status','snapshot_id','scope_manifest','reviewers','findings','dispositions','execution','publication','errors')) {
        if ($null -eq $Review.PSObject.Properties[$name]) { throw "review.$name is required" }
    }
    if ($Review.schema_version -ne $script:CrcSchemaVersion) { throw 'review schema_version is invalid' }
    if ($Review.status -notin @('PASS','BLOCKED')) { throw 'only PASS or BLOCKED results can be published' }
    if ($Review.snapshot_id -notmatch '^sha256:[0-9a-f]{64}$') { throw 'review snapshot_id is invalid' }
    if ($Review.errors -isnot [System.Array] -or $Review.errors.Count -gt 0) { throw 'review contains validation errors or errors is not an array' }
    if ($Review.publication -isnot [System.Management.Automation.PSCustomObject] -and $Review.publication -isnot [System.Collections.IDictionary]) { throw 'review.publication must be an object' }
    if ([string]::IsNullOrWhiteSpace([string]$Review.publication.status) -or $Review.publication.status -notin @('not-requested','dry-run','published','already-published')) { throw 'review publication status is invalid' }
}

function Assert-ReviewScope { param([object]$Review)
    $scope = $Review.scope_manifest
    foreach ($name in @('schema_version','mode','status','snapshot_id','repository','captured_at_utc','snapshot_material')) {
        if ($null -eq $scope.PSObject.Properties[$name]) { throw "review.scope_manifest.$name is required" }
    }
    if ($scope.schema_version -ne $script:CrcSchemaVersion -or $scope.mode -notin @('codebase','branch','worktree','pull-request') -or $scope.status -ne 'READY') { throw 'publishable review requires a READY scope manifest' }
    if ($scope.snapshot_id -ne $Review.snapshot_id) { throw 'review snapshot_id does not match nested scope' }
    if ((Get-CrcHashJson -Value $scope.snapshot_material) -ne $scope.snapshot_id) { throw 'review scope snapshot digest is invalid' }
    if ($scope.snapshot_material.mode -ne $scope.mode -or $scope.snapshot_material.status -ne $scope.status) { throw 'review scope status or mode differs from hashed material' }
    foreach ($name in @('revision','base','head','merge_base','changed_files','files','statuses','selected','unresolved_index','dirty_worktree','dirty_status','warnings')) {
        if ($null -ne $scope.snapshot_material.PSObject.Properties[$name] -and $null -ne $scope.PSObject.Properties[$name]) {
            if ((ConvertTo-CrcJson -Value $scope.snapshot_material.$name) -ne (ConvertTo-CrcJson -Value $scope.$name)) { throw "review scope mirrored field differs: $name" }
        }
    }
    if ($null -ne $scope.snapshot_material.PSObject.Properties['snapshot']) {
        if ($null -eq $scope.PSObject.Properties['pull_request'] -or (ConvertTo-CrcJson -Value $scope.pull_request) -ne (ConvertTo-CrcJson -Value $scope.snapshot_material.snapshot)) { throw 'review pull-request snapshot differs from hashed material' }
    }
}

function Assert-ReviewerRecord { param([object]$Reviewer,[string]$SnapshotId)
    foreach ($name in @('review_id','persona_id','snapshot_id','status','requested_model','effective_model','requested_concurrency','effective_concurrency','completed_at_utc','findings')) {
        if ($null -eq $Reviewer.PSObject.Properties[$name]) { throw "reviewer.$name is required" }
    }
    foreach ($name in @('review_id','persona_id','snapshot_id','status','completed_at_utc')) {
        if ([string]::IsNullOrWhiteSpace([string]$Reviewer.$name)) { throw "reviewer.$name must be non-empty" }
    }
    if ($Reviewer.persona_id -notin $script:CrcPersonas) { throw 'reviewer persona is unknown' }
    if ($Reviewer.snapshot_id -ne $SnapshotId) { throw "reviewer snapshot differs from review: $($Reviewer.review_id)" }
    if ($Reviewer.status -notin @('complete','not_applicable')) { throw "reviewer is not complete: $($Reviewer.review_id)" }
    if ($Reviewer.findings -isnot [System.Array]) { throw "reviewer findings must be an array: $($Reviewer.review_id)" }
    if ($Reviewer.status -eq 'not_applicable' -and [string]::IsNullOrWhiteSpace([string]$Reviewer.reason)) { throw "not_applicable reviewer requires a reason: $($Reviewer.review_id)" }
    if ($Reviewer.status -eq 'not_applicable' -and $Reviewer.findings.Count -gt 0) { throw "not_applicable reviewer cannot contain findings: $($Reviewer.review_id)" }
    if ($null -ne $Reviewer.requested_model -and $Reviewer.requested_model -isnot [string]) { throw "requested model is invalid: $($Reviewer.review_id)" }
    if ($null -ne $Reviewer.effective_model -and $Reviewer.effective_model -isnot [string]) { throw "effective model is invalid: $($Reviewer.review_id)" }
    if ($null -ne $Reviewer.requested_model -and $null -ne $Reviewer.effective_model -and $Reviewer.requested_model -ne $Reviewer.effective_model) { throw "reviewer model differs from requested model: $($Reviewer.review_id)" }
    foreach ($name in @('requested_concurrency','effective_concurrency')) {
        $value = $Reviewer.$name
        if ($null -ne $value -and (($value -isnot [int] -and $value -isnot [long]) -or $value -lt 1)) { throw "reviewer concurrency is invalid: $($Reviewer.review_id)" }
    }
}

function Assert-Reviewers { param([object]$Review)
    if ($Review.reviewers -isnot [System.Array] -or $Review.reviewers.Count -ne $script:CrcPersonas.Count) { throw 'review must contain all ten reviewer records' }
    $personas = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $reviewIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($reviewer in $Review.reviewers) {
        Assert-ReviewerRecord -Reviewer $reviewer -SnapshotId $Review.snapshot_id
        if (-not $personas.Add([string]$reviewer.persona_id)) { throw 'reviewer personas are duplicated' }
        if (-not $reviewIds.Add([string]$reviewer.review_id)) { throw 'reviewer IDs are duplicated' }
        foreach ($finding in $reviewer.findings) {
            Assert-FindingRecord -Finding $finding -SnapshotId $Review.snapshot_id
            if ($reviewer.persona_id -notin $finding.persona_ids) { throw "reviewer finding omits its originating persona: $($finding.fingerprint)" }
        }
    }
}

function Assert-FindingRecord { param([object]$Finding,[string]$SnapshotId,[switch]$RequireDisposition)
    $required = @('fingerprint','persona_ids','category','severity','snapshot_id','path','line','scenario','trigger','impact','evidence','remediation','uncertainty','change_relation')
    if ($RequireDisposition) { $required += 'disposition' }
    foreach ($name in $required) {
        if ($null -eq $Finding.PSObject.Properties[$name]) { throw "finding.$name is required" }
    }
    if ($Finding.fingerprint -notmatch '^sha256:[0-9a-f]{64}$' -or $Finding.snapshot_id -ne $SnapshotId) { throw 'finding fingerprint or snapshot is invalid' }
    if ($Finding.persona_ids -isnot [System.Array] -or $Finding.persona_ids.Count -eq 0) { throw "finding persona_ids must be a non-empty array: $($Finding.fingerprint)" }
    if (@($Finding.persona_ids | Where-Object { $_ -notin $script:CrcPersonas }).Count -gt 0) { throw "finding persona_ids contain an unknown persona: $($Finding.fingerprint)" }
    foreach ($name in @('category','path','scenario','trigger','impact','remediation','uncertainty')) {
        if ([string]::IsNullOrWhiteSpace([string]$Finding.$name)) { throw "finding.$name must be non-empty: $($Finding.fingerprint)" }
    }
    if ($Finding.severity -notin @('P0','P1','P2','P3') -or $Finding.change_relation -notin @('introduced','worsened','pre-existing','out-of-scope','unknown')) { throw "finding classification is invalid: $($Finding.fingerprint)" }
    if ($Finding.line -isnot [int] -and $Finding.line -isnot [long]) { throw "finding line must be an integer: $($Finding.fingerprint)" }
    if ($Finding.line -lt 1 -or $Finding.evidence -isnot [System.Array] -or $Finding.evidence.Count -eq 0) { throw "finding anchor or evidence is invalid: $($Finding.fingerprint)" }
    if ($null -ne $Finding.PSObject.Properties['disposition'] -and $Finding.disposition -notin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved')) { throw "finding disposition is invalid: $($Finding.fingerprint)" }
}

function Get-ReviewFindingIds { param([object]$Review)
    if ($Review.findings -isnot [System.Array]) { throw 'review findings must be an array' }
    $findingIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($finding in $Review.findings) {
        Assert-FindingRecord -Finding $finding -SnapshotId $Review.snapshot_id -RequireDisposition
        if (-not $findingIds.Add([string]$finding.fingerprint)) { throw 'review findings are duplicated' }
    }
    return ,$findingIds
}

function Assert-DuplicateDispositions { param([object[]]$Entries,[System.Collections.Generic.HashSet[string]]$FindingIds)
    $byFingerprint = @{}
    foreach ($entry in $Entries) { $byFingerprint[[string]$entry.fingerprint] = $entry }
    foreach ($entry in $Entries) {
        if ($entry.disposition -ne 'duplicate') { continue }
        if ([string]::IsNullOrWhiteSpace([string]$entry.duplicate_of) -or -not $FindingIds.Contains([string]$entry.duplicate_of)) { throw "duplicate disposition target is missing: $($entry.fingerprint)" }
        $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $current = [string]$entry.fingerprint
        while ($byFingerprint.ContainsKey($current) -and $byFingerprint[$current].disposition -eq 'duplicate') {
            if (-not $seen.Add($current)) { throw "duplicate disposition cycle includes $($entry.fingerprint)" }
            $current = [string]$byFingerprint[$current].duplicate_of
        }
        if (-not $FindingIds.Contains($current) -or ($byFingerprint.ContainsKey($current) -and $byFingerprint[$current].disposition -eq 'duplicate')) { throw "duplicate disposition has no canonical target: $($entry.fingerprint)" }
    }
}

function Assert-ReviewDispositions { param([object]$Review,[System.Collections.Generic.HashSet[string]]$FindingIds)
    if ($Review.dispositions -isnot [System.Array]) { throw 'review dispositions must be an array' }
    $dispositionIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in $Review.dispositions) {
        foreach ($name in @('fingerprint','disposition','rationale','snapshot_id')) { if ($null -eq $entry.PSObject.Properties[$name]) { throw "disposition.$name is required" } }
        if ($entry.snapshot_id -ne $Review.snapshot_id -or $entry.disposition -notin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved') -or [string]::IsNullOrWhiteSpace([string]$entry.rationale)) { throw "disposition is invalid: $($entry.fingerprint)" }
        if (-not $FindingIds.Contains([string]$entry.fingerprint) -or -not $dispositionIds.Add([string]$entry.fingerprint)) { throw "disposition fingerprint is unknown or duplicated: $($entry.fingerprint)" }
        $finding = $Review.findings | Where-Object fingerprint -eq $entry.fingerprint | Select-Object -First 1
        if ($finding.disposition -ne $entry.disposition) { throw "finding and disposition disagree: $($entry.fingerprint)" }
    }
    if ($dispositionIds.Count -ne $FindingIds.Count) { throw 'review does not have one disposition per finding' }
    Assert-DuplicateDispositions -Entries $Review.dispositions -FindingIds $FindingIds
}

function Assert-ReviewExecution { param([object]$Review)
    foreach ($name in @('reviewer_count','required_reviewer_count','adjudicator','adjudicated_at_utc','adjudication_snapshot_id')) {
        if ($null -eq $Review.execution.PSObject.Properties[$name] -or [string]::IsNullOrWhiteSpace([string]$Review.execution.$name)) { throw "review.execution.$name is required" }
    }
    if ([int]$Review.execution.reviewer_count -ne $script:CrcPersonas.Count -or [int]$Review.execution.required_reviewer_count -ne $script:CrcPersonas.Count -or $Review.execution.adjudication_snapshot_id -ne $Review.snapshot_id) { throw 'review execution metadata is incomplete' }
    if ($Review.execution.reviewer_count -isnot [int] -and $Review.execution.reviewer_count -isnot [long]) { throw 'review execution reviewer_count must be an integer' }
    if ($Review.execution.required_reviewer_count -isnot [int] -and $Review.execution.required_reviewer_count -isnot [long]) { throw 'review execution required_reviewer_count must be an integer' }
    if ($Review.execution.adjudication_snapshot_id -notmatch '^sha256:[0-9a-f]{64}$') { throw 'review execution adjudication snapshot is invalid' }
}

function Assert-ReviewStatus { param([object]$Review)
    $blockers = @($Review.findings | Where-Object { $_.disposition -eq 'validated' -and $_.severity -in @('P0','P1') })
    $unresolved = @($Review.findings | Where-Object disposition -eq 'unresolved')
    if ($Review.status -eq 'PASS' -and $blockers.Count -gt 0) { throw 'PASS result contains a validated P0/P1 finding' }
    if ($Review.status -eq 'BLOCKED' -and $blockers.Count -eq 0) { throw 'BLOCKED result has no validated P0/P1 finding' }
    if ($unresolved.Count -gt 0) { throw 'publishable result contains unresolved findings' }
}

function Assert-Review { param([object]$Review)
    Assert-ReviewEnvelope -Review $Review
    Assert-ReviewScope -Review $Review
    Assert-Reviewers -Review $Review
    $findingIds = Get-ReviewFindingIds -Review $Review
    Assert-ReviewDispositions -Review $Review -FindingIds $findingIds
    Assert-ReviewExecution -Review $Review
    Assert-ReviewStatus -Review $Review
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
    if ($OutputPath) { $outputSafePath = Assert-CrcWritableOutputPath -Path $OutputPath }
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
    $null=Invoke-GhApiJson -Arguments @("repos/$Repo/issues/$Pr/comments",'--method','POST','--input','-') -Body ([ordered]@{body=$body}); $result=[ordered]@{status='published';provider='github';marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0
}
catch { $result=[ordered]@{status='blocked';error=$_.Exception.Message}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 2 }
