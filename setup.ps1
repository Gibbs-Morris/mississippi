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
$script:SetupJsonOutput = $OutputFormat -eq 'Json'

function Write-SetupProgress {
    param([Parameter(Mandatory)][string]$Message)
    if (-not $script:SetupJsonOutput) { Write-Host $Message -ForegroundColor Cyan }
}

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
    }
    if ($profiles -contains 'Docs') {
        Add-SetupStep -Steps $steps -Name 'restore-docs' -Executable 'npm' -Arguments @('ci', '--ignore-scripts') -WorkingDirectory (Join-Path $Root 'docs/Docusaurus') -Purpose 'Restore documentation dependencies from package-lock.json without lifecycle scripts.'
        Add-SetupStep -Steps $steps -Name 'install-markdownlint' -Executable 'npm' -Arguments @('install', '--global', '--prefix', (Join-Path $Root '.tools/npm-global'), 'markdownlint-cli@0.45.0', '--yes') -WorkingDirectory $Root -Purpose 'Provide the repository-pinned Markdown linter in a repository-local writable prefix.'
    }
    if ($profiles -contains 'Browser') {
        Add-SetupStep -Steps $steps -Name 'browser-doctor' -Executable 'pwsh' -Arguments @('./test-spring.ps1', '-Doctor') -WorkingDirectory $Root -Purpose 'Report SDK and Docker/browser prerequisites without starting the application.'
    }
    if ($ShouldInstallPester) { $steps.Insert(0, [pscustomobject][ordered]@{ Name = 'install-pester'; Executable = 'pwsh'; Arguments = @('-NoProfile', '-Command', "Install-Module -Name Pester -Scope CurrentUser -Force -Confirm:`$false -RequiredVersion 5.7.1 -Repository PSGallery"); WorkingDirectory = $Root; Purpose = 'Install the repository-pinned PowerShell test dependency.' }) }
    return [pscustomobject][ordered]@{ SchemaVersion = '1.0'; Profile = $RequestedProfile; Profiles = $profiles; InstallPester = $ShouldInstallPester; Steps = @($steps) }
}

function Invoke-SetupStep {
    param([Parameter(Mandatory)][object]$Step, [switch]$NonInteractive)
    Write-SetupProgress -Message "SETUP: $($Step.Name)"
    $previousCi = $env:CI
    $previousNpmYes = $env:NPM_CONFIG_YES
    $previousDotnetTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    if ($NonInteractive) {
        $env:CI = 'true'
        $env:NPM_CONFIG_YES = 'true'
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
    }
    Push-Location -LiteralPath $Step.WorkingDirectory
    try {
        if ($script:SetupJsonOutput) { & $Step.Executable @($Step.Arguments) *> $null } else { & $Step.Executable @($Step.Arguments) }
        if ($LASTEXITCODE -ne 0) { throw "Setup step '$($Step.Name)' failed with exit code $LASTEXITCODE." }
        if ($Step.Name -eq 'install-markdownlint') {
            $prefixIndex = [Array]::IndexOf([string[]]$Step.Arguments, '--prefix')
            if ($prefixIndex -lt 0 -or $prefixIndex + 1 -ge $Step.Arguments.Count) { throw 'Markdownlint setup step is missing its npm prefix.' }
            $prefix = [System.IO.Path]::GetFullPath([string]$Step.Arguments[$prefixIndex + 1])
            $binDirectory = if ($IsWindows) { $prefix } else { Join-Path $prefix 'bin' }
            $env:PATH = $binDirectory + [IO.Path]::PathSeparator + $env:PATH
            if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_PATH)) { Add-Content -LiteralPath $env:GITHUB_PATH -Value $binDirectory }
            $activationPath = Join-Path $prefix 'activate-markdownlint.ps1'
            $activationRoot = $binDirectory.Replace("'", "''")
            New-Item -ItemType Directory -Path $prefix -Force | Out-Null
            Set-Content -LiteralPath $activationPath -Value ("`$env:PATH = '$activationRoot' + [IO.Path]::PathSeparator + `$env:PATH") -Encoding utf8
        }
    }
    finally {
        Pop-Location
        $env:CI = $previousCi
        $env:NPM_CONFIG_YES = $previousNpmYes
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousDotnetTelemetry
    }
}

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $plan = Get-SetupPlan -Root $root -RequestedProfile $Profile -ShouldInstallPester:$InstallPester
    if ($OutputFormat -eq 'Json' -or $PlanOnly) {
        if ($PlanOnly) { $plan | ConvertTo-Json -Depth 8 -Compress }
    }
    if ($PlanOnly) { exit 0 }

    $env:DOTNET_CLI_HOME = Join-Path $root '.tools/dotnet-home'
    New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME -Force | Out-Null
    Import-Module (Join-Path $root 'eng/src/agent-scripts/AgentDoctor.psm1') -Force
    $executedSteps = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($selectedProfile in @($plan.Profiles)) {
        $doctorProfile = if ($selectedProfile -eq 'Browser') { 'Spring' } else { $selectedProfile }
        $pre = Get-AgentDoctorReport -RepositoryRoot $root -Profile $doctorProfile
        Write-SetupProgress -Message "PRECHECK: $selectedProfile $($pre.Status)"
        foreach ($step in @($plan.Steps)) {
            $applies = switch ($step.Name) {
                'install-pester' { $true; break }
                'restore-tools' { $selectedProfile -eq 'Core'; break }
                'restore-core-solution' { $selectedProfile -eq 'Core'; break }
                'restore-sample-solution' { $selectedProfile -eq 'Core'; break }
                'restore-docs' { $selectedProfile -eq 'Docs'; break }
                'install-markdownlint' { $selectedProfile -eq 'Docs'; break }
                'browser-doctor' { $selectedProfile -eq 'Browser'; break }
                default { $false }
            }
            if ($applies -and $executedSteps.Add($step.Name)) {
                Invoke-SetupStep -Step $step -NonInteractive:$NonInteractive
            }
        }
        $post = Get-AgentDoctorReport -RepositoryRoot $root -Profile $doctorProfile
        if (-not $post.IsPrerequisiteReady) {
            $failed = $post.RequiredFailures -join ', '
            throw "Setup profile '$selectedProfile' is not ready after setup. Required failures: $failed"
        }
        Write-SetupProgress -Message "POSTCHECK: $selectedProfile READY"
    }
    if ($OutputFormat -eq 'Json') { $plan | Add-Member -NotePropertyName Status -NotePropertyValue 'READY' -PassThru | ConvertTo-Json -Depth 8 -Compress }
    else { Write-Output "SETUP: READY ($Profile)" }
    exit 0
}
catch {
    Write-Error "Setup failed: $($_.Exception.Message)"
    exit 1
}
