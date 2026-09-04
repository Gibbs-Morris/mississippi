#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module Pester -MinimumVersion 5.0.0 -ErrorAction Stop
$testPaths = @(
    (Join-Path $PSScriptRoot 'RepositoryAutomation.Tests.ps1'),
    (Join-Path $PSScriptRoot 'MutationAutomation.Tests.ps1'),
    (Join-Path $PSScriptRoot 'MutationSummary.Tests.ps1'),
    (Join-Path $PSScriptRoot 'PowerShellTestHarness.Tests.ps1')
)
foreach ($testPath in $testPaths) {
    if (-not (Test-Path -LiteralPath $testPath -PathType Leaf)) { throw "Test file not found: $testPath" }
}
$result = Invoke-Pester -Path $testPaths -PassThru

if ($PassThru) { return $result }
if ($null -eq $result -or $result.TotalCount -lt 1 -or $result.Result -ne 'Passed') { exit 1 }
exit 0
