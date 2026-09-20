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
    [Parameter(Mandatory)][string]$BaseRevision,
    [string]$OperationStateJson,
    [string]$ExpectedIssueBodyDigest,
    [string]$ExpectedContractBodyDigest,
    [string]$ExpectedHeadRevision,
    [string]$ExpectedBaseRevision,
    [string[]]$ExpectedAcceptanceCriteria,
    [string[]]$Decisions,
    [string[]]$AcceptanceEvidence,
    [string[]]$AttemptedFixes,
    [string[]]$OutstandingReviewWork,
    [string[]]$ResolvedReviewWork,
    [switch]$MergeAuthorized,
    [switch]$EvidenceValidated,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GoalRevision {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Name)
    $safeRoot = $Root.Replace([char]92, [char]47)
    $value = @(& git -c ("safe.directory=" + $safeRoot) -C $Root rev-parse --verify ("$Name^{commit}") 2>$null)
    $exitCode = if (Get-Variable -Name LASTEXITCODE -ErrorAction SilentlyContinue) { [int]$LASTEXITCODE } else { 0 }
    $revision = ([string]($value | Select-Object -First 1)).Trim()
    if ($exitCode -ne 0 -or $revision -notmatch '^[0-9a-fA-F]{7,64}$') { throw "Unable to resolve Git revision '$Name'." }
    return $revision
}

function Get-GoalRepositoryIdentity {
    param([Parameter(Mandatory)][string]$Root)

    $safeRoot = $Root.Replace('\', '/')
    $remoteNames = @(& git -c "safe.directory=$safeRoot" -C $Root remote 2>$null)
    if ($LASTEXITCODE -ne 0 -or $remoteNames.Count -eq 0) { throw "Unable to resolve Git remotes for '$Root'." }
    $identities = [System.Collections.Generic.List[string]]::new()
    foreach ($remoteName in $remoteNames) {
        $remote = @(& git -c "safe.directory=$safeRoot" -C $Root remote get-url ([string]$remoteName) 2>$null | Select-Object -First 1)
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$remote)) { continue }
        $remoteText = ([string]$remote).Trim() -replace '\.git$', ''
    $match = [regex]::Match($remoteText, '^(?:https?://github\.com/|git@github\.com:|ssh://git@github\.com/)(?<Owner>[^/]+)/(?<Name>[^/]+)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($match.Success) { $identities.Add("$($match.Groups['Owner'].Value)/$($match.Groups['Name'].Value)") }
    }
    if ($identities.Count -eq 0) { throw "Unable to identify a GitHub repository from the configured remotes for '$Root'." }
    return @($identities | Sort-Object -Unique)
}

