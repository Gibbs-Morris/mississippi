#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ValidationRepositoryRevision {
    param([Parameter(Mandatory)][string]$RepositoryRoot)
    $root = $RepositoryRoot.Replace('\', '/')
    $output = @(& git -c "safe.directory=$root" -C $RepositoryRoot rev-parse HEAD 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0) { return 'unknown' }
    return ([string]$output).Trim()
}

function Get-ValidationRelativePath {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$Path)
    $fullRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $fullRoot $Path)) }
    $relative = [System.IO.Path]::GetRelativePath($fullRoot, $fullPath).Replace('\', '/')
    if ($relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) { return $null }
    return $relative
}

function Test-ValidationZeroTestFacadeArtifact {
    param([Parameter(Mandatory)][string]$Path)

    return $Path.Replace('\', '/') -match '(?i)(?:^|/)Sdk\.(?:Client|Gateway|Runtime)\.L0Tests(?:/|/[^/]*\.trx$)'
}

function Get-ValidationSourceFingerprint { # NOSONAR - source evidence fingerprinting intentionally coordinates Git state and bounded file hashing.
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string[]]$InputPath = @()
    )

    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $revision = Get-ValidationRepositoryRevision -RepositoryRoot $root
    $statusLines = @(& git -c "safe.directory=$($root.Replace('\', '/'))" -C $root status --porcelain=v1 --untracked-files=all 2>$null)
    $dirty = $statusLines.Count -gt 0
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    if (@($InputPath).Count -gt 0) {
        foreach ($path in @($InputPath)) {
            $fullInput = if ([System.IO.Path]::IsPathRooted([string]$path)) { [System.IO.Path]::GetFullPath([string]$path) } else { [System.IO.Path]::GetFullPath((Join-Path $root ([string]$path))) }
            if (Test-Path -LiteralPath $fullInput -PathType Container) {
                foreach ($file in Get-ChildItem -LiteralPath $fullInput -Recurse -File -Force) {
                    $relative = Get-ValidationRelativePath -RepositoryRoot $root -Path $file.FullName
                    if ($null -ne $relative) { $null = $paths.Add($relative) }
                }
            }
            else {
                $relative = Get-ValidationRelativePath -RepositoryRoot $root -Path ([string]$path)
                if ($null -ne $relative) { $null = $paths.Add($relative) }
            }
        }
    }
    else {
        $tracked = @(& git -c "safe.directory=$($root.Replace('\', '/'))" -C $root ls-files --cached --others --exclude-standard 2>$null)
        foreach ($path in $tracked) { $null = $paths.Add(([string]$path).Replace('\', '/')) }
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($relative in @($paths | Sort-Object)) {
        $fullPath = Join-Path $root ($relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }
        $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $files.Add([pscustomobject]@{ Path = $relative; SHA256 = "SHA256:$hash" })
    }
    $identity = [ordered]@{ Revision = $revision; Dirty = $dirty; Files = @($files) } | ConvertTo-Json -Depth 8 -Compress
    $identityBytes = [System.Text.Encoding]::UTF8.GetBytes($identity)
    $identityHash = [System.Security.Cryptography.SHA256]::HashData($identityBytes)
    [pscustomobject][ordered]@{
        Revision = $revision
        Dirty = $dirty
        Files = @($files)
        Fingerprint = 'SHA256:' + (($identityHash | ForEach-Object { $_.ToString('x2') }) -join '')
    }
}

function Write-ValidationEvidence {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Record, [Parameter(Mandatory)][string]$Path)

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = "$Path.$PID.tmp"
    $Record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $temporary -Encoding utf8
    Move-Item -LiteralPath $temporary -Destination $Path -Force
    return $Path
}

function New-ValidationEvidenceRun {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Scope,
        [string[]]$InputPath = @(),
        [string[]]$Arguments = @(),
        [string]$BaseRevision = 'unknown'
    )

    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $runId = [guid]::NewGuid().ToString('N')
    $runDirectory = Join-Path $root ".scratchpad/validation-evidence/$runId"
    $path = Join-Path $runDirectory 'evidence.json'
    $before = Get-ValidationSourceFingerprint -RepositoryRoot $root -InputPath $InputPath
    $record = [ordered]@{
        SchemaVersion = '1.0'
        RunId = $runId
        Scope = $Scope
        RepositoryRoot = $root
        BaseRevision = $BaseRevision
        HeadRevision = $before.Revision
        Arguments = @($Arguments)
        ToolVersions = [ordered]@{ PowerShell = [string]$PSVersionTable.PSVersion }
        SourceBefore = $before
        SourceAfter = $null
        Status = 'INCOMPLETE'
        Phase = 'started'
        Executed = $false
        TestCount = 0
        ExitCode = $null
        Artifacts = @()
        Error = $null
        SourceChangedDuringRun = $false
        ArtifactMetadata = @()
        StartedUtc = (Get-Date).ToUniversalTime().ToString('o')
        EndedUtc = $null
        EvidencePath = $path
    }
    Write-ValidationEvidence -Record $record -Path $path | Out-Null
    return [pscustomobject]@{ Record = $record; Path = $path; InputPath = @($InputPath) }
}

function Complete-ValidationEvidenceRun {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Run,
        [Parameter(Mandatory)][ValidateSet('PASS', 'FAIL', 'INCOMPLETE', 'SKIPPED', 'READY')][string]$Status,
        [Parameter(Mandatory)][string]$Phase,
        [bool]$Executed = $true,
        [int]$TestCount = 0,
        [Nullable[int]]$ExitCode,
        [string[]]$ArtifactPath = @(),
        [string]$MirrorPath,
        [AllowEmptyString()][string]$ErrorMessage
    )

    $record = $Run.Record
    $record.Status = $Status
    $record.Phase = $Phase
    $record.Executed = $Executed
    $record.TestCount = $TestCount
    $record.ExitCode = $ExitCode
    $record.Error = if ([string]::IsNullOrWhiteSpace($ErrorMessage)) { $null } else { $ErrorMessage }
    $record.Artifacts = @($ArtifactPath | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace([string]$_)) { return $null }
        Get-ValidationRelativePath -RepositoryRoot $record.RepositoryRoot -Path ([string]$_)
    })
    $record.SourceAfter = Get-ValidationSourceFingerprint -RepositoryRoot $record.RepositoryRoot -InputPath $Run.InputPath
    $record.SourceChangedDuringRun = [string]$record.SourceBefore.Fingerprint -ne [string]$record.SourceAfter.Fingerprint
    $missingArtifacts = [System.Collections.Generic.List[string]]::new()
    $record.ArtifactMetadata = @($record.Artifacts | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace([string]$_)) {
            $missingArtifacts.Add('[invalid artifact path]')
            return
        }
        $artifactPath = Join-Path $record.RepositoryRoot ([string]$_)
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
            $missingArtifacts.Add([string]$_)
            return
        }
        [pscustomobject]@{ Path = [string]$_; SHA256 = 'SHA256:' + (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant(); Length = (Get-Item -LiteralPath $artifactPath).Length }
    })
    if ($missingArtifacts.Count -gt 0) {
        $missingMessage = 'Recorded artifacts are missing: ' + ($missingArtifacts -join ', ')
        $record.Error = if ([string]::IsNullOrWhiteSpace([string]$record.Error)) { $missingMessage } else { "$($record.Error) $missingMessage" }
        if ($record.Status -eq 'PASS') {
            $record.Status = 'INCOMPLETE'
            $record.ExitCode = 1
        }
    }
    if ($Status -eq 'PASS' -and $record.SourceChangedDuringRun) {
        $record.Status = 'INCOMPLETE'
        $record.ExitCode = 1
        $record.Error = 'Validation inputs changed during the run; PASS requires revalidation of the final source tree.'
    }
    $record.EndedUtc = (Get-Date).ToUniversalTime().ToString('o')
    Write-ValidationEvidence -Record $record -Path $Run.Path | Out-Null
    if (-not [string]::IsNullOrWhiteSpace($MirrorPath)) { Write-ValidationEvidence -Record $record -Path $MirrorPath | Out-Null }
    return $record
}

