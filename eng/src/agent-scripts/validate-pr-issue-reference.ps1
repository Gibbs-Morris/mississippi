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

function Remove-NonRenderedMarkdown {
    param([Parameter(Mandatory)][string]$Content)

    $insideFence = $false
    $fenceCharacter = ''
    $fenceLength = 0
    $withoutFences = foreach ($line in ($Content -split '\r?\n')) {
        if (-not $insideFence -and $line -match '^\s*(?<Fence>`{3,}|~{3,})') {
            $insideFence = $true
            $fenceCharacter = $Matches.Fence.Substring(0, 1)
            $fenceLength = $Matches.Fence.Length
            ''
            continue
        }
        if ($insideFence) {
            $closingPattern = '^\s*' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}\s*$'
            if ($line -match $closingPattern) { $insideFence = $false }
            ''
            continue
        }
        $line
    }
    $withoutFences = $withoutFences -join [Environment]::NewLine
    $withoutComments = [regex]::Replace($withoutFences, '(?s)<!--.*?(?:-->|$)', '')
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
        if ($closingLength -ge $delimiterLength) {
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
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$Errors
    )

    $references = [System.Collections.Generic.List[object]]::new()
    $fullUrlPattern = 'https://github\.com/(?<Owner>[^/\s]+)/(?<Repo>[^/#\s]+)/(?<Kind>issues|pull)/(?<Number>\d+)'
    foreach ($match in [regex]::Matches($Content, $fullUrlPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $matchOwner = $match.Groups['Owner'].Value
        $matchRepo = $match.Groups['Repo'].Value
        $kind = $match.Groups['Kind'].Value.ToLowerInvariant()
        $number = [int]$match.Groups['Number'].Value
        if (-not ([string]::Equals($matchOwner, $Owner, [System.StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($matchRepo, $Name, [System.StringComparison]::OrdinalIgnoreCase))) {
            continue
        }
        if ($kind -eq 'pull') {
            $Errors.Add("Pull request URL is not an issue reference: '$($match.Value)'.")
            continue
        }
        $references.Add([pscustomobject]@{ Number = $number; Text = $match.Value })
    }

    $withoutFullUrls = [regex]::Replace($Content, $fullUrlPattern, '')
    foreach ($match in [regex]::Matches($withoutFullUrls, '(?<![\w/])#(?<Number>\d+)\b')) {
        $number = [int]$match.Groups['Number'].Value
        if (@($references | Where-Object Number -EQ $number).Count -eq 0) {
            $references.Add([pscustomobject]@{ Number = $number; Text = $match.Value })
        }
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
            $errors.Add("Referenced issue #$($reference.Number) does not exist in $RepositoryOwner/$RepositoryName.")
            continue
        }
        $issue = $record[0]
        $hasPullRequestProperty = $null -ne $issue.PSObject.Properties['pull_request']
        if ($hasPullRequestProperty -or [string]$issue.type -eq 'pull_request') {
            $errors.Add("Referenced number #$($reference.Number) is a pull request, not an issue.")
            continue
        }
        if ([string]$issue.state -ne 'open') {
            $errors.Add("Referenced issue #$($reference.Number) is not open; update the PR to an active repository issue.")
            continue
        }
        $resolvedIssues.Add([pscustomobject]@{ Number = $reference.Number; Title = [string]$issue.title; State = [string]$issue.state })
    }
    catch {
        $errors.Add($_.Exception.Message)
    }
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
