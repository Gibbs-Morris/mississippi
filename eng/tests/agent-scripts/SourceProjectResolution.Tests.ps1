#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $scriptPath = Join-Path $PSScriptRoot '../../src/agent-scripts/test-project-quality.ps1'
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw 'Quality script parsing failed' }
    $definition = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Resolve-SourceProjectPathFromTest' }, $false)
    if ($null -eq $definition) { throw 'Source resolver function missing' }
    . ([scriptblock]::Create($definition.Extent.Text))
}

Describe 'Quality script source project resolution' {
    BeforeEach {
        $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        foreach ($directory in @('samples/Client', 'samples/Client.L0Tests', 'src/TestHarness')) {
            New-Item -ItemType Directory -Path (Join-Path $repo $directory) -Force | Out-Null
        }
        $client = Join-Path $repo 'samples/Client/Client.csproj'
        $harness = Join-Path $repo 'src/TestHarness/TestHarness.csproj'
        $tests = Join-Path $repo 'samples/Client.L0Tests/Client.L0Tests.csproj'
        Set-Content $client '<Project />'
        Set-Content $harness '<Project />'
    }

    It 'honors an explicit sample target over a src test-support reference' {
        Set-Content $tests '<Project><PropertyGroup><MutationSourceProject>../Client/Client.csproj</MutationSourceProject></PropertyGroup><ItemGroup><ProjectReference Include="../Client/Client.csproj"/><ProjectReference Include="../../src/TestHarness/TestHarness.csproj"/></ItemGroup></Project>'
        Resolve-SourceProjectPathFromTest -TestProjectPath $tests | Should -Be (Resolve-Path $client).Path
    }

    It 'fails instead of falling back when the explicit target is missing' {
        Set-Content $tests '<Project><PropertyGroup><MutationSourceProject>../Missing/Missing.csproj</MutationSourceProject></PropertyGroup><ItemGroup><ProjectReference Include="../../src/TestHarness/TestHarness.csproj"/></ItemGroup></Project>'
        { Resolve-SourceProjectPathFromTest -TestProjectPath $tests } | Should -Throw
    }

    It 'preserves source-directory inference without explicit metadata' {
        Set-Content $tests '<Project><ItemGroup><ProjectReference Include="../Client/Client.csproj"/><ProjectReference Include="../../src/TestHarness/TestHarness.csproj"/></ItemGroup></Project>'
        Resolve-SourceProjectPathFromTest -TestProjectPath $tests | Should -Be (Resolve-Path $harness).Path
    }

    It 'resolves the real Spring client test project to its primary source' {
        $actualTests = Join-Path $PSScriptRoot '../../../samples/Spring/Spring.Client.L0Tests/Spring.Client.L0Tests.csproj'
        $actualSource = Join-Path $PSScriptRoot '../../../samples/Spring/Spring.Client/Spring.Client.csproj'
        Resolve-SourceProjectPathFromTest -TestProjectPath $actualTests | Should -Be (Resolve-Path $actualSource).Path
    }
}
