#!/usr/bin/env pwsh

#requires -Module Pester
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1') -Force

Describe 'Current-job build receipts' {
    InModuleScope RepositoryAutomation {
        BeforeEach {
            $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
            $null = New-Item -ItemType Directory -Path (Join-Path $repo 'src'), (Join-Path $repo 'samples/Fixture') -Force
            $receipt = Join-Path $repo 'artifacts/build.json'
            $binary = Join-Path $repo 'samples/Fixture/bin/Release/net10.0/Fixture.dll'
            $assets = Join-Path $repo 'samples/Fixture/obj/project.assets.json'
            $referencedBinary = Join-Path $repo 'src/Dependency/bin/Debug/net10.0/Dependency.dll'
            "bin/`nobj/`nartifacts/" | Set-Content (Join-Path $repo '.gitignore')
            '<Solution><Project Path="samples/Fixture/Fixture.csproj" /></Solution>' | Set-Content (Join-Path $repo 'samples.slnx')
            '<Project />' | Set-Content (Join-Path $repo 'samples/Fixture/Fixture.csproj')
            '{}' | Set-Content (Join-Path $repo 'samples/Fixture/packages.lock.json')
            foreach ($arguments in @(
                @('init', '--quiet'), @('config', 'user.name', 'Test'), @('config', 'user.email', 'test@example.invalid'),
                @('config', 'commit.gpgsign', 'false'), @('add', '.'), @('commit', '--quiet', '-m', 'Fixture')
            )) {
                & git -c "safe.directory=$($repo.Replace('\', '/'))" -C $repo @arguments | Out-Null
                if ($LASTEXITCODE -ne 0) { throw 'Fixture Git failed.' }
            }
            $previousEnvironment = @{}
            foreach ($name in @('GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'GITHUB_JOB', 'RUNNER_NAME')) {
                $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
                [Environment]::SetEnvironmentVariable($name, 'fixture')
            }
            Mock Invoke-RepositoryProcess {
                if ($Arguments[0] -eq 'msbuild') {
                    return (@{ Properties = @{ TargetPath = $binary; ProjectAssetsFile = $assets } } | ConvertTo-Json)
                }
                '10.0.401'
            }
            Mock Invoke-SolutionBuild {
                $null = New-Item -ItemType Directory -Path (Split-Path $binary), (Split-Path $assets), (Split-Path $receipt), (Split-Path $referencedBinary) -Force
                'current binary' | Set-Content $binary
                'current referenced binary' | Set-Content $referencedBinary
                'dependencies' | Set-Content ([IO.Path]::ChangeExtension($binary, '.deps.json'))
                'runtime' | Set-Content ([IO.Path]::ChangeExtension($binary, '.runtimeconfig.json'))
                'installer' | Set-Content (Join-Path (Split-Path $binary) 'playwright.ps1')
                'current dependencies' | Set-Content $assets
            }
        }
        AfterEach {
            foreach ($name in $previousEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name]) }
        }

        It 'reuses a successfully built current job and records the actual build properties' {
            Invoke-CiBenchmarkBuild -RepoRoot $repo -Solution samples.slnx -BuildVersion '0.6.0' -ReceiptPath $receipt
            { Assert-SpringPrebuiltBuild -RepoRoot $repo -ReceiptPath $receipt -BuildVersion '0.6.0' } | Should -Not -Throw
            Should -Invoke Invoke-SolutionBuild -Times 1 -Exactly -ParameterFilter {
                $NoRestore -and $NoIncremental -and $WarnAsError -and $Configuration -eq 'Release' -and $AdditionalArguments -contains '-p:Version=0.6.0'
            }
        }

        It 'does not write a receipt when the real build fails' {
            Mock Invoke-SolutionBuild { throw 'Build failed' }
            { Invoke-CiBenchmarkBuild -RepoRoot $repo -Solution samples.slnx -BuildVersion '0.6.0' -ReceiptPath $receipt } | Should -Throw '*Build failed*'
            Test-Path $receipt | Should -BeFalse
        }

        It 'rejects <Mismatch> rather than running stale binaries' -TestCases @(
            @{ Mismatch = 'source' }, @{ Mismatch = 'dependencies' }, @{ Mismatch = 'binary' }, @{ Mismatch = 'missing binary' },
            @{ Mismatch = 'missing assets' }, @{ Mismatch = 'job attempt' }, @{ Mismatch = 'SDK' }, @{ Mismatch = 'configuration' },
            @{ Mismatch = 'version' }, @{ Mismatch = 'Sonar' }, @{ Mismatch = 'missing receipt' }, @{ Mismatch = 'unknown job' }
            @{ Mismatch = 'referenced configuration binary' }
        ) {
            param($Mismatch)
            Invoke-CiBenchmarkBuild -RepoRoot $repo -Solution samples.slnx -BuildVersion '0.6.0' -ReceiptPath $receipt
            $version = '0.6.0'
            $configuration = 'Release'
            switch ($Mismatch) {
                source { '<Project Changed="true" />' | Set-Content (Join-Path $repo 'samples/Fixture/Fixture.csproj') }
                dependencies { 'other dependencies' | Set-Content $assets }
                binary { 'other binary' | Set-Content $binary }
                'referenced configuration binary' { 'other referenced binary' | Set-Content $referencedBinary }
                'missing binary' { Remove-Item -LiteralPath $binary }
                'missing assets' { Remove-Item -LiteralPath $assets }
                'job attempt' { $env:GITHUB_RUN_ATTEMPT = 'other attempt' }
                SDK { Mock Invoke-RepositoryProcess { '10.0.402' } }
                configuration { $configuration = 'Debug' }
                version { $version = '0.7.0' }
                Sonar { $null = New-Item -ItemType Directory -Path (Join-Path $repo '.sonarqube') }
                'missing receipt' { Remove-Item -LiteralPath $receipt }
                'unknown job' { $env:GITHUB_JOB = '' }
            }
            { Assert-SpringPrebuiltBuild -RepoRoot $repo -ReceiptPath $receipt -BuildVersion $version -Configuration $configuration } | Should -Throw
        }

        It 'rejects source changes made during the build' {
            Mock Invoke-SolutionBuild { 'changed lock' | Set-Content (Join-Path $repo 'samples/Fixture/packages.lock.json') }
            { Invoke-CiBenchmarkBuild -RepoRoot $repo -Solution samples.slnx -BuildVersion '0.6.0' -ReceiptPath $receipt } | Should -Throw '*clean checked-out revision*'
            Test-Path $receipt | Should -BeFalse
        }
    }
}

