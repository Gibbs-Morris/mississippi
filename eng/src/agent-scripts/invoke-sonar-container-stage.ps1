#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Prepare','Version','Begin','Build','End')][string]$Phase,
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-CheckedTool {
    param([string]$Executable,[string[]]$Arguments)
    & $Executable @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Sonar $Phase command failed: $Executable." }
}

try {
    New-Item -ItemType Directory -Path '/tmp/home' -Force | Out-Null
    if ($Phase -eq 'Prepare') {
        foreach ($tool in @(@{Name='dotnet-sonarscanner';Version='11.3.0'},@{Name='dotnet-coverage';Version='18.11.0'},@{Name='GitVersion.Tool';Version='6.5.1'})) {
            Invoke-CheckedTool dotnet @('tool','install',$tool.Name,'--version',$tool.Version,'--tool-path','/tools/bin','--configfile','/driver/NuGet.Config')
        }
        Save-Module -Name Pester -RequiredVersion 5.7.1 -Path /tools/modules -Repository PSGallery -Force
        exit 0
    }
    Set-Location /work
    if ($Phase -in @('Version','Build') -and ($env:SONAR_ANALYSIS_TOKEN -or $env:SONAR_TOKEN -or $env:GH_TOKEN -or $env:GITHUB_TOKEN)) { throw 'Credentials cannot enter candidate execution.' }
    if ($Phase -eq 'Version') {
        & /tools/bin/dotnet-gitversion /output json /config /work/GitVersion.yml /nofetch /nonormalize | Set-Content /work/sonar-version.json
        if ($LASTEXITCODE -ne 0) { throw 'Candidate version calculation failed.' }
        exit 0
    }
    if ($Phase -in @('Begin','End')) { New-Item -ItemType Directory -Path '/cache/tmp' -Force | Out-Null }
    if ($Phase -eq 'Begin') {
        if (-not $env:SONAR_ANALYSIS_TOKEN) { throw 'Protected Sonar analysis credential is missing.' }
        $identity = Get-Content /driver/analysis-arguments.json -Raw | ConvertFrom-Json
        $arguments = @('begin','/s:/driver/SonarQube.Analysis.xml','/k:Gibbs-Morris_mississippi','/o:gibbs-morris','/d:sonar.host.url=https://sonarcloud.io',"/d:sonar.token=$($env:SONAR_ANALYSIS_TOKEN)","/v:$Version",'/d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml','/d:sonar.coverageReportPaths=powershell-coverage.xml','/d:sonar.exclusions=**/*.lock.json,**/*.DotSettings,**/packages.lock.json,docs/**','/d:sonar.cpd.exclusions=**/Inlet.Client.Generators/**,**/Inlet.Gateway.Generators/**,**/Inlet.Runtime.Generators/**,**/setup.ps1,**/Setup.Tests.ps1,**/AgentDoctor.Tests.ps1','/d:sonar.issue.ignore.multicriteria=s107','/d:sonar.issue.ignore.multicriteria.s107.ruleKey=csharpsquid:S107','/d:sonar.issue.ignore.multicriteria.s107.resourceKey=**/*.cs') + @($identity)
        Invoke-CheckedTool /tools/bin/dotnet-sonarscanner $arguments
        exit 0
    }
    if ($Phase -eq 'Build') {
        Invoke-CheckedTool dotnet @('restore','/work/mississippi.slnx','--use-lock-file','--locked-mode')
        Invoke-CheckedTool dotnet @('build','/work/mississippi.slnx','--configuration','Release','--no-restore','--no-incremental',"-p:Version=$Version",'-p:CustomBeforeMicrosoftCommonTargets=/work/.sonarqube/bin/Targets/SonarQube.Integration.ImportBefore.targets')
        Invoke-CheckedTool /tools/bin/dotnet-coverage @('collect','pwsh -NoProfile -File /work/eng/src/agent-scripts/test-solution.ps1 -SolutionPath /work/mississippi.slnx -Configuration Release -NoBuild','-f','xml','-o','/work/coverage.xml')
        $env:PSModulePath = "/tools/modules$([IO.Path]::PathSeparator)$($env:PSModulePath)"
        Invoke-CheckedTool pwsh @('-NoProfile','-File','/driver/measure-powershell-coverage.ps1','-RepositoryRoot','/work','-OutputPath','/work/powershell-coverage.xml')
        exit 0
    }
    if (-not $env:SONAR_ANALYSIS_TOKEN) { throw 'Protected Sonar upload credential is missing.' }
    Invoke-CheckedTool /tools/bin/dotnet-sonarscanner @('end',"/d:sonar.token=$($env:SONAR_ANALYSIS_TOKEN)")
    exit 0
}
catch {
    # Native scanner tools mask their token; never print an argument list here.
    [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
    exit 1
}
