#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1') -Force
}

Describe 'Mutation automation' {
    BeforeEach {
        $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        foreach ($directory in @('.git', 'src/Widget', 'tests/Widget.L0Tests', 'tests/Widget.L1Tests', 'tests/Widget.L2Tests', 'samples/Other.L0Tests')) {
            New-Item -ItemType Directory -Path (Join-Path $repo $directory) -Force | Out-Null
        }
        $sourceProject = Join-Path $repo 'src/Widget/Widget.csproj'
        Set-Content $sourceProject '<Project />'
        Set-Content (Join-Path $repo 'src/Widget/IWidget.cs') 'internal interface IWidget {}'
        foreach ($level in @('L0', 'L1', 'L2')) {
            Set-Content (Join-Path $repo "tests/Widget.${level}Tests/Widget.${level}Tests.csproj") '<Project><ItemGroup><ProjectReference Include="../../src/Widget/Widget.csproj" /></ItemGroup></Project>'
        }
        Set-Content (Join-Path $repo 'samples/Other.L0Tests/Other.L0Tests.csproj') '<Project />'
        $solution = Join-Path $repo 'mississippi.slnx'
        Set-Content $solution '<Solution><Folder Name="/Tests/"><Project Path="tests\Widget.L0Tests\Widget.L0Tests.csproj" /><Project Path="tests/Widget.L1Tests/Widget.L1Tests.csproj" /><Project Path="tests/Widget.L2Tests/Widget.L2Tests.csproj" /></Folder></Solution>'
        Set-Content (Join-Path $repo 'stryker-config.json') '{"stryker-config":{}}'
        $output = Join-Path $repo 'mutation-results'
        Mock Invoke-RepositoryProcess -ModuleName RepositoryAutomation { $repo } -ParameterFilter { $Arguments[0] -eq 'msbuild' }
    }

    It 'discovers only test projects declared in the canonical solution' {
        $projects = @(Get-TestProjects -SolutionPath $solution)
        $projects.Count | Should -Be 3
        $projects | Should -Not -Contain (Join-Path $repo 'samples/Other.L0Tests/Other.L0Tests.csproj')
    }

    It 'uses canonical membership when given a generated legacy solution' {
        $legacy = Join-Path $repo 'mississippi.sln'
        Set-Content $legacy ''
        @(Get-TestProjects -SolutionPath $legacy).Count | Should -Be 3
    }

    It 'groups the L0 and L1 tests for each source project exactly once' {
        Mock Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation { 'completed' }
        Invoke-StrykerMutationTest -SolutionPath $solution -OutputPath $output -Configuration Debug | Should -Be $output
        Should -Invoke Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation -Exactly 1 -ParameterFilter {
            $ProjectPath -like '*Widget.csproj' -and $TestProjects.Count -eq 2 -and
            -not ($TestProjects -match 'L2Tests') -and $Configuration -eq 'Debug'
        }
    }

    It 'fails when a mutation process fails even if it is the only target' {
        Mock Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation { throw 'Stryker exited 1' }
        { Invoke-StrykerMutationTest -SolutionPath $solution -OutputPath $output } | Should -Throw '*mutation testing failed*'
    }

    It 'skips package-only projects even when build output contains generated C#' {
        $packageDirectory = Join-Path $repo 'src/Package'
        foreach ($directory in @('bin', 'obj')) {
            $generatedDirectory = Join-Path $packageDirectory $directory
            New-Item -ItemType Directory -Path $generatedDirectory -Force | Out-Null
            Set-Content (Join-Path $generatedDirectory 'Generated.cs') 'internal class Generated {}'
        }
        Set-Content (Join-Path $packageDirectory 'Package.csproj') '<Project />'
        Set-Content (Join-Path $repo 'tests/Widget.L0Tests/Widget.L0Tests.csproj') '<Project><ItemGroup><ProjectReference Include="../../src/Widget/Widget.csproj" /><ProjectReference Include="../../src/Package/Package.csproj" /></ItemGroup></Project>'
        Mock Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation { 'completed' }
        Invoke-StrykerMutationTest -SolutionPath $solution -OutputPath $output | Should -Be $output
        Should -Invoke Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation -Exactly 1
        Should -Invoke Invoke-StrykerMutationTestPerProject -ModuleName RepositoryAutomation -Exactly 0 -ParameterFilter { $ProjectPath -like '*Package.csproj' }
    }

    It 'rejects a solution without mutation targets' {
        Set-Content $solution '<Solution />'
        { Invoke-StrykerMutationTest -SolutionPath $solution -OutputPath $output } | Should -Throw '*No*'
    }

    It 'invokes Stryker from the source directory with repository configuration and restores the caller location' {
        $originalLocation = (Get-Location).Path
        $tests = @(Join-Path $repo 'tests/Widget.L0Tests/Widget.L0Tests.csproj')
        Mock Invoke-RepositoryProcess -ModuleName RepositoryAutomation {
            Split-Path -Leaf (Get-Location).Path | Should -Be 'Widget'
            Set-Content (Join-Path $Arguments[8] 'mutation-report.json') '{}'
        } -ParameterFilter { $Arguments[0] -eq 'stryker' }
        Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects $tests -OutputPath $output -Configuration Debug | Out-Null
        (Get-Location).Path | Should -Be $originalLocation
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Exactly 1 -ParameterFilter {
            $Arguments -contains '--test-project' -and $Arguments -contains '--config-file' -and
            $Arguments -contains 'Debug' -and $Arguments -contains 'Widget.csproj' -and
            $Arguments -contains (Join-Path $repo 'MSBuild.dll')
        }
    }

    It 'restores the caller location after a Stryker failure' {
        $originalLocation = (Get-Location).Path
        Mock Invoke-RepositoryProcess -ModuleName RepositoryAutomation { throw 'native process failed' } -ParameterFilter { $Arguments[0] -eq 'stryker' }
        { Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects @('test.csproj') -OutputPath $output } | Should -Throw '*native process failed*'
        (Get-Location).Path | Should -Be $originalLocation
    }

    It 'rejects a successful native exit without a report' {
        Mock Invoke-RepositoryProcess -ModuleName RepositoryAutomation {} -ParameterFilter { $Arguments[0] -eq 'stryker' }
        { Invoke-StrykerMutationTestPerProject -ProjectPath $sourceProject -TestProjects @('test.csproj') -OutputPath $output } | Should -Throw '*without a mutation-report.json*'
    }

    It 'reports a failed quality gate when tests pass but focused mutation fails' {
        $qualityScript = Join-Path $PSScriptRoot '../../src/agent-scripts/test-project-quality.ps1'
        $testProject = Join-Path $repo 'tests/Widget.L0Tests/Widget.L0Tests.csproj'
        Mock Import-Module {}
        Mock dotnet {
            $results = $args[[Array]::IndexOf($args, '--results-directory') + 1]
            Set-Content (Join-Path $results 'test_results.trx') '<TestRun><ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
            & (Get-Process -Id $PID).Path -NoProfile -Command 'exit 0'
        }
        Mock Invoke-StrykerMutationTestPerProject { throw 'mutation failed' }
        Push-Location $repo
        try {
            $summary = & $qualityScript -TestProject $testProject -SourceProject $sourceProject -Configuration Debug -NoBuild 6>&1 3>$null
            $LASTEXITCODE | Should -Be 1
            ($summary | Out-String) | Should -Match 'RESULT: FAIL'
        }
        finally { Pop-Location }
        Should -Invoke Invoke-StrykerMutationTestPerProject -Exactly 1 -ParameterFilter {
            $ProjectPath -eq $sourceProject -and $TestProjects.Count -eq 1 -and
            $TestProjects[0] -eq $testProject -and $Configuration -eq 'Debug'
        }
    }
}
