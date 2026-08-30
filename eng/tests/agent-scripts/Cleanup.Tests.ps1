#requires -Module Pester

$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$modulePath = [System.IO.Path]::GetFullPath($modulePath)
Import-Module -Name $modulePath -Force

Describe 'Cleanup planning' {
    BeforeEach {
        $script:fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $script:fixtureRoot -Force | Out-Null
        $directories = @(
            'src/Foo',
            'src/Bar',
            'samples/App',
            'orphan'
        )
        foreach ($directory in $directories) {
            New-Item -ItemType Directory -Path (Join-Path $script:fixtureRoot $directory) -Force | Out-Null
        }

        @(
            'src/Foo/Foo.csproj',
            'src/Bar/Bar.csproj',
            'samples/App/App.csproj',
            'orphan/Orphan.csproj'
        ) | ForEach-Object {
            Set-Content -LiteralPath (Join-Path $script:fixtureRoot $_) -Value '<Project />' -Encoding utf8
        }

        @(
            'src/Foo/Foo.cs',
            'src/Bar/Bar.cs',
            'samples/App/App.cs',
            'orphan/orphan.cs'
        ) | ForEach-Object {
            Set-Content -LiteralPath (Join-Path $script:fixtureRoot $_) -Value 'class FixtureType { }' -Encoding utf8
        }

        Set-Content -LiteralPath (Join-Path $script:fixtureRoot 'mississippi.slnx') -Value @'
<Solution>
  <Project Path="src\Foo\Foo.csproj" />
  <Project Path="src\Bar\Bar.csproj" />
</Solution>
'@ -Encoding utf8
        Set-Content -LiteralPath (Join-Path $script:fixtureRoot 'samples.slnx') -Value @'
<Solution>
  <Project Path="samples\App\App.csproj" />
</Solution>
'@ -Encoding utf8
    }

    It 'normalizes and de-duplicates explicit paths before grouping them' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('src\Foo\Foo.cs', './src/Foo/Foo.cs', 'README.md')

        $plan.Mode | Should -Be 'Targeted'
        @($plan.InputPaths) | Should -HaveCount 2
        @($plan.EligiblePaths) | Should -HaveCount 1
        @($plan.Groups) | Should -HaveCount 1
        @($plan.Groups[0].IncludePaths) | Should -Be @('src/Foo/Foo.cs')
    }

    It 'groups changed files by owning project across both solutions' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('src/Foo/Foo.cs', 'src/Bar/Bar.cs', 'samples/App/App.cs')

        $plan.Mode | Should -Be 'Targeted'
        @($plan.Groups) | Should -HaveCount 3
        @($plan.AffectedProjects) | Should -Contain 'src/Foo/Foo.csproj'
        @($plan.AffectedProjects) | Should -Contain 'src/Bar/Bar.csproj'
        @($plan.AffectedProjects) | Should -Contain 'samples/App/App.csproj'
        @($plan.Groups | Where-Object { $_.RelativePath -eq 'samples/App/App.csproj' }).SolutionPaths | Should -Contain 'samples.slnx'
    }

    It 'builds the project catalog from solution membership without scanning the repository' {
        Mock -CommandName Get-ChildItem -ModuleName RepositoryAutomation -MockWith {
            throw 'The project catalog should not recursively scan the repository.'
        }

        $catalog = @(Get-CleanupProjectCatalog -RepoRoot $script:fixtureRoot)

        @($catalog) | Should -HaveCount 3
        @($catalog.RelativePath) | Should -Not -Contain 'orphan/Orphan.csproj'
        @($catalog | Where-Object { $_.RelativePath -eq 'samples/App/App.csproj' }).SolutionPaths | Should -Contain 'samples.slnx'
        Should -Invoke Get-ChildItem -ModuleName RepositoryAutomation -Times 0
    }

    It 'ignores documentation and deleted paths without scheduling cleanup' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('README.md', 'src/Foo/Deleted.cs')

        $plan.Mode | Should -Be 'NoOp'
        @($plan.EligiblePaths) | Should -HaveCount 0
        @($plan.IgnoredPaths) | Should -Contain 'README.md'
        @($plan.IgnoredPaths) | Should -Contain 'src/Foo/Deleted.cs'
    }

    It 'includes the renamed destination and ignores the missing source path' {
        $renamedPath = Join-Path $script:fixtureRoot 'src/Foo/Renamed.cs'
        Set-Content -LiteralPath $renamedPath -Value 'class RenamedFixtureType { }' -Encoding utf8

        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('src/Foo/Old.cs', 'src\Foo\Renamed.cs')

        $plan.Mode | Should -Be 'Targeted'
        @($plan.EligiblePaths) | Should -Be @('src/Foo/Renamed.cs')
        @($plan.IgnoredPaths) | Should -Be @('src/Foo/Old.cs')
    }

    It 'falls back to full cleanup for global cleanup inputs' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('Directory.DotSettings', 'src/Foo/Foo.cs')

        $plan.Mode | Should -Be 'FullFallback'
        $plan.Reason | Should -Match 'settings'
        @($plan.FallbackReasons) | Should -HaveCount 1
    }

    It 'falls back to full cleanup when a file cannot be mapped to a solution project' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('orphan/orphan.cs')

        $plan.Mode | Should -Be 'FullFallback'
        $plan.Reason | Should -Match 'could not be mapped'
        $plan.Reason | Should -Match 'orphan/orphan.cs'
    }

    It 'honors a solution skip switch without falling back to another solution' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('samples/App/App.cs') `
            -SkipSamples

        $plan.Mode | Should -Be 'NoOp'
        @($plan.IgnoredPaths) | Should -Contain 'samples/App/App.cs'
    }
}

Describe 'CleanupCode invocation' {
    It 'preserves settings, include paths, cache, and no-update options' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        $solutionPath = Join-Path $fixtureRoot 'fixture.csproj'
        $settingsPath = Join-Path $fixtureRoot 'Directory.DotSettings'
        Set-Content -LiteralPath $solutionPath -Value '<Project />' -Encoding utf8
        Set-Content -LiteralPath $settingsPath -Value '<ApplicationSettings />' -Encoding utf8

        Mock -CommandName Invoke-RepositoryProcess -ModuleName RepositoryAutomation -MockWith { }

        Invoke-ReSharperCleanup `
            -SolutionPath $solutionPath `
            -SettingsPath $settingsPath `
            -IncludePaths @('src/Foo/Foo.cs') `
            -CachesHome (Join-Path $fixtureRoot 'cache') `
            -NoUpdates

        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $Arguments -contains '--no-updates' -and
            $Arguments -contains '--include=src/Foo/Foo.cs' -and
            @($Arguments | Where-Object { $_ -like '--caches-home=*' }).Count -eq 1
        }
    }

    It 'creates a temporary one-project solution and uses project-relative include paths' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        $projectDirectory = Join-Path $fixtureRoot 'src/Foo'
        New-Item -ItemType Directory -Path (Join-Path $projectDirectory 'Sub') -Force | Out-Null
        $projectPath = Join-Path $projectDirectory 'Foo.csproj'
        Set-Content -LiteralPath $projectPath -Value '<Project />' -Encoding utf8
        Set-Content -LiteralPath (Join-Path $projectDirectory 'Foo.cs') -Value 'class Foo { }' -Encoding utf8
        Set-Content -LiteralPath (Join-Path $projectDirectory 'Sub/Other.cs') -Value 'class Other { }' -Encoding utf8
        $settingsPath = Join-Path $fixtureRoot 'Directory.DotSettings'
        Set-Content -LiteralPath $settingsPath -Value '<ApplicationSettings />' -Encoding utf8

        Mock -CommandName Invoke-SlnGeneration -ModuleName RepositoryAutomation -MockWith {
            param(
                [string]$SolutionPath,
                [string]$OutputPath
            )

            Set-Content -LiteralPath $OutputPath -Value 'temporary solution' -Encoding utf8
            return $OutputPath
        }
        Mock -CommandName Invoke-ReSharperCleanup -ModuleName RepositoryAutomation -MockWith { }

        Invoke-TargetedProjectCleanup `
            -ProjectGroup ([pscustomobject]@{
                ProjectPath  = $projectPath
                IncludePaths = @('src/Foo/Foo.cs', 'src/Foo/Sub/Other.cs')
            }) `
            -RepoRoot $fixtureRoot `
            -SettingsPath $settingsPath

        $temporarySolutionXPattern = Join-Path $projectDirectory '.cleanup-targeted-*.slnx'
        $temporarySolutionPattern = Join-Path $projectDirectory '.cleanup-targeted-*.sln'
        Should -Invoke Invoke-SlnGeneration -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $SolutionPath -like $temporarySolutionXPattern -and
            $OutputPath -like $temporarySolutionPattern
        }
        Should -Invoke Invoke-ReSharperCleanup -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $SolutionPath -like $temporarySolutionPattern -and
            ($IncludePaths -join ';') -eq 'Foo.cs;Sub/Other.cs'
        }
        @(Get-ChildItem -LiteralPath $projectDirectory -Filter '.cleanup-targeted-*' -Force) | Should -HaveCount 0
    }
}

Describe 'Canonical cleanup entry point' {
    It 'supports file-list preflight without invoking cleanup tooling' {
        $repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
        $fileListPath = Join-Path $TestDrive 'cleanup-files.txt'
        Set-Content -LiteralPath $fileListPath -Value 'README.md' -Encoding utf8
        $scriptPath = Join-Path $repoRoot 'clean-up.ps1'

        $json = @(& pwsh -NoProfile -File $scriptPath -FileListPath $fileListPath -PlanOnly)

        $LASTEXITCODE | Should -Be 0
        $plan = ($json -join "`n") | ConvertFrom-Json
        $plan.Mode | Should -Be 'NoOp'
    }
}

Describe 'Cleanup module exports' {
    It 'exports the standalone solution cleanup functions used by wrapper scripts' {
        Get-Command -Name Invoke-MississippiSolutionCleanup -Module RepositoryAutomation | Should -Not -BeNullOrEmpty
        Get-Command -Name Invoke-SampleSolutionCleanup -Module RepositoryAutomation | Should -Not -BeNullOrEmpty
    }
}
