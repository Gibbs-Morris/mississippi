#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'ReferenceTrimmer persisted restore inputs' {
    BeforeAll {
        $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $fixture = Join-Path $TestDrive 'source'
        $clean = Join-Path $TestDrive 'clean'
        $feed = Join-Path $TestDrive 'feed'
        $null = New-Item -ItemType Directory -Path $fixture, $clean, $feed
        foreach ($name in @('Directory.Build.props', 'Directory.Packages.props', '.gitignore')) {
            Copy-Item -LiteralPath (Join-Path $repositoryRoot $name) -Destination $fixture
        }
        $projectPaths = @('src/App/App.csproj', 'src/Dependency/Dependency.csproj')
        foreach ($path in $projectPaths) {
            $project = Join-Path $fixture $path
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $project) -Force
            # The fixture tests restore metadata offline, without downloading analyzers.
            Set-Content -LiteralPath $project -Value '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Remove="@(PackageReference)" /></ItemGroup></Project>'
        }
        Set-Content -LiteralPath (Join-Path $fixture 'fixture.slnx') -Value '<Solution><Project Path="src/App/App.csproj" /><Project Path="src/Dependency/Dependency.csproj" /></Solution>'

        function Invoke-FixtureRestore {
            param([string]$Root, [switch]$Diagnostic)
            $arguments = @('restore', (Join-Path $Root 'fixture.slnx'), '--locked-mode', '--source', $feed, '--verbosity', 'minimal')
            if ($Diagnostic) { $arguments += '-p:EnableReferenceTrimmer=true' }
            $output = & dotnet @arguments 2>&1
            return @{ ExitCode = $LASTEXITCODE; Output = ($output | Out-String) }
        }

        $ordinary = Invoke-FixtureRestore -Root $fixture
        if ($ordinary.ExitCode -ne 0) { throw $ordinary.Output }
        $diagnostic = Invoke-FixtureRestore -Root $fixture -Diagnostic
        if ($diagnostic.ExitCode -ne 0) { throw $diagnostic.Output }
        foreach ($file in (Get-ChildItem -LiteralPath $fixture -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](?:obj|bin)[\\/]' })) {
            $path = [IO.Path]::GetRelativePath($fixture, $file.FullName)
            $destination = Join-Path $clean $path
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
            Copy-Item -LiteralPath $file.FullName -Destination $destination
        }
    }

    It 'preserves diagnostic graphs on a clean checkout and rejects drift without changing ordinary locks' {
        $before = @{}
        foreach ($path in $projectPaths) {
            $directory = Split-Path -Parent (Join-Path $clean $path)
            Test-Path -LiteralPath (Join-Path $directory 'obj') | Should -BeFalse
            $lock = Join-Path $directory 'packages.reference-trimmer.lock.json'
            Test-Path -LiteralPath $lock -PathType Leaf | Should -BeTrue
            $before[$lock] = (Get-FileHash -LiteralPath $lock).Hash
            $ordinaryLock = Join-Path $directory 'packages.lock.json'
            $before[$ordinaryLock] = (Get-FileHash -LiteralPath $ordinaryLock).Hash
        }

        $restore = Invoke-FixtureRestore -Root $clean -Diagnostic
        $restore.ExitCode | Should -Be 0 -Because $restore.Output
        foreach ($path in $before.Keys) {
            (Get-FileHash -LiteralPath $path).Hash | Should -Be $before[$path]
        }

        $app = Join-Path $clean 'src/App/App.csproj'
        [xml]$project = Get-Content -LiteralPath $app -Raw
        $reference = $project.CreateElement('ProjectReference')
        $reference.SetAttribute('Include', '../Dependency/Dependency.csproj')
        $null = $project.Project.ItemGroup.AppendChild($reference)
        $project.Save($app)
        $drift = Invoke-FixtureRestore -Root $clean -Diagnostic
        $drift.ExitCode | Should -Not -Be 0
        $drift.Output | Should -Match 'NU1004'
        foreach ($path in $before.Keys) {
            (Get-FileHash -LiteralPath $path).Hash | Should -Be $before[$path]
        }
    }

    It 'requires a tracked diagnostic lock before the workflow restores (<State>)' -ForEach @(
        @{ State = 'tracked'; ExpectedExit = 0 },
        @{ State = 'untracked'; ExpectedExit = 1 },
        @{ State = 'deleted'; ExpectedExit = 1 }
    ) {
        $root = Join-Path $TestDrive "guard-$State"
        $directory = Join-Path $root 'src/App'
        $null = New-Item -ItemType Directory -Path $directory -Force
        $project = Join-Path $directory 'App.csproj'
        $lock = Join-Path $directory 'packages.reference-trimmer.lock.json'
        Set-Content -LiteralPath $project -Value '<Project Sdk="Microsoft.NET.Sdk" />'
        Set-Content -LiteralPath $lock -Value '{}'
        & git init --quiet $root
        if ($LASTEXITCODE -ne 0) { throw 'Fixture Git initialization failed.' }
        & git -C $root add -- 'src/App/App.csproj'
        if ($LASTEXITCODE -ne 0) { throw 'Fixture project staging failed.' }
        if ($State -ne 'untracked') {
            & git -C $root add -- 'src/App/packages.reference-trimmer.lock.json'
            if ($LASTEXITCODE -ne 0) { throw 'Fixture lock staging failed.' }
        }
        if ($State -eq 'deleted') { Remove-Item -LiteralPath $lock }
        $output = & pwsh -NoProfile -File (Join-Path $repositoryRoot 'eng/src/agent-scripts/assert-reference-trimmer-locks.ps1') -RepositoryRoot $root 2>&1
        $LASTEXITCODE | Should -Be $ExpectedExit -Because ($output | Out-String)
        if ($ExpectedExit -ne 0) {
            ($output | Out-String) | Should -Match 'Missing version-controlled diagnostic lock input'
        }
    }
}
