#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-SonarRegularPath {
    param([string]$Path,[string]$Root,[long]$MaximumBytes=67108864)

    $full = [IO.Path]::GetFullPath($Path)
    $base = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (-not $full.StartsWith($base,$comparison)) { throw 'Sonar report path is outside its workspace.' }
    $item = Get-Item -LiteralPath $full -Force
    if ($item.PSIsContainer -or $item.Length -gt $MaximumBytes) { throw 'Sonar report must be a bounded regular file.' }
    $cursor = $item
    while ($null -ne $cursor -and $cursor.FullName.Length -ge $base.TrimEnd([IO.Path]::DirectorySeparatorChar).Length) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Sonar handoff cannot contain symbolic links.' }
        $cursor = if ($cursor -is [IO.FileInfo]) { $cursor.Directory } else { $cursor.Parent }
    }
    return $full
}

function Read-SonarHandoffXml {
    param([string]$Path,[string]$Root)

    $file = Assert-SonarRegularPath -Path $Path -Root $Root
    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 67108864
    $reader = [Xml.XmlReader]::Create($file,$settings)
    try {
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        return $document
    }
    finally { $reader.Dispose() }
}

function ConvertTo-SonarWorkspaceRelativePath {
    param([string]$ContainerPath)

    if (-not $ContainerPath.StartsWith('/work/',[StringComparison]::Ordinal)) { throw 'Sonar analysis path must be inside /work.' }
    $relative = $ContainerPath.Substring(6)
    if (-not $relative -or $relative -match '[\\\x00-\x1f:*?]' -or @($relative.Split('/') | Where-Object { $_ -in @('','.', '..') }).Count -gt 0) { throw 'Sonar analysis path contains unsafe components.' }
    if ($relative -match '^\.git(?:/|$)') { throw 'Sonar reports cannot refer to private Git or scanner executables.' }
    return $relative
}

