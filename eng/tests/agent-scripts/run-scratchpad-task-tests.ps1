#!/usr/bin/env pwsh

[CmdletBinding()]
param([switch]$PassThru)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'run-pester-suite.ps1') -TestPath (Join-Path $PSScriptRoot 'scratchpad-task-scripts.Tests.ps1') -PassThru:$PassThru
if (-not $PassThru) { exit $LASTEXITCODE }
