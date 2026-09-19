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
    $allHeadings = [regex]::Matches($Content, '(?m)^(?<Level>#{1,6})[ \t]+(?<Title>[^\r\n]+)[ \t]*\r?$')
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
    param(
        [Parameter(Mandatory)][string]$Content,
        [switch]$MaskContent
    )

    $insideFence = $false
    $fenceCharacter = ''
    $fenceLength = 0
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Content -split '\r?\n')) {
        if (-not $insideFence -and $line -match ('^\s*(?<Fence>' + [regex]::Escape([char]96) + '{3,}|~{3,})')) {
            $insideFence = $true
            $fenceCharacter = $Matches.Fence.Substring(0, 1)
            $fenceLength = $Matches.Fence.Length
            $lines.Add('')
            continue
        }
        if ($insideFence -and $line -match ('^[ \t]{0,3}' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}[ \t]*$')) {
            $lines.Add('')
            $insideFence = $false
            continue
        }
        if ($insideFence -and $MaskContent) {
            $lines.Add('')
            continue
        }
        if ($insideFence -and $line -match '^\s*#{1,6}[ \t]+') {
            # Keep rendered commands and prose, but remove structural-looking
            # headings from fenced examples before section discovery.
            $lines.Add('')
            continue
        }
        $lines.Add($line)
    }
    return ($lines -join [Environment]::NewLine)
}

function Remove-MarkdownInlineCode {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $delimiterCharacter = [char]96
    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $Content.Length) {
        if ($Content[$index] -ne $delimiterCharacter) {
            $null = $builder.Append($Content[$index])
            $index++
            continue
        }

        $start = $index
        while ($index -lt $Content.Length -and $Content[$index] -eq $delimiterCharacter) { $index++ }
        $delimiterLength = $index - $start
        $cursor = $index
        $closing = -1
        while ($cursor -lt $Content.Length) {
            if ($Content[$cursor] -ne $delimiterCharacter) {
                $cursor++
                continue
            }
            $runStart = $cursor
            while ($cursor -lt $Content.Length -and $Content[$cursor] -eq $delimiterCharacter) { $cursor++ }
            if ($cursor - $runStart -eq $delimiterLength) {
                $closing = $runStart
                break
            }
        }
        if ($closing -lt 0) {
            $null = $builder.Append($Content.Substring($start, $delimiterLength))
            continue
        }
        $index = $cursor
    }

    return $builder.ToString()
}

function Remove-MarkdownHtmlComments {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $Content.Length) {
        if ($Content[$index] -eq '`') {
            $start = $index
            while ($index -lt $Content.Length -and $Content[$index] -eq '`') { $index++ }
            $delimiterLength = $index - $start
            $cursor = $index
            $closing = -1
            while ($cursor -lt $Content.Length) {
                if ($Content[$cursor] -ne [char]96) {
                    $cursor++
                    continue
                }
                $runStart = $cursor
                while ($cursor -lt $Content.Length -and $Content[$cursor] -eq [char]96) { $cursor++ }
                if ($cursor - $runStart -eq $delimiterLength) {
                    $closing = $runStart
                    break
                }
            }
            if ($closing -ge 0) {
                $null = $builder.Append($Content.Substring($start, $cursor - $start))
                $index = $cursor
                continue
            }
            $null = $builder.Append($Content.Substring($start, $delimiterLength))
            continue
        }
        if ($index + 4 -le $Content.Length -and $Content.Substring($index, 4) -eq '<!--') {
            $closingComment = $Content.IndexOf('-->', $index + 4, [System.StringComparison]::Ordinal)
            $index = if ($closingComment -ge 0) { $closingComment + 3 } else { $Content.Length }
            continue
        }
        $null = $builder.Append($Content[$index])
        $index++
    }
    return $builder.ToString()
}

