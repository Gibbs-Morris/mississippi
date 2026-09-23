#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:CrcSchemaVersion = 'code-review-council/v1'
$script:CrcPersonas = @(
    'domain-purist',
    'distributed-systems-pessimist',
    'security-adversary',
    'boundary-architect',
    'reluctant-maintainer',
    'performance-accountant',
    'test-sceptic',
    'framework-consumer',
    'compiler-engineer',
    'on-call-engineer'
)

function ConvertTo-CrcJson {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Value)

    $canonical = ConvertTo-CrcCanonicalValue -Value $Value
    return (ConvertTo-Json -InputObject $canonical -Depth 100 -Compress)
}

function ConvertTo-CrcCanonicalValue {
    [CmdletBinding()]
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) { return $null }
    if ($Value -is [string] -or $Value -is [char] -or $Value -is [bool] -or $Value -is [ValueType]) { return $Value }
    if ($Value -is [System.Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys | ForEach-Object { [string]$_ } | Sort-Object)) {
            $result[$key] = ConvertTo-CrcCanonicalValue -Value $Value[$key]
        }
        return ,([pscustomobject]$result)
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $result = @()
        foreach ($item in $Value) { $result += ConvertTo-CrcCanonicalValue -Value $item }
        return ,$result
    }
    $result = [ordered]@{}
    foreach ($property in @($Value.PSObject.Properties | Sort-Object Name)) {
        $result[$property.Name] = ConvertTo-CrcCanonicalValue -Value $property.Value
    }
    return ,([pscustomobject]$result)
}

function Get-CrcSha256 {
    [CmdletBinding()]
    param([AllowNull()][object]$Bytes)

    $byteArray = [byte[]]::new(0)
    if ($null -ne $Bytes) { $byteArray = [byte[]]$Bytes }
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([Convert]::ToHexString($algorithm.ComputeHash([byte[]]$byteArray))).ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Get-CrcHashJson {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Value)

    $json = ConvertTo-CrcJson -Value $Value
    return "sha256:$(Get-CrcSha256 -Bytes ([System.Text.Encoding]::UTF8.GetBytes($json)))"
}

function Get-CrcTempRoot {
    return [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
}

function Get-CrcPathComparison {
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        return [System.StringComparison]::OrdinalIgnoreCase
    }
    return [System.StringComparison]::Ordinal
}

function Test-CrcPathWithin {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$Root)

    $candidate = [System.IO.Path]::GetFullPath($Path).TrimEnd([char[]]@('\','/'))
    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\','/'))
    $comparison = Get-CrcPathComparison
    return $candidate.Equals($rootPath,$comparison) -or $candidate.StartsWith("$rootPath$([System.IO.Path]::DirectorySeparatorChar)",$comparison)
}

function Test-CrcPathEqual {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Left,[Parameter(Mandatory)][string]$Right)

    $comparison = Get-CrcPathComparison
    return ([System.IO.Path]::GetFullPath($Left)).Equals([System.IO.Path]::GetFullPath($Right),$comparison)
}

function Assert-CrcNoPathTraversal {
    param([string]$Path,[string]$Label)

    if (($Path -split '[\\/]') -contains '..') { throw "$Label path traversal is not allowed: $Path" }
}

function Get-CrcPermittedRoot {
    param([string]$Candidate,[string]$WorktreeRoot,[string]$TempRoot,[string]$Label,[string]$Path)

    if (Test-CrcPathWithin -Path $Candidate -Root $WorktreeRoot) { return $WorktreeRoot }
    if (Test-CrcPathWithin -Path $Candidate -Root $TempRoot) { return $TempRoot }
    throw "$Label path must be within the worktree or temporary directory: $Path"
}

