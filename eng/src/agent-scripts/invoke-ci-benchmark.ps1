#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('mississippi.slnx', 'samples.slnx')][string]$Solution,
    [Parameter(Mandatory)][ValidateSet('Inventory', 'Restore', 'Build', 'L0', 'L1', 'Aspire', 'L2', 'Summary')][string]$Stage,
    [string]$BuildVersion,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '../../..')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RepositoryAutomation.psm1') -Force
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$evidenceRoot = Join-Path $RepoRoot 'artifacts/ci-benchmark'
$null = New-Item -ItemType Directory -Path $evidenceRoot -Force
$record = [ordered]@{ stage = $Stage; status = 'FAIL'; startedUtc = [DateTime]::UtcNow.ToString('o'); error = $null }
$timer = [System.Diagnostics.Stopwatch]::StartNew()
Push-Location $RepoRoot
try {
    switch ($Stage) {
        Inventory {
            $revision = (& git rev-parse HEAD | Out-String).Trim()
            if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the checked-out revision.' }
            $sdk = (Invoke-RepositoryProcess -FilePath dotnet -Arguments @('--version') -SuppressCommandEcho | Out-String).Trim()
            [xml]$packages = Get-Content -LiteralPath 'Directory.Packages.props' -Raw
            $aspireVersion = ($packages.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Aspire.Hosting.AppHost').Version
            if ([string]::IsNullOrWhiteSpace($aspireVersion)) { throw 'Aspire version is missing.' }
            $metadata = [ordered]@{
                revision = $revision; event = $env:GITHUB_EVENT_NAME; runId = $env:GITHUB_RUN_ID; attempt = $env:GITHUB_RUN_ATTEMPT
                solution = $Solution; sdk = $sdk; configuration = 'Release'; gitVersionTool = '6.5.1'; aspireVersion = $aspireVersion
                image = $env:ImageOS; imageVersion = $env:ImageVersion; runner = $env:RUNNER_NAME; architecture = $env:RUNNER_ARCH
                projects = @(Get-TestProjects -SolutionPath $Solution | ForEach-Object { [IO.Path]::GetRelativePath($RepoRoot, $_).Replace('\', '/') })
                browserCache = 'OMITTED: no demonstrated transfer saving; matching Chromium is installed after L2'
            }
            $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'metadata.json') -Encoding utf8
            @("aspire-version=$aspireVersion", "sdk=$sdk") | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            $metadata | ConvertTo-Json -Depth 5 | Write-Host
        }
        Restore {
            Invoke-RepositoryProcess -FilePath dotnet -Arguments @('restore', $Solution, '--locked-mode') | Out-Host
        }
        Build {
            Invoke-CiBenchmarkBuild -RepoRoot $RepoRoot -Solution $Solution -BuildVersion $BuildVersion -ReceiptPath (Join-Path $evidenceRoot 'build.json')
            if ($Solution -eq 'samples.slnx') {
                $assets = Get-Content -LiteralPath 'samples/Spring/Spring.L3Tests/obj/project.assets.json' -Raw | ConvertFrom-Json -AsHashtable
                $playwright = @($assets.libraries.Keys | Where-Object { $_ -like 'Microsoft.Playwright/*' })
                if ($playwright.Count -ne 1) { throw 'Expected one resolved Playwright version.' }
                $record.playwright = $playwright[0]
            }
        }
        Aspire {
            Invoke-DotnetToolRestore -RepoRoot $RepoRoot
            $toolPath = Initialize-AspireCli -RepoRoot $RepoRoot
            $toolPath | Out-File -FilePath $env:GITHUB_PATH -Append -Encoding utf8
        }
        { $_ -in @('L0', 'L1', 'L2') } {
            $record.tests = Invoke-CiBenchmarkTestLevel -SolutionPath $Solution -Level $Stage -ResultsRoot (Join-Path $evidenceRoot 'test-results')
            $record.status = $record.tests.status
            $record.tests | ConvertTo-Json -Depth 5 | Write-Host
        }
        Summary {
            $phases = @(Get-ChildItem -LiteralPath $evidenceRoot -Filter 'phase-*.json' | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
            $caches = [ordered]@{ nugetExactHit = $env:NUGET_CACHE_HIT; gitVersionExactHit = $env:GITVERSION_CACHE_HIT; aspireExactHit = $env:ASPIRE_CACHE_HIT; browser = 'not cached' }
            $caches | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidenceRoot 'caches.json') -Encoding utf8
            $lines = @("### Experimental $Solution validation", '', '| Phase | Status | Execution seconds |', '| --- | --- | ---: |')
            $lines += @($phases | Sort-Object startedUtc | ForEach-Object { "| $($_.stage) | $($_.status) | $($_.seconds) |" })
            $lines += @('', "Cache exact hits: NuGet=$($env:NUGET_CACHE_HIT), GitVersion=$($env:GITVERSION_CACHE_HIT), Aspire=$($env:ASPIRE_CACHE_HIT). Browser caching omitted.", '', 'Runner waiting is excluded. Job totals and action transfer/upload/cleanup times come from the jobs/attempt API; these command phases do not cover the whole job.')
            $lines | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
        }
    }
    if ($record.status -eq 'FAIL') { $record.status = 'PASS' }
}
catch {
    $record.error = $_.Exception.Message
    Write-Error $_ -ErrorAction Continue
    exit 1
}
finally {
    $record.completedUtc = [DateTime]::UtcNow.ToString('o')
    $record.seconds = [Math]::Round($timer.Elapsed.TotalSeconds, 3)
    $record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot "phase-$Stage.json") -Encoding utf8
    Pop-Location
}
exit 0
