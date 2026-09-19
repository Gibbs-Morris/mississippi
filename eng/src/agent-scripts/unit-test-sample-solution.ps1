#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [ValidateSet('L0Tests', 'L1Tests', 'L2Tests', 'L3Tests', 'L4Tests')]
    [string[]]$TestLevels = @('L0Tests', 'L1Tests')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot

$executionLease = $null
try {
    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $repoRoot -OperationId "unit-test-sample-$([guid]::NewGuid().ToString('N'))"
    Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $repoRoot -TestLevels $TestLevels
}
catch {
    Write-Error "=== SAMPLE SOLUTION UNIT TESTING FAILED ===: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $executionLease) { Exit-RepositoryExecutionLease -Lease $executionLease }
}

exit 0