function Assert-CrcNoSymlinkTraversal {
    param([string]$Candidate,[string]$Root,[string]$Label)

    $rootItem = Get-Item -LiteralPath $Root -Force -ErrorAction SilentlyContinue
    if ($null -ne $rootItem -and ($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label root cannot be a symbolic link: $Root" }
    $relative = $Candidate.Substring($Root.Length).TrimStart([char[]]@('\','/'))
    $current = $Root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { $_ })) {
        $current = Join-Path $current $segment
        $item = Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
        if ($null -eq $item) { break }
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path cannot traverse a symbolic link: $current" }
    }
}

function Assert-CrcPathTarget {
    param([string]$Candidate,[string]$Path,[string]$Label,[bool]$MustExist,[bool]$Directory)

    $item = Get-Item -LiteralPath $Candidate -Force -ErrorAction SilentlyContinue
    if ($MustExist -and $null -eq $item) { throw "$Label path does not exist: $Path" }
    if ($MustExist -and $Directory -and ($null -eq $item -or -not $item.PSIsContainer)) { throw "$Label path is not a directory: $Path" }
    if ($MustExist -and -not $Directory -and ($null -eq $item -or $item.PSIsContainer)) { throw "$Label path is not a file: $Path" }
}

function Resolve-CrcSafePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label,
        [switch]$MustExist,
        [switch]$Directory
    )

    $candidate = [System.IO.Path]::GetFullPath($Path)
    Assert-CrcNoPathTraversal -Path $Path -Label $Label
    $worktreeRoot = (Get-Location).Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $tempRoot = Get-CrcTempRoot
    $candidateForCompare = $candidate.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $selectedRoot = Get-CrcPermittedRoot -Candidate $candidateForCompare -WorktreeRoot $worktreeRoot -TempRoot $tempRoot -Label $Label -Path $Path
    Assert-CrcNoSymlinkTraversal -Candidate $candidateForCompare -Root $selectedRoot -Label $Label
    Assert-CrcPathTarget -Candidate $candidateForCompare -Path $Path -Label $Label -MustExist ([bool]$MustExist) -Directory ([bool]$Directory)
    return [System.IO.Path]::GetFullPath($candidate)
}

function Invoke-CrcGit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $safeDirectory = $Repository.Replace('\', '/')
    $gitArguments = @('--no-replace-objects', '-c', "safe.directory=$safeDirectory", '-c', 'core.fsmonitor=false', '-C', $Repository) + $Arguments
    $errorPath = [System.IO.Path]::GetTempFileName()
    try {
        $output = & git @gitArguments 2> $errorPath
        if ($LASTEXITCODE -ne 0) {
            $detail = Get-Content -LiteralPath $errorPath -Raw -ErrorAction SilentlyContinue
            throw "Git command failed ($LASTEXITCODE): git $($Arguments -join ' ')`n$detail"
        }
    }
    finally {
        if (Test-Path -LiteralPath $errorPath) { Remove-Item -LiteralPath $errorPath -Force }
    }
    return ($output -join "`n")
}

function Invoke-CrcGitBytes {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $safeDirectory = $Repository.Replace('\', '/')
    $gitArguments = @('--no-replace-objects', '-c', "safe.directory=$safeDirectory", '-c', 'core.fsmonitor=false', '-C', $Repository) + $Arguments
    $outputPath = [System.IO.Path]::GetTempFileName()
    $errorPath = [System.IO.Path]::GetTempFileName()
    try {
        & git @gitArguments > $outputPath 2> $errorPath
        if ($LASTEXITCODE -ne 0) {
            $detail = Get-Content -LiteralPath $errorPath -Raw -ErrorAction SilentlyContinue
            throw "Git command failed ($LASTEXITCODE): git $($Arguments -join ' ')`n$detail"
        }
        return ,([System.IO.File]::ReadAllBytes($outputPath))
    }
    finally {
        if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Force }
        if (Test-Path -LiteralPath $errorPath) { Remove-Item -LiteralPath $errorPath -Force }
    }
}

function Get-CrcGitNulRecords {
    [CmdletBinding()]
    param([AllowNull()][object]$Bytes)

    $byteArray = [byte[]]::new(0)
    if ($null -ne $Bytes) { $byteArray = [byte[]]$Bytes }
    $records = [System.Collections.Generic.List[string]]::new()
    $start = 0
    for ($index = 0; $index -lt $byteArray.Length; $index++) {
        if ($byteArray[$index] -eq 0) {
            if ($index -gt $start) { $records.Add([System.Text.Encoding]::UTF8.GetString($byteArray,$start,$index - $start)) }
            $start = $index + 1
        }
    }
    if ($start -lt $byteArray.Length) { $records.Add([System.Text.Encoding]::UTF8.GetString($byteArray,$start,$byteArray.Length - $start)) }
    return @($records)
}

