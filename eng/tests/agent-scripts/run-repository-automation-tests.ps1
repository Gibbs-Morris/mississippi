#!/usr/bin/env pwsh

[CmdletBinding()]
param([switch]$PassThru)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module Pester -MinimumVersion 5.0.0 -Force -ErrorAction Stop
$result = Invoke-Pester -Path (Join-Path $PSScriptRoot 'RepositoryAutomation.Tests.ps1') -PassThru
if ($PassThru) { return $result }
if ($result.Result -ne 'Passed') { exit 1 }
exit 0
