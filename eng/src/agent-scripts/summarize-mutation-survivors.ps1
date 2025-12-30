[CmdletBinding()]
param(
    [switch]$SkipMutationRun,
    [string]$MutationScriptPath,
    [int]$Top,
    [int]$ContextLines = 3,
    [ValidateSet('Simple','Weighted')]
    [string]$ScoringMode = 'Weighted',
    [switch]$VerboseRanking,
    [switch]$GenerateTasks,
    [string]$TasksPath,
    [switch]$EmitTestSkeletons,
    [switch]$OverwriteSkeletons,
    [string]$Project
)

$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'RepositoryAutomation.psm1'
Import-Module -Name $modulePath -Force



function Get-RelativePath
{
    param(
        [Parameter(Mandatory)]
        [string]$BasePath,

        [Parameter(Mandatory)]
        [string]$TargetPath
    )

    try
    {
        if ([string]::IsNullOrWhiteSpace($TargetPath))
        {
            return $TargetPath
        }

        return [System.IO.Path]::GetRelativePath($BasePath, $TargetPath)
    }
    catch
    {
        return $TargetPath
    }
}

function Get-Snippet
{
    param(
        [Parameter(Mandatory)][string]$File,
        [Parameter(Mandatory)][int]$StartLine,
        [Parameter(Mandatory)][int]$EndLine,
        [Parameter(Mandatory)][int]$ContextLines
    )

    if (-not (Test-Path -Path $File -PathType Leaf)) { return $null }
    try {
        $all = Get-Content -Path $File -ErrorAction Stop
        $from = [Math]::Max(1, $StartLine - $ContextLines)
        $to = [Math]::Min($all.Count, $EndLine + $ContextLines)
        $range = $all[($from-1)..($to-1)]
        $snippet = $range -join [Environment]::NewLine
        # Trim very large snippets to avoid bloat
        if ($snippet.Length -gt 1200) { $snippet = $snippet.Substring(0, 1200) + ' ...<truncated>' }
        return $snippet
    }
    catch { return $null }
}

function Get-MutatorWeight
{
    param([string]$Mutator)
    # Tunable weights – adjust as experience dictates
    $weights = @{ 
        'ConditionalBoundary' = 3
        'ArithmeticOperator'  = 2
        'LogicalOperator'     = 2
        'NegateCondition'     = 1.5
        'Boolean'             = 1
        'String'              = 1
    }
    return ($weights[$Mutator] | ForEach-Object { $_ }) ?? 1
}

function Get-MutatorSuggestion
{
    param([string]$Mutator)
    switch ($Mutator) {
        'ConditionalBoundary' { 'Add edge-case test hitting boundary conditions (==, <=, >=).' ; break }
        'ArithmeticOperator'  { 'Strengthen numeric assertions; include negative/zero/boundary inputs.' ; break }
        'LogicalOperator'     { 'Add permutations covering each clause of the logical expression.' ; break }
        'NegateCondition'     { 'Add a test where the condition evaluates the opposite way.' ; break }
        'Boolean'             { 'Assert both true and false behavioral outcomes.' ; break }
        'String'              { 'Add test asserting full string content / variant case.' ; break }
        default               { 'Add or strengthen assertions that differentiate mutated behavior.' }
    }
}

function New-SurvivorKey
{
    param(
        [string]$File,
        [string]$Mutator,
        [int]$StartLine,
        [int]$EndLine,
        [string]$Replacement,
        [int]$StartColumn,
        [int]$EndColumn
    )

    $fileKey = $File ?? ''
    $mutatorKey = $Mutator ?? ''
    $replacementKey = $Replacement ?? ''
    return '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f $fileKey, $mutatorKey, $StartLine, $EndLine, $replacementKey, $StartColumn, $EndColumn
}

function Get-LatestMutationReportPath
{
    param([string]$MutationOutputPath)

    if (-not (Test-Path -Path $MutationOutputPath -PathType Container)) { return $null }

    $candidateDirs = Get-ChildItem -Path $MutationOutputPath -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    foreach ($dir in $candidateDirs)
    {
        $reportPath = Join-Path $dir.FullName 'reports/mutation-report.json'
        if (Test-Path -Path $reportPath -PathType Leaf) { return $reportPath }
    }

    return $null
}

