#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'ById')]
param(
    [Parameter(Mandatory, ParameterSetName = 'ById')][string]$Id,
    [Parameter(Mandatory, ParameterSetName = 'ByPath')][string]$TaskPath,
    [Parameter(Mandatory)][string]$Reason,
    [string]$NextSteps,
    [string]$ScratchpadRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Reason)) {
    throw 'Reason must be provided when deferring a task.'
}

$modulePath = Join-Path $PSScriptRoot 'ScratchpadTasks.psm1'
Import-Module -Name $modulePath -Scope Local -Force

$resolvedRoot = Resolve-ScratchpadRoot -ScratchpadRoot $ScratchpadRoot
$paths = Initialize-ScratchpadLayout -ScratchpadRoot $resolvedRoot

if ($PSCmdlet.ParameterSetName -eq 'ByPath') {
    $fullPath = [System.IO.Path]::GetFullPath($TaskPath)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Task file not found: $TaskPath"
    }
    $status = Get-ScratchpadTaskStatusFromPath -Path $fullPath -Paths $paths
    $target = [pscustomobject]@{
        Path = $fullPath
        Status = $status
    }
}
else {
    $target = Find-ScratchpadTaskById -Id $Id -ScratchpadRoot $resolvedRoot
    if (-not $target) {
        throw "No task found with id '$Id'."
    }
}

if ($target.Status -notin @('pending','claimed')) {
    throw "Only pending or claimed tasks may be deferred (current status: $($target.Status ?? 'unknown'))."
}

$task = Read-ScratchpadTask -Path $target.Path
$task.Data.status = 'deferred'
$task.Data.reason = $Reason
$task.Data.nextSteps = $NextSteps
$task.Data.deferredAt = (Get-Date).ToUniversalTime().ToString('o')

$destination = Join-Path $paths.Deferred $task.FileName
Move-Item -LiteralPath $task.Path -Destination $destination -ErrorAction Stop
Write-ScratchpadTask -Task $task.Data -Path $destination

$output = [pscustomobject]@{
    Id        = $task.Data.id
    Title     = $task.Data.title
    Status    = 'deferred'
    Reason    = $Reason
    NextSteps = $NextSteps
    Path      = $destination
}

Write-Host "Deferred task: $destination" -ForegroundColor Yellow
Write-Output $output

