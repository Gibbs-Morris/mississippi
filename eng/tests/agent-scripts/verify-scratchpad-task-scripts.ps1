#!/usr/bin/env pwsh

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param([Parameter(Mandatory)][string]$StartPath)

    $current = (Resolve-Path -LiteralPath $StartPath).Path
    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current '.git')) { return $current }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            break
        }
        $current = $parent
    }

    throw "Unable to locate repository root from $StartPath"
}

$repoRoot = Resolve-RepoRoot -StartPath $PSScriptRoot
$scriptsRoot = Join-Path $repoRoot 'eng/src/agent-scripts/tasks'

$paths = [pscustomobject]@{
    New      = Join-Path $scriptsRoot 'new-scratchpad-task.ps1'
    List     = Join-Path $scriptsRoot 'list-scratchpad-tasks.ps1'
    Claim    = Join-Path $scriptsRoot 'claim-scratchpad-task.ps1'
    Complete = Join-Path $scriptsRoot 'complete-scratchpad-task.ps1'
    Defer    = Join-Path $scriptsRoot 'defer-scratchpad-task.ps1'
}

foreach ($prop in $paths.PSObject.Properties) {
    if (-not (Test-Path -LiteralPath $prop.Value -PathType Leaf)) {
        throw "Required script missing: $($prop.Value)"
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("scratchpad-" + ([Guid]::NewGuid().ToString('N')))
$scratchpadRoot = Join-Path $tempRoot '.scratchpad'

$createdPaths = @()

try {
    Write-Host "Using temporary scratchpad: $scratchpadRoot" -ForegroundColor Cyan

    $taskA = & $paths.New -Title 'Verify Completion Flow' -Priority 'P1' -ScratchpadRoot $scratchpadRoot -Tags 'orchestration'
    $taskB = & $paths.New -Title 'Verify Deferral Flow' -Priority 'P2' -ScratchpadRoot $scratchpadRoot -Tags 'orchestration'
    $createdPaths += $taskA.Path
    $createdPaths += $taskB.Path

    Write-Host "Created tasks: $($taskA.Id), $($taskB.Id)" -ForegroundColor Green

    $pending = & $paths.List -ScratchpadRoot $scratchpadRoot -Status 'pending'
    if ($pending.Count -ne 2) {
        throw "Expected 2 pending tasks, found $($pending.Count)."
    }

    $claimed = & $paths.Claim -Id $taskA.Id -ScratchpadRoot $scratchpadRoot -Agent 'orchestrator'
    Write-Host "Claimed task $($claimed.Id)" -ForegroundColor Yellow

    $completed = & $paths.Complete -Id $taskA.Id -Result 'Orchestration validation succeeded' -ScratchpadRoot $scratchpadRoot
    Write-Host "Completed task $($completed.Id)" -ForegroundColor Green

    $deferred = & $paths.Defer -Id $taskB.Id -Reason 'Integration pending external dependency' -ScratchpadRoot $scratchpadRoot
    Write-Host "Deferred task $($deferred.Id)" -ForegroundColor Yellow

    $doneList = & $paths.List -ScratchpadRoot $scratchpadRoot -Status 'done'
    $deferredList = & $paths.List -ScratchpadRoot $scratchpadRoot -Status 'deferred'

    if ($doneList.Count -ne 1 -or $doneList[0].Id -ne $taskA.Id) {
        throw 'Completed task not found via list script.'
    }

    if ($deferredList.Count -ne 1 -or $deferredList[0].Id -ne $taskB.Id) {
        throw 'Deferred task not found via list script.'
    }

    Write-Host 'Scratchpad task scripts verified successfully.' -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Write-Host "Cleaning up temporary scratchpad: $tempRoot" -ForegroundColor Gray
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