function Get-MutationReportSurvivors
{
    param(
        [Parameter(Mandatory)][string]$ReportPath,
        [Parameter(Mandatory)][string]$RepoRoot
    )

    $content = Get-Content -Path $ReportPath -Raw -ErrorAction Stop
    if ([string]::IsNullOrWhiteSpace($content)) { return @() }

    $report = $content | ConvertFrom-Json
    if ($null -eq $report -or $null -eq $report.files) { return @() }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($fileProp in $report.files.PSObject.Properties)
    {
        $fullPath = $fileProp.Name
        $fileData = $fileProp.Value
        if ($null -eq $fileData -or $null -eq $fileData.mutants) { continue }

    foreach ($mutant in $fileData.mutants)
    {
        $mutantProps = $mutant.PSObject.Properties.Name
        $status = if ($mutantProps -contains 'status') { [string]$mutant.status } else { $null }
        if ($status -ne 'Survived') { continue }

        $location = if ($mutantProps -contains 'location') { $mutant.location } else { $null }
        $startLine = if ($location -and $location.start -and ($location.start.PSObject.Properties.Name -contains 'line')) { [int]$location.start.line } else { 0 }
        $endLine = if ($location -and $location.end -and ($location.end.PSObject.Properties.Name -contains 'line')) { [int]$location.end.line } else { $startLine }
        $startColumn = if ($location -and $location.start -and ($location.start.PSObject.Properties.Name -contains 'column')) { [int]$location.start.column } else { 0 }
        $endColumn = if ($location -and $location.end -and ($location.end.PSObject.Properties.Name -contains 'column')) { [int]$location.end.column } else { 0 }

        $mutatorName = if ($mutantProps -contains 'mutatorName') { [string]$mutant.mutatorName } elseif ($mutantProps -contains 'mutator') { [string]$mutant.mutator } else { $null }
        $replacement = if ($mutantProps -contains 'replacement') { [string]$mutant.replacement } else { $null }
        $statusReason = if ($mutantProps -contains 'statusReason') { $mutant.statusReason } else { $null }
        $coveredBy = if ($mutantProps -contains 'coveredBy') { $mutant.coveredBy } else { $null }
        $killedBy = if ($mutantProps -contains 'killedBy') { $mutant.killedBy } else { $null }
        $reportId = if ($mutantProps -contains 'id') { $mutant.id } else { $null }

        $results.Add([pscustomobject]@{
            File         = $fullPath
            RelativeFile = if ([string]::IsNullOrWhiteSpace($fullPath)) { '' } else { Get-RelativePath -BasePath $RepoRoot -TargetPath $fullPath }
            Mutator      = $mutatorName
            Replacement  = $replacement
            StartLine    = $startLine
            EndLine      = $endLine
            StartColumn  = $startColumn
            EndColumn    = $endColumn
            Status       = $status
            StatusReason = $statusReason
            CoveredBy    = $coveredBy
            KilledBy     = $killedBy
            ReportId     = $reportId
        }) | Out-Null
    }
    }

    return $results.ToArray()
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Get-RepositoryRoot -StartPath $scriptRoot

$taskModulePath = Join-Path $scriptRoot 'TaskAutomation.psm1'
if (-not (Test-Path -LiteralPath $taskModulePath -PathType Leaf)) {
    throw "Shared task automation module not found at '$taskModulePath'."
}

Import-Module -Name $taskModulePath -Force

if (-not $TasksPath) {
    $TasksPath = Join-Path $repoRoot '.scratchpad/testing/mutation-tasks.md'
}
$testsRoot = Join-Path $repoRoot 'tests'

function Resolve-ProjectNameFromFile {
    param([string]$RelativeFile)
    if ([string]::IsNullOrWhiteSpace($RelativeFile)) { return $null }
    $parts = $RelativeFile -split '[\\/]'
    $srcIndex = [Array]::IndexOf($parts,'src')
    if ($srcIndex -ge 0 -and $parts.Length -gt ($srcIndex+1)) { return $parts[$srcIndex+1] }
    return $null
}

function Resolve-TestProjectPath {
    param([string]$ProjectName)
    if ([string]::IsNullOrWhiteSpace($ProjectName)) { return $null }
    if (-not (Test-Path $testsRoot)) { return $null }
    $candidates = @(
        Get-ChildItem -Path $testsRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "$ProjectName*.Tests" -or $_.Name -eq "$ProjectName.Tests" -or $_.Name -like "$ProjectName.*Tests" }
    )
    if ($candidates.Count -gt 0) { return $candidates[0].FullName }
    return $null
}

