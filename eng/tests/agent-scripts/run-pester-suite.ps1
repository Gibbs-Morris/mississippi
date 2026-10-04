#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$TestPath,
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module Pester -MinimumVersion 5.0.0 -ErrorAction Stop
foreach ($path in $TestPath) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Test file not found: $path"
    }
}

$result = Invoke-Pester -Path $TestPath -PassThru
if ($PassThru) { return $result }
if ($null -eq $result -or $result.TotalCount -lt 1 -or $result.Result -ne 'Passed') { exit 1 }
exit 0
