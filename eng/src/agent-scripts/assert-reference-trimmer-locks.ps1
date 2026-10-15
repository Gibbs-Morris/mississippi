#!/usr/bin/env pwsh

[CmdletBinding()]
param([Parameter(Mandatory)][string]$RepositoryRoot)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Push-Location -LiteralPath $RepositoryRoot
try {
    $projects = @(& git -c core.quotepath=false ls-files -- '*.csproj')
    if ($LASTEXITCODE -ne 0 -or $projects.Count -eq 0) { throw 'Tracked project inventory is unavailable or empty.' }
    $trackedLocks = @(& git -c core.quotepath=false ls-files -- '**/packages.reference-trimmer.lock.json')
    if ($LASTEXITCODE -ne 0) { throw 'Tracked diagnostic lock inventory is unavailable.' }
    foreach ($project in $projects) {
        $lock = (Join-Path (Split-Path -Parent $project) 'packages.reference-trimmer.lock.json').Replace('\', '/')
        if ($lock -cnotin $trackedLocks -or -not (Test-Path -LiteralPath $lock -PathType Leaf)) {
            throw "Missing version-controlled diagnostic lock input: $lock"
        }
    }
    Write-Output "Verified $($projects.Count) tracked diagnostic lock inputs."
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
} finally {
    Pop-Location
}
exit 0
