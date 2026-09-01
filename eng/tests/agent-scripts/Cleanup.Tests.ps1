#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'

$modulePath = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', 'src', 'agent-scripts', 'RepositoryAutomation.psm1')
$modulePath = [System.IO.Path]::GetFullPath($modulePath)
Import-Module -Name $modulePath -Force
$monthlyModulePath = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', 'src', 'agent-scripts', 'MonthlyCleanupAutomation.psm1')
$monthlyModulePath = [System.IO.Path]::GetFullPath($monthlyModulePath)
Import-Module -Name $monthlyModulePath -Force

BeforeAll {
    $script:invokeGitTestCommand = {
        param(
            [Parameter(Mandatory)][string]$WorkingDirectory,
            [Parameter(Mandatory)][string[]]$Arguments
        )

        $disabledHooksPath = Join-Path $WorkingDirectory '.git/disabled-hooks'
        $output = @(
            git -C $WorkingDirectory `
                -c commit.gpgSign=false `
                -c tag.gpgSign=false `
                -c "core.hooksPath=$disabledHooksPath" `
                @Arguments
        )
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "Git command failed with exit code $($exitCode): git $($Arguments -join ' ')"
        }

        return @($output)
    }
}

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
            'src/Foo/Foo.razor.css',
            'src/Foo/site.js',
            'src/Foo/appsettings.json',
            'src/Foo/packages.lock.json',
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

    It 'keeps project-owned web assets in the targeted cleanup scope' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('src/Foo/Foo.razor.css', 'src/Foo/site.js')

        $plan.Mode | Should -Be 'Targeted'
        @($plan.EligiblePaths) | Should -Be @('src/Foo/Foo.razor.css', 'src/Foo/site.js')
        @($plan.Groups) | Should -HaveCount 1
        @($plan.Groups[0].IncludePaths) | Should -Be @('src/Foo/Foo.razor.css', 'src/Foo/site.js')
    }

    It 'targets project JSON files while ignoring package lock files' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('src/Foo/appsettings.json', 'src/Foo/packages.lock.json')

        $plan.Mode | Should -Be 'Targeted'
        @($plan.EligiblePaths) | Should -Be @('src/Foo/appsettings.json')
        @($plan.IgnoredPaths) | Should -Contain 'src/Foo/packages.lock.json'
        @($plan.Groups[0].IncludePaths) | Should -Be @('src/Foo/appsettings.json')
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
            -Paths @('.gitignore', 'Directory.DotSettings', 'src/Foo/Foo.cs')

        $plan.Mode | Should -Be 'FullFallback'
        $plan.Reason | Should -Match 'settings'
        @($plan.FallbackReasons) | Should -HaveCount 2
        $plan.ValidationScope | Should -Be 'Repository'
    }

    It 'runs publisher-only changes fully but validates only PR-owned drift' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @(
                '.github/workflows/monthly-cleanup.yml',
                'eng/src/agent-scripts/MonthlyCleanupAutomation.psm1')

        $plan.Mode | Should -Be 'FullValidation'
        @($plan.FallbackReasons) | Should -HaveCount 2
        $plan.ValidationScope | Should -Be 'ChangedPaths'
    }

    It 'validates canonical cleanup implementation changes repository-wide' {
        $plan = Get-CleanupPlan `
            -RepoRoot $script:fixtureRoot `
            -Paths @('clean-up.ps1', 'eng/src/agent-scripts/RepositoryAutomation.psm1')

        $plan.Mode | Should -Be 'FullFallback'
        @($plan.FallbackReasons) | Should -HaveCount 2
        $plan.ValidationScope | Should -Be 'Repository'
    }

    It 'rejects a cleanup file outside the canonical solutions' {
        {
            Get-CleanupPlan `
                -RepoRoot $script:fixtureRoot `
                -Paths @('orphan/orphan.cs')
        } | Should -Throw '*must belong to a project*orphan/orphan.cs*'
    }

    It 'rejects an unmapped cleanup file even when a global fallback input also changed' {
        {
            Get-CleanupPlan `
                -RepoRoot $script:fixtureRoot `
                -Paths @('mississippi.slnx', 'orphan/orphan.cs')
        } | Should -Throw '*must belong to a project*orphan/orphan.cs*'
    }

    It 'rejects an absolute path on another drive' -Skip:([System.IO.Path]::DirectorySeparatorChar -ne '\') {
        $repoDrive = [System.IO.Path]::GetPathRoot($script:fixtureRoot).Substring(0, 1).ToUpperInvariant()
        $otherDrive = if ($repoDrive -eq 'Z') { 'Y' } else { 'Z' }

        {
            ConvertTo-CleanupRelativePath `
                -RepoRoot $script:fixtureRoot `
                -Path "$otherDrive`:\outside.cs"
        } | Should -Throw '*outside repository root*'
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

Describe 'Changed cleanup path discovery' {
    It 'combines branch, staged, unstaged, untracked, renamed, deleted, and type-changed paths' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'src') -Force | Out-Null
        $unicodeFileName = 'Caf' + [char]0x00E9 + '.cs'

        @(
            '.editorconfig',
            'src/Deleted.cs',
            'src/Staged.cs',
            'src/TypeChanged.cs',
            'src/Unstaged.cs',
            ('src/' + $unicodeFileName)
        ) | ForEach-Object {
            Set-Content -LiteralPath (Join-Path $fixtureRoot $_) -Value 'class FixtureType { }' -Encoding utf8
        }

        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('init') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('branch', '-M', 'main') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.email', 'cleanup-tests@example.com') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.name', 'Cleanup Tests') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'core.quotePath', 'true') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Create cleanup fixture') | Out-Null
        $remoteRoot = Join-Path $TestDrive "$([Guid]::NewGuid()).git"
        & $script:invokeGitTestCommand -WorkingDirectory $TestDrive -Arguments @('clone', '--bare', $fixtureRoot, $remoteRoot) | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('remote', 'add', 'origin', $remoteRoot) | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('switch', '-c', 'feature') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('branch', '-D', 'main') | Out-Null

        Set-Content -LiteralPath (Join-Path $fixtureRoot 'src/Branch.cs') -Value 'class BranchFixtureType { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', 'src/Branch.cs') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Add branch file') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot ('src/' + $unicodeFileName)) -Value 'class ChangedCafeFixtureType { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Change non-ASCII path') | Out-Null

        Set-Content -LiteralPath (Join-Path $fixtureRoot 'src/Staged.cs') -Value 'class StagedFixtureType { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', 'src/Staged.cs') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'src/Unstaged.cs') -Value 'class UnstagedFixtureType { }' -Encoding utf8
        Remove-Item -LiteralPath (Join-Path $fixtureRoot 'src/Deleted.cs')
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'src/Untracked.cs') -Value 'class UntrackedFixtureType { }' -Encoding utf8
        & $script:invokeGitTestCommand `
            -WorkingDirectory $fixtureRoot `
            -Arguments @('mv', '.editorconfig', '.editorconfig.bak') | Out-Null
        $typeChangedBlob = @(
            & $script:invokeGitTestCommand `
                -WorkingDirectory $fixtureRoot `
                -Arguments @('hash-object', '-w', 'src/TypeChanged.cs')
        )[0]
        & $script:invokeGitTestCommand `
            -WorkingDirectory $fixtureRoot `
            -Arguments @('update-index', '--cacheinfo', "120000,$typeChangedBlob,src/TypeChanged.cs") | Out-Null

        $paths = @(
            Get-CleanupChangedPaths `
                -RepoRoot $fixtureRoot `
                -BaseRef 'main' `
                -HeadRef 'HEAD'
        )

        $paths | Should -HaveCount 9
        $paths | Should -Contain '.editorconfig'
        $paths | Should -Contain '.editorconfig.bak'
        $paths | Should -Contain 'src/Branch.cs'
        $paths | Should -Contain 'src/Staged.cs'
        $paths | Should -Contain 'src/TypeChanged.cs'
        $paths | Should -Contain 'src/Unstaged.cs'
        $paths | Should -Contain ('src/' + $unicodeFileName)
        $paths | Should -Contain 'src/Deleted.cs'
        $paths | Should -Contain 'src/Untracked.cs'
    }

    It 'rejects ref names that can be parsed as Git options' {
        {
            Get-CleanupChangedPaths `
                -RepoRoot $TestDrive `
                -BaseRef '--help' `
                -HeadRef 'HEAD'
        } | Should -Throw "*do not start with '-'*"
    }

    It 'prefers the remote-tracking default branch over a stale local branch or same-named tag' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('init') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('branch', '-M', 'main') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.email', 'cleanup-tests@example.com') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.name', 'Cleanup Tests') | Out-Null

        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Base.cs') -Value 'class Base { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Create base') | Out-Null
        $staleCommit = @(& $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('rev-parse', 'HEAD'))[0]
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Upstream.cs') -Value 'class Upstream { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Advance upstream') | Out-Null
        $remoteRoot = Join-Path $TestDrive "$([Guid]::NewGuid()).git"
        & $script:invokeGitTestCommand -WorkingDirectory $TestDrive -Arguments @('clone', '--bare', $fixtureRoot, $remoteRoot) | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('remote', 'add', 'origin', $remoteRoot) | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('fetch', 'origin') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('switch', '-c', 'feature') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Feature.cs') -Value 'class Feature { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Add feature') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('branch', '-f', 'main', $staleCommit) | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('tag', 'main', $staleCommit) | Out-Null

        $paths = @(Get-CleanupChangedPaths -RepoRoot $fixtureRoot)

        $paths | Should -Be @('Feature.cs')
    }

    It 'fails clearly when the index contains unresolved merge conflicts' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('init') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('branch', '-M', 'main') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.email', 'cleanup-tests@example.com') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('config', 'user.name', 'Cleanup Tests') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Conflict.cs') -Value 'class Base { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Create conflict base') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('switch', '-c', 'other') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Conflict.cs') -Value 'class Other { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Change other') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('switch', 'main') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'Conflict.cs') -Value 'class Main { }' -Encoding utf8
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('add', '--all') | Out-Null
        & $script:invokeGitTestCommand -WorkingDirectory $fixtureRoot -Arguments @('commit', '-m', 'Change main') | Out-Null

        git -C $fixtureRoot -c commit.gpgSign=false merge other 2>$null | Out-Null
        $LASTEXITCODE | Should -Not -Be 0

        {
            Get-CleanupChangedPaths -RepoRoot $fixtureRoot
        } | Should -Throw '*unresolved merge conflicts*Conflict.cs*'
    }
}

