#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][AllowEmptyString()][string]$Body,
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [string]$KnownIssuesJson,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$htmlTagRegex = [regex]::new('(?s)\G(?:<[A-Za-z][A-Za-z0-9-]*(?:\s+[A-Za-z_:][A-Za-z0-9_.:-]*(?:\s*=\s*(?:"[^"]*"|''[^'']*''|[^\s"''=<>`]+))?)*\s*/?>|</[A-Za-z][A-Za-z0-9-]*\s*>)')
$htmlNonTextRegex = [regex]::new('(?s)\G(?:<\?.*?(?:\?>|\z)|<![A-Za-z].*?(?:>|\z)|<!\[CDATA\[.*?(?:\]\]>|\z))')

function Remove-NonRenderedMarkdown { # NOSONAR - rendered HTML scanner tracks code and link ownership states.
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    if ([string]::IsNullOrEmpty($Content)) { return '' }
    # Preserve the validator's existing support for definitions adjacent to preceding text.
    $Content = [regex]::Replace($Content, '(?m)^(?=[ \t]{0,3}\[[^\]\r\n]+\]:)', [Environment]::NewLine)
    # PowerShell bundles Markdig; select GitHub extensions without its advanced-only syntax.
    Add-Type -Path (Join-Path $PSHOME 'Markdig.Signed.dll')
    $markdownPipelineBuilder = [Markdig.MarkdownPipelineBuilder]::new()
    $tableOptions = [Markdig.Extensions.Tables.PipeTableOptions]::new()
    $tableOptions.UseHeaderForColumnCount = $true
    $null = [Markdig.MarkdownExtensions]::UsePipeTables($markdownPipelineBuilder, $tableOptions)
    $null = [Markdig.MarkdownExtensions]::UseTaskLists($markdownPipelineBuilder)
    $null = [Markdig.MarkdownExtensions]::UseFootnotes($markdownPipelineBuilder)
    $null = [Markdig.MarkdownExtensions]::UseAutoLinks($markdownPipelineBuilder, $null)
    $null = [Markdig.MarkdownExtensions]::UseEmphasisExtras($markdownPipelineBuilder, [Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions]::Strikethrough)
    $html = [Markdig.Markdown]::ToHtml($Content, $markdownPipelineBuilder.Build())
    $localIssueHrefPattern = '(?i)^(?:(?:https?:)?//github\.com)?/' + [regex]::Escape($RepositoryOwner) + '/' + [regex]::Escape($RepositoryName) + '/issues/(?<Number>\d+)(?:[/?#].*)?$'
    $builder = [System.Text.StringBuilder]::new()
    $anchorHrefs = [System.Collections.Generic.List[string]]::new()
    $anchorHrefPattern = '(?is)^<a(?=\s|/?>)(?:"[^"]*"|''[^'']*''|[^''">])*?\s+href(?=\s|=|/?>)(?:\s*=\s*(?:"(?<Href>[^"]*)"|''(?<Href>[^'']*)''|(?<Href>[^\s>]+)))?[^>]*>'
    $insideAnchor = $false
    $insideSelect = $false
    $tableDepth = 0
    $cellScopes = [System.Collections.Generic.Stack[object]]::new()
    $codeDepth = 0
    $index = 0
    while ($index -lt $html.Length) {
        if ($index + 4 -le $html.Length -and $html.Substring($index, 4) -eq '<!--') {
            $closing = $html.IndexOf('-->', $index + 4, [System.StringComparison]::Ordinal)
            $index = if ($closing -ge 0) { $closing + 3 } else { $html.Length }
            continue
        }
        if ($html[$index] -eq '<') {
            $htmlNonText = $htmlNonTextRegex.Match($html, $index)
            if ($htmlNonText.Success) {
                $index += $htmlNonText.Length
                continue
            }
            $htmlTag = $htmlTagRegex.Match($html, $index)
            if ($htmlTag.Success) {
                $tagName = [regex]::Match($htmlTag.Value, '^</?(?<Name>[A-Za-z][A-Za-z0-9-]*)').Groups['Name'].Value.ToLowerInvariant()
                $isClosing = $htmlTag.Value.StartsWith('</', [System.StringComparison]::Ordinal)
                if ($tagName -eq 'select') {
                    $insideSelect = -not $isClosing -and -not $insideSelect
                    $index += $htmlTag.Length
                    continue
                }
                if ($insideSelect) {
                    $closesSelect = (-not $isClosing -and $tagName -eq 'input') -or
                        ($tableDepth -gt 0 -and $tagName -in @('caption', 'table', 'tbody', 'tfoot', 'thead', 'tr', 'td', 'th'))
                    if ($closesSelect) { $insideSelect = $false }
                    else {
                        $index += $htmlTag.Length
                        continue
                    }
                }
                # Table scopes restore both enclosing links and code, including implied ends.
                if ($tableDepth -gt 0 -and $cellScopes.Count -gt 0 -and $cellScopes.Peek().TableDepth -eq $tableDepth) {
                    $cellScope = $cellScopes.Peek()
                    $closesCell = if ($isClosing) {
                        $closingBoundaries = if ($cellScope.Name -eq 'caption') { @('table') } else { @('table', 'tbody', 'thead', 'tfoot', 'tr') }
                        $tagName -eq $cellScope.Name -or $tagName -in $closingBoundaries
                    }
                    else {
                        $tagName -in @('caption', 'col', 'colgroup', 'tbody', 'td', 'tfoot', 'th', 'thead', 'tr')
                    }
                    if ($closesCell) {
                        $null = $cellScopes.Pop()
                        $insideAnchor = $cellScope.PriorAnchor
                        $codeDepth = $cellScope.PriorCodeDepth
                    }
                }
                if ($tagName -eq 'table') {
                    $tableDepth = [Math]::Max(0, $tableDepth + $(if ($isClosing) { -1 } else { 1 }))
                }
                elseif (-not $isClosing -and $tableDepth -gt 0 -and $tagName -in @('caption', 'td', 'th')) {
                    $cellScopes.Push([pscustomobject]@{ Name = $tagName; TableDepth = $tableDepth; PriorAnchor = $insideAnchor; PriorCodeDepth = $codeDepth })
                }
                if ($tagName -in @('pre', 'code')) {
                    $codeDepth = [Math]::Max(0, $codeDepth + $(if ($isClosing) { -1 } else { 1 }))
                }
                elseif ($codeDepth -eq 0) {
                    if ($tagName -eq 'a' -and -not $isClosing) {
                        $insideAnchor = $false
                        $anchorHref = [regex]::Match($htmlTag.Value, $anchorHrefPattern)
                        if ($anchorHref.Success) {
                            $href = [System.Net.WebUtility]::HtmlDecode($anchorHref.Groups['Href'].Value)
                            $localIssueHref = [regex]::Match($href.TrimEnd([char[]](0..0x20)), $localIssueHrefPattern)
                            if ($localIssueHref.Success) {
                                $anchorHrefs.Add("https://github.com/$RepositoryOwner/$RepositoryName/issues/$($localIssueHref.Groups['Number'].Value)")
                            }
                            $insideAnchor = $href -notmatch '^[\x00-\x20]'
                            $null = $builder.Append(' ')
                        }
                    }
                    elseif ($tagName -eq 'a') { $insideAnchor = $false }
                    elseif (-not $insideAnchor -and $tagName -in @('p', 'div', 'li', 'blockquote', 'tr', 'td', 'th', 'br')) {
                        $null = $builder.Append(' ')
                    }
                }
                $index += $htmlTag.Length
                continue
            }
        }
        if ($codeDepth -eq 0 -and -not $insideAnchor) { $null = $builder.Append($html[$index]) }
        $index++
    }
    $renderedText = [System.Net.WebUtility]::HtmlDecode($builder.ToString())
    if ($anchorHrefs.Count -gt 0) {
        $renderedText += [Environment]::NewLine + [System.Net.WebUtility]::HtmlDecode(($anchorHrefs -join [Environment]::NewLine))
    }
    return $renderedText
}

