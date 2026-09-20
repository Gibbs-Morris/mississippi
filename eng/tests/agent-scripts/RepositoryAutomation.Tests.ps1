#requires -Module Pester

$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$modulePath = [System.IO.Path]::GetFullPath($modulePath)
Import-Module -Name $modulePath -Force

Describe 'RepositoryAutomation helpers' {
    AfterEach {
        if (-not $IsWindows) {
            & chmod -R u+rwX -- $TestDrive 2>$null | Out-Null
        }
    }

    It 'resolves repository root from test path' {
        $root = Get-RepositoryRoot -StartPath $PSScriptRoot
        Test-Path -LiteralPath (Join-Path $root '.git') | Should -Be $true
    }

    It 'creates automation run directories' {
        $testRoot = Join-Path $TestDrive 'runs'
        $runDirectory = New-AutomationRunDirectory -Root $testRoot -Prefix 'test'
        Test-Path -LiteralPath $runDirectory | Should -Be $true
        ($runDirectory -like (Join-Path $testRoot 'test-*')) | Should -Be $true
        (New-AutomationRunDirectory -Root $testRoot -Prefix 'test') | Should -Not -Be $runDirectory
    }

    It 'leases one worktree exclusively and permits reentrant reuse' {
        $leaseRoot = Join-Path $TestDrive 'lease-repository'
        $coordinationRoot = Join-Path $TestDrive 'lease-coordination'
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'owner-one' -LeaseDirectory $coordinationRoot
        try {
            { Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'owner-two' -LeaseDirectory $coordinationRoot } | Should -Throw '*execution lease*'
            $nestedLease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -ExistingLease $lease -LeaseDirectory $coordinationRoot
            try { $nestedLease.OperationId | Should -Be 'owner-one' } finally { Exit-RepositoryExecutionLease -Lease $nestedLease }
            { Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'owner-four' -LeaseDirectory $coordinationRoot } | Should -Throw '*execution lease*'
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $lease
        }

        $released = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'owner-three' -LeaseDirectory $coordinationRoot
        try { $released.OperationId | Should -Be 'owner-three' } finally { Exit-RepositoryExecutionLease -Lease $released }
    }

    It 'makes shared lease files writable by participating accounts on Unix' {
        if ($IsWindows) {
            Set-ItResult -Skipped -Because 'Unix lease permissions are not available on Windows.'
            return
        }

        $previousSharedWorktree = $env:MISSISSIPPI_SHARED_WORKTREE
        $leaseRoot = Join-Path $TestDrive 'shared-lease-repository'
        $coordinationRoot = Join-Path $TestDrive 'shared-lease-coordination'
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        try {
            Remove-Item Env:MISSISSIPPI_SHARED_WORKTREE -ErrorAction SilentlyContinue
            $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'shared-owner' -LeaseDirectory $coordinationRoot
            try {
                $directoryModeText = (& stat -c '%a' -- $coordinationRoot 2>$null | Out-String).Trim()
                if ($LASTEXITCODE -ne 0) { $directoryModeText = (& stat -f '%Lp' -- $coordinationRoot 2>$null | Out-String).Trim() }
                $fileModeText = (& stat -c '%a' -- $lease.Path 2>$null | Out-String).Trim()
                if ($LASTEXITCODE -ne 0) { $fileModeText = (& stat -f '%Lp' -- $lease.Path 2>$null | Out-String).Trim() }
                ([Convert]::ToInt32($directoryModeText, 8) -band 2) | Should -Be 0
                ([Convert]::ToInt32($fileModeText, 8) -band 2) | Should -Not -Be 0
            }
            finally {
                Exit-RepositoryExecutionLease -Lease $lease
            }
        }
        finally {
            if ($null -eq $previousSharedWorktree) { Remove-Item Env:MISSISSIPPI_SHARED_WORKTREE -ErrorAction SilentlyContinue }
            else { $env:MISSISSIPPI_SHARED_WORKTREE = $previousSharedWorktree }
        }
    }

    It 'acquires a shared lease on Windows without Unix file modes' {
        if (-not $IsWindows) {
            Set-ItResult -Skipped -Because 'Windows shared lease behavior is not available on Unix.'
            return
        }

        $previousSharedWorktree = $env:MISSISSIPPI_SHARED_WORKTREE
        $leaseRoot = Join-Path $TestDrive 'windows-shared-lease-repository'
        $coordinationRoot = Join-Path $TestDrive 'windows-shared-lease-coordination'
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        try {
            Remove-Item Env:MISSISSIPPI_SHARED_WORKTREE -ErrorAction SilentlyContinue
            $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'windows-shared-owner' -LeaseDirectory $coordinationRoot
            try { $lease.OperationId | Should -Be 'windows-shared-owner' }
            finally { Exit-RepositoryExecutionLease -Lease $lease }
        }
        finally {
            if ($null -eq $previousSharedWorktree) { Remove-Item Env:MISSISSIPPI_SHARED_WORKTREE -ErrorAction SilentlyContinue }
            else { $env:MISSISSIPPI_SHARED_WORKTREE = $previousSharedWorktree }
        }
    }

    It 'uses one lease identity for a worktree alias' {
        $realRoot = Join-Path $TestDrive 'lease-real'
        $aliasRoot = Join-Path $TestDrive 'lease-alias'
        New-Item -ItemType Directory -Path $realRoot -Force | Out-Null
        $aliasCreated = $false
        try {
            $aliasType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
            New-Item -ItemType $aliasType -Path $aliasRoot -Target $realRoot -ErrorAction Stop | Out-Null
            $aliasCreated = $true
            $coordinationRoot = Join-Path $TestDrive 'alias-coordination'
            $lease = Enter-RepositoryExecutionLease -RepoRoot $realRoot -OperationId 'physical-owner' -LeaseDirectory $coordinationRoot
            try {
                { Enter-RepositoryExecutionLease -RepoRoot $aliasRoot -OperationId 'alias-owner' -LeaseDirectory $coordinationRoot } | Should -Throw '*execution lease*'
            }
            finally {
                Exit-RepositoryExecutionLease -Lease $lease
            }
        }
        catch {
            if (-not $aliasCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create directory junctions.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $aliasRoot -Force -ErrorAction SilentlyContinue
        }
    }

    It 'rejects a reentrant lease from a different worktree' {
        $firstRoot = Join-Path $TestDrive 'lease-first'
        $secondRoot = Join-Path $TestDrive 'lease-second'
        $coordinationRoot = Join-Path $TestDrive 'different-worktree-coordination'
        New-Item -ItemType Directory -Path $firstRoot, $secondRoot -Force | Out-Null
        $lease = Enter-RepositoryExecutionLease -RepoRoot $firstRoot -OperationId 'first' -LeaseDirectory $coordinationRoot
        try {
            { Enter-RepositoryExecutionLease -RepoRoot $secondRoot -ExistingLease $lease -LeaseDirectory $coordinationRoot } | Should -Throw '*belongs to*'
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $lease
        }
    }

    It 'rejects a reentrant lease from a different coordination directory' {
        $leaseRoot = Join-Path $TestDrive 'coordination-lease-repository'
        $firstCoordinationRoot = Join-Path $TestDrive 'coordination-one'
        $secondCoordinationRoot = Join-Path $TestDrive 'coordination-two'
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'coordination-one' -LeaseDirectory $firstCoordinationRoot
        try {
            { Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -ExistingLease $lease -LeaseDirectory $secondCoordinationRoot } |
                Should -Throw '*coordination path*'
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $lease
        }
    }

    It 'leases multiple worktrees through one shared coordination directory' {
        $firstRoot = Join-Path $TestDrive 'shared-worktree-one'
        $secondRoot = Join-Path $TestDrive 'shared-worktree-two'
        $coordinationRoot = Join-Path $TestDrive 'shared-multi-coordination'
        New-Item -ItemType Directory -Path $firstRoot, $secondRoot -Force | Out-Null
        $firstLease = Enter-RepositoryExecutionLease -RepoRoot $firstRoot -OperationId 'shared-one' -LeaseDirectory $coordinationRoot
        try {
            { Enter-RepositoryExecutionLease -RepoRoot $secondRoot -OperationId 'shared-two' -LeaseDirectory $coordinationRoot } |
                Should -Throw '*coordination file is already held*'
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $firstLease
        }

        $secondLease = Enter-RepositoryExecutionLease -RepoRoot $secondRoot -OperationId 'shared-two' -LeaseDirectory $coordinationRoot
        try { $secondLease.OperationId | Should -Be 'shared-two' }
        finally { Exit-RepositoryExecutionLease -Lease $secondLease }
    }

    It 'rejects a lease contender in another PowerShell process and permits it after release' {
        $leaseRoot = Join-Path $TestDrive 'cross-process-lease-repository'
        $coordinationRoot = Join-Path $TestDrive 'cross-process-coordination'
        $scriptPath = Join-Path $TestDrive 'cross-process-contender.ps1'
        $modulePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        @'
param([string]$ModulePath, [string]$RepoRoot, [string]$LeaseDirectory)
$ErrorActionPreference = 'Stop'
Import-Module $ModulePath -Force
try {
    $lease = Enter-RepositoryExecutionLease -RepoRoot $RepoRoot -LeaseDirectory $LeaseDirectory
    Exit-RepositoryExecutionLease -Lease $lease
    exit 0
}
catch {
    if ($_.Exception.Message -like '*execution lease*') { exit 42 }
    throw
}
'@ | Set-Content -LiteralPath $scriptPath
        $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'parent-owner' -LeaseDirectory $coordinationRoot
        try {
            & $powerShellPath -NoProfile -File $scriptPath -ModulePath $modulePath -RepoRoot $leaseRoot -LeaseDirectory $coordinationRoot | Out-Null
            $LASTEXITCODE | Should -Be 42
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $lease
        }
        & $powerShellPath -NoProfile -File $scriptPath -ModulePath $modulePath -RepoRoot $leaseRoot -LeaseDirectory $coordinationRoot | Out-Null
        $LASTEXITCODE | Should -Be 0
    }

    It 'preserves lease state across a forced module import' {
        $leaseRoot = Join-Path $TestDrive 'reload-lease-repository'
        $coordinationRoot = Join-Path $TestDrive 'reload-coordination'
        New-Item -ItemType Directory -Path $leaseRoot -Force | Out-Null
        $lease = Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'reload-owner' -LeaseDirectory $coordinationRoot
        try {
            Import-Module -Name ([System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'))) -Force
            { Enter-RepositoryExecutionLease -RepoRoot $leaseRoot -OperationId 'reload-contender' -LeaseDirectory $coordinationRoot } |
                Should -Throw '*execution lease*'
        }
        finally {
            Exit-RepositoryExecutionLease -Lease $lease
        }
    }

    It 'resolves relative lease directories from the physical repository root' {
        $repoRoot = Join-Path $TestDrive 'relative-lease-repository'
        New-Item -ItemType Directory -Path $repoRoot -Force | Out-Null
        $originalLocation = Get-Location
        try {
            Push-Location $TestDrive
            $leasePath = Get-RepositoryExecutionLeasePath -RepoRoot $repoRoot -LeaseDirectory 'relative-leases'
            $leasePath | Should -Be (Join-Path $repoRoot 'relative-leases' ([System.IO.Path]::GetFileName($leasePath)))
        }
        finally {
            Set-Location $originalLocation
        }
    }

    It 'resolves relative and chained symlink targets before deriving lease identity' {
        $chainRoot = Join-Path $TestDrive 'lease-chain'
        $realRoot = Join-Path $chainRoot 'real'
        $linkTwo = Join-Path $chainRoot 'link-two'
        $linkOne = Join-Path $chainRoot 'link-one'
        New-Item -ItemType Directory -Path $realRoot -Force | Out-Null
        $linksCreated = $false
        try {
            New-Item -ItemType SymbolicLink -Path $linkTwo -Target $realRoot -ErrorAction Stop | Out-Null
            New-Item -ItemType SymbolicLink -Path $linkOne -Target 'link-two' -ErrorAction Stop | Out-Null
            $linksCreated = $true

            $coordinationRoot = Join-Path $TestDrive 'chain-coordination'
            $realLeasePath = Get-RepositoryExecutionLeasePath -RepoRoot $realRoot -LeaseDirectory $coordinationRoot
            $aliasLeasePath = Get-RepositoryExecutionLeasePath -RepoRoot $linkOne -LeaseDirectory $coordinationRoot

            $aliasLeasePath | Should -Be $realLeasePath
        }
        catch {
            if (-not $linksCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create chained symbolic links.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $linkOne -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $linkTwo -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $chainRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'resolves symlinked ancestors inside multi-component targets' {
        $root = Join-Path $TestDrive 'multi-component-chain'
        $realRoot = Join-Path $root 'real'
        $realSubdirectory = Join-Path $realRoot 'subdir'
        $middle = Join-Path $root 'middle'
        $outer = Join-Path $root 'outer'
        New-Item -ItemType Directory -Path $realSubdirectory -Force | Out-Null
        $linksCreated = $false
        try {
            New-Item -ItemType SymbolicLink -Path $middle -Target $realRoot -ErrorAction Stop | Out-Null
            New-Item -ItemType SymbolicLink -Path $outer -Target (Join-Path $middle 'subdir') -ErrorAction Stop | Out-Null
            $linksCreated = $true

            $coordinationRoot = Join-Path $TestDrive 'multi-component-coordination'
            $realLeasePath = Get-RepositoryExecutionLeasePath -RepoRoot $realSubdirectory -LeaseDirectory $coordinationRoot
            $aliasLeasePath = Get-RepositoryExecutionLeasePath -RepoRoot $outer -LeaseDirectory $coordinationRoot

            $aliasLeasePath | Should -Be $realLeasePath
        }
        catch {
            if (-not $linksCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create multi-component symbolic links.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $outer -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $middle -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'invokes automation steps and returns the result' {
        $result = Invoke-AutomationStep -Name 'Sample' -SilentSuccess -Action { 1 + 1 }
        $result | Should -Be 2
    }
}

Describe 'Repository automation quality gates' {
    AfterEach {
        if (-not $IsWindows) {
            & chmod -R u+rwX -- $TestDrive 2>$null | Out-Null
        }
    }

    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../../src/agent-scripts/RepositoryAutomation.psm1') -Force
    }

    It 'writes the requested banner' {
        Mock Write-Host {} -ModuleName RepositoryAutomation
        Write-AutomationBanner -Message 'Build gate' -ForegroundColor Green
        Should -Invoke Write-Host -ModuleName RepositoryAutomation -Times 1 -Exactly -ParameterFilter {
            $Object -eq 'Build gate' -and $ForegroundColor -eq [ConsoleColor]::Green
        }
    }

    It 'rejects warnings in both normal solution builds' {
        Mock Invoke-DotnetToolRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionBuild {} -ModuleName RepositoryAutomation
        Invoke-MississippiSolutionBuild -RepoRoot $TestDrive
        Invoke-SampleSolutionBuild -RepoRoot $TestDrive
        Should -Invoke Invoke-SolutionBuild -ModuleName RepositoryAutomation -Times 2 -Exactly -ParameterFilter { $WarnAsError }
    }

    It 'isolates MTP reports and excludes unselected test levels' {
        $solution = Join-Path $TestDrive 'test.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('First.L0Tests.csproj', 'Second.L0Tests.csproj', 'Other.L2Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
            $name = $Arguments[[array]::IndexOf($Arguments, '--report-xunit-trx-filename') + 1]
            Set-Content (Join-Path $directory $name) '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
            'Test details'
        } -ModuleName RepositoryAutomation
        Mock Out-Host {} -ModuleName RepositoryAutomation
        $result = Invoke-SolutionTests -SolutionPath $solution -TestLevels L0Tests -ResultsRoot (Join-Path $TestDrive 'isolated')
        $result.SolutionPath | Should -Be $solution
        @(Get-ChildItem $result.ResultsDirectory -Recurse -Filter '*.trx').Count | Should -Be 2
        Should -Invoke Out-Host -ModuleName RepositoryAutomation -Times 2 -Exactly
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 2 -Exactly -ParameterFilter {
            $Arguments -contains '--report-xunit-trx' -and $Arguments -contains '--project' -and
            $Arguments -contains 'FullyQualifiedName~.L0Tests.' -and $Arguments -notcontains '--ignore-exit-code'
        }
    }

    It 'rejects empty ordinary modules: <Executed>' -ForEach @(@{ Executed = 0 }, @{ Executed = 2 }) {
        $solution = Join-Path $TestDrive 'test.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('Widget.L0Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
            Set-Content (Join-Path $directory 'result.trx') "<TestRun><ResultSummary><Counters executed='$Executed' /></ResultSummary></TestRun>"
        } -ModuleName RepositoryAutomation
        if ($Executed -eq 0) {
            { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'empty') } | Should -Throw '*No tests executed*'
        }
        else {
            (Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'passing')).ResultsDirectory | Should -Not -BeNullOrEmpty
        }
    }

    It 'allows only the existing empty SDK facades and still rejects an entirely empty run' {
        $solution = Join-Path $TestDrive 'facades.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('Sdk.Client.L0Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
            Set-Content (Join-Path $directory 'empty.trx') '<TestRun><ResultSummary><Counters executed="0" /></ResultSummary></TestRun>'
        } -ModuleName RepositoryAutomation
        { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'facades') } | Should -Throw '*No tests executed*'
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 1 -Exactly -ParameterFilter {
            $Arguments -contains '--ignore-exit-code' -and $Arguments -contains '8'
        }
    }
    It 'isolates cleanup caches between invocations' {
        $solution = Join-Path $TestDrive 'cleanup.slnx'
        $settings = Join-Path $TestDrive 'Directory.DotSettings'
        Set-Content $solution '<Solution />'
        Set-Content $settings '<ResourceDictionary />'
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess { '10.0.400' } -ModuleName RepositoryAutomation -ParameterFilter { $Arguments[0] -eq '--version' }
        Invoke-ReSharperCleanup -SolutionPath $solution -SettingsPath $settings
        Invoke-ReSharperCleanup -SolutionPath $solution -SettingsPath $settings
        $caches = @(Get-ChildItem (Join-Path $TestDrive '.scratchpad/cleanup-caches') -Directory)
        $caches.Count | Should -Be 2
        foreach ($cache in $caches) {
            $expectedArgument = "--caches-home=$($cache.FullName)"
            Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Exactly 1 -ParameterFilter {
                $Arguments -contains $expectedArgument -and $Arguments -contains '--dotnetcoresdk=10.0.400'
            }
        }
    }

    It 'rejects a missing module report even when another module executes tests' {
        $solution = Join-Path $TestDrive 'partial.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('First.L0Tests.csproj', 'Second.L0Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            if ($Arguments -contains 'First.L0Tests.csproj') {
                $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
                Set-Content (Join-Path $directory 'result.trx') '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
            }
        } -ModuleName RepositoryAutomation
        { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'partial') } |
            Should -Throw '*No TRX reports were produced*'
    }

    It 'rejects a test level with no matching projects' {
        $solution = Join-Path $TestDrive 'missing.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('Widget.L0Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation
        { Invoke-SolutionTests -SolutionPath $solution -TestLevels L4Tests } | Should -Throw '*No projects match*'
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 0 -Exactly
    }

    It 'collects a later module result after an earlier module fails' {
        $solution = Join-Path $TestDrive 'failures.slnx'
        Set-Content $solution '<Solution />'
        Mock Get-TestProjects { @('First.L0Tests.csproj', 'Second.L0Tests.csproj') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            if ($Arguments -contains 'First.L0Tests.csproj') { throw 'first module failed' }
            $directory = $Arguments[[array]::IndexOf($Arguments, '--results-directory') + 1]
            Set-Content (Join-Path $directory 'result.trx') '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
        } -ModuleName RepositoryAutomation
        { Invoke-SolutionTests -SolutionPath $solution -ResultsRoot (Join-Path $TestDrive 'failures') } |
            Should -Throw '*first module failed*'
        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -Times 2 -Exactly
    }
    It 'stops the pipeline when the coverage summarizer exits unsuccessfully' {
        Mock Invoke-MississippiSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-MississippiSolutionUnitTests { [pscustomobject]@{ CoverageReportPath = (Join-Path $TestDrive 'coverage.cobertura.xml') } } -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess { throw 'summarizer exited 1' } -ModuleName RepositoryAutomation
        { Invoke-SolutionsPipeline -RepoRoot $TestDrive -SkipCleanup -LeaseDirectory (Join-Path $TestDrive ('pipeline-leases-' + [guid]::NewGuid().ToString('N'))) } | Should -Throw '*summarizer exited 1*'
        Should -Invoke Invoke-SampleSolutionBuild -ModuleName RepositoryAutomation -Times 0 -Exactly
    }

    It 'runs cleanup before authoritative solution tests' {
        $calls = [System.Collections.Generic.List[string]]::new()
        Mock Invoke-MississippiSolutionBuild { $calls.Add('mississippi-build') } -ModuleName RepositoryAutomation
        Mock Invoke-MississippiSolutionCleanup { $calls.Add('mississippi-cleanup') } -ModuleName RepositoryAutomation
        Mock Invoke-MississippiSolutionUnitTests {
            $calls.Add('mississippi-tests')
            [pscustomobject]@{ CoverageReportPath = Join-Path $TestDrive 'coverage.cobertura.xml' }
        } -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionBuild { $calls.Add('sample-build') } -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionCleanup { $calls.Add('sample-cleanup') } -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionUnitTests { $calls.Add('sample-tests') } -ModuleName RepositoryAutomation
        Mock Invoke-FinalSolutionsBuild { $calls.Add('final-build') } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation

        Invoke-SolutionsPipeline -RepoRoot $TestDrive -LeaseDirectory (Join-Path $TestDrive ('pipeline-leases-' + [guid]::NewGuid().ToString('N'))) | Out-Null

        @($calls | Where-Object { $_ -eq 'mississippi-cleanup' }).Count | Should -Be 1
        @($calls | Where-Object { $_ -eq 'sample-cleanup' }).Count | Should -Be 1
        $calls.IndexOf('mississippi-cleanup') | Should -BeLessThan $calls.IndexOf('mississippi-tests')
        $calls.IndexOf('sample-cleanup') | Should -BeLessThan $calls.IndexOf('sample-tests')
        $calls.IndexOf('final-build') | Should -BeGreaterThan $calls.IndexOf('sample-tests')
    }

    It 'uses the resolved physical root for protected pipeline operations' {
        $realRoot = Join-Path $TestDrive 'pipeline-real-root'
        $aliasRoot = Join-Path $TestDrive 'pipeline-alias-root'
        $observedRoots = [System.Collections.Generic.List[string]]::new()
        New-Item -ItemType Directory -Path $realRoot -Force | Out-Null
        $aliasCreated = $false
        try {
            $aliasType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
            New-Item -ItemType $aliasType -Path $aliasRoot -Target $realRoot -ErrorAction Stop | Out-Null
            $aliasCreated = $true
            Mock Invoke-MississippiSolutionBuild { $observedRoots.Add($RepoRoot) } -ModuleName RepositoryAutomation
            Mock Invoke-MississippiSolutionUnitTests {
                $observedRoots.Add($RepoRoot)
                [pscustomobject]@{ CoverageReportPath = Join-Path $RepoRoot 'coverage.cobertura.xml' }
            } -ModuleName RepositoryAutomation
            Mock Invoke-SampleSolutionBuild { $observedRoots.Add($RepoRoot) } -ModuleName RepositoryAutomation
            Mock Invoke-SampleSolutionUnitTests { $observedRoots.Add($RepoRoot) } -ModuleName RepositoryAutomation
            Mock Invoke-FinalSolutionsBuild { $observedRoots.Add($RepoRoot) } -ModuleName RepositoryAutomation
            Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation

            Invoke-SolutionsPipeline -RepoRoot $aliasRoot -SkipCleanup -LeaseDirectory (Join-Path $TestDrive ('pipeline-leases-' + [guid]::NewGuid().ToString('N'))) | Out-Null

            $expectedRoot = (Get-Item -LiteralPath $realRoot).FullName
            $observedRoots.Count | Should -Be 5
            foreach ($observedRoot in $observedRoots) {
                $observedRoot | Should -Be $expectedRoot
            }
        }
        catch {
            if (-not $aliasCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create directory junctions.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $aliasRoot -Force -ErrorAction SilentlyContinue
        }
    }

    It 'forwards the exact Mississippi coverage report to the summarizer' {
        $coveragePath = Join-Path $TestDrive 'exact-run/coverage.cobertura.xml'
        Mock Invoke-MississippiSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-MississippiSolutionUnitTests { [pscustomobject]@{ CoverageReportPath = $coveragePath } } -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-SampleSolutionUnitTests {} -ModuleName RepositoryAutomation
        Mock Invoke-FinalSolutionsBuild {} -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {} -ModuleName RepositoryAutomation

        Invoke-SolutionsPipeline -RepoRoot $TestDrive -SkipCleanup -LeaseDirectory (Join-Path $TestDrive ('pipeline-leases-' + [guid]::NewGuid().ToString('N'))) | Out-Null

        Should -Invoke Invoke-RepositoryProcess -ModuleName RepositoryAutomation -ParameterFilter {
            $Arguments -contains '-CoverageReportPath' -and $Arguments -contains $coveragePath
        }
    }

    It 'returns the exact aggregated coverage path through PassThru' {
        $repo = Join-Path $TestDrive 'passthru-repository'
        New-Item -ItemType Directory -Path $repo -Force | Out-Null
        $runDirectory = Join-Path $repo 'run-1'
        Mock Invoke-DotnetToolRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionTests {
            New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $runDirectory 'module.cobertura.xml') -Value '<coverage />'
            [pscustomobject]@{ ResultsDirectory = $runDirectory }
        } -ModuleName RepositoryAutomation
        Mock Invoke-RepositoryProcess {
            $targetArgument = @($Arguments | Where-Object { $_ -like '-targetdir:*' })[0]
            $targetDirectory = $targetArgument.Substring('-targetdir:'.Length)
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            Set-Content -LiteralPath (Join-Path $targetDirectory 'Cobertura.xml') -Value '<coverage />'
        } -ModuleName RepositoryAutomation

        $result = Invoke-MississippiSolutionUnitTests -RepoRoot $repo -PassThru

        $result.CoverageReportPath | Should -Be (Join-Path $runDirectory 'coverage.cobertura.xml')
        Test-Path -LiteralPath $result.CoverageReportPath -PathType Leaf | Should -BeTrue
    }

    It 'streams compiler diagnostics before a failing build step throws' {
        $solution = Join-Path $TestDrive 'failure.slnx'
        Set-Content $solution '<Solution />'
        Mock Invoke-RepositoryProcess { 'Compiler error details'; throw 'Build failed' } -ModuleName RepositoryAutomation
        Mock Out-Host {} -ModuleName RepositoryAutomation
        Mock Invoke-DotnetToolRestore {} -ModuleName RepositoryAutomation
        Mock Invoke-SolutionRestore {} -ModuleName RepositoryAutomation
        Copy-Item $solution (Join-Path $TestDrive 'mississippi.slnx')
        { Invoke-AutomationStep -Name Build -Action { Invoke-MississippiSolutionBuild -RepoRoot $TestDrive } } | Should -Throw '*Build failed*'
        Should -Invoke Out-Host -ModuleName RepositoryAutomation -Times 1 -Exactly
    }

    It 'preserves the original exception identity and data when logging a failed automation step' {
        $original = [InvalidOperationException]::new('Build failed')
        $original.Data['FailureId'] = 'original-build-failure'
        $failure = $null

        try {
            Invoke-AutomationStep -Name Build -Action { throw $original }
        }
        catch {
            $failure = $_.Exception
        }

        $failure | Should -Not -BeNullOrEmpty
        [object]::ReferenceEquals($failure, $original) | Should -BeTrue
        $failure.GetType() | Should -Be ([InvalidOperationException])
        $failure.Data['FailureId'] | Should -Be 'original-build-failure'
    }

    It 'preserves compiler output in an artifact even when the build fails' {
        $solution = Join-Path $TestDrive 'logged.slnx'
        Set-Content $solution '<Solution />'
        $log = Join-Path $TestDrive 'build.log'
        Mock Invoke-RepositoryProcess { 'Compiler error details'; throw 'Build failed' } -ModuleName RepositoryAutomation
        { Invoke-SolutionBuild -SolutionPath $solution | Tee-Object -FilePath $log | Out-Null } |
            Should -Throw '*Build failed*'
        Get-Content -LiteralPath $log -Raw | Should -Match 'Compiler error details'
    }
}


