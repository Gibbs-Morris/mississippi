#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'ChangedVsBase')]
param(
    [Parameter(ParameterSetName = 'ExplicitFiles', Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]]$Files,
    [Parameter(ParameterSetName = 'FileList', Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FileListPath,
    [Parameter(ParameterSetName = 'ChangedVsBase')]
    [ValidateNotNullOrEmpty()]
    [string]$BaseRef = 'main',
    [Parameter(ParameterSetName = 'ChangedVsBase')]
    [ValidateNotNullOrEmpty()]
    [string]$HeadRef = 'HEAD',
    [switch]$PlanOnly,
    [switch]$SkipSamples,
    [switch]$SkipMississippi,
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
$executionLease = $null
try {
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { throw "Repository automation module not found: $modulePath" }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    if ($SkipSamples -and $SkipMississippi) { throw 'Both -SkipSamples and -SkipMississippi were provided. At least one solution must be enabled.' }
    if (-not $PlanOnly) {
        $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId ('cleanup-targeted-' + [guid]::NewGuid().ToString('N')) -LeaseDirectory $LeaseDirectory
        $repoRoot = $executionLease.RepositoryRoot
    }
    $changedPaths = switch ($PSCmdlet.ParameterSetName) {
        'ExplicitFiles' { @($Files) }
        'FileList' { @(Read-CleanupPathList -Path $FileListPath) }
        default { @(Get-CleanupChangedPaths -RepoRoot $repoRoot -BaseRef $BaseRef -HeadRef $HeadRef) }
    }
    Write-Verbose "Discovered $($changedPaths.Count) changed path(s) for cleanup."
    if ($PlanOnly) {
        Get-CleanupPlan -Paths ([string[]]@($changedPaths)) -RepoRoot $repoRoot -SkipSamples:$SkipSamples -SkipMississippi:$SkipMississippi |
            ConvertTo-Json -Depth 10 -Compress | Write-Output
        exit 0
    }
    $null = Invoke-RepositoryCleanup -Mode Targeted -RepoRoot $repoRoot -Paths ([string[]]@($changedPaths)) -SkipSamples:$SkipSamples -SkipMississippi:$SkipMississippi
    exit 0
}
catch {
    Write-Error "Targeted cleanup failed: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}
