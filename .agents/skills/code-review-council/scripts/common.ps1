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

function Resolve-CrcSafePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label,
        [switch]$MustExist,
        [switch]$Directory
    )

    $candidate = [System.IO.Path]::GetFullPath($Path)
    $segments = $Path -split '[\\/]'
    if ($segments -contains '..') {
        throw "$Label path traversal is not allowed: $Path"
    }
    $worktreeRoot = (Get-Location).Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $tempRoot = Get-CrcTempRoot
    $candidateForCompare = $candidate.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $inWorktree = Test-CrcPathWithin -Path $candidateForCompare -Root $worktreeRoot
    $inTemp = Test-CrcPathWithin -Path $candidateForCompare -Root $tempRoot
    if (-not ($inWorktree -or $inTemp)) {
        throw "$Label path must be within the worktree or temporary directory: $Path"
    }

    $selectedRoot = if ($inWorktree) { $worktreeRoot } else { $tempRoot }
    $rootItem = Get-Item -LiteralPath $selectedRoot -Force -ErrorAction SilentlyContinue
    if ($null -ne $rootItem -and ($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label root cannot be a symbolic link: $selectedRoot"
    }
    $relative = $candidate.Substring($selectedRoot.Length).TrimStart([char[]]@('\','/'))
    $current = $selectedRoot
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { $_ })) {
        $current = Join-Path $current $segment
        $item = Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
        if ($null -eq $item) { break }
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label path cannot traverse a symbolic link: $current"
        }
    }
    $candidateItem = Get-Item -LiteralPath $candidate -Force -ErrorAction SilentlyContinue
    if ($MustExist -and $null -eq $candidateItem) {
        throw "$Label path does not exist: $Path"
    }
    if ($MustExist -and $Directory -and ($null -eq $candidateItem -or -not $candidateItem.PSIsContainer)) {
        throw "$Label path is not a directory: $Path"
    }
    if ($MustExist -and -not $Directory -and ($null -eq $candidateItem -or $candidateItem.PSIsContainer)) {
        throw "$Label path is not a file: $Path"
    }
    return [System.IO.Path]::GetFullPath($candidate)
}

function Invoke-CrcGit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $safeDirectory = $Repository.Replace('\', '/')
    $gitArguments = @('-c', "safe.directory=$safeDirectory", '-C', $Repository) + $Arguments
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
    $gitArguments = @('-c', "safe.directory=$safeDirectory", '-C', $Repository) + $Arguments
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

function Resolve-CrcRepository {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $requested = Resolve-CrcSafePath -Path $Path -Label 'repository' -MustExist -Directory
    $topLevel = (Invoke-CrcGit -Repository $requested -Arguments @('rev-parse', '--show-toplevel')).Trim()
    return [System.IO.Path]::GetFullPath($topLevel)
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
    if ($resolved -notmatch '^[0-9a-f]{40}$') {
        throw "Revision did not resolve to a full commit SHA: $Revision"
    }
    return $resolved
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
