#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Mutation summary report aggregation' {
    BeforeEach {
        $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $scriptDirectory = Join-Path $repo 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $scriptDirectory -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $repo '.git') | Out-Null
        foreach ($file in @('summarize-mutation-survivors.ps1', 'RepositoryAutomation.psm1', 'TaskAutomation.psm1')) {
            Copy-Item (Join-Path $PSScriptRoot "../../src/agent-scripts/$file") $scriptDirectory
        }
        $summaryScript = Join-Path $scriptDirectory 'summarize-mutation-survivors.ps1'
        $reportRoot = Join-Path $repo '.scratchpad/mutation-test-results'
        foreach ($project in @('First', 'Second')) {
            $reportDirectory = Join-Path $reportRoot "2026-09-04.12-00-00/$project/2026-09-04.12-00-01/reports"
            New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
            $report = @{
                projectRoot = Join-Path $repo "src/$project"
                files = @{ 'File.cs' = @{ mutants = @(@{ id = '1'; status = 'Survived'; mutatorName = 'Boolean'; replacement = 'false'; location = @{ start = @{ line = 1; column = 1 }; end = @{ line = 1; column = 5 } } }) } }
            }
            $report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $reportDirectory 'mutation-report.json')
        }
        $oldDirectory = Join-Path $reportRoot '2026-09-03.12-00-00/reports'
        New-Item -ItemType Directory -Path $oldDirectory -Force | Out-Null
        Set-Content (Join-Path $oldDirectory 'mutation-report.json') 'invalid stale report'
    }

    It 'combines every project report in the latest run using source-relative paths' {
        & pwsh -NoProfile -File $summaryScript -SkipMutationRun -GenerateTasks | Out-Host
        $LASTEXITCODE | Should -Be 0
        $summary = Get-Content (Join-Path $reportRoot 'mutation-survivors-enriched.json') -Raw | ConvertFrom-Json
        $summary.totalSurvivors | Should -Be 2
        $summary.report.paths.Count | Should -Be 2
        @($summary.survivors.RelativeFile -replace '\\', '/') | Should -Contain 'src/First/File.cs'
        @($summary.survivors.RelativeFile -replace '\\', '/') | Should -Contain 'src/Second/File.cs'
    }

    It 'does not use an older report when the newest run has no report' {
        New-Item -ItemType Directory -Path (Join-Path $reportRoot '2026-09-05.12-00-00') | Out-Null
        & pwsh -NoProfile -File $summaryScript -SkipMutationRun 2>&1 | Out-Host
        $LASTEXITCODE | Should -Not -Be 0
    }

    It 'summarizes a valid run with no survivors successfully' {
        Get-ChildItem (Join-Path $reportRoot '2026-09-04.12-00-00') -Recurse -Filter 'mutation-report.json' |
            ForEach-Object { Set-Content $_.FullName '{"files":{}}' }
        & pwsh -NoProfile -File $summaryScript -SkipMutationRun -GenerateTasks | Out-Host
        $LASTEXITCODE | Should -Be 0
        $summary = Get-Content (Join-Path $reportRoot 'mutation-survivors-enriched.json') -Raw | ConvertFrom-Json
        $summary.totalSurvivors | Should -Be 0
        (Get-Content (Join-Path $reportRoot 'mutation-survivors-summary.json') -Raw).Trim() | Should -Be '[]'
    }
}
