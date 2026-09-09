#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SolutionPath,
    [string]$Configuration = 'Release',
    [string[]]$TestLevels = @('L0Tests', 'L1Tests'),
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RepositoryAutomation.psm1') -Force
$arguments = @()
if ($NoBuild) { $arguments += '--no-build' }
Invoke-SolutionTests -SolutionPath $SolutionPath -Configuration $Configuration -TestLevels $TestLevels `
    -AdditionalArguments $arguments | Out-Host
