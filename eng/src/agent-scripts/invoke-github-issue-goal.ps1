#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet('start', 'resume')][string]$Action = 'start',
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [Parameter(Mandatory)][int]$IssueNumber,
    [string]$IssueJson,
    [string]$CheckpointPath,
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '../../..'),
    [string]$HeadRevision,
    [string]$BaseRevision,
    [string]$OperationStateJson,
    [switch]$MergeAuthorized,
    [switch]$EvidenceValidated,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GoalRevision {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Name)
    $value = @(& git -c "safe.directory=$($Root.Replace('\', '/'))" -C $Root rev-parse $Name 2>$null | Select-Object -First 1)
    $revision = ([string]$value).Trim()
    if ($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-fA-F]{7,64}$') { throw "Unable to resolve Git revision '$Name'." }
    return $revision
}

function Get-GoalWorktreeFingerprint {
    param([Parameter(Mandatory)][string]$Root)
    $status = @(& git -c "safe.directory=$($Root.Replace('\', '/'))" -C $Root status --porcelain=v1 --untracked-files=all 2>$null)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the current worktree fingerprint.' }
    $fileHashes = foreach ($line in $status) {
        if ([string]$line.Length -lt 4) { continue }
        $relative = ([string]$line).Substring(3).Trim('"')
        $full = Join-Path $Root $relative
        if (Test-Path -LiteralPath $full -PathType Leaf) {
            "${relative}:$((Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant())"
        }
        else { "${relative}:missing" }
    }
    $content = (($status -join "`n") + "`n" + ($fileHashes -join "`n"))
    $hash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($content))
    return 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-GoalIssue {
    param([Parameter(Mandatory)][string]$Owner, [Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][int]$Number, [AllowEmptyString()][string]$Json)
    if (-not [string]::IsNullOrWhiteSpace($Json)) { return ConvertFrom-Json -InputObject $Json }
    $output = & gh api "repos/$Owner/$Name/issues/$Number" --header 'Accept: application/vnd.github+json' 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Unable to read issue #${Number}: $($output.Trim())" }
    return ConvertFrom-Json -InputObject $output
}

function Get-GoalBodyDigest {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-GoalCheckpointPath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][int]$Number, [AllowEmptyString()][string]$Requested)
    if (-not [string]::IsNullOrWhiteSpace($Requested)) { return [System.IO.Path]::GetFullPath($Requested) }
    return [System.IO.Path]::GetFullPath((Join-Path $Root ".scratchpad/goals/$Number/checkpoint.json"))
}

function Get-GoalOperationState {
    param([AllowEmptyString()][string]$Json)
    if ([string]::IsNullOrWhiteSpace($Json)) { return [pscustomobject]@{ Status = 'none'; Handle = ''; Name = '' } }
    $state = ConvertFrom-Json -InputObject $Json
    return [pscustomobject]@{
        Status = [string]$state.Status
        Handle = if ($null -eq $state.Handle) { '' } else { [string]$state.Handle }
        Name = if ($null -eq $state.Name) { '' } else { [string]$state.Name }
    }
}

