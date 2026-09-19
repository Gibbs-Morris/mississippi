#requires -Module Pester

$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$automationModulePath = [System.IO.Path]::GetFullPath($automationModulePath)
Import-Module -Name $automationModulePath -Force



$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$script:scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/summarize-coverage-gaps.ps1'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
    throw "summarize-coverage-gaps.ps1 not found at '$scriptPath'"
}

Describe 'summarize-coverage-gaps.ps1' {
    It 'creates coverage tasks for files below the threshold' {
        $testRepo = Join-Path $TestDrive 'repo-coverage-tasks'
        New-Item -ItemType Directory -Path $testRepo | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $testRepo '.git') | Out-Null

        $sourceFile = Join-Path $testRepo 'src/Core/Sample.cs'
        New-Item -ItemType Directory -Path (Split-Path -Parent $sourceFile) -Force | Out-Null
        Set-Content -LiteralPath $sourceFile -Value "// sample"

        $coverageXml = @'
<?xml version="1.0"?>
<coverage line-rate="0.5" branch-rate="0" version="1.9" timestamp="0">
  <packages>
    <package name="Company.Core" line-rate="0.5" branch-rate="0">
      <classes>
        <class name="Company.Core.Sample" filename="src/Core/Sample.cs" line-rate="0.5" branch-rate="0">
          <lines>
            <line number="10" hits="0" />
            <line number="11" hits="1" />
            <line number="12" hits="0" />
            <line number="13" hits="2" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
'@
        $reportPath = Join-Path $testRepo 'coverage.cobertura.xml'
        Set-Content -LiteralPath $reportPath -Value $coverageXml

        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $scriptPath -CoverageReportPath $reportPath -Threshold 75 -EmitTasks -RepoRoot $testRepo | Out-Null
        $LASTEXITCODE | Should -Be 0

        $pendingDir = Join-Path $testRepo '.scratchpad/tasks/pending'
        Test-Path -LiteralPath $pendingDir | Should -Be $true
  $taskFiles = @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.json')
  $taskFiles.Count | Should -Be 1

        $taskJson = Get-Content -LiteralPath $taskFiles[0].FullName -Raw | ConvertFrom-Json
  $taskJson.category | Should -Be 'coverage'
  ($taskJson.autoTaskKey -replace '\\','/') | Should -Be 'coverage|src/Core/Sample.cs'
        $taskJson.coverage.coveragePercent | Should -Be 50
        $taskJson.coverage.threshold | Should -Be 75
        $taskJson.notes | Should -Match 'Uncovered lines: 10, 12'

        $summaryPath = Join-Path $testRepo '.scratchpad/coverage-test-results/coverage-gaps-summary.json'
        Test-Path -LiteralPath $summaryPath | Should -Be $true
        $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
        $summary.threshold | Should -Be 75
  $summaryItems = @($summary.items)
  $summaryItems.Count | Should -Be 1
  ($summaryItems[0].relativePath -replace '\\','/') | Should -Be 'src/Core/Sample.cs'
    }

    It 'supports WhatIf without creating tasks' {
        $testRepo = Join-Path $TestDrive 'repo-coverage-whatif'
        New-Item -ItemType Directory -Path $testRepo | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $testRepo '.git') | Out-Null

        $coverageXml = @'
<?xml version="1.0"?>
<coverage line-rate="0.5" branch-rate="0" version="1.9" timestamp="0">
  <packages>
    <package name="Company.Core" line-rate="0.5" branch-rate="0">
      <classes>
        <class name="Company.Core.Sample" filename="src/Core/Sample.cs" line-rate="0.5" branch-rate="0">
          <lines>
            <line number="1" hits="0" />
            <line number="2" hits="0" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
