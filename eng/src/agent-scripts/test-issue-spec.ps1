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

function Get-MarkdownSections { # NOSONAR - bounded Markdown heading parser intentionally tracks heading state.
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $allHeadings = [System.Collections.Generic.List[object]]::new()
    $offset = 0
    foreach ($rawLine in $Content.Split([char]10)) {
        $line = $rawLine.TrimEnd([char]13)
        $leading = $line.Length - $line.TrimStart(' ').Length
        $trimmed = $line.TrimStart(' ')
        $hashCount = 0
        while ($hashCount -lt $trimmed.Length -and $trimmed[$hashCount] -eq '#') { $hashCount++ }
        if ($leading -le 3 -and $hashCount -ge 1 -and $hashCount -le 6 -and
            $hashCount -lt $trimmed.Length -and ($trimmed[$hashCount] -eq ' ' -or $trimmed[$hashCount] -eq [char]9)) {
            $title = $trimmed.Substring($hashCount).Trim()
            if ($title) {
                $allHeadings.Add([pscustomobject]@{ Index = $offset; Length = $line.Length; Level = $hashCount; Title = $title })
            }
        }
        $offset += $rawLine.Length + 1
    }
    $requiredHeadingMatches = @($allHeadings | Where-Object { $requiredSections -contains $_.Title })
    $sections = [ordered]@{}
    for ($index = 0; $index -lt $requiredHeadingMatches.Count; $index++) {
        $match = $requiredHeadingMatches[$index]
        $start = $match.Index + $match.Length
        $level = $match.Level
        $nextPeer = @($allHeadings | Where-Object { $_.Index -gt $start -and $_.Level -le $level } | Select-Object -First 1)
        $end = if ($nextPeer.Count -gt 0) { $nextPeer[0].Index } else { $Content.Length }
        $title = $match.Title
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

function Get-MarkdownFenceOpening {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Line)

    $match = [regex]::Match($Line, '^[ \t]{0,3}(?<Fence>`{3,}|~{3,})')
    if (-not $match.Success) { return $null }
    $value = $match.Groups['Fence'].Value
    if ($value[0] -eq [char]96 -and $Line.Substring($match.Index + $match.Length).Contains([char]96)) { return $null }
    return [pscustomobject]@{ Character = $value.Substring(0, 1); Length = $value.Length }
}

function Test-MarkdownFenceClosing {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Line,
        [Parameter(Mandatory)][string]$Character,
        [Parameter(Mandatory)][int]$Length
    )
    return $Line -match ('^[ \t]{0,3}' + [regex]::Escape($Character) + '{' + $Length + ',}[ \t]*$')
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
    $fenceContentMarker = [char]0x1f
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Content -split '\r?\n')) {
        $openingFence = if (-not $insideFence) { Get-MarkdownFenceOpening -Line $line } else { $null }
        if ($null -ne $openingFence) {
            $insideFence = $true
            $fenceCharacter = $openingFence.Character
            $fenceLength = $openingFence.Length
            $lines.Add('')
            continue
        }
        if ($insideFence -and (Test-MarkdownFenceClosing -Line $line -Character $fenceCharacter -Length $fenceLength)) {
            $lines.Add('')
            $insideFence = $false
            continue
        }
        if ($insideFence) {
            if ($MaskContent -or $line -match '^\s*#{1,6}[ \t]+') {
                $lines.Add('')
                continue
            }
            $lines.Add($fenceContentMarker + $line)
            continue
        }
        $lines.Add($line)
    }
    return ($lines -join [Environment]::NewLine)
}