function Test-GoalIssueContract {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Body)
    $temporary = Join-Path ([System.IO.Path]::GetTempPath()) ("mississippi-goal-" + [guid]::NewGuid().ToString('N') + '.md')
    try {
        Set-Content -LiteralPath $temporary -Value $Body -Encoding utf8
        $validator = Join-Path $Root 'eng/src/agent-scripts/test-issue-spec.ps1'
        $output = & pwsh -NoProfile -File $validator -Path $temporary -RepositoryRoot $Root -Json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "Issue contract validation failed: $($output.Trim())" }
    }
    finally { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
}

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $issue = Get-GoalIssue -Owner $RepositoryOwner -Name $RepositoryName -Number $IssueNumber -Json $IssueJson
    if ([int]$issue.number -ne $IssueNumber) { throw "Issue identity mismatch: expected #$IssueNumber." }
    if ([string]$issue.state -ne 'open') { throw "Issue #$IssueNumber is not open." }
    if ($null -ne $issue.PSObject.Properties['pull_request']) { throw "Reference #$IssueNumber is a pull request, not an issue." }

    $checkpoint = Get-GoalCheckpointPath -Root $root -Number $IssueNumber -Requested $CheckpointPath
    $currentBody = if ($null -eq $issue.body) { '' } else { [string]$issue.body }
    $currentDigest = Get-GoalBodyDigest -Body $currentBody
    $currentHead = if ([string]::IsNullOrWhiteSpace($HeadRevision)) { Get-GoalRevision -Root $root -Name 'HEAD' } else { $HeadRevision }
    $currentBase = if ([string]::IsNullOrWhiteSpace($BaseRevision)) { Get-GoalRevision -Root $root -Name 'origin/main' } else { $BaseRevision }
    if ([string]::IsNullOrWhiteSpace($BaseRevision)) { throw 'BaseRevision must be supplied explicitly, including the immediate parent for stacked work.' }
    Test-GoalIssueContract -Root $root -Body $currentBody
    $operation = Get-GoalOperationState -Json $OperationStateJson

    if ($Action -eq 'resume' -and -not (Test-Path -LiteralPath $checkpoint -PathType Leaf)) {
        throw "Cannot resume issue #$IssueNumber because checkpoint '$checkpoint' does not exist."
    }

    $previous = $null
    if (Test-Path -LiteralPath $checkpoint -PathType Leaf) {
        $previous = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
    }

    if ($null -ne $previous) {
        $previousRepository = if ($null -ne $previous.Issue.PSObject.Properties['Repository']) { [string]$previous.Issue.Repository } else { '' }
        if ([int]$previous.Issue.Number -ne $IssueNumber -or $previousRepository -ne "$RepositoryOwner/$RepositoryName") {
            throw "Checkpoint '$checkpoint' belongs to a different issue or repository."
        }
        if ([string]::IsNullOrWhiteSpace($OperationStateJson) -and $null -ne $previous.Operation) {
            $operation = [pscustomobject]@{ Status = [string]$previous.Operation.Status; Handle = [string]$previous.Operation.Handle; Name = [string]$previous.Operation.Name }
        }
    }

    $currentWorktreeFingerprint = Get-GoalWorktreeFingerprint -Root $root
    $validatedDigest = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedIssueBodyDigest']) { [string]$previous.ValidatedIssueBodyDigest } else { '' }
    $validatedHead = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedHeadRevision']) { [string]$previous.ValidatedHeadRevision } else { '' }
    $validatedBase = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedBaseRevision']) { [string]$previous.ValidatedBaseRevision } else { '' }
    $validatedWorktree = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedWorktreeFingerprint']) { [string]$previous.ValidatedWorktreeFingerprint } else { '' }
    $scopeBaseline = if ($null -ne $previous -and -not [string]::IsNullOrWhiteSpace($validatedDigest)) { $validatedDigest } elseif ($null -ne $previous) { [string]$previous.IssueBodyDigest } else { '' }
    $headBaseline = if (-not [string]::IsNullOrWhiteSpace($validatedHead)) { $validatedHead } elseif ($null -ne $previous) { [string]$previous.HeadRevision } else { '' }
    $baseBaseline = if (-not [string]::IsNullOrWhiteSpace($validatedBase)) { $validatedBase } elseif ($null -ne $previous) { [string]$previous.BaseRevision } else { '' }
    $worktreeBaseline = if (-not [string]::IsNullOrWhiteSpace($validatedWorktree)) { $validatedWorktree } elseif ($null -ne $previous -and $null -ne $previous.PSObject.Properties['WorktreeFingerprint']) { [string]$previous.WorktreeFingerprint } else { '' }
    $scopeChanged = $null -ne $previous -and $scopeBaseline -ne $currentDigest
    $revisionChanged = $null -ne $previous -and (($headBaseline -ne $currentHead) -or ($baseBaseline -ne $currentBase))
    $worktreeChanged = $null -ne $previous -and $worktreeBaseline -ne $currentWorktreeFingerprint
    $activeOperation = $operation.Status -eq 'running'
    $operationCompleted = $null -ne $previous -and [string]$previous.Operation.Status -eq 'running' -and $operation.Status -in @('completed', 'failed')
    $status = if ($scopeChanged) { 'scope-changed' } elseif ($activeOperation) { 'operation-running' } elseif ($revisionChanged -or $worktreeChanged) { 'evidence-stale' } elseif ($null -eq $previous) { 'started' } else { 'resumed' }
    $evidenceFresh = -not $activeOperation -and (($EvidenceValidated) -or (-not $scopeChanged -and -not $revisionChanged -and -not $worktreeChanged -and ($null -ne $previous -and [bool]$previous.EvidenceFresh)))
    $nextAction = if ($scopeChanged) { 'reconcile-edited-issue-before-implementation' } elseif ($activeOperation) { "wait-for-existing-operation:$($operation.Handle)" } elseif ($operationCompleted) { 'inspect-completed-operation-result' } elseif ($revisionChanged -or $worktreeChanged) { 'invalidate-stale-evidence-and-revalidate' } elseif ($null -eq $previous) { 'inspect-guidance-and-prerequisites' } else { [string]$previous.NextAction }
    if ([string]::IsNullOrWhiteSpace($nextAction)) { $nextAction = 'inspect-guidance-and-prerequisites' }

    $record = [ordered]@{
        SchemaVersion = '1.0'
        Issue = [ordered]@{
            Number = $IssueNumber
            Url = [string]$issue.html_url
            Title = [string]$issue.title
            State = [string]$issue.state
            Repository = "$RepositoryOwner/$RepositoryName"
        }
        IssueBodyDigest = $currentDigest
        ReviewedSourceRevision = $currentHead
        HeadRevision = $currentHead
        BaseRevision = $currentBase
        WorktreeFingerprint = $currentWorktreeFingerprint
        ValidatedIssueBodyDigest = if ($EvidenceValidated -or $evidenceFresh) { $currentDigest } else { $validatedDigest }
        ValidatedHeadRevision = if ($EvidenceValidated -or $evidenceFresh) { $currentHead } else { $validatedHead }
        ValidatedBaseRevision = if ($EvidenceValidated -or $evidenceFresh) { $currentBase } else { $validatedBase }
        ValidatedWorktreeFingerprint = if ($EvidenceValidated -or $evidenceFresh) { $currentWorktreeFingerprint } else { $validatedWorktree }
        Worktree = $root
        Action = $Action
        Status = $status
        EvidenceFresh = $evidenceFresh
        MergeBoundary = if ($MergeAuthorized) { 'MERGE_AUTHORIZED_BY_USER_BUT_NOT_PERFORMED' } else { 'PR_READY_NOT_MERGED' }
        Contract = [ordered]@{
            IssueTextIsData = $true
            CommandsExecutedFromIssueText = $false
            ToolsInstalledFromIssueText = $false
            SecretsAccessedFromIssueText = $false
        }
        Decisions = if ($null -ne $previous) { @($previous.Decisions) } else { @() }
        AcceptanceEvidence = if ($null -ne $previous) { @($previous.AcceptanceEvidence) } else { @() }
        AttemptedFixes = if ($null -ne $previous) { @($previous.AttemptedFixes) } else { @() }
        OutstandingReviewWork = if ($null -ne $previous) { @($previous.OutstandingReviewWork) } else { @() }
        Operation = [ordered]@{ Status = $operation.Status; Handle = $operation.Handle; Name = $operation.Name }
        NextAction = $nextAction
        UpdatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    }

    $checkpointDirectory = Split-Path -Parent $checkpoint
    New-Item -ItemType Directory -Path $checkpointDirectory -Force | Out-Null
    $temporaryCheckpoint = "$checkpoint.$PID.tmp"
    $record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $temporaryCheckpoint -Encoding utf8
    Move-Item -LiteralPath $temporaryCheckpoint -Destination $checkpoint -Force

    $result = [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Status = $status
        IssueNumber = $IssueNumber
        IssueUrl = [string]$issue.html_url
        CheckpointPath = $checkpoint
        EvidenceFresh = $evidenceFresh
        NextAction = $nextAction
        MergeBoundary = $record.MergeBoundary
        Operation = $record.Operation
        IssueBodyDigest = $currentDigest
        HeadRevision = $currentHead
        BaseRevision = $currentBase
    }
    if ($Json) { $result | ConvertTo-Json -Depth 8 -Compress } else { $result | Format-List }
    if ($status -in @('scope-changed', 'operation-running')) { exit 2 }
    exit 0
}
catch {
    if ($Json) {
        [pscustomobject]@{ SchemaVersion = '1.0'; Status = 'ERROR'; Error = $_.Exception.Message } | ConvertTo-Json -Depth 5 -Compress
    }
    else { Write-Error "Goal route failed: $($_.Exception.Message)" }
    exit 1
}