function ConvertTo-MutationTaskItem {
    param(
        [Parameter(Mandatory)][psobject]$Survivor
    )

    $mutationKey = New-SurvivorKey -File $Survivor.File -Mutator $Survivor.Mutator -StartLine $Survivor.StartLine -EndLine $Survivor.EndLine -Replacement $Survivor.Replacement -StartColumn $Survivor.StartColumn -EndColumn $Survivor.EndColumn
    $projectName = Resolve-ProjectNameFromFile -RelativeFile $Survivor.RelativeFile
    $lineDisplay = if (($Survivor.StartColumn -ne 0) -or ($Survivor.EndColumn -ne 0)) {
        '{0}:{1}..{2}:{3}' -f $Survivor.StartLine, $Survivor.StartColumn, $Survivor.EndLine, $Survivor.EndColumn
    }
    else {
        '{0}..{1}' -f $Survivor.StartLine, $Survivor.EndLine
    }

    $titleBase = if ([string]::IsNullOrWhiteSpace($Survivor.RelativeFile)) {
        'Kill surviving mutant'
    }
    else {
        "Kill $($Survivor.Mutator) mutant in $($Survivor.RelativeFile)"
    }

    $title = '{0} ({1})' -f $titleBase, $lineDisplay

    $tags = @('auto', 'mutation', 'mutation-survivor')
    if ($Survivor.Mutator) { $tags += "mutator:$($Survivor.Mutator)" }
    if ($projectName) { $tags += "project:$projectName" }

    $relatedFiles = @()
    if (-not [string]::IsNullOrWhiteSpace($Survivor.RelativeFile)) {
        $relatedFiles += $Survivor.RelativeFile
    }

    $mutationDetails = [ordered]@{
        score       = $Survivor.Score
        rank        = $Survivor.Rank
        mutator     = $Survivor.Mutator
        file        = $Survivor.RelativeFile
        startLine   = $Survivor.StartLine
        endLine     = $Survivor.EndLine
        startColumn = $Survivor.StartColumn
        endColumn   = $Survivor.EndColumn
        suggestion  = $Survivor.Suggestion
        reportId    = $Survivor.ReportId
    }

    $notes = if ([string]::IsNullOrWhiteSpace($Survivor.Suggestion)) {
        'Review surviving mutant and strengthen assertions.'
    }
    else {
        "Suggestion: $($Survivor.Suggestion)"
    }

    $slugComponents = @()
    if ($projectName) { $slugComponents += $projectName }
    if ($Survivor.Mutator) { $slugComponents += $Survivor.Mutator }
    if ($Survivor.RelativeFile) { $slugComponents += $Survivor.RelativeFile }
    $slugComponents += "line-$($Survivor.StartLine)"

    $additional = [ordered]@{
        mutationKey = $mutationKey
        mutation    = $mutationDetails
    }

    return New-TaskItem -Category 'mutation' -UniqueKey $mutationKey -KeyPropertyNames @('autoTaskKey','mutationKey') -Title $title -Priority 'P2' -Notes $notes -Tags $tags -RelatedFiles $relatedFiles -References @('.github/instructions/mutation-testing.instructions.md') -EffortPoints 2 -AdditionalProperties $additional -SlugComponents $slugComponents
}

function ConvertTo-SanitizedIdentifier {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return 'MutationCase' }
    $clean = ($Value -replace '[^A-Za-z0-9_]','_')
    if ($clean -match '^[0-9]') { $clean = '_' + $clean }
    return $clean
}


if ([string]::IsNullOrWhiteSpace($MutationScriptPath))
{
    $MutationScriptPath = Join-Path $scriptRoot 'mutation-test-mississippi-solution.ps1'
}
else
{
    if (-not (Test-Path -Path $MutationScriptPath -PathType Leaf))
    {
        throw "Mutation script path '$MutationScriptPath' was not found."
    }
}

if (-not $SkipMutationRun)
{
    Write-Host "Running mutation tests via '$MutationScriptPath'..." -ForegroundColor Cyan
    & pwsh -NoLogo -NoProfile -File $MutationScriptPath
    if ($LASTEXITCODE -ne 0)
    {
        throw "Mutation test script exited with code $LASTEXITCODE."
    }
    Write-Host "Mutation tests completed." -ForegroundColor Green
}
else
{
    Write-Host "SkipMutationRun specified; using existing Stryker output." -ForegroundColor Yellow
}

$mutationOutputDirectory = Join-Path $repoRoot '.scratchpad/mutation-test-results'
if (-not (Test-Path -LiteralPath $mutationOutputDirectory)) { New-Item -ItemType Directory -Path $mutationOutputDirectory | Out-Null }
$latestSurvivorsPath = Join-Path $mutationOutputDirectory 'latest-survivors.json'
if (-not (Test-Path -Path $latestSurvivorsPath -PathType Leaf))
{
    Write-Warning "No latest-survivors.json file found at '$latestSurvivorsPath'. Writing empty summaries."
    $survivorItems = @()
}
else
{
    $rawContent = Get-Content -Path $latestSurvivorsPath -Raw
    if ([string]::IsNullOrWhiteSpace($rawContent))
    {
        $survivorItems = @()
    }
    else
    {
        $parsed = $rawContent | ConvertFrom-Json
        if ($null -eq $parsed)
        {
            $survivorItems = @()
        }
        elseif ($parsed -is [System.Array])
        {
            $survivorItems = $parsed
        }
        else
        {
            $survivorItems = @($parsed)
        }
    }
}

$mutationReportPath = Get-LatestMutationReportPath -MutationOutputPath $mutationOutputDirectory
$reportSurvivors = @()
if ($mutationReportPath)
{
    try
    {
        $reportSurvivors = Get-MutationReportSurvivors -ReportPath $mutationReportPath -RepoRoot $repoRoot
        Write-Host "Loaded mutation report survivors from '$mutationReportPath'." -ForegroundColor Cyan
    }
    catch
    {
        Write-Warning "Failed to parse mutation-report.json at '$mutationReportPath': $($_.Exception.Message)"
        $reportSurvivors = @()
    }
}
else
{
    Write-Host 'No mutation-report.json found; skipping detailed survivor extraction.' -ForegroundColor Yellow
}

$reportSurvivors = @($reportSurvivors | Where-Object { $_ })