function Remove-MarkdownInlineCode { # NOSONAR - bounded Markdown delimiter scanner is intentionally stateful.
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $delimiterCharacter = [char]96
    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $Content.Length) {
        if ($Content[$index] -ne $delimiterCharacter -or ($index -gt 0 -and $Content[$index - 1] -eq '\')) {
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

function Remove-MarkdownHtmlComments { # NOSONAR - bounded comment/code scanner is intentionally stateful.
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $Content.Length) {
        if ($Content[$index] -eq [char]0x1e) {
            $null = $builder.Append($Content[$index])
            $index++
            continue
        }
        $isUnescapedDelimiter = $false
        if ($Content[$index] -eq '`') {
            $precedingBackslashes = 0
            $backslashIndex = $index - 1
            while ($backslashIndex -ge 0 -and $Content[$backslashIndex] -eq '\') {
                $precedingBackslashes++
                $backslashIndex--
            }
            $isUnescapedDelimiter = $precedingBackslashes % 2 -eq 0
        }
        if ($isUnescapedDelimiter) {
            $start = $index
            while ($index -lt $Content.Length -and $Content[$index] -eq '`') { $index++ }
            $delimiterLength = $index - $start
            $cursor = $index
            $closing = -1
            while ($cursor -lt $Content.Length) {
                if ($Content[$cursor] -eq [char]0x1e) {
                    break
                }
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

function Remove-MarkdownHtmlBlocks { # NOSONAR - bounded raw-HTML block scanner intentionally tracks parser state.
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $blockTagNames = 'address|article|aside|base|basefont|blockquote|body|caption|center|col|colgroup|dd|details|dialog|dir|div|dl|dt|fieldset|figcaption|figure|footer|form|h1|h2|h3|h4|h5|h6|head|header|hr|html|iframe|legend|li|link|main|menu|menuitem|nav|ol|p|pre|script|search|section|summary|table|tbody|td|tfoot|th|thead|title|tr|track|ul'
    $lines = [System.Collections.Generic.List[string]]::new()
    $htmlBoundary = [char]0x1e
    $rawTag = ''
    $insideHtmlBlock = $false
    $tokenTerminator = ''
    $insideHtmlComment = $false
    foreach ($line in ($Content -split '\r?\n')) {
        if ($rawTag) {
            $lines.Add($htmlBoundary)
            if ($line -match ('(?i)</' + [regex]::Escape($rawTag) + '[ \t>]' )) {
                $rawTag = ''
            }
            continue
        }
        if ($tokenTerminator) {
            $lines.Add($htmlBoundary)
            if ($line -match $tokenTerminator) {
                $tokenTerminator = ''
            }
            continue
        }
        if ($insideHtmlBlock) {
            $lines.Add($htmlBoundary)
            if ([string]::IsNullOrWhiteSpace($line)) {
                $insideHtmlBlock = $false
            }
            continue
        }
        if ($insideHtmlComment) {
            $lines.Add($htmlBoundary)
            if ($line.Contains('-->')) { $insideHtmlComment = $false }
            continue
        }
        if ($line -match '^[ \t]{0,3}<!--') {
            $lines.Add($htmlBoundary)
            if (-not $line.Contains('-->')) { $insideHtmlComment = $true }
            continue
        }

        $rawTagMatch = [regex]::Match($line, '(?i)^[ \t]{0,3}<(?<Tag>pre|script|style|textarea)\b')
        if ($rawTagMatch.Success) {
            $lines.Add($htmlBoundary)
            $tag = $rawTagMatch.Groups['Tag'].Value
            if ($line -notmatch ('(?i)</' + [regex]::Escape($tag) + '[ \t>]' )) {
                $rawTag = $tag
            }
            continue
        }
        if ($line -match ('(?i)^[ \t]{0,3}<(?:(?:' + $blockTagNames + ')\b)')) {
            $lines.Add($htmlBoundary)
            $insideHtmlBlock = $true
            continue
        }
        $processingInstructionMatch = [regex]::Match($line, '(?i)^[ \t]{0,3}<\?')
        if ($processingInstructionMatch.Success) {
            $lines.Add($htmlBoundary)
            if ($line -notmatch '\?>') { $tokenTerminator = '\?>' }
            continue
        }
        $cdataMatch = [regex]::Match($line, '^[ \t]{0,3}<!\[CDATA\[')
        if ($cdataMatch.Success) {
            $lines.Add($htmlBoundary)
            if ($line -notmatch '\]\]>') { $tokenTerminator = '\]\]>' }
            continue
        }
        $declarationMatch = [regex]::Match($line, '^[ \t]{0,3}<![A-Z]')
        if ($declarationMatch.Success) {
            $lines.Add($htmlBoundary)
            if ($line -notmatch '>') { $tokenTerminator = '>' }
            continue
        }
        if ($line -match '(?i)^[ \t]{0,3}(?:</?[A-Za-z][^>\r\n]*>|<[A-Za-z][^>\r\n]*/>)\s*$') {
            $lines.Add($htmlBoundary)
            $insideHtmlBlock = $true
            continue
        }
        $lines.Add($line)
    }
    return ($lines -join [Environment]::NewLine)
}

function Remove-MarkdownIndentedCode {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Content -split '\r?\n')) {
        if ($line -match '^(?: {4,}|\t)') {
            $lines.Add('')
            continue
        }
        $lines.Add($line)
    }
    return ($lines -join [Environment]::NewLine)
}

function Get-RepositoryTrackedPaths {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Root)

    $gitRoot = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
    $gitRootArgument = $gitRoot.Replace('\', '/')
    $trackedPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $gitOutput = @(& git -c "safe.directory=$gitRootArgument" -C $gitRoot --literal-pathspecs ls-files 2>$null)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate tracked repository paths.'
    }

    foreach ($path in $gitOutput) {
        $segments = @($path.ToString().Replace('\', '/') -split '/' | Where-Object { $_ -and $_ -ne '.' })
        if ($segments.Count -gt 0) {
            $null = $trackedPaths.Add(($segments -join '/'))
        }
    }
    return ,$trackedPaths
}

function Test-RepositoryRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Candidate,
        [Parameter(Mandatory)][System.Collections.Generic.HashSet[string]]$TrackedPaths
    )

    if ([string]::IsNullOrWhiteSpace($Candidate) -or
        [System.IO.Path]::IsPathRooted($Candidate) -or
        $Candidate -match '^[A-Za-z]:|^[\\/]') {
        return $false
    }

    $segments = [System.Collections.Generic.List[string]]::new()
    foreach ($segment in ($Candidate.Replace('\', '/') -split '/')) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.') { continue }
        if ($segment -eq '..') { return $false }
        $segments.Add($segment)
    }
    if ($segments.Count -eq 0) { return $false }

    $gitPath = $segments -join '/'
    if ($TrackedPaths.Contains($gitPath)) { return $true }
    $childPrefix = "$gitPath/"
    foreach ($trackedPath in $TrackedPaths) {
        if ($trackedPath.StartsWith($childPrefix, [StringComparison]::Ordinal)) {
            return $true
        }
    }
    return $false
}

function Test-IssueSectionContent {
    [CmdletBinding()]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $withoutHeadings = [regex]::Replace($Content, '(?m)^[ \t]{0,3}#{1,6}[ \t]*[^\r\n]*$', '')
    $withoutHeadings = $withoutHeadings.Replace([string][char]0x1e, '')
    return -not [string]::IsNullOrWhiteSpace($withoutHeadings)
}

function Get-IssueSpecResult { # NOSONAR - this validator intentionally aggregates independent contract gates into one report.
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$IssuePath,
        [Parameter(Mandatory)][string]$RepositoryRoot
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $warnings = [System.Collections.Generic.List[string]]::new()
    $content = Get-Content -LiteralPath $IssuePath -Raw -ErrorAction Stop
    $trackedPaths = Get-RepositoryTrackedPaths -Root $RepositoryRoot
    $structuralContent = Remove-MarkdownHtmlComments -Content (Remove-MarkdownHtmlBlocks -Content (Remove-MarkdownFencedBlocks -Content $content))
    $nonRenderedContent = Remove-MarkdownHtmlComments -Content (Remove-MarkdownHtmlBlocks -Content (Remove-MarkdownFencedBlocks -Content $content -MaskContent))
    $nonRenderedContent = Remove-MarkdownIndentedCode -Content $nonRenderedContent
    $sections = Get-MarkdownSections -Content $structuralContent
    $nonRenderedSections = Get-MarkdownSections -Content $nonRenderedContent

    $firstRequiredSectionIndex = $nonRenderedContent.Length
    foreach ($requiredSection in $requiredSections) {
        $sectionHeading = [regex]::Match($nonRenderedContent, '(?m)^[ \t]{0,3}#{2,3}[ \t]+' + [regex]::Escape($requiredSection) + '[ \t]*\r?$')
        if ($sectionHeading.Success -and $sectionHeading.Index -lt $firstRequiredSectionIndex) {
            $firstRequiredSectionIndex = $sectionHeading.Index
        }
    }
    $prologueContent = $nonRenderedContent.Substring(0, $firstRequiredSectionIndex)
    $prologueVersionValues = @()
    $prologueVersionValues += @([regex]::Matches($prologueContent, '(?im)^[ \t]{0,3}Contract version:[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $prologueVersionValues += @([regex]::Matches($prologueContent, '(?im)^[ \t]{0,3}#{2,3}[ \t]+Contract version[ \t]*\r?\n(?:[ \t]*\r?\n)*[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $allVersionValues = @()
    $allVersionValues += @([regex]::Matches($nonRenderedContent, '(?im)^[ \t]{0,3}Contract version:[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
    $allVersionValues += @([regex]::Matches($nonRenderedContent, '(?im)^[ \t]{0,3}#{2,3}[ \t]+Contract version[ \t]*\r?\n(?:[ \t]*\r?\n)*[ \t]*(?<Value>\d+\.\d+)[ \t]*\r?$') | ForEach-Object { $_.Groups['Value'].Value })
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

    $headingMatches = [regex]::Matches($structuralContent, '(?m)^[ \t]{0,3}(?<Level>#{1,6})[ \t]+(?<Title>[^\r\n]+)[ \t]*\r?$')
    if (@($headingMatches | Where-Object { $requiredSections -contains $_.Groups['Title'].Value.Trim() -and [int]$_.Groups['Level'].Value.Length -notin @(2, 3) }).Count -gt 0) {
        Add-IssueSpecError -Errors $errors -Message 'Required sections must use level 2 or level 3 Markdown headings.'
    }
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
        $blockingContent -match '(?im)\b(?:TBD|TODO|FIXME)\b[^\r\n]{0,200}\bblocking\b' -or
        $blockingContent -match '(?im)\bblocking\b[^\r\n]{0,200}\b(?:TBD|TODO|FIXME)\b'
    if ($hasBlockingMarker) {
        Add-IssueSpecError -Errors $errors -Message 'Unresolved blocking TBD/TODO marker is not allowed.'
    }
    $placeholderContent = Remove-MarkdownInlineCode -Content $nonRenderedContent
    if ($placeholderContent -match '(?im)\{\{[^}]+\}\}|^\s*[-*]\s*\[(?:insert|describe|add|todo|tbd)[^\]]*\]') {
        Add-IssueSpecError -Errors $errors -Message 'Template placeholder remains in the issue body.'
    }

    if ($nonRenderedSections.Contains('Relevant source and contracts')) {
        $sourceSection = Remove-MarkdownIndentedCode -Content ([string]$nonRenderedSections['Relevant source and contracts'])
        $sourcePaths = [regex]::Matches(
            $sourceSection,
            '`(?<Path>[^`]+)`'
        )
        if ($sourcePaths.Count -eq 0) {
            Add-IssueSpecError -Errors $errors -Message 'Relevant source and contracts must list backtick-wrapped repository-relative paths.'
        }
        $sourcePathMatchLimit = 512
        if ($sourcePaths.Count -gt $sourcePathMatchLimit) {
            Add-IssueSpecError -Errors $errors -Message "Relevant source and contracts may contain at most $sourcePathMatchLimit total path references."
        }
        $validatedSourcePaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $sourcePathLimit = 256
        $sourcePathLimitExceeded = $false
        foreach ($pathMatch in @($sourcePaths | Select-Object -First $sourcePathMatchLimit)) {
            $candidate = $pathMatch.Groups['Path'].Value.Trim()
            if ($validatedSourcePaths.Add($candidate)) {
                if ($validatedSourcePaths.Count -gt $sourcePathLimit) {
                    $sourcePathLimitExceeded = $true
                    break
                }
                if (-not (Test-RepositoryRelativePath -Candidate $candidate -TrackedPaths $trackedPaths)) {
                    Add-IssueSpecError -Errors $errors -Message "Referenced repository-relative path does not exist: '$candidate'."
                }
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
        if ($sourcePathLimitExceeded) {
            Add-IssueSpecError -Errors $errors -Message "Relevant source and contracts may contain at most $sourcePathLimit distinct repository-relative paths."
        }
    }

    $acceptanceIds = [System.Collections.Generic.List[string]]::new()
    if ($sections.Contains('Acceptance criteria')) {
        $criteriaContent = Remove-MarkdownIndentedCode -Content ([string]$nonRenderedSections['Acceptance criteria'])
        $criteria = [regex]::Matches(
            $criteriaContent,
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
        $evidence = Remove-MarkdownIndentedCode -Content ([string]$nonRenderedSections['Validation evidence map'])
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
