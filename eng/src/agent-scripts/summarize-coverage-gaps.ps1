#!/usr/bin/env pwsh

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$CoverageReportPath,
    [double]$Threshold = 95,
    [int]$UncoveredLinePreview = 5,
    [string[]]$ExcludePaths,
    [switch]$EmitTasks,
    [string]$RepoRoot
)

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

function Get-RelativePath {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$TargetPath
    )

    try {
        if ([string]::IsNullOrWhiteSpace($TargetPath)) { return $TargetPath }
        return [System.IO.Path]::GetRelativePath($BasePath, $TargetPath)
    }
    catch {
        return $TargetPath
    }
}

function Resolve-LatestCoverageReport {
    param([string]$RepoRoot)

    $searchRoot = Join-Path $RepoRoot '.scratchpad/coverage-test-results'
    if (-not (Test-Path -LiteralPath $searchRoot)) { return $null }

    $reports = Get-ChildItem -Path $searchRoot -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    return $reports | Select-Object -First 1
}

function Get-CoverageReportData {
    param(
        [Parameter(Mandatory)][string]$ReportPath,
        [Parameter(Mandatory)][string]$RepoRoot,
        [string[]]$ExcludePaths,
        [int]$UncoveredLinePreview
    )

    [xml]$xml = Get-Content -LiteralPath $ReportPath
    $classNodes = $xml.SelectNodes('//class')
    if (-not $classNodes) { return @() }

    $fileMap = @{}

    foreach ($classNode in $classNodes) {
        $fileName = [string]$classNode.filename
        if ([string]::IsNullOrWhiteSpace($fileName)) { continue }

        if ($ExcludePaths) {
            $shouldExclude = $false
            foreach ($pattern in $ExcludePaths) {
                if ($fileName -like $pattern) { $shouldExclude = $true; break }
            }
            if ($shouldExclude) { continue }
        }

        if (-not $fileMap.ContainsKey($fileName)) {
            $fileMap[$fileName] = [ordered]@{
                Lines    = @{}
                Classes  = @()
            }
        }

        $entry = $fileMap[$fileName]
        $entry.Classes += [string]$classNode.name

        $lineNodes = $classNode.SelectNodes('lines/line')
        if (-not $lineNodes) { continue }

        foreach ($line in $lineNodes) {
            $lineNumber = [int]$line.number
            $hits = [int]$line.hits
            if ($lineNumber -le 0) { continue }

            if (-not $entry.Lines.ContainsKey($lineNumber)) {
                $entry.Lines[$lineNumber] = $hits
            }
            else {
                if ($hits -gt $entry.Lines[$lineNumber]) {
                    $entry.Lines[$lineNumber] = $hits
                }
            }
        }
    }

    $results = @()
    foreach ($kvp in $fileMap.GetEnumerator()) {
        $fileName = $kvp.Key
        $info = $kvp.Value
    $lineEntries = @($info.Lines.GetEnumerator() | ForEach-Object { $_ })
    $lineCount = ($lineEntries | Measure-Object).Count
    if ($lineCount -eq 0) { continue }

    $total = $lineCount
    $covered = ($lineEntries | Where-Object { $_.Value -gt 0 } | Measure-Object).Count
        $coveragePercent = if ($total -gt 0) { [Math]::Round(($covered / $total) * 100, 2) } else { 100 }

    $uncovered = $lineEntries | Where-Object { $_.Value -eq 0 } | ForEach-Object { [int]$_.Key } | Sort-Object
        $preview = if ($UncoveredLinePreview -gt 0) { $uncovered | Select-Object -First $UncoveredLinePreview } else { @() }

        $fullPath = $null
        if (Test-Path -LiteralPath $fileName) {
            $fullPath = (Resolve-Path -LiteralPath $fileName).Path
        }
        else {
            $candidate = Join-Path $RepoRoot $fileName
            if (Test-Path -LiteralPath $candidate) { $fullPath = (Resolve-Path -LiteralPath $candidate).Path }
        }

        $relative = if ($fullPath) { Get-RelativePath -BasePath $RepoRoot -TargetPath $fullPath } else { $fileName }

        $results += [pscustomobject]@{
            FilePath        = $fullPath
            RelativePath    = $relative
            CoveragePercent = $coveragePercent
            TotalLines      = $total
            CoveredLines    = $covered
            UncoveredLines  = $uncovered
            PreviewLines    = $preview
            ClassNames      = $info.Classes
        }
    }

    return $results
}

