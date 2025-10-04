#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet('pending','claimed','done','deferred')] [string[]]$Status,
    [ValidateSet('P0','P1','P2','P3')] [string[]]$Priority,
    [string[]]$Tag,
    [string]$Id,
    [switch]$IncludeData,
    [string]$ScratchpadRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'ScratchpadTasks.psm1'
Import-Module -Name $modulePath -Scope Local -Force

$paths = Get-ScratchpadPaths -ScratchpadRoot $ScratchpadRoot
$folders = [ordered]@{
    pending  = $paths.Pending
    claimed  = $paths.Claimed
    done     = $paths.Done
    deferred = $paths.Deferred
}

$statusesToSearch = if ($Status) { $Status } else { $folders.Keys }
$results = New-Object System.Collections.Generic.List[object]

foreach ($state in $statusesToSearch) {
    $folder = $folders[$state]
    if (-not $folder -or -not (Test-Path -LiteralPath $folder -PathType Container)) { continue }

    $files = Get-ChildItem -LiteralPath $folder -Filter '*.json' -File -ErrorAction SilentlyContinue | Sort-Object Name
    foreach ($file in $files) {
        try {
            $task = Read-ScratchpadTask -Path $file.FullName
        }
        catch {
            Write-Warning "Skipping unreadable task file: $($file.FullName). $($_.Exception.Message)"
            continue
        }

        $record = ConvertTo-ScratchpadTaskRecord -Task $task -Status $state
        $created = [datetime]::MinValue
        if ($record.CreatedAt) {
            [void][DateTime]::TryParse($record.CreatedAt, [ref]$created)
        }

        $results.Add([pscustomobject]@{
                Record = $record
                Data   = if ($IncludeData) { $task.Data } else { $null }
                Created = $created
            }) | Out-Null
    }
}

$items = $results.ToArray()

if ($Id) {
    $items = $items | Where-Object { $_.Record.Id -eq $Id }
}

if ($Priority) {
    $items = $items | Where-Object { $Priority -contains $_.Record.Priority }
}

if ($Tag) {
    $items = $items | Where-Object {
        $taskTags = $_.Record.Tags
        if (-not $taskTags) { return $false }
        foreach ($target in $Tag) {
            if ($taskTags -contains $target) { return $true }
        }
        return $false
    }
}

$ordered = $items | Sort-Object Created -Descending

if ($IncludeData) {
    $ordered | ForEach-Object {
        [pscustomobject]@{
            Id        = $_.Record.Id
            Title     = $_.Record.Title
            Priority  = $_.Record.Priority
            Status    = $_.Record.Status
            Attempts  = $_.Record.Attempts
            ClaimedBy = $_.Record.ClaimedBy
            CreatedAt = $_.Record.CreatedAt
            Path      = $_.Record.Path
            Data      = $_.Data
        }
    }
}
else {
    $ordered | ForEach-Object { $_.Record }
}

