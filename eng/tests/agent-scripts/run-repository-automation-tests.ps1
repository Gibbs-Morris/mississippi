#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module Pester -MinimumVersion 5.0.0 -ErrorAction Stop
$testPath = Join-Path $PSScriptRoot 'RepositoryAutomation.Tests.ps1'
if (-not (Test-Path -LiteralPath $testPath -PathType Leaf)) {
    throw "Test file not found: $testPath"
}

$result = Invoke-Pester -Path $testPath -PassThru
if ($PassThru) {
    return $result
}

if ($null -eq $result -or $result.TotalCount -lt 1 -or $result.Result -ne 'Passed') {
    exit 1
}
exit 0
