#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Pester module coverage for Sonar' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $source = Join-Path $repoRoot 'eng/src/agent-scripts/measure-powershell-coverage.ps1'
        $tokens = $null
        $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($source,[ref]$tokens,[ref]$errors)
        if ($errors.Count -gt 0) { throw 'Coverage script syntax errors.' }
        foreach ($function in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] },$false)) {
            . ([scriptblock]::Create($function.Extent.Text))
        }
    }
    BeforeEach {
        $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $sourceDirectory = Join-Path $root 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path (Join-Path $sourceDirectory 'tasks') -Force | Out-Null
        $module = Join-Path $sourceDirectory 'Fixture.psm1'
        $nestedModule = Join-Path $sourceDirectory 'tasks/Nested.psm1'
        Set-Content -LiteralPath $module -Value @('line one','line two','line three','line four','line five')
        Set-Content -LiteralPath $nestedModule -Value @('line one','line two','line three')
        $destination = Join-Path $root 'coverage.xml'
        $coverage = [pscustomobject]@{
            CommandsExecuted = @([pscustomobject]@{File=$module;Line=2})
            CommandsMissed = @([pscustomobject]@{File=$module;Line=4})
        }
        function Invoke-Writer { Write-ModuleCoverage -Coverage $coverage -ModulePaths @($module,$nestedModule) -Root $root -Destination $destination }
    }
    It 'exports actual covered and missed lines in Sonar generic format' {
        Invoke-Writer
        [xml]$report = Get-Content -LiteralPath $destination -Raw
        $report.coverage.version | Should -Be '1'
        $report.coverage.file.path | Should -Be 'eng/src/agent-scripts/Fixture.psm1'
        @($report.coverage.file.lineToCover.lineNumber) | Should -Be @('2','4')
        @($report.coverage.file.lineToCover.covered) | Should -Be @('true','false')
    }
    It 'marks a partially covered line covered without inventing branch counts' {
        $coverage.CommandsMissed += [pscustomobject]@{File=$module;Line=2}
        Invoke-Writer
        [xml]$report = Get-Content -LiteralPath $destination -Raw
        @($report.coverage.file.lineToCover).Count | Should -Be 2
        @($report.coverage.file.lineToCover)[0].covered | Should -Be 'true'
        @($report.SelectNodes('//*[@branchesToCover or @coveredBranches]')).Count | Should -Be 0
    }
    It 'reports an entirely unexecuted module without treating missed lines as hits' {
        $coverage.CommandsExecuted = @()
        $coverage.CommandsMissed = @([pscustomobject]@{File=$module;Line=1},[pscustomobject]@{File=$module;Line=3})
        Invoke-Writer
        [xml]$report = Get-Content -LiteralPath $destination -Raw
        @($report.coverage.file.lineToCover.covered) | Should -Be @('false','false')
    }
    It 'exports nested module paths relative to the checkout' {
        $coverage.CommandsExecuted += [pscustomobject]@{File=$nestedModule;Line=1}
        Invoke-Writer
        [xml]$report = Get-Content -LiteralPath $destination -Raw
        @($report.coverage.file.path) | Should -Be @('eng/src/agent-scripts/Fixture.psm1','eng/src/agent-scripts/tasks/Nested.psm1')
    }
    It 'rejects <Case>' -TestCases @(
        @{Case='zero line'},@{Case='negative line'},@{Case='non-integer line'},@{Case='line beyond source'},@{Case='outside selected modules'},@{Case='empty coverage'}
    ) {
        param($Case)
        $message = 'invalid source line'
        switch ($Case) {
            'zero line' { $coverage.CommandsExecuted[0].Line=0 }
            'negative line' { $coverage.CommandsExecuted[0].Line=-1 }
            'non-integer line' { $coverage.CommandsExecuted[0].Line='2' }
            'line beyond source' { $coverage.CommandsExecuted[0].Line=10 }
            'outside selected modules' { $coverage.CommandsExecuted[0].File=Join-Path $root 'other.psm1'; $message='outside the selected modules' }
            'empty coverage' { $coverage.CommandsExecuted=@();$coverage.CommandsMissed=@();$message='no module coverage commands' }
        }
        { Invoke-Writer } | Should -Throw "*$message*"
        Test-Path -LiteralPath $destination | Should -BeFalse
    }
    It 'rejects a selected source outside the repository automation directory' {
        $outside = Join-Path $root 'other.psm1'
        Set-Content -LiteralPath $outside -Value 'line one'
        { Write-ModuleCoverage -Coverage $coverage -ModulePaths @($outside) -Root $root -Destination $destination } | Should -Throw '*inside the repository automation source*'
    }
    It 'writes a real Pester coverage result with its source line numbers' {
        $case = Join-Path $root 'Small.Tests.ps1'
        Set-Content -LiteralPath $module -Value @('function Get-FixtureValue { return 42 }','Export-ModuleMember -Function Get-FixtureValue')
        $fixtureText = "Describe 'real coverage' { BeforeAll { Import-Module '$($module.Replace("'","''"))' -Force }; It 'calls the function' { Get-FixtureValue | Should -Be 42 } }"
        Set-Content -LiteralPath $case -Value $fixtureText
        $driver = Join-Path $root 'measure.ps1'
        $driverText = @"
