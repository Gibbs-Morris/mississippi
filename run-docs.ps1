#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Mississippi Docusaurus documentation site from the repository root.

.DESCRIPTION
    Starts the Docusaurus development server by default from docs/Docusaurus.
    Optionally builds the site or serves the built output.

.EXAMPLE
    ./run-docs.ps1

.EXAMPLE
    ./run-docs.ps1 -Mode Serve

.EXAMPLE
    ./run-docs.ps1 -Mode Start --port 3001
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Start', 'Build', 'Serve')]
    [string]$Mode = 'Start',

    [Parameter()]
    [switch]$SkipInstall,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ExitCode = 0
$script:FailureMessage = $null

function Stop-Script {
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter()]
        [int]$Code = 1
    )

    $script:FailureMessage = $Message
    $script:ExitCode = $Code
    throw [System.InvalidOperationException]::new($Message)
}

$docsRoot = Join-Path $PSScriptRoot 'docs/Docusaurus'
$packageJsonPath = Join-Path $docsRoot 'package.json'
$packageLockPath = Join-Path $docsRoot 'package-lock.json'
$nodeModulesPath = Join-Path $docsRoot 'node_modules'

if (-not (Test-Path $docsRoot)) {
    Stop-Script -Message "Docs directory not found at: $docsRoot"
}

if (-not (Test-Path $packageJsonPath)) {
    Stop-Script -Message "package.json not found at: $packageJsonPath"
}

$npmCommand = Get-Command npm -ErrorAction SilentlyContinue
if ($null -eq $npmCommand) {
    Stop-Script -Message 'npm was not found on PATH. Install Node.js 20+ and ensure npm is available.'
}

$script:NpmExecutable = $npmCommand.Source

function Invoke-NpmCommand {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    if ($script:NpmExecutable.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)) {
        & pwsh -NoProfile -File $script:NpmExecutable @Arguments
    }
    else {
        & $script:NpmExecutable @Arguments
    }

    if ($LASTEXITCODE -ne 0) {
        Stop-Script -Message "$FailureMessage Exit code: $LASTEXITCODE" -Code $LASTEXITCODE
    }
}

try {
    Push-Location $docsRoot
    try {
        if (-not $SkipInstall.IsPresent -and -not (Test-Path $nodeModulesPath)) {
            if (Test-Path $packageLockPath) {
                Write-Host 'Restoring docs dependencies with npm ci...' -ForegroundColor Cyan
                Invoke-NpmCommand -Arguments @('ci') -FailureMessage 'npm ci failed.'
            }
            else {
                Write-Host 'Restoring docs dependencies with npm install...' -ForegroundColor Cyan
                Invoke-NpmCommand -Arguments @('install') -FailureMessage 'npm install failed.'
            }
        }

        $scriptName = switch ($Mode) {
            'Start' { 'start' }
            'Build' { 'build' }
            'Serve' { 'serve' }
            default { throw "Unsupported mode: $Mode" }
        }

        Write-Host "Running docs command: npm run $scriptName" -ForegroundColor Green
        if (($null -ne $ScriptArgs) -and ($ScriptArgs.Length -gt 0)) {
            Write-Host "Forwarding arguments: $($ScriptArgs -join ' ')" -ForegroundColor Green
            Invoke-NpmCommand -Arguments (@('run', $scriptName, '--') + $ScriptArgs) -FailureMessage "npm run $scriptName failed."
        }
        else {
            Invoke-NpmCommand -Arguments @('run', $scriptName) -FailureMessage "npm run $scriptName failed."
        }
    }
    finally {
        Pop-Location
    }
}
catch {
    if ($script:ExitCode -eq 0) {
        $script:FailureMessage = $_.Exception.Message
        $script:ExitCode = 1
    }
}

if ($script:ExitCode -ne 0) {
    [Console]::Error.WriteLine($script:FailureMessage)
    exit $script:ExitCode
}

exit 0