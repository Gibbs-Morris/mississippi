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
function Test-CrcArrayValue { param([object]$Value) return $Value -is [System.Array] }
function Get-PropertyValue { param([object]$Object,[string]$Name,[object]$Default = $null) if (Test-Property $Object $Name) { $value=$Object.PSObject.Properties[$Name].Value; return ,$value }; return ,$Default }
function New-ReviewerEvidence { param([object]$Reviewer,[object]$Finding)
    return [ordered]@{
        reviewer_id = [string]$Reviewer.review_id
        persona_id = [string]$Reviewer.persona_id
        category = [string]$Finding.category
        severity = [string]$Finding.severity
        path = [string]$Finding.path
        symbol = [string](Get-PropertyValue -Object $Finding -Name 'symbol')
        line = [int]$Finding.line
        scenario = [string]$Finding.scenario
        trigger = [string]$Finding.trigger
        impact = [string]$Finding.impact
        change_relation = [string]$Finding.change_relation
        evidence = @($Finding.evidence | Sort-Object -Unique)
        remediation = [string]$Finding.remediation
        uncertainty = [string]$Finding.uncertainty
    }
}

function Get-ScopePatchTexts { param([object]$Scope)
    $patches = @()
    if (Test-Property $Scope 'patch') { $patches += [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String([string]$Scope.patch.content_base64)) }
    if (Test-Property $Scope 'selected') {
        foreach ($streamName in @('staged','unstaged')) {
            if (Test-Property $Scope.selected $streamName -and Test-Property $Scope.selected.$streamName 'patch') { $patches += [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String([string]$Scope.selected.$streamName.patch.content_base64)) }
        }
    }
    $pullRequest = Get-PropertyValue -Object $Scope -Name 'pull_request'; if ($null -ne $pullRequest -and (Test-Property $pullRequest 'diff')) { $patches += [string]$pullRequest.diff }
    return @($patches)
}

function Test-FindingLine { param([object]$Scope,[string]$Path,[int]$Line)
    if ($Scope.mode -eq 'codebase') {
        if (@($Scope.files | Where-Object path -eq $Path).Count -eq 0) { return $false }
        try {
            $repository = Resolve-CrcRepository -Path ([string]$Scope.repository.root)
            $content = Invoke-CrcGit -Repository $repository -Arguments @('show','--no-ext-diff',"$($Scope.revision):$Path")
            return $Line -le @($content -split "`r?`n").Count
        }
        catch { return $false }
    }
    foreach ($patch in Get-ScopePatchTexts -Scope $Scope) {
        $currentPaths = @()
        foreach ($patchLine in ($patch -split "`r?`n")) {
            if ($patchLine -match '^diff --git a/(.*) b/(.*)$') {
                $currentPaths = @((Get-CrcPath -Path $Matches[1]),(Get-CrcPath -Path $Matches[2]))
                continue
            }
            if ($patchLine -match '^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@') {
                $oldStart = [int]$Matches[1]; $oldCount = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
                $newStart = [int]$Matches[3]; $newCount = if ($Matches[4]) { [int]$Matches[4] } else { 1 }
                if ($Path -in $currentPaths -and (($Line -ge $oldStart -and $Line -lt ($oldStart + $oldCount)) -or ($Line -ge $newStart -and $Line -lt ($newStart + $newCount)))) { return $true }
            }
        }
    }
    return $false
}

