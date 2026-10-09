#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Sonar report handoff' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        Import-Module (Join-Path $repoRoot 'eng/src/agent-scripts/SonarReportHandoff.psm1') -Force
        function Write-FixtureFile([string]$Relative,[string]$Content) {
            $path = Join-Path $build $Relative
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
            [IO.File]::WriteAllText($path,$Content)
        }
        function Invoke-Handoff { Copy-ValidatedSonarHandoff -BuildRoot $build -UploadRoot $upload }
        function Add-RazorProjectPair([string]$CoreIndex='0') {
            $reportPath = Join-Path $build '.sonarqube/out/0/ProjectInfo.xml'
            $core = [IO.File]::ReadAllText($reportPath)
            if ($CoreIndex -cne '0') {
                $core = $core.Replace('/work/.sonarqube/out/0',"/work/.sonarqube/out/$CoreIndex").Replace('/work/.sonarqube/conf/0/',"/work/.sonarqube/conf/$CoreIndex/")
                Write-FixtureFile ".sonarqube/out/$CoreIndex/ProjectInfo.xml" $core
                foreach ($name in @('Issues.json','Telemetry.json')) {
                    Write-FixtureFile ".sonarqube/out/$CoreIndex/$name" ([IO.File]::ReadAllText((Join-Path $build ".sonarqube/out/0/$name")))
                }
                Write-FixtureFile ".sonarqube/conf/$CoreIndex/FilesToAnalyze.txt" ([IO.File]::ReadAllText((Join-Path $build '.sonarqube/conf/0/FilesToAnalyze.txt')))
                Remove-Item -LiteralPath $reportPath
            }
            $razor = $core.Replace("/work/.sonarqube/out/$CoreIndex","/work/.sonarqube/out/$CoreIndex.Razor")
            $razor = $razor.Replace("<Property Name=`"sonar.cs.scanner.telemetry`">/work/.sonarqube/out/$CoreIndex.Razor/Telemetry.json</Property>",'')
            Write-FixtureFile ".sonarqube/out/$CoreIndex.Razor/ProjectInfo.xml" $razor
            Write-FixtureFile ".sonarqube/out/$CoreIndex.Razor/Issues.json" ([IO.File]::ReadAllText((Join-Path $build '.sonarqube/out/0/Issues.json')))
        }
        function Change-Project([string]$Old,[string]$New) {
            $path = Join-Path $build '.sonarqube/out/0/ProjectInfo.xml'
            [IO.File]::WriteAllText($path,[IO.File]::ReadAllText($path).Replace($Old,$New))
        }
    }
    BeforeEach {
        $build = Join-Path $TestDrive "build-$([guid]::NewGuid())"
        $upload = Join-Path $TestDrive "upload-$([guid]::NewGuid())"
        [IO.Directory]::CreateDirectory($upload) | Out-Null
        Write-FixtureFile 'Fixture.csproj' '<Project />'
        Write-FixtureFile 'Program.cs' 'class Program {}'
        Write-FixtureFile 'obj/Release/net10.0/apphost' 'generated binary'
        Copy-Item (Join-Path $build 'Fixture.csproj') $upload
        Copy-Item (Join-Path $build 'Program.cs') $upload
        Write-FixtureFile '.sonarqube/conf/0/FilesToAnalyze.txt' "/work/Program.cs`n/work/obj/Release/net10.0/apphost"
        Write-FixtureFile '.sonarqube/out/0/ProjectInfo.xml' '<ProjectInfo xmlns="http://www.sonarsource.com/msbuild/integration/2015/1"><ProjectName>Fixture</ProjectName><ProjectLanguage>C#</ProjectLanguage><ProjectType>Product</ProjectType><ProjectGuid>537d53fc-5c9e-4c86-a5cb-247b0c88f4f8</ProjectGuid><FullPath>/work/Fixture.csproj</FullPath><IsExcluded>false</IsExcluded><AnalysisResultFiles><AnalysisResultFile Id="FilesToAnalyze" Location="/work/.sonarqube/conf/0/FilesToAnalyze.txt" /></AnalysisResultFiles><AnalysisSettings><Property Name="sonar.cs.roslyn.reportFilePaths">/work/.sonarqube/out/0/Issues.json</Property><Property Name="sonar.cs.analyzer.projectOutPaths">/work/.sonarqube/out/0</Property><Property Name="sonar.cs.scanner.telemetry">/work/.sonarqube/out/0/Telemetry.json</Property></AnalysisSettings><Configuration>Release</Configuration><Platform>AnyCPU</Platform><TargetFramework>net10.0</TargetFramework></ProjectInfo>'
        Write-FixtureFile '.sonarqube/out/0/Issues.json' '{"version":"1.0.0","runs":[{"results":[{"locations":[{"resultFile":{"uri":"file:///work/Program.cs"}}]}]}]}'
        Write-FixtureFile '.sonarqube/out/0/Telemetry.json' "{`"dotnetenterprise.s4net.build.deterministic.cnt`":`"true`"}`n{`"dotnetenterprise.s4net.build.netcore_sdk_version`":`"10.0.401`"}"
        Write-FixtureFile '.sonarqube/out/Telemetry.Targets.S4NET.json' '{"dotnetenterprise.s4net.build.msbuild_version":"18.9.11"}'
        Write-FixtureFile 'coverage.xml' '<results />'
        Write-FixtureFile 'powershell-coverage.xml' '<coverage version="1"><file path="eng/src/agent-scripts/Example.psm1"><lineToCover lineNumber="1" covered="true" /></file></coverage>'
        Write-FixtureFile 'eng/src/agent-scripts/Example.psm1' 'Get-Date'
        Copy-Item (Join-Path $build 'eng') $upload -Recurse
    }
    It 'rejects Unix named pipes before opening candidate paths: <Relative>' -Skip:(-not $IsLinux) -TestCases @(
        @{Relative='.sonarqube/out/0/Issues.json'},@{Relative='coverage.xml'},
        @{Relative='.sonarqube/conf/0/FilesToAnalyze.txt'},@{Relative='obj/Release/net10.0/apphost'}
    ) {
        param($Relative)
        $pipe=Join-Path $build $Relative
        Remove-Item -LiteralPath $pipe
        & /usr/bin/mkfifo -- $pipe
        if($LASTEXITCODE -ne 0){throw 'FIFO fixture creation failed.'}
        {InModuleScope SonarReportHandoff -Parameters @{Path=$pipe;Root=$build} {param($Path,$Root) Assert-SonarRegularPath -Path $Path -Root $Root}} | Should -Throw '*regular file*'
    }
    It 'rejects Unix sockets as candidate files' -Skip:(-not $IsLinux) {
        $path=Join-Path $TestDrive 'report.socket'
        $socket=[Net.Sockets.Socket]::new([Net.Sockets.AddressFamily]::Unix,[Net.Sockets.SocketType]::Stream,[Net.Sockets.ProtocolType]::Unspecified)
        try{
            $socket.Bind([Net.Sockets.UnixDomainSocketEndPoint]::new($path))
            {InModuleScope SonarReportHandoff -Parameters @{Path=$path;Root=$TestDrive} {param($Path,$Root) Assert-SonarRegularPath -Path $Path -Root $Root}} | Should -Throw '*regular file*'
        }finally{$socket.Dispose()}
    }
    It 'accepts real scanner shapes, JSON-lines telemetry, generated inputs and source already present' {
        $manifest = @(Invoke-Handoff)
        $manifest.Path | Should -Contain 'obj/Release/net10.0/apphost'
        $manifest.Path | Should -Contain '.sonarqube/out/0/ProjectInfo.xml'
        $manifest.Path | Should -Not -Contain '.sonarqube/conf/SonarQubeAnalysisConfig.xml'
        $manifest | Where-Object { $_.Sha256 -notmatch '^[0-9A-F]{64}$' } | Should -BeNullOrEmpty
        (Get-Content (Join-Path $upload 'obj/Release/net10.0/apphost') -Raw).Trim() | Should -Be 'generated binary'
    }
    It 'accepts scanner Roslyn report paths: <Case>' -TestCases @(
        @{Case='pipe-delimited reports';Names=@('Issues.json','Issues2.json')},
        @{Case='literal comma in report filename';Names=@('Issues,generated.json')}
    ) {
        param($Names)
        $report = Get-Content (Join-Path $build '.sonarqube/out/0/Issues.json') -Raw
        foreach ($name in $Names) { Write-FixtureFile ".sonarqube/out/0/$name" $report }
        $locations = @($Names | ForEach-Object { "/work/.sonarqube/out/0/$_" }) -join '|'
        Change-Project '/work/.sonarqube/out/0/Issues.json' $locations
        $manifest = @(Invoke-Handoff)
        foreach ($name in $Names) { $manifest.Path | Should -Contain ".sonarqube/out/0/$name" }
    }
    It 'rejects unsafe report-list members and lists in single-path settings: <Case>' -TestCases @(
        @{Case='outside workspace';Old='/work/.sonarqube/out/0/Issues.json';Value='/work/.sonarqube/out/0/Issues.json|/etc/private.json'},
        @{Case='different project';Old='/work/.sonarqube/out/0/Issues.json';Value='/work/.sonarqube/out/0/Issues.json|/work/.sonarqube/out/1/Issues.json'},
        @{Case='empty list member';Old='/work/.sonarqube/out/0/Issues.json';Value='/work/.sonarqube/out/0/Issues.json|'},
        @{Case='telemetry list';Old='/work/.sonarqube/out/0/Telemetry.json';Value='/work/.sonarqube/out/0/Telemetry.json|/work/.sonarqube/out/0/Issues.json'},
        @{Case='analyzer-output list';Old='/work/.sonarqube/out/0';Value='/work/.sonarqube/out/0|/work/.sonarqube/out/1'}
    ) {
        param($Old,$Value)
        Change-Project ">${Old}</Property>" ">${Value}</Property>"
        { Invoke-Handoff } | Should -Throw
    }
    It 'accepts excluded projects with the scanner telemetry setting only' {
        Change-Project '<IsExcluded>false</IsExcluded>' '<IsExcluded>true</IsExcluded>'
        Change-Project '<Property Name="sonar.cs.roslyn.reportFilePaths">/work/.sonarqube/out/0/Issues.json</Property>' ''
        Change-Project '<Property Name="sonar.cs.analyzer.projectOutPaths">/work/.sonarqube/out/0</Property>' ''
        { Invoke-Handoff } | Should -Not -Throw
    }
    It 'accepts core and Razor companions with shared project identity and source list: <CoreIndex>' -TestCases @(
        @{CoreIndex='0'},@{CoreIndex='0_1'}
    ) {
        param($CoreIndex)
        Add-RazorProjectPair -CoreIndex $CoreIndex
        $manifest = @(Invoke-Handoff)
        $manifest.Path | Should -Contain ".sonarqube/out/$CoreIndex/ProjectInfo.xml"
        $manifest.Path | Should -Contain ".sonarqube/out/$CoreIndex.Razor/ProjectInfo.xml"
        $manifest.Path | Should -Contain ".sonarqube/conf/$CoreIndex/FilesToAnalyze.txt"
        $manifest.Path | Should -Not -Contain ".sonarqube/conf/$CoreIndex.Razor/FilesToAnalyze.txt"
    }
    It 'rejects unsupported project-output suffixes: <Index>' -TestCases @(
        @{Index='0.tmp'},@{Index='0.razor'},@{Index='0.Razor.tmp'},@{Index='0.Razor_1'}
    ) {
        param($Index)
        Write-FixtureFile ".sonarqube/out/$Index/ProjectInfo.xml" ([IO.File]::ReadAllText((Join-Path $build '.sonarqube/out/0/ProjectInfo.xml')))
        Remove-Item -LiteralPath (Join-Path $build '.sonarqube/out/0/ProjectInfo.xml')
        { Invoke-Handoff } | Should -Throw '*output directory*'
    }
    It 'rejects duplicate reports of the same compilation kind: <Kind>' -TestCases @(
        @{Kind='core';Index='0';Duplicate='1'},@{Kind='Razor';Index='0.Razor';Duplicate='1.Razor'}
    ) {
        param($Kind,$Index,$Duplicate)
        if ($Kind -ceq 'Razor') { Add-RazorProjectPair }
        $report = [IO.File]::ReadAllText((Join-Path $build ".sonarqube/out/$Index/ProjectInfo.xml"))
        Write-FixtureFile ".sonarqube/out/$Duplicate/ProjectInfo.xml" $report
        { Invoke-Handoff } | Should -Throw '*Duplicate Sonar project identity*'
    }
    It 'rejects malicious or malformed reports: <Case>' -TestCases @(
        @{Case='setting name'},@{Case='setting outside project'},@{Case='duplicate setting'},@{Case='project outside workspace'},@{Case='project traversal'},
        @{Case='unknown XML element'},@{Case='wrong language'},@{Case='result path'},@{Case='source traversal'},@{Case='source Git path'},@{Case='source scanner config'},
        @{Case='SARIF outside workspace'},@{Case='DTD'},@{Case='unexpected executable'},@{Case='malformed JSON'},@{Case='source replacement'},@{Case='same workspace'},
        @{Case='coverage traversal'},@{Case='coverage invalid line'},@{Case='missing project'},@{Case='duplicate XML element'},@{Case='XML setting attribute'},@{Case='unapproved telemetry key'}
    ) {
        param($Case)
        switch ($Case) {
            'setting name' { Change-Project 'sonar.cs.roslyn.reportFilePaths' 'sonar.scanner.dumpToFile' }
            'setting outside project' { Change-Project '/work/.sonarqube/out/0/Issues.json' '/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml' }
            'duplicate setting' { Change-Project '</AnalysisSettings>' '<Property Name="sonar.cs.scanner.telemetry">/work/.sonarqube/out/0/Telemetry.json</Property></AnalysisSettings>' }
            'project outside workspace' { Change-Project '/work/Fixture.csproj' '/etc/Fixture.csproj' }
            'project traversal' { Change-Project '/work/Fixture.csproj' '/work/../Fixture.csproj' }
            'unknown XML element' { Change-Project '</ProjectInfo>' '<Executable>/tmp/payload</Executable></ProjectInfo>' }
            'wrong language' { Change-Project '<ProjectLanguage>C#</ProjectLanguage>' '<ProjectLanguage>Java</ProjectLanguage>' }
            'result path' { Change-Project '/work/.sonarqube/conf/0/FilesToAnalyze.txt' '/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml' }
            'source traversal' { Write-FixtureFile '.sonarqube/conf/0/FilesToAnalyze.txt' '/work/../secret' }
            'source Git path' { Write-FixtureFile '.git/config' 'private'; Write-FixtureFile '.sonarqube/conf/0/FilesToAnalyze.txt' '/work/.git/config' }
            'source scanner config' { Write-FixtureFile '.sonarqube/conf/SonarQubeAnalysisConfig.xml' 'private'; Write-FixtureFile '.sonarqube/conf/0/FilesToAnalyze.txt' '/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml' }
            'SARIF outside workspace' { Write-FixtureFile '.sonarqube/out/0/Issues.json' '{"runs":[{"results":[{"locations":[{"resultFile":{"uri":"file:///etc/passwd"}}]}]}]}' }
            'DTD' { Write-FixtureFile 'coverage.xml' '<!DOCTYPE results [<!ENTITY e SYSTEM "file:///etc/passwd">]><results>&e;</results>' }
            'unexpected executable' { Write-FixtureFile '.sonarqube/out/0/payload.exe' 'executable' }
            'malformed JSON' { Write-FixtureFile '.sonarqube/out/0/Issues.json' '{broken' }
            'source replacement' { Write-FixtureFile 'Program.cs' 'changed' }
            'same workspace' { $upload = $build }
            'coverage traversal' { Write-FixtureFile 'powershell-coverage.xml' '<coverage version="1"><file path="../private"><lineToCover lineNumber="1" covered="true" /></file></coverage>' }
            'coverage invalid line' { Write-FixtureFile 'powershell-coverage.xml' '<coverage version="1"><file path="eng/src/agent-scripts/Example.psm1"><lineToCover lineNumber="-1" covered="maybe" /></file></coverage>' }
            'missing project' { Remove-Item (Join-Path $build '.sonarqube/out/0/ProjectInfo.xml') }
            'duplicate XML element' { Change-Project '</ProjectInfo>' '<FullPath>/work/Fixture.csproj</FullPath></ProjectInfo>' }
            'XML setting attribute' { Change-Project 'Name="sonar.cs.scanner.telemetry"' 'Name="sonar.cs.scanner.telemetry" Executable="payload"' }
            'unapproved telemetry key' { Write-FixtureFile '.sonarqube/out/0/Telemetry.json' '{"sonar.scanner.javaExePath":"/tmp/payload"}' }
        }
        { Invoke-Handoff } | Should -Throw
    }
    It 'rejects linked output directories before traversing their contents' {
        $external = Join-Path $TestDrive 'external'; [IO.Directory]::CreateDirectory($external) | Out-Null
        New-Item -ItemType SymbolicLink -Path (Join-Path $build '.sonarqube/out/link') -Target $external | Out-Null
        { Invoke-Handoff } | Should -Throw '*symbolic links*'
    }
    It 'rejects linked destination ancestors before writing any report there' {
        $external = Join-Path $TestDrive 'destination-external'; [IO.Directory]::CreateDirectory($external) | Out-Null
        New-Item -ItemType SymbolicLink -Path (Join-Path $upload '.sonarqube') -Target $external | Out-Null
        { Invoke-Handoff } | Should -Throw '*symbolic links*'
        @(Get-ChildItem $external -Recurse).Count | Should -Be 0
    }
}
