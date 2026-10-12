#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'ReferenceTrimmer project scope' {
    BeforeAll {
        $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $fixtureRoot = Join-Path $TestDrive 'scope'
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repositoryRoot 'Directory.Build.props') -Destination $fixtureRoot

        function Get-ReferenceTrimmerEvaluation {
            param([string]$ProjectPath, [string]$Enabled)

            $projectFile = Join-Path $fixtureRoot $ProjectPath
            New-Item -ItemType Directory -Path (Split-Path -Parent $projectFile) -Force | Out-Null
            Set-Content -LiteralPath $projectFile -Value '<Project Sdk="Microsoft.NET.Sdk" />'
            $arguments = @('msbuild', $projectFile, '-nologo', '-getItem:PackageReference', '-getProperty:NuGetLockFilePath')
            if ($Enabled -ne 'default') { $arguments += "-p:EnableReferenceTrimmer=$Enabled" }
            $output = & dotnet @arguments
            if ($LASTEXITCODE -ne 0) { throw "MSBuild scope evaluation failed: $ProjectPath" }
            return (($output -join [Environment]::NewLine) | ConvertFrom-Json)
        }
    }

    It 'selects <ProjectPath> with opt-in <Enabled>' -ForEach @(
        @{ ProjectPath = 'src/Feature.Core/Feature.Core.csproj'; Enabled = 'default'; Included = $false },
        @{ ProjectPath = 'src/Feature.Core/Feature.Core.csproj'; Enabled = 'false'; Included = $false },
        @{ ProjectPath = 'src/Feature.Core/Feature.Core.csproj'; Enabled = 'true'; Included = $true },
        @{ ProjectPath = 'src/Feature.TestHarness/Feature.TestHarness.csproj'; Enabled = 'true'; Included = $true },
        @{ ProjectPath = 'src/Sdk.Client/Sdk.Client.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'src/Inlet.Client.Generators/Inlet.Client.Generators.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'src/Inlet.Generators.Core/Inlet.Generators.Core.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'src/Inlet.Generators.Abstractions/Inlet.Generators.Abstractions.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'tests/Feature.Core.L0Tests/Feature.Core.L0Tests.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'samples/Spring/Spring.Domain/Spring.Domain.csproj'; Enabled = 'true'; Included = $false },
        @{ ProjectPath = 'src-mirror/Feature.Core/Feature.Core.csproj'; Enabled = 'true'; Included = $false }
    ) {
        $evaluation = Get-ReferenceTrimmerEvaluation -ProjectPath $ProjectPath -Enabled $Enabled
        $references = @($evaluation.Items.PackageReference | Where-Object Identity -eq 'ReferenceTrimmer')
        $references.Count | Should -Be $(if ($Included) { 1 } else { 0 })
        if ($Included) { $references[0].PrivateAssets | Should -Be 'all' }
        if ($Enabled -eq 'true') {
            $expectedLock = Join-Path (Split-Path -Parent (Join-Path $fixtureRoot $ProjectPath)) 'packages.reference-trimmer.lock.json'
            [System.IO.Path]::GetFullPath($evaluation.Properties.NuGetLockFilePath) | Should -Be ([System.IO.Path]::GetFullPath($expectedLock))
        }
        else {
            $evaluation.Properties.NuGetLockFilePath | Should -Not -Match 'reference-trimmer'
        }
    }

    It 'keeps persisted diagnostic locks out of Razor content with opt-in <Enabled>' -ForEach @(
        @{ Enabled = 'false' },
        @{ Enabled = 'true' }
    ) {
        $projectFile = Join-Path $fixtureRoot 'src/Feature.Client/Feature.Client.csproj'
        $directory = Split-Path -Parent $projectFile
        $null = New-Item -ItemType Directory -Path $directory -Force
        Set-Content -LiteralPath $projectFile -Value '<Project Sdk="Microsoft.NET.Sdk.Razor" />'
        Set-Content -LiteralPath (Join-Path $directory 'packages.reference-trimmer.lock.json') -Value '{}'
        $output = & dotnet msbuild $projectFile -nologo '-getItem:Content,None' "-p:EnableReferenceTrimmer=$Enabled"
        if ($LASTEXITCODE -ne 0) { throw 'Razor content evaluation failed.' }
        $evaluation = ($output -join [Environment]::NewLine) | ConvertFrom-Json
        foreach ($itemType in @('Content', 'None')) {
            @($evaluation.Items.$itemType | Where-Object Identity -eq 'packages.reference-trimmer.lock.json').Count | Should -Be 0
        }
    }
}