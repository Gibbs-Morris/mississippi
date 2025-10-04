#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'ById')]
param(
    [Parameter(Mandatory, ParameterSetName = 'ById')][string]$Id,
    [Parameter(Mandatory, ParameterSetName = 'ByPath')][string]$TaskPath,
    [Parameter(Mandatory)][string]$Result,
    [string]$ScratchpadRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Result)) {
    throw 'Result must be provided when completing a task.'
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

if ($target.Status -ne 'claimed') {
    throw "Task must be in claimed to complete (current status: $($target.Status ?? 'unknown'))."
}

$task = Read-ScratchpadTask -Path $target.Path
$task.Data.status = 'done'
$task.Data.completedAt = (Get-Date).ToUniversalTime().ToString('o')
$task.Data.result = $Result

$destination = Join-Path $paths.Done $task.FileName
Move-Item -LiteralPath $task.Path -Destination $destination -ErrorAction Stop
Write-ScratchpadTask -Task $task.Data -Path $destination

$output = [pscustomobject]@{
    Id        = $task.Data.id
    Title     = $task.Data.title
    Status    = 'done'
    CompletedAt = $task.Data.completedAt
    Result    = $Result
    Path      = $destination
}

Write-Host "Completed task: $destination" -ForegroundColor Green
Write-Output $output

