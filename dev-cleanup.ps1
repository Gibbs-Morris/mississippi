<#
.SYNOPSIS
    Code cleanup script for the Mississippi framework solution using ReSharper.

.DESCRIPTION
    This script performs automated code cleanup on the Mississippi framework solution using
    JetBrains ReSharper command-line tools. It applies code formatting, style corrections,
    and best practices according to the project's ReSharper settings. This script is essential
    for maintaining consistent code quality and ensuring the codebase passes GitHub Actions
    cleanup checks.

.PARAMETER SolutionPath
    Path to the solution file. Defaults to "src\mississippi.sln" relative to the script location.

.PARAMETER DotSettingsPath
    Path to the ReSharper DotSettings file containing project-specific rules.
    Defaults to "src\mississippi.sln.DotSettings" relative to the script location.

.PARAMETER Profile
    The ReSharper cleanup profile to use. Defaults to "Built-in: Full Cleanup".

.EXAMPLE
    .\cleanup.ps1
    # Runs cleanup using default settings and the "Built-in: Full Cleanup" profile

.EXAMPLE
    .\cleanup.ps1 -Profile "Built-in: Reformat Code"
    # Runs cleanup using only code reformatting without other corrections

.EXAMPLE
    .\cleanup.ps1 -SolutionPath "path\to\custom.sln"
    # Runs cleanup on a custom solution file

.NOTES
    This script is part of the local development toolset for the Mississippi framework:
    - build-only.ps1: Quick build verification
    - build-and-test.ps1: Standard development workflow with tests
    - build-test-mutate.ps1: Comprehensive quality assurance
    - cleanup.ps1: Code cleanup and formatting (this script)

    For Cursor and Copilot: Use this script when you need to:
    - Clean up code formatting and style issues
    - Apply consistent code standards across the solution
    - Fix ReSharper warnings and suggestions automatically
    - Prepare code for GitHub Actions cleanup checks
    - Maintain code quality standards
    - Resolve formatting inconsistencies after merging code

    The script will:
    1. Restore .NET tools and ensure ReSharper CLI tools are available
    2. Check for and install JetBrains.ReSharper.GlobalTools if needed
    3. Run ReSharper cleanup using the specified profile and settings
    4. Generate an inspection report of any remaining issues
    5. Save the report to resharper-report.xml for manual review

    IMPORTANT: This script modifies your source code files. Make sure you have:
    - Committed or backed up your current changes
    - Reviewed the cleanup profile to understand what changes will be made
    - Tested the cleanup on a small subset of files if unsure

    The cleanup process may take several minutes depending on the size of the solution.

.LINK
    Related scripts: build-only.ps1, build-and-test.ps1, build-test-mutate.ps1
#>

#!/usr/bin/env pwsh
[CmdletBinding()]
param (
    # Path to the solution file
    [string]$SolutionPath = (Join-Path $PSScriptRoot "src\mississippi.sln"),
    
    # Path to the DotSettings file
    [string]$DotSettingsPath = (Join-Path $PSScriptRoot "src\mississippi.sln.DotSettings"),
    
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
    Invoke-CommandLine -Command "dotnet" -Arguments "jb cleanupcode --profile=`"$Profile`" --settings=`"$DotSettingsPath`" `"$SolutionPath`""
}
else {
    Invoke-CommandLine -Command "dotnet" -Arguments "jb cleanupcode --profile=`"$Profile`" `"$SolutionPath`""
}

# Step 3: Check for any remaining issues
Write-Header "Checking for any remaining ReSharper issues"
Invoke-CommandLine -Command "dotnet" -Arguments "jb inspectcode `"$SolutionPath`" --output=`"$RepoRoot\resharper-report.xml`""

Write-Host "ReSharper inspection report saved to $RepoRoot\resharper-report.xml" -ForegroundColor Green
Write-Host "You can open this file to view any remaining issues that need to be addressed manually." -ForegroundColor Green

Write-Header "Cleanup Completed Successfully"
Write-Host "Your code has been cleaned up according to ReSharper rules."
Write-Host "You should now be able to pass the GitHub Actions cleanup check."