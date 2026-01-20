<#
.SYNOPSIS
    Runs the Spring Aspire AppHost for local development and testing.

.DESCRIPTION
    Starts the Spring sample application with debug logging enabled.
    Use Ctrl+C to stop the application.

.EXAMPLE
    ./run-spring.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appHostProject = Join-Path $PSScriptRoot 'samples/Spring/Spring.AppHost/Spring.AppHost.csproj'

if (-not (Test-Path $appHostProject)) {
    Write-Error "AppHost project not found at: $appHostProject"
    exit 1
}

Write-Host "Building Spring AppHost..." -ForegroundColor Cyan
dotnet build $appHostProject -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`nStarting Spring Aspire AppHost (Debug mode)..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Yellow

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Logging__LogLevel__Default = 'Warning'
$env:Logging__LogLevel__Microsoft = 'Information'
$env:Logging__LogLevel__Microsoft__AspNetCore = 'Warning'
$env:Logging__LogLevel__Orleans = 'Warning'
$env:Logging__LogLevel__Mississippi = 'Debug'

dotnet run --project $appHostProject # -c Debug --no-build --launch-profile https
