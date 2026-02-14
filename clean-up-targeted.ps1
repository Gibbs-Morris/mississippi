#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'ChangedVsBase')]
param(
    [Parameter(ParameterSetName = 'ExplicitFiles', Mandatory = $true)]
    [string[]]$Files,

    [Parameter(ParameterSetName = 'FileList', Mandatory = $true)]
    [string]$FileListPath,

    [Parameter(ParameterSetName = 'ChangedVsBase')]
    [string]$BaseRef = 'main',

    [switch]$SkipSamples,
    [switch]$SkipMississippi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$activeParameterSet = $PSCmdlet.ParameterSetName

if ($SkipSamples -and $SkipMississippi) {
    throw 'Both -SkipSamples and -SkipMississippi were provided. At least one solution must be enabled.'
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$cleanupExtensions = @(
    '.cs',
    '.csproj',
    '.props',
    '.targets',
    '.razor',
    '.cshtml',
    '.xaml',
    '.axaml',
    '.resx'
)

function Get-LastNativeExitCode {
    [CmdletBinding()]
    param()

    $exitCodeVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
    if ($null -eq $exitCodeVariable) {
        return 0
    }

    return [int]$exitCodeVariable.Value
}

function Get-ChangedFilesFromBase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$BaseRef
    )

    $mergeBase = (& git merge-base HEAD $BaseRef | Select-Object -First 1)
    if ((Get-LastNativeExitCode) -ne 0 -or [string]::IsNullOrWhiteSpace($mergeBase)) {
        throw "Unable to resolve merge-base between HEAD and '$BaseRef'."
    }

    $branchDiff = & git diff --name-only --diff-filter=ACMR "$($mergeBase.Trim())...HEAD"
    if ((Get-LastNativeExitCode) -ne 0) {
        throw "Failed to compute changed files against '$BaseRef'."
    }

    $workingTreeDiff = & git diff --name-only --diff-filter=ACMR
    if ((Get-LastNativeExitCode) -ne 0) {
        throw 'Failed to compute unstaged changed files.'
    }

    $stagedDiff = & git diff --name-only --diff-filter=ACMR --cached
    if ((Get-LastNativeExitCode) -ne 0) {
        throw 'Failed to compute staged changed files.'
    }

    return @($branchDiff) + @($workingTreeDiff) + @($stagedDiff)
}

function Get-InputFiles {
    [CmdletBinding()]
    param()

    switch ($activeParameterSet) {
        'ExplicitFiles' {
            return $Files
        }

        'FileList' {
            $resolvedFileListPath = Resolve-Path -LiteralPath $FileListPath
            return Get-Content -LiteralPath $resolvedFileListPath.Path |
                ForEach-Object { $_.Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith('#') }
        }

        default {
            return Get-ChangedFilesFromBase -BaseRef $BaseRef
        }
    }
}

function Normalize-RepositoryRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path
    )

    $normalized = $Path.Trim()
    $normalized = $normalized -replace '\\', '/'
    $normalized = $normalized.TrimStart('.')
    $normalized = $normalized.TrimStart('/')
    return $normalized
}

try {
    Push-Location -Path $repoRoot

    $inputFiles = Get-InputFiles
    $normalizedInputFiles = @($inputFiles |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { Normalize-RepositoryRelativePath -Path $_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique)

    if ($normalizedInputFiles.Count -eq 0) {
        Write-Host 'No input files were provided or discovered. Nothing to clean up.' -ForegroundColor Yellow
        exit 0
    }

    $cleanupCandidates = @($normalizedInputFiles |
            Where-Object {
                $relativePath = $_
                $fullPath = Join-Path $repoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
                    return $false
                }

                $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()
                return $cleanupExtensions -contains $extension
            } |
            Sort-Object -Unique)

    if ($cleanupCandidates.Count -eq 0) {
        Write-Host 'No cleanup-eligible files found from the provided input. Nothing to clean up.' -ForegroundColor Yellow
        exit 0
    }

    $mississippiIncludes = @($cleanupCandidates | Where-Object { $_ -notlike 'samples/*' })
    $sampleIncludes = @($cleanupCandidates | Where-Object { $_ -like 'samples/*' })

    if (-not $SkipMississippi) {
        if ($mississippiIncludes.Count -gt 0) {
            Write-Host "Running targeted Mississippi cleanup for $($mississippiIncludes.Count) file(s)..." -ForegroundColor Cyan
            Invoke-MississippiSolutionCleanup -RepoRoot $repoRoot -IncludePaths $mississippiIncludes
        }
        else {
            Write-Host 'No Mississippi solution files matched for cleanup.' -ForegroundColor Yellow
        }
    }

    if (-not $SkipSamples) {
        if ($sampleIncludes.Count -gt 0) {
            Write-Host "Running targeted Samples cleanup for $($sampleIncludes.Count) file(s)..." -ForegroundColor Cyan
            Invoke-SampleSolutionCleanup -RepoRoot $repoRoot -IncludePaths $sampleIncludes
        }
        else {
            Write-Host 'No Samples solution files matched for cleanup.' -ForegroundColor Yellow
        }
    }

    Write-Host 'Targeted cleanup completed successfully.' -ForegroundColor Green
    exit 0
}
catch {
    Write-Error "Targeted cleanup failed: $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
}
