#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][Alias('Scope')][string]$ScopePath,
    [Parameter(Mandatory)][Alias('Reviewers')][string]$ReviewersPath,
    [Parameter(Mandatory)][Alias('Adjudication')][string]$AdjudicationPath,
    [Parameter(Mandatory)][Alias('Output')][string]$OutputPath,
    [Alias('MarkdownOutput')][string]$MarkdownPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

function Get-JsonInput { param([string]$Path,[string]$Label) $safe = Resolve-CrcSafePath -Path $Path -Label $Label -MustExist; return (Get-Content -LiteralPath $safe -Raw | ConvertFrom-Json) }
function Test-Property { param([object]$Object,[string]$Name) return $null -ne $Object.PSObject.Properties[$Name] }
function New-ReviewerEvidence { param([object]$Reviewer,[object]$Finding)
    return [ordered]@{
        reviewer_id = [string]$Reviewer.review_id
        persona_id = [string]$Reviewer.persona_id
        change_relation = [string]$Finding.change_relation
        evidence = @($Finding.evidence | Sort-Object -Unique)
        uncertainty = [string]$Finding.uncertainty
    }
}
function Get-ScopePaths { param([object]$Scope)
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in @($Scope.changed_files)) { if ($entry.path) { [void]$paths.Add([string]$entry.path) }; if ($entry.old_path) { [void]$paths.Add([string]$entry.old_path) } }
    if (Test-Property $Scope 'selected') {
        foreach ($streamName in @('staged','unstaged')) { if (Test-Property $Scope.selected $streamName) { foreach ($entry in @($Scope.selected.$streamName.files)) { if ($entry.path) { [void]$paths.Add([string]$entry.path) }; if ($entry.old_path) { [void]$paths.Add([string]$entry.old_path) } } } }
        if (Test-Property $Scope.selected 'untracked') { foreach ($entry in @($Scope.selected.untracked)) { if ($entry.path) { [void]$paths.Add([string]$entry.path) } } }
    }
    if ($Scope.mode -eq 'codebase') { foreach ($entry in @($Scope.files)) { if ($entry.path) { [void]$paths.Add([string]$entry.path) } } }
    return $paths
}
function Add-Error { param([System.Collections.Generic.List[string]]$Errors,[string]$Message) $Errors.Add($Message) | Out-Null }
function Validate-Scope { param([object]$Scope,[System.Collections.Generic.List[string]]$Errors)
    if ($Scope.schema_version -ne $script:CrcSchemaVersion) { Add-Error $Errors 'scope schema_version is invalid' }
    if ($Scope.snapshot_id -notmatch '^sha256:[0-9a-f]{64}$') { Add-Error $Errors 'scope snapshot_id is invalid' }
    if ($Scope.mode -notin @('codebase','branch','worktree','pull-request')) { Add-Error $Errors 'scope mode is invalid' }
    if ($Scope.status -notin @('READY','NO_CHANGES','BLOCKED')) { Add-Error $Errors 'scope status is invalid' }
    if (-not (Test-Property $Scope 'snapshot_material')) { Add-Error $Errors 'scope snapshot_material is missing' }
    else {
        if ((Get-CrcHashJson -Value $Scope.snapshot_material) -ne $Scope.snapshot_id) { Add-Error $Errors 'scope snapshot_id does not match snapshot_material' }
        foreach ($name in @('mode','revision','base','head','merge_base','changed_files','files','statuses','selected','unresolved_index','dirty_worktree','dirty_status','warnings')) {
            if ((Test-Property $Scope.snapshot_material $name) -and (Test-Property $Scope $name) -and ((ConvertTo-CrcJson $Scope.snapshot_material.$name) -ne (ConvertTo-CrcJson $Scope.$name))) { Add-Error $Errors "scope mirrored field differs: $name" }
        }
        if ((Test-Property $Scope.snapshot_material 'snapshot') -and ((ConvertTo-CrcJson $Scope.pull_request) -ne (ConvertTo-CrcJson $Scope.snapshot_material.snapshot))) { Add-Error $Errors 'scope pull_request differs from snapshot_material' }
    }
}
function Validate-Finding { param([object]$Finding,[string]$Persona,[string]$SnapshotId,[System.Collections.Generic.HashSet[string]]$Allowed,[string]$Mode,[System.Collections.Generic.List[string]]$Errors,[string]$Location)
    foreach ($name in @('fingerprint','category','path','scenario','trigger','impact','remediation','uncertainty')) { if ([string]::IsNullOrWhiteSpace([string]$Finding.$name)) { Add-Error $Errors "$Location.$name is required" } }
    if ($Finding.fingerprint -notmatch '^sha256:[0-9a-f]{64}$') { Add-Error $Errors "$Location.fingerprint is invalid" }
    if ($Finding.snapshot_id -ne $SnapshotId) { Add-Error $Errors "$Location.snapshot_id differs from scope" }
    if ($Finding.severity -notin @('P0','P1','P2','P3')) { Add-Error $Errors "$Location.severity is invalid" }
    if ($Finding.change_relation -notin @('introduced','worsened','pre-existing','out-of-scope','unknown')) { Add-Error $Errors "$Location.change_relation is invalid" }
    if (@($Finding.persona_ids).Count -eq 0 -or @($Finding.persona_ids | Where-Object { $_ -notin $script:CrcPersonas }).Count -gt 0 -or $Persona -notin @($Finding.persona_ids)) { Add-Error $Errors "$Location.persona_ids is invalid" }
    if ([int]$Finding.line -lt 1) { Add-Error $Errors "$Location.line must be positive" }
    if (@($Finding.evidence).Count -eq 0) { Add-Error $Errors "$Location.evidence is required" }
    if ($Mode -ne 'codebase' -and -not $Allowed.Contains([string]$Finding.path)) { Add-Error $Errors "$Location.path is outside the immutable scope" }
}

