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
        $manifest = @()
        foreach ($project in @('First', 'Second')) {
            $reportDirectory = Join-Path $reportRoot "2026-09-04.12-00-00/$project/2026-09-04.12-00-01/reports"
            New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
            $report = @{
                projectRoot = Join-Path $repo "src/$project"
                files = @{ 'File.cs' = @{ mutants = @(@{ id = '1'; status = 'Survived'; mutatorName = 'Boolean'; replacement = 'false'; location = @{ start = @{ line = 1; column = 1 }; end = @{ line = 1; column = 5 } } }) } }
            }
            $report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $reportDirectory 'mutation-report.json')
            $manifest += @{ Project = $project; Status = 'Completed'; ReportPath = (Join-Path $reportDirectory 'mutation-report.json') }
        }
        $manifestPath = Join-Path $reportRoot '2026-09-04.12-00-00/project-results.json'
        ConvertTo-Json -InputObject @{ Scope = 'Solution'; Projects = $manifest } -Depth 6 | Set-Content $manifestPath
        $oldDirectory = Join-Path $reportRoot '2026-09-03.12-00-00/reports'
        New-Item -ItemType Directory -Path $oldDirectory -Force | Out-Null
        Set-Content (Join-Path $oldDirectory 'mutation-report.json') 'invalid stale report'
    }

    It 'combines every project report in the latest run using source-relative paths' {
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun -GenerateTasks | Out-Host
        $LASTEXITCODE | Should -Be 0
        $summary = Get-Content (Join-Path $reportRoot 'mutation-survivors-enriched.json') -Raw | ConvertFrom-Json
        $summary.totalSurvivors | Should -Be 2
        $summary.report.paths.Count | Should -Be 2
        @($summary.survivors.RelativeFile -replace '\\', '/') | Should -Contain 'src/First/File.cs'
        @($summary.survivors.RelativeFile -replace '\\', '/') | Should -Contain 'src/Second/File.cs'
    }

    It 'does not use an older report when the newest run has no report' {
        New-Item -ItemType Directory -Path (Join-Path $reportRoot '2026-09-05.12-00-00') | Out-Null
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun 2>&1 | Out-Host
        $LASTEXITCODE | Should -Not -Be 0
    }

    It 'summarizes a valid run with no survivors successfully' {
        Get-ChildItem (Join-Path $reportRoot '2026-09-04.12-00-00') -Recurse -Filter 'mutation-report.json' |
            ForEach-Object { Set-Content $_.FullName '{"files":{}}' }
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun -GenerateTasks | Out-Host
        $LASTEXITCODE | Should -Be 0
        $summary = Get-Content (Join-Path $reportRoot 'mutation-survivors-enriched.json') -Raw | ConvertFrom-Json
        $summary.totalSurvivors | Should -Be 0
        (Get-Content (Join-Path $reportRoot 'mutation-survivors-summary.json') -Raw).Trim() | Should -Be '[]'
    }

    It 'rejects a missing report from one declared target' {
        Remove-Item -LiteralPath $manifest[0].ReportPath
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun 2>&1 | Out-Null
        $LASTEXITCODE | Should -Not -Be 0
    }

    It 'rejects an unfinished target even when another report is available' {
        $manifest[0].Status = 'Pending'
        ConvertTo-Json -InputObject @{ Scope = 'Solution'; Projects = $manifest } -Depth 6 | Set-Content $manifestPath
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun 2>&1 | Out-Null
        $LASTEXITCODE | Should -Not -Be 0
    }

    It 'retains valid reports from failed score runs for survivor analysis' {
        $manifest[0].Status = 'Failed'
        ConvertTo-Json -InputObject @{ Scope = 'Solution'; Projects = $manifest } -Depth 6 | Set-Content $manifestPath
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun | Out-Null
        $LASTEXITCODE | Should -Be 0
    }

    It 'ignores a newer focused run for repository summaries' {
        $focused = Join-Path $reportRoot '2026-09-06.12-00-00'
        New-Item -ItemType Directory -Path $focused | Out-Null
        Set-Content (Join-Path $focused 'project-results.json') '{"Scope":"Project","Projects":[{"Status":"Pending"}]}'
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun | Out-Null
        $LASTEXITCODE | Should -Be 0
        (Get-Content (Join-Path $reportRoot 'mutation-survivors-enriched.json') -Raw | ConvertFrom-Json).totalSurvivors | Should -Be 2
    }

    It 'writes an explicitly selected focused summary only within that run' {
        ConvertTo-Json -InputObject @{ Scope = 'Project'; Projects = $manifest } -Depth 6 | Set-Content $manifestPath
        $selected = Split-Path -Parent $manifestPath
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun -RunPath $selected | Out-Null
        $LASTEXITCODE | Should -Be 0
        Test-Path (Join-Path $selected 'mutation-survivors-enriched.json') | Should -BeTrue
        Test-Path (Join-Path $reportRoot 'mutation-survivors-enriched.json') | Should -BeFalse
        Test-Path (Join-Path $repo '.scratchpad/testing/mutation-survivors-summary.md') | Should -BeFalse
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun -RunPath $selected -GenerateTasks 2>&1 | Out-Null
        $LASTEXITCODE | Should -Not -Be 0
    }

    It 'rejects a report from outside the selected run' {
        $manifest[0].ReportPath = Join-Path $oldDirectory 'mutation-report.json'
        ConvertTo-Json -InputObject @{ Scope = 'Solution'; Projects = $manifest } -Depth 6 | Set-Content $manifestPath
        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $summaryScript -SkipMutationRun 2>&1 | Out-Null
        $LASTEXITCODE | Should -Not -Be 0
    }
}
