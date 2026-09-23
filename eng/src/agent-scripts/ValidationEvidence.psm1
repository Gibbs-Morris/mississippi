#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ValidationRepositoryRevision {
    param([Parameter(Mandatory)][string]$RepositoryRoot)
    $root = $RepositoryRoot.Replace('\', '/')
    $output = @(& git -c "safe.directory=$root" -C $RepositoryRoot rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or $output.Count -ne 1) { return 'unknown' }
    return ([string]$output[0]).Trim()
}

function Get-ValidationRelativePath {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$Path)
    $fullRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $fullRoot $Path)) }
    $relative = [System.IO.Path]::GetRelativePath($fullRoot, $fullPath).Replace('\', '/')
    if ($relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) { return $null }
    return $relative
}

function Get-ValidationLinkTarget {
    param([Parameter(Mandatory)][System.IO.FileSystemInfo]$Item)

    $linkTargetProperty = $Item.PSObject.Properties['LinkTarget']
    $linkTarget = if ($null -ne $linkTargetProperty) { [string]$linkTargetProperty.Value } else { '' }
    if ([string]::IsNullOrWhiteSpace($linkTarget) -and $null -ne $Item.PSObject.Properties['Target']) {
        $linkTarget = [string]$Item.Target
    }
    if ([string]::IsNullOrWhiteSpace($linkTarget)) { return $null }
    return $linkTarget
}

function Get-ValidationReparseTargetPath {
    param([Parameter(Mandatory)][string]$Path)

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    if ($null -eq $item -or (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)) { return '' }
    $linkTarget = Get-ValidationLinkTarget -Item $item
    if ([string]::IsNullOrWhiteSpace($linkTarget)) { throw "Unable to resolve symbolic-link target for '$Path'." }
    if ([System.IO.Path]::IsPathRooted($linkTarget)) { return [System.IO.Path]::GetFullPath($linkTarget) }
    return [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $Path) $linkTarget))
}

function Test-ValidationContainedPath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Resolved)

    $rootWithSeparator = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $pathComparison = if ($env:OS -eq 'Windows_NT') { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    return $Resolved.Equals($Root, $pathComparison) -or $Resolved.StartsWith($rootWithSeparator, $pathComparison)
}

function Resolve-ValidationContainedPath {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$Path, [switch]$ReturnResolvedPath)

    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $relative = Get-ValidationRelativePath -RepositoryRoot $root -Path $Path
    if ($null -eq $relative) { return $null }
    $resolved = $root
    foreach ($segment in ($relative -split '[\\/]')) {
        if ([string]::IsNullOrWhiteSpace($segment) -or $segment -eq '.') { continue }
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $resolved $segment))
        $target = Get-ValidationReparseTargetPath -Path $candidate
        $resolved = if ([string]::IsNullOrWhiteSpace($target)) { $candidate } else { $target }
    }
    for ($linkDepth = 0; $linkDepth -lt 40; $linkDepth++) {
        $target = Get-ValidationReparseTargetPath -Path $resolved
        if ([string]::IsNullOrWhiteSpace($target)) { break }
        $resolved = $target
        if ($linkDepth -eq 39) { return $null }
    }
    if (-not (Test-ValidationContainedPath -Root $root -Resolved $resolved)) { return $null }
    if ($ReturnResolvedPath) { return [string]$resolved }
    return [string][System.IO.Path]::GetFullPath((Join-Path $root ([string]$relative)))
}