$errors = [System.Collections.Generic.List[string]]::new()
try {
    $scope = Get-JsonInput -Path $ScopePath -Label 'scope input'
    $reviewers = @(Get-Content -LiteralPath (Resolve-CrcSafePath -Path $ReviewersPath -Label 'reviewer input' -MustExist) | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
    $adjudication = Get-JsonInput -Path $AdjudicationPath -Label 'adjudication input'
    $adjudicator = if (Test-Property $adjudication 'adjudicator') { [string]$adjudication.adjudicator } else { 'unavailable' }
    $adjudicatedAt = if (Test-Property $adjudication 'created_at_utc') { [string]$adjudication.created_at_utc } else { Get-CrcUtcNow }
    $adjudicationSnapshot = if (Test-Property $adjudication 'snapshot_id') { [string]$adjudication.snapshot_id } else { [string]$scope.snapshot_id }
    Validate-Scope -Scope $scope -Errors $errors
    $allowed = Get-ScopePaths -Scope $scope
    foreach ($name in @('adjudicator','created_at_utc','snapshot_id')) {
        if (-not (Test-Property $adjudication $name) -or [string]::IsNullOrWhiteSpace([string]$adjudication.$name)) { Add-Error $errors "adjudication.$name is required" }
    }
    if ((Test-Property $adjudication 'snapshot_id') -and $adjudication.snapshot_id -ne $scope.snapshot_id) { Add-Error $errors 'adjudication.snapshot_id differs from scope' }
    $byPersona = @{}
    $candidates = @{}
    if (-not ($scope.status -eq 'NO_CHANGES' -and $reviewers.Count -eq 0)) {
        foreach ($reviewer in $reviewers) {
            $location = "reviewer[$($reviewer.review_id)]"
            if ($reviewer.persona_id -notin $script:CrcPersonas) { Add-Error $errors "$location persona is unknown"; continue }
            if ($byPersona.ContainsKey($reviewer.persona_id)) { Add-Error $errors "duplicate reviewer persona $($reviewer.persona_id)" }
            $byPersona[$reviewer.persona_id] = $reviewer
            foreach ($name in @('requested_model','effective_model','requested_concurrency','effective_concurrency','completed_at_utc')) { if (-not (Test-Property $reviewer $name)) { Add-Error $errors "$location.$name is missing" } }
            if ($reviewer.requested_model -and $reviewer.effective_model -and $reviewer.requested_model -ne $reviewer.effective_model) { Add-Error $errors "$location model mismatch" }
            if ($reviewer.requested_concurrency -and $reviewer.effective_concurrency -and $reviewer.requested_concurrency -ne $reviewer.effective_concurrency) { Add-Error $errors "$location concurrency mismatch" }
            if ($reviewer.status -notin @('complete','not_applicable','failed')) { Add-Error $errors "$location status is invalid" }
            if ($reviewer.status -in @('not_applicable','failed') -and [string]::IsNullOrWhiteSpace([string]$reviewer.reason)) { Add-Error $errors "$location reason is required" }
            if ($reviewer.status -eq 'not_applicable' -and @($reviewer.findings).Count -gt 0) { Add-Error $errors "$location not_applicable cannot contain findings" }
            $findingIndex = 0
            foreach ($finding in @($reviewer.findings)) {
                $findingIndex++
                $findingErrorsBefore = $errors.Count
                Validate-Finding -Finding $finding -Persona $reviewer.persona_id -SnapshotId $scope.snapshot_id -Allowed $allowed -Mode $scope.mode -Errors $errors -Location "$location.findings[$findingIndex]"
                if ($errors.Count -eq $findingErrorsBefore) {
                    $evidenceRecord = New-ReviewerEvidence -Reviewer $reviewer -Finding $finding
                    if (-not $candidates.ContainsKey($finding.fingerprint)) {
                        $candidate = $finding | ConvertTo-Json -Depth 100 | ConvertFrom-Json
                        $candidate.persona_ids = @($finding.persona_ids | Sort-Object -Unique)
                        $candidate.evidence = @($finding.evidence | Sort-Object -Unique)
                        $candidate | Add-Member -NotePropertyName reviewer_evidence -NotePropertyValue @($evidenceRecord) -Force
                        $candidates[$finding.fingerprint] = $candidate
                    }
                    else {
                        $candidate = $candidates[$finding.fingerprint]
                        $candidate.persona_ids = @($candidate.persona_ids + $finding.persona_ids | Sort-Object -Unique)
                        $candidate.evidence = @($candidate.evidence + $finding.evidence | Sort-Object -Unique)
                        if ($candidate.change_relation -ne $finding.change_relation) { $candidate.change_relation = 'unknown' }
                        $candidate | Add-Member -NotePropertyName reviewer_evidence -NotePropertyValue @(@($candidate.reviewer_evidence) + @($evidenceRecord) | Sort-Object persona_id,reviewer_id) -Force
                    }
                }
            }
        }
        foreach ($persona in $script:CrcPersonas) { if (-not $byPersona.ContainsKey($persona)) { Add-Error $errors "missing reviewer persona $persona" } }
    }
    $dispositions = @{}
    if ($null -eq $adjudication.dispositions -or $adjudication.dispositions -isnot [System.Collections.IEnumerable]) { Add-Error $errors 'adjudication dispositions is required' }
    else {
        foreach ($entry in @($adjudication.dispositions)) {
            foreach ($name in @('fingerprint','disposition','rationale','snapshot_id')) { if (-not (Test-Property $entry $name) -or [string]::IsNullOrWhiteSpace([string]$entry.$name)) { Add-Error $errors "disposition.$name is required" } }
            if ((Test-Property $entry 'snapshot_id') -and $entry.snapshot_id -ne $scope.snapshot_id) { Add-Error $errors "disposition snapshot differs from scope: $($entry.fingerprint)" }
            if (-not $candidates.ContainsKey($entry.fingerprint)) { Add-Error $errors "unknown disposition fingerprint $($entry.fingerprint)" }
            else { $dispositions[$entry.fingerprint] = $entry; if ($entry.disposition -eq 'duplicate' -and (-not $entry.duplicate_of -or $entry.duplicate_of -eq $entry.fingerprint)) { Add-Error $errors "invalid duplicate target $($entry.fingerprint)" } }
        }
        foreach ($fingerprint in $candidates.Keys) { if (-not $dispositions.ContainsKey($fingerprint)) { Add-Error $errors "missing disposition for $fingerprint" } }
        foreach ($fingerprint in $dispositions.Keys) { if ($dispositions[$fingerprint].disposition -eq 'duplicate') { $seen=@{}; $current=$fingerprint; while ($dispositions.ContainsKey($current) -and $dispositions[$current].disposition -eq 'duplicate') { if ($seen.ContainsKey($current)) { Add-Error $errors "duplicate cycle includes $fingerprint"; break }; $seen[$current]=$true; $current=$dispositions[$current].duplicate_of }; if (-not $dispositions.ContainsKey($current) -or $dispositions[$current].disposition -eq 'duplicate') { Add-Error $errors "duplicate has no canonical target $fingerprint" } } }
    }
    $status = if ($errors.Count -gt 0) { 'INCOMPLETE' } elseif ($scope.status -eq 'NO_CHANGES') { 'NO_CHANGES' } elseif ($scope.status -eq 'BLOCKED') { 'INCOMPLETE' } elseif (@($byPersona.Values | Where-Object status -eq 'failed').Count -gt 0) { 'INCOMPLETE' } elseif (@($dispositions.Values | Where-Object disposition -eq 'unresolved').Count -gt 0) { 'INCOMPLETE' } elseif (@($dispositions.Keys | Where-Object { $dispositions[$_].disposition -eq 'validated' -and $candidates[$_].severity -in @('P0','P1') }).Count -gt 0) { 'BLOCKED' } else { 'PASS' }
    $findings = @($candidates.Values | ForEach-Object { $item = $_ | ConvertTo-Json -Depth 100 | ConvertFrom-Json; if ($dispositions.ContainsKey($item.fingerprint)) { $item | Add-Member -NotePropertyName disposition -NotePropertyValue $dispositions[$item.fingerprint].disposition -Force }; $item })
    $result = [ordered]@{ schema_version = $script:CrcSchemaVersion; status = $status; snapshot_id = $scope.snapshot_id; scope_manifest = $scope; reviewers = @($byPersona.Values); findings = $findings; dispositions = @($dispositions.Values); execution = [ordered]@{ reviewer_count = $byPersona.Count; required_reviewer_count = $script:CrcPersonas.Count; adjudicator = $adjudicator; adjudicated_at_utc = $adjudicatedAt; adjudication_snapshot_id = $adjudicationSnapshot }; publication = [ordered]@{ status = 'not-requested' }; errors = @($errors) }
    Write-CrcJson -Path $OutputPath -Value $result
    if ($MarkdownPath) { $markdownSafePath = Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown output'; $text = "# Code Review Council`n`n- Status: ``$status```n- Snapshot: ``$($scope.snapshot_id)```n"; Set-Content -LiteralPath $markdownSafePath -Value $text -Encoding utf8 }
    Write-Output $status
    exit $(if ($status -in @('PASS','NO_CHANGES')) { 0 } else { 2 })
}
catch {
    $errorSnapshot = "sha256:$(Get-CrcHashText -Text $_.Exception.Message)"
    $result = [ordered]@{ schema_version = $script:CrcSchemaVersion; status = 'INCOMPLETE'; snapshot_id = $errorSnapshot; reviewers = @(); findings = @(); dispositions = @(); execution = [ordered]@{ reviewer_count = 0; required_reviewer_count = $script:CrcPersonas.Count; adjudicator = 'unavailable'; adjudicated_at_utc = Get-CrcUtcNow; adjudication_snapshot_id = $errorSnapshot }; publication = [ordered]@{ status = 'not-requested' }; errors = @($_.Exception.Message) }
    Write-CrcJson -Path $OutputPath -Value $result
    Write-Error $_.Exception.Message
    exit 2
}