function Get-CrcGitLines {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Repository,[Parameter(Mandatory)][string[]]$Arguments)

    $text = Invoke-CrcGit -Repository $Repository -Arguments $Arguments
    return @($text -split "`r?`n" | Where-Object { $_ -and $_.Trim() })
}

function Add-CrcUtf8PathBytes { param([System.Collections.Generic.List[byte]]$Bytes,[string]$Text)
    foreach ($value in [System.Text.Encoding]::UTF8.GetBytes($Text)) { $Bytes.Add($value) }
}

function Get-CrcEscapedPathByte { param([string]$Token,[ref]$Index)
    $Index.Value++
    if ($Index.Value -ge ($Token.Length - 1)) { throw 'Git returned a truncated quoted diff path' }
    $escaped = $Token[$Index.Value]
    if ([string]$escaped -match '^[0-7]$') {
        if ($Index.Value + 2 -ge ($Token.Length - 1)) { throw 'Git returned a truncated octal diff path escape' }
        $octal = [string]$escaped + [string]$Token[$Index.Value + 1] + [string]$Token[$Index.Value + 2]
        $Index.Value += 2
        return [byte][Convert]::ToInt32($octal,8)
    }
    $escapedByte = switch ($escaped) {
        '"' { 34; break }
        '\' { 92; break }
        'a' { 7; break }
        'b' { 8; break }
        'f' { 12; break }
        'n' { 10; break }
        'r' { 13; break }
        't' { 9; break }
        'v' { 11; break }
        default { throw "Git returned an unsupported diff path escape: \$escaped" }
    }
    return [byte]$escapedByte
}

function ConvertFrom-CrcDiffPathToken {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Token)

    if (-not ($Token.StartsWith('"') -and $Token.EndsWith('"'))) { return Get-CrcPath -Path $Token }
    $bytes = [System.Collections.Generic.List[byte]]::new()
    for ($index = 1; $index -lt ($Token.Length - 1); $index++) {
        $character = $Token[$index]
        if ($character -eq '\') { $bytes.Add((Get-CrcEscapedPathByte -Token $Token -Index ([ref]$index))); continue }
        $text = [string]$character
        if ([char]::IsHighSurrogate($character) -and $index + 1 -lt ($Token.Length - 1) -and [char]::IsLowSurrogate($Token[$index + 1])) { $text += [string]$Token[++$index] }
        Add-CrcUtf8PathBytes -Bytes $bytes -Text $text
    }
    return Get-CrcPath -Path ([System.Text.Encoding]::UTF8.GetString($bytes.ToArray()))
}

function ConvertFrom-CrcDiffSidePath { param([string]$Token,[string]$Side)
    $path = ConvertFrom-CrcDiffPathToken -Token $Token
    if ($path.StartsWith("$Side/")) { $path = $path.Substring(2) }
    return Get-CrcPath -Path $path
}

function Get-CrcDiffHeaderPaths {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$DiffText)

    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $pattern = '^diff --git (?<old>"(?:\\.|[^"])*"|[^ ]+) (?<new>"(?:\\.|[^"])*"|[^ ]+)$'
    foreach ($line in ($DiffText -split "`r?`n")) {
        if ($line -match $pattern) {
            [void]$paths.Add((ConvertFrom-CrcDiffSidePath -Token $Matches.old -Side 'a'))
            [void]$paths.Add((ConvertFrom-CrcDiffSidePath -Token $Matches.new -Side 'b'))
        }
    }
    return ,$paths
}

function Get-CrcHunkCount {
    param([System.Text.RegularExpressions.Group]$Group)
    if ($Group.Success) { return [int]$Group.Value }
    return 1
}

function Test-CrcLineInRange {
    param([int]$Line,[int]$Start,[int]$Count)
    return $Line -ge $Start -and $Line -lt ($Start + $Count)
}

function Test-CrcHunkContainsLine {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Hunk,[Parameter(Mandatory)][int]$Line)

    $match = [regex]::Match($Hunk,'^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@')
    if (-not $match.Success) { return $false }
    $oldRange = Test-CrcLineInRange -Line $Line -Start ([int]$match.Groups[1].Value) -Count (Get-CrcHunkCount -Group $match.Groups[2])
    $newRange = Test-CrcLineInRange -Line $Line -Start ([int]$match.Groups[3].Value) -Count (Get-CrcHunkCount -Group $match.Groups[4])
    return $oldRange -or $newRange
}

