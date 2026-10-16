#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    Import-Module Pester -MinimumVersion 5.0.0 -ErrorAction Stop | Out-Null
}
catch {
    throw 'Pester v5+ is required to run cleanup tests.'
}

$testPath = Join-Path $PSScriptRoot 'Cleanup.Tests.ps1'
if (-not (Test-Path -LiteralPath $testPath -PathType Leaf)) {
    throw "Test file not found: $testPath"
}

$testPaths = @($testPath)
$monthlyTestPath = Join-Path $PSScriptRoot 'MonthlyCleanup.Tests.ps1'
if (Test-Path -LiteralPath $monthlyTestPath -PathType Leaf) {
    $testPaths += $monthlyTestPath
}

$result = Invoke-Pester -Path $testPaths -PassThru
if ($PassThru) {
    return $result
}

if ([int]$result.FailedCount -gt 0) {
    exit 1
}

exit 0
