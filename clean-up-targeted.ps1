#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$BaseRef = 'main',

    [ValidateNotNullOrEmpty()]
    [string]$HeadRef = 'HEAD',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$SettingsPath,

    [string]$Profile = 'Built-in: Full Cleanup',

    [string]$CachesHome,

    [switch]$NoUpdates,

    [switch]$SkipSamples,

    [switch]$SkipMississippi,

    [switch]$SkipToolRestore,

    [switch]$SkipRestore,

    [switch]$SkipBuild,

    [switch]$PlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
$cleanupScriptPath = Join-Path $repoRoot 'clean-up.ps1'
$temporaryFilePath = $null
$exitCode = 0

try {
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        throw "Repository automation module not found: $modulePath"
    }

    if (-not (Test-Path -LiteralPath $cleanupScriptPath -PathType Leaf)) {
        throw "Canonical cleanup script not found: $cleanupScriptPath"
    }

    Import-Module -Name $modulePath -Force

    $changedPaths = @(
        Get-CleanupChangedPaths `
            -RepoRoot $repoRoot `
            -BaseRef $BaseRef `
            -HeadRef $HeadRef
    )

    Write-Verbose "Discovered $($changedPaths.Count) changed path(s) for cleanup."

    $temporaryFilePath = [System.IO.Path]::GetTempFileName()
    if ($changedPaths.Count -gt 0) {
        Set-Content -LiteralPath $temporaryFilePath -Value $changedPaths -Encoding utf8NoBOM
    }

    $pwshPath = (Get-Command pwsh -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    $cleanupArguments = @('-FileListPath', $temporaryFilePath)
    $cleanupArguments += ConvertTo-CleanupArgumentList -Parameters $PSBoundParameters

    & $pwshPath -NoProfile -File $cleanupScriptPath @cleanupArguments
    $exitCode = $LASTEXITCODE
}
catch {
    Write-Error "Targeted cleanup failed: $($_.Exception.Message)"
    $exitCode = 1
}
finally {
    if ($null -ne $temporaryFilePath -and (Test-Path -LiteralPath $temporaryFilePath -PathType Leaf)) {
        try {
            Remove-Item -LiteralPath $temporaryFilePath -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Unable to remove temporary cleanup file '$temporaryFilePath': $($_.Exception.Message)"
        }
    }
}

exit $exitCode