function Test-ValidationEvidence { # NOSONAR - evidence verification intentionally validates source, artifact, and schema state together.
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path, [string]$RepositoryRoot)

    $errors = [System.Collections.Generic.List[string]]::new()
    try { $record = Get-Content -LiteralPath $Path -Raw -ErrorAction Stop | ConvertFrom-Json }
    catch { return [pscustomobject]@{ Valid = $false; Fresh = $false; Errors = @('Evidence is missing or unreadable.'); Record = $null } }
    $requiredProperties = @('SchemaVersion', 'Status', 'RepositoryRoot', 'SourceBefore', 'SourceAfter', 'Executed', 'TestCount', 'SourceChangedDuringRun', 'Artifacts', 'ArtifactMetadata')
    foreach ($property in $requiredProperties) {
        if ($null -eq $record.PSObject.Properties[$property]) { $errors.Add("Evidence is missing required field '$property'.") }
    }
    if ($errors.Count -gt 0) { return [pscustomobject]@{ Valid = $false; Fresh = $false; Errors = @($errors); Record = $record } }
    $verificationRoot = if (-not [string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    }
    elseif (Test-Path -LiteralPath ([string]$record.RepositoryRoot) -PathType Container) {
        (Resolve-Path -LiteralPath $record.RepositoryRoot -ErrorAction Stop).Path
    }
    else {
        Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $Path)))
    }
    if ([string]$record.SchemaVersion -ne '1.0') { $errors.Add('Unsupported evidence schema.') }
    if ([string]$record.Status -notin @('PASS', 'FAIL', 'INCOMPLETE', 'SKIPPED', 'READY')) { $errors.Add('Evidence status is invalid.') }
    if ([string]$record.Status -eq 'PASS' -and (-not [bool]$record.Executed -or [int]$record.TestCount -lt 1)) { $errors.Add('PASS requires executed tests and a nonzero test count.') }
    if ([string]$record.Status -in @('PASS', 'FAIL') -and $null -eq $record.SourceAfter) { $errors.Add('Completed evidence has no final source fingerprint.') }
    if ([string]$record.Status -eq 'PASS' -and @($record.Artifacts).Count -eq 0) { $errors.Add('PASS requires at least one recorded artifact.') }
    if ([string]$record.Status -eq 'PASS' -and [bool]$record.SourceChangedDuringRun) { $errors.Add('PASS evidence was changed by a later cleanup or source edit.') }
    foreach ($artifact in @($record.Artifacts)) {
        $artifactText = [string]$artifact
        if ([string]::IsNullOrWhiteSpace($artifactText)) {
            $errors.Add("Required artifact is missing: '$artifact'.")
            continue
        }
        if ([System.IO.Path]::IsPathRooted($artifactText) -or $null -eq (Get-ValidationRelativePath -RepositoryRoot $verificationRoot -Path $artifactText)) {
            $errors.Add("Artifact path escapes the verification root: '$artifactText'.")
            continue
        }
        if (-not (Test-Path -LiteralPath (Join-Path $verificationRoot $artifactText) -PathType Leaf)) { $errors.Add("Required artifact is missing: '$artifactText'.") }
    }
    $trxExecutedTotal = [int64]0
    $trxFailedTotal = [int64]0
    $artifactSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($artifact in @($record.Artifacts)) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $null = $artifactSet.Add([string]$artifact) } }
    $metadataByPath = @{}
    foreach ($metadata in @($record.ArtifactMetadata | Where-Object { $null -ne $_ })) {
        try {
            if ($null -eq $metadata) { throw 'Artifact metadata entry is null.' }
            $pathProperty = $metadata.PSObject.Properties['Path']
            $hashProperty = $metadata.PSObject.Properties['SHA256']
            $lengthProperty = $metadata.PSObject.Properties['Length']
            if ($null -eq $pathProperty -or $null -eq $hashProperty -or $null -eq $lengthProperty) { throw 'Artifact metadata must contain Path, SHA256, and Length.' }
            $metadataPath = [string]$pathProperty.Value
            $metadataHash = [string]$hashProperty.Value
            if ([string]::IsNullOrWhiteSpace($metadataPath) -or $metadataHash -notmatch '^SHA256:[0-9a-fA-F]{64}$') { throw 'Artifact metadata has an invalid path or SHA256 value.' }
            if ([System.IO.Path]::IsPathRooted($metadataPath) -or $null -eq (Get-ValidationRelativePath -RepositoryRoot $verificationRoot -Path $metadataPath)) { throw "Artifact path escapes the verification root: '$metadataPath'." }
            if (-not $metadataByPath.ContainsKey($metadataPath)) { $metadataByPath[$metadataPath] = [System.Collections.Generic.List[object]]::new() }
            $metadataByPath[$metadataPath].Add($metadata)
            if (-not $artifactSet.Contains($metadataPath)) { $errors.Add("Artifact metadata references an unrecorded artifact: '$metadataPath'."); continue }
            $artifactPath = Join-Path $verificationRoot $metadataPath
            if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) { continue }
            $currentHash = 'SHA256:' + (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($currentHash -ne $metadataHash) { $errors.Add("Artifact content changed: '$metadataPath'.") }
            if ([int64]$lengthProperty.Value -ne [int64](Get-Item -LiteralPath $artifactPath).Length) { $errors.Add("Artifact length changed: '$metadataPath'.") }
            switch ([System.IO.Path]::GetExtension($artifactPath).ToLowerInvariant()) {
                '.json' { Get-Content -LiteralPath $artifactPath -Raw | ConvertFrom-Json | Out-Null }
                '.xml' { [xml]$xml = Get-Content -LiteralPath $artifactPath -Raw; if ($null -eq $xml.DocumentElement) { throw 'XML document has no root element.' } }
                '.trx' {
                    [xml]$trx = Get-Content -LiteralPath $artifactPath -Raw
                    if ($null -eq $trx.TestRun) { throw 'TRX document has no TestRun element.' }
                    $summaryProperty = $trx.TestRun.PSObject.Properties['ResultSummary']
                    if ($null -eq $summaryProperty) { throw 'TRX document has no completed ResultSummary.' }
                    $summary = $summaryProperty.Value
                    if ($null -eq $summary -or [string]$summary.outcome -ne 'Completed') { throw 'TRX document has no completed ResultSummary.' }
                    $countersProperty = $summary.PSObject.Properties['Counters']
                    if ($null -eq $countersProperty) { throw 'TRX document has no ResultSummary.Counters.' }
                    $counters = $countersProperty.Value
                    foreach ($counterName in @('total', 'executed', 'passed', 'failed', 'notExecuted')) {
                        $counter = $counters.Attributes[$counterName]
                        if ($null -eq $counter -or [string]$counter.Value -notmatch '^\d+$') { throw "TRX document has no nonnegative $counterName counter." }
                    }
                    $executed = [int64]$counters.executed
                    if ($executed -lt 1 -and -not (Test-ValidationZeroTestFacadeArtifact -Path $metadataPath)) { throw 'TRX document must report a nonzero executed counter.' }
                    $trxExecutedTotal += $executed
                    $trxFailedTotal += [int64]$counters.failed
                }
            }
        }
        catch { $errors.Add("Artifact metadata is malformed or unreadable: $($_.Exception.Message)") }
    }
    foreach ($artifact in @($record.Artifacts)) {
        $artifactPath = [string]$artifact
        if ([string]::IsNullOrWhiteSpace($artifactPath)) { continue }
        $metadataCount = if ($metadataByPath.ContainsKey($artifactPath)) { [int]$metadataByPath[$artifactPath].Count } else { 0 }
        if ($metadataCount -ne 1) { $errors.Add("Every recorded artifact must have exactly one hash metadata entry: '$artifactPath'.") }
    }
    if ($trxExecutedTotal -gt 0 -and [int64]$record.TestCount -ne $trxExecutedTotal) {
        $errors.Add("TRX executed counters ($trxExecutedTotal) do not reconcile with evidence TestCount ($($record.TestCount)).")
    }
    if ([string]$record.Status -eq 'PASS' -and $trxFailedTotal -gt 0) { $errors.Add('PASS evidence contains failed TRX tests.') }
    try {
        $current = Get-ValidationSourceFingerprint -RepositoryRoot $verificationRoot -InputPath @($record.SourceBefore.Files.Path)
        $fresh = $null -ne $record.SourceAfter -and [string]$record.SourceAfter.Fingerprint -eq [string]$current.Fingerprint
    }
    catch { $fresh = $false; $errors.Add('Current source fingerprint could not be collected.') }
    if (-not $fresh) { $errors.Add('Evidence inputs are stale or changed.') }
    [pscustomobject]@{ Valid = $errors.Count -eq 0; Fresh = $fresh; Errors = @($errors); Record = $record }
}

Export-ModuleMember -Function Get-ValidationSourceFingerprint, New-ValidationEvidenceRun, Complete-ValidationEvidenceRun, Write-ValidationEvidence, Test-ValidationEvidence
