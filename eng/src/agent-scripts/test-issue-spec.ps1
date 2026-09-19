#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$RepositoryRoot = (Get-Location).Path,

    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredSections = @(
    'Problem',
    'Observable outcome',
    'Scope',
    'Relevant source and contracts',
    'Decisions and non-goals',
    'Dependencies and readiness',
    'Acceptance criteria',
    'Implementation outline',
    'Validation plan',
    'Risks and delivery boundary',
    'Validation evidence map'
)

function Get-MarkdownSections {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $knownTitles = ($requiredSections | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $matches = [regex]::Matches($Content, "(?m)^#{2,3}\s+(?<Title>$knownTitles)\s*$")
    $sections = [ordered]@{}
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $match = $matches[$index]
        $start = $match.Index + $match.Length
        $end = if ($index + 1 -lt $matches.Count) { $matches[$index + 1].Index } else { $Content.Length }
        $title = $match.Groups['Title'].Value.Trim()
        $sections[$title] = $Content.Substring($start, $end - $start).Trim()
    }

    return $sections
}

function Add-IssueSpecError {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$Errors,
        [Parameter(Mandatory)][string]$Message
    )

    $null = $Errors.Add($Message)
}

function Test-RepositoryRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Candidate,
        [Parameter(Mandatory)][string]$Root
    )

    if ([string]::IsNullOrWhiteSpace($Candidate) -or
        [System.IO.Path]::IsPathRooted($Candidate) -or
        $Candidate -match '(^|[\\/])\.\.?([\\/]|$)' -or
        $Candidate -match '^[A-Za-z]:') {
        return $false
    }

    return Test-Path -LiteralPath (Join-Path $Root $Candidate)
}