function Test-CrcHunksContainLine {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Patch,[Parameter(Mandatory)][int]$Line)

    foreach ($patchLine in ($Patch -split "`r?`n")) {
        if (Test-CrcHunkContainsLine -Hunk $patchLine -Line $Line) { return $true }
    }
    return $false
}

function Test-CrcPatchContainsLine {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Patch,[Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][int]$Line)

    $normalizedPath = Get-CrcPath -Path $Path
    $currentPaths = @()
    foreach ($patchLine in ($Patch -split "`r?`n")) {
        if ($patchLine -match '^diff --git ("(?:\\.|[^"])*"|[^ ]+) ("(?:\\.|[^"])*"|[^ ]+)$') {
            $oldPath = ConvertFrom-CrcDiffSidePath -Token $Matches[1] -Side 'a'
            $newPath = ConvertFrom-CrcDiffSidePath -Token $Matches[2] -Side 'b'
            $currentPaths = @($oldPath,$newPath)
            continue
        }
        if ($patchLine -notmatch '^@@ ') { continue }
        if ($normalizedPath -notin $currentPaths) { continue }
        if (Test-CrcHunkContainsLine -Hunk $patchLine -Line $Line) { return $true }
    }
    return $false
}

function Resolve-CrcRepository {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $requested = Resolve-CrcSafePath -Path $Path -Label 'repository' -MustExist -Directory
    $topLevel = (Invoke-CrcGit -Repository $requested -Arguments @('rev-parse', '--show-toplevel')).Trim()
    return Resolve-CrcSafePath -Path $topLevel -Label 'repository root' -MustExist -Directory
}

function Assert-CrcRevisionName {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Revision)

    if ($Revision.StartsWith('-') -or $Revision -notmatch '^[A-Za-z0-9_./~^:@+,-]+$') {
        throw "Revision contains unsupported characters: $Revision"
    }
    return $Revision
}

function Resolve-CrcCommit {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Repository,[Parameter(Mandatory)][string]$Revision)

    $safeRevision = Assert-CrcRevisionName -Revision $Revision
    $resolved = (Invoke-CrcGit -Repository $Repository -Arguments @('rev-parse', '--verify', '--end-of-options', "$safeRevision^{commit}")).Trim()
    $objectIdLength = Get-CrcObjectIdLength -Repository $Repository
    if ($resolved -notmatch "^[0-9a-f]{$objectIdLength}$") {
        throw "Revision did not resolve to a full commit SHA: $Revision"
    }
    return $resolved
}

function Get-CrcObjectIdLength {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Repository)

    $format = (Invoke-CrcGit -Repository $Repository -Arguments @('rev-parse','--show-object-format=storage')).Trim()
    switch ($format) {
        'sha1' { return 40 }
        'sha256' { return 64 }
        default { throw "Git repository uses an unsupported object format: $format" }
    }
}

function Get-CrcPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $candidate = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') { $Path.Replace('\', '/') } else { $Path }
    if ([string]::IsNullOrWhiteSpace($candidate) -or $candidate.StartsWith('/') -or $candidate -match '^[A-Za-z]:/') {
        throw "Absolute or empty repository path is not allowed: $Path"
    }
    $parts = @($candidate -split '/' | Where-Object { $_ -and $_ -ne '.' })
    if ($parts -contains '..') {
        throw "Repository path escapes the root: $Path"
    }
    return ($parts -join '/')
}

function Get-CrcHashText {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Text)

    return Get-CrcSha256 -Bytes ([System.Text.Encoding]::UTF8.GetBytes($Text))
}

function Write-CrcJson {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][object]$Value)

    $safe = Resolve-CrcSafePath -Path $Path -Label 'output'
    $parent = Split-Path -Parent $safe
    if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    Set-Content -LiteralPath $safe -Value ((ConvertTo-CrcJson -Value $Value) + [Environment]::NewLine) -Encoding utf8
}

function Get-CrcUtcNow {
    return [DateTime]::UtcNow.ToString('o')
}
