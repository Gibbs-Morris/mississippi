#!/usr/bin/env pwsh
#requires -Version 7.0

<##
.SYNOPSIS
Reports read-only prerequisite readiness for an agent task profile.
.DESCRIPTION
Checks declared repository prerequisites and returns READY only for prerequisite state; it does not install tools, start services, run tests, or mutate configuration.
#>

[CmdletBinding()]
param(
    [string]$RepositoryRoot = $PSScriptRoot,
    [ValidateSet('Core', 'Docs', 'Spring', 'GitHub', 'All')][string]$Profile = 'Core',
    [ValidateSet('Text', 'Json')][string]$OutputFormat = 'Text',
    [string[]]$TrustedGitHubHost = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'eng/src/agent-scripts/AgentDoctor.psm1') -Force
try {
    $report = Get-AgentDoctorReport -RepositoryRoot $RepositoryRoot -Profile $Profile -TrustedGitHubHost $TrustedGitHubHost
    if ($OutputFormat -eq 'Json') { $report | ConvertTo-Json -Depth 8 -Compress } else { Format-AgentDoctorText -Report $report }
    if ($report.IsPrerequisiteReady) { exit 0 }
    exit 1
}
catch {
    Write-Error "Doctor failed: $($_.Exception.Message)"
    exit 1
}
