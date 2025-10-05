#requires -Version 7.0

<#
.SYNOPSIS
    Shared helpers for deterministic scratchpad task generation.

.DESCRIPTION
    Provides reusable functions that standardize the way agent scripts create
    and synchronize task JSON files under the repository scratchpad. Keeping
    these helpers in a dedicated module makes the logic easier to unit test
    and keeps automation scripts focused on parsing their domain-specific data.
#>

function Get-DeterministicTaskHash {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Value
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash($bytes)
    }
    finally {
        $sha.Dispose()
    }

    return ([BitConverter]::ToString($hashBytes) -replace '-', '').ToLowerInvariant()
}

function Get-AutoTaskSlug {
    [CmdletBinding()]
    param(
        [string[]]$Components,
        [int]$MaxLength = 48
    )

    if (-not $Components -or $Components.Count -eq 0) {
        return 'task'
    }

    $joined = ($Components -join '-')
    $slug = ($joined.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($slug)) {
        $slug = 'task'
    }

    if ($slug.Length -gt $MaxLength) {
        $slug = $slug.Substring(0, $MaxLength)
    }

    return $slug
}

function New-TaskItem {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Category,
        [Parameter(Mandatory)][string]$UniqueKey,
        [string[]]$KeyPropertyNames = @('autoTaskKey'),
        [Parameter(Mandatory)][string]$Title,
        [string]$Priority = 'P2',
        [string]$Notes,
        [string[]]$Tags,
        [string[]]$RelatedFiles,
        [string[]]$References,
        [int]$EffortPoints = 2,
        [hashtable]$AdditionalProperties,
        [string[]]$SlugComponents
    )

    if ([string]::IsNullOrWhiteSpace($Category)) {
        throw "Category must be provided."
    }

    if ([string]::IsNullOrWhiteSpace($UniqueKey)) {
        throw "UniqueKey must be provided."
    }

    if ([string]::IsNullOrWhiteSpace($Title)) {
        throw "Title must be provided."
    }

    return [pscustomobject]@{
        Category            = $Category
        UniqueKey           = $UniqueKey
        KeyPropertyNames    = $KeyPropertyNames
        Title               = $Title
        Priority            = $Priority
        Notes               = $Notes
        Tags                = @($Tags | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        RelatedFiles        = @($RelatedFiles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        References          = @($References | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        EffortPoints        = if ($EffortPoints -gt 0) { $EffortPoints } else { 2 }
        AdditionalProperties = $AdditionalProperties
        SlugComponents      = if ($SlugComponents) { $SlugComponents } else { @($Category, $Title) }
    }
}

function Sync-AutoTasks {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()]
        [psobject[]]$Tasks,
        [Parameter(Mandatory)][string]$RepoRoot,
        [switch]$WhatIf
    )

    if (-not $Tasks -or $Tasks.Count -eq 0) {
        Write-Verbose 'No tasks supplied to Sync-AutoTasks; nothing to do.'
        return @()
    }

    if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
        throw "RepoRoot '$RepoRoot' is not a directory."
    }

    $scratchpadRoot = Join-Path $RepoRoot '.scratchpad/tasks'
    $pendingPath = Join-Path $scratchpadRoot 'pending'

    if (-not (Test-Path -LiteralPath $pendingPath)) {
        if (-not $WhatIf) {
            $null = New-Item -ItemType Directory -Path $pendingPath -Force
        }
    }

    $keyNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($task in $Tasks) {
        if ($task.KeyPropertyNames) {
            foreach ($keyName in $task.KeyPropertyNames) {
                if (-not [string]::IsNullOrWhiteSpace($keyName)) {
                    [void]$keyNames.Add($keyName)
                }
            }
        }
    }

    if (-not $keyNames.Contains('autoTaskKey')) {
        [void]$keyNames.Add('autoTaskKey')
    }

    $existingKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $statusFolders = @('pending', 'claimed', 'done', 'deferred')

    foreach ($status in $statusFolders) {
        $statusPath = Join-Path $scratchpadRoot $status
        if (-not (Test-Path -LiteralPath $statusPath)) {
            continue
        }

        Get-ChildItem -Path $statusPath -Filter '*.json' -File -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $payload = Get-Content -Path $_.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
                foreach ($name in $keyNames) {
                    if ($payload.PSObject.Properties.Name -contains $name) {
                        $value = $payload.$name
                        if (-not [string]::IsNullOrWhiteSpace($value)) {
                            [void]$existingKeys.Add($value)
                        }
                    }
                }
            }
            catch {
                Write-Warning "Failed to parse scratchpad task '$($_.FullName)': $($_.Exception.Message)"
            }
        }
    }

    $results = New-Object System.Collections.Generic.List[pscustomobject]
    $now = (Get-Date).ToUniversalTime()
    $timestamp = $now.ToString('yyyyMMddTHHmmssZ')

    foreach ($task in $Tasks) {
        $uniqueKey = $task.UniqueKey
        if ([string]::IsNullOrWhiteSpace($uniqueKey)) {
            Write-Warning 'Skipping task with empty UniqueKey.'
            continue
        }

        if ($existingKeys.Contains($uniqueKey)) {
            $results.Add([pscustomobject]@{
                    Task      = $task
                    Status    = 'Skipped'
                    Reason    = 'Existing task'
                    FilePath  = $null
                }) | Out-Null
            continue
        }

        $hash = Get-DeterministicTaskHash -Value $uniqueKey
        $slug = Get-AutoTaskSlug -Components $task.SlugComponents
        $fileName = '{0}_{1}_{2}.json' -f $timestamp, $slug, $hash.Substring(0, 12)
        $filePath = Join-Path $pendingPath $fileName
        $taskId = '{0}-{1}' -f $task.Category, $hash.Substring(0, 26)

        $payload = [ordered]@{
            schemaVersion = '1.0'
            id            = $taskId
            category      = $task.Category
            title         = $task.Title
            createdAt     = $now.ToString('o')
            priority      = $task.Priority
            tags          = $task.Tags
            status        = 'pending'
            claimedBy     = $null
            claimedAt     = $null
            attempts      = 0
            effortPoints  = $task.EffortPoints
            relatedFiles  = $task.RelatedFiles
            references    = $task.References
            notes         = $task.Notes
            autoTaskKey   = $uniqueKey
            completedAt   = $null
            result        = $null
            reason        = $null
            nextSteps     = $null
        }

        if ($task.AdditionalProperties) {
            foreach ($pair in $task.AdditionalProperties.GetEnumerator()) {
                $payload[$pair.Key] = $pair.Value
            }
        }

        if ($task.KeyPropertyNames) {
            foreach ($keyName in $task.KeyPropertyNames) {
                if (-not [string]::IsNullOrWhiteSpace($keyName) -and -not $payload.Contains($keyName)) {
                    $payload[$keyName] = $uniqueKey
                }
            }
        }

        if ($WhatIf) {
            $results.Add([pscustomobject]@{
                    Task      = $task
                    Status    = 'Planned'
                    Reason    = 'WhatIf'
                    FilePath  = $filePath
                }) | Out-Null
            continue
        }

        $json = $payload | ConvertTo-Json -Depth 10
        Set-Content -Path $filePath -Value $json -Encoding UTF8
        [void]$existingKeys.Add($uniqueKey)

        $results.Add([pscustomobject]@{
                Task      = $task
                Status    = 'Created'
                Reason    = $null
                FilePath  = $filePath
            }) | Out-Null
    }

    return $results.ToArray()
}

Export-ModuleMember -Function Get-DeterministicTaskHash, Get-AutoTaskSlug, New-TaskItem, Sync-AutoTasks