$reportMap = @{}
foreach ($reportEntry in $reportSurvivors)
{
    $key = New-SurvivorKey -File $reportEntry.File -Mutator $reportEntry.Mutator -StartLine $reportEntry.StartLine -EndLine $reportEntry.EndLine -Replacement $reportEntry.Replacement -StartColumn $reportEntry.StartColumn -EndColumn $reportEntry.EndColumn
    $reportMap[$key] = $reportEntry
}

$normalizedList = New-Object System.Collections.Generic.List[object]
$existingKeys = New-Object System.Collections.Generic.HashSet[string]

foreach ($item in $survivorItems)
{
    $filePath = $item.File
    $className = if ([string]::IsNullOrWhiteSpace($filePath)) { '' } else { [System.IO.Path]::GetFileNameWithoutExtension($filePath) }
    $startColumn = if ($item.PSObject.Properties.Name -contains 'StartColumn') { [int]$item.StartColumn } else { 0 }
    $endColumn = if ($item.PSObject.Properties.Name -contains 'EndColumn') { [int]$item.EndColumn } else { 0 }
    $key = New-SurvivorKey -File $filePath -Mutator $item.Mutator -StartLine $item.StartLine -EndLine $item.EndLine -Replacement $item.Replacement -StartColumn $startColumn -EndColumn $endColumn
    [void]$existingKeys.Add($key)
    $reportData = if ($reportMap.ContainsKey($key)) { $reportMap[$key] } else { $null }

    $normalizedList.Add([pscustomobject]@{
            File         = $filePath
        RelativeFile = if ([string]::IsNullOrWhiteSpace($filePath)) { '' } else { Get-RelativePath -BasePath $repoRoot -TargetPath $filePath }
            ClassName    = $className
            Mutator      = $item.Mutator
            Description  = $item.Description
            Replacement  = $item.Replacement
            StartLine    = $item.StartLine
            EndLine      = $item.EndLine
            StartColumn  = $startColumn
            EndColumn    = $endColumn
            Symbol       = $item.Symbol
            Tests        = $item.Tests
            Status       = if ($item.PSObject.Properties.Name -contains 'Status') { $item.Status } elseif ($reportData) { $reportData.Status } else { 'Unknown' }
            StatusReason = if ($item.PSObject.Properties.Name -contains 'StatusReason') { $item.StatusReason } elseif ($reportData) { $reportData.StatusReason } else { $null }
            CoveredBy    = if ($reportData) { $reportData.CoveredBy } else { $null }
            KilledBy     = if ($reportData) { $reportData.KilledBy } else { $null }
            ReportId     = if ($reportData) { $reportData.ReportId } else { $null }
        }) | Out-Null
}

if ($reportSurvivors.Count -gt 0)
{
    foreach ($reportEntry in $reportSurvivors)
    {
        $key = New-SurvivorKey -File $reportEntry.File -Mutator $reportEntry.Mutator -StartLine $reportEntry.StartLine -EndLine $reportEntry.EndLine -Replacement $reportEntry.Replacement -StartColumn $reportEntry.StartColumn -EndColumn $reportEntry.EndColumn
        if (-not $existingKeys.Contains($key))
        {
            $className = if ([string]::IsNullOrWhiteSpace($reportEntry.File)) { '' } else { [System.IO.Path]::GetFileNameWithoutExtension($reportEntry.File) }
            $normalizedList.Add([pscustomobject]@{
                    File         = $reportEntry.File
                RelativeFile = $reportEntry.RelativeFile
                    ClassName    = $className
                    Mutator      = $reportEntry.Mutator
                    Description  = $null
                    Replacement  = $reportEntry.Replacement
                    StartLine    = $reportEntry.StartLine
                    EndLine      = $reportEntry.EndLine
                    StartColumn  = $reportEntry.StartColumn
                    EndColumn    = $reportEntry.EndColumn
                    Symbol       = $null
                    Tests        = ''
                    Status       = $reportEntry.Status
                    StatusReason = $reportEntry.StatusReason
                    CoveredBy    = $reportEntry.CoveredBy
                    KilledBy     = $reportEntry.KilledBy
                    ReportId     = $reportEntry.ReportId
                }) | Out-Null
        }
    }
}

if (($normalizedList.Count -eq 0) -and ($reportSurvivors.Count -gt 0))
{
    Write-Warning 'latest-survivors.json was empty, but mutation-report.json contained survivors; using report data only.'
}

$normalized = $normalizedList.ToArray()

# --- Scoring & Ranking (Phase 1) -----------------------------------------------------------

$fileGroups = @($normalized | Group-Object File)
$maxFileCount = if ($fileGroups.Count -gt 0) { ($fileGroups | Measure-Object Count -Maximum).Maximum } else { 0 }

$rankWorking = New-Object System.Collections.Generic.List[object]

