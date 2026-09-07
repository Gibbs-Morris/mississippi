#!/usr/bin/env pwsh
[CmdletBinding()]
param([switch]$PassThru)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module Pester -MinimumVersion 5.0.0 -Force
$result = Invoke-Pester -Path (Join-Path $PSScriptRoot 'SpringValidation.Tests.ps1') -PassThru
if ($PassThru) { return $result }
if ($result.FailedCount -gt 0 -or $result.PassedCount -eq 0) { exit 1 }
exit 0
