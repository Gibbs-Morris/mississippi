#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeDiscovery {
    Import-Module (Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1') -Force
}

Describe 'Spring validation runner' {
    BeforeAll {
        $runner = Join-Path $PSScriptRoot 'run-spring-validation-tests.ps1'
    }

    BeforeEach {
        Mock Import-Module {}
    }

    It 'rejects empty discovery in pass-through mode' {
        Mock Invoke-Pester { [pscustomobject]@{ PassedCount = 0; FailedCount = 0 } }
        { & $runner -PassThru } | Should -Throw '*did not execute*'
    }

    It 'returns passing results to the orchestrator' {
        Mock Invoke-Pester { [pscustomobject]@{ PassedCount = 1; FailedCount = 0 } }
        (& $runner -PassThru).PassedCount | Should -Be 1
    }

    It 'preserves failures for the orchestrator to reject' {
        Mock Invoke-Pester { [pscustomobject]@{ PassedCount = 1; FailedCount = 1 } }
        (& $runner -PassThru).FailedCount | Should -Be 1
    }
}

Describe 'Spring test project boundaries' {
    BeforeAll {
        $springRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../samples/Spring'))
    }

    It 'keeps Playwright out of L2 and the shared application harness dependency graphs' {
        foreach ($name in @('Spring.L2Tests', 'Spring.TestHarness')) {
            $lock = Get-Content (Join-Path $springRoot "$name/packages.lock.json") -Raw | ConvertFrom-Json
            foreach ($framework in $lock.dependencies.PSObject.Properties) {
                @($framework.Value.PSObject.Properties.Name) | Should -Not -Contain 'Microsoft.Playwright'
            }
        }
    }

    It 'shares application setup without making either test project reference another test project' {
        foreach ($level in @('L2', 'L3')) {
            [xml]$project = Get-Content (Join-Path $springRoot "Spring.${level}Tests/Spring.${level}Tests.csproj") -Raw
            $references = @($project.SelectNodes('//ProjectReference') | ForEach-Object { $_.Include.Replace('\', '/') })
            $references | Should -Contain '../Spring.TestHarness/Spring.TestHarness.csproj'
            @($references | Where-Object { $_ -match '/Spring\.L[0-4]Tests/' }).Count | Should -Be 0
        }
    }

    It 'explicitly keeps the xUnit fixture harness out of test discovery' {
        [xml]$project = Get-Content (Join-Path $springRoot 'Spring.TestHarness/Spring.TestHarness.csproj') -Raw
        $project.SelectSingleNode('//IsTestProject').InnerText | Should -Be 'false'
    }
}

Describe 'Spring validation' {
    InModuleScope RepositoryAutomation {
        BeforeEach {
            $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
            $null = New-Item -ItemType Directory -Path $repo
            '<Project><ItemGroup><PackageVersion Include="Aspire.Hosting.AppHost" Version="13.5.3" /></ItemGroup></Project>' |
                Set-Content (Join-Path $repo 'Directory.Packages.props')
            Mock Invoke-RepositoryProcess {
                if ($FilePath -eq 'docker') { return 'linux' }
                if ($Arguments[0] -eq '--version') { return $(if ($FilePath -eq 'dotnet') { '10.0.400' } else { '13.5.3+test' }) }
            }
        }

        It 'checks prerequisites without building, installing, or launching anything' {
            Invoke-SpringValidation -RepoRoot $repo -Doctor
            Should -Invoke Invoke-RepositoryProcess -Times 2 -Exactly
            $result = Get-Content (Get-ChildItem $repo -Recurse -Filter summary.json).FullName -Raw | ConvertFrom-Json
            $result.status | Should -Be READY
            $result.passed | Should -Be 0
        }

        It 'normalizes case-insensitive choices for case-sensitive project paths' {
            Invoke-SpringValidation -RepoRoot $repo -TestLevel l3 -Suite smoke -Doctor
            $result = Get-Content (Get-ChildItem $repo -Recurse -Filter summary.json).FullName -Raw | ConvertFrom-Json
            $result.testLevel | Should -BeExactly 'L3'
            $result.suite | Should -BeExactly 'Smoke'
            $result.project.EndsWith('Spring.L3Tests.csproj', [StringComparison]::Ordinal) | Should -BeTrue
        }

        It 'fails on inaccessible Docker and preserves the failed phase and environment' {
            $originalPath = $env:PATH
            $originalLocation = (Get-Location).Path
            Mock Invoke-RepositoryProcess { throw 'Docker unavailable' } -ParameterFilter { $FilePath -eq 'docker' }
            { Invoke-SpringValidation -RepoRoot $repo } | Should -Throw '*Docker unavailable*'
            $result = Get-Content (Get-ChildItem $repo -Recurse -Filter summary.json).FullName -Raw | ConvertFrom-Json
            $result.status | Should -Be FAIL
            $result.phase | Should -Be prerequisites
            $env:PATH | Should -BeExactly $originalPath
            (Get-Location).Path | Should -BeExactly $originalLocation
        }

        It 'rejects Windows containers' {
            Mock Invoke-RepositoryProcess { 'windows' } -ParameterFilter { $FilePath -eq 'docker' }
            { Invoke-SpringValidation -RepoRoot $repo -Doctor } | Should -Throw '*Linux containers*'
        }

        It 'stops on a failed build and restores process environment' {
            $originalPath = $env:PATH
            $originalArtifacts = $env:SPRING_TEST_ARTIFACTS
            Mock Invoke-SolutionBuild { throw 'Build failed' }
            { Invoke-SpringValidation -RepoRoot $repo } | Should -Throw '*Build failed*'
            $result = Get-Content (Get-ChildItem $repo -Recurse -Filter summary.json).FullName -Raw | ConvertFrom-Json
            $result.phase | Should -Be build
            $result.status | Should -Be FAIL
            Should -Invoke Invoke-RepositoryProcess -Times 0 -ParameterFilter { $Arguments[0] -eq 'test' }
            $env:PATH | Should -BeExactly $originalPath
            $env:SPRING_TEST_ARTIFACTS | Should -BeExactly $originalArtifacts
        }

        It 'rejects a mismatched cached Aspire CLI before building' {
            Mock Invoke-RepositoryProcess { '13.0.2' } -ParameterFilter { $FilePath -like '*aspire*' }
            Mock Invoke-SolutionBuild {}
            { Invoke-SpringValidation -RepoRoot $repo } | Should -Throw '*Expected Aspire CLI 13.5.3*'
            Should -Invoke Invoke-SolutionBuild -Times 0
        }

        It 'preserves separate evidence directories for successive runs' {
            Invoke-SpringValidation -RepoRoot $repo -Doctor
            Invoke-SpringValidation -RepoRoot $repo -Doctor
            @(Get-ChildItem $repo -Recurse -Filter summary.json).Count | Should -Be 2
        }

        It 'builds and runs <TestLevel> <Suite> with isolated tooling and strict results' -TestCases @(
            @{ TestLevel = 'L3'; Suite = 'Smoke' }; @{ TestLevel = 'L3'; Suite = 'Full' }; @{ TestLevel = 'L2'; Suite = 'Full' }
        ) {
            param($TestLevel, $Suite)
            $target = Join-Path $repo "samples/Spring/Spring.${TestLevel}Tests/bin"
            $null = New-Item -ItemType Directory -Path $target -Force
            if ($TestLevel -eq 'L3') { '' | Set-Content (Join-Path $target 'playwright.ps1') }
            Mock Invoke-SolutionBuild {}
            Mock Invoke-RepositoryProcess {
                if ($FilePath -eq 'docker') { return 'linux' }
                if ($Arguments[0] -eq '--version') { return $(if ($FilePath -eq 'dotnet') { '10.0.400' } else { '13.5.3+test' }) }
                if ($Arguments[0] -eq 'msbuild') { return Join-Path (Split-Path $Arguments[1]) 'bin' }
                if ($Arguments[0] -eq 'test') {
                    $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
                    '<TestRun><ResultSummary outcome="Completed"><Counters total="1" passed="1" executed="1"/></ResultSummary></TestRun>' |
                        Set-Content (Join-Path $directory 'spring.trx')
                }
            }
            $originalBrowsers = $env:PLAYWRIGHT_BROWSERS_PATH
            Invoke-SpringValidation -RepoRoot $repo -TestLevel $TestLevel -Suite $Suite -InstallBrowserDependencies
            $result = Get-Content (Get-ChildItem $repo -Recurse -Filter summary.json).FullName -Raw | ConvertFrom-Json
            $result.status | Should -Be PASS
            $result.passed | Should -Be 1
            $result.testLevel | Should -Be $TestLevel
            $result.suite | Should -Be $Suite
            $result.project | Should -Be (Join-Path $repo "samples/Spring/Spring.${TestLevel}Tests/Spring.${TestLevel}Tests.csproj")
            Should -Invoke Invoke-SolutionBuild -Times 1 -Exactly -ParameterFilter {
                $WarnAsError -and $NoRestore -and $SolutionPath.EndsWith("Spring.${TestLevel}Tests.csproj")
            }
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter { $Arguments -contains '--locked-mode' }
            $browserCalls = if ($TestLevel -eq 'L3') { 1 } else { 0 }
            Should -Invoke Invoke-RepositoryProcess -Times $browserCalls -Exactly -ParameterFilter { $Arguments -contains '--with-deps' -and $Arguments -contains 'chromium' }
            Should -Invoke Invoke-RepositoryProcess -Times $browserCalls -Exactly -ParameterFilter { $Arguments[0] -eq 'msbuild' }
            Should -Invoke Invoke-RepositoryProcess -Times 1 -ParameterFilter {
                $Arguments[0] -eq 'test' -and $Arguments -contains '--no-build' -and
                $Arguments -contains '--minimum-expected-tests' -and
                $Arguments[[array]::IndexOf($Arguments, '--minimum-expected-tests') + 1] -eq '1' -and
                $Arguments -contains '--report-xunit-trx' -and $Arguments -contains '--timeout' -and
                $Arguments[[array]::IndexOf($Arguments, '--timeout') + 1] -eq '15m'
            }
            $filterCalls = if ($Suite -eq 'Smoke') { 1 } else { 0 }
            Should -Invoke Invoke-RepositoryProcess -Times $filterCalls -Exactly -ParameterFilter { $Arguments -contains 'Category=Smoke' }
            $env:PLAYWRIGHT_BROWSERS_PATH | Should -BeExactly $originalBrowsers
        }

        It 'rejects a nonexistent L2 smoke suite before running tools' {
            { Invoke-SpringValidation -RepoRoot $repo -TestLevel L2 -Suite Smoke } | Should -Throw '*smoke journeys are L3*'
            Should -Invoke Invoke-RepositoryProcess -Times 0
        }

        It 'rejects zero, skipped, failed, or aborted test runs: <Outcome>/<Total>/<Passed>/<Executed>' -TestCases @(
            @{ Outcome = 'Completed'; Total = 0; Passed = 0; Executed = 0 }
            @{ Outcome = 'Completed'; Total = 2; Passed = 1; Executed = 1 }
            @{ Outcome = 'Failed'; Total = 2; Passed = 1; Executed = 2 }
            @{ Outcome = 'Aborted'; Total = 1; Passed = 1; Executed = 1 }
        ) {
            param($Outcome, $Total, $Passed, $Executed)
            $trx = Join-Path $repo 'spring.trx'
            "<TestRun><ResultSummary outcome='$Outcome'><Counters total='$Total' passed='$Passed' executed='$Executed'/></ResultSummary></TestRun>" | Set-Content $trx
            { Get-SpringTestResult -Path $trx } | Should -Throw '*completed run*'
        }

        It 'requires a result file and valid XML' {
            $trx = Join-Path $repo 'spring.trx'
            { Get-SpringTestResult -Path $trx } | Should -Throw
            '<TestRun>' | Set-Content $trx
            { Get-SpringTestResult -Path $trx } | Should -Throw
        }

        It 'returns the count from a completed passing run' {
            $trx = Join-Path $repo 'spring.trx'
            '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><ResultSummary outcome="Completed"><Counters total="2" passed="2" executed="2"/></ResultSummary></TestRun>' | Set-Content $trx
            Get-SpringTestResult -Path $trx | Should -Be 2
        }
    }
}