function Get-SonarHandoffFiles {
    param([string]$Root,[string]$Directory)

    $pending = [Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($Directory)
    $files = [Collections.Generic.List[string]]::new()
    while ($pending.Count -gt 0) {
        $folder = $pending.Dequeue()
        $item = Get-Item -LiteralPath $folder -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Sonar handoff cannot contain symbolic links.' }
        foreach ($entry in (Get-ChildItem -LiteralPath $folder -Force)) {
            if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Sonar handoff cannot contain symbolic links.' }
            if ($entry.PSIsContainer) { $pending.Enqueue($entry.FullName) }
            else { $files.Add((Assert-SonarRegularPath -Path $entry.FullName -Root $Root)) }
            if ($files.Count + $pending.Count -gt 20000) { throw 'Sonar handoff has too many entries.' }
        }
    }
    return $files.ToArray()
}

function Assert-SonarProjectReport {
    param([string]$Path,[string]$Root,[Collections.Generic.HashSet[string]]$ProjectIdentities,[Collections.Generic.HashSet[string]]$Files)

    $document = Read-SonarHandoffXml -Path $Path -Root $Root
    $project = $document.DocumentElement
    if ($project.LocalName -cne 'ProjectInfo' -or $project.NamespaceURI -cne 'http://www.sonarsource.com/msbuild/integration/2015/1') { throw 'Invalid Sonar project report schema.' }
    $allowed = @('ProjectName','ProjectLanguage','ProjectType','ProjectGuid','FullPath','IsExcluded','AnalysisResultFiles','AnalysisSettings','Configuration','Platform','TargetFramework','Encoding')
    $childrenSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($child in $project.ChildNodes) {
        if ($child -is [Xml.XmlElement] -and ($child.LocalName -notin $allowed -or $child.NamespaceURI -cne $project.NamespaceURI -or -not $childrenSeen.Add($child.LocalName))) { throw 'Unexpected Sonar project report element.' }
        if ($child -is [Xml.XmlElement] -and $child.LocalName -notin @('AnalysisResultFiles','AnalysisSettings') -and ($child.InnerText -match '[\x00-\x1f]' -or @($child.ChildNodes | Where-Object { $_ -is [Xml.XmlElement] }).Count -gt 0)) { throw 'Sonar project metadata must contain scalar values without control characters.' }
    }
    if ($project.ProjectLanguage -cne 'C#' -or $project.ProjectType -notin @('Product','Test') -or $project.IsExcluded -notin @('true','false')) { throw 'Invalid Sonar project classification.' }
    $projectPath = ConvertTo-SonarWorkspaceRelativePath -ContainerPath ([string]$project.FullPath)
    if ($projectPath -cnotmatch '\.csproj$') { throw 'Sonar project path must identify a C# project.' }
    Assert-SonarRegularPath -Path (Join-Path $Root $projectPath) -Root $Root | Out-Null
    $guid = [Guid]::Parse([string]$project.ProjectGuid)
    $identity = "$guid|$projectPath|$($project.TargetFramework)|$($project.Configuration)|$($project.Platform)"
    if (-not $ProjectIdentities.Add($identity)) { throw 'Duplicate Sonar project identity.' }
    $index = [IO.DirectoryInfo]::new([IO.Path]::GetDirectoryName($Path)).Name
    if ($index -cnotmatch '^[0-9]+(?:_[0-9]+)?$') { throw 'Invalid Sonar project output directory.' }
    $prefix = "/work/.sonarqube/out/$index"
    $settingsSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $approved = @('sonar.cs.roslyn.reportFilePaths','sonar.cs.analyzer.projectOutPaths','sonar.cs.scanner.telemetry')
    foreach ($setting in @($project.AnalysisSettings.Property)) {
        $name = $setting.GetAttribute('Name')
        if ($setting.Attributes.Count -ne 1 -or $setting.LocalName -cne 'Property') { throw 'Unexpected Sonar setting schema.' }
        if ($name -cnotin $approved -or -not $settingsSeen.Add($name)) { throw 'Sonar project report contains unapproved analysis settings.' }
        $value = [string]$setting.InnerText
        $locations = if ($name -ceq 'sonar.cs.roslyn.reportFilePaths') { $value.Split('|') } else { @($value) }
        foreach ($location in $locations) {
            if ($location -cne $prefix -and -not $location.StartsWith("$prefix/",[StringComparison]::Ordinal)) { throw 'Sonar analysis setting refers outside its project output.' }
            $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath $location
            if ($name -cne 'sonar.cs.analyzer.projectOutPaths') {
                Assert-SonarRegularPath -Path (Join-Path $Root $relative) -Root $Root | Out-Null
                $Files.Add($relative) | Out-Null
            }
        }
    }
    foreach ($result in @($project.AnalysisResultFiles.AnalysisResultFile)) {
        if ($result.Attributes.Count -ne 2 -or $result.GetAttribute('Id') -cne 'FilesToAnalyze' -or $result.GetAttribute('Location') -cne "/work/.sonarqube/conf/$index/FilesToAnalyze.txt") { throw 'Unexpected Sonar analysis result path.' }
        $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath ($result.GetAttribute('Location'))
        $file = Assert-SonarRegularPath -Path (Join-Path $Root $relative) -Root $Root -MaximumBytes 8388608
        $Files.Add($relative) | Out-Null
        foreach ($line in [IO.File]::ReadAllLines($file)) {
            $source = ConvertTo-SonarWorkspaceRelativePath -ContainerPath $line
            if ($source -match '^\.sonarqube(?:/|$)') { throw 'Scanner configuration cannot be analyzed as source.' }
            Assert-SonarRegularPath -Path (Join-Path $Root $source) -Root $Root | Out-Null
            $Files.Add($source) | Out-Null
            if ($Files.Count -gt 20000) { throw 'Sonar source handoff has too many entries.' }
        }
    }
    $Files.Add([IO.Path]::GetRelativePath($Root,$Path).Replace('\','/')) | Out-Null
}

function Assert-SonarSarifLocation {
    param([object]$Location)
    $uri = if ($Location.PSObject.Properties['resultFile']) { [string]$Location.resultFile.uri }
        elseif ($Location.PSObject.Properties['physicalLocation']) { [string]$Location.physicalLocation.artifactLocation.uri }
        else { throw 'Unrecognized SARIF source location.' }
    if ($uri.StartsWith('file://',[StringComparison]::Ordinal)) { $uri = ([Uri]$uri).LocalPath }
    $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath $uri
    if ($relative -match '^\.sonarqube(?:/|$)') { throw 'SARIF cannot refer to scanner configuration.' }
}

function Assert-SonarJsonReport {
    param([string]$Path,[string]$Root)
    $file = Assert-SonarRegularPath -Path $Path -Root $Root
    if ([IO.Path]::GetFileName($Path) -cmatch '^Telemetry(?:\.Targets\.S4NET)?\.json$') {
        foreach ($line in [IO.File]::ReadAllLines($file)) {
            $record = $line | ConvertFrom-Json -Depth 8
            if ($null -eq $record -or @($record.PSObject.Properties).Count -ne 1) { throw 'Invalid Sonar telemetry record.' }
            foreach ($property in $record.PSObject.Properties) {
                if ($property.Name -cnotmatch '^dotnetenterprise\.s4net\.build\.[a-z_]+(?:\.cnt)?$' -or $property.Value -isnot [string]) { throw 'Unapproved Sonar telemetry property.' }
            }
        }
        return
    }
    $report = [IO.File]::ReadAllText($file) | ConvertFrom-Json -Depth 100
    if ($null -eq $report) { throw 'Sonar JSON report is empty.' }
    # Rule help links are data; only diagnostic source locations identify workspace files.
    if (-not $report.PSObject.Properties['runs']) { return }
    foreach ($run in @($report.runs)) {
        if (-not $run.PSObject.Properties['results']) { continue }
        foreach ($result in @($run.results)) {
            if (-not $result.PSObject.Properties['locations']) { continue }
            foreach ($location in @($result.locations)) { Assert-SonarSarifLocation -Location $location }
        }
    }
}

function Assert-SonarCoverageReport {
    param([string]$Path,[string]$Root,[bool]$PowerShell)
    $document = Read-SonarHandoffXml -Path $Path -Root $Root
    if (-not $PowerShell) {
        if ($document.DocumentElement.LocalName -cne 'results') { throw 'Invalid .NET coverage report.' }
        foreach ($file in $document.SelectNodes('//source_file[@path]')) {
            $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath $file.GetAttribute('path')
            if ($relative -match '^\.sonarqube(?:/|$)') { throw 'Coverage cannot refer to scanner configuration.' }
        }
        return
    }
    $coverage = $document.DocumentElement
    if ($coverage.LocalName -cne 'coverage' -or $coverage.GetAttribute('version') -cne '1') { throw 'Invalid PowerShell coverage report.' }
    foreach ($file in $coverage.ChildNodes) {
        if ($file.LocalName -cne 'file') { throw 'Invalid PowerShell coverage file.' }
        $relative = $file.GetAttribute('path')
        if ($relative -cnotmatch '^eng/src/agent-scripts/[A-Za-z0-9_/.-]+\.psm1$') { throw 'PowerShell coverage must identify automation modules.' }
        ConvertTo-SonarWorkspaceRelativePath -ContainerPath "/work/$relative" | Out-Null
        $source = Assert-SonarRegularPath -Path (Join-Path $Root $relative) -Root $Root
        $length = [IO.File]::ReadAllLines($source).Length
        foreach ($line in $file.ChildNodes) {
            $number = 0
            if ($line.LocalName -cne 'lineToCover' -or -not [int]::TryParse($line.GetAttribute('lineNumber'),[ref]$number) -or $number -lt 1 -or $number -gt $length -or $line.GetAttribute('covered') -cnotin @('true','false')) { throw 'Invalid PowerShell coverage line.' }
        }
    }
}

function Assert-SonarDestinationAncestors {
    param([string]$Path,[string]$Root)
    $base = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $cursor = [IO.Path]::GetDirectoryName($Path)
    while ($cursor.Length -ge $base.Length) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Sonar destination cannot contain symbolic links.' }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
        if (-not $cursor) { break }
    }
}
function Copy-ValidatedSonarHandoff {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$BuildRoot,[Parameter(Mandatory)][string]$UploadRoot)

    $build = [IO.Path]::GetFullPath($BuildRoot)
    $upload = [IO.Path]::GetFullPath($UploadRoot)
    if ($build -ceq $upload) { throw 'Sonar upload requires a separate clean workspace.' }
    $files = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $identities = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $out = Join-Path $build '.sonarqube/out'
    $outputs = @(Get-SonarHandoffFiles -Root $build -Directory $out)
    $projects = @($outputs | Where-Object { [IO.Path]::GetFileName($_) -ceq 'ProjectInfo.xml' })
    if ($projects.Count -lt 1) { throw 'Sonar handoff contains no project reports.' }
    foreach ($project in $projects) { Assert-SonarProjectReport -Path $project -Root $build -ProjectIdentities $identities -Files $files }
    foreach ($file in $outputs) {
        switch -CaseSensitive ([IO.Path]::GetExtension($file)) {
            '.json' { Assert-SonarJsonReport -Path $file -Root $build }
            '.pb' { }
            '.xml' { if ([IO.Path]::GetFileName($file) -cne 'ProjectInfo.xml') { throw 'Unexpected XML in Sonar output.' } }
            default { throw 'Unexpected file type in Sonar output.' }
        }
        $files.Add([IO.Path]::GetRelativePath($build,$file).Replace('\','/')) | Out-Null
    }
    foreach ($report in @('coverage.xml','powershell-coverage.xml')) {
        Assert-SonarCoverageReport -Path (Join-Path $build $report) -Root $build -PowerShell:($report -ceq 'powershell-coverage.xml')
        $files.Add($report) | Out-Null
    }
    if ($files.Count -gt 20000) { throw 'Sonar handoff has too many files.' }
    $total = 0L
    $manifest = [Collections.Generic.List[object]]::new()
    foreach ($relative in ($files | Sort-Object)) {
        $source = Assert-SonarRegularPath -Path (Join-Path $build $relative) -Root $build
        $total += [IO.FileInfo]::new($source).Length
        if ($total -gt 2147483648) { throw 'Sonar handoff exceeds its total size bound.' }
        $destination = [IO.Path]::GetFullPath((Join-Path $upload $relative))
        Assert-SonarDestinationAncestors -Path $destination -Root $upload
        if (Test-Path -LiteralPath $destination) {
            $existing = Assert-SonarRegularPath -Path $destination -Root $upload
            if ((Get-FileHash -LiteralPath $source).Hash -cne (Get-FileHash -LiteralPath $existing).Hash) { throw 'Sonar handoff would replace trusted source or configuration.' }
        }
        else {
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            Copy-Item -LiteralPath $source -Destination $destination
        }
        $manifest.Add([pscustomobject]@{Path=$relative;Bytes=[IO.FileInfo]::new($source).Length;Sha256=(Get-FileHash -LiteralPath $source).Hash})
    }
    return $manifest.ToArray()
}

Export-ModuleMember -Function Copy-ValidatedSonarHandoff
