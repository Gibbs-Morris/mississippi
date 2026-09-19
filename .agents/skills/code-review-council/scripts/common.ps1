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

    return ($Value | ConvertTo-Json -Depth 100 -Compress)
}

function Get-CrcSha256 {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$Bytes)

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([Convert]::ToHexString($algorithm.ComputeHash($Bytes))).ToLowerInvariant()
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
    $comparison = [System.StringComparison]::OrdinalIgnoreCase
    $inWorktree = $candidateForCompare.Equals($worktreeRoot, $comparison) -or $candidateForCompare.StartsWith("$worktreeRoot$([System.IO.Path]::DirectorySeparatorChar)", $comparison)
    $inTemp = $candidateForCompare.Equals($tempRoot, $comparison) -or $candidateForCompare.StartsWith("$tempRoot$([System.IO.Path]::DirectorySeparatorChar)", $comparison)
    if (-not ($inWorktree -or $inTemp)) {
        throw "$Label path must be within the worktree or temporary directory: $Path"
    }
    if ($MustExist -and -not (Test-Path -LiteralPath $candidate -PathType Any)) {
        throw "$Label path does not exist: $Path"
    }
    if ($MustExist -and $Directory -and -not (Test-Path -LiteralPath $candidate -PathType Container)) {
        throw "$Label path is not a directory: $Path"
    }
    if ($MustExist -and -not $Directory -and -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
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
    Set-Content -LiteralPath $safe -Value (($Value | ConvertTo-Json -Depth 100) + [Environment]::NewLine) -Encoding utf8
}

function Get-CrcUtcNow {
    return [DateTime]::UtcNow.ToString('o')
}
