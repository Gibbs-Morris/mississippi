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
    $allHeadings = [regex]::Matches($Content, '(?m)^(?<Level>#{2,3})\s+(?<Title>[^\r\n]+)\s*$')
    $matches = @($allHeadings | Where-Object { $requiredSections -contains $_.Groups['Title'].Value.Trim() })
    $sections = [ordered]@{}
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $match = $matches[$index]
        $start = $match.Index + $match.Length
        $level = $match.Groups['Level'].Value.Length
        $nextPeer = @($allHeadings | Where-Object { $_.Index -gt $start -and $_.Groups['Level'].Value.Length -le $level } | Select-Object -First 1)
        $end = if ($nextPeer.Count -gt 0) { $nextPeer[0].Index } else { $Content.Length }
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

function Remove-MarkdownFencedBlocks {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $insideFence = $false
    $fenceCharacter = ''
    $lines = foreach ($line in ($Content -split '\r?\n')) {
        if (-not $insideFence -and $line -match '^\s*(?<Fence>`{3,}|~{3,})') {
            $insideFence = $true
            $fenceCharacter = $Matches.Fence.Substring(0, 1)
            ''
            continue
        }
        elseif ($insideFence) { '' }
        else { $line }
        if ($insideFence -and $line -match ('^\s*' + [regex]::Escape($fenceCharacter) + '{3,}\s*$')) {
            $insideFence = $false
        }
    }
    return ($lines -join [Environment]::NewLine)
}

function Remove-MarkdownHtmlComments {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    return [regex]::Replace($Content, '(?s)<!--.*?(?:-->|$)', '')
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
    $structuralContent = Remove-MarkdownHtmlComments -Content (Remove-MarkdownFencedBlocks -Content $content)
    $sections = Get-MarkdownSections -Content $structuralContent

    $versionValues = @()
    $versionValues += @([regex]::Matches($structuralContent, '(?im)^\s*Contract version:\s*(?<Value>\d+\.\d+)\s*$') | ForEach-Object { $_.Groups['Value'].Value })
    $versionValues += @([regex]::Matches($structuralContent, '(?im)^#{2,3}\s+Contract version\s*\r?\n\s*(?<Value>\d+\.\d+)\s*$') | ForEach-Object { $_.Groups['Value'].Value })
    $version = if ($versionValues.Count -gt 0) { $versionValues[0] } else { '' }
    if ($versionValues.Count -eq 0) {
        Add-IssueSpecError -Errors $errors -Message 'Missing Contract version: major.minor.'
    }
    elseif (@($versionValues | Sort-Object -Unique).Count -ne 1) {
        Add-IssueSpecError -Errors $errors -Message 'Contract version is declared more than once with conflicting values.'
    }
    elseif ($versionValues.Count -gt 1) {
        Add-IssueSpecError -Errors $errors -Message 'Contract version must be declared exactly once.'
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

    $headingMatches = [regex]::Matches($structuralContent, '(?m)^#{2,3}\s+(?<Title>[^\r\n]+)\s*$')
    foreach ($group in @($headingMatches | ForEach-Object { $_.Groups['Title'].Value.Trim() } | Group-Object)) {
        if ($group.Count -gt 1 -and $requiredSections -contains $group.Name) {
            Add-IssueSpecError -Errors $errors -Message "Duplicate required section heading: '## $($group.Name)'."
        }
    }
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

    $hasBlockingMarker =
        $structuralContent -match '(?im)\b(?:TBD|TODO|FIXME)\b\s*(?::|[-–—])?\s*(?:\([^)]*blocking[^)]*\)|\[[^]]*blocking[^]]*\]|blocking\b)' -or
        $structuralContent -match '(?im)\bblocking\b\s*[:\-]\s*(?:TBD|TODO|FIXME)\b'
    if ($hasBlockingMarker) {
        Add-IssueSpecError -Errors $errors -Message 'Unresolved blocking TBD/TODO marker is not allowed.'
    }
    if ($structuralContent -match '(?im)\{\{[^}]+\}\}|^\s*[-*]\s*\[(?:insert|describe|add|todo|tbd)[^\]]*\]') {
        Add-IssueSpecError -Errors $errors -Message 'Template placeholder remains in the issue body.'
    }

    if ($sections.Contains('Relevant source and contracts')) {
        $sourcePaths = [regex]::Matches(
            [string]$sections['Relevant source and contracts'],
            '`(?<Path>[^`]+)`'
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
        $evidenceMatches = [regex]::Matches($evidence, '(?im)^\s*(?:[-*]|\d+\.)\s*\[(?<Id>AC\d+)\]\s*(?<Kind>Command|Test|Manual\s+observation)\s*:\s*(?<Evidence>.+?)\s*;\s*expected\s*:\s*(?<Expected>[^\r\n]+?)\s*$')
        foreach ($line in @($evidence -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
            $lineId = [regex]::Match($line, '\[(?<Id>AC\d+)\]').Groups['Id'].Value.ToUpperInvariant()
            if ($lineId -and $line -notmatch '(?im)^\s*(?:[-*]|\d+\.)\s*\[AC\d+\]\s*(?:Command|Test|Manual\s+observation)\s*:\s*.+?\s*;\s*expected\s*:\s*[^\r\n]+?\s*$') {
                Add-IssueSpecError -Errors $errors -Message "Validation evidence entry for '$lineId' must include Command, Test, or Manual observation evidence and an expected result."
            }
        }
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
