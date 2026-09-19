#!/usr/bin/env pwsh
#requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateSet('Core', 'Docs', 'Browser', 'CoreDocs', 'All')][string]$Profile = 'Core',
    [switch]$InstallPester,
    [switch]$PlanOnly,
    [switch]$NonInteractive,
    [string]$RepositoryRoot = $PSScriptRoot,
    [ValidateSet('Text', 'Json')][string]$OutputFormat = 'Text'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SetupProfiles {
    param([Parameter(Mandatory)][string]$Requested)
    switch ($Requested) {
        'CoreDocs' { return @('Core', 'Docs') }
        'All' { return @('Core', 'Docs', 'Browser') }
        default { return @($Requested) }
    }
}

function Add-SetupStep {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Steps,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][string]$Purpose
    )
    $Steps.Add([pscustomobject][ordered]@{
        Name = $Name
        Executable = $Executable
        Arguments = @($Arguments)
        WorkingDirectory = $WorkingDirectory
        Purpose = $Purpose
    })
}

function Get-SetupPlan {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$RequestedProfile, [bool]$ShouldInstallPester)

    $steps = [System.Collections.Generic.List[object]]::new()
    $profiles = @(Get-SetupProfiles -Requested $RequestedProfile)
    if ($profiles -contains 'Core') {
        Add-SetupStep -Steps $steps -Name 'restore-tools' -Executable 'dotnet' -Arguments @('tool', 'restore') -WorkingDirectory $Root -Purpose 'Restore repository-pinned local tools without changing the manifest.'
        Add-SetupStep -Steps $steps -Name 'restore-core-solution' -Executable 'dotnet' -Arguments @('restore', './mississippi.slnx', '--locked-mode') -WorkingDirectory $Root -Purpose 'Restore the canonical core solution in locked mode.'
        Add-SetupStep -Steps $steps -Name 'restore-sample-solution' -Executable 'dotnet' -Arguments @('restore', './samples.slnx', '--locked-mode') -WorkingDirectory $Root -Purpose 'Restore the canonical sample solution in locked mode.'
        if ($ShouldInstallPester) { Add-SetupStep -Steps $steps -Name 'install-pester' -Executable 'pwsh' -Arguments @('-NoProfile', '-Command', "Install-Module -Name Pester -Scope CurrentUser -Force -MinimumVersion 5.0.0 -Repository PSGallery") -WorkingDirectory $Root -Purpose 'Explicitly install the supported PowerShell test dependency.' }
    }
    if ($profiles -contains 'Docs') {
        Add-SetupStep -Steps $steps -Name 'restore-docs' -Executable 'npm' -Arguments @('ci', '--ignore-scripts') -WorkingDirectory (Join-Path $Root 'docs/Docusaurus') -Purpose 'Restore documentation dependencies from package-lock.json without lifecycle scripts.'
    }
    if ($profiles -contains 'Browser') {
        Add-SetupStep -Steps $steps -Name 'browser-doctor' -Executable 'pwsh' -Arguments @('./test-spring.ps1', '-Doctor') -WorkingDirectory $Root -Purpose 'Report SDK and Docker/browser prerequisites without starting the application.'
    }
    return [pscustomobject][ordered]@{ SchemaVersion = '1.0'; Profile = $RequestedProfile; Profiles = $profiles; InstallPester = $ShouldInstallPester; Steps = @($steps) }
}

function Invoke-SetupStep {
    param([Parameter(Mandatory)][object]$Step)
    Write-Host "SETUP: $($Step.Name)" -ForegroundColor Cyan
    Push-Location -LiteralPath $Step.WorkingDirectory
    try {
        & $Step.Executable @($Step.Arguments)
        if ($LASTEXITCODE -ne 0) { throw "Setup step '$($Step.Name)' failed with exit code $LASTEXITCODE." }
    }
    finally { Pop-Location }
}

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $plan = Get-SetupPlan -Root $root -RequestedProfile $Profile -ShouldInstallPester:$InstallPester
    if ($OutputFormat -eq 'Json' -or $PlanOnly) {
        if ($PlanOnly) { $plan | ConvertTo-Json -Depth 8 -Compress }
    }
    if ($PlanOnly) { exit 0 }

    Import-Module (Join-Path $root 'eng/src/agent-scripts/AgentDoctor.psm1') -Force
    foreach ($selectedProfile in @($plan.Profiles)) {
        $doctorProfile = if ($selectedProfile -eq 'Browser') { 'Spring' } else { $selectedProfile }
        $pre = Get-AgentDoctorReport -RepositoryRoot $root -Profile $doctorProfile
        Write-Output "PRECHECK: $selectedProfile $($pre.Status)"
        foreach ($step in @($plan.Steps | Where-Object { $_.Name -like '*restore*' -or $_.Name -eq 'install-pester' })) {
            if ($selectedProfile -eq 'Core' -and ($step.Name -eq 'restore-tools' -or $step.Name -eq 'restore-core-solution' -or $step.Name -eq 'restore-sample-solution' -or $step.Name -eq 'install-pester')) { Invoke-SetupStep -Step $step }
            elseif ($selectedProfile -eq 'Docs' -and $step.Name -eq 'restore-docs') { Invoke-SetupStep -Step $step }
        }
        if ($selectedProfile -eq 'Browser') {
            $browserStep = @($plan.Steps | Where-Object Name -EQ 'browser-doctor')[0]
            Invoke-SetupStep -Step $browserStep
        }
        $post = Get-AgentDoctorReport -RepositoryRoot $root -Profile $doctorProfile
        if (-not $post.IsPrerequisiteReady) {
            $failed = $post.RequiredFailures -join ', '
            throw "Setup profile '$selectedProfile' is not ready after setup. Required failures: $failed"
        }
        Write-Output "POSTCHECK: $selectedProfile READY"
    }
    if ($OutputFormat -eq 'Json') { $plan | Add-Member -NotePropertyName Status -NotePropertyValue 'READY' -PassThru | ConvertTo-Json -Depth 8 -Compress }
    else { Write-Output "SETUP: READY ($Profile)" }
    exit 0
}
catch {
    Write-Error "Setup failed: $($_.Exception.Message)"
    exit 1
}