foreach ($entry in $normalized) {
    $fileCount = ($fileGroups | Where-Object { $_.Name -eq $entry.File } | Measure-Object).Count
    $base = 1
    $mutatorWeight = if ($ScoringMode -eq 'Weighted') { Get-MutatorWeight -Mutator $entry.Mutator } else { 0 }
    $densityWeight = if ($ScoringMode -eq 'Weighted' -and $maxFileCount -gt 0) { [Math]::Round(($fileCount / $maxFileCount) * 2,2) } else { 0 }
    $missingTestsWeight = if ($ScoringMode -eq 'Weighted' -and ([string]::IsNullOrWhiteSpace($entry.Tests))) { 2 } else { 0 }
    $score = [double]($base + $mutatorWeight + $densityWeight + $missingTestsWeight)
    $suggestion = Get-MutatorSuggestion -Mutator $entry.Mutator
    $rankWorking.Add([pscustomobject]@{
            File              = $entry.File
            RelativeFile      = $entry.RelativeFile
            ClassName         = $entry.ClassName
            Mutator           = $entry.Mutator
        Replacement       = $entry.Replacement
            StartLine         = $entry.StartLine
            EndLine           = $entry.EndLine
            StartColumn       = $entry.StartColumn
            EndColumn         = $entry.EndColumn
            Symbol            = $entry.Symbol
            Tests             = $entry.Tests
            Suggestion        = $suggestion
            Score             = [Math]::Round($score,2)
            ScoreBreakdown    = [pscustomobject]@{
                Base        = $base
                Mutator     = $mutatorWeight
                Density     = $densityWeight
                MissingTests= $missingTestsWeight
            }
            Snippet           = Get-Snippet -File $entry.File -StartLine $entry.StartLine -EndLine $entry.EndLine -ContextLines $ContextLines
            Status            = $entry.Status
            StatusReason      = $entry.StatusReason
            CoveredBy         = $entry.CoveredBy
            KilledBy          = $entry.KilledBy
            ReportId          = $entry.ReportId
        })
}

$ordered = $rankWorking | Sort-Object -Property @{
        Expression  = 'Score'
        Descending  = $true
    }, @{
        Expression  = 'File'
        Descending  = $false
    }, @{
        Expression  = 'StartLine'
        Descending  = $false
    }
if ($Top -gt 0) { $focusSelection = $ordered | Select-Object -First $Top } else { $focusSelection = $ordered }

# Optional project filter
if ($Project) {
    $focusSelection = $focusSelection | Where-Object { (Resolve-ProjectNameFromFile -RelativeFile $_.RelativeFile) -eq $Project }
}

# Add Rank property
$rank = 1
foreach ($f in $focusSelection) { Add-Member -InputObject $f -NotePropertyName Rank -NotePropertyValue $rank -Force; $rank++ }

# Summary aggregates
$byMutator = $normalized | Group-Object Mutator | Sort-Object Count -Descending | ForEach-Object { [pscustomobject]@{ Mutator=$_.Name; Count=$_.Count } }
$byFile = $fileGroups | Sort-Object Count -Descending | ForEach-Object { [pscustomobject]@{ File=$_.Name; Count=$_.Count; RelativeFile = if ([string]::IsNullOrWhiteSpace($_.Name)) { '' } else { Get-RelativePath -BasePath $repoRoot -TargetPath $_.Name } } }

$enriched = [pscustomobject]@{
    schemaVersion = '1.1.0'
    generatedAt   = (Get-Date -Format o)
    scoringMode   = $ScoringMode
    totalSurvivors= $normalized.Count
    aggregates    = [pscustomobject]@{
        byMutator = $byMutator
        byFile    = $byFile
    }
    focusOrder    = $focusSelection
    survivors     = $normalized
    report        = if ($mutationReportPath -and ($reportSurvivors.Count -gt 0)) { [pscustomobject]@{ path = $mutationReportPath; survivors = $reportSurvivors } } else { $null }
}

$enrichedJsonPath = Join-Path $mutationOutputDirectory 'mutation-survivors-enriched.json'
$enriched | ConvertTo-Json -Depth 6 | Set-Content -Path $enrichedJsonPath -Encoding UTF8

$summaryJsonPath = Join-Path $mutationOutputDirectory 'mutation-survivors-summary.json'
$summaryMarkdownPath = Join-Path $repoRoot '.scratchpad/testing/mutation-survivors-summary.md'

$null = New-Item -Path (Split-Path -Parent $summaryJsonPath) -ItemType Directory -Force
$null = New-Item -Path (Split-Path -Parent $summaryMarkdownPath) -ItemType Directory -Force

# Maintain backward compatibility: original summary file still emits basic array
$normalized | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryJsonPath -Encoding UTF8

$reportJsonPath = Join-Path $mutationOutputDirectory 'mutation-survivors-report.json'
if ($reportSurvivors.Count -gt 0)
{
    $reportSurvivors | ConvertTo-Json -Depth 4 | Set-Content -Path $reportJsonPath -Encoding UTF8
}
elseif (Test-Path -Path $reportJsonPath)
{
    Remove-Item -Path $reportJsonPath -ErrorAction SilentlyContinue
}

$totalCount = $normalized.Count
$grouped = $normalized | Group-Object File | Sort-Object Count -Descending

