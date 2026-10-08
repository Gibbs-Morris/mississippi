#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DriverPath,
    [Parameter(Mandatory)][ValidateSet('Prepare','Version','Begin','Build','End')][string]$Phase,
    [Parameter(Mandatory)][string]$CapturePath,
    [string]$FailAt,
    [ValidateSet('None','SONAR_ANALYSIS_TOKEN','SONAR_TOKEN','GH_TOKEN','GITHUB_TOKEN')][string]$TokenName='None'
)

Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

# This process executes the real dispatcher, with every filesystem/tool side effect replaced.
$global:fixtureCalls=[Collections.Generic.List[object]]::new()
$global:fixtureNativeCount=0
$global:LASTEXITCODE=0
foreach($name in @('SONAR_ANALYSIS_TOKEN','SONAR_TOKEN','GH_TOKEN','GITHUB_TOKEN')){
    [Environment]::SetEnvironmentVariable($name,$null,'Process')
}
if($TokenName -cne 'None'){[Environment]::SetEnvironmentVariable($TokenName,'fixture-credential','Process')}

function Add-FixtureCall {
    param([string]$Executable,[string[]]$Arguments)
    $global:fixtureCalls.Add([pscustomobject]@{Executable=$Executable;Arguments=@($Arguments)})
}
function Invoke-FixtureNative {
    param([string]$Executable,[string[]]$Arguments)
    $global:fixtureNativeCount++
    Add-FixtureCall -Executable $Executable -Arguments $Arguments
    $global:LASTEXITCODE=if($FailAt -ceq "native-$global:fixtureNativeCount"){17}else{0}
    if($Executable -ceq '/tools/bin/dotnet-gitversion'){'{"SemVer":"3.4.5"}'}
}
function global:dotnet {Invoke-FixtureNative -Executable dotnet -Arguments $args}
function global:pwsh {Invoke-FixtureNative -Executable pwsh -Arguments $args}
foreach($executable in @('/tools/bin/dotnet-gitversion','/tools/bin/dotnet-sonarscanner','/tools/bin/dotnet-coverage')){
    $shim=[scriptblock]::Create("Invoke-FixtureNative -Executable '$executable' -Arguments `$args")
    Set-Item -LiteralPath "Function:global:$executable" -Value $shim
}
function global:New-Item {
    param([string]$ItemType,[string]$Path,[switch]$Force)
    Add-FixtureCall -Executable New-Item -Arguments @($ItemType,$Path,[string][bool]$Force)
}
function global:Set-Location {
    param([string]$Path)
    Add-FixtureCall -Executable Set-Location -Arguments @($Path)
    if($Path -cne '/work'){throw 'Unexpected fixture working directory.'}
}
function global:Get-Content {
    param([string]$Path,[switch]$Raw)
    if($Path -cne '/driver/analysis-arguments.json' -or -not $Raw){throw 'Unexpected fixture read.'}
    @('/d:sonar.branch.name=fixture-source',('/d:sonar.scm.revision='+('a'*40)))|ConvertTo-Json
}
function global:Set-Content {
    param([string]$Path,[Parameter(ValueFromPipeline)][object]$Value)
    process {Add-FixtureCall -Executable Set-Content -Arguments @($Path,[string]$Value)}
}
function global:Save-Module {
    param([string]$Name,[string]$RequiredVersion,[string]$Path,[string]$Repository,[switch]$Force)
    Add-FixtureCall -Executable Save-Module -Arguments @($Name,$RequiredVersion,$Path,$Repository,[string][bool]$Force)
    if($FailAt -ceq 'Save-Module'){throw 'Fixture module download failed.'}
}

$stageExit=1
try {
    & $DriverPath -Phase $Phase -Version '1.2.3'
    $stageExit=$LASTEXITCODE
}
finally {
    $capture=[pscustomobject]@{Calls=@($global:fixtureCalls.ToArray());NativeCount=$global:fixtureNativeCount;ModulePath=$env:PSModulePath}
    [IO.File]::WriteAllText($CapturePath,($capture|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
}
exit $stageExit