'@
        $reportPath = Join-Path $testRepo 'coverage.cobertura.xml'
        Set-Content -LiteralPath $reportPath -Value $coverageXml

        & $scriptPath -CoverageReportPath $reportPath -Threshold 80 -EmitTasks -RepoRoot $testRepo -WhatIf | Out-Null

        $pendingDir = Join-Path $testRepo '.scratchpad/tasks/pending'
        Test-Path -LiteralPath $pendingDir | Should -Be $false
    }

    It 'summarizes the explicitly selected report instead of the newest report' {
        $testRepo = Join-Path $TestDrive 'repo-coverage-selection'
        New-Item -ItemType Directory -Path (Join-Path $testRepo '.git'), (Join-Path $testRepo 'src/Old'), (Join-Path $testRepo 'src/New'), (Join-Path $testRepo '.scratchpad/coverage-test-results/newer') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRepo 'src/Old/Old.cs') -Value '// old'
        Set-Content -LiteralPath (Join-Path $testRepo 'src/New/New.cs') -Value '// new'
        $oldXml = @'
<?xml version="1.0"?><coverage><packages><package><classes><class name="Old" filename="src/Old/Old.cs"><lines><line number="1" hits="0" /></lines></class></classes></package></packages></coverage>
'@
        $newXml = @'
<?xml version="1.0"?><coverage><packages><package><classes><class name="New" filename="src/New/New.cs"><lines><line number="1" hits="0" /></lines></class></classes></package></packages></coverage>
'@
        $selectedPath = Join-Path $testRepo 'run-1/coverage.cobertura.xml'
        $newerPath = Join-Path $testRepo '.scratchpad/coverage-test-results/newer/coverage.cobertura.xml'
        New-Item -ItemType Directory -Path (Split-Path -Parent $selectedPath) -Force | Out-Null
        Set-Content -LiteralPath $selectedPath -Value $oldXml
        Set-Content -LiteralPath $newerPath -Value $newXml
        (Get-Item -LiteralPath $newerPath).LastWriteTime = (Get-Date).AddMinutes(5)

        & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $scriptPath -CoverageReportPath $selectedPath -Threshold 95 -RepoRoot $testRepo | Out-Null

        $summary = Get-Content -LiteralPath (Join-Path $testRepo '.scratchpad/coverage-test-results/coverage-gaps-summary.json') -Raw | ConvertFrom-Json
        $summary.reportPath | Should -Be $selectedPath
        (@($summary.items).relativePath | ForEach-Object { $_ -replace '\\', '/' }) | Should -Contain 'src/Old/Old.cs'
        (@($summary.items).relativePath | ForEach-Object { $_ -replace '\\', '/' }) | Should -Not -Contain 'src/New/New.cs'
    }

    It 'fails when an explicit current-run report is missing' {
        $testRepo = Join-Path $TestDrive 'repo-coverage-missing'
        New-Item -ItemType Directory -Path (Join-Path $testRepo '.git'), (Join-Path $testRepo '.scratchpad/coverage-test-results/older') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRepo '.scratchpad/coverage-test-results/older/coverage.cobertura.xml') -Value '<coverage />'
        $missingPath = Join-Path $testRepo 'current/coverage.cobertura.xml'

        $output = & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $scriptPath -CoverageReportPath $missingPath -RepoRoot $testRepo 2>&1 | Out-String

        $LASTEXITCODE | Should -Not -Be 0
        $output | Should -Match 'Coverage report not found'
    }

    It 'fails on malformed explicit coverage XML' {
        $testRepo = Join-Path $TestDrive 'repo-coverage-malformed'
        New-Item -ItemType Directory -Path (Join-Path $testRepo '.git') -Force | Out-Null
        $reportPath = Join-Path $testRepo 'coverage.cobertura.xml'
        Set-Content -LiteralPath $reportPath -Value '<coverage><broken>'

        $output = & (Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })) -NoProfile -File $scriptPath -CoverageReportPath $reportPath -RepoRoot $testRepo 2>&1 | Out-String

        $LASTEXITCODE | Should -Not -Be 0
        $output | Should -Match 'XML|Document|parse'
    }
}

