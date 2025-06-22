#!/usr/bin/env pwsh
# Description: Cross-platform build script used by automation. Restores tools,
# builds the solution, runs tests with coverage and optionally executes Stryker
# mutation testing. Designed to run under PowerShell Core on Windows, Linux and
# macOS.
[CmdletBinding()]
param (
    # Skip the build step entirely - useful when you just want to run tests on already built code
    [switch]$SkipBuild,
    
    # Skip running tests - useful when you just want to build the code
    [switch]$SkipTests,
    
    # Skip Stryker mutation testing - which can be time-consuming
    [switch]$SkipStryker,
    
    # Build configuration (Debug/Release) - Release is optimized for performance
    [string]$Configuration = "Release"
)

# Set the error action preference to stop on error
# This ensures the script fails fast if something goes wrong
$ErrorActionPreference = "Stop"

# Script variables
$RepoRoot = $PSScriptRoot  # Root directory of the repository (where this script is located)
$SolutionPath = Join-Path $RepoRoot (Join-Path 'src' 'mississippi.sln')  # Path to the solution file
$TestResultsDir = Join-Path $RepoRoot 'test-results'  # Directory for test result outputs
$CoverageOutputFile = Join-Path $RepoRoot 'coverage.xml'  # Path for test coverage report
$StrykerOutputDir = Join-Path $RepoRoot (Join-Path 'src' 'StrykerOutput')  # Directory for mutation testing results

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

# Step 2: Get version using GitVersion
Write-Header "Determining Version with GitVersion"
$gitVersionJson = Invoke-CommandLine -Command "dotnet" -Arguments "gitversion /output json /showvariable SemVer"
$version = $gitVersionJson.Trim()
Write-Host "Version: $version" -ForegroundColor Green

# Step 3: Build the solution
if (-not $SkipBuild) {
    Write-Header "Building Solution"
    Invoke-CommandLine -Command "dotnet" -Arguments "restore `"$SolutionPath`""
    Invoke-CommandLine -Command "dotnet" -Arguments "build `"$SolutionPath`" --configuration $Configuration --no-restore -p:Version=$version"
}
else {
    Write-Host "Skipping build step..." -ForegroundColor Yellow
}

# Step 4: Run tests with code coverage
if (-not $SkipTests) {
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
}
else {
    Write-Host "Skipping tests step..." -ForegroundColor Yellow
}

# Step 5: Run Stryker mutation testing
# Stryker is a mutation testing tool that deliberately changes your code to test the quality of your unit tests
if (-not $SkipStryker) {
    Write-Header "Running Stryker Mutation Testing"
    
    # Change to src directory where stryker-config.json is located
    # Stryker config file defines settings for mutation testing like which files to include/exclude
    Push-Location (Join-Path $RepoRoot "src")
    
    try {
        # Run Stryker mutation testing - it will create mutants (code changes) and check if tests catch them
        Invoke-CommandLine -Command "dotnet" -Arguments "stryker"
        
        # Find the latest Stryker report directory by sorting by creation time
        # Stryker creates a timestamped directory for each run
        $latestStrykerDir = Get-ChildItem -Path $StrykerOutputDir -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
        
        if ($latestStrykerDir) {
            # Path to the JSON report containing detailed mutation testing results
            $reportJsonPath = Join-Path $latestStrykerDir.FullName "reports\mutation-report.json"
            
            if (Test-Path $reportJsonPath) {
                try {
                    # Parse the JSON report to extract mutation scores and metrics
                    # This helps us understand how effective our tests are at catching bugs
                    $strykerReport = Get-Content $reportJsonPath -Raw | ConvertFrom-Json
                    
                    Write-Header "Stryker Mutation Testing Results"
                    if ($strykerReport.PSObject.Properties.Name -contains "mutationScore") {
                        Write-Host "Mutation Score: $($strykerReport.mutationScore)%" -ForegroundColor Green
                    }
                    
                    if ($strykerReport.PSObject.Properties.Name -contains "metrics") {
                        Write-Host "Total Mutants: $($strykerReport.metrics.totalMutants)" -ForegroundColor Cyan
                        Write-Host "Killed Mutants: $($strykerReport.metrics.killedMutants)" -ForegroundColor Green
                        Write-Host "Survived Mutants: $($strykerReport.metrics.survivedMutants)" -ForegroundColor $(if ($strykerReport.metrics.survivedMutants -gt 0) { "Red" } else { "Green" })
                        Write-Host "Timed Out Mutants: $($strykerReport.metrics.timedOutMutants)" -ForegroundColor Yellow
                    } else {
                        Write-Warning "Stryker report doesn't contain metrics information."
                    }
                    
                    Write-Host ""
                    Write-Host "Stryker HTML report available at:" -ForegroundColor Cyan
                    Write-Host $reportJsonPath.Replace("mutation-report.json", "mutation-report.html") -ForegroundColor Cyan
                } catch {
                    Write-Warning "Error parsing Stryker report: $_"
                }
            }
            else {
                Write-Warning "No Stryker report found at $reportJsonPath"
            }
        }
        else {
            Write-Warning "No Stryker output directory found"
        }
    }
    catch {
        Write-Warning "Error running Stryker tests: $_"
    }
    finally {
        # Return to the original directory
        Pop-Location
    }
}
else {
    Write-Host "Skipping Stryker step..." -ForegroundColor Yellow
}

Write-Header "Build Process Completed Successfully"
