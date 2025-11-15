Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'RepositoryAutomation.psm1'
Import-Module -Name $automationModulePath -Force

$script:ScriptsRoot = Split-Path -Parent $PSScriptRoot
$script:RepoRoot = Get-RepositoryRoot -StartPath $script:ScriptsRoot

function Resolve-ScratchpadRoot {
    param([string]$ScratchpadRoot)

    if ([string]::IsNullOrWhiteSpace($ScratchpadRoot)) {
        return Join-Path $script:RepoRoot '.scratchpad'
    }

    return [System.IO.Path]::GetFullPath($ScratchpadRoot)
}

function Get-ScratchpadPaths {
    param([string]$ScratchpadRoot)

    $root = Resolve-ScratchpadRoot -ScratchpadRoot $ScratchpadRoot
    $tasks = Join-Path $root 'tasks'
    return [pscustomobject]@{
        Root     = $root
        Tasks    = $tasks
        Pending  = Join-Path $tasks 'pending'
        Claimed  = Join-Path $tasks 'claimed'
        Done     = Join-Path $tasks 'done'
        Deferred = Join-Path $tasks 'deferred'
    }
}

function Initialize-ScratchpadLayout {
    param([string]$ScratchpadRoot)

    $paths = Get-ScratchpadPaths -ScratchpadRoot $ScratchpadRoot
    foreach ($path in @($paths.Root, $paths.Tasks, $paths.Pending, $paths.Claimed, $paths.Done, $paths.Deferred)) {
        if (-not (Test-Path -LiteralPath $path -PathType Container)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }

    return $paths
}

function ConvertTo-Slug {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) { return 'task' }

    $normalized = $Value.ToLowerInvariant()
    $clean = ($normalized -replace '[^a-z0-9]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($clean)) { return 'task' }
    return $clean
}

function New-ScratchpadTaskFileName {
    param(
        [string]$Title,
        [string]$Id
    )

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $slug = ConvertTo-Slug -Value $Title
    return '{0}_{1}_{2}.json' -f $timestamp, $slug, $Id
}

function Read-ScratchpadTask {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Task file not found: $Path"
    }

    $raw = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop
    return [pscustomobject]@{
        Path = $Path
        FileName = Split-Path -Path $Path -Leaf
        Directory = Split-Path -Path $Path -Parent
        Data = $raw
    }
}

function Write-ScratchpadTask {
    param(
        [Parameter(Mandatory)]$Task,
        [Parameter(Mandatory)][string]$Path
    )

    $json = $Task | ConvertTo-Json -Depth 32
    Set-Content -LiteralPath $Path -Value $json -Encoding UTF8
}

function Get-ScratchpadTaskStatusFromPath {
    param([string]$Path, [pscustomobject]$Paths)

    $directory = Split-Path -Path $Path -Parent
    switch ($directory) {
        { $_ -ieq $Paths.Pending }  { return 'pending' }
        { $_ -ieq $Paths.Claimed }  { return 'claimed' }
        { $_ -ieq $Paths.Done }     { return 'done' }
        { $_ -ieq $Paths.Deferred } { return 'deferred' }
        default { return $null }
    }
}

function Find-ScratchpadTaskById {
    param(
        [Parameter(Mandatory)][string]$Id,
        [string]$ScratchpadRoot
    )

    $paths = Get-ScratchpadPaths -ScratchpadRoot $ScratchpadRoot
    foreach ($folder in @($paths.Pending, $paths.Claimed, $paths.Done, $paths.Deferred)) {
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) { continue }
        $match = Get-ChildItem -LiteralPath $folder -Filter '*.json' -File -ErrorAction SilentlyContinue | Where-Object {
            try {
                $content = Get-Content -LiteralPath $_.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
                return ($content.id -eq $Id)
            }
            catch {
                return $false
            }
        } | Select-Object -First 1

        if ($match) {
            return [pscustomobject]@{
                Path = $match.FullName
                Status = Get-ScratchpadTaskStatusFromPath -Path $match.FullName -Paths $paths
                Paths = $paths
            }
        }
    }

    return $null
}

function ConvertTo-ScratchpadTaskRecord {
    param(
        [Parameter(Mandatory)]$Task,
        [string]$Status
    )

    $data = $Task.Data
    return [pscustomobject]@{
        Id = $data.id
        Title = $data.title
        Priority = $data.priority
        Tags = $data.tags
        Status = if ($Status) { $Status } elseif ($data.status) { $data.status } else { 'pending' }
        Attempts = $data.attempts
        ClaimedBy = $data.claimedBy
        ClaimedAt = $data.claimedAt
        CreatedAt = $data.createdAt
        Path = $Task.Path
    }
}

Export-ModuleMember -Function Resolve-ScratchpadRoot, Get-ScratchpadPaths, Initialize-ScratchpadLayout, New-ScratchpadTaskFileName, Read-ScratchpadTask, Write-ScratchpadTask, Find-ScratchpadTaskById, ConvertTo-ScratchpadTaskRecord, Get-ScratchpadTaskStatusFromPath