function Test-RepositoryRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Candidate,
        [Parameter(Mandatory)][string]$Root
    )

    if ([string]::IsNullOrWhiteSpace($Candidate) -or
        [System.IO.Path]::IsPathRooted($Candidate) -or
        $Candidate -match '^[A-Za-z]:') {
        return $false
    }

    try {
        $current = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
        foreach ($segment in ($Candidate.Replace('\', '/') -split '/')) {
            if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.') { continue }
            if ($segment -eq '..') { return $false }
            $child = @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop | Where-Object { $_.Name -ceq $segment } | Select-Object -First 1)
            if ($child.Count -eq 0) { return $false }
            $current = $child[0].FullName
        }
        return $true
    }
    catch {
        return $false
    }
}

function Test-IssueSectionContent {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $withoutHeadings = [regex]::Replace($Content, '(?m)^[ \t]{0,3}#{1,6}[ \t]*[^\r\n]*$', '')
    return -not [string]::IsNullOrWhiteSpace($withoutHeadings)
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
    $nonRenderedContent = Remove-MarkdownHtmlComments -Content (Remove-MarkdownFencedBlocks -Content $content -MaskContent)
    $sections = Get-MarkdownSections -Content $structuralContent

    $firstRequiredSectionIndex = $nonRenderedContent.Length
    foreach ($requiredSection in $requiredSections) {
        $sectionHeading = [regex]::Match($nonRenderedContent, '(?m)^[ \t]{0,3}#{2,3}[ \t]+' + [regex]::Escape($requiredSection) + '[ \t]*\r?$')
        if ($sectionHeading.Success -and $sectionHeading.Index -lt $firstRequiredSectionIndex) {
            $firstRequiredSectionIndex = $sectionHeading.Index
        }
    }
    $prologueContent = $nonRenderedContent.Substring(0, $firstRequiredSectionIndex)
    $prologueVersionValues = @()
    $prologueVersionValues += @([regex]::Matches($prologueContent, '(?im)^[ \t]*Contract version:[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $prologueVersionValues += @([regex]::Matches($prologueContent, '(?im)^#{2,3}[ \t]+Contract version[ \t]*\r?\n(?:[ \t]*\r?\n)*[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $allVersionValues = @()
    $allVersionValues += @([regex]::Matches($nonRenderedContent, '(?im)^[ \t]*Contract version:[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $allVersionValues += @([regex]::Matches($nonRenderedContent, '(?im)^#{2,3}[ \t]+Contract version[ \t]*\r?\n(?:[ \t]*\r?\n)*[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $versionValues = $prologueVersionValues
    $version = if ($versionValues.Count -gt 0) { $versionValues[0] } else { '' }
    if ($versionValues.Count -eq 0) {
        Add-IssueSpecError -Errors $errors -Message 'Missing Contract version: major.minor.'
    }
    elseif (@($allVersionValues | Sort-Object -Unique).Count -ne 1) {
        Add-IssueSpecError -Errors $errors -Message 'Contract version is declared more than once with conflicting values.'
    }
    elseif ($allVersionValues.Count -gt 1) {
        Add-IssueSpecError -Errors $errors -Message 'Contract version must be declared exactly once.'
    }
    elseif ($version -ne '1.0') {
        Add-IssueSpecError -Errors $errors -Message "Unsupported contract version '$version'; expected '1.0'."
    }

    foreach ($section in $requiredSections) {
        if (-not $sections.Contains($section)) {
            Add-IssueSpecError -Errors $errors -Message "Missing required section '## $section'."
        }
        elseif (-not (Test-IssueSectionContent -Content ([string]$sections[$section]))) {
            Add-IssueSpecError -Errors $errors -Message "Required section '## $section' is empty."
        }
    }

    $headingMatches = [regex]::Matches($structuralContent, '(?m)^(?<Level>#{2,3})[ \t]+(?<Title>[^\r\n]+)[ \t]*\r?$')
    $requiredHeadingLevels = @($headingMatches | Where-Object { $requiredSections -contains $_.Groups['Title'].Value.Trim() } | ForEach-Object { $_.Groups['Level'].Value.Length } | Sort-Object -Unique)
    if ($requiredHeadingLevels.Count -gt 1) {
        Add-IssueSpecError -Errors $errors -Message 'Required sections must use one consistent Markdown heading level.'
    }
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

    $blockingContent = Remove-MarkdownInlineCode -Content $nonRenderedContent
    $blockingContent = $blockingContent -replace '(?im)\bno\s+(?:unresolved\s+)?blocking\s+(?:TBD|TODO|FIXME)s?\b', ''
    $hasBlockingMarker =
        $blockingContent -match '(?im)\b(?:TBD|TODO|FIXME)\b\s*(?::|[-–—])?\s*(?:\([^)]*blocking[^)]*\)|\[[^]]*blocking[^]]*\]|blocking\b)' -or
        $blockingContent -match '(?im)\bblocking\b\s*[:\-]\s*(?:TBD|TODO|FIXME)\b'
    if ($hasBlockingMarker) {
        Add-IssueSpecError -Errors $errors -Message 'Unresolved blocking TBD/TODO marker is not allowed.'
    }
    $placeholderContent = Remove-MarkdownInlineCode -Content $nonRenderedContent
    if ($placeholderContent -match '(?im)\{\{[^}]+\}\}|^\s*[-*]\s*\[(?:insert|describe|add|todo|tbd)[^\]]*\]') {
        Add-IssueSpecError -Errors $errors -Message 'Template placeholder remains in the issue body.'
    }

    if ($sections.Contains('Relevant source and contracts')) {
        $sourceSection = [string]$sections['Relevant source and contracts']
        $sourcePaths = [regex]::Matches(
            $sourceSection,
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
            $lineStart = $sourceSection.LastIndexOf("`n", $pathMatch.Index) + 1
            $lineEnd = $sourceSection.IndexOf("`n", $pathMatch.Index)
            if ($lineEnd -lt 0) { $lineEnd = $sourceSection.Length }
            $line = [regex]::Replace($sourceSection.Substring($lineStart, $lineEnd - $lineStart), '`[^`]+`', '')
            $line = $line -replace '^[ \t]*(?:[-*+]|\d+[.)])(?:[ \t]+|\z)', ''
            $line = $line -replace '^[ \t\-*:;,\.—–]+|[ \t\-*:;,\.—–]+$', ''
            if ([string]::IsNullOrWhiteSpace($line)) {
                Add-IssueSpecError -Errors $errors -Message "Referenced repository-relative path must include an explanation: '$candidate'."
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
            if ([string]::IsNullOrWhiteSpace($criterion.Groups['Text'].Value)) {
                Add-IssueSpecError -Errors $errors -Message "Acceptance criterion '$id' must contain a nonempty observable outcome."
                continue
            }
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
        $evidenceLinePattern = '(?im)^\s*(?:[-*]|\d+\.)\s*\[(?<Id>AC\d+)\]\s*(?<Kind>Command|Test|Manual\s+observation)\s*:\s*(?<Evidence>[^\r\n]*?)\s*;\s*expected\s*:\s*(?<Expected>[^\r\n]*)\s*$'
        $evidenceMatches = [regex]::Matches($evidence, $evidenceLinePattern)
        foreach ($line in @($evidence -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
            $lineId = [regex]::Match($line, '\[(?<Id>AC\d+)\]').Groups['Id'].Value.ToUpperInvariant()
            if ($lineId -and $line -notmatch $evidenceLinePattern) {
                Add-IssueSpecError -Errors $errors -Message "Validation evidence entry for '$lineId' must include Command, Test, or Manual observation evidence and an expected result."
            }
        }
        foreach ($evidenceMatch in $evidenceMatches) {
            $evidenceId = $evidenceMatch.Groups['Id'].Value.ToUpperInvariant()
            if ([string]::IsNullOrWhiteSpace($evidenceMatch.Groups['Evidence'].Value) -or
                [string]::IsNullOrWhiteSpace($evidenceMatch.Groups['Expected'].Value)) {
                Add-IssueSpecError -Errors $errors -Message "Validation evidence entry for '$evidenceId' must include nonempty evidence and expected result."
                continue
            }
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