function Get-IssueSpecResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$IssuePath,
        [Parameter(Mandatory)][string]$RepositoryRoot
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $warnings = [System.Collections.Generic.List[string]]::new()
    $content = Get-Content -LiteralPath $IssuePath -Raw -ErrorAction Stop
    $sections = Get-MarkdownSections -Content $content

    $versionMatch = [regex]::Match($content, '(?im)^\s*Contract version:\s*(?<Value>\d+\.\d+)\s*$')
    if (-not $versionMatch.Success) {
        $versionMatch = [regex]::Match($content, '(?im)^#{2,3}\s+Contract version\s*\r?\n\s*(?<Value>\d+\.\d+)\s*$')
    }
    $version = if ($versionMatch.Success) { $versionMatch.Groups['Value'].Value } else { '' }
    if (-not $versionMatch.Success) {
        Add-IssueSpecError -Errors $errors -Message 'Missing Contract version: major.minor.'
    }
    elseif ($version -ne '1.0') {
        Add-IssueSpecError -Errors $errors -Message "Unsupported contract version '$version'; expected '1.0'."
    }

    foreach ($section in $requiredSections) {
        if (-not $sections.Contains($section)) {
            Add-IssueSpecError -Errors $errors -Message "Missing required section '## $section'."
        }
        elseif ([string]::IsNullOrWhiteSpace([string]$sections[$section])) {
            Add-IssueSpecError -Errors $errors -Message "Required section '## $section' is empty."
        }
    }

    $headingMatches = [regex]::Matches($content, '(?m)^#{2,3}\s+(?<Title>[^\r\n]+)\s*$')
    $previousIndex = -1
    foreach ($section in $requiredSections) {
        $currentIndex = -1
        for ($index = 0; $index -lt $headingMatches.Count; $index++) {
            if ($headingMatches[$index].Groups['Title'].Value.Trim() -eq $section) {
                $currentIndex = $index
                break
            }
        }
        if ($currentIndex -ge 0 -and $previousIndex -ge 0 -and $currentIndex -lt $previousIndex) {
            Add-IssueSpecError -Errors $errors -Message 'Required sections must appear in the contract order.'
            break
        }
        if ($currentIndex -ge 0) { $previousIndex = $currentIndex }
    }

    if ($content -match '(?im)\b(?:TBD|TODO|FIXME)\b\s*(?:\(|\[)?\s*blocking') {
        Add-IssueSpecError -Errors $errors -Message 'Unresolved blocking TBD/TODO marker is not allowed.'
    }
    if ($content -match '(?im)\{\{[^}]+\}\}|^\s*[-*]\s*\[(?:insert|describe|add|todo|tbd)[^\]]*\]') {
        Add-IssueSpecError -Errors $errors -Message 'Template placeholder remains in the issue body.'
    }

    if ($sections.Contains('Relevant source and contracts')) {
        $sourcePaths = [regex]::Matches(
            [string]$sections['Relevant source and contracts'],
            '(?m)^\s*(?:[-*]|\d+\.)\s*`(?<Path>[^`]+)`'
        )
        if ($sourcePaths.Count -eq 0) {
            Add-IssueSpecError -Errors $errors -Message 'Relevant source and contracts must list backtick-wrapped repository-relative paths.'
        }
        foreach ($pathMatch in $sourcePaths) {
            $candidate = $pathMatch.Groups['Path'].Value.Trim()
            if (-not (Test-RepositoryRelativePath -Candidate $candidate -Root $RepositoryRoot)) {
                Add-IssueSpecError -Errors $errors -Message "Referenced repository-relative path does not exist: '$candidate'."
            }
        }
    }

    $acceptanceIds = [System.Collections.Generic.List[string]]::new()
    if ($sections.Contains('Acceptance criteria')) {
        $criteria = [regex]::Matches(
            [string]$sections['Acceptance criteria'],
            '(?im)^\s*(?:[-*]|\d+\.)\s*\[(?<Id>AC\d+)\]\s+(?<Text>.+?)\s*$'
        )
        if ($criteria.Count -eq 0) {
            Add-IssueSpecError -Errors $errors -Message 'Acceptance criteria must contain stable IDs such as [AC1].'
        }
        foreach ($criterion in $criteria) {
            $id = $criterion.Groups['Id'].Value.ToUpperInvariant()
            if ($acceptanceIds.Contains($id)) {
                Add-IssueSpecError -Errors $errors -Message "Duplicate acceptance criterion ID: '$id'."
            }
            else {
                $acceptanceIds.Add($id)
            }
        }
    }

    if ($sections.Contains('Validation evidence map')) {
        $evidence = [string]$sections['Validation evidence map']
        $evidenceIds = [System.Collections.Generic.List[string]]::new()
        $evidenceMatches = [regex]::Matches($evidence, '(?im)^\s*(?:[-*]|\d+\.)\s*\[(?<Id>AC\d+)\]\s+.+$')
        foreach ($evidenceMatch in $evidenceMatches) {
            $evidenceId = $evidenceMatch.Groups['Id'].Value.ToUpperInvariant()
            if ($evidenceIds.Contains($evidenceId)) {
                Add-IssueSpecError -Errors $errors -Message "Duplicate validation evidence mapping ID: '$evidenceId'."
            }
            else {
                $evidenceIds.Add($evidenceId)
            }
        }
        foreach ($id in $acceptanceIds) {
            if (-not $evidenceIds.Contains($id)) {
                Add-IssueSpecError -Errors $errors -Message "Acceptance criterion '$id' has no validation evidence mapping."
            }
        }
        foreach ($evidenceId in $evidenceIds) {
            if (-not $acceptanceIds.Contains($evidenceId)) {
                Add-IssueSpecError -Errors $errors -Message "Validation evidence map contains unknown acceptance criterion ID: '$evidenceId'."
            }
        }
    }

    [pscustomobject]@{
        SchemaVersion = '1.0'
        Valid = $errors.Count -eq 0
        IssuePath = $IssuePath
        RepositoryRoot = $RepositoryRoot
        ContractVersion = $version
        AcceptanceCriteria = @($acceptanceIds)
        Errors = @($errors)
        Warnings = @($warnings)
    }
}

try {
    $resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $resolvedIssuePath = (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
    $result = Get-IssueSpecResult -IssuePath $resolvedIssuePath -RepositoryRoot $resolvedRoot
}
catch {
    $result = [pscustomobject]@{
        SchemaVersion = '1.0'
        Valid = $false
        IssuePath = $Path
        RepositoryRoot = $RepositoryRoot
        ContractVersion = ''
        AcceptanceCriteria = @()
        Errors = @("Unable to inspect issue body: $($_.Exception.Message)")
        Warnings = @()
    }
}

if ($Json) {
    $result | ConvertTo-Json -Depth 5 -Compress
}
else {
    $status = if ($result.Valid) { 'PASS' } else { 'FAIL' }
    Write-Output "ISSUE_SPEC: $status"
    foreach ($error in $result.Errors) { Write-Output "ERROR: $error" }
    foreach ($warning in $result.Warnings) { Write-Output "WARNING: $warning" }
}

if ($result.Valid) { exit 0 }
exit 1