function Get-MarkdownReview { param([string]$Status,[string]$SnapshotId,[object[]]$Findings,[object[]]$Dispositions,[object[]]$Errors,[object]$Execution)
    $lines = @('# Code Review Council','',"- Status: **$Status**","- Snapshot: ``$SnapshotId``",'')
    $lines += '## Execution'; $lines += ''; $lines += "- Reviewers: $($Execution.reviewer_count)/$($Execution.required_reviewer_count)"; $lines += "- Adjudicator: $($Execution.adjudicator)"; $lines += "- Adjudicated at UTC: $($Execution.adjudicated_at_utc)"; $lines += ''
    $lines += '## Findings'; $lines += ''
    if ($Findings.Count -eq 0) { $lines += 'No findings were produced for this snapshot.' }
    else {
        foreach ($finding in $Findings) {
            $lines += "- **$($finding.severity)** $($finding.path):$($finding.line) - $($finding.scenario)"
            $lines += "  - Disposition: $($finding.disposition)"
            $lines += "  - Change relation: $($finding.change_relation)"
            foreach ($evidence in @($finding.evidence)) { $lines += "  - Evidence: $evidence" }
        }
    }
    $lines += @('', '## Dispositions','')
    if ($Dispositions.Count -eq 0) { $lines += 'No dispositions were recorded.' } else { foreach ($entry in $Dispositions) { $lines += "- $($entry.fingerprint): $($entry.disposition) - $($entry.rationale)" } }
    if ($Errors.Count -gt 0) { $lines += @('', '## Validation errors',''); foreach ($errorText in $Errors) { $lines += "- $errorText" } }
    return ($lines -join "`n") + "`n"
}
function Test-NullablePositiveInteger { param([object]$Value) return $null -eq $Value -or (($Value -is [int] -or $Value -is [long] -or $Value -is [uint32] -or $Value -is [uint64]) -and $Value -gt 0) }
function Get-ScopePaths { param([object]$Scope)
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in @($Scope.changed_files)) { $entryPath=Get-PropertyValue -Object $entry -Name 'path'; $oldPath=Get-PropertyValue -Object $entry -Name 'old_path'; if ($entryPath) { [void]$paths.Add([string]$entryPath) }; if ($oldPath) { [void]$paths.Add([string]$oldPath) } }
    if (Test-Property $Scope 'selected') {
        foreach ($streamName in @('staged','unstaged')) { if (Test-Property $Scope.selected $streamName) { foreach ($entry in @($Scope.selected.$streamName.files)) { $entryPath=Get-PropertyValue -Object $entry -Name 'path'; $oldPath=Get-PropertyValue -Object $entry -Name 'old_path'; if ($entryPath) { [void]$paths.Add([string]$entryPath) }; if ($oldPath) { [void]$paths.Add([string]$oldPath) } } } }
        if (Test-Property $Scope.selected 'untracked') { foreach ($entry in @($Scope.selected.untracked)) { $entryPath=Get-PropertyValue -Object $entry -Name 'path'; if ($entryPath) { [void]$paths.Add([string]$entryPath) } } }
    }
    if ($Scope.mode -eq 'codebase') { foreach ($entry in @($Scope.files)) { $entryPath=Get-PropertyValue -Object $entry -Name 'path'; if ($entryPath) { [void]$paths.Add([string]$entryPath) } } }
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
        foreach ($name in @('mode','status','revision','base','head','merge_base','changed_files','files','statuses','selected','unresolved_index','dirty_worktree','dirty_status','warnings')) {
            if (Test-Property $Scope.snapshot_material $name) {
                if (Test-Property $Scope $name) { $materialValue=Get-PropertyValue -Object $Scope.snapshot_material -Name $name; $manifestValue=Get-PropertyValue -Object $Scope -Name $name; if ((ConvertTo-CrcJson -Value $materialValue) -ne (ConvertTo-CrcJson -Value $manifestValue)) { Add-Error $Errors "scope mirrored field differs: $name" } }
            }
        }
        if (Test-Property $Scope.snapshot_material 'snapshot') { $pullRequest = Get-PropertyValue -Object $Scope -Name 'pull_request'; if ($null -eq $pullRequest -or (ConvertTo-CrcJson $pullRequest) -ne (ConvertTo-CrcJson $Scope.snapshot_material.snapshot)) { Add-Error $Errors 'scope pull_request differs from snapshot_material' } }
    }
}
function Validate-Finding { param([object]$Finding,[string]$Persona,[string]$SnapshotId,[System.Collections.Generic.HashSet[string]]$Allowed,[string]$Mode,[System.Collections.Generic.List[string]]$Errors,[string]$Location)
    foreach ($name in @('fingerprint','category','path','scenario','trigger','impact','remediation','uncertainty')) { if ([string]::IsNullOrWhiteSpace([string]$Finding.$name)) { Add-Error $Errors "$Location.$name is required" } }
    if (-not (Test-Property $Finding 'persona_ids') -or -not (Test-CrcArrayValue $Finding.persona_ids)) { Add-Error $Errors "$Location.persona_ids must be an array" }
    if (-not (Test-Property $Finding 'evidence') -or -not (Test-CrcArrayValue $Finding.evidence)) { Add-Error $Errors "$Location.evidence must be an array" }
    if ($Finding.fingerprint -notmatch '^sha256:[0-9a-f]{64}$') { Add-Error $Errors "$Location.fingerprint is invalid" }
    if ($Finding.snapshot_id -ne $SnapshotId) { Add-Error $Errors "$Location.snapshot_id differs from scope" }
    if ($Finding.severity -notin @('P0','P1','P2','P3')) { Add-Error $Errors "$Location.severity is invalid" }
    if ($Finding.change_relation -notin @('introduced','worsened','pre-existing','out-of-scope','unknown')) { Add-Error $Errors "$Location.change_relation is invalid" }
    if (Test-CrcArrayValue $Finding.persona_ids) {
        if ($Finding.persona_ids.Count -eq 0 -or @($Finding.persona_ids | Where-Object { $_ -notin $script:CrcPersonas }).Count -gt 0 -or $Persona -notin $Finding.persona_ids) { Add-Error $Errors "$Location.persona_ids is invalid" }
    }
    if (-not (Test-Property $Finding 'line') -or $Finding.line -isnot [ValueType] -or [int]$Finding.line -lt 1) { Add-Error $Errors "$Location.line must be positive" }
    if (Test-CrcArrayValue $Finding.evidence) {
        if ($Finding.evidence.Count -eq 0) { Add-Error $Errors "$Location.evidence is required" }
    }
    if (-not $Allowed.Contains([string]$Finding.path)) { Add-Error $Errors "$Location.path is outside the immutable scope" }
    if ((Test-Property $Finding 'line') -and [int]$Finding.line -ge 1 -and $Allowed.Contains([string]$Finding.path) -and -not (Test-FindingLine -Scope $script:CurrentScope -Path ([string]$Finding.path) -Line ([int]$Finding.line))) { Add-Error $Errors "$Location.line is outside the captured evidence" }
}

