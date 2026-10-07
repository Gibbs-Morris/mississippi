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

function Assert-SonarProjectChild {
    param([Xml.XmlNode]$Child,[string]$Namespace,[string[]]$Allowed,[Collections.Generic.HashSet[string]]$Seen)
    if ($Child -isnot [Xml.XmlElement]) { return }
    if ($Child.LocalName -notin $Allowed -or $Child.NamespaceURI -cne $Namespace -or -not $Seen.Add($Child.LocalName)) { throw 'Unexpected Sonar project report element.' }
    if ($Child.LocalName -notin @('AnalysisResultFiles','AnalysisSettings') -and ($Child.InnerText -match '[\x00-\x1f]' -or @($Child.ChildNodes | Where-Object { $_ -is [Xml.XmlElement] }).Count -gt 0)) { throw 'Sonar project metadata must contain scalar values without control characters.' }
}

function Get-SonarProjectMetadata {
    param([string]$Path,[string]$Root)
    $document = Read-SonarHandoffXml -Path $Path -Root $Root
    $project = $document.DocumentElement
    if ($project.LocalName -cne 'ProjectInfo' -or $project.NamespaceURI -cne 'http://www.sonarsource.com/msbuild/integration/2015/1') { throw 'Invalid Sonar project report schema.' }
    $allowed = @('ProjectName','ProjectLanguage','ProjectType','ProjectGuid','FullPath','IsExcluded','AnalysisResultFiles','AnalysisSettings','Configuration','Platform','TargetFramework','Encoding')
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($child in $project.ChildNodes) { Assert-SonarProjectChild -Child $child -Namespace $project.NamespaceURI -Allowed $allowed -Seen $seen }
    if ($project.ProjectLanguage -cne 'C#' -or $project.ProjectType -notin @('Product','Test') -or $project.IsExcluded -notin @('true','false')) { throw 'Invalid Sonar project classification.' }
    return $project
}

function Get-SonarProjectOutputContext {
    param([Xml.XmlElement]$Project,[string]$Path,[string]$Root,[Collections.Generic.HashSet[string]]$ProjectIdentities)
    $projectPath = ConvertTo-SonarWorkspaceRelativePath -ContainerPath ([string]$Project.FullPath)
    if ($projectPath -cnotmatch '\.csproj$') { throw 'Sonar project path must identify a C# project.' }
    Assert-SonarRegularPath -Path (Join-Path $Root $projectPath) -Root $Root | Out-Null
    $guid = [Guid]::Parse([string]$Project.ProjectGuid)
    $index = [IO.DirectoryInfo]::new([IO.Path]::GetDirectoryName($Path)).Name
    if ($index -cnotmatch '^(?<CoreIndex>[0-9]+(?:_[0-9]+)?)(?<Razor>\.Razor)?$') { throw 'Invalid Sonar project output directory.' }
    $coreIndex = $Matches['CoreIndex']
    $compilation = if ($Matches['Razor']) { 'Razor' } else { 'Core' }
    $identity = "$guid|$projectPath|$($Project.TargetFramework)|$($Project.Configuration)|$($Project.Platform)|$compilation"
    if (-not $ProjectIdentities.Add($identity)) { throw 'Duplicate Sonar project identity.' }
    return [pscustomobject]@{Prefix="/work/.sonarqube/out/$index";CoreIndex=$coreIndex}
}

function Add-SonarProjectSettingFiles {
    param([string]$Name,[string]$Value,[string]$Prefix,[string]$Root,[Collections.Generic.HashSet[string]]$Files)
    $locations = if ($Name -ceq 'sonar.cs.roslyn.reportFilePaths') { $Value.Split('|') } else { @($Value) }
    foreach ($location in $locations) {
        if ($location -cne $Prefix -and -not $location.StartsWith("$Prefix/",[StringComparison]::Ordinal)) { throw 'Sonar analysis setting refers outside its project output.' }
        $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath $location
        if ($Name -cne 'sonar.cs.analyzer.projectOutPaths') {
            Assert-SonarRegularPath -Path (Join-Path $Root $relative) -Root $Root | Out-Null
            $Files.Add($relative) | Out-Null
        }
    }
}

