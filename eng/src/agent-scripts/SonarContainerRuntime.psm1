#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Copy-SonarAnalyzerAssembly {
    param([string]$ContainerPath,[string]$PrivateCache,[string]$Projection)
    if ($ContainerPath -cnotmatch '^/cache/[A-Za-z0-9_/.-]+\.dll$' -or $ContainerPath.Split('/') -contains '..') { throw 'Unexpected analyzer assembly path.' }
    $relative = $ContainerPath.Substring(7)
    $source = Join-Path $PrivateCache $relative
    $file = Get-Item -LiteralPath $source -Force
    if ($file.PSIsContainer -or $file.Length -gt 67108864) { throw 'Analyzer assembly must be a bounded regular file.' }
    $cursor = $file
    while ($cursor -and $cursor.FullName.Length -ge $PrivateCache.Length) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Analyzer assemblies cannot contain symbolic links.' }
        $cursor = if($cursor -is [IO.FileInfo]){$cursor.Directory}else{$cursor.Parent}
    }
    $destination = Join-Path $Projection $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    return [pscustomobject]@{Source=$destination;Target=$ContainerPath}
}

function Get-SonarBuildContainerMounts {
    param([string]$BuildPackages,[string]$ScannerAssets,[object[]]$AnalyzerMounts)
    if (-not $BuildPackages) { throw 'Sonar build requires a separate tokenless package directory.' }
    if (-not $ScannerAssets) { throw 'Sonar build requires immutable scanner assets.' }
    if (@($AnalyzerMounts | Where-Object Target -CEQ '/work/.sonarqube/conf/SonarQubeAnalysisConfig.xml').Count -ne 1) { throw 'Invalid analyzer mounts: exact trusted root configuration required.' }
    $arguments = @('--env=NUGET_PACKAGES=/packages','--mount',"type=bind,source=$BuildPackages,target=/packages",'--mount',"type=bind,source=$ScannerAssets/bin,target=/work/.sonarqube/bin,readonly")
    foreach ($mount in $AnalyzerMounts) {
        if (-not [IO.Path]::IsPathFullyQualified($mount.Source) -or $mount.Source.Contains(',') -or $mount.Source.Contains("`n") -or $mount.Target -cnotmatch '^/(?:work/\.sonarqube/conf|cache)/[A-Za-z0-9_/.-]+$' -or $mount.Target.Split('/') -contains '..') { throw 'Invalid read-only Sonar analyzer mount.' }
        $arguments += @('--mount',"type=bind,source=$($mount.Source),target=$($mount.Target),readonly")
    }
    return $arguments
}

function Assert-SonarContainerMountPaths {
    param([string[]]$Paths)
    foreach ($path in ($Paths | Where-Object { $_ })) {
        if ($path.Contains(',') -or $path.Contains("`n") -or -not [IO.Path]::IsPathFullyQualified($path)) { throw 'Sonar container mount paths must be explicit and safe.' }
    }
}

function Invoke-SonarNative {
    param([string]$Executable,[string[]]$Arguments)
    & $Executable @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Trusted Sonar command failed: $Executable." }
}

function Get-SonarContainerArguments {
    param([string]$Tools,[string]$Driver,[string]$Workspace,[string]$Cache,[ValidateSet('Prepare','Version','Begin','Build','End')][string]$Phase,[string]$Version,[string]$ScannerAssets,[string]$BuildPackages,[object[]]$AnalyzerMounts=@(),[ValidateRange(1,65535)][int]$UserId=1000,[ValidateRange(1,65535)][int]$GroupId=1000)
    $image = 'mcr.microsoft.com/dotnet/sdk@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317'
    $arguments = @('run','--rm','--read-only','--cap-drop=ALL','--security-opt=no-new-privileges',"--user=${UserId}:$GroupId",'--pids-limit=512','--tmpfs=/tmp:rw,nosuid,nodev,size=4g','--env=HOME=/tmp/home','--env=XDG_CACHE_HOME=/tmp/home/.cache','--env=DOTNET_CLI_HOME=/tmp/home','--env=DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1','--env=DOTNET_NOLOGO=1','--env=DOTNET_CLI_TELEMETRY_OPTOUT=1','--env=SONAR_USER_HOME=/cache')
    Assert-SonarContainerMountPaths -Paths @($Tools,$Driver,$Workspace,$Cache,$ScannerAssets,$BuildPackages)
    $toolMode = if ($Phase -ceq 'Prepare') { '' } else { ',readonly' }
    $arguments += @('--mount',"type=bind,source=$Tools,target=/tools$toolMode",'--mount',"type=bind,source=$Driver,target=/driver,readonly")
    if ($Workspace) { $arguments += @('--mount',"type=bind,source=$Workspace,target=/work",'--workdir=/work') }
    if ($Cache -and $Phase -in @('Begin','End')) { $arguments += @('--mount',"type=bind,source=$Cache,target=/cache") }
    if ($Phase -in @('Begin','End')) { $arguments += @('--env=SONAR_ANALYSIS_TOKEN','--env=TMPDIR=/cache/tmp') }
    if ($Phase -ceq 'Build') { $arguments += @(Get-SonarBuildContainerMounts -BuildPackages $BuildPackages -ScannerAssets $ScannerAssets -AnalyzerMounts $AnalyzerMounts) }
    $arguments += @($image,'pwsh','-NoLogo','-NoProfile','-File','/driver/invoke-sonar-container-stage.ps1','-Phase',$Phase)
    if ($Version) { $arguments += @('-Version',$Version) }
    return $arguments
}