$errors = [System.Collections.Generic.List[string]]::new()
try {
    $scope = Get-JsonInput -Path $ScopePath -Label 'scope input'
    $reviewers = @(Get-Content -LiteralPath (Resolve-CrcSafePath -Path $ReviewersPath -Label 'reviewer input' -MustExist) | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
    $adjudication = Get-JsonInput -Path $AdjudicationPath -Label 'adjudication input'
    $adjudicator = if (Test-Property $adjudication 'adjudicator') { [string]$adjudication.adjudicator } else { 'unavailable' }
    $adjudicatedAt = if (Test-Property $adjudication 'created_at_utc') { [string]$adjudication.created_at_utc } else { Get-CrcUtcNow }
    $adjudicationSnapshot = if (Test-Property $adjudication 'snapshot_id') { [string]$adjudication.snapshot_id } else { [string]$scope.snapshot_id }
    $script:CurrentScope = $scope
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
            $reviewId = [string](Get-PropertyValue -Object $reviewer -Name 'review_id')
            $personaId = [string](Get-PropertyValue -Object $reviewer -Name 'persona_id')
            $location = "reviewer[$reviewId]"
            if ([string]::IsNullOrWhiteSpace($reviewId)) { Add-Error $errors "$location.review_id is required" }
            $personaValid = $personaId -in $script:CrcPersonas
            if (-not $personaValid) { Add-Error $errors "$location persona is unknown" }
            if ($personaValid) {
                if ($byPersona.ContainsKey($personaId)) { Add-Error $errors "duplicate reviewer persona $personaId" }
                $byPersona[$personaId] = $reviewer
            }
            if (-not (Test-Property $reviewer 'snapshot_id') -or [string]::IsNullOrWhiteSpace([string]$reviewer.snapshot_id)) { Add-Error $errors "$location.snapshot_id is required" }
            elseif ($reviewer.snapshot_id -ne $scope.snapshot_id) { Add-Error $errors "$location.snapshot_id differs from scope" }
            foreach ($name in @('requested_model','effective_model','requested_concurrency','effective_concurrency','completed_at_utc')) { if (-not (Test-Property $reviewer $name)) { Add-Error $errors "$location.$name is missing" } }
            foreach ($name in @('requested_model','effective_model')) { if ((Test-Property $reviewer $name) -and $null -ne $reviewer.$name -and [string]::IsNullOrWhiteSpace([string]$reviewer.$name)) { Add-Error $errors "$location.$name is empty" } }
            foreach ($name in @('requested_concurrency','effective_concurrency')) { if ((Test-Property $reviewer $name) -and -not (Test-NullablePositiveInteger -Value $reviewer.$name)) { Add-Error $errors "$location.$name must be a positive integer or null" } }
            if ((Test-Property $reviewer 'completed_at_utc') -and [string]::IsNullOrWhiteSpace([string]$reviewer.completed_at_utc)) { Add-Error $errors "$location.completed_at_utc is empty" }
            if ((Test-Property $reviewer 'requested_model') -and (Test-Property $reviewer 'effective_model') -and $null -ne $reviewer.requested_model -and $null -ne $reviewer.effective_model -and $reviewer.requested_model -ne $reviewer.effective_model) { Add-Error $errors "$location model mismatch" }
            $reviewerStatus = [string](Get-PropertyValue -Object $reviewer -Name 'status')
            if ($reviewerStatus -notin @('complete','not_applicable','failed')) { Add-Error $errors "$location status is invalid" }
            if ($reviewerStatus -in @('not_applicable','failed') -and [string]::IsNullOrWhiteSpace([string](Get-PropertyValue -Object $reviewer -Name 'reason'))) { Add-Error $errors "$location reason is required" }
            $reviewerFindings = @()
            if (-not (Test-Property $reviewer 'findings') -or -not (Test-CrcArrayValue $reviewer.findings)) { Add-Error $errors "$location.findings must be an array" } else { $reviewerFindings = @($reviewer.findings) }
            if ($reviewerStatus -eq 'not_applicable' -and $reviewerFindings.Count -gt 0) { Add-Error $errors "$location not_applicable cannot contain findings" }
            $findingIndex = 0
            foreach ($finding in $reviewerFindings) {
                $findingIndex++
                $findingErrorsBefore = $errors.Count
                Validate-Finding -Finding $finding -Persona $personaId -SnapshotId $scope.snapshot_id -Allowed $allowed -Mode $scope.mode -Errors $errors -Location "$location.findings[$findingIndex]"
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
                        if ($candidate.severity -ne $finding.severity) { Add-Error $errors "conflicting severity for shared fingerprint $($finding.fingerprint)"; continue }
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
    if (-not (Test-Property $adjudication 'dispositions') -or -not (Test-CrcArrayValue $adjudication.dispositions)) { Add-Error $errors 'adjudication dispositions must be an array' }
    else {
        foreach ($entry in @($adjudication.dispositions)) {
            foreach ($name in @('fingerprint','disposition','rationale','snapshot_id')) { if (-not (Test-Property $entry $name) -or [string]::IsNullOrWhiteSpace([string]$entry.$name)) { Add-Error $errors "disposition.$name is required" } }
            if ((Test-Property $entry 'snapshot_id') -and $entry.snapshot_id -ne $scope.snapshot_id) { Add-Error $errors "disposition snapshot differs from scope: $($entry.fingerprint)" }
            if ($entry.disposition -notin @('validated','duplicate','rejected','pre-existing','out-of-scope','unresolved')) { Add-Error $errors "unknown disposition $($entry.disposition)" }
            if ($dispositions.ContainsKey($entry.fingerprint)) { Add-Error $errors "duplicate disposition fingerprint $($entry.fingerprint)"; continue }
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
    if ($MarkdownPath) { $markdownSafePath = Resolve-CrcSafePath -Path $MarkdownPath -Label 'Markdown output'; Set-Content -LiteralPath $markdownSafePath -Value (Get-MarkdownReview -Status $status -SnapshotId $scope.snapshot_id -Findings $findings -Dispositions @($dispositions.Values) -Errors @($errors) -Execution $result.execution) -Encoding utf8 }
    Write-Output $status
    exit $(if ($status -in @('PASS','NO_CHANGES')) { 0 } else { 2 })
}
catch {
    $errorSnapshot = "sha256:$(Get-CrcHashText -Text $_.Exception.Message)"
    $result = [ordered]@{ schema_version = $script:CrcSchemaVersion; status = 'INCOMPLETE'; snapshot_id = $errorSnapshot; reviewers = @(); findings = @(); dispositions = @(); execution = [ordered]@{ reviewer_count = 0; required_reviewer_count = $script:CrcPersonas.Count; adjudicator = 'unavailable'; adjudicated_at_utc = Get-CrcUtcNow; adjudication_snapshot_id = $errorSnapshot }; publication = [ordered]@{ status = 'not-requested' }; errors = @($_.Exception.Message) }
    Write-CrcJson -Path $OutputPath -Value $result
    [Console]::Error.WriteLine($_.Exception.Message)
    [Console]::Error.WriteLine($_.InvocationInfo.PositionMessage)
    exit 2
}