$markdown = New-Object System.Text.StringBuilder
[void]$markdown.AppendLine("# Mutation Survivors Summary")
[void]$markdown.AppendLine()
[void]$markdown.AppendLine("Generated on $(Get-Date -Format o)")
[void]$markdown.AppendLine()
[void]$markdown.AppendLine("## Totals")
[void]$markdown.AppendLine("- Survivors: $totalCount")
[void]$markdown.AppendLine()
[void]$markdown.AppendLine("## Survivors by File")
[void]$markdown.AppendLine("| Class Name | Survivors | Path |")
[void]$markdown.AppendLine("| --- | ---: | --- |")

foreach ($group in $grouped)
{
        $className = if ([string]::IsNullOrWhiteSpace($group.Name)) { '(Unknown)' } else { [System.IO.Path]::GetFileNameWithoutExtension($group.Name) }
        $relativePath = if ([string]::IsNullOrWhiteSpace($group.Name)) { string.Empty } else { Get-RelativePath -BasePath $repoRoot -TargetPath $group.Name }
        $escapedPath = ($relativePath -replace '\|', '\\|')
        if ([string]::IsNullOrWhiteSpace($escapedPath) -and -not [string]::IsNullOrWhiteSpace($group.Name))
        {
            $escapedPath = ($group.Name -replace '\|', '\\|')
        }
        $displayPath = if ([string]::IsNullOrWhiteSpace($escapedPath)) { '(unknown)' } else { $escapedPath }
        [void]$markdown.AppendLine("| $className | $($group.Count) | <code>$displayPath</code> |")
}

if ($totalCount -gt 0)
{
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("## Prioritized Focus Order" + ($(if($Top -gt 0){" (Top $Top)"} else {''})))
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("| Rank | Score | Mutator | File | Lines | Suggestion |")
    [void]$markdown.AppendLine("| ---: | ----: | ------- | ---- | ----- | ---------- |")
    foreach ($f in $focusSelection) {
        $dispFile = if ([string]::IsNullOrWhiteSpace($f.RelativeFile)) { '(unknown)' } else { $f.RelativeFile }
        $escapedFile = ($dispFile -replace '\|','\\|')
        $lineDisplay = if (($f.StartColumn -ne 0) -or ($f.EndColumn -ne 0)) { "{0}:{1}..{2}:{3}" -f $f.StartLine, $f.StartColumn, $f.EndLine, $f.EndColumn } else { "{0}..{1}" -f $f.StartLine, $f.EndLine }
        [void]$markdown.AppendLine("| $($f.Rank) | $($f.Score) | $($f.Mutator) | <code>$escapedFile</code> | $lineDisplay | $($f.Suggestion) |")
    }
    if ($VerboseRanking) {
        [void]$markdown.AppendLine()
        [void]$markdown.AppendLine("### Scoring Breakdown (Verbose)")
        [void]$markdown.AppendLine()
        foreach ($f in $focusSelection) {
            $lineDisplay = if (($f.StartColumn -ne 0) -or ($f.EndColumn -ne 0)) { "{0}:{1}..{2}:{3}" -f $f.StartLine, $f.StartColumn, $f.EndLine, $f.EndColumn } else { "{0}..{1}" -f $f.StartLine, $f.EndLine }
            [void]$markdown.AppendLine("- Rank $($f.Rank) Score $($f.Score): Base=$($f.ScoreBreakdown.Base), Mutator=$($f.ScoreBreakdown.Mutator), Density=$($f.ScoreBreakdown.Density), MissingTests=$($f.ScoreBreakdown.MissingTests) — $($f.Mutator) in <code>$($f.RelativeFile)</code> ($lineDisplay)")
        }
    }

    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("## Survivor Details")
    [void]$markdown.AppendLine()
    foreach ($entry in $normalized)
    {
        $relativeFile = if ([string]::IsNullOrWhiteSpace($entry.File)) { string.Empty } else { Get-RelativePath -BasePath $repoRoot -TargetPath $entry.File }
        $escapedFile = ($relativeFile -replace '\|', '\\|')
        if ([string]::IsNullOrWhiteSpace($escapedFile) -and -not [string]::IsNullOrWhiteSpace($entry.File))
        {
            $escapedFile = ($entry.File -replace '\|', '\\|')
        }
        $displayFile = if ([string]::IsNullOrWhiteSpace($escapedFile)) { '(unknown)' } else { $escapedFile }
        [void]$markdown.AppendLine("- **Class:** $($entry.ClassName)  ")
        [void]$markdown.AppendLine("  **File:** <code>$displayFile</code>  ")
        [void]$markdown.AppendLine("  **Mutator:** $($entry.Mutator)  ")
        if ($entry.Description)
        {
            [void]$markdown.AppendLine("  **Description:** $($entry.Description)  ")
        }
        if ($entry.Replacement)
        {
            [void]$markdown.AppendLine("  **Replacement:** ``$($entry.Replacement)``  ")
        }
        $lineDisplay = if (($entry.StartColumn -ne 0) -or ($entry.EndColumn -ne 0)) { "{0}:{1}..{2}:{3}" -f $entry.StartLine, $entry.StartColumn, $entry.EndLine, $entry.EndColumn } else { "{0}..{1}" -f $entry.StartLine, $entry.EndLine }
        [void]$markdown.AppendLine("  **Lines:** $lineDisplay  ")
        if ($entry.Status)
        {
            [void]$markdown.AppendLine("  **Status:** $($entry.Status)  ")
        }
        if ($entry.StatusReason)
        {
            [void]$markdown.AppendLine("  **Status Reason:** $($entry.StatusReason)  ")
        }
        if ($entry.Symbol)
        {
            [void]$markdown.AppendLine("  **Symbol:** $($entry.Symbol)  ")
        }
        if ($entry.Tests)
        {
            [void]$markdown.AppendLine("  **Tests:** $($entry.Tests)  ")
        }
        if ($entry.CoveredBy)
        {
            $coveredDisplay = (@($entry.CoveredBy) -join ', ')
            [void]$markdown.AppendLine("  **Covered By:** $coveredDisplay  ")
        }
        $killedBy = @($entry.KilledBy) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        $killedBy = @($killedBy)
        if ($killedBy.Count -gt 0)
        {
            $killedDisplay = ($killedBy -join ', ')
            [void]$markdown.AppendLine("  **Killed By:** $killedDisplay  ")
        }
        if ($entry.ReportId)
        {
            [void]$markdown.AppendLine("  **Report Id:** $($entry.ReportId)  ")
        }
        [void]$markdown.AppendLine()
    }
}
else
{
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine("🎉 No surviving mutants were reported in the latest run.")
}

