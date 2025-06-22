#!/usr/bin/env pwsh
# Description: Cross-platform cleanup script that restores tools and uses
# JetBrains ReSharper command line utilities to enforce formatting rules across
# the solution. Designed for automated environments on Windows, Linux and macOS.
[CmdletBinding()]
param (
    # Path to the solution file
    [string]$SolutionPath = (Join-Path $PSScriptRoot (Join-Path 'src' 'mississippi.sln')),
    
    # Path to the DotSettings file
    [string]$DotSettingsPath = (Join-Path $PSScriptRoot (Join-Path 'src' 'mississippi.sln.DotSettings')),
    
    # Profile to use for cleanup
    [string]$Profile = "Built-in: Full Cleanup"
)

# Set the error action preference to stop on error
$ErrorActionPreference = "Stop"

# Script variables
$RepoRoot = $PSScriptRoot  # Root directory of the repository (where this script is located)

# Function to format output with a clear header
function Write-Header {
    param (
        [string]$Message
    )
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
}

# Function to execute a command and throw on error
function Invoke-CommandLine {
    param (
        [string]$Command,
        [string]$Arguments,
        [int[]]$AllowedExitCodes = @(0)
    )

    Write-Host "> $Command $Arguments" -ForegroundColor Yellow
    
    # Create a new process with redirected output
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Command
    $psi.Arguments = $Arguments
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.WorkingDirectory = Get-Location

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    $process.Start() | Out-Null
    
    # Capture stdout and stderr
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    
    $process.WaitForExit()

    if ($stdout) { Write-Host $stdout }
    if ($stderr) { Write-Host $stderr -ForegroundColor Red }

    # Throw an exception if the command failed
    if ($AllowedExitCodes -notcontains $process.ExitCode) {
        throw "Command failed with exit code $($process.ExitCode)"
    }

    return $stdout
}

# Check if dotnet CLI is available
try {
    Invoke-CommandLine -Command "dotnet" -Arguments "--version"
}
catch {
    Write-Error "The .NET SDK is not installed or not found in the PATH."
    exit 1
}

# Step 1: Restore .NET tools
Write-Header "Restoring .NET Tools"
Invoke-CommandLine -Command "dotnet" -Arguments "tool restore"

# Check if JetBrains.ReSharper.GlobalTools is installed either locally or globally
Write-Header "Checking for ReSharper Command Line Tools"
$toolListOutput = Invoke-CommandLine -Command "dotnet" -Arguments "tool list"

if (-not ($toolListOutput -match "jetbrains.resharper.globaltools")) {
    Write-Host "JetBrains.ReSharper.GlobalTools not found in local tools. Checking globally..." -ForegroundColor Yellow
    
    $globalToolListOutput = Invoke-CommandLine -Command "dotnet" -Arguments "tool list -g"
    
    if (-not ($globalToolListOutput -match "jetbrains.resharper.globaltools")) {
        Write-Host "Installing JetBrains.ReSharper.GlobalTools globally..." -ForegroundColor Yellow
        Invoke-CommandLine -Command "dotnet" -Arguments "tool install -g JetBrains.ReSharper.GlobalTools"
    }
    else {
        Write-Host "JetBrains.ReSharper.GlobalTools found globally." -ForegroundColor Green
    }
}
else {
    Write-Host "JetBrains.ReSharper.GlobalTools found in local tools." -ForegroundColor Green
}

# Check if solution and settings files exist
if (-not (Test-Path $SolutionPath)) {
    Write-Error "Solution file not found at path: $SolutionPath"
    exit 1
}

if (-not (Test-Path $DotSettingsPath)) {
    Write-Warning "DotSettings file not found at path: $DotSettingsPath"
    $useDotSettings = $false
}
else {
    $useDotSettings = $true
}

# Step 2: Run ReSharper Cleanup
Write-Header "Running ReSharper Cleanup"
if ($useDotSettings) {
    Invoke-CommandLine -Command "dotnet" -Arguments "tool run jb cleanupcode --profile=`"$Profile`" --settings=`"$DotSettingsPath`" `"$SolutionPath`"" -AllowedExitCodes @(0,3)
}
else {
    Invoke-CommandLine -Command "dotnet" -Arguments "tool run jb cleanupcode --profile=`"$Profile`" `"$SolutionPath`"" -AllowedExitCodes @(0,3)
}

# Step 3: Check for any remaining issues
Write-Header "Checking for any remaining ReSharper issues"
$InspectionReport = Join-Path $RepoRoot 'resharper-report.xml'
Invoke-CommandLine -Command "dotnet" -Arguments "tool run jb inspectcode `"$SolutionPath`" --output=`"$InspectionReport`"" -AllowedExitCodes @(0,3)

Write-Host "ReSharper inspection report saved to $InspectionReport" -ForegroundColor Green
Write-Host "You can open this file to view any remaining issues that need to be addressed manually." -ForegroundColor Green

Write-Header "Cleanup Completed Successfully"
Write-Host "Your code has been cleaned up according to ReSharper rules."
Write-Host "You should now be able to pass the GitHub Actions cleanup check."
