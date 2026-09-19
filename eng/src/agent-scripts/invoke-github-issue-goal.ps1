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
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GoalRevision {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Name)
    $value = @(& git -c "safe.directory=$($Root.Replace('\', '/'))" -C $Root rev-parse $Name 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$value)) { return 'unknown' }
    return ([string]$value).Trim()
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
    $operation = Get-GoalOperationState -Json $OperationStateJson

    if ($Action -eq 'resume' -and -not (Test-Path -LiteralPath $checkpoint -PathType Leaf)) {
        throw "Cannot resume issue #$IssueNumber because checkpoint '$checkpoint' does not exist."
    }

    $previous = $null
    if (Test-Path -LiteralPath $checkpoint -PathType Leaf) {
        $previous = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
    }

    $scopeChanged = $null -ne $previous -and [string]$previous.IssueBodyDigest -ne $currentDigest
    $revisionChanged = $null -ne $previous -and ([string]$previous.HeadRevision -ne $currentHead -or [string]$previous.BaseRevision -ne $currentBase)
    $activeOperation = $operation.Status -eq 'running'
    $status = if ($scopeChanged) { 'scope-changed' } elseif ($activeOperation) { 'operation-running' } elseif ($revisionChanged) { 'evidence-stale' } elseif ($null -eq $previous) { 'started' } else { 'resumed' }
    $evidenceFresh = -not $scopeChanged -and -not $revisionChanged -and -not $activeOperation
    $nextAction = if ($scopeChanged) { 'reconcile-edited-issue-before-implementation' } elseif ($activeOperation) { "wait-for-existing-operation:$($operation.Handle)" } elseif ($revisionChanged) { 'invalidate-stale-evidence-and-revalidate' } elseif ($null -eq $previous) { 'inspect-guidance-and-prerequisites' } else { [string]$previous.NextAction }
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
    Write-Error "Goal route failed: $($_.Exception.Message)"
    exit 1
}