Describe 'Benchmark test-level discovery and failure' {
    InModuleScope RepositoryAutomation {
        BeforeEach {
            $solution = Join-Path $TestDrive 'samples.slnx'
            '<Solution />' | Set-Content $solution
            Mock Invoke-SolutionTests { throw 'A failed level' }
        }

        It 'reports N/A only after a successful empty Samples L1 inventory' {
            (Invoke-CiBenchmarkTestLevel -SolutionPath $solution -Level L1 -ResultsRoot $TestDrive).status | Should -Be 'NOT_APPLICABLE'
            Should -Invoke Invoke-SolutionTests -Times 0
        }

        It 'automatically executes future L1 membership and propagates its failure' {
            '<Project />' | Set-Content (Join-Path $TestDrive 'Added.L1Tests.csproj')
            '<Solution><Project Path="Added.L1Tests.csproj" /></Solution>' | Set-Content $solution
            { Invoke-CiBenchmarkTestLevel -SolutionPath $solution -Level L1 -ResultsRoot $TestDrive } | Should -Throw '*failed level*'
            Should -Invoke Invoke-SolutionTests -Times 1 -ParameterFilter { $TestLevels -contains 'L1Tests' -and $AdditionalArguments -contains '--no-build' }
        }

        It 'fails closed on malformed or unresolved inventory rather than calling it N/A' -TestCases @(
            @{ Xml = '<Solution>' }; @{ Xml = '<Solution><Project Path="Missing.L1Tests.csproj" /></Solution>' }
            @{ Xml = '<Solution><Project /></Solution>' }; @{ Xml = '<Solution><Project Path="." /></Solution>' }
        ) {
            param($Xml)
            $Xml | Set-Content $solution
            { Invoke-CiBenchmarkTestLevel -SolutionPath $solution -Level L1 -ResultsRoot $TestDrive } | Should -Throw
            Should -Invoke Invoke-SolutionTests -Times 0
        }

        It 'preserves the canonical failure for an applicable level with no executed tests' {
            Mock Invoke-SolutionTests { throw 'No tests executed' }
            { Invoke-CiBenchmarkTestLevel -SolutionPath $solution -Level L0 -ResultsRoot $TestDrive } | Should -Throw '*No tests executed*'
        }
    }
}

Describe 'Additive sequential benchmark workflow contract' {
    It 'stops later levels after failure in <Solution> and always retains evidence' -TestCases @(
        @{ Solution = 'mississippi' }, @{ Solution = 'samples' }
    ) {
        param($Solution)
        $path = Join-Path $PSScriptRoot "../../../.github/workflows/$Solution-validation-benchmark.yml"
        $workflow = Get-Content -LiteralPath $path -Raw
        $workflow | Should -Not -Match 'continue-on-error|strategy:|matrix:|pull_request_target'
        $stages = @([regex]::Matches($workflow, '-Stage (Build|L0|L1|L2)\b') | ForEach-Object { $_.Groups[1].Value })
        ($stages -join ',') | Should -Be 'Build,L0,L1,L2'
        $workflow.IndexOf('Cache versioned Aspire') | Should -BeGreaterThan $workflow.IndexOf('-Stage L1')
        $workflow | Should -Match '(?s)Publish phase and cache summary\s+if: always\(\)'
        $workflow | Should -Match '(?s)Retain benchmark and test evidence\s+if: always\(\)'
        if ($Solution -eq 'samples') {
            $workflow.IndexOf('Run Spring L3') | Should -BeGreaterThan $workflow.IndexOf('-Stage L2')
            $workflow | Should -Match '-PrebuiltReceiptPath artifacts/ci-benchmark/build.json'
        }
    }
}
