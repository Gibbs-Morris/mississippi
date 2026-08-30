#!/usr/bin/env pwsh

[CmdletBinding()]
param(
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

    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
$coreScriptPath = Join-Path $PSScriptRoot 'clean-up-core.ps1'
$exitCode = 0

try {
    Import-Module -Name $modulePath -Force -ErrorAction Stop

    if (-not (Test-Path -LiteralPath $coreScriptPath -PathType Leaf)) {
        throw "Cleanup core script not found: $coreScriptPath"
    }

    $pwshPath = (Get-Command pwsh -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    $coreArguments = ConvertTo-CleanupArgumentList -Parameters $PSBoundParameters

    & $pwshPath -NoProfile -File $coreScriptPath @coreArguments

    $exitCode = $LASTEXITCODE
}
catch {
    Write-Error "Full cleanup failed: $($_.Exception.Message)"
    $exitCode = 1
}

exit $exitCode
