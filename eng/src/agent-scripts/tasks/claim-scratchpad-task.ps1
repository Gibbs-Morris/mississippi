#!/usr/bin/env pwsh

[CmdletBinding(DefaultParameterSetName = 'ById')]
param(
    [Parameter(Mandatory, ParameterSetName = 'ById')][string]$Id,
    [Parameter(Mandatory, ParameterSetName = 'ByPath')][string]$TaskPath,
    [string]$Agent = $env:USERNAME,
    [string]$ScratchpadRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'ScratchpadTasks.psm1'
Import-Module -Name $modulePath -Scope Local -Force

$resolvedRoot = Resolve-ScratchpadRoot -ScratchpadRoot $ScratchpadRoot
$paths = Initialize-ScratchpadLayout -ScratchpadRoot $resolvedRoot

if ([string]::IsNullOrWhiteSpace($Agent)) {
    $Agent = 'unknown-agent'
}

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

if ($target.Status -ne 'pending') {
    throw "Task must be in pending to claim (current status: $($target.Status ?? 'unknown'))."
}

$task = Read-ScratchpadTask -Path $target.Path
$attempts = 0
if ($task.Data.PSObject.Properties.Name -contains 'attempts' -and $task.Data.attempts) {
    $attempts = [int]$task.Data.attempts
}

if ($attempts -ge 5) {
    throw "Task '$($task.Data.id)' has reached the maximum of 5 attempts. Defer it instead."
}

$task.Data.status = 'claimed'
$task.Data.claimedBy = $Agent
$task.Data.claimedAt = (Get-Date).ToUniversalTime().ToString('o')
$task.Data.attempts = $attempts + 1

$destination = Join-Path $paths.Claimed $task.FileName
Move-Item -LiteralPath $task.Path -Destination $destination -ErrorAction Stop
Write-ScratchpadTask -Task $task.Data -Path $destination

$output = [pscustomobject]@{
    Id        = $task.Data.id
    Title     = $task.Data.title
    Status    = 'claimed'
    Attempts  = $task.Data.attempts
    ClaimedBy = $task.Data.claimedBy
    Path      = $destination
}

Write-Host "Claimed task: $destination" -ForegroundColor Cyan
Write-Output $output