function Invoke-SonarContainer {
    param([string]$Tools,[string]$Driver,[string]$Workspace,[string]$Cache,[ValidateSet('Prepare','Version','Begin','Build','End')][string]$Phase,[string]$Version,[string]$ScannerAssets,[string]$BuildPackages,[object[]]$AnalyzerMounts=@(),[ValidateRange(1,65535)][int]$UserId=1000,[ValidateRange(1,65535)][int]$GroupId=1000)
    $arguments = Get-SonarContainerArguments @PSBoundParameters
    Invoke-SonarNative -Executable docker -Arguments $arguments
}

function Initialize-SonarSourceWorkspace {
    param([string]$Path,[string]$Repository,[string]$Revision,[string]$Branch,[Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$DefaultBranch,[Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$TargetRef)
    if ($Repository -cne 'Gibbs-Morris/mississippi' -or $Revision -cnotmatch '^[0-9a-f]{40}$') { throw 'Invalid immutable Sonar checkout identity.' }
    if (Test-Path -LiteralPath $Path) { throw 'Sonar source checkout must start in a fresh directory.' }
    Invoke-SonarNative git @('init','--initial-branch=sonar-source',$Path)
    Invoke-SonarNative git @('-C',$Path,'config','core.hooksPath','/dev/null')
    Invoke-SonarNative git @('-C',$Path,'remote','add','origin',"https://github.com/$Repository.git")
    $refs=@("+refs/heads/${DefaultBranch}:refs/remotes/origin/$DefaultBranch")
    if($TargetRef -cne $DefaultBranch){$refs+="+refs/heads/${TargetRef}:refs/remotes/origin/$TargetRef"}
    Invoke-SonarNative git (@('-C',$Path,'fetch','--no-tags','origin')+$refs+@($Revision))
    Invoke-SonarNative git @('-C',$Path,'checkout','--no-guess','-b',$Branch,$Revision)
    if (Test-Path (Join-Path $Path '.sonarqube')) { throw 'Candidate source cannot supply scanner state.' }
}

function Assert-SonarSourceWorkspaceClean {
    param([string]$Path,[string]$Revision)
    $head = (& git -c "safe.directory=$Path" -C $Path rev-parse HEAD | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $head -cne $Revision) { throw 'Sonar upload workspace revision changed.' }
    $changes = @(& git -c "safe.directory=$Path" -C $Path diff --name-only HEAD --)
    if ($LASTEXITCODE -ne 0 -or $changes.Count -ne 0) { throw 'Sonar upload requires unchanged tracked source.' }
}

function Assert-SonarAssetCredentialAbsent {
    param([string]$Directory,[string]$Token)
    if (-not $Token) { throw 'Sonar credential was not supplied.' }
    foreach ($file in (Get-ChildItem -LiteralPath $Directory -File -Recurse -Force)) {
        if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Trusted scanner assets cannot contain symbolic links.' }
        if ([Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($file.FullName)).Contains($Token,[StringComparison]::Ordinal)) { throw 'Scanner assets contain the protected credential.' }
    }
}

function Copy-SonarAnalyzerProjection {
    param([string]$Scanner,[string]$PrivateCache,[string]$Projection,[string]$BuildRoot,[string]$Token)
    Assert-SonarAssetCredentialAbsent -Directory $Scanner -Token $Token
    $mounts = [Collections.Generic.List[object]]::new()
    $conf = Join-Path $Scanner 'conf'
    foreach ($file in (Get-ChildItem -LiteralPath $conf -File -Recurse -Force)) {
        $relative = [IO.Path]::GetRelativePath($conf,$file.FullName).Replace('\','/')
        if ($relative -cnotmatch '^[A-Za-z0-9_/.-]+$') { throw 'Unexpected trusted scanner configuration path.' }
        $destination = Join-Path $BuildRoot ".sonarqube/conf/$relative"
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        [IO.File]::WriteAllText($destination,'')
        $mounts.Add([pscustomobject]@{Source=$file.FullName;Target="/work/.sonarqube/conf/$relative"})
    }
    $settings = [Xml.XmlReaderSettings]::new(); $settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit; $settings.XmlResolver=$null
    $reader = [Xml.XmlReader]::Create((Join-Path $conf 'SonarQubeAnalysisConfig.xml'),$settings)
    try { $config=[Xml.XmlDocument]::new();$config.XmlResolver=$null;$config.Load($reader) }
    finally { $reader.Dispose() }
    $paths = @($config.SelectNodes("//*[local-name()='AnalyzersSettings']//*[local-name()='Path']") | ForEach-Object InnerText | Sort-Object -Unique)
    foreach ($path in $paths) {
        if ($path.StartsWith('/work/.sonarqube/conf/',[StringComparison]::Ordinal)) {
            if (-not @($mounts | Where-Object Target -CEQ $path).Count) { throw 'Required analyzer configuration is missing.' }
            continue
        }
        $mounts.Add((Copy-SonarAnalyzerAssembly -ContainerPath $path -PrivateCache $PrivateCache -Projection $Projection))
    }
    Assert-SonarAssetCredentialAbsent -Directory $Projection -Token $Token
    return $mounts.ToArray()
}
Export-ModuleMember -Function Copy-SonarAnalyzerProjection,Get-SonarContainerArguments,Invoke-SonarContainer,Initialize-SonarSourceWorkspace,Assert-SonarSourceWorkspaceClean,Assert-SonarAssetCredentialAbsent