function Get-GoalWorktreeFingerprint { # NOSONAR - bounded Git/index/worktree fingerprinting intentionally coordinates several integrity checks.
    param(
        [Parameter(Mandatory)][string]$Root,
        [AllowEmptyCollection()][string[]]$ExcludePaths = @()
    )

    $pathComparer = if ($env:OS -eq 'Windows_NT') { [StringComparer]::OrdinalIgnoreCase } else { [StringComparer]::Ordinal }
    $excludedPaths = [System.Collections.Generic.HashSet[string]]::new($pathComparer)
    foreach ($excludePath in $ExcludePaths) {
        if (-not [string]::IsNullOrWhiteSpace($excludePath)) {
            $null = $excludedPaths.Add([System.IO.Path]::GetFullPath($excludePath))
        }
    }

    $indexRaw = ((& git -c "safe.directory=$($Root.Replace('\', '/'))" -C $Root ls-files --stage -z 2>$null) -join '')
    $indexExitCode = if (Get-Variable -Name LASTEXITCODE -ErrorAction SilentlyContinue) { [int]$LASTEXITCODE } else { 0 }
    if ($indexExitCode -ne 0) { throw 'Unable to resolve the current index fingerprint.' }
    $indexHashes = @{}
    foreach ($indexEntry in @($indexRaw -split [char]0 | Where-Object { -not [string]::IsNullOrEmpty($_) })) {
        $tabIndex = $indexEntry.IndexOf([char]9)
        if ($tabIndex -lt 0) { continue }
        $metadata = $indexEntry.Substring(0, $tabIndex).Split(' ')
        if ($metadata.Count -ge 2) {
            $indexPath = $indexEntry.Substring($tabIndex + 1).Replace('\', '/')
            $indexHashes[$indexPath] = $metadata[1]
        }
    }

    $statusRaw = ((& git -c "safe.directory=$($Root.Replace('\', '/'))" -C $Root status --porcelain=v1 --untracked-files=all -z 2>$null) -join '')
    $status = @($statusRaw -split [char]0 | Where-Object { -not [string]::IsNullOrEmpty($_) })
    $exitCode = if (Get-Variable -Name LASTEXITCODE -ErrorAction SilentlyContinue) { [int]$LASTEXITCODE } else { 0 }
    if ($exitCode -ne 0) { throw 'Unable to resolve the current worktree fingerprint.' }
    $statusEntries = [System.Collections.Generic.List[string]]::new()
    $fileHashes = [System.Collections.Generic.List[string]]::new()
    for ($statusIndex = 0; $statusIndex -lt $status.Count; $statusIndex++) {
        $entry = [string]$status[$statusIndex]
        if ($entry.Length -lt 4) { continue }
        $relativePaths = [System.Collections.Generic.List[string]]::new()
        $relativePaths.Add($entry.Substring(3).Trim('"').Replace('\', '/'))
        if ($entry[0] -in @('R', 'C') -and $statusIndex + 1 -lt $status.Count) {
            $statusIndex++
            $relativePaths.Add(([string]$status[$statusIndex]).Trim('"').Replace('\', '/'))
            $statusEntries.Add($entry + ' -> ' + [string]$status[$statusIndex])
        }
        else { $statusEntries.Add($entry) }
        foreach ($relative in $relativePaths) {
            $full = [System.IO.Path]::GetFullPath((Join-Path $Root $relative))
            if ($excludedPaths.Contains($full)) { continue }
            $indexHash = if ($indexHashes.ContainsKey($relative)) { [string]$indexHashes[$relative] } else { 'absent' }
            $item = Get-Item -LiteralPath $full -Force -ErrorAction SilentlyContinue
            if ($null -ne $item -and (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) {
                $linkTargetProperty = $item.PSObject.Properties['LinkTarget']
                $linkTarget = if ($null -ne $linkTargetProperty) { [string]$linkTargetProperty.Value } else { '' }
                if ([string]::IsNullOrWhiteSpace($linkTarget) -and $null -ne $item.PSObject.Properties['Target']) {
                    $linkTarget = [string]$item.Target
                }
                $fileHashes.Add($relative + ':index=' + $indexHash + ':worktree=symlink:' + $linkTarget)
            }
            elseif ($null -ne $item -and -not $item.PSIsContainer) {
                $fileHashes.Add($relative + ':index=' + $indexHash + ':worktree=' + (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant())
            }
            else { $fileHashes.Add($relative + ':index=' + $indexHash + ':worktree=missing') }
        }
    }
    $content = (($statusEntries -join [Environment]::NewLine) + [Environment]::NewLine + ($fileHashes -join [Environment]::NewLine))
    $hash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($content))
    return 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-GoalIssue {
    param([Parameter(Mandatory)][string]$Owner, [Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][int]$Number, [AllowEmptyString()][string]$Json, [switch]$ForceCommentFallback)
    $issue = $null
    if (-not [string]::IsNullOrWhiteSpace($Json)) {
        $issue = ConvertFrom-Json -InputObject $Json
    }
    else {
        $output = & gh api "repos/$Owner/$Name/issues/$Number" --header 'Accept: application/vnd.github+json' 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "Unable to read issue #${Number}: $($output.Trim())" }
        $issue = ConvertFrom-Json -InputObject $output
    }

    $commentBodies = @()
    if ($null -ne $issue.PSObject.Properties['comments']) {
        $commentBodies = @($issue.comments | Where-Object { $null -ne $_.PSObject.Properties['body'] -and -not [string]::IsNullOrWhiteSpace([string]$_.body) })
    }
    if ($ForceCommentFallback -and $commentBodies.Count -eq 0) {
        $commentsOutput = & gh api "repos/$Owner/$Name/issues/$Number/comments" --header 'Accept: application/vnd.github+json' --paginate --slurp 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) {
            $commentPages = @(ConvertFrom-Json -InputObject $commentsOutput)
            $comments = @($commentPages | ForEach-Object { @($_) })
            $issue | Add-Member -NotePropertyName comments -NotePropertyValue $comments -Force
        }
        else { throw "Unable to read comments for issue #${Number}: $($commentsOutput.Trim())" }
    }
    return $issue
}

function Get-GoalBodyDigest {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-GoalCheckpointPath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Owner, [Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][int]$Number, [AllowEmptyString()][string]$Requested)
    if (-not [string]::IsNullOrWhiteSpace($Requested)) { return [System.IO.Path]::GetFullPath($Requested) }
    $repositoryKey = ("$Owner-$Name" -replace '[^A-Za-z0-9._-]', '_').ToLowerInvariant()
    $stateRoot = if ($env:OS -eq 'Windows_NT' -and -not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        Join-Path $env:LOCALAPPDATA 'Mississippi'
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:XDG_STATE_HOME)) {
        Join-Path $env:XDG_STATE_HOME 'mississippi'
    }
    else {
        $userHome = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
        if ([string]::IsNullOrWhiteSpace($userHome)) { throw 'Unable to resolve a persistent user state directory for the goal checkpoint.' }
        Join-Path $userHome '.local/state/mississippi'
    }
    return [System.IO.Path]::GetFullPath((Join-Path $stateRoot "goals/$repositoryKey/$Number/checkpoint.json"))
}

function Get-GoalCollection {
    param([AllowNull()][object]$Object, [Parameter(Mandatory)][string]$Name)
    if ($null -eq $Object -or $null -eq $Object.PSObject.Properties[$Name]) { return @() }
    return @($Object.$Name)
}

function Merge-GoalCollection {
    param(
        [AllowEmptyCollection()][object[]]$Existing = @(),
        [AllowEmptyCollection()][string[]]$Added = @()
    )
    $merged = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @(@($Existing) + @($Added) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [string]$_ } | Select-Object -Unique)) {
        $merged.Add($value)
    }
    return ,$merged.ToArray()
}

function ConvertTo-GoalArray {
    param([AllowEmptyCollection()][object[]]$Values = @())

    $array = [System.Collections.Generic.List[object]]::new()
    foreach ($value in @($Values)) { $array.Add($value) }
    return ,$array.ToArray()
}

function Expand-GoalStringCollection {
    param([AllowNull()][string[]]$Values)
    $expanded = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @($Values)) {
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        if ($value.TrimStart().StartsWith('[')) {
            try {
                foreach ($item in @(ConvertFrom-Json -InputObject $value)) {
                    if (-not [string]::IsNullOrWhiteSpace([string]$item)) { $expanded.Add([string]$item) }
                }
                continue
            }
            catch {
                Write-Verbose "Goal collection value was not JSON; preserving it as one entry."
            }
        }
        $expanded.Add($value)
    }
    return @($expanded)
}

