#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Spring Aspire AppHost for local development and testing.

.DESCRIPTION
    Starts the Spring sample application with debug logging enabled.
    Use Ctrl+C to stop the application.

.EXAMPLE
    ./run-spring.ps1

.EXAMPLE
    ./run-spring.ps1 -LocalAuth On

.EXAMPLE
    ./run-spring.ps1 -LocalAuth Off

#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('On', 'Off')]
    [string]$LocalAuth = 'Off',

    [Parameter()]
    [switch]$ForceCleanup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$solutionFile = Join-Path $PSScriptRoot 'samples.slnx'
$appHostProject = Join-Path $PSScriptRoot 'samples/Spring/Spring.AppHost/Spring.AppHost.csproj'

if (-not (Test-Path $solutionFile)) {
    Write-Error "Solution file not found at: $solutionFile"
    exit 1
}

if (-not (Test-Path $appHostProject)) {
    Write-Error "AppHost project not found at: $appHostProject"
    exit 1
}

$launchSettingsPath = Join-Path $PSScriptRoot 'samples/Spring/Spring.AppHost/Properties/launchSettings.json'
if (-not (Test-Path $launchSettingsPath)) {
    Write-Error "Launch settings file not found at: $launchSettingsPath"
    exit 1
}

$staleProcessNames = @('Spring.AppHost', 'Spring.Runtime', 'Spring.Gateway')
$staleProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -in $staleProcessNames }
if ($staleProcesses) {
    if ($ForceCleanup.IsPresent) {
        Write-Host "Stopping stale Spring processes..." -ForegroundColor Yellow
        $staleProcesses | Stop-Process -Force
    } else {
        Write-Warning "Detected existing Spring processes. Re-run with -ForceCleanup to stop them automatically."
    }
}

Write-Host "Building Samples solution..." -ForegroundColor Cyan
# Build the full solution to ensure source generators and all dependencies are built in correct order
dotnet build $solutionFile -c Debug --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`nStarting Spring Aspire AppHost (Debug mode)..." -ForegroundColor Green
Write-Host "Local auth mode: $LocalAuth" -ForegroundColor Green

$launchSettings = Get-Content -Path $launchSettingsPath -Raw | ConvertFrom-Json
$appHostProfile = $launchSettings.profiles.https
if ($null -eq $appHostProfile) {
    Write-Error "'https' launch profile not found in $launchSettingsPath"
    exit 1
}

$applicationUrls = @($appHostProfile.applicationUrl -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$aspireDashboardUrl = ($applicationUrls | Where-Object { $_ -like 'https://*' } | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($aspireDashboardUrl)) {
    $aspireDashboardUrl = $applicationUrls | Select-Object -First 1
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Logging__LogLevel__Default = 'Warning'
$env:Logging__LogLevel__Aspire = 'Information'
$env:Logging__LogLevel__Aspire__Hosting = 'Information'
$env:Logging__LogLevel__Aspire__Hosting__Dashboard = 'Information'
$env:Logging__LogLevel__Microsoft = 'Information'
$env:Logging__LogLevel__Microsoft__AspNetCore = 'Warning'
$env:Logging__LogLevel__Orleans = 'Warning'
$env:Logging__LogLevel__Mississippi = 'Debug'
$env:Spring__AuthProofMode = if ($LocalAuth -eq 'On') { 'true' } else { 'false' }

if (-not [string]::IsNullOrWhiteSpace($aspireDashboardUrl)) {
    $aspireDashboardBase = $aspireDashboardUrl.TrimEnd('/')
    Write-Output "Aspire dashboard URL: $aspireDashboardBase"
}

$parsedDashboardLoginUrl = $null

& dotnet run --project $appHostProject --no-build --launch-profile https 2>&1 |
    ForEach-Object {
        $line = $_.ToString()
        Write-Output $line

        if ([string]::IsNullOrWhiteSpace($parsedDashboardLoginUrl) -and
            ($line -match 'Login to the dashboard at\s+(https?://\S+)')) {
            $parsedDashboardLoginUrl = $matches[1].TrimEnd('.')
        }
    }

$dotnetExitCode = $LASTEXITCODE

if (-not [string]::IsNullOrWhiteSpace($parsedDashboardLoginUrl)) {
    Write-Output "DEV ONLY: dashboard login token output is enabled for local engineering workflows."

    try {
        $parsedDashboardUri = [System.Uri]$parsedDashboardLoginUrl
        $redactedLoginUrl = "{0}{1}" -f $parsedDashboardUri.GetLeftPart([System.UriPartial]::Path), '?t=<redacted>'
        Write-Output "Aspire dashboard login URL (token redacted): $redactedLoginUrl"
    } catch {
        Write-Output "Aspire dashboard login URL detected (token redacted)."
    }

    if ($parsedDashboardLoginUrl -match '[?&]t=([^&\s]+)') {
        Write-Output "Aspire dashboard token: $($matches[1])"
    }
} elseif (-not [string]::IsNullOrWhiteSpace($aspireDashboardUrl)) {
    Write-Output "Aspire dashboard login URL was not detected from runtime output. Open $aspireDashboardUrl and use the login token printed by Aspire."
}

if ($dotnetExitCode -ne 0) {
    Write-Error "Spring AppHost exited with code $dotnetExitCode"
    exit $dotnetExitCode
}

exit 0
