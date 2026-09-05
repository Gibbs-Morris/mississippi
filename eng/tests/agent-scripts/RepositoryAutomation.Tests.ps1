#requires -Module Pester

$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$modulePath = [System.IO.Path]::GetFullPath($modulePath)
Import-Module -Name $modulePath -Force

Describe 'RepositoryAutomation helpers' {
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
            $null = New-Item -ItemType Directory -Path (Join-Path $root 'tests') -Force
            $null = New-Item -ItemType Directory -Path (Join-Path $root 'samples') -Force
            '{"stryker-config":{"thresholds":{"high":80,"low":60,"break":50}}}' |
                Set-Content -LiteralPath (Join-Path $root 'stryker-config.json')
            return $root
        }

        function New-TestProject {
            param(
                [string]$Root,
                [string]$RelativePath,
                [string[]]$References = @()
            )

            $path = Join-Path $Root ($RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force
            $referenceXml = ($References | ForEach-Object {
                    '<ProjectReference Include="{0}" />' -f $_
                }) -join ''
            "<Project Sdk=`"Microsoft.NET.Sdk`"><ItemGroup>$referenceXml</ItemGroup></Project>" |
                Set-Content -LiteralPath $path
            return (Resolve-Path -LiteralPath $path).Path
        }

        function Write-TestMutationReport {
            param(
                [string]$OutputPath,
                [string]$SourcePath = 'Example.cs'
            )

            $reportsPath = Join-Path $OutputPath 'reports'
            $null = New-Item -ItemType Directory -Path $reportsPath -Force
            $report = @{ files = @{ $SourcePath = @{ mutants = @(@{ id = '0'; status = 'Killed' }) } } }
            $reportPath = Join-Path $reportsPath 'mutation-report.json'
            $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath
            return $reportPath
        }
    }

    Describe 'Mutation automation' {
        BeforeEach {
            $script:testRepository = New-TestRepository
            $script:sourceProject = New-TestProject -Root $testRepository -RelativePath 'src/Example/Example.csproj'
            '// source' | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $sourceProject) 'Example.cs')
            $script:testProject = New-TestProject -Root $testRepository -RelativePath 'tests/Example.L0Tests/Example.L0Tests.csproj' -References @('../../src/Example/Example.csproj')
            $script:solutionPath = Join-Path $testRepository 'mississippi.slnx'
            '<Solution><Project Path="tests/Example.L0Tests/Example.L0Tests.csproj" /></Solution>' |
                Set-Content -LiteralPath $solutionPath
        }

        It 'discovers only test projects listed in the solution' {
            $unlisted = New-TestProject -Root $testRepository -RelativePath 'samples/Unlisted.L0Tests/Unlisted.L0Tests.csproj'

            $projects = @(Get-TestProjects -SolutionPath $solutionPath)

            $projects.Count | Should -Be 1
            $projects[0] | Should -Be $testProject
            $projects | Should -Not -Contain $unlisted
        }

        It 'reads test projects from generated legacy solutions' {
            $legacySolution = Join-Path $testRepository 'mississippi.sln'
            'Project("{GUID}") = "Example.L0Tests", "tests\Example.L0Tests\Example.L0Tests.csproj", "{GUID}"' |
                Set-Content -LiteralPath $legacySolution

            @(Get-TestProjects -SolutionPath $legacySolution) | Should -Be @($testProject)
        }

        It 'maps a test project to its matching source project' {
            @(Get-MutationSourceProjects -TestProjectPath $testProject) | Should -Be @($sourceProject)
        }

        It 'runs Stryker from the source directory with explicit project settings' {
            Mock Get-DotnetMsBuildPath { Join-Path $testRepository 'MSBuild.dll' }
            Mock Invoke-RepositoryProcess {
                $outputIndex = [array]::IndexOf($Arguments, '--output')
                Write-TestMutationReport -OutputPath $Arguments[$outputIndex + 1] -SourcePath (Join-Path (Split-Path -Parent $sourceProject) 'Example.cs') | Out-Null
            } -ParameterFilter { $Arguments[0] -eq 'stryker' }

            $result = Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects @($testProject) -OutputPath (Join-Path $testRepository 'results') -Configuration Debug

            $result.Success | Should -BeTrue
            $result.ReportPath | Should -Exist
            Should -Invoke Invoke-RepositoryProcess -Times 1 -Exactly -ParameterFilter {
                $Arguments[0] -eq 'stryker' -and
                $Arguments[2] -eq 'Example.csproj' -and
                $Arguments -contains '--test-project' -and
                $Arguments -contains $testProject -and
                $Arguments -contains '--msbuild-path' -and
                $Arguments -contains '--disable-bail' -and
                $Arguments -contains 'Debug' -and
                $WorkingDirectory -eq (Split-Path -Parent $sourceProject)
            }
        }

        It 'fails when Stryker exits successfully without producing a report' {
            Mock Get-DotnetMsBuildPath { Join-Path $testRepository 'MSBuild.dll' }
            Mock Invoke-RepositoryProcess {} -ParameterFilter { $Arguments[0] -eq 'stryker' }

            { Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects @($testProject) -OutputPath (Join-Path $testRepository 'results') } |
                Should -Throw '*Expected one mutation report*'
        }

        It 'aggregates successful source reports and fails the suite when one source fails' {
            Mock Invoke-StrykerMutationTestPerProject { throw 'native Stryker failure' }

            { Invoke-StrykerMutationTest -SolutionPath $solutionPath -OutputPath (Join-Path $testRepository 'results') } |
                Should -Throw '*failed*'
        }

        It 'reports mapping failures before the no-target fallback' {
            Mock Get-MutationSourceProjects { throw 'source mapping failure' }

            { Invoke-StrykerMutationTest -SolutionPath $solutionPath -OutputPath (Join-Path $testRepository 'results') } |
                Should -Throw '*Stryker mutation testing failed*'
        }

        It 'writes the aggregate report consumed by the survivor summarizer' {
            Mock Invoke-StrykerMutationTestPerProject {
                $reportPath = Write-TestMutationReport -OutputPath (Join-Path $OutputPath 'Example')
                [pscustomobject]@{
                    Project    = $ProjectPath
                    ReportPath = $reportPath
                    Success    = $true
                }
            }

            $output = Invoke-StrykerMutationTest -SolutionPath $solutionPath -OutputPath (Join-Path $testRepository 'results')
            $aggregatePath = Join-Path $output 'reports/mutation-report.json'

            $aggregatePath | Should -Exist
            $report = Get-Content -LiteralPath $aggregatePath -Raw | ConvertFrom-Json
            @($report.files.PSObject.Properties).Count | Should -Be 1
        }

        It 'restores the caller location after a Stryker failure' {
            Mock Get-DotnetMsBuildPath { Join-Path $testRepository 'MSBuild.dll' }
            Mock Invoke-RepositoryProcess { throw 'native Stryker failure' } -ParameterFilter { $Arguments[0] -eq 'stryker' }
            $originalLocation = (Get-Location).Path

            { Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects @($testProject) -OutputPath (Join-Path $testRepository 'results') } |
                Should -Throw '*native Stryker failure*'
            (Get-Location).Path | Should -Be $originalLocation
        }
    }
}