function Get-GoalOperationState {
    param([AllowEmptyString()][string]$Json)
    if ([string]::IsNullOrWhiteSpace($Json)) { return [pscustomobject]@{ Status = 'none'; Handle = ''; Name = '' } }
    $state = ConvertFrom-Json -InputObject $Json
    $status = if ($null -eq $state.PSObject.Properties['Status']) { '' } else { [string]$state.Status }
    if ($status -notin @('none', 'running', 'completed', 'failed')) {
        throw "Unsupported goal operation status '$status'. Expected none, running, completed, or failed."
    }
    return [pscustomobject]@{
        Status = $status
        Handle = if ($null -eq $state.PSObject.Properties['Handle'] -or $null -eq $state.Handle) { '' } else { [string]$state.Handle }
        Name = if ($null -eq $state.PSObject.Properties['Name'] -or $null -eq $state.Name) { '' } else { [string]$state.Name }
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
        return $output | ConvertFrom-Json
    }
    finally { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
}

function Remove-GoalMarkdownFencedBlocks {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $insideFence = $false
    $fenceCharacter = ''
    $fenceLength = 0
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Content -split '\r?\n')) {
        if (-not $insideFence -and $line -match '^[ \t]{0,3}(?<Fence>`{3,}|~{3,})') {
            $insideFence = $true
            $fenceCharacter = $Matches.Fence.Substring(0, 1)
            $fenceLength = $Matches.Fence.Length
            $lines.Add('')
            continue
        }
        if ($insideFence -and $line -match ('^[ \t]{0,3}' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}[ \t]*$')) {
            $lines.Add('')
            $insideFence = $false
            continue
        }
        $lines.Add($(if ($insideFence) { '' } else { $line }))
    }
    return ($lines -join [Environment]::NewLine)
}

