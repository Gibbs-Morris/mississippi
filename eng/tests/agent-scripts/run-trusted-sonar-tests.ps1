#!/usr/bin/env pwsh

[CmdletBinding()]
param([switch]$PassThru)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$files = @('TrustedSonarAnalysis.Tests.ps1','SonarReportHandoff.Tests.ps1','SonarContainerRuntime.Tests.ps1','SonarQualityPolicy.Tests.ps1')
$results = @()
foreach ($file in $files) {
    $results += & (Join-Path $PSScriptRoot 'run-pester-suite.ps1') -TestPath (Join-Path $PSScriptRoot $file) -PassThru
}
$total = ($results | Measure-Object TotalCount -Sum).Sum
$passed = ($results | Measure-Object PassedCount -Sum).Sum
$failed = ($results | Measure-Object FailedCount -Sum).Sum
$successful = @($results | Where-Object { $_.Result -ne 'Passed' }).Count -eq 0 -and $total -gt 0
$result = [pscustomobject]@{Result=$(if($successful){'Passed'}else{'Failed'});TotalCount=$total;PassedCount=$passed;FailedCount=$failed}
if ($PassThru) { return $result }
if ($successful) { exit 0 }
exit 1