# Mutator guidance cheat sheet (always included for consistency if survivors exist)
if ($totalCount -gt 0) {
    [void]$markdown.AppendLine('## Mutator Strategy Cheat Sheet')
    [void]$markdown.AppendLine()
    [void]$markdown.AppendLine('| Mutator | Indicates | Typical Fix |')
    [void]$markdown.AppendLine('| ------- | --------- | ----------- |')
    $cheat = @(
        @{ M='ConditionalBoundary'; I='Missing edge / boundary case'; F='Add boundary input(s) asserting both sides' },
        @{ M='ArithmeticOperator'; I='Loose numeric assertion'; F='Assert exact value; add negative/zero/boundary inputs' },
        @{ M='LogicalOperator'; I='Not all logical clauses covered'; F='Add tests for each clause combination' },
        @{ M='NegateCondition'; I='Only one branch exercised'; F='Add test where predicate is false' },
        @{ M='Boolean'; I='True/false outcomes unbalanced'; F='Add opposite outcome test' },
        @{ M='String'; I='String content not asserted'; F='Assert full string / variant differences' },
        @{ M='Assignment'; I='State change not validated'; F='Assert state after mutation' },
        @{ M='RemoveCall'; I='Call side-effect unverified'; F='Assert side-effect / interaction' }
    )
    foreach ($c in $cheat) { [void]$markdown.AppendLine("| $($c.M) | $($c.I) | $($c.F) |") }
    [void]$markdown.AppendLine()
}

# --- Task file generation (Phase 2) --------------------------------------------------------
if ($GenerateTasks) {
    $taskDir = Split-Path -Parent $TasksPath
    if (-not (Test-Path $taskDir)) { $null = New-Item -Path $taskDir -ItemType Directory -Force }
    $lines = @()
    $lines += '# Mutation Survivor Tasks (Auto-Generated)'
    $lines += ''
    $lines += "Generated: $(Get-Date -Format o)"
    $lines += "ScoringMode: $ScoringMode"
    if ($Top -gt 0) { $lines += "Filtered: Top $Top" }
    if ($Project) { $lines += "Project Filter: $Project" }
    $lines += ''
    $lines += '| Status | Rank | Score | Project | File | Lines | Mutator | Suggestion |'
    $lines += '| ------ | ----:| ----: | ------- | ---- | ----- | ------- | ---------- |'
    foreach ($f in $focusSelection) {
        $proj = Resolve-ProjectNameFromFile -RelativeFile $f.RelativeFile
        $dispFile = if ([string]::IsNullOrWhiteSpace($f.RelativeFile)) { '(unknown)' } else { $f.RelativeFile }
        $lineDisplay = if (($f.StartColumn -ne 0) -or ($f.EndColumn -ne 0)) { "{0}:{1}..{2}:{3}" -f $f.StartLine, $f.StartColumn, $f.EndLine, $f.EndColumn } else { "{0}..{1}" -f $f.StartLine, $f.EndLine }
        $lines += "| Todo | $($f.Rank) | $($f.Score) | $proj | $dispFile | $lineDisplay | $($f.Mutator) | $($f.Suggestion) |"
    }
    $lines += ''
    $lines += 'Legend: Update Status to Done when killed, Deferred with justification if unkillable without code change.'
    Set-Content -Path $TasksPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Tasks file generated: $TasksPath" -ForegroundColor Cyan
}

$focusSelection = @($focusSelection | Where-Object { $_ })

