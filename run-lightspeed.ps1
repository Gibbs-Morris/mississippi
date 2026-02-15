<#
.SYNOPSIS
    Runs the LightSpeed Aspire AppHost for local development and testing.

.DESCRIPTION
    Starts the LightSpeed sample application with debug logging enabled.
    Use Ctrl+C to stop the application.

.EXAMPLE
    ./run-lightspeed.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$solutionFile = Join-Path $PSScriptRoot 'samples.slnx'
$appHostProject = Join-Path $PSScriptRoot 'samples/LightSpeed/LightSpeed.AppHost/LightSpeed.AppHost.csproj'

if (-not (Test-Path $solutionFile)) {
    Write-Error "Solution file not found at: $solutionFile"
    exit 1
}

if (-not (Test-Path $appHostProject)) {
    Write-Error "AppHost project not found at: $appHostProject"
    exit 1
}

function Stop-LightSpeedProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProcessName
    )

    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($null -eq $processes) {
        return
    }

    foreach ($process in $processes) {
        Write-Host "Stopping stale process: $($process.ProcessName) ($($process.Id))" -ForegroundColor Yellow
        Stop-Process -Id $process.Id -Force
    }

    $timeout = [TimeSpan]::FromSeconds(10)
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    while ($stopwatch.Elapsed -lt $timeout) {
        $remainingProcesses = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
        if ($null -eq $remainingProcesses) {
            return
        }

        Start-Sleep -Milliseconds 200
    }

    Write-Error "Timed out waiting for process '$ProcessName' to exit."
    exit 1
}

Stop-LightSpeedProcess -ProcessName 'LightSpeed.AppHost'
Stop-LightSpeedProcess -ProcessName 'LightSpeed.Server'

Write-Host "Building Samples solution..." -ForegroundColor Cyan
# Build the full solution to ensure source generators and all dependencies are built in correct order
dotnet build $solutionFile -c Debug --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`nStarting LightSpeed Aspire AppHost (Debug mode)..." -ForegroundColor Green

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Logging__LogLevel__Default = 'Warning'
$env:Logging__LogLevel__Microsoft = 'Information'
$env:Logging__LogLevel__Microsoft__AspNetCore = 'Warning'
$env:Logging__LogLevel__Orleans = 'Warning'
$env:Logging__LogLevel__Mississippi = 'Debug'

dotnet run --project $appHostProject # -c Debug --no-build --launch-profile https

if ($LASTEXITCODE -ne 0) {
    Write-Error "LightSpeed AppHost exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

exit 0