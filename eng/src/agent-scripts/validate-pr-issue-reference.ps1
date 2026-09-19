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

function Remove-MarkdownHtmlComments {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)
    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $Content.Length) {
        if ($Content[$index] -eq '`') {
            $start = $index
            while ($index -lt $Content.Length -and $Content[$index] -eq '`') { $index++ }
            $delimiterLength = $index - $start
            $closing = $Content.IndexOf(('`' * $delimiterLength), $index, [System.StringComparison]::Ordinal)
            if ($closing -ge 0) {
                $null = $builder.Append($Content.Substring($start, $closing + $delimiterLength - $start))
                $index = $closing + $delimiterLength
                continue
            }
            $null = $builder.Append($Content.Substring($start))
            break
        }
        if ($index + 4 -le $Content.Length -and $Content.Substring($index, 4) -eq '<!--') {
            $closing = $Content.IndexOf('-->', $index + 4, [System.StringComparison]::Ordinal)
            $index = if ($closing -ge 0) { $closing + 3 } else { $Content.Length }
            continue
        }
        $null = $builder.Append($Content[$index])
        $index++
    }
    return $builder.ToString()
}

function Remove-MarkdownLinkDestinations {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)
    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    $operations = 0
    $operationBudget = [Math]::Max(1000, [Math]::Min(1000000, ($Content.Length * 4) + 1000))
    while ($index -lt $Content.Length) {
        if ($index + 1 -lt $Content.Length -and $Content[$index] -eq ']' -and $Content[$index + 1] -eq '(') {
            $depth = 1
            $cursor = $index + 2
            while ($cursor -lt $Content.Length -and $depth -gt 0) {
                $operations++
                if ($operations -gt $operationBudget) {
                    $null = $builder.Append($Content.Substring($index))
                    return $builder.ToString()
                }
                if ($Content[$cursor] -eq '\' -and $cursor + 1 -lt $Content.Length) {
                    $cursor += 2
                    continue
                }
                if ($Content[$cursor] -eq '(') { $depth++ }
                elseif ($Content[$cursor] -eq ')') { $depth-- }
                $cursor++
            }
            if ($depth -eq 0) {
                $null = $builder.Append(']')
                $index = $cursor
                continue
            }
        }
        $null = $builder.Append($Content[$index])
        $index++
    }
    return $builder.ToString()
}

function Remove-NonRenderedMarkdown {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

    $insideFence = $false
    $fenceCharacter = ''
    $fenceLength = 0
    $withoutFences = foreach ($line in ($Content -split '\r?\n')) {
        $containerLine = $line -replace '^(?:[ ]{0,3}>[ \t]?)+', ''
        if (-not $insideFence -and $containerLine -match '^[ ]{0,3}(?<Fence>`{3,}|~{3,})') {
            $insideFence = $true
            $fenceCharacter = $Matches.Fence.Substring(0, 1)
            $fenceLength = $Matches.Fence.Length
            ''
            continue
        }
        if ($insideFence) {
            $closingPattern = '^[ ]{0,3}' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}[ \t]*$'
            if ($containerLine -match $closingPattern) { $insideFence = $false }
            ''
            continue
        }
        $line
    }
    $withoutFences = $withoutFences -join [Environment]::NewLine
    $withoutComments = Remove-MarkdownHtmlComments -Content $withoutFences
    $anchorHrefPattern = '(?is)<a\b[^>]*\bhref\s*=\s*(?:"(?<Href>[^"]+)"|''(?<Href>[^'']+)''|(?<Href>[^\s>]+))[^>]*>'
    $anchorHrefs = @([regex]::Matches($withoutComments, $anchorHrefPattern) | ForEach-Object { $_.Groups['Href'].Value })
    $withoutComments = [regex]::Replace($withoutComments, '(?m)<(?!https?://|mailto:)(?:[^>\"''\r\n]|\"[^\"]*\"|''[^'']*'')*>', '')
    if ($anchorHrefs.Count -gt 0) { $withoutComments += [Environment]::NewLine + ($anchorHrefs -join [Environment]::NewLine) }
    $withoutComments = [regex]::Replace($withoutComments, '(?m)^(?: {4}|\t)[^\r\n]*(?:\r?\n|$)', '')
    $withoutComments = [regex]::Replace($withoutComments, '(?m)^(?:[ ]{0,3}>[ \t]?)+[ ]{4,}[^\r\n]*(?:\r?\n|$)', '')
    $builder = [System.Text.StringBuilder]::new()
    $index = 0
    while ($index -lt $withoutComments.Length) {
        if ($withoutComments[$index] -ne '`') {
            $null = $builder.Append($withoutComments[$index])
            $index++
            continue
        }

        $start = $index
        while ($index -lt $withoutComments.Length -and $withoutComments[$index] -eq '`') { $index++ }
        $delimiterLength = $index - $start
        $closingIndex = $index
        $closingLength = 0
        while ($closingIndex -lt $withoutComments.Length) {
            if ($withoutComments[$closingIndex] -ne '`') { $closingIndex++; continue }
            $candidate = $closingIndex
            while ($candidate -lt $withoutComments.Length -and $withoutComments[$candidate] -eq '`') { $candidate++ }
            if (($candidate - $closingIndex) -ge $delimiterLength) {
                $closingLength = $candidate - $closingIndex
                break
            }
            $closingIndex = $candidate
        }
        if ($closingLength -eq $delimiterLength) {
            $index = $closingIndex + $closingLength
        }
        else {
            $null = $builder.Append($withoutComments.Substring($start, $delimiterLength))
        }
    }
    return $builder.ToString()
}

