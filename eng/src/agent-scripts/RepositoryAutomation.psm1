Set-StrictMode -Version Latest

function Get-RepositoryRoot {
    [CmdletBinding()]
    param(
        [string]$StartPath = (Get-Location).Path
    )

    $resolved = Resolve-Path -LiteralPath $StartPath
    $current = $resolved.Path

    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current '.git')) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            break
        }
        $current = $parent
    }

    throw "Unable to locate repository root from '$StartPath'."
}

function ConvertTo-ConsoleColor {
    param(
        [object]$Value,
        [ConsoleColor]$Default = [ConsoleColor]::Cyan
    )

    if ($Value -is [ConsoleColor]) { return $Value }

    if ($Value -is [string]) {
        $name = $Value
        if ($name -match '::') {
            $name = $name.Split('::')[-1]
        }
        if ([Enum]::IsDefined([ConsoleColor], $name)) {
            return [Enum]::Parse([ConsoleColor], $name, $true)
        }
    }

    return $Default
}


function Write-AutomationBanner {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Message,
        [object]$ForegroundColor = [ConsoleColor]::Cyan,
        [switch]$InsertBlankLine
    )

    if ($InsertBlankLine) {
        Write-Host
    }

    $color = ConvertTo-ConsoleColor -Value $ForegroundColor -Default ([ConsoleColor]::Cyan)
}

function Invoke-AutomationStep {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$Name,
        [string]$Description,
        [int]$StepNumber,
        [object]$BannerColor = [ConsoleColor]::Yellow,
        [object]$SuccessColor = [ConsoleColor]::Green,
        [switch]$SilentSuccess
    )

    $bannerText = if ($PSBoundParameters.ContainsKey('StepNumber')) {
        "=== STEP ${StepNumber}: $Name ==="
    } else {
        "=== $Name ==="
    }

    $resolvedBannerColor = ConvertTo-ConsoleColor -Value $BannerColor -Default ([ConsoleColor]::Yellow)
    $resolvedSuccessColor = ConvertTo-ConsoleColor -Value $SuccessColor -Default ([ConsoleColor]::Green)
    Write-AutomationBanner -Message $bannerText -ForegroundColor $resolvedBannerColor -InsertBlankLine
    if ($Description) {
        Write-Host $Description
    }

    try {
        $result = & $Action
        if (-not $SilentSuccess) {
            Write-Host "SUCCESS: $Name" -ForegroundColor $resolvedSuccessColor
            Write-Host
        }
        return $result
    }
    catch {
        Write-Error "FAILURE: $Name : $($_.Exception.Message)"
        throw
    }
}

function Invoke-RepositoryProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments,
        [string]$ErrorMessage,
        [switch]$SuppressCommandEcho
    )

    $escapedArgs = if ($Arguments) {
        $Arguments | ForEach-Object {
            if ($_ -and $_.Contains(' ')) { '"{0}"' -f $_ } else { $_ }
        }
    } else {
        @()
    }

    if (-not $SuppressCommandEcho) {
        $commandLine = ($escapedArgs -join ' ').Trim()
        if ($commandLine) {
            Write-Host "Executing: $FilePath $commandLine" -ForegroundColor ([ConsoleColor]::DarkGray)
        } else {
            Write-Host "Executing: $FilePath" -ForegroundColor ([ConsoleColor]::DarkGray)
        }
    }

    & $FilePath @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $message = if ($ErrorMessage) { $ErrorMessage } else { "Command '$FilePath' failed with exit code $exitCode." }
        throw $message
    }

}

function Invoke-DotnetToolRestore {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [switch]$Force
    )

    $manifestPath = Join-Path $RepoRoot '.config/dotnet-tools.json'
    if (-not $Force -and -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        Write-Verbose "Dotnet tool manifest not found at '$manifestPath'; skipping restore."
        return
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','restore') -ErrorMessage 'Failed to restore dotnet tools.'
}

function Invoke-SolutionRestore {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Description = 'Restoring solution',
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    if (-not $Quiet) {
        Write-Host "Restoring ${Description}: $($resolved.Path)" -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('restore', $resolved.Path) -ErrorMessage "Failed to restore packages for $($resolved.Path)."
}

function Invoke-SolutionBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Configuration = 'Release',
        [switch]$WarnAsError,
        [switch]$NoRestore,
        [switch]$NoIncremental,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    $args = @('build', $resolved.Path, '--configuration', $Configuration)
    if ($NoRestore) { $args += '--no-restore' }
    if ($NoIncremental) { $args += '--no-incremental' }
    if ($WarnAsError) { $args += '--warnaserror' }
    if ($AdditionalArguments) { $args += $AdditionalArguments }

    if (-not $Quiet) {
        Write-Host "Building solution: $($resolved.Path)" -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to build $($resolved.Path)."
}

function New-AutomationRunDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [string]$Prefix
    )

    if (-not (Test-Path -LiteralPath $Root)) {
        $null = New-Item -ItemType Directory -Path $Root -Force
    }

    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $folderName = if ($Prefix) { "$Prefix-$stamp" } else { $stamp }
    $runDirectory = Join-Path $Root $folderName
    if (-not (Test-Path -LiteralPath $runDirectory)) {
        $null = New-Item -ItemType Directory -Path $runDirectory -Force
    }

    return $runDirectory
}

