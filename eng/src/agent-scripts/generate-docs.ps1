#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates documentation from source code for the Mississippi repository.

.DESCRIPTION
    This script builds and runs the DocumentationGenerator tool to create
    Mermaid diagrams and documentation from the repository's .NET code.

    Generated documentation includes:
    - Project dependency diagrams
    - Class diagrams
    - Orleans grain call mappings

    Output is written to {DocusaurusRoot}/docs/generated/

.PARAMETER RepoRoot
    Override the repository root path. Default: autodiscover.

.PARAMETER DocusaurusRoot
    Override the Docusaurus root path. Default: autodiscover from docs/Docusaurus.

.PARAMETER OutputDir
    Override the output directory. Default: {DocusaurusDocsRoot}/generated.

.PARAMETER ConfigPath
    Override the configuration file path. Default: docsgen.json at repo root.

.PARAMETER Reports
    Specify which reports to run. Default: all reports.
    Valid values: ProjectDependencies, ClassDiagrams, OrleansGrains

.PARAMETER Configuration
    Build configuration. Default: Release.

.EXAMPLE
    ./generate-docs.ps1
    Runs all reports with default settings.

.EXAMPLE
    ./generate-docs.ps1 -Reports ProjectDependencies,OrleansGrains
    Runs only the specified reports.

.EXAMPLE
    ./generate-docs.ps1 -RepoRoot /path/to/repo -OutputDir /path/to/output
    Runs with explicit paths.
#>

[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$DocusaurusRoot,
    [string]$OutputDir,
    [string]$ConfigPath,
    [string[]]$Reports,
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force

# Discover repo root if not provided
if (-not $RepoRoot) {
    $RepoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
}

Write-Host '=== DOCUMENTATION GENERATOR ===' -ForegroundColor Cyan
Write-Host

# Resolve paths
$generatorProject = Join-Path $RepoRoot 'src/DocumentationGenerator/DocumentationGenerator.csproj'
$defaultConfigPath = Join-Path $RepoRoot 'docsgen.json'

if (-not (Test-Path -LiteralPath $generatorProject)) {
    Write-Error "DocumentationGenerator project not found at: $generatorProject"
    exit 1
}

Write-Host "Repository root: $RepoRoot"

# Build the generator
Write-Host
Write-Host '[1/2] Building DocumentationGenerator...' -ForegroundColor Yellow
try {
    $buildArgs = @('build', $generatorProject, '--configuration', $Configuration, '--no-incremental')
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host 'Build completed successfully.' -ForegroundColor Green
}
catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    exit 1
}

# Run the generator
Write-Host
Write-Host '[2/2] Running DocumentationGenerator...' -ForegroundColor Yellow

$runArgs = @('run', '--project', $generatorProject, '--configuration', $Configuration, '--no-build', '--')

if ($RepoRoot) {
    $runArgs += '--repoRoot'
    $runArgs += $RepoRoot
}

if ($DocusaurusRoot) {
    $runArgs += '--docusaurusRoot'
    $runArgs += $DocusaurusRoot
}

if ($OutputDir) {
    $runArgs += '--outputDir'
    $runArgs += $OutputDir
}

if ($ConfigPath) {
    $runArgs += '--config'
    $runArgs += $ConfigPath
} elseif (Test-Path -LiteralPath $defaultConfigPath) {
    $runArgs += '--config'
    $runArgs += $defaultConfigPath
}

if ($Reports -and $Reports.Count -gt 0) {
    foreach ($report in $Reports) {
        $runArgs += '--reports'
        $runArgs += $report
    }
}

try {
    & dotnet @runArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Generator failed with exit code $LASTEXITCODE"
    }
    Write-Host
    Write-Host '=== DOCUMENTATION GENERATION COMPLETED ===' -ForegroundColor Green
}
catch {
    Write-Error "Documentation generation failed: $($_.Exception.Message)"
    exit 1
}
