#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [AllowEmptyString()][string]$PullRequestsJson,
    [string]$PullRequestsPath,
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [string]$ValidatorPath = (Join-Path $PSScriptRoot 'validate-pr-issue-reference.ps1'),
    [string]$KnownIssuesJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-MergeGroupIssueRecord {
    param([Parameter(Mandatory)][int]$Number, [Parameter(Mandatory)][string]$Owner, [Parameter(Mandatory)][string]$Name)

    try {
        $apiOutput = & gh api "repos/$Owner/$Name/issues/$Number" --header 'Accept: application/vnd.github+json' 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            $detail = $apiOutput.Trim()
            if ($detail.Length -gt 512) { $detail = $detail.Substring(0, 512) }
            throw "Unable to resolve issue #$Number through the GitHub API: $detail"
        }
        $decoded = @(ConvertFrom-Json -InputObject $apiOutput)
        if ($decoded.Count -ne 1 -or [int]$decoded[0].number -ne $Number) {
            throw "GitHub returned an invalid identity for issue #$Number."
        }
        $issue = $decoded[0]
        $record = [ordered]@{ number = $Number; title = [string]$issue.title; state = [string]$issue.state }
        if ($null -ne $issue.PSObject.Properties['pull_request']) { $record['pull_request'] = $true }
        $type = $issue.PSObject.Properties['type']
        if ($null -ne $type -and [string]$type.Value -eq 'pull_request') { $record['type'] = 'pull_request' }
        return [pscustomobject]$record
    }
    catch {
        # The single-PR validator retains its existing invalid-reference warning rules.
        return [pscustomobject]@{ number = $Number; error = $_.Exception.Message }
    }
}

function Get-MergeGroupIssueCache {
    param([Parameter(Mandatory)][object[]]$PullRequests, [Parameter(Mandatory)][string]$Owner, [Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$ValidatorPath)

    $issues = @{}
    $referencesByIndex = [System.Collections.Generic.List[object]]::new()
    foreach ($pullRequest in $PullRequests) {
        $body = if ($null -eq $pullRequest.body) { '' } else { [string]$pullRequest.body }
        $output = & pwsh -NoProfile -File $ValidatorPath -Body $body -RepositoryOwner $Owner -RepositoryName $Name -ReferencesOnly -Json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "Pull request issue-reference parsing failed for #$($pullRequest.number): $($output.Trim())"
        }
        $parsed = ConvertFrom-Json -InputObject $output
        if (-not $parsed.Valid -or [string]$parsed.Repository -cne "$Owner/$Name" -or @($parsed.References).Count -gt 20) {
            throw 'Merge-group reference extraction returned invalid repository evidence.'
        }
        $referencesByIndex.Add(@($parsed.References))
        foreach ($reference in $parsed.References) {
            $number = [int]$reference.Number
            if ($number -le 0) { throw 'Merge-group reference extraction returned an invalid issue number.' }
            if (-not $issues.ContainsKey($number)) {
                $issues[$number] = Get-MergeGroupIssueRecord -Number $number -Owner $Owner -Name $Name
            }
        }
    }
    return [pscustomobject]@{ Issues = $issues; ReferencesByIndex = $referencesByIndex }
}

try {
    if (-not [string]::IsNullOrWhiteSpace($PullRequestsPath)) {
        $PullRequestsJson = Get-Content -LiteralPath $PullRequestsPath -Raw -ErrorAction Stop
    }
    if ([string]::IsNullOrWhiteSpace($PullRequestsJson)) { throw 'Merge-group pull-request data was empty.' }
    $decoded = @(ConvertFrom-Json -InputObject $PullRequestsJson)
    $pullRequests = [System.Collections.Generic.List[object]]::new()
    foreach ($page in $decoded) {
        if ($page -is [array]) {
            foreach ($pullRequest in $page) { $pullRequests.Add($pullRequest) }
        }
        else {
            $pullRequests.Add($page)
        }
    }
    if ($pullRequests.Count -eq 0) {
        throw 'Merge-group issue-reference validation could not resolve any constituent pull requests.'
    }
    foreach ($pullRequest in $pullRequests) {
        if ($null -eq $pullRequest.PSObject.Properties['number']) {
            throw 'Merge-group pull-request data is missing a number.'
        }
    }

    $cache = if ([string]::IsNullOrWhiteSpace($KnownIssuesJson)) {
        Get-MergeGroupIssueCache -PullRequests @($pullRequests) -Owner $RepositoryOwner -Name $RepositoryName -ValidatorPath $ValidatorPath
    } else { $null }
    $index = 0
    foreach ($pullRequest in $pullRequests) {
        $body = if ($null -eq $pullRequest.body) { '' } else { [string]$pullRequest.body }
        Write-Host "Validating merge-group pull request #$($pullRequest.number)"
        $arguments = @('-NoProfile', '-File', $ValidatorPath, '-Body', $body, '-RepositoryOwner', $RepositoryOwner, '-RepositoryName', $RepositoryName, '-Json')
        $knownForPullRequest = $KnownIssuesJson
        if ($null -ne $cache) {
            # Keep the CLI payload bounded to this PR's existing twenty-reference limit.
            $records = @($cache.ReferencesByIndex[$index] | ForEach-Object { $cache.Issues[[int]$_.Number] })
            $knownForPullRequest = ConvertTo-Json -InputObject $records -Depth 8 -Compress
        }
        $arguments += @('-KnownIssuesJson', $knownForPullRequest)
        $output = & pwsh @arguments 2>&1 | Out-String
        Write-Output $output
        if ($LASTEXITCODE -ne 0) {
            throw "Pull request issue-reference validation failed for #$($pullRequest.number)."
        }
        $index++
    }

    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