function Resolve-ProjectNameFromPath {
    param([string]$RelativePath)
    if ([string]::IsNullOrWhiteSpace($RelativePath)) { return $null }
    $parts = $RelativePath -split '[\\/]'
    $srcIndex = [Array]::IndexOf($parts, 'src')
    if ($srcIndex -ge 0 -and $parts.Length -gt ($srcIndex + 1)) { return $parts[$srcIndex + 1] }
    return $null
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$defaultRepoRoot = Resolve-RepoRoot -StartPath $scriptRoot
$effectiveRepoRoot = if ($RepoRoot) {
    (Resolve-Path -LiteralPath $RepoRoot).Path
} else {
    $defaultRepoRoot
}

$taskModulePath = Join-Path $scriptRoot 'TaskAutomation.psm1'
if (-not (Test-Path -LiteralPath $taskModulePath -PathType Leaf)) {
    throw "Shared task automation module not found at '$taskModulePath'."
}
Import-Module -Name $taskModulePath -Force

if (-not $CoverageReportPath) {
    $latest = Resolve-LatestCoverageReport -RepoRoot $effectiveRepoRoot
    if (-not $latest) {
        throw "No coverage.cobertura.xml report found under '.scratchpad/coverage-test-results'."
    }
    $CoverageReportPath = $latest.FullName
}

if (-not (Test-Path -LiteralPath $CoverageReportPath -PathType Leaf)) {
    throw "Coverage report not found at '$CoverageReportPath'."
}

Write-Host "Using coverage report: $CoverageReportPath" -ForegroundColor Cyan
Write-Host "Coverage threshold: $Threshold%" -ForegroundColor Cyan

    $coverageItems = Get-CoverageReportData -ReportPath $CoverageReportPath -RepoRoot $effectiveRepoRoot -ExcludePaths $ExcludePaths -UncoveredLinePreview $UncoveredLinePreview

if (-not $coverageItems -or $coverageItems.Count -eq 0) {
    Write-Host 'No coverage data found in report.' -ForegroundColor Yellow
    return
}

$belowThreshold = $coverageItems | Where-Object { $_.CoveragePercent -lt $Threshold } | Sort-Object CoveragePercent

if ($belowThreshold.Count -eq 0) {
    Write-Host 'All files meet or exceed the coverage threshold.' -ForegroundColor Green
}
else {
    Write-Host "Files below coverage threshold:" -ForegroundColor Yellow
    $table = $belowThreshold | Select-Object @{n='Coverage%';e={$_.CoveragePercent}}, @{n='Lines';e={"$($_.CoveredLines)/$($_.TotalLines)"}}, @{n='File';e={$_.RelativePath}}
    $table | Format-Table -AutoSize | Out-String | Write-Host
}

$summaryRoot = Join-Path $effectiveRepoRoot '.scratchpad/coverage-test-results'
if (-not (Test-Path -LiteralPath $summaryRoot)) {
    New-Item -ItemType Directory -Path $summaryRoot | Out-Null
}
$summaryPath = Join-Path $summaryRoot 'coverage-gaps-summary.json'
$summaryPayload = [ordered]@{
    generatedAt = (Get-Date).ToUniversalTime().ToString('o')
    threshold   = $Threshold
    reportPath  = $CoverageReportPath
    items       = $belowThreshold | ForEach-Object {
        [ordered]@{
            relativePath    = $_.RelativePath
            coveragePercent = $_.CoveragePercent
            totalLines      = $_.TotalLines
            coveredLines    = $_.CoveredLines
            uncoveredLines  = $_.UncoveredLines
            previewLines    = $_.PreviewLines
        }
    }
}
$summaryPayload | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-Host "Coverage gap summary written to $summaryPath" -ForegroundColor Cyan

if ($EmitTasks -and $belowThreshold.Count -gt 0) {
    $taskItems = @()
    foreach ($item in $belowThreshold) {
        $projectName = Resolve-ProjectNameFromPath -RelativePath $item.RelativePath
        $uniqueKey = "coverage|$($item.RelativePath)"
        $notes = "Coverage $($item.CoveragePercent)% (threshold $Threshold%)."
        if ($item.PreviewLines -and $item.PreviewLines.Count -gt 0) {
            $previewText = ($item.PreviewLines -join ', ')
            $notes += " Uncovered lines: $previewText."
        }

        $tags = @('auto','coverage','below-threshold')
        if ($projectName) { $tags += "project:$projectName" }

        $relatedFiles = @()
        if (-not [string]::IsNullOrWhiteSpace($item.RelativePath)) { $relatedFiles += $item.RelativePath }

        $additional = [ordered]@{
            coverage = [ordered]@{
                coveragePercent = $item.CoveragePercent
                threshold       = $Threshold
                totalLines      = $item.TotalLines
                coveredLines    = $item.CoveredLines
                uncoveredLines  = $item.UncoveredLines
            }
        }

        $slugComponents = @()
        if ($projectName) { $slugComponents += $projectName }
        $slugComponents += 'coverage'
        if ($item.RelativePath) { $slugComponents += $item.RelativePath }

        $taskItems += New-TaskItem -Category 'coverage' -UniqueKey $uniqueKey -KeyPropertyNames @('autoTaskKey','coverageKey') -Title "Raise coverage for $($item.RelativePath)" -Priority 'P2' -Notes $notes -Tags $tags -RelatedFiles $relatedFiles -References @('.github/instructions/testing.instructions.md') -EffortPoints 2 -AdditionalProperties $additional -SlugComponents $slugComponents
    }

    $shouldProcess = $PSCmdlet.ShouldProcess('scratchpad pending tasks', 'Create coverage improvement tasks')
    $syncParams = @{
        Tasks    = $taskItems
        RepoRoot = $effectiveRepoRoot
    }
    if (-not $shouldProcess) { $syncParams['WhatIf'] = $true }

    $results = Sync-AutoTasks @syncParams
    foreach ($result in $results) {
        switch ($result.Status) {
            'Created' { Write-Host "Created coverage task: $($result.FilePath)" -ForegroundColor Cyan }
            'Skipped' { Write-Host "Coverage task already exists for key '$($result.Task.UniqueKey)'; skipping." -ForegroundColor DarkGray }
            Default   { Write-Host "Coverage task $($result.Task.UniqueKey) status $($result.Status)" -ForegroundColor DarkGray }
        }
    }
}