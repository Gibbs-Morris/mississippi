[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir '..')).Path
}

function Get-Frontmatter {
    param([string]$Path)

    $content = Get-Content -Raw -Path $Path
    if ($content -notmatch '(?s)^---\r?\n(.*?)\r?\n---') {
        throw "Missing YAML frontmatter in $Path"
    }

    $frontmatterText = $Matches[1]
    $lines = @($frontmatterText -split '\r?\n')
    $data = [ordered]@{}

    function Parse-InlineArray {
        param([string]$Value)

        $trimmed = $Value.Trim()
        if ($trimmed -eq '[]') {
            return @()
        }

        if ($trimmed -notmatch '^\[(.*)\]$') {
            return @()
        }

        $inner = $Matches[1].Trim()
        if ([string]::IsNullOrWhiteSpace($inner)) {
            return @()
        }

        $items = foreach ($part in ($inner -split ',')) {
            $item = $part.Trim()
            if ($item -match '^"(.*)"$') {
                $Matches[1]
            } else {
                $item
            }
        }

        return @($items)
    }

    function Parse-Scalar {
        param([string]$Value)

        $trimmed = $Value.Trim()
        if ($trimmed -eq 'true') { return $true }
        if ($trimmed -eq 'false') { return $false }
        if ($trimmed -match '^"(.*)"$') { return $Matches[1] }
        return $trimmed
    }

    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        if ($line -match '^handoffs:\s*$') {
            $handoffs = @()
            $i++
            while ($i -lt $lines.Count -and $lines[$i] -match '^\s{2}-\s') {
                $handoff = [ordered]@{}
                while ($i -lt $lines.Count) {
                    $current = $lines[$i]
                    if ($current -match '^\s{2}-\s+([A-Za-z-]+):\s*(.*)$') {
                        $handoff[$Matches[1]] = Parse-Scalar $Matches[2]
                        $i++
                        continue
                    }

                    if ($current -match '^\s{4}([A-Za-z-]+):\s*(.*)$') {
                        $handoff[$Matches[1]] = Parse-Scalar $Matches[2]
                        $i++
                        continue
                    }

                    break
                }

                $handoffs += [pscustomobject]$handoff
            }

            $data['handoffs'] = $handoffs
            continue
        }

        if ($line -match '^([A-Za-z-]+):\s*(.*)$') {
            $key = $Matches[1]
            $value = $Matches[2]
            if ($value.Trim().StartsWith('[')) {
                $data[$key] = Parse-InlineArray $value
            } else {
                $data[$key] = Parse-Scalar $value
            }
        }

        $i++
    }

    return [pscustomobject]$data
}

function Get-WorkflowRoster {
    param([string]$Path)

    $content = Get-Content -Raw -Path $Path
    $section = [regex]::Match($content, '(?s)## Agent Roster .*?## Quality Bar').Value
    if ([string]::IsNullOrWhiteSpace($section)) {
        throw "Could not locate Agent Roster section in $Path"
    }

    $names = @()
    foreach ($line in ($section -split '\r?\n')) {
        if ($line -match '^\|\s*(cs [^|]+?)\s*\|') {
            $names += $Matches[1].Trim()
        }
    }

    return $names
}

function To-Array {
    param([object]$Value)

    if ($null -eq $Value) { return @() }
    if ($Value -is [System.Array]) { return @($Value) }
    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) { return @($Value) }
    return @($Value)
}

function Compare-ExactArray {
    param(
        [string]$Label,
        [object[]]$Expected,
        [object[]]$Actual,
        [System.Collections.Generic.List[string]]$Errors
    )

    if ($Expected.Count -ne $Actual.Count -or (@($Expected) -join "`n") -ne (@($Actual) -join "`n")) {
        $Errors.Add("$Label mismatch.`nExpected: $(@($Expected) -join ', ')`nActual:   $(@($Actual) -join ', ')")
    }
}

$repoRoot = Get-RepoRoot
$workflowPath = Join-Path $repoRoot '.github\clean-squad\WORKFLOW.md'
$manifestPath = Join-Path $repoRoot '.github\clean-squad\customization-manifest.json'

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json -Depth 20
$workflowRoster = Get-WorkflowRoster -Path $workflowPath
$errors = [System.Collections.Generic.List[string]]::new()

