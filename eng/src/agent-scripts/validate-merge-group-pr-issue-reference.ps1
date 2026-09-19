#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][AllowEmptyString()][string]$PullRequestsJson,
    [Parameter(Mandatory)][string]$RepositoryOwner,
    [Parameter(Mandatory)][string]$RepositoryName,
    [string]$ValidatorPath = (Join-Path $PSScriptRoot 'validate-pr-issue-reference.ps1'),
    [string]$KnownIssuesJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
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
        $body = if ($null -eq $pullRequest.body) { '' } else { [string]$pullRequest.body }
        Write-Host "Validating merge-group pull request #$($pullRequest.number)"
        $arguments = @('-NoProfile', '-File', $ValidatorPath, '-Body', $body, '-RepositoryOwner', $RepositoryOwner, '-RepositoryName', $RepositoryName, '-Json')
        if (-not [string]::IsNullOrWhiteSpace($KnownIssuesJson)) {
            $arguments += @('-KnownIssuesJson', $KnownIssuesJson)
        }
        $output = & pwsh @arguments 2>&1 | Out-String
        Write-Output $output
        if ($LASTEXITCODE -ne 0) {
            throw "Pull request issue-reference validation failed for #$($pullRequest.number)."
        }
    }

    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