function Get-ValidationPathMetadata {
    param(
        [Parameter(Mandatory)][string]$Path,
        [AllowEmptyString()][string]$LinkTarget = ''
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { return $null }
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Path '$Path' must be resolved before metadata is collected."
    }

    $fileHash = 'SHA256:' + (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not [string]::IsNullOrWhiteSpace($LinkTarget)) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("SYMLINK:$LinkTarget`nTARGET:$fileHash")
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $fileHash = 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    return [pscustomobject]@{ SHA256 = $fileHash; Length = $item.Length }
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
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            $files.Add([pscustomobject]@{ Path = $relative; SHA256 = 'MISSING' })
            continue
        }
        $resolvedPaths = @(Resolve-ValidationContainedPath -RepositoryRoot $root -Path $relative -ReturnResolvedPath | ForEach-Object { [string]$_ })
        if ($resolvedPaths.Count -ne 1 -or [string]::IsNullOrWhiteSpace(($resolvedPaths -join ''))) { throw "Source input escapes the verification root: '$relative'." }
        $sourceItem = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        $linkTarget = if (($sourceItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { Get-ValidationLinkTarget -Item $sourceItem } else { '' }
        $metadata = Get-ValidationPathMetadata -Path ($resolvedPaths -join '') -LinkTarget $linkTarget
        if ($null -ne $metadata) {
            $files.Add([pscustomobject]@{ Path = $relative; SHA256 = [string]$metadata.SHA256 })
        }
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
        InputPath = @($InputPath)
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

function Get-ValidationRecordedArtifactMetadata {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [AllowEmptyCollection()][object[]]$ArtifactPaths = @())

    $missing = [System.Collections.Generic.List[string]]::new()
    $metadata = [System.Collections.Generic.List[object]]::new()
    foreach ($artifact in @($ArtifactPaths)) {
        $artifactText = @($artifact) -join ''
        if ([string]::IsNullOrWhiteSpace($artifactText)) { $missing.Add('[invalid artifact path]'); continue }
        $resolvedPaths = @(Resolve-ValidationContainedPath -RepositoryRoot $RepositoryRoot -Path $artifactText -ReturnResolvedPath | ForEach-Object { [string]$_ })
        if ($resolvedPaths.Count -ne 1) { $missing.Add($artifactText); continue }
        $resolvedPath = $resolvedPaths -join ''
        $artifactItem = Get-Item -LiteralPath (Join-Path $RepositoryRoot $artifactText) -Force -ErrorAction SilentlyContinue
        $linkTarget = if ($null -ne $artifactItem -and (($artifactItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) { Get-ValidationLinkTarget -Item $artifactItem } else { '' }
        try { $fileMetadata = Get-ValidationPathMetadata -Path $resolvedPath -LinkTarget $linkTarget } catch { $fileMetadata = $null }
        if ($null -eq $fileMetadata) { $missing.Add($artifactText); continue }
        $metadata.Add([pscustomobject]@{ Path = $artifactText; SHA256 = [string]$fileMetadata.SHA256; Length = [int64]$fileMetadata.Length })
    }
    return [pscustomobject]@{ Missing = @($missing); Metadata = @($metadata) }
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
    $artifactResult = Get-ValidationRecordedArtifactMetadata -RepositoryRoot $record.RepositoryRoot -ArtifactPaths @($record.Artifacts)
    $record.ArtifactMetadata = @($artifactResult.Metadata)
    if (@($artifactResult.Missing).Count -gt 0) {
        $missingMessage = 'Recorded artifacts are missing: ' + (@($artifactResult.Missing) -join ', ')
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
    $requiredProperties = @('SchemaVersion', 'Status', 'RepositoryRoot', 'InputPath', 'SourceBefore', 'SourceAfter', 'Executed', 'TestCount', 'ExitCode', 'SourceChangedDuringRun', 'Artifacts', 'ArtifactMetadata')
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
    if ([string]$record.Status -eq 'PASS') {
        try {
            if ($null -eq $record.ExitCode) { $errors.Add('PASS evidence requires a numeric zero exit code.') }
            elseif ([int]$record.ExitCode -ne 0) { $errors.Add('PASS evidence requires a zero exit code.') }
        }
        catch { $errors.Add('PASS evidence requires a numeric zero exit code.') }
    }
    if ([string]$record.Status -in @('PASS', 'FAIL') -and $null -eq $record.SourceAfter) { $errors.Add('Completed evidence has no final source fingerprint.') }
    if ([string]$record.Status -eq 'PASS' -and @($record.Artifacts).Count -eq 0) { $errors.Add('PASS requires at least one recorded artifact.') }
    if ([string]$record.Status -eq 'PASS' -and [bool]$record.SourceChangedDuringRun) { $errors.Add('PASS evidence was changed by a later cleanup or source edit.') }
    foreach ($artifact in @($record.Artifacts)) {
        $artifactText = @($artifact) -join ''
        if ([string]::IsNullOrWhiteSpace($artifactText)) {
            $errors.Add("Required artifact is missing: '$artifact'.")
            continue
        }
        $artifactResolvedPaths = @(Resolve-ValidationContainedPath -RepositoryRoot $verificationRoot -Path $artifactText -ReturnResolvedPath | ForEach-Object { [string]$_ })
        if ([System.IO.Path]::IsPathRooted($artifactText) -or $artifactResolvedPaths.Count -ne 1) {
            $errors.Add("Artifact path escapes the verification root: '$artifactText'.")
            continue
        }
        $artifactResolvedPath = $artifactResolvedPaths -join ''
        if ([string]::IsNullOrWhiteSpace($artifactResolvedPath)) {
            $errors.Add("Required artifact is missing: '$artifactText'.")
            continue
        }
        $artifactItem = Get-Item -LiteralPath (Join-Path $verificationRoot $artifactText) -Force -ErrorAction SilentlyContinue
        $linkTarget = if ($null -ne $artifactItem -and (($artifactItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) { Get-ValidationLinkTarget -Item $artifactItem } else { '' }
        try { $artifactMetadata = Get-ValidationPathMetadata -Path $artifactResolvedPath -LinkTarget $linkTarget } catch { $artifactMetadata = $null }
        if ($null -eq $artifactMetadata) { $errors.Add("Required artifact is missing: '$artifactText'.") }
    }
    $trxExecutedTotal = [int64]0
    $trxFailedTotal = [int64]0
    $artifactSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($artifact in @($record.Artifacts)) { $artifactText = @($artifact) -join ''; if (-not [string]::IsNullOrWhiteSpace($artifactText)) { $null = $artifactSet.Add($artifactText) } }
    $metadataByPath = @{}
    foreach ($metadata in @($record.ArtifactMetadata | Where-Object { $null -ne $_ })) {
        try {
            if ($null -eq $metadata) { throw 'Artifact metadata entry is null.' }
            $pathProperty = $metadata.PSObject.Properties['Path']
            $hashProperty = $metadata.PSObject.Properties['SHA256']
            $lengthProperty = $metadata.PSObject.Properties['Length']
            if ($null -eq $pathProperty -or $null -eq $hashProperty -or $null -eq $lengthProperty) { throw 'Artifact metadata must contain Path, SHA256, and Length.' }
            $metadataPath = @($pathProperty.Value) -join ''
            $metadataHash = [string]$hashProperty.Value
            if ([string]::IsNullOrWhiteSpace($metadataPath) -or $metadataHash -notmatch '^SHA256:[0-9a-fA-F]{64}$') { throw 'Artifact metadata has an invalid path or SHA256 value.' }
            $metadataResolvedPaths = @(Resolve-ValidationContainedPath -RepositoryRoot $verificationRoot -Path $metadataPath -ReturnResolvedPath | ForEach-Object { [string]$_ })
            if ([System.IO.Path]::IsPathRooted($metadataPath) -or $metadataResolvedPaths.Count -ne 1 -or [string]::IsNullOrWhiteSpace(($metadataResolvedPaths -join ''))) { throw "Artifact path escapes the verification root: '$metadataPath'." }
            $metadataResolvedPath = $metadataResolvedPaths -join ''
            if (-not $metadataByPath.ContainsKey($metadataPath)) { $metadataByPath[$metadataPath] = [System.Collections.Generic.List[object]]::new() }
            $metadataByPath[$metadataPath].Add($metadata)
            if (-not $artifactSet.Contains($metadataPath)) { $errors.Add("Artifact metadata references an unrecorded artifact: '$metadataPath'."); continue }
            $artifactPath = $metadataResolvedPath
            $metadataItem = Get-Item -LiteralPath (Join-Path $verificationRoot $metadataPath) -Force -ErrorAction SilentlyContinue
            $linkTarget = if ($null -ne $metadataItem -and (($metadataItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) { Get-ValidationLinkTarget -Item $metadataItem } else { '' }
            $currentMetadata = Get-ValidationPathMetadata -Path $artifactPath -LinkTarget $linkTarget
            if ($null -eq $currentMetadata) { continue }
            $currentHash = [string]$currentMetadata.SHA256
            if ($currentHash -ne $metadataHash) { $errors.Add("Artifact content changed: '$metadataPath'.") }
            if ([int64]$lengthProperty.Value -ne [int64]$currentMetadata.Length) { $errors.Add("Artifact length changed: '$metadataPath'.") }
            switch ([System.IO.Path]::GetExtension($metadataPath).ToLowerInvariant()) {
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
                    $passed = [int64]$counters.passed
                    $failed = [int64]$counters.failed
                    $notExecuted = [int64]$counters.notExecuted
                    $total = [int64]$counters.total
                    if ($executed -gt $total -or $passed + $failed -gt $executed -or $executed + $notExecuted -ne $total) { throw 'TRX document has inconsistent execution counters.' }
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
        $current = Get-ValidationSourceFingerprint -RepositoryRoot $verificationRoot -InputPath @($record.InputPath)
        $fresh = $null -ne $record.SourceAfter -and [string]$record.SourceAfter.Fingerprint -eq [string]$current.Fingerprint
    }
    catch { $fresh = $false; $errors.Add('Current source fingerprint could not be collected.') }
    if (-not $fresh) { $errors.Add('Evidence inputs are stale or changed.') }
    [pscustomobject]@{ Valid = $errors.Count -eq 0; Fresh = $fresh; Errors = @($errors); Record = $record }
}

Export-ModuleMember -Function Get-ValidationSourceFingerprint, New-ValidationEvidenceRun, Complete-ValidationEvidenceRun, Write-ValidationEvidence, Test-ValidationEvidence
