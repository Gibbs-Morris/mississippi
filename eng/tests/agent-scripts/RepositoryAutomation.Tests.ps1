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

Describe 'Repository automation quality gates' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1') -Force
    }

    It 'writes the requested banner' {
        Mock Write-Host {} -ModuleName RepositoryAutomation
        Write-AutomationBanner -Message 'Build gate' -ForegroundColor Green
        Should -Invoke Write-Host -ModuleName RepositoryAutomation -Times 1 -Exactly -ParameterFilter {
            $Object -eq 'Build gate' -and $ForegroundColor -eq [ConsoleColor]::Green
        }
    }

    It 'rejects warnings in both normal solution builds' {
        Mock Invoke-DotnetToolRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionBuild {} -ModuleName RepositoryAutomation
        Invoke-MississippiSolutionBuild -RepoRoot $TestDrive
        Invoke-SampleSolutionBuild -RepoRoot $TestDrive
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 2 -Exactly -ParameterFilter { $WarnAsError }
    }

    It 'preserves test output and uses unique TRX filenames across projects' {
        $solution = Join-Path $TestDrive 'test.slnx'
        Set-Content $solution '<Solution />'
        Mock Invoke-RepositoryProcess { 'Test failure details' } -ModuleName RepositoryAutomation
        Mock Out-Host {} -ModuleName RepositoryAutomation
        $result = Invoke-SolutionTests -SolutionPath $solution -TestLevels L0Tests
        $result.SolutionPath | Should -Be $solution
        Should -Invoke Out-Host -ModuleName RepositoryAutomation -Times 1 -Exactly
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 1 -Exactly -ParameterFilter {
            $Arguments -contains 'trx;LogFilePrefix=test_results' -and
            $Arguments -contains 'FullyQualifiedName~.L0Tests.'
        }
    }

    It 'stops the pipeline when the coverage summarizer exits unsuccessfully' {
        Mock Invoke-MississippiSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-MississippiSolutionUnitTests {} -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess { throw 'summarizer exited 1' } -ModuleName RepositoryAutomation
        { Invoke-SolutionsPipeline -RepoRoot $TestDrive -SkipCleanup } | Should -Throw '*summarizer exited 1*'
        Should -Invoke Invoke-SampleSolutionBuild -ModuleName RepositoryAutomation -Times 0 -Exactly
    }

    It 'streams compiler diagnostics before a failing build step throws' {
        $solution = Join-Path $TestDrive 'failure.slnx'
        Set-Content $solution '<Solution />'
        Mock Invoke-RepositoryProcess { 'Compiler error details'; throw 'Build failed' } -ModuleName RepositoryAutomation
        Mock Out-Host {} -ModuleName RepositoryAutomation
        { Invoke-AutomationStep -Name Build -Action { Invoke-SolutionBuild -SolutionPath $solution } } | Should -Throw '*Build failed*'
        Should -Invoke Out-Host -ModuleName RepositoryAutomation -Times 1 -Exactly
    }
}


