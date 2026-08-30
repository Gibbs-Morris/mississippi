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

$result = Invoke-Pester -Path $testPath -PassThru
if ($PassThru) {
    return $result
}

if ([int]$result.FailedCount -gt 0) {
    exit 1
}

exit 0