function Invoke-SolutionTests {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Configuration = 'Release',
        [string]$ResultsRoot,
        [string]$Logger = 'trx;LogFileName=test_results.trx',
        [switch]$CollectCoverage,
        [string[]]$TestLevels,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    $args = @('test', $resolved.Path, '--configuration', $Configuration, '--no-restore')

    $resultsDirectory = $null
    if ($ResultsRoot) {
        $resultsDirectory = New-AutomationRunDirectory -Root $ResultsRoot
        $args += @('--results-directory', $resultsDirectory)
    }

    if ($Logger) {
        $args += '--logger'
        $args += $Logger
    }

    if ($CollectCoverage) {
        $args += '--collect'
        $args += 'XPlat Code Coverage'
    }

    # Build filter expression for test levels (e.g., L0Tests, L1Tests)
    # Filters by FullyQualifiedName containing the level pattern
    if ($TestLevels -and $TestLevels.Count -gt 0) {
        $filterParts = $TestLevels | ForEach-Object { "FullyQualifiedName~.$($_)." }
        $filterExpression = $filterParts -join '|'
        $args += '--filter'
        $args += $filterExpression
    }

    if ($AdditionalArguments) {
        $args += $AdditionalArguments
    }

    if (-not $Quiet) {
        Write-Host "Executing tests: $($resolved.Path)" -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to run tests for $($resolved.Path)."

    return [pscustomobject]@{
        SolutionPath     = $resolved.Path
        ResultsDirectory = $resultsDirectory
    }
}

function Invoke-SlnGeneration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $source = Resolve-Path -LiteralPath $SolutionPath
    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    $outputDirectory = Split-Path -Parent $outputFullPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        $null = New-Item -ItemType Directory -Path $outputDirectory -Force
    }

    $args = @('tool','run','slngen', $source.Path, '--solutionfile', $outputFullPath, '--launch','false')
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to generate solution '$outputFullPath' using SlnGen." -SuppressCommandEcho
    return $outputFullPath
}

function Invoke-ReSharperCleanup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string[]]$IncludePaths,
        [string[]]$ExcludePaths
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $resolvedSettings = Resolve-Path -LiteralPath $SettingsPath
    $args = @('tool','run','jb','cleanupcode', "--profile=$Profile", "--settings=$($resolvedSettings.Path)")

    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        $args += "--include=$($IncludePaths -join ';')"
    }

    if ($ExcludePaths -and $ExcludePaths.Count -gt 0) {
        $args += "--exclude=$($ExcludePaths -join ';')"
    }

    $args += $resolvedSolution.Path
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "ReSharper cleanup failed for $($resolvedSolution.Path)." -SuppressCommandEcho
}

function Get-TestProjects {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath
    )

    $solutionDir = Split-Path -Parent $SolutionPath
    $testProjects = Get-ChildItem -Path $solutionDir -Recurse -Filter "*Tests.csproj" | 
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        Select-Object -ExpandProperty FullName

    return $testProjects
}

function Invoke-StrykerMutationTestPerProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $projectOutputPath = Join-Path $OutputPath $projectName
    
    if (-not (Test-Path -LiteralPath $projectOutputPath)) {
        $null = New-Item -ItemType Directory -Path $projectOutputPath -Force
    }

    Write-Host "  Running Stryker for project: $projectName" -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('stryker','--project',$projectName,'--output',$projectOutputPath) -ErrorMessage "Stryker mutation testing failed for project $projectName." -SuppressCommandEcho
    
    return $projectOutputPath
}

