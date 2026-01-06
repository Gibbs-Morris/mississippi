#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs the Mississippi Documentation Generator to generate documentation from code.

.DESCRIPTION
    This script builds and runs the DocumentationGenerator console app to generate
    Markdown/MDX documentation with Mermaid diagrams derived from the .NET codebase.

.PARAMETER Configuration
    Build configuration. Default is Release.

.PARAMETER OutputDir
    Optional output directory override. Defaults to docs/Docusaurus/docs/generated.

.PARAMETER Reports
    Optional list of specific reports to run. When not specified, all reports run.

.EXAMPLE
    .\generate-docs.ps1
    Generates all documentation using default settings.

.EXAMPLE
    .\generate-docs.ps1 -Reports "Dependencies","OrleansGrains"
    Generates only the specified reports.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDir,
    [string[]]$Reports
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Determine the repository root directory (this script resides there)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host "=== MISSISSIPPI DOCUMENTATION GENERATOR ===" -ForegroundColor Yellow
Write-Host "Repository Root: $repoRoot"

# Build the generator project
$generatorProject = Join-Path $repoRoot 'src\DocumentationGenerator\DocumentationGenerator.csproj'
Write-Host "Building DocumentationGenerator..." -ForegroundColor Cyan

try {
    & dotnet build $generatorProject --configuration $Configuration --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "Build completed successfully." -ForegroundColor Green
}
catch {
    Write-Error "Failed to build DocumentationGenerator: $_"
    exit 1
}

# Run the generator
Write-Host "Running documentation generator..." -ForegroundColor Cyan

$runArgs = @()

if ($OutputDir) {
    $runArgs += @('--outputDir', $OutputDir)
}

if ($Reports -and $Reports.Count -gt 0) {
    foreach ($report in $Reports) {
        $runArgs += @('--reports', $report)
    }
}

try {
    & dotnet run --project $generatorProject --configuration $Configuration --no-build -- @runArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Generator failed with exit code $LASTEXITCODE"
    }
    Write-Host "=== DOCUMENTATION GENERATION COMPLETED ===" -ForegroundColor Green
}
catch {
    Write-Error "Documentation generation failed: $_"
    exit 1
}