param([string]`$Helper,[string]`$TestFile,[string]`$Module,[string]`$Root,[string]`$Destination)
Set-StrictMode -Version Latest
`$ErrorActionPreference='Stop'
Import-Module Pester -MinimumVersion 5.7.1
`$tokens=`$null;`$errors=`$null
`$ast=[Management.Automation.Language.Parser]::ParseFile(`$Helper,[ref]`$tokens,[ref]`$errors)
foreach (`$function in `$ast.FindAll({param(`$node) `$node -is [Management.Automation.Language.FunctionDefinitionAst]},`$false)) {
    . ([scriptblock]::Create(`$function.Extent.Text))
}
`$config=New-PesterConfiguration
`$config.Run.Path=`$TestFile;`$config.Run.PassThru=`$true
`$config.CodeCoverage.Enabled=`$true;`$config.CodeCoverage.Path=`$Module
`$config.CodeCoverage.OutputPath=Join-Path `$Root 'pester.xml'
`$config.Output.Verbosity='None'
`$result=Invoke-Pester -Configuration `$config
if (`$result.Result -ne 'Passed') { throw 'Real coverage fixture failed.' }
Write-ModuleCoverage -Coverage `$result.CodeCoverage -ModulePaths @(`$Module) -Root `$Root -Destination `$Destination
"@
        Set-Content -LiteralPath $driver -Value $driverText
        & pwsh -NoProfile -File $driver -Helper $source -TestFile $case -Module $module -Root $root -Destination $destination
        $LASTEXITCODE | Should -Be 0
        [xml]$report=Get-Content -LiteralPath $destination -Raw
        @($report.coverage.file.lineToCover | Where-Object covered -EQ 'true').Count | Should -BeGreaterThan 0
    }
    It 'combines real coverage from isolated test files without sharing module mocks' {
        $testDirectory = Join-Path $root 'eng/tests/agent-scripts'
        New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
        Set-Content -LiteralPath $module -Value @(
            'function Get-Value { return 1 }',
            'function Get-First { Get-Value }',
            'function Get-Second { Get-Value }',
            'function Get-Missed { return 3 }',
            'Export-ModuleMember -Function *'
        )
        foreach ($name in @('First','Second')) {
            $fixtureText = @"
BeforeDiscovery { Import-Module '$($module.Replace("'","''"))' -Force }
Describe '$name coverage' {
    InModuleScope Fixture {
        It 'keeps its mock scope' {
            Mock Get-Value { 42 }
            Get-$name | Should -Be 42
            Should -Invoke Get-Value -Times 1 -Exactly
        }
    }
}
"@
            Set-Content -LiteralPath (Join-Path $testDirectory "$name.Tests.ps1") -Value $fixtureText
        }
        Measure-IsolatedModuleCoverage -Root $root -ModulePaths @($module,$nestedModule) -Destination $destination -Worker $source
        [xml]$report = Get-Content -LiteralPath $destination -Raw
        $file = @($report.coverage.file | Where-Object path -EQ 'eng/src/agent-scripts/Fixture.psm1')[0]
        @($file.lineToCover | Where-Object lineNumber -In @('2','3') | Where-Object covered -EQ 'true').Count | Should -Be 2
        @($file.lineToCover | Where-Object lineNumber -EQ '4')[0].covered | Should -Be 'false'
    }
    It 'rejects an empty coverage test discovery' {
        New-Item -ItemType Directory -Path (Join-Path $root 'eng/tests/agent-scripts') -Force | Out-Null
        { Measure-IsolatedModuleCoverage -Root $root -ModulePaths @($module) -Destination $destination -Worker $source } | Should -Throw '*No PowerShell coverage test files*'
        Test-Path -LiteralPath $destination | Should -BeFalse
    }
    It 'does not publish coverage when a child test fails' {
        $testDirectory = Join-Path $root 'eng/tests/agent-scripts'
        New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
        Set-Content -LiteralPath $module -Value 'function Get-FixtureValue { return 42 }; Export-ModuleMember -Function *'
        Set-Content -LiteralPath (Join-Path $testDirectory 'Failing.Tests.ps1') -Value "Describe 'failure' { It 'fails' { 1 | Should -Be 2 } }"
        { Measure-IsolatedModuleCoverage -Root $root -ModulePaths @($module) -Destination $destination -Worker $source } | Should -Throw '*coverage failed for Failing.Tests.ps1*'
        Test-Path -LiteralPath $destination | Should -BeFalse
    }
    It 'collects the report before analysis and supplies its exact path to Sonar' {
        $workflow=Get-Content (Join-Path $repoRoot '.github/workflows/sonar-cloud.yml') -Raw
        $workflow.IndexOf('Collect PowerShell module coverage') | Should -BeLessThan $workflow.IndexOf('Sonar restore-build-report')
        $workflow | Should -Match "measure-powershell-coverage.ps1 -OutputPath \(Join-Path \`$env:RUNNER_TEMP 'powershell-coverage.xml'\)"
        $workflow | Should -Match '/d:"sonar.coverageReportPaths=\$RUNNER_TEMP/powershell-coverage.xml"'
    }
}
