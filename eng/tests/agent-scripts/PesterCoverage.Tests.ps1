#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

    It 'writes relative output in the PowerShell current location' {
        Set-Content -LiteralPath $inputPath -Value '<report><package><sourcefile name="source &amp; helper.ps1"><line nr="1" ci="1"/></sourcefile></package></report>'
        $processDirectory = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $processDirectory | Out-Null
        $originalProcessDirectory = [Environment]::CurrentDirectory
        $originalLocation = Get-Location
        try {
            [Environment]::CurrentDirectory = $processDirectory
            Set-Location -LiteralPath $coverageRoot

            $summary = ConvertTo-SonarCoverageReport -InputPath ./pester.xml -OutputPath ./sonar.xml -RepositoryRoot $coverageRoot

            Test-Path -LiteralPath $outputPath | Should -BeTrue
            Test-Path -LiteralPath (Join-Path $processDirectory 'sonar.xml') | Should -BeFalse
            $summary.Covered | Should -Be 1
            [xml]$report = Get-Content -LiteralPath $outputPath -Raw
            $report.coverage.file.lineToCover.covered | Should -Be 'true'
        }
        finally {
            Set-Location -LiteralPath $originalLocation.Path
            [Environment]::CurrentDirectory = $originalProcessDirectory
        }
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