Describe 'Git cleanup path handling' {
    It 'round-trips non-ASCII paths through a NUL-delimited Git diff' {
        $fixtureRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'src') -Force | Out-Null
        $invokeGit = {
            param([Parameter(Mandatory)][string[]]$GitArguments)

            $output = @(
                git -C $fixtureRoot `
                    -c commit.gpgSign=false `
                    -c tag.gpgSign=false `
                    -c "core.hooksPath=$(Join-Path $fixtureRoot '.git/disabled-hooks')" `
                    @GitArguments
            )
            if ($LASTEXITCODE -ne 0) {
                throw "Git command failed: git $($GitArguments -join ' ')"
            }

            return @($output)
        }

        & $invokeGit -GitArguments @('init') | Out-Null
        & $invokeGit -GitArguments @('config', 'user.email', 'cleanup-tests@example.com') | Out-Null
        & $invokeGit -GitArguments @('config', 'user.name', 'Cleanup Tests') | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'README.md') -Value 'fixture' -Encoding utf8
        & $invokeGit -GitArguments @('add', '--all') | Out-Null
        & $invokeGit -GitArguments @('commit', '-m', 'Create fixture') | Out-Null
        $baseCommit = @(& $invokeGit -GitArguments @('rev-parse', 'HEAD'))[0]

        $unicodePath = 'src/Café.cs'
        Set-Content -LiteralPath (Join-Path $fixtureRoot $unicodePath) -Value 'class Café { }' -Encoding utf8
        & $invokeGit -GitArguments @('add', '--all') | Out-Null
        & $invokeGit -GitArguments @('commit', '-m', 'Add Unicode path') | Out-Null
        $headCommit = @(& $invokeGit -GitArguments @('rev-parse', 'HEAD'))[0]

        $paths = @(Get-CleanupChangedPaths -RepoRoot $fixtureRoot -BaseRef $baseCommit -HeadRef $headCommit)

        $paths | Should -Be @($unicodePath)
    }

    It 'reads NUL-delimited paths without trimming path characters' {
        $pathList = Join-Path $TestDrive 'cleanup-paths.txt'
        $content = " Café.cs$([char]0)trailing.cs $([char]0)"
        [System.IO.File]::WriteAllText($pathList, $content, [System.Text.UTF8Encoding]::new($false))

        $paths = @(Read-CleanupPathList -Path $pathList)

        $paths | Should -Be @(' Café.cs', 'trailing.cs ')
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

Describe 'Repository cleanup dispatch' {
    BeforeEach {
        $script:dispatcherRoot = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $script:dispatcherRoot -Force | Out-Null
        Set-Content `
            -LiteralPath (Join-Path $script:dispatcherRoot 'Directory.DotSettings') `
            -Value '<ApplicationSettings />' `
            -Encoding utf8

        $script:dispatcherProject = Join-Path $script:dispatcherRoot 'src/Foo/Foo.csproj'
        New-Item -ItemType Directory -Path (Split-Path -Parent $script:dispatcherProject) -Force | Out-Null
        Set-Content -LiteralPath $script:dispatcherProject -Value '<Project />' -Encoding utf8

        Mock -CommandName Invoke-DotnetToolRestore -ModuleName RepositoryAutomation -MockWith { }
        Mock -CommandName Invoke-SolutionRestore -ModuleName RepositoryAutomation -MockWith { }
        Mock -CommandName Invoke-SolutionBuild -ModuleName RepositoryAutomation -MockWith { }
        Mock -CommandName Invoke-TargetedProjectCleanup -ModuleName RepositoryAutomation -MockWith { }
        Mock -CommandName Invoke-MississippiSolutionCleanup -ModuleName RepositoryAutomation -MockWith { }
        Mock -CommandName Invoke-SampleSolutionCleanup -ModuleName RepositoryAutomation -MockWith { }
    }

    It 'returns before tool, restore, build, and cleanup work for a no-op plan' {
        Mock -CommandName Get-CleanupPlan -ModuleName RepositoryAutomation -MockWith {
            [pscustomobject]@{
                Mode             = 'NoOp'
                Reason           = 'Nothing to clean.'
                InputPaths       = @('README.md')
                EligiblePaths    = @()
                IgnoredPaths     = @('README.md')
                Groups           = @()
                AffectedProjects = @()
            }
        }

        $plan = Invoke-RepositoryCleanup `
            -Mode Targeted `
            -RepoRoot $script:dispatcherRoot `
            -Paths @('README.md')

        $plan.Mode | Should -Be 'NoOp'
        Should -Invoke Invoke-DotnetToolRestore -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-SolutionRestore -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-TargetedProjectCleanup -ModuleName RepositoryAutomation -Times 0
    }

    It 'restores, builds, and cleans each targeted project once' {
        Mock -CommandName Get-CleanupPlan -ModuleName RepositoryAutomation -MockWith {
            [pscustomobject]@{
                Mode             = 'Targeted'
                Reason           = 'One project selected.'
                InputPaths       = @('src/Foo/Foo.cs')
                EligiblePaths    = @('src/Foo/Foo.cs')
                IgnoredPaths     = @()
                Groups           = @(
                    [pscustomobject]@{
                        ProjectPath   = $script:dispatcherProject
                        RelativePath  = 'src/Foo/Foo.csproj'
                        SolutionPaths = @('mississippi.slnx')
                        IncludePaths  = @('src/Foo/Foo.cs')
                    }
                )
                AffectedProjects = @('src/Foo/Foo.csproj')
            }
        }

        $plan = Invoke-RepositoryCleanup `
            -Mode Targeted `
            -RepoRoot $script:dispatcherRoot `
            -Paths @('src/Foo/Foo.cs')

        $plan.Mode | Should -Be 'Targeted'
        Should -Invoke Invoke-DotnetToolRestore -ModuleName RepositoryAutomation -Times 1
        Should -Invoke Invoke-SolutionRestore -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $SolutionPath -eq $script:dispatcherProject
        }
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $SolutionPath -eq $script:dispatcherProject -and
            $AdditionalArguments -contains '-p:RunAnalyzers=false'
        }
        Should -Invoke Invoke-TargetedProjectCleanup -ModuleName RepositoryAutomation -Times 1 -ParameterFilter {
            $ProjectGroup.RelativePath -eq 'src/Foo/Foo.csproj'
        }
    }

    It 'routes a full-validation plan through full cleanup' {
        Mock -CommandName Get-CleanupPlan -ModuleName RepositoryAutomation -MockWith {
            [pscustomobject]@{
                Mode             = 'FullValidation'
                Reason           = 'Cleanup automation changed.'
                InputPaths       = @('clean-up.ps1')
                EligiblePaths    = @()
                IgnoredPaths     = @('clean-up.ps1')
                Groups           = @()
                AffectedProjects = @()
            }
        }

        $plan = Invoke-RepositoryCleanup `
            -Mode Targeted `
            -RepoRoot $script:dispatcherRoot `
            -Paths @('clean-up.ps1')

        $plan.Mode | Should -Be 'FullValidation'
        Should -Invoke Invoke-DotnetToolRestore -ModuleName RepositoryAutomation -Times 1
        Should -Invoke Invoke-SolutionRestore -ModuleName RepositoryAutomation -Times 2
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 2
        Should -Invoke Invoke-MississippiSolutionCleanup -ModuleName RepositoryAutomation -Times 1
        Should -Invoke Invoke-SampleSolutionCleanup -ModuleName RepositoryAutomation -Times 1
    }

    It 'honors full-cleanup skip switches without running unnecessary preparation' {
        $plan = Invoke-RepositoryCleanup `
            -Mode Full `
            -RepoRoot $script:dispatcherRoot `
            -SkipSamples `
            -SkipToolRestore `
            -SkipRestore `
            -SkipBuild

        $plan.Mode | Should -Be 'Full'
        Should -Invoke Invoke-DotnetToolRestore -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-SolutionRestore -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 0
        Should -Invoke Invoke-MississippiSolutionCleanup -ModuleName RepositoryAutomation -Times 1
        Should -Invoke Invoke-SampleSolutionCleanup -ModuleName RepositoryAutomation -Times 0
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

Describe 'Monthly cleanup pull request publishing' {
    BeforeEach {
        $script:monthlyPatchPath = Join-Path $TestDrive 'cleanup.patch'
        Set-Content -LiteralPath $script:monthlyPatchPath -Value 'fixture patch' -Encoding utf8
        $script:publisherParameters = @{
            DriftState      = 'true'
            PatchPath       = $script:monthlyPatchPath
            DefaultBranch   = 'main'
            RepositoryOwner = 'Gibbs-Morris'
            PullRequestTitle = 'Monthly cleanup'
            RunId           = '123'
            RunAttempt      = '1'
        }
        $script:monthlyScenario = 'Existing'
        $script:monthlyLeaseFails = $false
        Mock -CommandName Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -MockWith {
            if ($FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'list') {
                $output = if ($script:monthlyScenario -eq 'New') {
                    '[]'
                }
                else {
                    '[{"number":42,"title":"Monthly cleanup","headRefName":"automation/monthly-cleanup-old","url":"https://example.test/42","isCrossRepository":false,"headRepositoryOwner":{"login":"Gibbs-Morris"}}]'
                }
                return [pscustomobject]@{ ExitCode = 0; Output = @($output) }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'diff' -and $Arguments -contains '--quiet') {
                $exitCode = if ($script:monthlyScenario -eq 'NoDrift') { 0 } else { 1 }
                return [pscustomobject]@{ ExitCode = $exitCode; Output = @() }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'diff') {
                return [pscustomobject]@{ ExitCode = 0; Output = @('src/Foo.cs') }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'rev-parse') {
                $sha = if ($Arguments[1] -eq 'HEAD') { 'cleanup-sha' } else { 'remote-sha' }
                return [pscustomobject]@{ ExitCode = 0; Output = @($sha) }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'push' -and $script:monthlyLeaseFails) {
                throw 'force-with-lease rejected'
            }
            return [pscustomobject]@{ ExitCode = 0; Output = @() }
        }
    }

    It 'closes an existing automation pull request when drift has disappeared' {
        $script:monthlyScenario = 'NoDrift'
        $script:publisherParameters.DriftState = 'false'

        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'NoDrift'
        $result.Branch | Should -BeNullOrEmpty
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'close' -and
            $Arguments -contains '42' -and $Arguments -contains '--delete-branch'
        }
    }

    It 'replaces an existing automation branch with a guarded lease' {
        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'Updated'
        $result.Branch | Should -Be 'automation/monthly-cleanup-old'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'push' -and
            $Arguments -contains '--force-with-lease=refs/heads/automation/monthly-cleanup-old:remote-sha'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'edit'
        }
    }

    It 'creates a new automation branch and pull request when none exists' {
        $script:monthlyScenario = 'New'

        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'Created'
        $result.Branch | Should -Be 'automation/monthly-cleanup-123-1'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'switch' -and
            $Arguments -contains 'automation/monthly-cleanup-123-1'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'create'
        }
    }

    It 'stops without editing the pull request when the replacement lease fails' {
        $script:monthlyLeaseFails = $true

        { Publish-MonthlyCleanupPullRequest @script:publisherParameters } |
            Should -Throw '*force-with-lease rejected*'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 0 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'edit'
        }
    }

    It 'reconciles the branch with the latest base before dispatching validation' {
        $result = Invoke-MonthlyCleanupPullRequestValidation `
            -CleanupBranch 'automation/monthly-cleanup-123-1' `
            -DefaultBranch 'main' `
            -Workflows @('cleanup.yml', 'full-build.yml')

        $result.WorkflowCount | Should -Be 2
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'merge' -and
            $Arguments -contains 'refs/remotes/origin/main'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 2 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'run' -and
            $Arguments -contains '--ref' -and $Arguments -contains 'automation/monthly-cleanup-123-1'
        }
    }

    It 'dispatches mutation testing as part of the default validation set' {
        $result = Invoke-MonthlyCleanupPullRequestValidation `
            -CleanupBranch 'automation/monthly-cleanup-123-1' `
            -DefaultBranch 'main'

        $result.WorkflowCount | Should -BeGreaterThan 2
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'run' -and
            $Arguments -contains 'stryker.yml'
        }
    }
}

Describe 'Cleanup module exports' {
    It 'exports the standalone solution cleanup functions used by wrapper scripts' {
        Get-Command -Name Invoke-MississippiSolutionCleanup -Module RepositoryAutomation | Should -Not -BeNullOrEmpty
        Get-Command -Name Invoke-SampleSolutionCleanup -Module RepositoryAutomation | Should -Not -BeNullOrEmpty
    }
}
