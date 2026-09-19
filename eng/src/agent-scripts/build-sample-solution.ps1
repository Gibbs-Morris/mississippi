#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

$executionLease = $null
try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "build-sample-$([guid]::NewGuid().ToString('N'))"
    Invoke-SampleSolutionBuild -Configuration $Configuration -RepoRoot $repoRoot
}
catch {
    Write-Error "=== SAMPLE SOLUTION BUILD FAILED ===: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}

exit 0