if ($focusSelection.Count -gt 0) {
    $taskItems = @()
    foreach ($survivor in $focusSelection) {
        $taskItems += ConvertTo-MutationTaskItem -Survivor $survivor
    }

    if ($taskItems.Count -gt 0) {
        $syncResults = Sync-AutoTasks -Tasks $taskItems -RepoRoot $repoRoot
        foreach ($result in $syncResults) {
            switch ($result.Status) {
                'Created' {
                    Write-Host "Created scratchpad task: $($result.FilePath)" -ForegroundColor Cyan
                }
                'Skipped' {
                    Write-Host "Scratchpad task already exists for key '$($result.Task.UniqueKey)'; skipping." -ForegroundColor DarkGray
                }
                Default {
                    Write-Host "Task $($result.Task.UniqueKey) status $($result.Status)" -ForegroundColor DarkGray
                }
            }
        }
    }
    else {
        Write-Host 'No mutation survivors to create scratchpad tasks for.' -ForegroundColor DarkGray
    }
}
else {
    Write-Host 'No mutation survivors to create scratchpad tasks for.' -ForegroundColor DarkGray
}

# --- Test skeleton generation (Phase 2) ----------------------------------------------------
if ($EmitTestSkeletons) {
    foreach ($f in $focusSelection) {
        $proj = Resolve-ProjectNameFromFile -RelativeFile $f.RelativeFile
        if (-not $proj) { continue }
        $testProjPath = Resolve-TestProjectPath -ProjectName $proj
        if (-not $testProjPath) { Write-Host "No test project found for $proj; skipping skeleton." -ForegroundColor Yellow; continue }
        $mutationDir = Join-Path $testProjPath 'Mutation'
        if (-not (Test-Path $mutationDir)) { $null = New-Item -Path $mutationDir -ItemType Directory -Force }
        $skeletonFile = Join-Path $mutationDir 'GeneratedMutationTests.cs'
        if (-not (Test-Path $skeletonFile) -or $OverwriteSkeletons) {
            $header = @(
                '// <auto-generated />',
                'using System;',
                'using Xunit;',
                '',
                "namespace Company.$proj.Tests.Mutation", '{',
                '    /// <summary>',
                '    ///     Generated mutation test skeletons. Fill in Arrange/Act/Assert to kill survivors.',
                '    /// </summary>',
                '    public static class GeneratedMutationTests {',
                '    }',
                '}'
            )
            Set-Content -Path $skeletonFile -Value $header -Encoding UTF8
        }
        # Insert methods before final closing braces (very simple append approach)
        $content = Get-Content -Path $skeletonFile -Raw
    $methodName = 'Mutant_' + (ConvertTo-SanitizedIdentifier -Value ("R${($f.Rank)}_${f.ClassName}_${f.StartLine}_${f.Mutator}"))
        if ($content -notmatch [Regex]::Escape($methodName)) {
            $snippetComment = ($f.Snippet -replace [Regex]::Escape('*/'), '*\/')
            $lineDisplay = if (($f.StartColumn -ne 0) -or ($f.EndColumn -ne 0)) { "{0}:{1}..{2}:{3}" -f $f.StartLine, $f.StartColumn, $f.EndLine, $f.EndColumn } else { "{0}..{1}" -f $f.StartLine, $f.EndLine }
            $factDisplay = ('[Fact(DisplayName="{0} {1} lines {2}")]' -f $f.ClassName, $f.Mutator, $lineDisplay)
            $method = @(
                '',
                '        /// <summary>',
                "        ///     Auto-generated mutation survivor skeleton for $($f.ClassName) at $lineDisplay.",
                '        /// </summary>',
                "        $factDisplay",
                "        public static void $methodName()",
                '        {',
                "            // Mutator: $($f.Mutator)",
                "            // Suggestion: $($f.Suggestion)",
                "            // File: $($f.RelativeFile) ($lineDisplay)",
                '            // Snippet:',
                '            /*',
                "            $snippetComment",
                '            */',
                '            // TODO: Implement Arrange/Act/Assert to kill this mutant.',
                '            Assert.True(true);',
                '        }'
            )
            # naive insertion: replace last two lines '    }','}' with methods + those
            $updated = $content -replace "public static class GeneratedMutationTests {", ("public static class GeneratedMutationTests {" + [Environment]::NewLine + ($method -join [Environment]::NewLine))
            Set-Content -Path $skeletonFile -Value $updated -Encoding UTF8
            Write-Host "Added skeleton test $methodName to $skeletonFile" -ForegroundColor Green
        }
    }
}

$markdownContent = $markdown.ToString()
Set-Content -Path $summaryMarkdownPath -Value $markdownContent -Encoding UTF8

Write-Host "Mutation survivor summaries written." -ForegroundColor Cyan
Write-Host "- JSON: $summaryJsonPath" -ForegroundColor Gray
Write-Host "- Enriched JSON: $enrichedJsonPath" -ForegroundColor Gray
if ($reportSurvivors.Count -gt 0)
{
    Write-Host "- Report JSON: $reportJsonPath" -ForegroundColor Gray
}
Write-Host "- Markdown: $summaryMarkdownPath" -ForegroundColor Gray
Write-Host "Total survivors: $totalCount" -ForegroundColor Cyan

return 0