function Get-PrIssueReferences { # NOSONAR - bounded reference extraction intentionally coordinates rendered Markdown and repository validation states.
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Content,
        [Parameter(Mandatory)][string]$Owner,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$Errors,
        [int]$MaximumReferences = 20
    )

    $references = [System.Collections.Generic.List[object]]::new()
    $seenNumbers = [System.Collections.Generic.HashSet[int]]::new()
    $addReference = {
        param([int]$Number, [string]$Text)
        if ($seenNumbers.Contains($Number)) { return $true }
        if ($seenNumbers.Count -ge $MaximumReferences) {
            $Errors.Add("Too many repository issue references were supplied; maximum supported is $MaximumReferences.")
            return $false
        }
        $null = $seenNumbers.Add($Number)
        $references.Add([pscustomobject]@{ Number = $Number; Text = $Text })
        return $true
    }
    $contentForExtraction = $Content
    $fullUrlPattern = '(?<![A-Za-z0-9+./?=&%_#:-])https://github\.com/(?<Owner>[^/\s]+)/(?<Repo>[^/#\s]+)/(?<Kind>issues|pull)/(?<Number>\d+)(?:[/?#][^\s<>()]*)?(?=[\s>)\].,;!?]|$)'
    foreach ($match in [regex]::Matches($contentForExtraction, $fullUrlPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $matchOwner = $match.Groups['Owner'].Value
        $matchRepo = $match.Groups['Repo'].Value
        $kind = $match.Groups['Kind'].Value.ToLowerInvariant()
        $number = [int]$match.Groups['Number'].Value
        if (-not ([string]::Equals($matchOwner, $Owner, [System.StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($matchRepo, $Name, [System.StringComparison]::OrdinalIgnoreCase))) {
            continue
        }
        if ($kind -eq 'pull') { continue }
        if (-not (& $addReference -Number $number -Text $match.Value)) { return @($references) }
    }

    $withoutFullUrls = [regex]::Replace($contentForExtraction, $fullUrlPattern, '')
    $withoutUriComponents = [regex]::Replace($withoutFullUrls, '(?i)\b[A-Za-z][A-Za-z0-9+.-]*://[^\s<>()]+', '')
    foreach ($match in [regex]::Matches($withoutUriComponents, '(?i)(?<![\w/])(?<Owner>[A-Za-z0-9_.-]+)/(?<Repo>[A-Za-z0-9_.-]+)#(?<QualifiedNumber>\d+)\b')) {
        if ([string]::Equals($match.Groups['Owner'].Value, $Owner, [System.StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($match.Groups['Repo'].Value, $Name, [System.StringComparison]::OrdinalIgnoreCase)) {
            if (-not (& $addReference -Number ([int]$match.Groups['QualifiedNumber'].Value) -Text $match.Value)) { return @($references) }
        }
    }
    foreach ($match in [regex]::Matches($withoutUriComponents, '(?<![\w/])#(?<Number>\d+)\b')) {
        $number = [int]$match.Groups['Number'].Value
        if (-not (& $addReference -Number $number -Text $match.Value)) { return @($references) }
    }

    return @($references | Sort-Object Number -Unique)
}

function Get-PrIssueRecord {
    param(
        [Parameter(Mandatory)][int]$Number,
        [Parameter(Mandatory)][string]$Owner,
        [Parameter(Mandatory)][string]$Name,
        [string]$KnownIssuesJson
    )

    if (-not [string]::IsNullOrWhiteSpace($KnownIssuesJson)) {
        $known = @(ConvertFrom-Json -InputObject $KnownIssuesJson)
        return @($known | Where-Object { [int]$_.number -eq $Number } | Select-Object -First 1)
    }

    $apiOutput = & gh api "repos/$Owner/$Name/issues/$Number" --header 'Accept: application/vnd.github+json' 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to resolve issue #$Number through the GitHub API: $($apiOutput.Trim())"
    }
    return @(ConvertFrom-Json -InputObject $apiOutput)
}

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$renderedBody = Remove-NonRenderedMarkdown -Content $Body
$references = @(Get-PrIssueReferences -Content $renderedBody -Owner $RepositoryOwner -Name $RepositoryName -Errors $errors)
$resolvedIssues = [System.Collections.Generic.List[object]]::new()
$referenceErrors = [System.Collections.Generic.List[string]]::new()
$maximumReferences = 20

if ($references.Count -eq 0) {
    $errors.Add('No repository issue reference was found in the rendered pull request description.')
}
elseif ($references.Count -gt $maximumReferences) {
    $errors.Add("Too many repository issue references were supplied ($($references.Count)); maximum supported is $maximumReferences.")
}

foreach ($reference in @($references | Select-Object -First $maximumReferences)) {
    if ($references.Count -gt $maximumReferences) { break }
    try {
        $record = @(Get-PrIssueRecord -Number $reference.Number -Owner $RepositoryOwner -Name $RepositoryName -KnownIssuesJson $KnownIssuesJson)
        if ($record.Count -eq 0) {
            $referenceErrors.Add("Referenced issue #$($reference.Number) does not exist in $RepositoryOwner/$RepositoryName.")
            continue
        }
        $issue = $record[0]
        $hasPullRequestProperty = $null -ne $issue.PSObject.Properties['pull_request']
        $issueTypeProperty = $issue.PSObject.Properties['type']
        $isPullRequest = $hasPullRequestProperty -or ($null -ne $issueTypeProperty -and [string]$issueTypeProperty.Value -eq 'pull_request')
        if ($isPullRequest) {
            $referenceErrors.Add("Referenced number #$($reference.Number) is a pull request, not an issue.")
            continue
        }
        if ([string]$issue.state -ne 'open') {
            $referenceErrors.Add("Referenced issue #$($reference.Number) is not open; update the PR to an active repository issue.")
            continue
        }
        $resolvedIssues.Add([pscustomobject]@{ Number = $reference.Number; Title = [string]$issue.title; State = [string]$issue.state })
    }
    catch {
        $referenceErrors.Add($_.Exception.Message)
    }
}

if ($resolvedIssues.Count -eq 0) {
    foreach ($referenceError in $referenceErrors) { $errors.Add($referenceError) }
}
else {
    foreach ($referenceError in $referenceErrors) { $warnings.Add($referenceError) }
}

$result = [pscustomobject][ordered]@{
    SchemaVersion = '1.0'
    Valid = $errors.Count -eq 0 -and $resolvedIssues.Count -gt 0
    Repository = "$RepositoryOwner/$RepositoryName"
    References = @($references)
    ResolvedIssues = @($resolvedIssues)
    Warnings = @($warnings)
    Errors = @($errors | Sort-Object -Unique)
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8 -Compress
}
else {
    Write-Output "PR_ISSUE_REFERENCE: $(if ($result.Valid) { 'PASS' } else { 'FAIL' })"
    foreach ($issue in @($result.ResolvedIssues)) { Write-Output "ISSUE: #$($issue.Number) [$($issue.State)] $($issue.Title)" }
    foreach ($warning in @($result.Warnings)) { Write-Output "WARNING: $warning" }
    foreach ($error in @($result.Errors)) { Write-Output "ERROR: $error" }
}

if ($result.Valid) { exit 0 }
exit 1
