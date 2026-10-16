#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

Describe 'Trusted Sonar container stage execution' {
    BeforeAll {
        $repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $driver=Join-Path $repoRoot 'eng/src/agent-scripts/invoke-sonar-container-stage.ps1'
        $fixture=Join-Path $PSScriptRoot 'fixtures/invoke-sonar-stage-fixture.ps1'
        $powerShellPath=(Get-Process -Id $PID).Path
        function Invoke-StageFixture {
            param([string]$Phase,[string]$FailAt='',[string]$TokenName='None')
            $capture=Join-Path $TestDrive ([guid]::NewGuid().ToString('N')+'.json')
            $arguments=@('-NoProfile','-NonInteractive','-File',$fixture,'-DriverPath',$driver,'-Phase',$Phase,'-CapturePath',$capture,'-TokenName',$TokenName)
            if($FailAt){$arguments+=@('-FailAt',$FailAt)}
            $output=& $powerShellPath @arguments 2>&1|Out-String
            $code=$LASTEXITCODE
            $trace=Get-Content -LiteralPath $capture -Raw|ConvertFrom-Json
            [pscustomobject]@{Exit=$code;Output=$output;Trace=$trace;Native=@($trace.Calls|Where-Object Executable -In @('dotnet','pwsh','/tools/bin/dotnet-gitversion','/tools/bin/dotnet-sonarscanner','/tools/bin/dotnet-coverage'))}
        }
    }

    It 'executes the actual <Phase> dispatcher commands' -TestCases @(
        @{Phase='Prepare'},@{Phase='Version'},@{Phase='Begin'},@{Phase='Build'},@{Phase='End'}
    ) {
        param($Phase)
        $token=if($Phase -in @('Begin','End')){'SONAR_ANALYSIS_TOKEN'}else{'None'}
        $result=Invoke-StageFixture -Phase $Phase -TokenName $token
        $result.Exit | Should -Be 0
        $result.Output | Should -Not -Match 'fixture-credential'
        @($result.Trace.Calls|Where-Object Executable -EQ New-Item)[0].Arguments | Should -Be @('Directory','/tmp/home','True')
        switch($Phase){
            'Prepare' {
                $result.Native.Count | Should -Be 3
                $tools=@(@{Name='dotnet-sonarscanner';Version='11.3.0'},@{Name='dotnet-coverage';Version='18.11.0'},@{Name='GitVersion.Tool';Version='6.5.1'})
                for($index=0;$index -lt $tools.Count;$index++){
                    $result.Native[$index].Executable | Should -Be dotnet
                    $result.Native[$index].Arguments | Should -Be @('tool','install',$tools[$index].Name,'--version',$tools[$index].Version,'--tool-path','/tools/bin','--configfile','/driver/NuGet.Config')
                }
                @($result.Trace.Calls|Where-Object Executable -EQ Save-Module)[0].Arguments | Should -Be @('Pester','5.7.1','/tools/modules','PSGallery','True')
            }
            'Version' {
                $result.Native.Count | Should -Be 1
                $result.Native[0].Executable | Should -Be '/tools/bin/dotnet-gitversion'
                $result.Native[0].Arguments | Should -Be @('/output','json','/config','/work/GitVersion.yml','/nofetch','/nonormalize')
                @($result.Trace.Calls|Where-Object Executable -EQ Set-Content)[0].Arguments | Should -Be @('/work/sonar-version.json','{"SemVer":"3.4.5"}')
            }
            'Begin' {
                $result.Native.Count | Should -Be 1
                $result.Native[0].Executable | Should -Be '/tools/bin/dotnet-sonarscanner'
                $result.Native[0].Arguments[0] | Should -Be begin
                foreach($argument in @('/s:/driver/SonarQube.Analysis.xml','/d:sonar.token=fixture-credential','/v:1.2.3','/d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml','/d:sonar.coverageReportPaths=powershell-coverage.xml','/d:sonar.branch.name=fixture-source',('/d:sonar.scm.revision='+('a'*40)))){
                    $result.Native[0].Arguments | Should -Contain $argument
                }
                @($result.Trace.Calls|Where-Object Executable -EQ New-Item)[1].Arguments | Should -Be @('Directory','/cache/tmp','True')
            }
            'Build' {
                $result.Native.Count | Should -Be 4
                $result.Native[0].Arguments | Should -Be @('restore','/work/mississippi.slnx','--use-lock-file','--locked-mode')
                $result.Native[1].Arguments | Should -Be @('build','/work/mississippi.slnx','--configuration','Release','--no-restore','--no-incremental','-p:Version=1.2.3','-p:CustomBeforeMicrosoftCommonTargets=/work/.sonarqube/bin/Targets/SonarQube.Integration.ImportBefore.targets')
                $result.Native[2].Executable | Should -Be '/tools/bin/dotnet-coverage'
                $result.Native[2].Arguments | Should -Be @('collect','pwsh -NoProfile -File /work/eng/src/agent-scripts/test-solution.ps1 -SolutionPath /work/mississippi.slnx -Configuration Release -NoBuild','-f','xml','-o','/work/coverage.xml')
                $result.Native[3].Executable | Should -Be pwsh
                $result.Native[3].Arguments | Should -Be @('-NoProfile','-File','/driver/measure-powershell-coverage.ps1','-RepositoryRoot','/work','-OutputPath','/work/powershell-coverage.xml')
                $result.Trace.ModulePath.StartsWith('/tools/modules'+[IO.Path]::PathSeparator) | Should -BeTrue
            }
            'End' {
                $result.Native.Count | Should -Be 1
                $result.Native[0].Executable | Should -Be '/tools/bin/dotnet-sonarscanner'
                $result.Native[0].Arguments | Should -Be @('end','/d:sonar.token=fixture-credential')
                @($result.Trace.Calls|Where-Object Executable -EQ New-Item)[1].Arguments | Should -Be @('Directory','/cache/tmp','True')
            }
        }
    }

    It 'stops <Phase> after native command <Index> fails' -TestCases @(
        @{Phase='Prepare';Index=1},@{Phase='Prepare';Index=2},@{Phase='Prepare';Index=3},
        @{Phase='Version';Index=1},@{Phase='Begin';Index=1},
        @{Phase='Build';Index=1},@{Phase='Build';Index=2},@{Phase='Build';Index=3},@{Phase='Build';Index=4},
        @{Phase='End';Index=1}
    ) {
        param($Phase,$Index)
        $token=if($Phase -in @('Begin','End')){'SONAR_ANALYSIS_TOKEN'}else{'None'}
        $result=Invoke-StageFixture -Phase $Phase -TokenName $token -FailAt "native-$Index"
        $result.Exit | Should -Be 1
        $result.Trace.NativeCount | Should -Be $Index
        $result.Output | Should -Match 'failed'
        $result.Output | Should -Not -Match 'fixture-credential'
        if($Phase -eq 'Prepare'){@($result.Trace.Calls|Where-Object Executable -EQ Save-Module).Count | Should -Be 0}
    }

    It 'fails Prepare when the pinned Pester download fails' {
        $result=Invoke-StageFixture -Phase Prepare -FailAt Save-Module
        $result.Exit | Should -Be 1
        $result.Trace.NativeCount | Should -Be 3
        $result.Output | Should -Match 'Fixture module download failed'
    }

    It 'rejects <Token> before candidate <Phase> tools run' -TestCases @(
        foreach($phase in @('Version','Build')){
            foreach($token in @('SONAR_ANALYSIS_TOKEN','SONAR_TOKEN','GH_TOKEN','GITHUB_TOKEN')){@{Phase=$phase;Token=$token}}
        }
    ) {
        param($Phase,$Token)
        $result=Invoke-StageFixture -Phase $Phase -TokenName $Token
        $result.Exit | Should -Be 1
        $result.Trace.NativeCount | Should -Be 0
        $result.Output | Should -Match 'Credentials cannot enter candidate execution'
        $result.Output | Should -Not -Match 'fixture-credential'
    }

    It 'rejects missing credentials before protected <Phase> tools run' -TestCases @(@{Phase='Begin'},@{Phase='End'}) {
        param($Phase)
        $result=Invoke-StageFixture -Phase $Phase
        $result.Exit | Should -Be 1
        $result.Trace.NativeCount | Should -Be 0
        $result.Output | Should -Match 'credential is missing'
    }
}
