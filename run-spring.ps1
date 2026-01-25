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

Write-Host "Building Samples solution..." -ForegroundColor Cyan
# Build the full solution to ensure source generators and all dependencies are built in correct order
dotnet build $solutionFile -c Debug --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`nStarting Spring Aspire AppHost (Debug mode)..." -ForegroundColor Green

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Logging__LogLevel__Default = 'Warning'
$env:Logging__LogLevel__Microsoft = 'Information'
$env:Logging__LogLevel__Microsoft__AspNetCore = 'Warning'
$env:Logging__LogLevel__Orleans = 'Warning'
$env:Logging__LogLevel__Mississippi = 'Debug'

dotnet run --project $appHostProject # -c Debug --no-build --launch-profile https
