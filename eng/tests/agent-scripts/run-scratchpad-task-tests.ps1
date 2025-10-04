#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    Import-Module Pester -ErrorAction Stop | Out-Null
}
catch {
    throw 'Pester module is required to run these tests.'
}

$pesterModule = Get-Module Pester
$pesterMajor = if ($pesterModule) { $pesterModule.Version.Major } else { 0 }

$testPath = Join-Path $PSScriptRoot 'scratchpad-task-scripts.Tests.ps1'
if (-not (Test-Path -LiteralPath $testPath -PathType Leaf)) {
    throw "Test file not found: $testPath"
}

Write-Host "Running scratchpad task Pester tests (Pester v$pesterMajor)..." -ForegroundColor Cyan

$result = $null

if ($pesterMajor -ge 5) {
    if ($PassThru) {
        $result = Invoke-Pester -Path $testPath -PassThru
    }
    else {
        Invoke-Pester -Path $testPath | Out-Null
    }
}
else {
    $scriptDefinition = @{ Path = $testPath }
    if ($PassThru) {
        $result = Invoke-Pester -Script $scriptDefinition -PassThru
    }
    else {
        Invoke-Pester -Script $scriptDefinition | Out-Null
    }
}

if ($PassThru) {
    return $result
}