$workflowSha = (Get-FileHash -Path $workflowPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($manifest.workflowContractSha256 -ne $workflowSha) {
    $errors.Add("Workflow digest mismatch. Manifest expects $($manifest.workflowContractSha256) but current workflow digest is $workflowSha.")
}

$manifestNames = @($manifest.agents | ForEach-Object workflowRosterLabel)
Compare-ExactArray -Label 'Workflow roster vs manifest membership' -Expected $workflowRoster -Actual $manifestNames -Errors $errors

$publicAgents = @($manifest.agents | Where-Object public)
if ($publicAgents.Count -ne 2) {
    $errors.Add("Expected exactly 2 public agents but found $($publicAgents.Count).")
}

$expectedPublic = @('cs Entrepreneur', 'cs River Orchestrator')
Compare-ExactArray -Label 'Public agent set' -Expected $expectedPublic -Actual @($publicAgents | ForEach-Object workflowRosterLabel) -Errors $errors

$rootCoordinators = @($manifest.agents | Where-Object delegationKind -eq 'root coordinator')
if ($rootCoordinators.Count -ne 1 -or $rootCoordinators[0].workflowRosterLabel -ne 'cs River Orchestrator') {
    $errors.Add('Expected exactly one root coordinator named cs River Orchestrator.')
}

$internalNames = @($manifest.agents | Where-Object { -not $_.public } | ForEach-Object workflowRosterLabel)

foreach ($entry in $manifest.agents) {
    $filePath = Join-Path $repoRoot $entry.file
    if (-not (Test-Path $filePath)) {
        $errors.Add("Manifest file path does not exist: $($entry.file)")
        continue
    }

    $frontmatter = Get-Frontmatter -Path $filePath
    if ($frontmatter.name -ne $entry.frontmatterName) {
        $errors.Add("$($entry.id): frontmatter name mismatch. Expected '$($entry.frontmatterName)' but found '$($frontmatter.name)'.")
    }

    if ([bool]$frontmatter.'user-invocable' -ne [bool]$entry.public) {
        $errors.Add("$($entry.id): user-invocable mismatch.")
    }

    if (-not $entry.public -and [bool]$frontmatter.'disable-model-invocation' -ne $true) {
        $errors.Add("$($entry.id): internal agent must set disable-model-invocation: true.")
    }

    $actualAgents = To-Array $frontmatter.agents
    Compare-ExactArray -Label "$($entry.id): agents allowlist" -Expected @($entry.allowedSubagentTargets) -Actual @($actualAgents) -Errors $errors

    $actualHandoffs = @()
    foreach ($handoff in (To-Array $frontmatter.handoffs)) {
        if ($handoff.agent) {
            $actualHandoffs += [string]$handoff.agent
        }
    }

    Compare-ExactArray -Label "$($entry.id): handoff targets" -Expected @($entry.allowedHandoffTargets) -Actual $actualHandoffs -Errors $errors

    $sourceSha = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($entry.sourceSha256 -ne $sourceSha) {
        $errors.Add("$($entry.id): source SHA mismatch.")
    }
}

$river = $manifest.agents | Where-Object id -eq 'cs-river-orchestrator'
if ($null -eq $river) {
    $errors.Add('Manifest does not contain cs-river-orchestrator.')
} else {
    Compare-ExactArray -Label 'River allowlist' -Expected $internalNames -Actual @($river.allowedSubagentTargets) -Errors $errors
}

$entrepreneurPath = Join-Path $repoRoot '.github\agents\cs-entrepreneur.agent.md'
$entrepreneur = Get-Frontmatter -Path $entrepreneurPath
$handoff = @(To-Array $entrepreneur.handoffs)
if ($handoff.Count -ne 1 -or $handoff[0].agent -ne 'cs-river-orchestrator' -or [bool]$handoff[0].send -ne $false) {
    $errors.Add('cs Entrepreneur must expose exactly one public handoff to cs-river-orchestrator with send: false.')
}

if ($errors.Count -gt 0) {
    Write-Host 'Clean Squad customization validation failed:' -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "- $error" -ForegroundColor Red
    }

    exit 1
}

Write-Host 'Clean Squad customization validation passed.' -ForegroundColor Green
Write-Host "Validated workflow roster, manifest parity, frontmatter identity, allowlists, handoffs, and protected-worker flags for $($manifest.agents.Count) agents."
