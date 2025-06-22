<#
.SYNOPSIS
    Development build script for the Mississippi framework solution.

.DESCRIPTION
    This script performs a clean build of the Mississippi framework solution without running tests.
    It's designed for local development scenarios where you only need to compile the code, such as:
    - Quick build verification during development
    - Syntax checking after making changes
    - Verifying build configuration changes
    - Fast validation before committing code
    - Development environments where you want to skip testing

.PARAMETER Configuration
    The build configuration to use (Debug/Release). Defaults to "Release".

.EXAMPLE
    .\dev-build.ps1
    # Builds the solution in Release configuration

.EXAMPLE
    .\dev-build.ps1 -Configuration Debug
    # Builds the solution in Debug configuration

.NOTES
    This script is part of a set of four specialized development scripts:
    - dev-build.ps1: Development build only (this script)
    - dev-test.ps1: Development build + unit tests + coverage
    - dev-quality.ps1: Development quality assurance (full pipeline)
    - dev-cleanup.ps1: Development code cleanup

    For Cursor and Copilot: Use this script when you need to quickly verify that the solution
    compiles successfully without running the full test suite. This is useful for:
    - Syntax checking during active development
    - Verifying build configuration changes
    - Quick validation before committing code
    - Fast feedback loops when iterating on code

    Note: For CI/CD, this project uses GitHub Actions workflows.

.LINK
    Related scripts: dev-test.ps1, dev-quality.ps1, dev-cleanup.ps1
#>

#!/usr/bin/env pwsh
[CmdletBinding()]
param (
    # Build configuration (Debug/Release) - Release is optimized for performance
    [string]$Configuration = "Release"
)

# Set the error action preference to stop on error
# This ensures the script fails fast if something goes wrong
$ErrorActionPreference = "Stop"

# Script variables
$RepoRoot = $PSScriptRoot  # Root directory of the repository (where this script is located)
$SolutionPath = Join-Path $RepoRoot "src\mississippi.sln"  # Path to the solution file

# Function to format output with a clear header
# Makes the build output more readable by creating visual sections
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
# This is a wrapper around process execution that captures and displays output
# It also throws an exception if the command fails, allowing for proper error handling
function Invoke-CommandLine {
    param (
        [string]$Command,
        [string]$Arguments
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
    if ($process.ExitCode -ne 0) {
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

# Step 2: Get version using GitVersion
Write-Header "Determining Version with GitVersion"
$gitVersionJson = Invoke-CommandLine -Command "dotnet" -Arguments "gitversion /output json /showvariable SemVer"
$version = $gitVersionJson.Trim()
Write-Host "Version: $version" -ForegroundColor Green

# Step 3: Build the solution
Write-Header "Building Solution"
Invoke-CommandLine -Command "dotnet" -Arguments "restore `"$SolutionPath`""
Invoke-CommandLine -Command "dotnet" -Arguments "build `"$SolutionPath`" --configuration $Configuration --no-restore -p:Version=$version"

Write-Header "Build Process Completed Successfully" 