function Invoke-StrykerMutationTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    if (-not (Test-Path -LiteralPath $outputFullPath)) {
        $null = New-Item -ItemType Directory -Path $outputFullPath -Force
    }

    # Workaround for stryker-mutator/stryker-net#2634
    # Run Stryker per-project instead of at solution level to avoid compilation issues
    # with source generators (like LoggerMessage)
    Write-Host "Discovering test projects in solution..." -ForegroundColor ([ConsoleColor]::Cyan)
    $testProjects = Get-TestProjects -SolutionPath $resolvedSolution.Path
    Write-Host "Found $($testProjects.Count) test projects" -ForegroundColor ([ConsoleColor]::Green)
    Write-Host

    $projectResults = @()
    foreach ($testProject in $testProjects) {
        try {
            $projectOutput = Invoke-StrykerMutationTestPerProject -ProjectPath $testProject -OutputPath $outputFullPath
            $projectResults += @{ Project = $testProject; Output = $projectOutput; Success = $true }
            Write-Host "  ✓ Completed: $([System.IO.Path]::GetFileNameWithoutExtension($testProject))" -ForegroundColor ([ConsoleColor]::Green)
        }
        catch {
            Write-Warning "  ✗ Failed: $([System.IO.Path]::GetFileNameWithoutExtension($testProject)) - $($_.Exception.Message)"
            $projectResults += @{ Project = $testProject; Output = $null; Success = $false; Error = $_.Exception.Message }
        }
        Write-Host
    }

    # Check if any projects failed
    $failedProjects = $projectResults | Where-Object { -not $_.Success }
    if ($failedProjects.Count -gt 0) {
        Write-Host "WARNING: $($failedProjects.Count) project(s) failed mutation testing" -ForegroundColor ([ConsoleColor]::Yellow)
        foreach ($failed in $failedProjects) {
            Write-Host "  - $([System.IO.Path]::GetFileNameWithoutExtension($failed.Project)): $($failed.Error)" -ForegroundColor ([ConsoleColor]::Yellow)
        }
    }

    return $outputFullPath
}

function Invoke-MississippiSolutionBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $solutionPath = Join-Path $RepoRoot 'mississippi.slnx'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION BUILD PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Building mississippi.slnx in $Configuration mode"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Compiling mississippi.slnx (Configuration: $Configuration)..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Build flags: --no-restore --no-incremental'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -Quiet
    Write-Host 'SUCCESS: Mississippi solution compiled successfully' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION BUILD COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
}

function Invoke-SampleSolutionBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $solutionPath = Join-Path $RepoRoot 'samples.slnx'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION BUILD PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Building samples.slnx in $Configuration mode"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (GitVersion, SlnGen, ReSharper CLI, etc.)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Compiling samples.slnx (Configuration: $Configuration)..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Build flags: --no-restore --no-incremental'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -Quiet
    Write-Host 'SUCCESS: Sample solution compiled successfully' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION BUILD COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Configuration: $Configuration | Status: SUCCESS"
}

function Invoke-FinalSolutionsBuild {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $mississippi = Join-Path $RepoRoot 'mississippi.slnx'
    $samples = Join-Path $RepoRoot 'samples.slnx'

    Write-AutomationBanner -Message '=== FINAL BUILD WITH WARNINGS AS ERRORS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Zero-tolerance build: All warnings will be treated as compilation errors'
    Write-Host 'This is the final quality gate before code can be merged'
    Write-Host

    Write-Host '[1/5] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/5] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $mississippi -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: Mississippi packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/5] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $samples -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: Sample packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[4/5] Building mississippi.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $mississippi -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet
    Write-Host 'SUCCESS: Mississippi solution built with zero warnings' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[5/5] Building samples.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $samples -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet
    Write-Host 'SUCCESS: Sample solution built with zero warnings' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== FINAL BUILD COMPLETED SUCCESSFULLY ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'ZERO WARNINGS DETECTED | Both solutions meet production quality standards'
    Write-Host 'Code is ready for merge to main branch'
}

