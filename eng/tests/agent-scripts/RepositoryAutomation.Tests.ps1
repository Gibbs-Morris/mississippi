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
        (New-AutomationRunDirectory -Root $testRoot -Prefix 'test') | Should -Not -Be $runDirectory
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
            $Arguments -contains '-p:RepositoryTestResults=true' -and
            $Arguments -contains 'FullyQualifiedName~.L0Tests.'
        }
    }

    It 'validates executed tests across the selected solution: <Executed> <Logger>' -ForEach @(
        @{ Executed = 0; Logger = $null },
        @{ Executed = 2; Logger = $null },
        @{ Executed = 0; Logger = 'trx;LogFileName=custom.trx' },
        @{ Executed = 2; Logger = 'trx;LogFileName=custom.trx' }
    ) {
        $solution = Join-Path $TestDrive 'test.slnx'
        Set-Content $solution '<Solution />'
        $reports = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $reports | Out-Null
        Set-Content (Join-Path $reports 'empty.trx') '<TestRun><ResultSummary><Counters executed="0" /></ResultSummary></TestRun>'
        Set-Content (Join-Path $reports 'selected.trx') "<TestRun><ResultSummary><Counters executed='$Executed' /></ResultSummary></TestRun>"
        Mock New-AutomationRunDirectory { $Root } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation
        if ($Executed -eq 0) {
            { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot $reports -TestLevels L4Tests -Logger $Logger } |
                Should -Throw '*No tests executed*'
        }
        else {
            (Invoke-SolutionTests -SolutionPath $solution -ResultsRoot $reports -TestLevels L0Tests -Logger $Logger).ResultsDirectory |
                Should -Be $reports
        }
    }

    It 'isolates cleanup caches between invocations' {
        $solution = Join-Path $TestDrive 'cleanup.slnx'
        $settings = Join-Path $TestDrive 'Directory.DotSettings'
        Set-Content $solution '<Solution />'
        Set-Content $settings '<ResourceDictionary />'
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation
        Invoke-ReSharperCleanup -SolutionPath $solution -SettingsPath $settings
        Invoke-ReSharperCleanup -SolutionPath $solution -SettingsPath $settings
        $caches = @(Get-ChildItem (Join-Path $TestDrive '.scratchpad/cleanup-caches') -Directory)
        $caches.Count | Should -Be 2
        foreach ($cache in $caches) {
            $expectedArgument = "--caches-home=$($cache.FullName)"
            Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Exactly 1 -ParameterFilter {
                $Arguments -contains $expectedArgument
            }
        }
    }

    It 'distinguishes missing reports from a reported empty test run' {
        $solution = Join-Path $TestDrive 'missing.slnx'
        Set-Content $solution '<Solution />'
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation
        { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'missing-results') } |
            Should -Throw '*No TRX reports were produced*'
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
        Mock Invoke-DotnetToolRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionRestore {} -ModuleName RepositoryAutomation
        Copy-Item $solution (Join-Path $TestDrive 'mississippi.slnx')
        { Invoke-AutomationStep -Name Build -Action { Invoke-MississippiSolutionBuild -RepoRoot $TestDrive } } | Should -Throw '*Build failed*'
        Should -Invoke Out-Host -ModuleName RepositoryAutomation -Times 1 -Exactly
    }

    It 'preserves compiler output in an artifact even when the build fails' {
        $solution = Join-Path $TestDrive 'logged.slnx'
        Set-Content $solution '<Solution />'
        $log = Join-Path $TestDrive 'build.log'
        Mock Invoke-RepositoryProcess { 'Compiler error details'; throw 'Build failed' } -ModuleName RepositoryAutomation
        { Invoke-SolutionBuild -SolutionPath $solution | Tee-Object -FilePath $log | Out-Null } |
            Should -Throw '*Build failed*'
        Get-Content -LiteralPath $log -Raw | Should -Match 'Compiler error details'
    }
}


