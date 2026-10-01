#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-PesterCoverageSourcePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$RepositoryRoot
    )

    $fullPath = [IO.Path]::GetFullPath($SourcePath, $RepositoryRoot)
    $relativePath = [IO.Path]::GetRelativePath($RepositoryRoot, $fullPath)
    if ([IO.Path]::IsPathRooted($relativePath) -or $relativePath -eq '..' -or
        $relativePath.StartsWith('..' + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
        throw "Coverage source is outside the repository: $SourcePath"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Coverage source does not exist: $SourcePath"
    }
    return $relativePath.Replace('\', '/')
}

function ConvertTo-SonarCoverageReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$InputPath,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string]$RepositoryRoot
    )

    [xml]$pesterReport = Get-Content -LiteralPath $InputPath -Raw
    [xml]$sonarReport = '<coverage version="1"/>'
    $fileCount = 0
    $lineCount = 0
    $coveredCount = 0
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    foreach ($sourceFile in ($pesterReport.SelectNodes('/report/package/sourcefile') | Sort-Object name)) {
        $sourcePath = $sourceFile.GetAttribute('name')
        $relativePath = Resolve-PesterCoverageSourcePath -SourcePath $sourcePath -RepositoryRoot $root
        $lineGroups = @($sourceFile.SelectNodes('line') | Group-Object nr | Sort-Object { [int]$_.Name })
        if ($lineGroups.Count -eq 0) { continue }
        $file = $sonarReport.CreateElement('file')
        $file.SetAttribute('path', $relativePath)
        foreach ($lineGroup in $lineGroups) {
            $lineNumber = [int]$lineGroup.Name
            if ($lineNumber -le 0) { throw 'Coverage line numbers must be positive.' }
            $covered = @($lineGroup.Group | Where-Object { [int]$_.ci -gt 0 }).Count -gt 0
            $line = $sonarReport.CreateElement('lineToCover')
            $line.SetAttribute('lineNumber', $lineNumber.ToString([Globalization.CultureInfo]::InvariantCulture))
            $line.SetAttribute('covered', $covered.ToString().ToLowerInvariant())
            $file.AppendChild($line) | Out-Null
            $lineCount++
            if ($covered) { $coveredCount++ }
        }
        $sonarReport.DocumentElement.AppendChild($file) | Out-Null
        $fileCount++
    }
    if ($fileCount -eq 0) { throw 'No Pester line coverage found.' }
    $sonarReport.Save([IO.Path]::GetFullPath($OutputPath))
    [pscustomobject]@{ Files = $fileCount; Lines = $lineCount; Covered = $coveredCount }
}

Export-ModuleMember -Function ConvertTo-SonarCoverageReport