$checkpointLockStream = $null
try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    if ((Get-GoalRepositoryIdentity -Root $root) -notcontains "$RepositoryOwner/$RepositoryName") {
        throw "Repository root '$root' does not match requested repository '$RepositoryOwner/$RepositoryName'."
    }
    $checkpoint = Get-GoalCheckpointPath -Root $root -Owner $RepositoryOwner -Name $RepositoryName -Number $IssueNumber -Requested $CheckpointPath
    $checkpointDirectory = Split-Path -Parent $checkpoint
    New-Item -ItemType Directory -Path $checkpointDirectory -Force | Out-Null
    $checkpointLockPath = "$checkpoint.lock"
    $checkpointLockStream = [System.IO.File]::Open(
        $checkpointLockPath,
        [System.IO.FileMode]::OpenOrCreate,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
    $temporaryCheckpoint = "$checkpoint.$PID.tmp"
    $issue = Get-GoalIssue -Owner $RepositoryOwner -Name $RepositoryName -Number $IssueNumber -Json $IssueJson
    if ([int]$issue.number -ne $IssueNumber) { throw "Issue identity mismatch: expected #$IssueNumber." }
    $expectedRepository = "$RepositoryOwner/$RepositoryName"
    if ($null -ne $issue.PSObject.Properties['html_url'] -and [string]$issue.html_url -notmatch "/$([regex]::Escape($expectedRepository))/issues/$IssueNumber(?:$|[/?#])") {
        throw "Issue repository mismatch: expected '$expectedRepository'."
    }
    if ($null -ne $issue.PSObject.Properties['repository_url'] -and [string]$issue.repository_url -notmatch "/$([regex]::Escape($expectedRepository))$") {
        throw "Issue repository mismatch: expected '$expectedRepository'."
    }
    if ([string]$issue.state -ne 'open') { throw "Issue #$IssueNumber is not open." }
    if ($null -ne $issue.PSObject.Properties['pull_request']) { throw "Reference #$IssueNumber is a pull request, not an issue." }

    $issueBody = if ($null -eq $issue.body) { '' } else { [string]$issue.body }
    $issueBodyDigest = Get-GoalBodyDigest -Body $issueBody
    $checkedOutHead = Get-GoalRevision -Root $root -Name 'HEAD'
    $currentHead = if ([string]::IsNullOrWhiteSpace($HeadRevision)) { $checkedOutHead } else { Get-GoalRevision -Root $root -Name $HeadRevision }
    if ($currentHead -ne $checkedOutHead) {
        throw "HeadRevision '$HeadRevision' does not resolve to the checked-out HEAD '$checkedOutHead'."
    }
    $currentBase = Get-GoalRevision -Root $root -Name $BaseRevision
    $currentBody = $issueBody
    $contractBodyDigest = $issueBodyDigest
    $contractSource = 'issue-body'
    $contractResult = $null
    try {
        $contractResult = Test-GoalIssueContract -Root $root -Body $currentBody
    }
    catch {
        $hasEmbeddedComments = $null -ne $issue.PSObject.Properties['comments'] -and @($issue.comments | Where-Object { $null -ne $_.PSObject.Properties['body'] -and -not [string]::IsNullOrWhiteSpace([string]$_.body) }).Count -gt 0
        if (-not $hasEmbeddedComments) {
            $refetchedIssue = Get-GoalIssue -Owner $RepositoryOwner -Name $RepositoryName -Number $IssueNumber -Json '' -ForceCommentFallback
            $refetchedBody = if ($null -eq $refetchedIssue.body) { '' } else { [string]$refetchedIssue.body }
            if ([int]$refetchedIssue.number -ne $IssueNumber -or
                [string]$refetchedIssue.state -ne 'open' -or
                (Get-GoalBodyDigest -Body $refetchedBody) -ne $issueBodyDigest) {
                throw 'The issue changed while fallback comments were being fetched; retry from a newly verified issue response.'
            }
            $issue = $refetchedIssue
        }
        $issueComments = if ($null -ne $issue.PSObject.Properties['comments']) { @($issue.comments) } else { @() }
        $fallbackBodies = @($issueComments | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.body) } | Sort-Object created_at -Descending | ForEach-Object { [string]$_.body })
        foreach ($fallbackBody in $fallbackBodies) {
            try {
                $contractResult = Test-GoalIssueContract -Root $root -Body $fallbackBody
                $currentBody = $fallbackBody
                $contractBodyDigest = Get-GoalBodyDigest -Body $currentBody
                $contractSource = 'issue-comment'
                break
            }
            catch {
                Write-Verbose "Issue comment was not a valid contract; trying the next newest comment."
            }
        }
        if ($null -eq $contractResult) { throw }
    }
    $operation = Get-GoalOperationState -Json $OperationStateJson
    if ($operation.Status -eq 'running' -and [string]::IsNullOrWhiteSpace($operation.Handle)) {
        throw 'Running operation state requires a nonempty authoritative handle.'
    }

    if ($Action -eq 'resume' -and -not (Test-Path -LiteralPath $checkpoint -PathType Leaf)) {
        throw "Cannot resume issue #$IssueNumber because checkpoint '$checkpoint' does not exist."
    }

    $previous = $null
    if (Test-Path -LiteralPath $checkpoint -PathType Leaf) {
        $previous = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
    }

    if ($contractSource -eq 'issue-comment') {
        if ($null -eq $previous) {
            if ([string]::IsNullOrWhiteSpace($ExpectedContractBodyDigest)) { throw 'A fallback issue contract requires an expected contract body digest.' }
            if ($ExpectedContractBodyDigest -ne $contractBodyDigest) { throw 'The selected fallback contract does not match the authorized contract body digest.' }
        }
        elseif (-not [string]::IsNullOrWhiteSpace($ExpectedContractBodyDigest)) {
            if ($ExpectedContractBodyDigest -ne $contractBodyDigest) { throw 'The selected fallback contract does not match the authorized contract body digest.' }
        }
        elseif ($null -eq $previous.PSObject.Properties['ContractBodyDigest'] -or [string]$previous.ContractBodyDigest -ne $contractBodyDigest) {
            throw 'The selected fallback contract does not match the saved contract body digest; supply ExpectedContractBodyDigest to authorize reconciliation.'
        }
    }

    if ($null -ne $previous) {
        $previousRepository = if ($null -ne $previous.Issue.PSObject.Properties['Repository']) { [string]$previous.Issue.Repository } else { '' }
        if ([int]$previous.Issue.Number -ne $IssueNumber -or $previousRepository -ne "$RepositoryOwner/$RepositoryName") {
            throw "Checkpoint '$checkpoint' belongs to a different issue or repository."
        }
        if ([string]::IsNullOrWhiteSpace($OperationStateJson) -and $null -ne $previous.Operation) {
            $operation = [pscustomobject]@{ Status = [string]$previous.Operation.Status; Handle = [string]$previous.Operation.Handle; Name = [string]$previous.Operation.Name }
        }
        if ($null -ne $previous.Operation -and [string]$previous.Operation.Status -eq 'running') {
            if ($operation.Status -notin @('running', 'completed', 'failed')) {
                throw "Unsupported operation transition '$($operation.Status)'."
            }
            if ($operation.Handle -ne [string]$previous.Operation.Handle) {
                throw 'Operation update does not match the saved authoritative handle.'
            }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedIssueBodyDigest) -and $ExpectedIssueBodyDigest -ne $issueBodyDigest) {
        throw 'The issue body does not match the authorized local plan digest.'
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedHeadRevision) -and (Get-GoalRevision -Root $root -Name $ExpectedHeadRevision) -ne $currentHead) {
        throw 'The checked-out head does not match the authorized local plan revision.'
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedBaseRevision) -and (Get-GoalRevision -Root $root -Name $ExpectedBaseRevision) -ne $currentBase) {
        throw 'The selected base does not match the authorized local plan revision.'
    }
    $actualAcceptance = @($contractResult.AcceptanceCriteria | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $expectedAcceptance = @(Expand-GoalStringCollection -Values $ExpectedAcceptanceCriteria | Sort-Object -Unique)
    if ($expectedAcceptance.Count -gt 0 -and (($actualAcceptance -join '|') -ne ($expectedAcceptance -join '|'))) {
        throw 'The issue acceptance criteria do not match the authorized local plan.'
    }

    if ($EvidenceValidated -and $operation.Status -eq 'failed') {
        throw 'Failed operation evidence cannot be marked as validated.'
    }
    if ($EvidenceValidated -and $operation.Status -eq 'running') {
        throw 'Running operation evidence cannot be marked as validated before completion.'
    }

    $currentWorktreeFingerprint = Get-GoalWorktreeFingerprint -Root $root -ExcludePaths @($checkpoint, $temporaryCheckpoint, $checkpointLockPath)
    if ($null -eq $contractResult.PSObject.Properties['DependenciesAndReadiness']) {
        throw 'Issue contract validator did not return the DependenciesAndReadiness section.'
    }
    $dependenciesAndReadiness = [string]$contractResult.DependenciesAndReadiness
    $validatedDigest = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedIssueBodyDigest']) { [string]$previous.ValidatedIssueBodyDigest } else { '' }
    $validatedHead = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedHeadRevision']) { [string]$previous.ValidatedHeadRevision } else { '' }
    $validatedBase = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedBaseRevision']) { [string]$previous.ValidatedBaseRevision } else { '' }
    $validatedWorktree = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedWorktreeFingerprint']) { [string]$previous.ValidatedWorktreeFingerprint } else { '' }
    $scopeBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineIssueBodyDigest']) { [string]$previous.BaselineIssueBodyDigest } elseif ($null -ne $previous) { [string]$previous.IssueBodyDigest } else { '' }
    $scopeContractBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineContractBodyDigest']) { [string]$previous.BaselineContractBodyDigest } elseif ($null -ne $previous) { [string]$previous.ContractBodyDigest } else { '' }
    $scopeContractSource = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineContractSource']) { [string]$previous.BaselineContractSource } elseif ($null -ne $previous) { [string]$previous.ContractSource } else { '' }
    $headBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineHeadRevision']) { [string]$previous.BaselineHeadRevision } elseif (-not [string]::IsNullOrWhiteSpace($validatedHead)) { $validatedHead } elseif ($null -ne $previous) { [string]$previous.HeadRevision } else { '' }
    $baseBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineBaseRevision']) { [string]$previous.BaselineBaseRevision } elseif (-not [string]::IsNullOrWhiteSpace($validatedBase)) { $validatedBase } elseif ($null -ne $previous) { [string]$previous.BaseRevision } else { '' }
    $baseNameBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineBaseRevisionName']) { [string]$previous.BaselineBaseRevisionName } elseif ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaseRevisionName']) { [string]$previous.BaseRevisionName } else { '' }
    $worktreeBaseline = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['BaselineWorktreeFingerprint']) { [string]$previous.BaselineWorktreeFingerprint } elseif (-not [string]::IsNullOrWhiteSpace($validatedWorktree)) { $validatedWorktree } elseif ($null -ne $previous -and $null -ne $previous.PSObject.Properties['WorktreeFingerprint']) { [string]$previous.WorktreeFingerprint } else { '' }
    $scopeChanged = $null -ne $previous -and ($scopeBaseline -ne $issueBodyDigest -or $scopeContractBaseline -ne $contractBodyDigest -or $scopeContractSource -ne $contractSource)
    $revisionChanged = $null -ne $previous -and (($headBaseline -ne $currentHead) -or ($baseBaseline -ne $currentBase) -or ($baseNameBaseline -ne $BaseRevision))
    $worktreeChanged = $null -ne $previous -and $worktreeBaseline -ne $currentWorktreeFingerprint
    $activeOperation = $operation.Status -eq 'running'
    $operationCompleted = $null -ne $previous -and $null -ne $previous.PSObject.Properties['Operation'] -and [string]$previous.Operation.Status -eq 'running' -and $operation.Status -in @('completed', 'failed')
    $validatedNow = $EvidenceValidated -and $operation.Status -notin @('running', 'failed')
    $status = if ($activeOperation) { 'operation-running' } elseif ($scopeChanged -and -not $validatedNow) { 'scope-changed' } elseif ($revisionChanged -or $worktreeChanged) { 'evidence-stale' } elseif ($null -eq $previous) { 'started' } else { 'resumed' }
    $evidenceFresh = -not $activeOperation -and ($validatedNow -or (-not $scopeChanged -and -not $revisionChanged -and -not $worktreeChanged -and ($null -ne $previous -and [bool]$previous.EvidenceFresh)))
    $nextAction = if ($activeOperation) { "wait-for-existing-operation:$($operation.Handle)" } elseif ($scopeChanged -and -not $validatedNow) { 'reconcile-edited-issue-before-implementation' } elseif ($validatedNow) { 'continue-implementation-or-review' } elseif ($operationCompleted) { 'inspect-completed-operation-result' } elseif ($revisionChanged -or $worktreeChanged) { 'invalidate-stale-evidence-and-revalidate' } elseif ($null -eq $previous) { 'inspect-guidance-and-prerequisites' } else { [string]$previous.NextAction }
    if ([string]::IsNullOrWhiteSpace($nextAction)) { $nextAction = 'inspect-guidance-and-prerequisites' }

    $currentContract = [ordered]@{
        IssueTextIsData = $true
        CommandsExecutedFromIssueText = $false
        ToolsInstalledFromIssueText = $false
        SecretsAccessedFromIssueText = $false
        AcceptanceCriteria = ConvertTo-GoalArray -Values @($contractResult.AcceptanceCriteria)
        DependenciesAndReadiness = $dependenciesAndReadiness
    }
    $previousValidatedContract = if ($null -ne $previous -and $null -ne $previous.PSObject.Properties['ValidatedContract']) {
        $previous.ValidatedContract
    }
    elseif ($null -ne $previous -and [bool]$previous.EvidenceFresh -and $null -ne $previous.PSObject.Properties['Contract']) {
        $previous.Contract
    }
    else {
        [ordered]@{}
    }
    $validatedContract = if ($validatedNow -or $evidenceFresh) { $currentContract } else { $previousValidatedContract }
    $baselineIssueBodyDigest = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $issueBodyDigest } else { $scopeBaseline }
    $baselineContractBodyDigest = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $contractBodyDigest } else { $scopeContractBaseline }
    $baselineContractSource = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $contractSource } else { $scopeContractSource }
    $baselineHeadRevision = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $currentHead } else { $headBaseline }
    $baselineBaseRevision = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $currentBase } else { $baseBaseline }
    $baselineBaseRevisionName = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $BaseRevision } else { $baseNameBaseline }
    $baselineWorktreeFingerprint = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $currentWorktreeFingerprint } else { $worktreeBaseline }
    $reviewWork = Merge-GoalCollection -Existing (Get-GoalCollection -Object $previous -Name 'OutstandingReviewWork') -Added (Expand-GoalStringCollection -Values $OutstandingReviewWork)
    $resolvedReviewWork = @(Expand-GoalStringCollection -Values $ResolvedReviewWork)
    $reviewWork = @($reviewWork | Where-Object { $_ -notin $resolvedReviewWork })
    $reviewedSourceRevision = if ($validatedNow -or $evidenceFresh -or $null -eq $previous) { $currentHead } elseif ($null -ne $previous.PSObject.Properties['ReviewedSourceRevision']) { [string]$previous.ReviewedSourceRevision } else { $validatedHead }
    $verificationHead = Get-GoalRevision -Root $root -Name 'HEAD'
    $verificationWorktreeFingerprint = Get-GoalWorktreeFingerprint -Root $root -ExcludePaths @($checkpoint, $temporaryCheckpoint, $checkpointLockPath)
    $verificationBase = Get-GoalRevision -Root $root -Name $BaseRevision
    if ($verificationHead -ne $checkedOutHead -or $verificationBase -ne $currentBase -or $verificationWorktreeFingerprint -ne $currentWorktreeFingerprint) {
        throw 'Repository head or worktree changed while evidence was being collected; retry the goal route.'
    }

    $record = [ordered]@{
        SchemaVersion = '1.0'
        Issue = [ordered]@{
            Number = $IssueNumber
            Url = [string]$issue.html_url
            Title = [string]$issue.title
            State = [string]$issue.state
            Repository = "$RepositoryOwner/$RepositoryName"
        }
        IssueBodyDigest = $issueBodyDigest
        ContractBodyDigest = $contractBodyDigest
        ContractSource = $contractSource
        ReviewedSourceRevision = $reviewedSourceRevision
        HeadRevision = $currentHead
        BaseRevision = $currentBase
        BaseRevisionName = $BaseRevision
        WorktreeFingerprint = $currentWorktreeFingerprint
        BaselineIssueBodyDigest = $baselineIssueBodyDigest
        BaselineContractBodyDigest = $baselineContractBodyDigest
        BaselineContractSource = $baselineContractSource
        BaselineHeadRevision = $baselineHeadRevision
        BaselineBaseRevision = $baselineBaseRevision
        BaselineBaseRevisionName = $baselineBaseRevisionName
        BaselineWorktreeFingerprint = $baselineWorktreeFingerprint
        ValidatedIssueBodyDigest = if ($validatedNow -or $evidenceFresh) { $issueBodyDigest } else { $validatedDigest }
        ValidatedHeadRevision = if ($validatedNow -or $evidenceFresh) { $currentHead } else { $validatedHead }
        ValidatedBaseRevision = if ($validatedNow -or $evidenceFresh) { $currentBase } else { $validatedBase }
        ValidatedWorktreeFingerprint = if ($validatedNow -or $evidenceFresh) { $currentWorktreeFingerprint } else { $validatedWorktree }
        Worktree = $root
        Action = $Action
        Status = $status
        EvidenceFresh = $evidenceFresh
        MergeBoundary = if ($MergeAuthorized) { 'MERGE_AUTHORIZED_BY_USER_BUT_NOT_PERFORMED' } else { 'PR_READY_NOT_MERGED' }
        Contract = $currentContract
        ValidatedContract = $validatedContract
        Decisions = ConvertTo-GoalArray -Values (Merge-GoalCollection -Existing (Get-GoalCollection -Object $previous -Name 'Decisions') -Added (Expand-GoalStringCollection -Values $Decisions))
        AcceptanceEvidence = ConvertTo-GoalArray -Values (Merge-GoalCollection -Existing (Get-GoalCollection -Object $previous -Name 'AcceptanceEvidence') -Added (Expand-GoalStringCollection -Values $AcceptanceEvidence))
        AttemptedFixes = ConvertTo-GoalArray -Values (Merge-GoalCollection -Existing (Get-GoalCollection -Object $previous -Name 'AttemptedFixes') -Added (Expand-GoalStringCollection -Values $AttemptedFixes))
        OutstandingReviewWork = ConvertTo-GoalArray -Values $reviewWork
        Operation = [ordered]@{ Status = $operation.Status; Handle = $operation.Handle; Name = $operation.Name }
        NextAction = $nextAction
        UpdatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    }

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
        IssueBodyDigest = $issueBodyDigest
        HeadRevision = $currentHead
        BaseRevision = $currentBase
        BaseRevisionName = $BaseRevision
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
finally {
    if ($null -ne $checkpointLockStream) {
        $checkpointLockStream.Dispose()
    }
}
