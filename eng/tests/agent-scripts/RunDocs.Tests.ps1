#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Documentation runtime prerequisites' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $scriptPath = Join-Path $repoRoot 'run-docs.ps1'

        function Invoke-DocsScript {
            param([string]$Version = 'v24.0.0', [int]$NodeExit = 0, [switch]$MissingNode)

            $shimRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $shimRoot | Out-Null
            $npmLog = Join-Path $shimRoot 'npm-arguments.json'
            if (-not $MissingNode) {
                $nodeScript = "Write-Output '$Version'" + [Environment]::NewLine + "exit $NodeExit"
                Set-Content -LiteralPath (Join-Path $shimRoot 'node.ps1') -Value $nodeScript
            }
            Set-Content -LiteralPath (Join-Path $shimRoot 'npm.ps1') -Value ('$args | ConvertTo-Json -AsArray -Compress | Set-Content -LiteralPath $env:MISSISSIPPI_RUN_DOCS_TEST_LOG' + [Environment]::NewLine + 'exit 0')
            $originalPath = $env:PATH
            $originalLog = $env:MISSISSIPPI_RUN_DOCS_TEST_LOG
            $env:PATH = $shimRoot + [IO.Path]::PathSeparator + $PSHOME
            $env:MISSISSIPPI_RUN_DOCS_TEST_LOG = $npmLog
            try {
                try {
                    $output = & $scriptPath -Mode Build -SkipInstall 2>&1 | Out-String
                    $exitCode = $LASTEXITCODE
                }
                catch {
                    $output = $_.Exception.Message
                    $exitCode = 1
                }
                $npmArguments = if (Test-Path -LiteralPath $npmLog) {
                    @(Get-Content -LiteralPath $npmLog -Raw | ConvertFrom-Json)
                }
                else {
                    @()
                }
                [pscustomobject]@{ ExitCode = $exitCode; Output = $output; NpmArguments = @($npmArguments) }
            }
            finally {
                $env:PATH = $originalPath
                $env:MISSISSIPPI_RUN_DOCS_TEST_LOG = $originalLog
            }
        }
    }

    It 'rejects a missing node executable before invoking npm' {
        $outcome = Invoke-DocsScript -MissingNode

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js was not found on PATH'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'rejects unsupported or malformed node output <Version> before invoking npm' -ForEach @(
        @{ Version = 'v20.0.0' }
        @{ Version = 'v22.0.0' }
        @{ Version = 'v23.0.0' }
        @{ Version = 'not-a-version' }
        @{ Version = 'v24.0' }
    ) {
        $outcome = Invoke-DocsScript -Version $Version

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js 24 or newer is required'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'rejects a failed node version command even when its output is supported' {
        $outcome = Invoke-DocsScript -NodeExit 7

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js 24 or newer is required'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'runs the requested npm command for supported node version <Version>' -ForEach @(
        @{ Version = 'v24.0.0' }
        @{ Version = 'v25.0.0' }
        @{ Version = '24.0.0' }
    ) {
        $outcome = Invoke-DocsScript -Version $Version

        $outcome.ExitCode | Should -Be 0
        $outcome.NpmArguments.Count | Should -Be 2
        $outcome.NpmArguments[0] | Should -Be 'run'
        $outcome.NpmArguments[1] | Should -Be 'build'
    }
}

Describe 'Pester coverage import' {
    BeforeAll {
        $coverageModule = Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))) 'eng/src/agent-scripts/PesterCoverage.psm1'
        Import-Module $coverageModule -Force
    }

    BeforeEach {
        $coverageRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $coverageRoot | Out-Null
        Set-Content -LiteralPath (Join-Path $coverageRoot 'source & helper.ps1') -Value 'Write-Output 1'
        $inputPath = Join-Path $coverageRoot 'pester.xml'
        $outputPath = Join-Path $coverageRoot 'sonar.xml'
    }

    It 'preserves executed and missed lines, merges duplicate lines, and escapes file names' {
        Set-Content -LiteralPath $inputPath -Value '<report><package name="repo"><sourcefile name="source &amp; helper.ps1"><line nr="4" ci="0"/><line nr="4" ci="1"/><line nr="8" ci="0"/></sourcefile></package></report>'

        $summary = ConvertTo-SonarCoverageReport -InputPath $inputPath -OutputPath $outputPath -RepositoryRoot $coverageRoot

        $summary.Files | Should -Be 1
        $summary.Lines | Should -Be 2
        $summary.Covered | Should -Be 1
        [xml]$report = Get-Content -LiteralPath $outputPath -Raw
        $report.coverage.version | Should -Be '1'
        $report.coverage.file.path | Should -Be 'source & helper.ps1'
        $report.coverage.file.lineToCover[0].lineNumber | Should -Be '4'
        $report.coverage.file.lineToCover[0].covered | Should -Be 'true'
        $report.coverage.file.lineToCover[1].covered | Should -Be 'false'
    }

    It 'rejects an empty coverage report' {
        Set-Content -LiteralPath $inputPath -Value '<report/>'

        { ConvertTo-SonarCoverageReport -InputPath $inputPath -OutputPath $outputPath -RepositoryRoot $coverageRoot } |
            Should -Throw '*No Pester line coverage found*'
    }

    It 'rejects a source outside the scanned repository' {
        Set-Content -LiteralPath $inputPath -Value '<report><package><sourcefile name="../outside.ps1"><line nr="1" ci="1"/></sourcefile></package></report>'

        { ConvertTo-SonarCoverageReport -InputPath $inputPath -OutputPath $outputPath -RepositoryRoot $coverageRoot } |
            Should -Throw '*outside the repository*'
    }

    It 'rejects missing source files' {
        Set-Content -LiteralPath $inputPath -Value '<report><package><sourcefile name="missing.ps1"><line nr="1" ci="1"/></sourcefile></package></report>'

        { ConvertTo-SonarCoverageReport -InputPath $inputPath -OutputPath $outputPath -RepositoryRoot $coverageRoot } |
            Should -Throw '*does not exist*'
    }

    It 'rejects nonpositive line numbers' {
        Set-Content -LiteralPath $inputPath -Value '<report><package><sourcefile name="source &amp; helper.ps1"><line nr="0" ci="1"/></sourcefile></package></report>'

        { ConvertTo-SonarCoverageReport -InputPath $inputPath -OutputPath $outputPath -RepositoryRoot $coverageRoot } |
            Should -Throw '*line numbers must be positive*'
    }
}
