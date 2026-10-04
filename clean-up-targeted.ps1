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
    [switch]$SkipMississippi,
    [string]$LeaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force -ErrorAction Stop
$executionLease = $null

try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId ('cleanup-targeted-' + [guid]::NewGuid().ToString('N')) -LeaseDirectory $LeaseDirectory
    $repoRoot = $executionLease.RepositoryRoot
    $paths = switch ($PSCmdlet.ParameterSetName) {
        'ExplicitFiles' { @($Files) }
        'FileList' { @(Read-CleanupPathList -Path $FileListPath) }
        default { @(Get-CleanupChangedPaths -RepoRoot $repoRoot -BaseRef $BaseRef -HeadRef HEAD) }
    }
    $null = Invoke-RepositoryCleanup -Mode Targeted -RepoRoot $repoRoot -Paths ([string[]]@($paths)) -SkipSamples:$SkipSamples -SkipMississippi:$SkipMississippi
    exit 0
}
catch {
    Write-Error "Targeted cleanup failed: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}
