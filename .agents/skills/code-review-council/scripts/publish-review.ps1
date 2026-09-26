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

function Invoke-GhApiText { param([string[]]$Arguments,[object]$Body)
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
    return ($result -join "`n")
}

function Invoke-GhApiJson { param([string[]]$Arguments,[object]$Body)
    $text = Invoke-GhApiText -Arguments $Arguments -Body $Body
    return ($text | ConvertFrom-Json)
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

function Assert-PublicationPathPairDistinct { param([object]$Left,[object]$Right)
    if (Test-CrcPathEqual -Left $Left.path -Right $Right.path) { throw "publication paths must not alias: $($Left.name) and $($Right.name)" }
}

function Assert-PublicationPathsDistinct { param([object[]]$Paths)
    for ($leftIndex = 0; $leftIndex -lt $Paths.Count; $leftIndex++) {
        for ($rightIndex = $leftIndex + 1; $rightIndex -lt $Paths.Count; $rightIndex++) {
            Assert-PublicationPathPairDistinct -Left $Paths[$leftIndex] -Right $Paths[$rightIndex]
        }
    }
}

function Assert-ReviewEnvelope { param([object]$Review)
    foreach ($name in @('schema_version','status','snapshot_id','scope_manifest','reviewers','findings','dispositions','execution','publication','errors')) {
        if ($null -eq $Review.PSObject.Properties[$name]) { throw "review.$name is required" }
    }
    if ($Review.schema_version -ne $script:CrcSchemaVersion) { throw 'review schema_version is invalid' }
    if ($Review.status -cnotin @('PASS','BLOCKED')) { throw 'only PASS or BLOCKED results can be published' }
    if ($Review.snapshot_id -notmatch '^sha256:[0-9a-f]{64}$') { throw 'review snapshot_id is invalid' }
    if ($Review.errors -isnot [System.Array] -or $Review.errors.Count -gt 0) { throw 'review contains validation errors or errors is not an array' }
    if ($Review.publication -isnot [System.Management.Automation.PSCustomObject] -and $Review.publication -isnot [System.Collections.IDictionary]) { throw 'review.publication must be an object' }
    if ([string]::IsNullOrWhiteSpace([string]$Review.publication.status) -or $Review.publication.status -cnotin @('not-requested','dry-run','published','already-published')) { throw 'review publication status is invalid' }
}

function Assert-ReviewScopeHeader { param([object]$Scope)
    foreach ($name in @('schema_version','mode','status','snapshot_id','repository','captured_at_utc','snapshot_material')) {
        if ($null -eq $Scope.PSObject.Properties[$name]) { throw "review.scope_manifest.$name is required" }
    }
    if ($Scope.schema_version -ne $script:CrcSchemaVersion -or $Scope.mode -cnotin @('codebase','branch','worktree','pull-request') -or $Scope.status -cne 'READY') { throw 'publishable review requires a READY scope manifest' }
}

function Assert-ReviewScopeSnapshot { param([object]$Review,[object]$Scope)
    if ($Scope.snapshot_id -ne $Review.snapshot_id) { throw 'review snapshot_id does not match nested scope' }
    if ((Get-CrcHashJson -Value $Scope.snapshot_material) -ne $Scope.snapshot_id) { throw 'review scope snapshot digest is invalid' }
    if ($Scope.snapshot_material.mode -ne $Scope.mode -or $Scope.snapshot_material.status -ne $Scope.status) { throw 'review scope status or mode differs from hashed material' }
}

function Assert-ReviewScopeMirrors { param([object]$Scope)
    foreach ($name in @('revision','base','head','merge_base','changed_files','files','patch','statuses','selected','unresolved_index','dirty_worktree','dirty_status','warnings')) {
        if ($null -ne $Scope.snapshot_material.PSObject.Properties[$name] -and $null -ne $Scope.PSObject.Properties[$name]) {
            if ((ConvertTo-CrcJson -Value $Scope.snapshot_material.$name) -ne (ConvertTo-CrcJson -Value $Scope.$name)) { throw "review scope mirrored field differs: $name" }
        }
    }
}

function Assert-ReviewBranchPatchBinding { param([object]$Scope)
    if ($Scope.mode -ne 'branch') { return }
    foreach ($object in @($Scope.snapshot_material,$Scope)) {
        if ($null -eq $object.PSObject.Properties['patch']) { throw 'branch scope patch is required in the hashed material and manifest' }
    }
    if ((ConvertTo-CrcJson -Value $Scope.snapshot_material.patch) -ne (ConvertTo-CrcJson -Value $Scope.patch)) { throw 'branch scope patch differs from hashed material' }
}

function Assert-ReviewPullRequestMirror { param([object]$Scope)
    if ($null -ne $Scope.snapshot_material.PSObject.Properties['snapshot']) {
        if ($null -eq $Scope.PSObject.Properties['pull_request'] -or (ConvertTo-CrcJson -Value $Scope.pull_request) -ne (ConvertTo-CrcJson -Value $Scope.snapshot_material.snapshot)) { throw 'review pull-request snapshot differs from hashed material' }
    }
}

function Assert-ReviewScope { param([object]$Review)
    $scope = $Review.scope_manifest
    Assert-ReviewScopeHeader -Scope $scope
    Assert-ReviewScopeSnapshot -Review $Review -Scope $scope
    Assert-ReviewScopeMirrors -Scope $scope
    Assert-ReviewBranchPatchBinding -Scope $scope
    Assert-ReviewPullRequestMirror -Scope $scope
}

function Assert-ReviewerRequiredFields { param([object]$Reviewer)
    foreach ($name in @('review_id','persona_id','snapshot_id','status','requested_model','effective_model','requested_concurrency','effective_concurrency','completed_at_utc','findings')) {
        if ($null -eq $Reviewer.PSObject.Properties[$name]) { throw "reviewer.$name is required" }
    }
}

function Assert-ReviewerIdentity { param([object]$Reviewer,[string]$SnapshotId)
    foreach ($name in @('review_id','persona_id','snapshot_id','status','completed_at_utc')) {
        if ([string]::IsNullOrWhiteSpace([string]$Reviewer.$name)) { throw "reviewer.$name must be non-empty" }
    }
    if ($Reviewer.persona_id -cnotin $script:CrcPersonas) { throw 'reviewer persona is unknown' }
    if ($Reviewer.snapshot_id -ne $SnapshotId) { throw "reviewer snapshot differs from review: $($Reviewer.review_id)" }
}

function Assert-ReviewerCompletion { param([object]$Reviewer)
    if ($Reviewer.status -cnotin @('complete','not_applicable')) { throw "reviewer is not complete: $($Reviewer.review_id)" }
    if ($Reviewer.findings -isnot [System.Array]) { throw "reviewer findings must be an array: $($Reviewer.review_id)" }
    if ($Reviewer.status -eq 'not_applicable' -and [string]::IsNullOrWhiteSpace([string]$Reviewer.reason)) { throw "not_applicable reviewer requires a reason: $($Reviewer.review_id)" }
    if ($Reviewer.status -eq 'not_applicable' -and $Reviewer.findings.Count -gt 0) { throw "not_applicable reviewer cannot contain findings: $($Reviewer.review_id)" }
}

function Assert-ReviewerModels { param([object]$Reviewer)
    if ($null -ne $Reviewer.requested_model -and $Reviewer.requested_model -isnot [string]) { throw "requested model is invalid: $($Reviewer.review_id)" }
    if ($null -ne $Reviewer.effective_model -and $Reviewer.effective_model -isnot [string]) { throw "effective model is invalid: $($Reviewer.review_id)" }
    if ($null -ne $Reviewer.requested_model -and $null -ne $Reviewer.effective_model -and $Reviewer.requested_model -ne $Reviewer.effective_model) { throw "reviewer model differs from requested model: $($Reviewer.review_id)" }
}

function Assert-ReviewerConcurrency { param([object]$Reviewer)
    foreach ($name in @('requested_concurrency','effective_concurrency')) {
        $value = $Reviewer.$name
        if ($null -ne $value -and (($value -isnot [int] -and $value -isnot [long]) -or $value -lt 1)) { throw "reviewer concurrency is invalid: $($Reviewer.review_id)" }
    }
}

function Assert-ReviewerRecord { param([object]$Reviewer,[string]$SnapshotId)
    Assert-ReviewerRequiredFields -Reviewer $Reviewer
    Assert-ReviewerIdentity -Reviewer $Reviewer -SnapshotId $SnapshotId
    Assert-ReviewerCompletion -Reviewer $Reviewer
    Assert-ReviewerModels -Reviewer $Reviewer
    Assert-ReviewerConcurrency -Reviewer $Reviewer
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

function Add-ReviewerFindingToGroups { param([object]$Finding,[System.Collections.Generic.Dictionary[string,object]]$Groups)
    $fingerprint = [string]$Finding.fingerprint
    if (-not $Groups.ContainsKey($fingerprint)) {
        $Groups[$fingerprint] = [pscustomobject]@{
            severities=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            persona_ids=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            evidence=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            change_relations=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }
    }
    $group = $Groups[$fingerprint]
    [void]$group.severities.Add([string]$Finding.severity)
    foreach ($personaId in $Finding.persona_ids) { [void]$group.persona_ids.Add([string]$personaId) }
    foreach ($evidence in $Finding.evidence) { [void]$group.evidence.Add([string]$evidence) }
    [void]$group.change_relations.Add([string]$Finding.change_relation)
}

function Get-ReviewerFindingGroups { param([object]$Review)
    $groups = [System.Collections.Generic.Dictionary[string,object]]::new([System.StringComparer]::Ordinal)
    foreach ($reviewer in $Review.reviewers) { foreach ($finding in $reviewer.findings) { Add-ReviewerFindingToGroups -Finding $finding -Groups $groups } }
    foreach ($fingerprint in $groups.Keys) { if ($groups[$fingerprint].severities.Count -ne 1) { throw "reviewer records have conflicting severity for $fingerprint" } }
    return $groups
}

function Assert-StringArrayMatchesSet { param([object[]]$Actual,[System.Collections.Generic.HashSet[string]]$Expected,[string]$Label)
    $actualSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($value in $Actual) { [void]$actualSet.Add([string]$value) }
    if ($actualSet.Count -ne $Actual.Count -or $actualSet.Count -ne $Expected.Count) { throw "consolidated finding $Label differs from reviewer records" }
    foreach ($value in $Expected) { if (-not $actualSet.Contains($value)) { throw "consolidated finding $Label differs from reviewer records" } }
}

function Assert-ConsolidatedFindingMatchesGroup { param([object]$Finding,[object]$Group)
    $expectedSeverity = @($Group.severities)[0]
    if ($Finding.severity -cne $expectedSeverity) { throw "consolidated finding severity differs from reviewer records: $($Finding.fingerprint)" }
    $expectedRelation = if ($Group.change_relations.Count -eq 1) { @($Group.change_relations)[0] } else { 'unknown' }
    if ($Finding.change_relation -cne $expectedRelation) { throw "consolidated finding change_relation differs from reviewer records: $($Finding.fingerprint)" }
    Assert-StringArrayMatchesSet -Actual $Finding.persona_ids -Expected $Group.persona_ids -Label 'persona_ids'
    Assert-StringArrayMatchesSet -Actual $Finding.evidence -Expected $Group.evidence -Label 'evidence'
}

function Assert-ReviewConsolidatedFindings { param([object]$Review)
    if ($Review.findings -isnot [System.Array]) { throw 'review findings must be an array' }
    $groups = Get-ReviewerFindingGroups -Review $Review
    if ($groups.Count -ne $Review.findings.Count) { throw 'consolidated findings do not match reviewer fingerprint set' }
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($finding in $Review.findings) {
        $fingerprint = [string]$finding.fingerprint
        if (-not $groups.ContainsKey($fingerprint) -or -not $seen.Add($fingerprint)) { throw "consolidated finding has no unique reviewer evidence: $fingerprint" }
        Assert-ConsolidatedFindingMatchesGroup -Finding $finding -Group $groups[$fingerprint]
    }
}

function Assert-FindingRequiredFields { param([object]$Finding,[switch]$RequireDisposition)
    $required = @('fingerprint','persona_ids','category','severity','snapshot_id','path','line','scenario','trigger','impact','evidence','remediation','uncertainty','change_relation')
    if ($RequireDisposition) { $required += 'disposition' }
    foreach ($name in $required) {
        if ($null -eq $Finding.PSObject.Properties[$name]) { throw "finding.$name is required" }
    }
}

function Assert-FindingIdentityAndPersonas { param([object]$Finding,[string]$SnapshotId)
    if ($Finding.fingerprint -notmatch '^sha256:[0-9a-f]{64}$' -or $Finding.snapshot_id -ne $SnapshotId) { throw 'finding fingerprint or snapshot is invalid' }
    if ($Finding.persona_ids -isnot [System.Array] -or $Finding.persona_ids.Count -eq 0) { throw "finding persona_ids must be a non-empty array: $($Finding.fingerprint)" }
    if (@($Finding.persona_ids | Where-Object { $_ -cnotin $script:CrcPersonas }).Count -gt 0) { throw "finding persona_ids contain an unknown persona: $($Finding.fingerprint)" }
}

function Assert-FindingTextFields { param([object]$Finding)
    foreach ($name in @('category','path','scenario','trigger','impact','remediation','uncertainty')) {
        if ([string]::IsNullOrWhiteSpace([string]$Finding.$name)) { throw "finding.$name must be non-empty: $($Finding.fingerprint)" }
    }
}

function Assert-FindingClassification { param([object]$Finding)
    if ($Finding.severity -cnotin @('P0','P1','P2','P3') -or $Finding.change_relation -cnotin @('introduced','worsened','pre-existing','out-of-scope','unknown')) { throw "finding classification is invalid: $($Finding.fingerprint)" }
}

function Assert-FindingAnchorAndEvidence { param([object]$Finding)
    if ($Finding.line -isnot [int] -and $Finding.line -isnot [long]) { throw "finding line must be an integer: $($Finding.fingerprint)" }
    if ($Finding.line -lt 1 -or $Finding.evidence -isnot [System.Array] -or $Finding.evidence.Count -eq 0) { throw "finding anchor or evidence is invalid: $($Finding.fingerprint)" }
}

function Assert-FindingDisposition { param([object]$Finding)
    if ($null -ne $Finding.PSObject.Properties['disposition'] -and $Finding.disposition -cnotin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved')) { throw "finding disposition is invalid: $($Finding.fingerprint)" }
}

function Assert-FindingRecord { param([object]$Finding,[string]$SnapshotId,[switch]$RequireDisposition)
    Assert-FindingRequiredFields -Finding $Finding -RequireDisposition:$RequireDisposition
    Assert-FindingIdentityAndPersonas -Finding $Finding -SnapshotId $SnapshotId
    Assert-FindingTextFields -Finding $Finding
    Assert-FindingClassification -Finding $Finding
    Assert-FindingAnchorAndEvidence -Finding $Finding
    Assert-FindingDisposition -Finding $Finding
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

function Assert-DuplicateDispositionChain { param([object]$Entry,[hashtable]$ByFingerprint,[System.Collections.Generic.HashSet[string]]$FindingIds)
    if ($Entry.disposition -ne 'duplicate') { return }
    if ([string]::IsNullOrWhiteSpace([string]$Entry.duplicate_of) -or -not $FindingIds.Contains([string]$Entry.duplicate_of)) { throw "duplicate disposition target is missing: $($Entry.fingerprint)" }
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $current = [string]$Entry.fingerprint
    while ($ByFingerprint.ContainsKey($current) -and $ByFingerprint[$current].disposition -eq 'duplicate') {
        if (-not $seen.Add($current)) { throw "duplicate disposition cycle includes $($Entry.fingerprint)" }
        $current = [string]$ByFingerprint[$current].duplicate_of
    }
    if (-not $FindingIds.Contains($current) -or ($ByFingerprint.ContainsKey($current) -and $ByFingerprint[$current].disposition -eq 'duplicate')) { throw "duplicate disposition has no canonical target: $($Entry.fingerprint)" }
}

function Assert-DuplicateDispositions { param([object[]]$Entries,[System.Collections.Generic.HashSet[string]]$FindingIds)
    $byFingerprint = @{}
    foreach ($entry in $Entries) { $byFingerprint[[string]$entry.fingerprint] = $entry }
    foreach ($entry in $Entries) { Assert-DuplicateDispositionChain -Entry $entry -ByFingerprint $byFingerprint -FindingIds $FindingIds }
}

function Assert-ReviewDispositionEntry { param([object]$Entry,[object]$Review)
    foreach ($name in @('fingerprint','disposition','rationale','snapshot_id')) { if ($null -eq $Entry.PSObject.Properties[$name]) { throw "disposition.$name is required" } }
    if ($Entry.snapshot_id -ne $Review.snapshot_id -or $Entry.disposition -cnotin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved') -or [string]::IsNullOrWhiteSpace([string]$Entry.rationale)) { throw "disposition is invalid: $($Entry.fingerprint)" }
}

function Assert-ReviewDispositionFinding { param([object]$Entry,[object]$Review,[System.Collections.Generic.HashSet[string]]$FindingIds,[System.Collections.Generic.HashSet[string]]$DispositionIds)
    $fingerprint = [string]$Entry.fingerprint
    if (-not $FindingIds.Contains($fingerprint) -or -not $DispositionIds.Add($fingerprint)) { throw "disposition fingerprint is unknown or duplicated: $fingerprint" }
    $finding = $Review.findings | Where-Object fingerprint -eq $fingerprint | Select-Object -First 1
    if ($finding.disposition -ne $Entry.disposition) { throw "finding and disposition disagree: $fingerprint" }
}

function Assert-ReviewDispositions { param([object]$Review,[System.Collections.Generic.HashSet[string]]$FindingIds)
    if ($Review.dispositions -isnot [System.Array]) { throw 'review dispositions must be an array' }
    $dispositionIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in $Review.dispositions) {
        Assert-ReviewDispositionEntry -Entry $entry -Review $Review
        Assert-ReviewDispositionFinding -Entry $entry -Review $Review -FindingIds $FindingIds -DispositionIds $dispositionIds
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
    Assert-ReviewConsolidatedFindings -Review $Review
    Assert-ReviewDispositions -Review $Review -FindingIds $findingIds
    Assert-ReviewExecution -Review $Review
    Assert-ReviewStatus -Review $Review
}

function Assert-LivePullRequest { param([string]$Repository,[int]$Number,[string]$Base,[string]$Head)
    $live = Invoke-GhApiJson -Arguments @("repos/$Repository/pulls/$Number")
    if ($live.state -ne 'open' -or $live.base.sha -ne $Base -or $live.head.sha -ne $Head) { throw 'live PR state or base/head differs from reviewed scope' }
    return $live
}

function Test-ExistingPublication { param([object]$Comment,[string]$Marker,[string]$ExpectedBody)
    if ($null -eq $Comment.body) { return $false }
    $body = [string]$Comment.body
    return $body.Contains($Marker,[System.StringComparison]::Ordinal) -and $body.TrimEnd() -ceq $ExpectedBody.TrimEnd()
}

function Assert-GitHubReviewMode { param([object]$Scope,[string]$ExpectedBase,[string]$ExpectedHead)
    if ($Scope.mode -ne 'pull-request') { throw 'GitHub publication requires a pull-request scope snapshot' }
    if ([string]::IsNullOrWhiteSpace($ExpectedBase) -or [string]::IsNullOrWhiteSpace($ExpectedHead)) { throw 'GitHub publication requires expected base and head SHAs' }
}

function Get-GitHubPullRequestSnapshot { param([object]$Scope)
    if ($null -eq $Scope.snapshot_material.PSObject.Properties['snapshot']) { throw 'hashed pull-request scope snapshot is required for GitHub publication' }
    if ($null -eq $Scope.PSObject.Properties['pull_request']) { throw 'pull-request manifest is required for GitHub publication' }
    $snapshot = $Scope.snapshot_material.snapshot
    if ((ConvertTo-CrcJson -Value $Scope.pull_request) -ne (ConvertTo-CrcJson -Value $snapshot)) { throw 'pull-request manifest differs from its hashed snapshot' }
    return $snapshot
}

function Assert-GitHubPullRequestIdentity { param([object]$Snapshot,[string]$Repository,[int]$Number)
    if ($snapshot.repository -isnot [string] -or [string]::IsNullOrWhiteSpace($snapshot.repository)) { throw 'pull-request scope lacks repository identity' }
    if (-not $Snapshot.repository.Equals($Repository,[System.StringComparison]::OrdinalIgnoreCase)) { throw 'requested repository differs from reviewed pull-request snapshot' }
    if ($Snapshot.number -isnot [int] -and $Snapshot.number -isnot [long]) { throw 'pull-request scope number must be an integer' }
    if ([int]$Snapshot.number -ne $Number) { throw 'requested pull request differs from reviewed snapshot' }
}

function Assert-GitHubPullRequestRevisions { param([object]$Snapshot,[object]$Scope,[string]$ExpectedBase,[string]$ExpectedHead)
    $objectIdPattern = '^(?:[0-9a-f]{40}|[0-9a-f]{64})$'
    if ($Snapshot.base_sha -notmatch $objectIdPattern -or $Snapshot.head_sha -notmatch $objectIdPattern) { throw 'pull-request snapshot lacks full base/head SHAs' }
    if ($Scope.base -ne $Snapshot.base_sha -or $Scope.head -ne $Snapshot.head_sha) { throw 'pull-request manifest revisions differ from its hashed snapshot' }
    if ($ExpectedBase -ne $Scope.base -or $ExpectedHead -ne $Scope.head) { throw 'expected base/head differs from reviewed scope' }
}

function Assert-GitHubReviewTarget { param([object]$Review,[string]$Repository,[int]$Number,[string]$ExpectedBase,[string]$ExpectedHead)
    $scope = $Review.scope_manifest
    Assert-GitHubReviewMode -Scope $scope -ExpectedBase $ExpectedBase -ExpectedHead $ExpectedHead
    $snapshot = Get-GitHubPullRequestSnapshot -Scope $scope
    Assert-GitHubPullRequestIdentity -Snapshot $snapshot -Repository $Repository -Number $Number
    Assert-GitHubPullRequestRevisions -Snapshot $snapshot -Scope $scope -ExpectedBase $ExpectedBase -ExpectedHead $ExpectedHead
}

function Get-LiveFileMap { param([object[]]$Files)
    $byPath = [System.Collections.Generic.Dictionary[string,object]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $Files) { Add-LiveFilePaths -PathMap $byPath -File $file }
    return $byPath
}

function Add-LiveDiffFilePath { param([object]$File,[string]$PropertyName,[System.Collections.Generic.HashSet[string]]$Paths)
    $property = $File.PSObject.Properties[$PropertyName]
    if ($null -eq $property) { return }
    $path = [string]$property.Value
    if (-not $path) { return }
    [void]$Paths.Add((Get-CrcPath -Path $path))
}

function Get-LiveDiffFilePaths { param([object[]]$Files)
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $Files) {
        Add-LiveDiffFilePath -File $file -PropertyName 'filename' -Paths $paths
        Add-LiveDiffFilePath -File $file -PropertyName 'previous_filename' -Paths $paths
    }
    return ,$paths
}

function Assert-LiveDiffPathSetsMatch { param([System.Collections.Generic.HashSet[string]]$FilePaths,[System.Collections.Generic.HashSet[string]]$DiffPaths)
    if ($FilePaths.Count -eq 0 -or $DiffPaths.Count -ne $FilePaths.Count) { throw 'live diff headers do not match the complete PR file list' }
    foreach ($path in $FilePaths) { if (-not $DiffPaths.Contains($path)) { throw "live diff is missing PR file $path" } }
}

function Assert-LiveDiffMatchesFiles { param([string]$Diff,[object[]]$Files)
    if ($Files.Count -ge 3000) { throw 'GitHub file-list limit prevents complete live-diff validation' }
    $filePaths = Get-LiveDiffFilePaths -Files $Files
    $diffPaths = Get-CrcDiffHeaderPaths -DiffText $Diff
    Assert-LiveDiffPathSetsMatch -FilePaths $filePaths -DiffPaths $diffPaths
}

function Add-LiveFilePaths { param([System.Collections.Generic.Dictionary[string,object]]$PathMap,[object]$File)
    $filename = if ($null -ne $File.PSObject.Properties['filename']) { [string]$File.filename } else { '' }
    $previous = if ($null -ne $File.PSObject.Properties['previous_filename']) { [string]$File.previous_filename } else { '' }
    if ($filename) { $PathMap[$filename] = $File }
    if ($previous) { $PathMap[$previous] = $File }
}

function Assert-LiveFindingAnchor { param([object]$Finding,[System.Collections.Generic.Dictionary[string,object]]$PathMap,[string]$FullDiff)
    $path = [string]$Finding.path
    if (-not $PathMap.ContainsKey($path)) { throw "finding path missing from live diff: $path`:$($Finding.line)" }
    if ([string]::IsNullOrWhiteSpace($FullDiff)) { throw 'complete live pull-request diff is unavailable' }
    if (-not (Test-CrcPatchContainsLine -Patch $FullDiff -Path $path -Line ([int]$Finding.line))) { throw "finding line missing from complete live diff: $path`:$($Finding.line)" }
}

function Test-LiveAnchors { param([object]$Review,[object[]]$Files,[string]$FullDiff)
    if ($Files.Count -ge 3000) { throw 'GitHub file-list limit prevents complete live-anchor validation' }
    Assert-LiveDiffMatchesFiles -Diff $FullDiff -Files $Files
    $byPath = Get-LiveFileMap -Files $Files
    foreach ($finding in $Review.findings) { Assert-LiveFindingAnchor -Finding $finding -PathMap $byPath -FullDiff $FullDiff }
}

$outputSafePath = $null
$ledgerSafePath = $null
try {
    $reviewInputPath = Resolve-CrcSafePath -Path $ReviewPath -Label 'review input' -MustExist
    $pathEntries = [System.Collections.Generic.List[object]]::new()
    $pathEntries.Add([pscustomobject]@{name='review input';path=$reviewInputPath})
    if ($LedgerPath) {
        $ledgerSafePath = Resolve-CrcSafePath -Path $LedgerPath -Label 'ledger'
        $pathEntries.Add([pscustomobject]@{name='ledger';path=$ledgerSafePath})
    }
    if ($Markdown) {
        $markdownInputPath = Resolve-CrcSafePath -Path $Markdown -Label 'Markdown input' -MustExist
        $pathEntries.Add([pscustomobject]@{name='Markdown input';path=$markdownInputPath})
    }
    if ($OutputPath) {
        $outputCandidatePath = Resolve-CrcSafePath -Path $OutputPath -Label 'publication output'
        $pathEntries.Add([pscustomobject]@{name='output';path=$outputCandidatePath})
    }
    Assert-PublicationPathsDistinct -Paths $pathEntries.ToArray()
    if ($OutputPath) { $outputSafePath = Assert-CrcWritableOutputPath -Path $outputCandidatePath }
    $review = Get-Content -LiteralPath $reviewInputPath -Raw | ConvertFrom-Json
    Assert-Review -Review $review
    $marker=Get-Marker -Review $review
    $body=Get-Body -Review $review -Marker $marker -MarkdownPath $Markdown
    if ($body.Length -gt 60000) { throw 'publication body exceeds 60,000 characters' }
    if ($Provider -eq 'mock') {
        if ($Execute) { throw '--Execute is only valid for the GitHub provider' }
        if (-not $LedgerPath) { throw 'mock provider requires --Ledger' }
        $ledgerPath=$ledgerSafePath
        $ledger=if(Test-Path -LiteralPath $ledgerPath){Get-Content -LiteralPath $ledgerPath -Raw | ConvertFrom-Json}else{[pscustomobject]@{schema_version=$script:CrcSchemaVersion;published=@()}}
        $existing=@($ledger.published | Where-Object marker -eq $marker)
        if($existing.Count -gt 0){$result=[ordered]@{status='already-published';provider='mock';idempotency_key=($marker -replace '^.*v1:([0-9a-f]+).*$','$1')}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
        $ledger.published=@($ledger.published)+([pscustomobject]@{marker=$marker;snapshot_id=$review.snapshot_id;status=$review.status;body=$body}); Write-CrcJson -Path $ledgerPath -Value $ledger
        $result=[ordered]@{status='published';provider='mock';idempotency_key=($marker -replace '^.*v1:([0-9a-f]+).*$','$1')}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0
    }
    if ($Repo -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$' -or $Pr -lt 1) { throw 'GitHub provider requires a valid --Repo and positive --Pr' }
    Assert-GitHubReviewTarget -Review $review -Repository $Repo -Number $Pr -ExpectedBase $ExpectedBase -ExpectedHead $ExpectedHead
    $scope=$review.scope_manifest
    if(-not $Execute){$result=[ordered]@{status='dry-run';provider='github';requires_revalidation=$true;marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
    $live=Assert-LivePullRequest -Repository $Repo -Number $Pr -Base $scope.base -Head $scope.head
    $files=@(Invoke-GhApiJson -Arguments @("repos/$Repo/pulls/$Pr/files",'--paginate','--slurp') | ForEach-Object { $_ })
    if ($files.Count -ge 3000) { throw 'GitHub file-list limit prevents complete live-anchor validation' }
    $fullDiff = Invoke-GhApiText -Arguments @('-H','Accept: application/vnd.github.diff',"repos/$Repo/pulls/$Pr")
    if ([string]::IsNullOrWhiteSpace($fullDiff)) { throw 'GitHub returned an empty complete pull-request diff' }
    $null = Assert-LivePullRequest -Repository $Repo -Number $Pr -Base $scope.base -Head $scope.head
    Test-LiveAnchors -Review $review -Files $files -FullDiff $fullDiff
    $comments=@(Invoke-GhApiJson -Arguments @("repos/$Repo/issues/$Pr/comments",'--paginate','--slurp') | ForEach-Object { $_ }); if(@($comments | Where-Object { Test-ExistingPublication -Comment $_ -Marker $marker -ExpectedBody $body }).Count -gt 0){$result=[ordered]@{status='already-published';provider='github';marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0}
    $null=Assert-LivePullRequest -Repository $Repo -Number $Pr -Base $scope.base -Head $scope.head
    $null=Invoke-GhApiJson -Arguments @("repos/$Repo/issues/$Pr/comments",'--method','POST','--input','-') -Body ([ordered]@{body=$body}); $result=[ordered]@{status='published';provider='github';marker=$marker}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 0
}
catch { $result=[ordered]@{status='blocked';error=$_.Exception.Message}; if($outputSafePath){Write-CrcJson -Path $outputSafePath -Value $result}; $result|ConvertTo-Json -Compress; exit 2 }
