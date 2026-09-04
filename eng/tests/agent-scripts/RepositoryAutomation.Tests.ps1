#!/usr/bin/env pwsh
#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$modulePath = [System.IO.Path]::GetFullPath($modulePath)
Import-Module -Name $modulePath -Force

Describe 'RepositoryAutomation helpers' {
    It 'exports the display helper with the Show verb' {
        (Get-Command Show-AutomationBanner -Module RepositoryAutomation).Name | Should -Be 'Show-AutomationBanner'
    }
    It 'resolves repository root from test path' {
        $root = Get-RepositoryRoot -StartPath $PSScriptRoot
        Test-Path -LiteralPath (Join-Path $root '.git') | Should -Be $true
    }

    It 'creates automation run directories' {
        $testRoot = Join-Path $TestDrive 'runs'
        $runDirectory = New-AutomationRunDirectory -Root $testRoot -Prefix 'test'
        Test-Path -LiteralPath $runDirectory | Should -Be $true
        ($runDirectory -like (Join-Path $testRoot 'test-*')) | Should -Be $true
    }

    It 'invokes automation steps and returns the result' {
        $result = Invoke-AutomationStep -Name 'Sample' -SilentSuccess -Action { 1 + 1 }
        $result | Should -Be 2
    }
}

InModuleScope RepositoryAutomation {
    BeforeAll {
        function New-TestRepository {
            $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
            $null = New-Item -ItemType Directory -Path (Join-Path $root '.git') -Force
            $null = New-Item -ItemType Directory -Path (Join-Path $root 'src') -Force
            '{"stryker-config":{"thresholds":{"high":80,"low":80,"break":80}}}' | Set-Content (Join-Path $root 'stryker-config.json')
            return $root
        }

        function New-TestProject {
            param([string]$Root, [string]$Path, [string[]]$References)
            $fullPath = Join-Path $Root $Path
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $fullPath) -Force
            $items = ($References | ForEach-Object { '<ProjectReference Include="{0}" />' -f $_ }) -join ''
            "<Project Sdk=`"Microsoft.NET.Sdk`"><ItemGroup>$items</ItemGroup></Project>" | Set-Content -LiteralPath $fullPath
            return $fullPath
        }

        function Write-TestMutationReport {
            param([string]$OutputPath, [string[]]$Statuses = @('Killed'))
            $reports = Join-Path $OutputPath 'reports'
            $null = New-Item -ItemType Directory -Path $reports -Force
            $mutants = @($Statuses | ForEach-Object { @{ status = $_; id = [string]([guid]::NewGuid()) } })
            @{ schemaVersion = '1'; files = @{ 'Example.cs' = @{ language = 'cs'; source = 'class Example {}'; mutants = $mutants } } } |
                ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $reports 'mutation-report.json')
        }
    }

    Describe 'Run directory paths' {
        It 'resolves relative run roots before a child changes its working directory' {
            Push-Location $TestDrive
            try {
                $path = New-AutomationRunDirectory -Root 'relative-runs'
                [System.IO.Path]::IsPathRooted($path) | Should -BeTrue
                $path | Should -BeLike (Join-Path $TestDrive 'relative-runs/*')
            }
            finally { Pop-Location }
        }
    }

    Describe 'Solution membership' {
        BeforeEach {
            $script:repository = New-TestRepository
            $script:unitProject = New-TestProject $repository 'tests/Example.L0Tests/Example.L0Tests.csproj'
            $null = New-TestProject $repository 'samples/Unlisted.L0Tests/Unlisted.L0Tests.csproj'
            $script:integrationProject = New-TestProject $repository 'tests/Example.L2Tests/Example.L2Tests.csproj'
        }

        It 'reads only listed slnx members and filters test levels' {
            $solution = Join-Path $repository 'example.slnx'
            '<Solution><Folder Name="/Tests/"><Project Path="tests\Example.L0Tests\Example.L0Tests.csproj"/><Project Path="tests/Example.L2Tests/Example.L2Tests.csproj"/></Folder></Solution>' | Set-Content $solution
            $projects = @(Get-TestProjects $solution -TestLevels @('L0Tests', 'L1Tests'))
            $projects.Count | Should -Be 1
            $projects[0] | Should -Be $unitProject
            @(Get-TestProjects $solution).Count | Should -Be 2
        }

        It 'reads generated sln membership without discovering unlisted sample tests' {
            $solution = Join-Path $repository 'example.sln'
            'Project("{GUID}") = "Example.L0Tests", "tests\Example.L0Tests\Example.L0Tests.csproj", "{GUID}"' | Set-Content $solution
            $projects = @(Get-TestProjects $solution)
            $projects.Count | Should -Be 1
            $projects[0] | Should -Be $unitProject
        }

        It 'handles an empty solution' {
            $solution = Join-Path $repository 'example.slnx'
            '<Solution />' | Set-Content $solution
            @(Get-TestProjects $solution).Count | Should -Be 0
        }

        It 'rejects missing listed test projects' {
            $solution = Join-Path $repository 'example.slnx'
            '<Solution><Project Path="tests/Missing.L0Tests.csproj"/></Solution>' | Set-Content $solution
            { Get-TestProjects $solution } | Should -Throw
        }
    }

    Describe 'Mutation source selection' {
        BeforeEach { $script:repository = New-TestRepository }

        It 'selects the matching source stem among multiple dependencies' {
            $source = New-TestProject $repository 'src/Example/Example.csproj'
            $null = New-TestProject $repository 'src/Other/Other.csproj'
            $null = New-TestProject $repository 'tests/Testing.Utilities/Testing.Utilities.csproj'
            $test = New-TestProject $repository 'tests/Example.L0Tests/Example.L0Tests.csproj' @('../../src/Other/Other.csproj', '../../src/Example/Example.csproj', '../Testing.Utilities/Testing.Utilities.csproj')
            @(Get-MutationSourceProjects $test) | Should -Be @($source)
        }

        It 'selects a single eligible source with a different test name' {
            $source = New-TestProject $repository 'src/Example/Example.csproj'
            $test = New-TestProject $repository 'tests/Behavior.L0Tests/Behavior.L0Tests.csproj' @('../../src/Example/Example.csproj')
            @(Get-MutationSourceProjects $test) | Should -Be @($source)
        }

        It 'rejects ambiguous and absent source references' {
            $null = New-TestProject $repository 'src/First/First.csproj'
            $null = New-TestProject $repository 'src/Second/Second.csproj'
            $test = New-TestProject $repository 'tests/Behavior.L0Tests/Behavior.L0Tests.csproj' @('../../src/First/First.csproj', '../../src/Second/Second.csproj')
            { Get-MutationSourceProjects $test } | Should -Throw '*explicit mapping*'
            $empty = New-TestProject $repository 'tests/Empty.L0Tests/Empty.L0Tests.csproj'
            { Get-MutationSourceProjects $empty } | Should -Throw '*explicit mapping*'
        }

        It 'uses the explicit split Inlet abstraction mapping' {
            $references = foreach ($name in @('Inlet.Client.Abstractions', 'Inlet.Gateway.Abstractions', 'Inlet.Runtime.Abstractions')) {
                $null = New-TestProject $repository "src/$name/$name.csproj"
                "../../src/$name/$name.csproj"
            }
            $test = New-TestProject $repository 'tests/Inlet.Abstractions.L0Tests/Inlet.Abstractions.L0Tests.csproj' $references
            @(Get-MutationSourceProjects $test).Count | Should -Be 3
        }
    }

    Describe 'Child process environment' {
        It 'keeps process diagnostics out of its structured output' {
            Mock Show-RepositoryDiagnostic {}
            $output = @(Invoke-RepositoryProcess -FilePath (Get-Process -Id $PID).Path -Arguments @(
                '-NoProfile', '-NonInteractive', '-Command', '[Console]::Error.WriteLine("diagnostic"); [Console]::WriteLine("result"); exit 0'
            ) -SuppressCommandEcho)
            $output | Should -Be @('result')
            Should -Invoke Show-RepositoryDiagnostic -Times 1 -ParameterFilter { $Message.Trim() -eq 'diagnostic' }
        }

        It 'displays command arguments without adding display text to process output' {
            Mock Write-Host {}
            @(Show-RepositoryCommand -FilePath 'dotnet' -Arguments @('test', 'two words')).Count | Should -Be 0
            Should -Invoke Write-Host -Times 1 -ParameterFilter { $Object -eq 'Executing: dotnet test "two words"' }
        }

        It 'scopes SDK environment and working directory to the child and preserves LASTEXITCODE' -ForEach @(@{ ExitCode = 0 }, @{ ExitCode = 7 }) {
            $beforeMsbuild = $env:MSBUILD_EXE_PATH
            $beforeSdks = $env:MSBuildSDKsPath
            $beforeDirectory = (Get-Location).Path
            $command = '[Console]::WriteLine($env:MSBUILD_EXE_PATH); [Console]::WriteLine($env:MSBuildSDKsPath); [Console]::WriteLine((Get-Location).Path); exit ' + $ExitCode
            $parameters = @{
                FilePath = (Get-Process -Id $PID).Path
                Arguments = @('-NoProfile', '-NonInteractive', '-Command', $command)
                WorkingDirectory = $TestDrive
                Environment = @{ MSBUILD_EXE_PATH = 'child-msbuild'; MSBuildSDKsPath = 'child-sdks' }
                SuppressCommandEcho = $true
            }
            if ($ExitCode -eq 0) {
                $output = @(Invoke-RepositoryProcess @parameters)
                $output[0] | Should -Be 'child-msbuild'
                $output[1] | Should -Be 'child-sdks'
                $output[2] | Should -Be $TestDrive
            }
            else { { Invoke-RepositoryProcess @parameters } | Should -Throw '*Exit code: 7*' }
            $LASTEXITCODE | Should -Be $ExitCode
            $env:MSBUILD_EXE_PATH | Should -Be $beforeMsbuild
            $env:MSBuildSDKsPath | Should -Be $beforeSdks
            (Get-Location).Path | Should -Be $beforeDirectory
        }
    }

    Describe 'Selected SDK discovery' {
        BeforeEach {
            $script:sdkRoot = Join-Path $TestDrive 'installed-sdk'
            $script:selectedSdk = Join-Path $sdkRoot '10.0.400'
            $null = New-Item -ItemType Directory -Path (Join-Path $selectedSdk 'Sdks') -Force
            '' | Set-Content (Join-Path $selectedSdk 'MSBuild.dll')
            Mock Invoke-RepositoryProcess {
                if ($Arguments[0] -eq '--version') { '10.0.400' }
                else { "10.0.111 [$sdkRoot]"; "10.0.400 [$sdkRoot]" }
            }
        }

        It 'uses the selected version instead of the first installed SDK' {
            $environment = Get-DotnetSdkEnvironment -WorkingDirectory $TestDrive
            $environment.MSBUILD_EXE_PATH | Should -Be (Join-Path $selectedSdk 'MSBuild.dll')
            $environment.MSBuildSDKsPath | Should -Be (Join-Path $selectedSdk 'Sdks')
            Should -Invoke Invoke-RepositoryProcess -Times 2 -Exactly -ParameterFilter { $WorkingDirectory -eq $TestDrive }
        }

        It 'rejects an SDK with missing build assets' {
            Remove-Item -LiteralPath (Join-Path $selectedSdk 'MSBuild.dll')
            { Get-DotnetSdkEnvironment -WorkingDirectory $TestDrive } | Should -Throw '*missing MSBuild.dll or Sdks*'
        }

        It 'rejects a selected SDK absent from the installation listing' {
            Mock Invoke-RepositoryProcess { if ($Arguments[0] -eq '--version') { '10.0.500' } else { "10.0.400 [$sdkRoot]" } }
            { Get-DotnetSdkEnvironment -WorkingDirectory $TestDrive } | Should -Throw '*Unable to locate*'
        }
    }

    Describe 'Per-project mutation execution' {
        BeforeEach {
            $script:repository = New-TestRepository
            $script:source = New-TestProject $repository 'src/Example/Example.csproj'
            $script:testProject = New-TestProject $repository 'tests/Example.L0Tests/Example.L0Tests.csproj' @('../../src/Example/Example.csproj')
            $script:outputRoot = Join-Path $repository 'results'
            $script:statuses = @('Killed', 'Killed', 'Killed', 'Killed', 'Survived')
            Mock Get-DotnetSdkEnvironment { @{ MSBUILD_EXE_PATH = 'sdk/MSBuild.dll'; MSBuildSDKsPath = 'sdk/Sdks' } }
            Mock Invoke-RepositoryProcess {
                $directory = $Arguments[[array]::IndexOf($Arguments, '--output') + 1]
                Write-TestMutationReport $directory $statuses
            }
        }

        It 'passes source filename, configuration, explicit config, child SDK environment and test directory' {
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot -Configuration Debug
            $result.Success | Should -BeTrue
            $result.Score | Should -Be 80
            Should -Invoke Invoke-RepositoryProcess -Times 1 -Exactly -ParameterFilter {
                $Arguments[0] -eq 'stryker' -and $Arguments[2] -eq 'Example.csproj' -and $Arguments[4] -eq 'Debug' -and
                $Arguments[6] -eq (Join-Path $repository 'stryker-config.json') -and $WorkingDirectory -eq (Split-Path -Parent $testProject) -and
                $Environment.MSBUILD_EXE_PATH -eq 'sdk/MSBuild.dll' -and $Environment.MSBuildSDKsPath -eq 'sdk/Sdks'
            }
        }

        It 'uses distinct report directories for repeated invocations' {
            $first = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $second = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $first.ReportPath | Should -Not -Be $second.ReportPath
        }

        It 'fails if no report is produced despite a successful process exit' {
            Mock Invoke-RepositoryProcess {}
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*Expected one mutation report*'
        }

        It 'fails below the configured score even when Stryker returns success' {
            $script:statuses = @('Killed', 'Survived')
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*below 80%*'
        }

        It 'rejects weakened mutation thresholds before starting Stryker' {
            '{"stryker-config":{"thresholds":{"break":50}}}' | Set-Content (Join-Path $repository 'stryker-config.json')
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*at least 80*'
            Should -Invoke Invoke-RepositoryProcess -Times 0
        }

        It 'rejects incomplete mutation results' {
            $script:statuses = @('Pending')
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*incomplete or invalid*'
        }

        It 'reports no applicable mutants without inventing a numerical score' {
            $script:statuses = @()
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.Score | Should -BeNullOrEmpty
            $result.Success | Should -BeTrue
        }

        It 'propagates process failures' {
            Mock Invoke-RepositoryProcess { throw 'Stryker failed' }
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*Stryker failed*'
        }
    }

    Describe 'Mutation aggregation' {
        BeforeEach {
            $script:repository = New-TestRepository
            $script:solution = Join-Path $repository 'example.slnx'
            '<Solution />' | Set-Content $solution
            Mock Get-MutationSourceProjects { "${ProjectPath}.source.csproj" }
            Mock Invoke-StrykerMutationTestPerProject {
                $reportDirectory = Join-Path $OutputPath ([System.IO.Path]::GetFileNameWithoutExtension($ProjectPath))
                Write-TestMutationReport $reportDirectory
                [pscustomobject]@{ Project = $ProjectPath; SourceProject = $SourceProjectPath; Success = $true; Configuration = $Configuration }
            }
        }

        It 'fails when no test projects are found' {
            Mock Get-TestProjects { @() }
            { Invoke-StrykerMutationTest $solution (Join-Path $repository 'runs') } | Should -Throw '*No L0/L1*'
        }

        It 'supports one or multiple successful projects under strict mode' -ForEach @(@{ Count = 1 }, @{ Count = 3 }) {
            $script:projectCount = $Count
            Mock Get-TestProjects { 1..$projectCount | ForEach-Object { "Example$_.L0Tests.csproj" } }
            $output = Invoke-StrykerMutationTest $solution (Join-Path $repository 'runs') -Configuration Debug
            Test-Path (Join-Path $output 'reports/mutation-report.json') | Should -BeTrue
            $report = Get-Content (Join-Path $output 'reports/mutation-report.json') -Raw | ConvertFrom-Json
            @($report.files.'Example.cs'.mutants).Count | Should -Be $Count
            @($report.files.'Example.cs'.mutants.id | Sort-Object -Unique).Count | Should -Be $Count
            $results = @(Get-Content (Join-Path $output 'project-results.json') -Raw | ConvertFrom-Json)
            $results.Count | Should -Be $Count
            $results[0].Configuration | Should -Be Debug
            Should -Invoke Get-TestProjects -Times 1 -ParameterFilter { $TestLevels.Count -eq 2 -and $TestLevels -contains 'L0Tests' -and $TestLevels -contains 'L1Tests' }
        }

        It 'continues remaining projects and fails the aggregate when one fails' {
            Mock Get-TestProjects { 'First.L0Tests.csproj'; 'Second.L0Tests.csproj' }
            Mock Invoke-StrykerMutationTestPerProject { throw 'mutation failure' } -ParameterFilter { $ProjectPath -eq 'First.L0Tests.csproj' }
            { Invoke-StrykerMutationTest $solution (Join-Path $repository 'runs') } | Should -Throw '*1 mutation project run(s) failed*'
            Should -Invoke Invoke-StrykerMutationTestPerProject -Times 2 -Exactly
        }

        It 'fails when only the architecture suite is listed' {
            Mock Get-TestProjects { 'Architecture.L0Tests.csproj' }
            { Invoke-StrykerMutationTest $solution (Join-Path $repository 'runs') } | Should -Throw '*No eligible mutation projects*'
            Should -Invoke Invoke-StrykerMutationTestPerProject -Times 0
        }
    }

    Describe 'Dependency-only SDK mutation treatment' {
        BeforeEach {
            $script:repository = New-TestRepository
            $script:source = New-TestProject $repository 'src/Sdk.Client/Sdk.Client.csproj'
            $script:testProject = New-TestProject $repository 'tests/Sdk.Client.L0Tests/Sdk.Client.L0Tests.csproj' @('../../src/Sdk.Client/Sdk.Client.csproj')
            $script:outputRoot = Join-Path $repository 'results'
            Mock Get-DotnetSdkEnvironment { @{ MSBUILD_EXE_PATH = 'sdk/MSBuild.dll'; MSBuildSDKsPath = 'sdk/Sdks' } }
            Mock Invoke-RepositoryProcess {
                if ($Arguments[0] -eq 'msbuild') { '{"Items":{"Compile":[]}}'; return }
                Write-TestMutationReport $Arguments[[array]::IndexOf($Arguments, '--output') + 1]
            }
        }

        It 'records a null-score skip for each validated dependency-only SDK pair' -ForEach @(
            @{ Name = 'Sdk.Client' }, @{ Name = 'Sdk.Gateway' }, @{ Name = 'Sdk.Runtime' }
        ) {
            $sourceProject = New-TestProject $repository "src/$Name/$Name.csproj"
            $test = New-TestProject $repository "tests/$Name.L0Tests/$Name.L0Tests.csproj" @("../../src/$Name/$Name.csproj")
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $test -OutputPath $outputRoot -Configuration Debug
            $result.Status | Should -Be 'Skipped'
            $result.Score | Should -BeNullOrEmpty
            $result.ReportPath | Should -BeNullOrEmpty
            $result.Success | Should -BeTrue
            $result.Reason | Should -BeLike '*build and pack validation*'
            $record = Get-Content $result.SkipReportPath -Raw | ConvertFrom-Json
            $record.Status | Should -Be 'Skipped'
            $record.Score | Should -BeNullOrEmpty
            Should -Invoke Invoke-RepositoryProcess -Times 2 -Exactly -ParameterFilter { $Arguments[0] -eq 'msbuild' -and $Arguments -contains '-property:Configuration=Debug' }
            Should -Invoke Invoke-RepositoryProcess -Times 0 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'ignores generated bin and obj files when identifying authored inputs' {
            foreach ($directory in @('bin', 'obj')) {
                $generated = Join-Path (Split-Path -Parent $source) $directory
                $null = New-Item -ItemType Directory -Path $generated -Force
                '// SDK-generated output' | Set-Content (Join-Path $generated 'Generated.cs')
            }
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.Status | Should -Be 'Skipped'
        }

        It 'uses normal validation when authored code or tests are added' -ForEach @(
            @{ Role = 'Source'; Extension = '.cs' }, @{ Role = 'Test'; Extension = '.cs' },
            @{ Role = 'Source'; Extension = '.razor' }, @{ Role = 'Test'; Extension = '.xaml' }
        ) {
            $project = if ($Role -eq 'Source') { $source } else { $testProject }
            '// Authored input' | Set-Content (Join-Path (Split-Path -Parent $project) "Authored$Extension")
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.ReportPath | Should -Not -BeNullOrEmpty
            $result.PSObject.Properties.Name | Should -Not -Contain 'SkipReportPath'
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'uses normal validation for explicit linked or shared project inputs' -ForEach @(
            @{ Xml = '<ItemGroup><Compile Include="../../shared/Linked.cs" Link="Linked.cs" /></ItemGroup>' },
            @{ Xml = '<Import Project="../../shared/Shared.projitems" />' },
            @{ Xml = '<PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>' }
        ) {
            "<Project Sdk=`"Microsoft.NET.Sdk`">$Xml</Project>" | Set-Content -LiteralPath $source
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.ReportPath | Should -Not -BeNullOrEmpty
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'rejects shared inputs declared in ancestor build files' -ForEach @(
            @{ File = 'Directory.Build.props'; Xml = '<Project><ItemGroup><Compile Include="shared/Linked.cs" /></ItemGroup></Project>' },
            @{ File = 'Directory.Build.targets'; Xml = '<Project><Import Project="shared/Shared.targets" /></Project>' }
        ) {
            $Xml | Set-Content (Join-Path $repository $File)
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.ReportPath | Should -Not -BeNullOrEmpty
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'uses evaluated Compile items to detect inputs outside the project folders' -ForEach @(
            @{ Role = 'Source' }, @{ Role = 'Test' }
        ) {
            $script:evaluatedProject = if ($Role -eq 'Source') { $source } else { $testProject }
            Mock Invoke-RepositoryProcess { '{"Items":{"Compile":[{"Identity":"../../shared/Linked.cs"}]}}' } -ParameterFilter {
                $Arguments[0] -eq 'msbuild' -and $Arguments[1] -eq $evaluatedProject
            }
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot
            $result.ReportPath | Should -Not -BeNullOrEmpty
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'does not hide compilation-evaluation failures' {
            Mock Invoke-RepositoryProcess { throw 'Compile evaluation failed' } -ParameterFilter { $Arguments[0] -eq 'msbuild' }
            { Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputRoot } | Should -Throw '*Compile evaluation failed*'
            Should -Invoke Invoke-RepositoryProcess -Times 0 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }

        It 'does not apply SDK treatment to similarly named pairs outside the known locations' {
            $otherSource = New-TestProject $repository 'src/Other/Sdk.Client.csproj'
            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -SourceProjectPath $otherSource -OutputPath $outputRoot
            $result.ReportPath | Should -Not -BeNullOrEmpty
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[0] -eq 'stryker' }
        }
    }

    Describe 'Solution test reporting' {
        It 'lets the TRX logger generate unique per-assembly filenames' {
            $solution = Join-Path $TestDrive 'example.slnx'
            '<Solution />' | Set-Content $solution
            Mock Invoke-RepositoryProcess {}
            Invoke-SolutionTests $solution -ResultsRoot (Join-Path $TestDrive 'results') | Out-Null
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments[[array]::IndexOf($Arguments, '--logger') + 1] -eq 'trx' }
        }
    }
}


