#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1,[long]::MaxValue)][long]$SourceRunId,
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$DefaultBranch,
    [switch]$IntakeOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    Import-Module (Join-Path $PSScriptRoot 'MergeGroupIssueReference.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'TrustedSonarAnalysis.psm1') -Force
    $checkout = (& git rev-parse HEAD | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Trusted controller checkout is missing.' }
    Assert-TrustedSonarControllerOrigin -Repository $Repository -DefaultBranch $DefaultBranch -WorkflowRef $env:GITHUB_WORKFLOW_REF -WorkflowSha $env:GITHUB_WORKFLOW_SHA -CheckoutSha $checkout
    Assert-SonarCredentialDeployment -Repository $Repository -DefaultBranch $DefaultBranch
    $source = Get-TrustedSonarSource -Repository $Repository -RunId $SourceRunId -DefaultBranch $DefaultBranch
    if ($IntakeOnly) {
        if ($env:GLOBAL_SONAR_TOKEN_PRESENT -cne 'false' -or $env:GLOBAL_ANALYSIS_TOKEN_PRESENT -cne 'false') { throw 'Move Sonar credentials out of repository and organization secret scope before activation.' }
        "source-run-id=$SourceRunId" | Add-Content -LiteralPath $env:GITHUB_OUTPUT
        exit 0
    }
    if (-not $IsLinux -or -not $env:SONAR_ANALYSIS_TOKEN) { throw 'Protected Sonar analysis requires Linux and its environment credential.' }
    Import-Module (Join-Path $PSScriptRoot 'SonarReportHandoff.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'SonarContainerRuntime.psm1') -Force
    $startedAt = [datetimeoffset]::UtcNow
    $policy = Get-SonarQualityPolicySnapshot -Source $source
    $root = Join-Path $env:RUNNER_TEMP "trusted-sonar-$([guid]::NewGuid().ToString('N'))"
    $tools = Join-Path $root 'tools'; $driver = Join-Path $root 'driver'; $cache = Join-Path $root 'cache'
    $projection = Join-Path $root 'analyzer-projection'
    $packages = Join-Path $root 'build-packages'
    $build = Join-Path $root 'build'; $upload = Join-Path $root 'upload'
    foreach ($path in @($tools,$driver,$cache,$packages,$projection,(Join-Path $cache 'tmp'))) { [IO.Directory]::CreateDirectory($path) | Out-Null }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'invoke-sonar-container-stage.ps1'),(Join-Path $PSScriptRoot 'measure-powershell-coverage.ps1') -Destination $driver
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../../SonarQube.Analysis.xml') -Destination $driver
    '<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' | Set-Content -LiteralPath (Join-Path $driver 'NuGet.Config')
    @(Get-TrustedSonarAnalysisArguments -Source $source) | ConvertTo-Json | Set-Content (Join-Path $driver 'analysis-arguments.json')
    $branch = if ($source.Mode -ceq 'PullRequest') { "pull/$($source.PullRequest)/merge" } else { $source.HeadRef }
    Initialize-SonarSourceWorkspace -Path $build -Repository $Repository -Revision $source.BuildSha -Branch $branch -DefaultBranch $DefaultBranch
    Initialize-SonarSourceWorkspace -Path $upload -Repository $Repository -Revision $source.BuildSha -Branch $branch -DefaultBranch $DefaultBranch
    $userId = [int](& id -u)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine runner user identity.' }
    $groupId = [int](& id -g)
    if ($LASTEXITCODE -ne 0 -or $userId -lt 1 -or $groupId -lt 1) { throw 'Sonar containers must use a non-root runner identity.' }
    $container = @{Tools=$tools;Driver=$driver;Cache=$cache;UserId=$userId;GroupId=$groupId}
    Invoke-SonarContainer @container -Phase Prepare
    Invoke-SonarContainer @container -Workspace $build -Phase Version
    $version = (Get-Content (Join-Path $build 'sonar-version.json') -Raw | ConvertFrom-Json).SemVer
    if ($version -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$') { throw 'Invalid calculated Sonar project version.' }
    Invoke-SonarContainer @container -Workspace $upload -Phase Begin -Version $version
    $scanner = Join-Path $upload '.sonarqube'
    Assert-SonarSourceWorkspaceClean -Path $upload -Revision $source.BuildSha
    Assert-SonarAssetCredentialAbsent -Directory $scanner -Token $env:SONAR_ANALYSIS_TOKEN
    [IO.Directory]::CreateDirectory((Join-Path $build '.sonarqube/conf')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $build '.sonarqube/bin')) | Out-Null
    $analyzerMounts = @(Copy-SonarAnalyzerProjection -Scanner $scanner -PrivateCache $cache -Projection $projection -BuildRoot $build -Token $env:SONAR_ANALYSIS_TOKEN)
    Invoke-SonarContainer @container -Workspace $build -Phase Build -Version $version -ScannerAssets $scanner -BuildPackages $packages -AnalyzerMounts $analyzerMounts
    Assert-SonarSourceWorkspaceClean -Path $upload -Revision $source.BuildSha
    $manifest = @(Copy-ValidatedSonarHandoff -BuildRoot $build -UploadRoot $upload)
    $manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $root 'validated-handoff.json')
    Assert-SonarSourceWorkspaceClean -Path $upload -Revision $source.BuildSha
    $latest = Get-TrustedSonarSource -Repository $Repository -RunId $SourceRunId -DefaultBranch $DefaultBranch
    Assert-TrustedSonarSourceUnchanged -Before $source -After $latest
    Assert-SonarCredentialDeployment -Repository $Repository -DefaultBranch $DefaultBranch
    $currentPolicy = Get-SonarQualityPolicySnapshot -Source $latest
    Assert-SonarQualityPolicyUnchanged -Before $policy -After $currentPolicy
    Invoke-SonarContainer @container -Workspace $upload -Phase End
    Assert-SonarPublishedAnalysis -Source $latest -Repository $Repository -StartedAt $startedAt
    $publishedPolicy = Get-SonarQualityPolicySnapshot -Source $latest
    Assert-SonarQualityPolicyUnchanged -Before $policy -After $publishedPolicy
    Write-Host "Sonar analysis completed for $($source.Mode) revision $($source.HeadSha)."
    exit 0
}
catch {
    [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
    exit 1
}
