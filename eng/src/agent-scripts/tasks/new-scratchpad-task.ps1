#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Title,
    [ValidateSet('P0','P1','P2','P3')] [string]$Priority = 'P2',
    [string[]]$Tags = @(),
    [int]$EffortPoints = 2,
    [string]$Notes = '',
    [string]$ScratchpadRoot,
    [string]$Id,
    [string]$Slug
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'ScratchpadTasks.psm1'
Import-Module -Name $modulePath -Scope Local -Force

$resolvedRoot = Resolve-ScratchpadRoot -ScratchpadRoot $ScratchpadRoot
$paths = Initialize-ScratchpadLayout -ScratchpadRoot $resolvedRoot

if ([string]::IsNullOrWhiteSpace($Id)) {
    $Id = [Guid]::NewGuid().ToString()
}

$effectiveTitle = if ([string]::IsNullOrWhiteSpace($Title)) { 'Task' } else { $Title }
$slugSource = if ([string]::IsNullOrWhiteSpace($Slug)) { $effectiveTitle } else { $Slug }
$fileName = New-ScratchpadTaskFileName -Title $slugSource -Id $Id
$destination = Join-Path $paths.Pending $fileName

if (Test-Path -LiteralPath $destination -PathType Leaf) {
    throw "Task file already exists: $destination"
}

$now = (Get-Date).ToUniversalTime().ToString('o')
$task = [ordered]@{
    schemaVersion = '1.0'
    id             = $Id
    title          = $effectiveTitle
    createdAt      = $now
    priority       = $Priority
    tags           = $Tags
    status         = 'pending'
    claimedBy      = $null
    claimedAt      = $null
    attempts       = 0
    effortPoints   = $EffortPoints
    notes          = $Notes
    result         = $null
    completedAt    = $null
    reason         = $null
    nextSteps      = $null
    deferredAt     = $null
}

Write-ScratchpadTask -Task $task -Path $destination

$output = [pscustomobject]@{
    Id       = $Id
    Title    = $effectiveTitle
    Priority = $Priority
    Tags     = $Tags
    Path     = $destination
}

Write-Host "Created task: $destination" -ForegroundColor Green
Write-Output $output


