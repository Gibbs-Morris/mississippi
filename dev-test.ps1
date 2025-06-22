<#
.SYNOPSIS
    Development test script for the Mississippi framework solution.

.DESCRIPTION
    This script performs a complete build and test cycle for the Mississippi framework solution.
    It builds the solution, runs all unit tests, and generates code coverage reports.
    This is the standard script for most local development scenarios where you need
    to ensure both build success and test quality before committing or pushing changes.

.PARAMETER Configuration
    The build configuration to use (Debug/Release). Defaults to "Release".

.EXAMPLE
    .\dev-test.ps1
    # Builds and tests the solution in Release configuration

.EXAMPLE
    .\dev-test.ps1 -Configuration Debug
    # Builds and tests the solution in Debug configuration

.NOTES
    This script is part of a set of four specialized development scripts:
    - dev-build.ps1: Development build only
    - dev-test.ps1: Development build + unit tests + coverage (this script)
    - dev-quality.ps1: Development quality assurance (full pipeline)
    - dev-cleanup.ps1: Development code cleanup

    For Cursor and Copilot: This is the recommended script for most local development scenarios.
    Use this script when you need to:
    - Verify that your code builds and passes all tests before committing
    - Generate code coverage reports for quality analysis
    - Run the standard development workflow
    - Ensure code quality before pushing changes
    - Validate changes after significant refactoring

    The script will:
    1. Restore .NET tools and dependencies
    2. Determine version using GitVersion
    3. Build the entire solution
    4. Run all unit tests with coverage collection
    5. Display a comprehensive coverage summary
    6. Generate coverage.xml and test results

    Note: For CI/CD, this project uses GitHub Actions workflows.

.LINK
    Related scripts: dev-build.ps1, dev-quality.ps1, dev-cleanup.ps1
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
$TestResultsDir = Join-Path $RepoRoot "test-results"  # Directory for test result outputs
$CoverageOutputFile = Join-Path $RepoRoot "coverage.xml"  # Path for test coverage report

# Ensure the test results directory exists
if (-not (Test-Path $TestResultsDir)) {
    New-Item -ItemType Directory -Path $TestResultsDir | Out-Null
}

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

# Function to calculate percentage safely (prevent division by zero)
# This prevents errors when calculating coverage percentages with zero values
function Get-SafePercentage {
    param (
        [double]$Numerator,
        [double]$Denominator
    )
    
    if ($Denominator -eq 0 -or $Numerator -eq 0) {
        return "0.00"
    }
    
    return [Math]::Round(($Numerator / $Denominator) * 100, 2)
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

# Step 4: Run tests with code coverage
Write-Header "Running Unit Tests with Code Coverage"

# Run tests with coverage
Invoke-CommandLine -Command "dotnet" -Arguments "dotnet-coverage collect `"dotnet test `"$SolutionPath`" --configuration $Configuration --no-build --verbosity normal --logger \`"trx;LogFileName=test_results.trx\`" --results-directory `"$TestResultsDir`"`" -f xml -o `"$CoverageOutputFile`""

# Display coverage summary
Write-Header "Code Coverage Summary"

# Parse the coverage XML and display summary
if (Test-Path $CoverageOutputFile) {
    try {
        [xml]$coverageReport = Get-Content $CoverageOutputFile
        if ($coverageReport -and $coverageReport.CoverageSession -and $coverageReport.CoverageSession.Summary) {
            $coverageSummary = $coverageReport.CoverageSession.Summary
            
            $linesCovered = [int]$coverageSummary.LinesCovered
            $linesTotal = [int]$coverageSummary.LinesTotal
            $linePercentage = Get-SafePercentage -Numerator $linesCovered -Denominator $linesTotal
            
            $branchesCovered = [int]$coverageSummary.BranchesCovered
            $branchesTotal = [int]$coverageSummary.BranchesTotal
            $branchPercentage = Get-SafePercentage -Numerator $branchesCovered -Denominator $branchesTotal
            
            $methodsCovered = [int]$coverageSummary.MethodsCovered
            $methodsTotal = [int]$coverageSummary.MethodsTotal
            $methodPercentage = Get-SafePercentage -Numerator $methodsCovered -Denominator $methodsTotal
            
            Write-Host "Line Coverage: $linesCovered/$linesTotal = $linePercentage%" -ForegroundColor Green
            Write-Host "Branch Coverage: $branchesCovered/$branchesTotal = $branchPercentage%" -ForegroundColor Green
            Write-Host "Method Coverage: $methodsCovered/$methodsTotal = $methodPercentage%" -ForegroundColor Green
        } else {
            Write-Warning "Coverage report doesn't contain summary information."
        }
    } catch {
        Write-Warning "Error parsing coverage report: $_"
    }
}
else {
    Write-Warning "No coverage report found at $CoverageOutputFile"
}

Write-Header "Build and Test Process Completed Successfully" 