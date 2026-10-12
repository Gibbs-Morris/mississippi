#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'ReferenceTrimmer workflow reporting' {
    BeforeAll {
        $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $workflowPath = Join-Path $repositoryRoot '.github/workflows/project-reference-hygiene-gate.yml'
        $workflow = Get-Content -LiteralPath $workflowPath -Raw
        $match = [regex]::Match($workflow, '(?ms)^      - name: Build with ReferenceTrimmer.*?^        run: \|\r?\n(?<Body>.*?)(?=^      - name:)')
        if (-not $match.Success) { throw 'ReferenceTrimmer workflow build step was not found.' }
        $body = [regex]::Replace($match.Groups['Body'].Value, '(?m)^          ', '')
        $body = $body.Replace('${{ matrix.solution }}', 'mississippi.slnx')
        $workflowScript = [scriptblock]::Create($body)
    }

    BeforeEach {
        $savedEnvironment = @{}
        foreach ($name in @('GITHUB_WORKSPACE', 'RUNNER_TEMP', 'GITHUB_STEP_SUMMARY', 'CONFIGURATION')) {
            $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
        }
        $savedNativeExitCode = $global:LASTEXITCODE
        $env:GITHUB_WORKSPACE = $TestDrive
        $env:RUNNER_TEMP = $TestDrive
        $env:GITHUB_STEP_SUMMARY = Join-Path $TestDrive 'summary.md'
        $env:CONFIGURATION = 'Release'
    }

    AfterEach {
        foreach ($name in $savedEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name])
        }
        $global:LASTEXITCODE = $savedNativeExitCode
    }

    It 'reports an RT0002 finding even when warnings fail the build' {
        Mock dotnet {
            $global:LASTEXITCODE = 1
            'error RT0002: ProjectReference Unused.csproj can be removed [Owner.csproj]'
            'error RT0002: ProjectReference Unused.csproj can be removed [Owner.csproj]'
        }

        { & $workflowScript } | Should -Throw '*RT0002 findings detected*'
        $summary = Get-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Raw
        $summary | Should -Match 'Found 1 removable project reference'
        $summary | Should -Match '\| `Owner.csproj` \| `Unused.csproj` \|'
    }

    It 'retains an unrelated compiler failure and does not report a clean gate' {
        Mock dotnet {
            $global:LASTEXITCODE = 1
            'error CS1000: Compilation failed'
        }

        { & $workflowScript } | Should -Throw '*Build failed with exit code 1*'
        Get-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Raw | Should -Not -Match 'No RT0002 findings'
    }

    It 'fails on a finding even if the compiler returns success' {
        Mock dotnet {
            $global:LASTEXITCODE = 0
            'warning RT0002: ProjectReference Unused.csproj can be removed [Owner.csproj]'
        }

        { & $workflowScript } | Should -Throw '*RT0002 findings detected*'
    }

    It 'reports success only after a successful build with no findings' {
        Mock dotnet {
            $global:LASTEXITCODE = 0
            'Build succeeded.'
        }

        { & $workflowScript } | Should -Not -Throw
        Get-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Raw | Should -Match 'No RT0002 findings'
    }
}