function Add-SonarProjectSettings {
    param([Xml.XmlElement]$Project,[string]$Prefix,[string]$Root,[Collections.Generic.HashSet[string]]$Files)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $approved = @('sonar.cs.roslyn.reportFilePaths','sonar.cs.analyzer.projectOutPaths','sonar.cs.scanner.telemetry')
    foreach ($setting in @($Project.AnalysisSettings.Property)) {
        $name = $setting.GetAttribute('Name')
        if ($setting.Attributes.Count -ne 1 -or $setting.LocalName -cne 'Property') { throw 'Unexpected Sonar setting schema.' }
        if ($name -cnotin $approved -or -not $seen.Add($name)) { throw 'Sonar project report contains unapproved analysis settings.' }
        Add-SonarProjectSettingFiles -Name $name -Value ([string]$setting.InnerText) -Prefix $Prefix -Root $Root -Files $Files
    }
}

function Add-SonarAnalysisSourceFiles {
    param([Xml.XmlElement]$Project,[string]$CoreIndex,[string]$Root,[Collections.Generic.HashSet[string]]$Files)
    foreach ($result in @($Project.AnalysisResultFiles.AnalysisResultFile)) {
        if ($result.Attributes.Count -ne 2 -or $result.GetAttribute('Id') -cne 'FilesToAnalyze' -or $result.GetAttribute('Location') -cne "/work/.sonarqube/conf/$CoreIndex/FilesToAnalyze.txt") { throw 'Unexpected Sonar analysis result path.' }
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
}

function Assert-SonarProjectReport {
    param([string]$Path,[string]$Root,[Collections.Generic.HashSet[string]]$ProjectIdentities,[Collections.Generic.HashSet[string]]$Files)
    $project = Get-SonarProjectMetadata -Path $Path -Root $Root
    $context = Get-SonarProjectOutputContext -Project $project -Path $Path -Root $Root -ProjectIdentities $ProjectIdentities
    Add-SonarProjectSettings -Project $project -Prefix $context.Prefix -Root $Root -Files $Files
    Add-SonarAnalysisSourceFiles -Project $project -CoreIndex $context.CoreIndex -Root $Root -Files $Files
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

function Assert-SonarTelemetryReport {
    param([string]$Path)
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        $record = $line | ConvertFrom-Json -Depth 8
        if ($null -eq $record -or @($record.PSObject.Properties).Count -ne 1) { throw 'Invalid Sonar telemetry record.' }
        foreach ($property in $record.PSObject.Properties) {
            if ($property.Name -cnotmatch '^dotnetenterprise\.s4net\.build\.[a-z_]+(?:\.cnt)?$' -or $property.Value -isnot [string]) { throw 'Unapproved Sonar telemetry property.' }
        }
    }
}

function Assert-SonarSarifReport {
    param([object]$Report)
    foreach ($run in @($Report.runs | Where-Object { $_.PSObject.Properties['results'] })) {
        foreach ($result in @($run.results | Where-Object { $_.PSObject.Properties['locations'] })) {
            foreach ($location in @($result.locations)) { Assert-SonarSarifLocation -Location $location }
        }
    }
}

function Assert-SonarJsonReport {
    param([string]$Path,[string]$Root)
    $file = Assert-SonarRegularPath -Path $Path -Root $Root
    if ([IO.Path]::GetFileName($Path) -cmatch '^Telemetry(?:\.Targets\.S4NET)?\.json$') {
        Assert-SonarTelemetryReport -Path $file
        return
    }
    $report = [IO.File]::ReadAllText($file) | ConvertFrom-Json -Depth 100
    if ($null -eq $report) { throw 'Sonar JSON report is empty.' }
    # Rule help links are data; only diagnostic source locations identify workspace files.
    if (-not $report.PSObject.Properties['runs']) { return }
    Assert-SonarSarifReport -Report $report
}

function Assert-SonarDotNetCoverage {
    param([Xml.XmlDocument]$Document)
    if ($Document.DocumentElement.LocalName -cne 'results') { throw 'Invalid .NET coverage report.' }
    foreach ($source in @($Document.SelectNodes("//*[local-name()='source_file']"))) {
        $relative = ConvertTo-SonarWorkspaceRelativePath -ContainerPath ($source.GetAttribute('path'))
        if ($relative -match '^\.sonarqube(?:/|$)') { throw 'Coverage cannot refer to scanner configuration.' }
    }
}

function Assert-SonarPowerShellCoverageFile {
    param([Xml.XmlElement]$File,[string]$Root)
    $relative = $File.GetAttribute('path')
    if ($relative -cnotmatch '^eng/src/agent-scripts/[A-Za-z0-9_/.-]+\.psm1$') { throw 'PowerShell coverage must identify automation modules.' }
    ConvertTo-SonarWorkspaceRelativePath -ContainerPath "/work/$relative" | Out-Null
    $source = Assert-SonarRegularPath -Path (Join-Path $Root $relative) -Root $Root
    $length = [IO.File]::ReadAllLines($source).Length
    foreach ($line in $File.ChildNodes) {
        $number = 0
        if ($line.LocalName -cne 'lineToCover' -or -not [int]::TryParse($line.GetAttribute('lineNumber'),[ref]$number) -or $number -lt 1 -or $number -gt $length -or $line.GetAttribute('covered') -cnotin @('true','false')) { throw 'Invalid PowerShell coverage line.' }
    }
}

function Assert-SonarCoverageReport {
    param([string]$Path,[string]$Root,[bool]$PowerShell)
    $document = Read-SonarHandoffXml -Path $Path -Root $Root
    if (-not $PowerShell) { Assert-SonarDotNetCoverage -Document $document; return }
    $coverage = $document.DocumentElement
    if ($coverage.LocalName -cne 'coverage' -or $coverage.GetAttribute('version') -cne '1') { throw 'Invalid PowerShell coverage report.' }
    foreach ($file in $coverage.ChildNodes) {
        if ($file.LocalName -cne 'file') { throw 'Invalid PowerShell coverage file.' }
        Assert-SonarPowerShellCoverageFile -File $file -Root $Root
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

function Assert-SonarOutputFile {
    param([string]$Path,[string]$Root)
    switch -CaseSensitive ([IO.Path]::GetExtension($Path)) {
        '.json' { Assert-SonarJsonReport -Path $Path -Root $Root }
        '.pb' { }
        '.xml' { if ([IO.Path]::GetFileName($Path) -cne 'ProjectInfo.xml') { throw 'Unexpected XML in Sonar output.' } }
        default { throw 'Unexpected file type in Sonar output.' }
    }
}

function Copy-SonarHandoffFile {
    param([string]$Source,[string]$Relative,[string]$UploadRoot)
    $destination = [IO.Path]::GetFullPath((Join-Path $UploadRoot $Relative))
    Assert-SonarDestinationAncestors -Path $destination -Root $UploadRoot
    if (Test-Path -LiteralPath $destination) {
        $existing = Assert-SonarRegularPath -Path $destination -Root $UploadRoot
        if ((Get-FileHash -LiteralPath $Source).Hash -cne (Get-FileHash -LiteralPath $existing).Hash) { throw 'Sonar handoff would replace trusted source or configuration.' }
    }
    else {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        Copy-Item -LiteralPath $Source -Destination $destination
    }
    return [pscustomobject]@{Path=$Relative;Bytes=[IO.FileInfo]::new($Source).Length;Sha256=(Get-FileHash -LiteralPath $Source).Hash}
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
        Assert-SonarOutputFile -Path $file -Root $build
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
        $manifest.Add((Copy-SonarHandoffFile -Source $source -Relative $relative -UploadRoot $upload))
    }
    return $manifest.ToArray()
}

Export-ModuleMember -Function Copy-ValidatedSonarHandoff
