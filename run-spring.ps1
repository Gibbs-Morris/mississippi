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
    [string]$LocalAuth = 'Off'
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

$staleProcessNames = @('Spring.AppHost', 'Spring.Silo', 'Spring.Server')
$staleProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -in $staleProcessNames }
if ($staleProcesses) {
    Write-Host "Stopping stale Spring processes..." -ForegroundColor Yellow
    $staleProcesses | Stop-Process -Force
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

$dashboardBrowserToken = [guid]::NewGuid().ToString('N')

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
$env:Dashboard__Frontend__BrowserToken = $dashboardBrowserToken

if (-not [string]::IsNullOrWhiteSpace($aspireDashboardUrl)) {
    $aspireDashboardBase = $aspireDashboardUrl.TrimEnd('/')
    Write-Output "Aspire dashboard URL: $aspireDashboardBase"
    Write-Output "Aspire dashboard token: $dashboardBrowserToken"
    Write-Output "Direct login URL: $aspireDashboardBase/login?t=$dashboardBrowserToken"
}

dotnet run --project $appHostProject --no-build --launch-profile https

if ($LASTEXITCODE -ne 0) {
    Write-Error "Spring AppHost exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

exit 0