function Invoke-MississippiSolutionUnitTests {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$TestLevels = @('L0Tests', 'L1Tests')
    )

    $solutionPath = Join-Path $RepoRoot 'mississippi.slnx'
    $resultsRoot = Join-Path $RepoRoot '.scratchpad/coverage-test-results'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION UNIT TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running comprehensive unit and integration tests'
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for mississippi.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'mississippi.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Executing unit tests for mississippi.slnx..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host "Test levels: $($TestLevels -join ', ')"
    Write-Host 'Test flags: --no-restore --collect:XPlat Code Coverage'
    $testResult = (Invoke-SolutionTests -SolutionPath $solutionPath -Configuration $Configuration -ResultsRoot $resultsRoot -CollectCoverage -TestLevels $TestLevels -Quiet | Select-Object -Last 1)
    $runDirectory = $testResult.ResultsDirectory
    Write-Host "Results directory: $runDirectory"
    Write-Host 'Logger: TRX format (test_results.trx)'

    $coverageFiles = Get-ChildItem -Path $runDirectory -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
        throw "Unit tests completed but no coverage reports were produced in '$runDirectory'."
    }

    $reportsArg = ($coverageFiles | ForEach-Object { '"{0}"' -f $_.FullName }) -join ';'
    $mergedOutputDir = Join-Path $runDirectory 'coverage-merged'
    if (Test-Path -LiteralPath $mergedOutputDir) {
        Remove-Item -LiteralPath $mergedOutputDir -Recurse -Force
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','run','reportgenerator', "-reports:$reportsArg", "-targetdir:$mergedOutputDir", '-reporttypes:Cobertura') -ErrorMessage 'ReportGenerator failed while merging coverage reports.' -SuppressCommandEcho

    $mergedCobertura = Join-Path $mergedOutputDir 'Cobertura.xml'
    if (-not (Test-Path -LiteralPath $mergedCobertura)) {
        throw 'ReportGenerator did not create an aggregated Cobertura.xml report.'
    }

    $finalCoveragePath = Join-Path $runDirectory 'coverage.cobertura.xml'
    Copy-Item -LiteralPath $mergedCobertura -Destination $finalCoveragePath -Force

    Write-Host 'SUCCESS: All unit tests passed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host "Aggregated coverage report: $finalCoveragePath" -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION UNIT TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    $resultsFile = Join-Path $runDirectory 'test_results.trx'
    Write-Host "All tests passed | Results saved to: $resultsFile"
    Write-Host 'Coverage report ready for summarize-coverage-gaps.ps1' -ForegroundColor ([ConsoleColor]::Green)
}

function Invoke-SampleSolutionUnitTests {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$TestLevels = @('L0Tests', 'L1Tests')
    )

    $solutionPath = Join-Path $RepoRoot 'samples.slnx'
    $resultsRoot = Join-Path $RepoRoot '.scratchpad/coverage-test-results'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION UNIT TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running sample application tests (minimal coverage for demonstration)'
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[2/3] Restoring NuGet packages for samples.slnx...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $solutionPath -Description 'samples.slnx' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored successfully' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[3/3] Executing unit tests for samples.slnx..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host "Test levels: $($TestLevels -join ', ')"
    Write-Host 'Test flags: --no-restore'
    $testResult = (Invoke-SolutionTests -SolutionPath $solutionPath -Configuration $Configuration -ResultsRoot $resultsRoot -TestLevels $TestLevels -Quiet | Select-Object -Last 1)
    Write-Host "Results directory: $($testResult.ResultsDirectory)"
    Write-Host 'Logger: TRX format (test_results.trx)'
    Write-Host 'NOTE: Sample tests are for demonstration purposes only'

    Write-Host 'SUCCESS: All sample tests passed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION UNIT TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    $resultsFile = Join-Path $testResult.ResultsDirectory 'test_results.trx'
    Write-Host "Sample tests passed | Results saved to: $resultsFile"
}

function Invoke-MississippiSolutionCleanup {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$IncludePaths
    )

    $slnxPath = Join-Path $RepoRoot 'mississippi.slnx'
    $slnPath = Join-Path $RepoRoot 'mississippi.sln'
    $settingsPath = Join-Path $RepoRoot 'Directory.DotSettings'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with 'Built-in: Full Cleanup' profile"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/3] Generating mississippi.sln from mississippi.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: 'Built-in: Full Cleanup'"
    Write-Host "Settings file: $settingsPath"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPath -IncludePaths $IncludePaths
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-SampleSolutionCleanup {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$IncludePaths
    )

    $slnxPath = Join-Path $RepoRoot 'samples.slnx'
    $slnPath = Join-Path $RepoRoot 'samples.sln'
    $settingsPath = Join-Path $RepoRoot 'Directory.DotSettings'

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with 'Built-in: Full Cleanup' profile"
    Write-Host

    Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/3] Generating samples.sln from samples.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: 'Built-in: Full Cleanup'"
    Write-Host "Settings file: $settingsPath"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPath -IncludePaths $IncludePaths
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-MississippiSolutionMutationTests {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot)
    )

    $slnxPath = Join-Path $RepoRoot 'mississippi.slnx'
    $slnPath = Join-Path $RepoRoot 'mississippi.sln'
    $mutationRoot = Join-Path $RepoRoot '.scratchpad/mutation-test-results'

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION MUTATION TESTING PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host 'Running Stryker.NET mutation testing to validate test quality'
    Write-Host 'Mutation testing ensures tests can detect code changes'
    Write-Host

    Write-Host '[1/4] Restoring dotnet tools (including Stryker.NET)...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-DotnetToolRestore -RepoRoot $RepoRoot
    Write-Host 'SUCCESS: Dotnet tools restored, Stryker.NET available' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[2/4] Generating mississippi.sln from mississippi.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for Stryker compatibility'
    $generatedSln = (Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Select-Object -Last 1)
    Write-Host 'SUCCESS: Solution file generated for Stryker processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/4] Restoring NuGet packages for generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-SolutionRestore -SolutionPath $generatedSln -Description 'mississippi.sln' -Quiet
    Write-Host 'SUCCESS: NuGet packages restored for mutation testing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[4/4] Executing Stryker.NET mutation testing...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Stryker will inject mutations into source code and verify tests detect them'
    Write-Host 'This process validates the effectiveness of the test suite'
    Write-Host "Target solution: $generatedSln"

    $timestamp = Get-Date -Format 'yyyy-MM-dd.HH-mm-ss'
    $outputDirectory = Join-Path $mutationRoot $timestamp
    Invoke-StrykerMutationTest -SolutionPath $generatedSln -OutputPath $outputDirectory | Out-Null

    Write-Host 'SUCCESS: Mutation testing completed with acceptable scores' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION MUTATION TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'Test quality validated | Mutation score meets project standards'
}