function Get-PrIssueReferences {
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
    $usedLabels = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($used in [regex]::Matches($Content, '\[[^\]\r\n]+\]\[(?<Label>[^\]\r\n]*)\]')) {
        $label = [regex]::Replace($used.Groups['Label'].Value.Trim(), '\s+', ' ')
        if ([string]::IsNullOrWhiteSpace($label)) { $label = [regex]::Match($used.Value, '^\[(?<Text>[^\]\r\n]+)\]\[\]$').Groups['Text'].Value.Trim(); $label = [regex]::Replace($label, '\s+', ' ') }
        if ($label) { $null = $usedLabels.Add($label) }
    }
    foreach ($used in [regex]::Matches($Content, '(?<!\!)\[(?<Label>[^\]\r\n]+)\](?![ \t]*(?:\(|\[|:))')) {
        $prefix = $Content.Substring(0, $used.Index)
        if ($prefix -match '(?m)(?:^|\r?\n)[ \t]*[-*+][ \t]+$' -and $used.Groups['Label'].Value -match '^[ xX]$') { continue }
        $null = $usedLabels.Add([regex]::Replace($used.Groups['Label'].Value.Trim(), '\s+', ' '))
    }
    $contentForExtraction = $Content
    $definitionPattern = '(?m)^[ \t]{0,3}\[(?<Label>[^\]\r\n]+)\]:[ \t]*(?<Destination><[^>\r\n]+>|\S+)(?:[ \t]+.*)?$'
    foreach ($definition in [regex]::Matches($Content, $definitionPattern)) {
        $definitionLabel = [regex]::Replace($definition.Groups['Label'].Value.Trim(), '\s+', ' ')
        $replacement = if ($usedLabels.Contains($definitionLabel)) { " $($definition.Groups['Destination'].Value) " } else { '' }
        $contentForExtraction = $contentForExtraction.Replace($definition.Value, $replacement)
    }
    $fullUrlPattern = '(?<![A-Za-z0-9+./?=&%_-])https://github\.com/(?<Owner>[^/\s]+)/(?<Repo>[^/#\s]+)/(?<Kind>issues|pull)/(?<Number>\d+)(?:[/?#][^\s<>()]*)?(?=[\s>)\].,;!?]|$)'
    foreach ($match in [regex]::Matches($contentForExtraction, $fullUrlPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $matchOwner = $match.Groups['Owner'].Value
        $matchRepo = $match.Groups['Repo'].Value
        $kind = $match.Groups['Kind'].Value.ToLowerInvariant()
        $number = [int]$match.Groups['Number'].Value
        if (-not ([string]::Equals($matchOwner, $Owner, [System.StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($matchRepo, $Name, [System.StringComparison]::OrdinalIgnoreCase))) {
            continue
        }
        $prefix = $contentForExtraction.Substring(0, $match.Index)
        if ($prefix -match '!\[[^\]\r\n]*\]\($') { continue }
        if ($kind -eq 'pull') { continue }
        if (-not (& $addReference -Number $number -Text $match.Value)) { return @($references) }
    }

    $withoutFullUrls = [regex]::Replace($contentForExtraction, $fullUrlPattern, '')
    $withoutLinkDestinations = Remove-MarkdownLinkDestinations -Content $withoutFullUrls
    $withoutUriComponents = [regex]::Replace($withoutLinkDestinations, '(?i)\b[A-Za-z][A-Za-z0-9+.-]*://[^\s<>()]+', '')
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
