#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'Full')]
param(
    [Parameter(ParameterSetName = 'ExplicitFiles', Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]]$Files,

    [Parameter(ParameterSetName = 'FileList', Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FileListPath,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [string]$SettingsPath,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [string]$Profile = 'Built-in: Full Cleanup',

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [string]$CachesHome,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$NoUpdates,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$SkipSamples,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$SkipMississippi,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$SkipToolRestore,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$SkipRestore,

    [Parameter(ParameterSetName = 'Full')]
    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$SkipBuild,

    [Parameter(ParameterSetName = 'ExplicitFiles')]
    [Parameter(ParameterSetName = 'FileList')]
    [switch]$PlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force -ErrorAction Stop

function Get-SelectedCleanupPaths {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Selection,
        [string[]]$ExplicitFiles,
        [string]$SelectionFilePath
    )

    if ($Selection -eq 'ExplicitFiles') {
        return @($ExplicitFiles)
    }

    return @(Read-CleanupPathList -Path $SelectionFilePath)
}

try {
    if ($SkipSamples -and $SkipMississippi) {
        throw 'Both -SkipSamples and -SkipMississippi were provided. At least one solution must be enabled.'
    }

    $isTargeted = $PSCmdlet.ParameterSetName -ne 'Full'
    if (-not $isTargeted -and $PlanOnly) {
        throw '-PlanOnly requires -Files or -FileListPath.'
    }

    if (-not $isTargeted) {
        $null = Invoke-RepositoryCleanup -Mode Full `
            -RepoRoot $repoRoot `
            -Configuration $Configuration `
            -SettingsPath $SettingsPath `
            -Profile $Profile `
            -CachesHome $CachesHome `
            -NoUpdates:$NoUpdates `
            -SkipSamples:$SkipSamples `
            -SkipMississippi:$SkipMississippi `
            -SkipToolRestore:$SkipToolRestore `
            -SkipRestore:$SkipRestore `
            -SkipBuild:$SkipBuild
        exit 0
    }

    $selectedPaths = Get-SelectedCleanupPaths `
        -Selection $PSCmdlet.ParameterSetName `
        -ExplicitFiles $Files `
        -SelectionFilePath $FileListPath

    if ($PlanOnly) {
        $plan = Get-CleanupPlan `
            -Paths ([string[]]@($selectedPaths)) `
            -RepoRoot $repoRoot `
            -SkipSamples:$SkipSamples `
            -SkipMississippi:$SkipMississippi
        $plan | ConvertTo-Json -Depth 10 -Compress | Write-Output
        exit 0
    }

    $null = Invoke-RepositoryCleanup -Mode Targeted `
        -RepoRoot $repoRoot `
        -Paths $selectedPaths `
        -Configuration $Configuration `
        -SettingsPath $SettingsPath `
        -Profile $Profile `
        -CachesHome $CachesHome `
        -NoUpdates:$NoUpdates `
        -SkipSamples:$SkipSamples `
        -SkipMississippi:$SkipMississippi `
        -SkipToolRestore:$SkipToolRestore `
        -SkipRestore:$SkipRestore `
        -SkipBuild:$SkipBuild
    exit 0
}
catch {
    Write-Error "Cleanup failed: $($_.Exception.Message)"
    exit 1
}