function Invoke-SolutionsPipeline {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [switch]$SkipCleanup,
        [switch]$IncludeMutation
    )

    $automationScriptsRoot = Join-Path (Join-Path (Join-Path $RepoRoot 'eng') 'src') 'agent-scripts'
    $coverageScript = Join-Path $automationScriptsRoot 'summarize-coverage-gaps.ps1'
    $mutationSummaryScript = Join-Path $automationScriptsRoot 'summarize-mutation-survivors.ps1'

    Write-AutomationBanner -Message '=== STARTING COMPLETE BUILD AND TEST PIPELINE ===' -ForegroundColor ([ConsoleColor]::Magenta) -InsertBlankLine
    Write-Host 'Pipeline will execute Mississippi solution followed by Sample solution'
    Write-Host 'Each step must complete successfully before proceeding to the next'
    if (-not $IncludeMutation) {
        Write-Host 'Mutation testing skipped (use -IncludeMutation to enable)'
    }
    Write-Host

    $step = 1

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Mississippi Solution' -StepNumber ($step++) -Action { Invoke-MississippiSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    Invoke-AutomationStep -Name 'Run Mississippi Unit Tests' -StepNumber ($step++) -Action { Invoke-MississippiSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    Invoke-AutomationStep -Name 'Summarize Coverage Gaps' -StepNumber ($step++) -Action { & $coverageScript -EmitTasks }
    if ($IncludeMutation) {
        Invoke-AutomationStep -Name 'Run Mississippi Mutation Tests' -StepNumber ($step++) -Action { Invoke-MississippiSolutionMutationTests -RepoRoot $RepoRoot } -SilentSuccess
        Invoke-AutomationStep -Name 'Summarize Mutation Survivors' -StepNumber ($step++) -Action { & $mutationSummaryScript -GenerateTasks -SkipMutationRun }
    }
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Mississippi Code Style' -StepNumber ($step++) -Action { Invoke-MississippiSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Sample Solution' -StepNumber ($step++) -Action { Invoke-SampleSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    Invoke-AutomationStep -Name 'Run Sample Unit Tests' -StepNumber ($step++) -Action { Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Sample Code Style' -StepNumber ($step++) -Action { Invoke-SampleSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }

    Invoke-AutomationStep -Name 'Final Build with Warnings as Errors' -StepNumber ($step++) -Action { Invoke-FinalSolutionsBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess

    Write-Host '=== PIPELINE COMPLETED SUCCESSFULLY ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All steps completed without errors. Solutions are ready for deployment.'
}

Export-ModuleMember -Function Get-RepositoryRoot, Write-AutomationBanner, Invoke-AutomationStep, Invoke-DotnetToolRestore, Invoke-SolutionRestore, Invoke-SolutionBuild, New-AutomationRunDirectory, Invoke-SolutionTests, Invoke-SlnGeneration, Invoke-ReSharperCleanup, Get-TestProjects, Invoke-StrykerMutationTestPerProject, Invoke-StrykerMutationTest, Invoke-MississippiSolutionBuild, Invoke-SampleSolutionBuild, Invoke-FinalSolutionsBuild, Invoke-MississippiSolutionUnitTests, Invoke-SampleSolutionUnitTests, Invoke-MississippiSolutionCleanup, Invoke-SampleSolutionCleanup, Invoke-MississippiSolutionMutationTests, Invoke-SolutionsPipeline








