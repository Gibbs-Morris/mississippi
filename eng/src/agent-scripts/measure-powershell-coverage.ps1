#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '../../..'),
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$TestPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-ModuleCoverageLine {
    param([object]$Command, [bool]$Covered, [Collections.Generic.Dictionary[string,int]]$ModuleLengths, [string]$Root)

    $file = [IO.Path]::GetFullPath([string]$Command.File)
    if (-not $ModuleLengths.ContainsKey($file)) { throw 'Coverage command is outside the selected modules.' }
    if (($Command.Line -isnot [int] -and $Command.Line -isnot [long]) -or $Command.Line -lt 1 -or $Command.Line -gt $ModuleLengths[$file]) { throw 'Coverage command has an invalid source line.' }
    return [pscustomobject]@{ File = [IO.Path]::GetRelativePath($Root, $file).Replace('\','/'); Line = [int]$Command.Line; Covered = $Covered }
}

function Write-ModuleCoverage {
    param([object]$Coverage, [string[]]$ModulePaths, [string]$Root, [string]$Destination)

    $comparer = if ($IsWindows) { [StringComparer]::OrdinalIgnoreCase } else { [StringComparer]::Ordinal }
    $lengths = [Collections.Generic.Dictionary[string,int]]::new($comparer)
    foreach ($modulePath in $ModulePaths) {
        $fullPath = [IO.Path]::GetFullPath($modulePath)
        $relative = [IO.Path]::GetRelativePath($Root, $fullPath).Replace('\','/')
        if ($relative -notmatch '^eng/src/agent-scripts/.+\.psm1$') { throw 'Coverage module must be inside the repository automation source.' }
        $lengths.Add($fullPath, [IO.File]::ReadAllLines($fullPath).Length)
    }
    $lines = @($Coverage.CommandsExecuted | ForEach-Object { ConvertTo-ModuleCoverageLine -Command $_ -Covered $true -ModuleLengths $lengths -Root $Root })
    $lines += @($Coverage.CommandsMissed | ForEach-Object { ConvertTo-ModuleCoverageLine -Command $_ -Covered $false -ModuleLengths $lengths -Root $Root })
    if ($lines.Count -eq 0) { throw 'Pester returned no module coverage commands.' }
    $settings = [Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [Text.UTF8Encoding]::new($false)
    $writer = [Xml.XmlWriter]::Create($Destination, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('coverage')
        $writer.WriteAttributeString('version','1')
        foreach ($fileGroup in ($lines | Group-Object File | Sort-Object Name)) {
            $writer.WriteStartElement('file')
            $writer.WriteAttributeString('path',$fileGroup.Name)
            foreach ($lineGroup in ($fileGroup.Group | Group-Object Line | Sort-Object { [int]$_.Name })) {
                $writer.WriteStartElement('lineToCover')
                $writer.WriteAttributeString('lineNumber',$lineGroup.Name)
                $covered = @($lineGroup.Group | Where-Object Covered).Count -gt 0
                $writer.WriteAttributeString('covered',$covered.ToString().ToLowerInvariant())
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally { $writer.Dispose() }
}

function Measure-TestFileCoverage {
    param([string]$TestFile, [string[]]$ModulePaths, [string]$Destination)

    Import-Module Pester -MinimumVersion 5.2.0 -ErrorAction Stop
    $configuration = New-PesterConfiguration
    $configuration.Run.Path = $TestFile
    $configuration.Run.PassThru = $true
    $configuration.CodeCoverage.Enabled = $true
    $configuration.CodeCoverage.Path = $ModulePaths
    $configuration.CodeCoverage.OutputFormat = 'JaCoCo'
    $configuration.CodeCoverage.OutputPath = "$Destination.pester.xml"
    $result = Invoke-Pester -Configuration $configuration
    if ($null -eq $result -or $result.TotalCount -lt 1 -or $result.Result -ne 'Passed') { throw 'PowerShell coverage tests failed.' }
    [pscustomobject]@{
        Result = $result.Result
        TotalCount = $result.TotalCount
        CommandsExecuted = @($result.CodeCoverage.CommandsExecuted | Select-Object File,Line)
        CommandsMissed = @($result.CodeCoverage.CommandsMissed | Select-Object File,Line)
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Destination -Encoding utf8NoBOM
}

function Measure-IsolatedModuleCoverage {
    param([string]$Root, [string[]]$ModulePaths, [string]$Destination, [string]$Worker)

    $tests = @(Get-ChildItem -LiteralPath (Join-Path $Root 'eng/tests/agent-scripts') -Filter '*.Tests.ps1' -File -Recurse | Sort-Object FullName)
    if ($tests.Count -eq 0) { throw 'No PowerShell coverage test files were found.' }
    $reports = Join-Path ([IO.Path]::GetDirectoryName($Destination)) ('pester-' + [guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($reports) | Out-Null
    $executed = [Collections.Generic.List[object]]::new()
    $missed = [Collections.Generic.List[object]]::new()
    foreach ($test in $tests) {
        $reportPath = Join-Path $reports ($test.BaseName + '-' + [guid]::NewGuid().ToString('N') + '.json')
        # Discovery imports can replace module mock scopes in another test file.
        # A fresh process keeps coverage and mocks local to their test container.
        & pwsh -NoProfile -File $Worker -RepositoryRoot $Root -TestPath $test.FullName -OutputPath $reportPath
        if ($LASTEXITCODE -ne 0) { throw "PowerShell coverage failed for $($test.Name)." }
        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        if ($report.Result -ne 'Passed' -or $report.TotalCount -lt 1) { throw "Invalid coverage result for $($test.Name)." }
        foreach ($command in $report.CommandsExecuted) { $executed.Add($command) }
        foreach ($command in $report.CommandsMissed) { $missed.Add($command) }
    }
    Write-ModuleCoverage -Coverage ([pscustomobject]@{CommandsExecuted=$executed;CommandsMissed=$missed}) -ModulePaths $ModulePaths -Root $Root -Destination $Destination
}

try {
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    $destination = [IO.Path]::GetFullPath($OutputPath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    $modules = @(Get-ChildItem -LiteralPath (Join-Path $root 'eng/src/agent-scripts') -Filter '*.psm1' -File -Recurse | Select-Object -ExpandProperty FullName)
    if ($modules.Count -eq 0) { throw 'No repository automation modules were found.' }
    if ($TestPath) {
        Measure-TestFileCoverage -TestFile $TestPath -ModulePaths $modules -Destination $destination
    }
    else {
        Measure-IsolatedModuleCoverage -Root $root -ModulePaths $modules -Destination $destination -Worker $PSCommandPath
        Write-Output "PowerShell module coverage written to $destination"
    }
    exit 0
}
catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
