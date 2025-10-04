#requires -Module Pester

$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$automationModulePath = [System.IO.Path]::GetFullPath($automationModulePath)
Import-Module -Name $automationModulePath -Force



$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/summarize-coverage-gaps.ps1'
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

        & $scriptPath -CoverageReportPath $reportPath -Threshold 75 -EmitTasks -RepoRoot $testRepo | Out-Null

        $pendingDir = Join-Path $testRepo '.scratchpad/tasks/pending'
        Test-Path -LiteralPath $pendingDir | Should Be $true
  $taskFiles = @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.json')
  $taskFiles.Count | Should Be 1

        $taskJson = Get-Content -LiteralPath $taskFiles[0].FullName -Raw | ConvertFrom-Json
  $taskJson.category | Should Be 'coverage'
  ($taskJson.autoTaskKey -replace '\\','/') | Should Be 'coverage|src/Core/Sample.cs'
        $taskJson.coverage.coveragePercent | Should Be 50
        $taskJson.coverage.threshold | Should Be 75
        $taskJson.notes | Should Match 'Uncovered lines: 10, 12'

        $summaryPath = Join-Path $testRepo '.scratchpad/coverage-test-results/coverage-gaps-summary.json'
        Test-Path -LiteralPath $summaryPath | Should Be $true
        $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
        $summary.threshold | Should Be 75
  $summaryItems = @($summary.items)
  $summaryItems.Count | Should Be 1
  ($summaryItems[0].relativePath -replace '\\','/') | Should Be 'src/Core/Sample.cs'
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
        Test-Path -LiteralPath $pendingDir | Should Be $false
    }
}

