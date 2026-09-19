#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Get-Location).Path,
    [string[]]$ChangedPath = @(),
    [string[]]$IntendedPath = @(),
    [string[]]$ReviewedPath = @(),
    [string[]]$RequiredPath = @(),
    [string[]]$ContentDomain = @(),
    [string[]]$WorkflowRole = @(),
    [ValidateSet('Text', 'Json')]
    [string]$OutputFormat = 'Text'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'AgentContext.psm1'
Import-Module -Name $modulePath -Force

try {
    $contextParameters = @{ RepositoryRoot = $RepositoryRoot }
    foreach ($name in @('RepositoryRoot', 'ChangedPath', 'IntendedPath', 'ReviewedPath', 'RequiredPath', 'ContentDomain', 'WorkflowRole')) {
        if ($name -ne 'RepositoryRoot' -and $PSBoundParameters.ContainsKey($name)) { $contextParameters[$name] = $PSBoundParameters[$name] }
    }
    $context = Get-AgentContext @contextParameters
    if ($OutputFormat -eq 'Json') {
        $context | ConvertTo-Json -Depth 10 -Compress
    }
    else {
        Format-AgentContextText -Context $context
    }

    if ($context.Complete) { exit 0 }
    exit 1
}
catch {
    Write-Error "Agent context preparation failed: $($_.Exception.Message)"
    exit 1
}
