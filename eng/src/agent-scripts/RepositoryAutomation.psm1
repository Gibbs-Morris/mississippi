#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sharedExecutionLeaseDirectoryMode = [System.IO.UnixFileMode]::UserRead -bor
    [System.IO.UnixFileMode]::UserWrite -bor
    [System.IO.UnixFileMode]::UserExecute -bor
    [System.IO.UnixFileMode]::GroupRead -bor
    [System.IO.UnixFileMode]::GroupWrite -bor
    [System.IO.UnixFileMode]::GroupExecute -bor
    [System.IO.UnixFileMode]::OtherRead -bor
    [System.IO.UnixFileMode]::OtherWrite -bor
    [System.IO.UnixFileMode]::OtherExecute -bor
    [System.IO.UnixFileMode]::StickyBit
$sharedExecutionLeaseFileMode = [System.IO.UnixFileMode]::UserRead -bor
    [System.IO.UnixFileMode]::UserWrite -bor
    [System.IO.UnixFileMode]::GroupRead -bor
    [System.IO.UnixFileMode]::GroupWrite -bor
    [System.IO.UnixFileMode]::OtherRead -bor
    [System.IO.UnixFileMode]::OtherWrite

function Set-RepositoryExecutionLeaseUnixMode {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][System.IO.UnixFileMode]$Mode
    )

    if ($IsWindows) { return }

    try {
        [System.IO.File]::SetUnixFileMode($Path, $Mode)
        $actualMode = [System.IO.File]::GetUnixFileMode($Path)
    }
    catch {
        throw "Unable to set shared execution lease permissions on '$Path': $($_.Exception.Message)"
    }

    if (([int]$actualMode -band [int]$Mode) -ne [int]$Mode) {
        throw "Shared execution lease permissions on '$Path' are insufficient for all participating accounts."
    }
}

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

function Get-RepositoryPathComparison {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)

    if ($IsWindows) { return [System.StringComparison]::OrdinalIgnoreCase }
    if (-not $IsMacOS) { return [System.StringComparison]::Ordinal }

    $probeName = '.mississippi-case-probe-' + [guid]::NewGuid().ToString('N')
    $probePath = Join-Path $RepoRoot $probeName
    try {
        New-Item -ItemType Directory -LiteralPath $probePath -Force -ErrorAction Stop | Out-Null
        if (Test-Path -LiteralPath (Join-Path $RepoRoot $probeName.ToUpperInvariant())) {
            return [System.StringComparison]::OrdinalIgnoreCase
        }
    }
    catch {
        # An unproven macOS volume remains case-sensitive for identity.
    }
    finally {
        Remove-Item -LiteralPath $probePath -Recurse -Force -ErrorAction SilentlyContinue
    }

    return [System.StringComparison]::Ordinal
}

function Get-RepositoryExecutionLeasePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [string]$LeaseDirectory
    )

    $canonicalRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
    $keyRoot = if ((Get-RepositoryPathComparison -RepoRoot $canonicalRoot) -eq [System.StringComparison]::OrdinalIgnoreCase) { $canonicalRoot.ToLowerInvariant() } else { $canonicalRoot }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($keyRoot)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $fileName = (($hash | ForEach-Object { $_.ToString('x2') }) -join '') + '.lease'
    $sharedLease = $env:MISSISSIPPI_SHARED_WORKTREE -eq 'true'
    $leaseDirectory = if ([string]::IsNullOrWhiteSpace($LeaseDirectory)) { Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) '.mississippi/execution-leases' } else { [System.IO.Path]::GetFullPath($LeaseDirectory) }
    if (Test-Path -LiteralPath $leaseDirectory) {
        $leaseItem = Get-Item -LiteralPath $leaseDirectory -Force -ErrorAction Stop
        if (-not $leaseItem.PSIsContainer -or [bool]($leaseItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Lease directory is not a trusted private directory: '$leaseDirectory'."
        }
    }
    else {
        New-Item -ItemType Directory -Path $leaseDirectory -Force | Out-Null
    }
    if ($sharedLease) {
        Set-RepositoryExecutionLeaseUnixMode -Path $leaseDirectory -Mode $sharedExecutionLeaseDirectoryMode
    }
    return Join-Path $leaseDirectory $fileName
}

function Resolve-RepositoryExecutionRoot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepoRoot)

    function Resolve-ReparsePathComponent {
        param(
            [Parameter(Mandatory)][string]$Path,
            [System.Collections.Generic.HashSet[string]]$SeenTargets
        )

        if ($null -eq $SeenTargets) {
            $comparison = if ((Get-RepositoryPathComparison -RepoRoot ([System.IO.Path]::GetFullPath($RepoRoot))) -eq [System.StringComparison]::OrdinalIgnoreCase) { [System.StringComparer]::OrdinalIgnoreCase } else { [System.StringComparer]::Ordinal }
            $SeenTargets = [System.Collections.Generic.HashSet[string]]::new($comparison)
        }

        $fullPath = [System.IO.Path]::GetFullPath($Path)
        $root = [System.IO.Path]::GetPathRoot($fullPath)
        $segments = @($fullPath.Substring($root.Length).Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) | Where-Object { $_ })
        $current = $root
        for ($index = 0; $index -lt $segments.Count; $index++) {
            $candidate = Join-Path $current $segments[$index]
            $item = Get-Item -LiteralPath $candidate -Force -ErrorAction Stop
            if (-not [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                $current = $item.FullName
                continue
            }

            if (-not $SeenTargets.Add([System.IO.Path]::GetFullPath($candidate))) {
                throw "Worktree path resolution loop detected at '$candidate'."
            }
            $target = @($item.Target | Select-Object -First 1)[0]
            if ([string]::IsNullOrWhiteSpace([string]$target)) {
                throw "Unable to resolve worktree path component '$candidate'."
            }
            if (-not [System.IO.Path]::IsPathRooted([string]$target)) {
                $target = Join-Path (Split-Path -Parent $candidate) ([string]$target)
            }
            $resolvedTarget = [System.IO.Path]::GetFullPath([string]$target)
            $remaining = @(
                if ($index -lt ($segments.Count - 1)) {
                    $segments[($index + 1)..($segments.Count - 1)]
                }
            )
            if (@($remaining).Count -gt 0) {
                $resolvedTarget = Join-Path $resolvedTarget ($remaining -join [System.IO.Path]::DirectorySeparatorChar)
            }
            return Resolve-ReparsePathComponent -Path $resolvedTarget -SeenTargets $SeenTargets
        }

        return $current
    }

    return Resolve-ReparsePathComponent -Path ([System.IO.Path]::GetFullPath($RepoRoot))
}

function Enter-RepositoryExecutionLease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [string]$OperationId = ([guid]::NewGuid().ToString('N')),
        [object]$ExistingLease,
        [string]$LeaseDirectory
    )

    if ($null -ne $ExistingLease) {
        if ($null -eq $ExistingLease.Stream -or $ExistingLease.Stream.SafeFileHandle.IsClosed -or -not $ExistingLease.Stream.CanRead) {
            throw 'Existing repository execution lease handle is closed or unavailable.'
        }
        $requestedRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
        $existingRoot = Resolve-RepositoryExecutionRoot -RepoRoot ([string]$ExistingLease.RepositoryRoot)
        $comparison = Get-RepositoryPathComparison -RepoRoot $requestedRoot
        if (-not [string]::Equals($requestedRoot, $existingRoot, $comparison)) {
            throw "Existing lease belongs to '$existingRoot', not requested worktree '$requestedRoot'."
        }
        return [pscustomobject]@{
            Path = $ExistingLease.Path
            OperationId = $ExistingLease.OperationId
            RepositoryRoot = $ExistingLease.RepositoryRoot
            Stream = $ExistingLease.Stream
            OwnsStream = $false
        }
    }
    if ([string]::IsNullOrWhiteSpace($LeaseDirectory) -and $env:MISSISSIPPI_SHARED_WORKTREE -eq 'true') {
        throw 'Cross-account shared worktrees require an explicit trusted -LeaseDirectory.'
    }
    $leasePath = Get-RepositoryExecutionLeasePath -RepoRoot $RepoRoot -LeaseDirectory $LeaseDirectory
    $canonicalRoot = Resolve-RepositoryExecutionRoot -RepoRoot $RepoRoot
    if (Test-Path -LiteralPath $leasePath) {
        $leaseItem = Get-Item -LiteralPath $leasePath -Force -ErrorAction Stop
        if ($leaseItem.PSIsContainer -or [bool]($leaseItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Lease path is not a regular file: '$leasePath'."
        }
    }
    $metadata = [ordered]@{
        operationId = $OperationId
        repositoryRoot = $canonicalRoot
        processId = $PID
        startedUtc = (Get-Date).ToUniversalTime().ToString('o')
    } | ConvertTo-Json -Compress

    $sharedLease = $env:MISSISSIPPI_SHARED_WORKTREE -eq 'true'
    $stream = $null
    try {
        $streamOptions = [System.IO.FileStreamOptions]::new()
        $streamOptions.Mode = [System.IO.FileMode]::OpenOrCreate
        $streamOptions.Access = [System.IO.FileAccess]::ReadWrite
        $streamOptions.Share = [System.IO.FileShare]::Read
        if ($sharedLease) { $streamOptions.UnixCreateMode = $sharedExecutionLeaseFileMode }
        $stream = [System.IO.FileStream]::new($leasePath, $streamOptions)
        if ($sharedLease) {
            Set-RepositoryExecutionLeaseUnixMode -Path $leasePath -Mode $sharedExecutionLeaseFileMode
        }
    }
    catch [System.UnauthorizedAccessException] {
        if ($sharedLease) {
            throw "Shared execution lease '$leasePath' is not writable by this account. Ensure existing lease files in the shared directory are writable by all participating accounts."
        }
        throw
    }
    catch [System.IO.IOException] {
        $errorCode = $_.Exception.HResult -band 0xFFFF
        if ($errorCode -notin @(32, 33)) { throw }
        $owner = ''
        try { $owner = (Get-Content -LiteralPath $leasePath -Raw -ErrorAction Stop).Trim() } catch { }
        throw "Worktree execution lease is held for '$canonicalRoot'. Current owner: $owner. Use a separate worktree or wait for the active operation."
    }

    try {
        $stream.SetLength(0)
        $metadataBytes = [System.Text.Encoding]::UTF8.GetBytes($metadata)
        $stream.Write($metadataBytes, 0, $metadataBytes.Length)
        $stream.Flush($true)
        return [pscustomobject]@{
            Path = $leasePath
            OperationId = $OperationId
            RepositoryRoot = $canonicalRoot
            Stream = $stream
            OwnsStream = $true
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Exit-RepositoryExecutionLease {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Lease)

    if ($Lease.OwnsStream -and $null -ne $Lease.Stream) { $Lease.Stream.Dispose() }
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
    Write-Host $Message -ForegroundColor $color
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
        Write-Error "FAILURE: $Name : $($_.Exception.Message)" -ErrorAction Continue
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

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','restore') -ErrorMessage 'Failed to restore dotnet tools.' | Out-Host
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

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('restore', $resolved.Path) -ErrorMessage "Failed to restore packages for $($resolved.Path)." | Out-Host
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
    $runId = [guid]::NewGuid().ToString('N')
    $folderName = if ($Prefix) { "$Prefix-$stamp-$runId" } else { "$stamp-$runId" }
    $runDirectory = Join-Path $Root $folderName
    if (-not (Test-Path -LiteralPath $runDirectory)) {
        $null = New-Item -ItemType Directory -Path $runDirectory -Force
    }

    return $runDirectory
}

function Get-PowerShellExecutable {
    [CmdletBinding()]
    param()

    return Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
}

function Get-TestExecutionCount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ResultsDirectory
    )

    $reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter '*.trx' -File -ErrorAction Stop)
    if ($reports.Count -eq 0) {
        throw "No TRX reports were produced in '$ResultsDirectory'. Check the test logger configuration."
    }
    $executed = 0
    foreach ($report in $reports) {
        [xml]$trx = Get-Content -LiteralPath $report.FullName -Raw
        $executed += [int]$trx.TestRun.ResultSummary.Counters.executed
    }
    return $executed
}

function Invoke-TestModule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ResultsDirectory,
        [string]$Configuration = 'Release',
        [switch]$CollectCoverage,
        [string[]]$TestLevels,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $moduleDirectory = Join-Path $ResultsDirectory $projectName
    $null = New-Item -ItemType Directory -Path $moduleDirectory -Force
    $arguments = @('test', '--project', $ProjectPath, '--configuration', $Configuration, '--no-restore',
        '--report-xunit-trx', '--report-xunit-trx-filename', "test_results_$projectName.trx",
        '--results-directory', $moduleDirectory)
    # These existing SDK facade projects have no test implementations; all other modules must execute tests.
    $emptyFacade = $projectName -in @('Sdk.Client.L0Tests', 'Sdk.Gateway.L0Tests', 'Sdk.Runtime.L0Tests')
    if ($emptyFacade) { $arguments += @('--ignore-exit-code', '8') }
    if ($CollectCoverage) { $arguments += @('--coverlet', '--coverlet-output-format', 'cobertura') }
    if ($TestLevels) {
        $filter = ($TestLevels | ForEach-Object { "FullyQualifiedName~.$($_)." }) -join '|'
        $arguments += @('--filter', $filter)
    }
    if ($AdditionalArguments) { $arguments += $AdditionalArguments }
    if (-not $Quiet) { Write-Host "Executing tests: $ProjectPath" -ForegroundColor Cyan }
    Invoke-RepositoryProcess -FilePath dotnet -Arguments $arguments -ErrorMessage "Failed to run tests for $ProjectPath." | Out-Host
    $executed = Get-TestExecutionCount -ResultsDirectory $moduleDirectory
    if ($executed -lt 1 -and -not $emptyFacade) { throw "No tests executed for '$ProjectPath'. Reports: $moduleDirectory" }
    if (@(Get-ChildItem -LiteralPath $moduleDirectory -Recurse -Filter '*.trx' -File).Count -ne 1) {
        throw "Expected one TRX report for '$ProjectPath'. Reports: $moduleDirectory"
    }
}

function Invoke-SolutionTests {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [string]$Configuration = 'Release',
        [string]$ResultsRoot,
        [switch]$CollectCoverage,
        [string[]]$TestLevels,
        [string[]]$AdditionalArguments,
        [switch]$Quiet
    )

    $resolved = Resolve-Path -LiteralPath $SolutionPath
    $projects = @(Get-TestProjects -SolutionPath $resolved.Path)
    if ($TestLevels) {
        $levels = ($TestLevels | ForEach-Object { [regex]::Escape($_) }) -join '|'
        $projects = @($projects | Where-Object { $_ -match "\.($levels)\.csproj$" })
    }
    if ($projects.Count -eq 0) { throw "No tests executed: no projects match levels '$($TestLevels -join ',')'." }
    if (-not $ResultsRoot) { $ResultsRoot = Join-Path (Get-RepositoryRoot) '.scratchpad/coverage-test-results' }
    $resultsDirectory = New-AutomationRunDirectory -Root $ResultsRoot
    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($project in $projects) {
        try {
            Invoke-TestModule -ProjectPath $project -ResultsDirectory $resultsDirectory -Configuration $Configuration `
                -CollectCoverage:$CollectCoverage -TestLevels $TestLevels -AdditionalArguments $AdditionalArguments -Quiet:$Quiet
        }
        catch {
            $failures.Add($_.Exception.Message)
        }
    }
    if ($failures.Count -gt 0) { throw "Test modules failed. Reports: $resultsDirectory`n$($failures -join "`n")" }
    if ((Get-TestExecutionCount -ResultsDirectory $resultsDirectory) -lt 1) {
        throw "No tests executed for '$($resolved.Path)'. Reports: $resultsDirectory"
    }
    return [pscustomobject]@{ SolutionPath = $resolved.Path; ResultsDirectory = $resultsDirectory }
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
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "Failed to generate solution '$outputFullPath' using SlnGen." -SuppressCommandEcho | Out-Host
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
    $cacheRoot = Join-Path (Split-Path -Parent $resolvedSolution.Path) '.scratchpad/cleanup-caches'
    $cacheDirectory = New-AutomationRunDirectory -Root $cacheRoot
    Write-Host "Cleanup cache: $cacheDirectory" -ForegroundColor DarkGray
    $sdkVersion = @(Invoke-RepositoryProcess -FilePath dotnet -Arguments @('--version') -SuppressCommandEcho)
    if ($sdkVersion.Count -ne 1 -or $sdkVersion[0] -notmatch '^\d+\.\d+\.\d+(?:-[\w.-]+)?$') {
        throw 'Expected one SDK version selected by global.json.'
    }
    $cleanupArguments = @('tool','run','jb','cleanupcode', "--profile=$Profile", "--settings=$($resolvedSettings.Path)",
        "--caches-home=$cacheDirectory", "--dotnetcoresdk=$($sdkVersion[0])")

    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        $cleanupArguments += "--include=$($IncludePaths -join ';')"
    }

    if ($ExcludePaths -and $ExcludePaths.Count -gt 0) {
        $cleanupArguments += "--exclude=$($ExcludePaths -join ';')"
    }

    $cleanupArguments += $resolvedSolution.Path
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $cleanupArguments -ErrorMessage "ReSharper cleanup failed for $($resolvedSolution.Path)." -SuppressCommandEcho | Out-Host
}

function Get-SolutionProjectPaths {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath
    )

    # Generated legacy solutions share their membership with the canonical .slnx.
    $canonicalPath = [System.IO.Path]::ChangeExtension($SolutionPath, '.slnx')
    [xml]$solution = Get-Content -LiteralPath $canonicalPath -Raw
    $solutionDir = Split-Path -Parent (Resolve-Path -LiteralPath $canonicalPath).Path
    foreach ($project in $solution.SelectNodes('//Project')) {
        $relativePath = $project.GetAttribute('Path') -replace '\\', '/'
        (Resolve-Path -LiteralPath (Join-Path $solutionDir $relativePath)).Path
    }
}

function Get-TestProjects {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$SolutionPath)

    Get-SolutionProjectPaths -SolutionPath $SolutionPath | Where-Object { $_ -like '*Tests.csproj' }
}

function Read-MutationReport {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ReportPath)

    $report = Get-Content -LiteralPath $ReportPath -Raw -ErrorAction Stop | ConvertFrom-Json -AsHashtable
    if ($report -isnot [System.Collections.IDictionary] -or $report['files'] -isnot [System.Collections.IDictionary]) {
        throw "Stryker report has no valid files collection: $ReportPath"
    }
    $completedStatuses = @('Killed', 'Survived', 'NoCoverage', 'CompileError', 'RuntimeError', 'Timeout', 'Ignored')
    foreach ($fileResult in $report['files'].Values) {
        if ($fileResult -isnot [System.Collections.IDictionary] -or $fileResult['mutants'] -isnot [array]) {
            throw "Stryker report has no valid mutants collection: $ReportPath"
        }
        foreach ($mutant in $fileResult['mutants']) {
            if ($mutant -isnot [System.Collections.IDictionary] -or $mutant['status'] -isnot [string] -or $mutant['status'] -cnotin $completedStatuses) {
                throw "Stryker report contains an incomplete or invalid mutant result: $ReportPath"
            }
        }
    }
    return $report
}

function Get-MutationReportPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$OutputPath)

    $reports = @(Get-ChildItem -LiteralPath $OutputPath -Recurse -Filter 'mutation-report.json' -File -ErrorAction Stop)
    if ($reports.Count -ne 1) {
        throw "Expected one mutation-report.json; found $($reports.Count). Reports: $OutputPath"
    }
    Read-MutationReport -ReportPath $reports[0].FullName | Out-Null
    return $reports[0].FullName
}

function Get-StrykerBreakThreshold {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    $config = Get-Content -LiteralPath $ConfigPath -Raw -ErrorAction Stop | ConvertFrom-Json -AsHashtable
    if ($config -isnot [System.Collections.IDictionary]) {
        throw "Stryker configuration is not a JSON object: $ConfigPath"
    }

    $strykerConfig = $config['stryker-config']
    $thresholds = if ($strykerConfig -is [System.Collections.IDictionary]) { $strykerConfig['thresholds'] } else { $null }
    $breakValue = if ($thresholds -is [System.Collections.IDictionary]) { $thresholds['break'] } else { $null }
    if ($null -eq $breakValue) {
        return [double]0
    }

    try {
        return [double]$breakValue
    }
    catch {
        throw "Stryker break threshold is not numeric in configuration: $ConfigPath"
    }
}

function Get-MutationReportMetrics {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ReportPath)

    $report = Read-MutationReport -ReportPath $ReportPath
    $mutants = @(
        foreach ($fileResult in $report['files'].Values) {
            @($fileResult['mutants'])
        }
    )
    $statuses = @('Killed', 'Timeout', 'Survived', 'NoCoverage', 'RuntimeError', 'CompileError', 'Ignored', 'Pending')
    $counts = @{}
    foreach ($status in $statuses) {
        $counts[$status] = @($mutants | Where-Object { $_['status'] -eq $status }).Count
    }

    $detected = $counts['Killed'] + $counts['Timeout']
    $valid = $detected + $counts['Survived'] + $counts['NoCoverage']
    $rawScore = if ($valid -gt 0) { $detected / $valid * 100 } else { $null }
    $score = if ($null -ne $rawScore) { [Math]::Round($rawScore, 2) } else { $null }
    return [pscustomobject]@{
        Score = $score
        RawScore = $rawScore
        Valid = $valid
        Detected = $detected
        Killed = $counts['Killed']
        Timeout = $counts['Timeout']
        Survived = $counts['Survived']
        NoCoverage = $counts['NoCoverage']
        RuntimeError = $counts['RuntimeError']
        CompileError = $counts['CompileError']
        Ignored = $counts['Ignored']
        Pending = $counts['Pending']
    }
}

function Get-MutationProjectStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ExecutionStatus,
        [Parameter(Mandatory)][bool]$ReportValid,
        [Nullable[double]]$Score,
        [double]$BreakThreshold
    )

    if ($ExecutionStatus -eq 'Skipped') { return 'SKIPPED' }
    if (-not $ReportValid) { return 'NO_REPORT' }
    if ($null -eq $Score) { return 'NO_SCORE' }
    if ($BreakThreshold -gt 0 -and $Score -lt $BreakThreshold) { return 'BELOW_BREAK' }
    return 'AT_OR_ABOVE_BREAK'
}

function Get-MutationProjectSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [double]$BreakThreshold
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension([string]$ProjectResult.Project)
    $reportValid = $false
    $reportError = $ProjectResult.ReportError
    $metrics = $null
    if ($ProjectResult.ReportPath -and (Test-Path -LiteralPath $ProjectResult.ReportPath -PathType Leaf)) {
        try {
            $metrics = Get-MutationReportMetrics -ReportPath $ProjectResult.ReportPath
            $reportValid = $true
        }
        catch {
            $reportError = $_.Exception.Message
        }
    }

    $score = if ($metrics) { $metrics.Score } else { $null }
    $rawScore = if ($metrics) { $metrics.RawScore } else { $null }
    return [pscustomobject]@{
        Project = $projectName
        ExecutionStatus = switch ($ProjectResult.Status) {
            'Failed' { 'FAILED' }
            'ThresholdFailed' { 'COMPLETED_WITH_THRESHOLD_FAILURE' }
            default { 'COMPLETED' }
        }
        Status = Get-MutationProjectStatus -ExecutionStatus $ProjectResult.Status -ReportValid $reportValid -Score $rawScore -BreakThreshold $BreakThreshold
        Score = $score
        RawScore = $rawScore
        ValidMutants = if ($metrics) { $metrics.Valid } else { 0 }
        DetectedMutants = if ($metrics) { $metrics.Detected } else { 0 }
        ReportPath = $ProjectResult.ReportPath
        ReportValid = $reportValid
        Error = $ProjectResult.Error
        ReportError = $reportError
    }
}

function Get-MutationProjectMessage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Project,
        [Parameter(Mandatory)][string]$Fallback
    )

    if ($Project.Error) { return $Project.Error }
    if ($Project.ReportError) { return $Project.ReportError }
    return $Fallback
}

function Get-GitHubMutationSummaryDetailLines {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Summary)

    $detailLines = @()
    if ($Summary.BelowBreakThresholdCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects below the advisory threshold'
        foreach ($project in $Summary.BelowBreakThresholdProjects) {
            $detailLines += "- $($project.Project): $($project.Score)%"
        }
    }
    if ($Summary.NoScoreProjectCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects without a mutation score'
        foreach ($project in $Summary.NoScoreProjects) {
            $detailLines += "- $($project.Project): $(Get-MutationProjectMessage -Project $project -Fallback 'No valid mutants were scored')"
        }
    }
    if ($Summary.FailedProjectCount -gt 0) {
        $detailLines += ''
        $detailLines += '### Projects with execution failures'
        foreach ($project in $Summary.FailedProjects) {
            $detailLines += "- $($project.Project): $(Get-MutationProjectMessage -Project $project -Fallback $project.Status)"
        }
    }
    return $detailLines
}

function Write-GitHubMutationSummary {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Summary)

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) { return }

    $heading = switch ($Summary.MutationResult) {
        'FAIL' { 'failed'; break }
        'WARN' { 'completed with warnings'; break }
        default { 'completed' }
    }
    $summaryLines = @(
        "## Mutation testing: $heading"
        ''
        "- Execution: **$($Summary.ExecutionStatus)**"
        "- Result: **$($Summary.MutationResult)**"
        "- Complete reports: **$($Summary.CompleteReportCount)/$($Summary.ProjectCount)**"
        "- Skipped projects: **$($Summary.SkippedProjectCount)**"
        "- Threshold-only exits: **$($Summary.ThresholdFailureCount)**"
        "- Scored projects: **$($Summary.ScoredProjectCount)**"
        "- Unscored projects: **$($Summary.NoScoreProjectCount)**"
        "- Below break threshold ($($Summary.BreakThreshold)%): **$($Summary.BelowBreakThresholdCount)**"
    )
    $summaryLines += Get-GitHubMutationSummaryDetailLines -Summary $Summary
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summaryLines -Encoding utf8
}

function Show-MutationRunSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][object[]]$ProjectResults,
        [double]$BreakThreshold = 0
    )

    $projectSummaries = @($ProjectResults | ForEach-Object {
        Get-MutationProjectSummary -ProjectResult $_ -BreakThreshold $BreakThreshold
    })

    $failedProjects = @($projectSummaries | Where-Object { $_.ExecutionStatus -eq 'FAILED' })
    $thresholdFailures = @($projectSummaries | Where-Object { $_.ExecutionStatus -eq 'COMPLETED_WITH_THRESHOLD_FAILURE' })
    $belowBreak = @($projectSummaries | Where-Object { $_.Status -eq 'BELOW_BREAK' })
    $noScoreProjects = @($projectSummaries | Where-Object { $_.Status -eq 'NO_SCORE' })
    $skippedProjects = @($projectSummaries | Where-Object { $_.Status -eq 'SKIPPED' })
    $reportEligibleProjects = @($projectSummaries | Where-Object { $_.Status -ne 'SKIPPED' })
    $completeReports = @($projectSummaries | Where-Object { $_.ReportValid })
    $scoredReports = @($projectSummaries | Where-Object { $null -ne $_.Score })
    $hasWarnings = $thresholdFailures.Count -gt 0 -or $belowBreak.Count -gt 0 -or $noScoreProjects.Count -gt 0
    $executionStatus = if ($failedProjects.Count -gt 0) { 'FAILED' } elseif ($thresholdFailures.Count -gt 0 -or $noScoreProjects.Count -gt 0) { 'COMPLETED_WITH_WARNINGS' } else { 'COMPLETED' }
    $mutationResult = if ($executionStatus -eq 'FAILED') { 'FAIL' } elseif ($hasWarnings) { 'WARN' } else { 'PASS' }

    $summary = [ordered]@{
        SchemaVersion = 1
        ExecutionStatus = $executionStatus
        MutationResult = $mutationResult
        BreakThreshold = $BreakThreshold
        ProjectCount = $reportEligibleProjects.Count
        TargetProjectCount = $ProjectResults.Count
        SkippedProjectCount = $skippedProjects.Count
        CompleteReportCount = $completeReports.Count
        ScoredProjectCount = $scoredReports.Count
        BelowBreakThresholdCount = $belowBreak.Count
        FailedProjectCount = $failedProjects.Count
        ThresholdFailureCount = $thresholdFailures.Count
        NoScoreProjectCount = $noScoreProjects.Count
        BelowBreakThresholdProjects = @($belowBreak | Select-Object Project, Score)
        FailedProjects = @($failedProjects | Select-Object Project, Status, ReportValid, Error, ReportError)
        ThresholdFailureProjects = @($thresholdFailures | Select-Object Project, Score, RawScore)
        NoScoreProjects = @($noScoreProjects | Select-Object Project, Error, ReportError)
        Projects = $projectSummaries
    }
    $summaryPath = Join-Path $OutputPath 'mutation-summary.json'
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath

    Write-Host "MUTATION_EXECUTION: $executionStatus"
    Write-Host "MUTATION_RESULT: $mutationResult"
    Write-Host "MUTATION_REPORTS: $($completeReports.Count)/$($reportEligibleProjects.Count) complete (skipped: $($skippedProjects.Count))"
    Write-Host "MUTATION_SCORED_PROJECTS: $($scoredReports.Count)"
    Write-Host "MUTATION_UNSCORED_PROJECTS: $($noScoreProjects.Count)"
    Write-Host "MUTATION_BELOW_BREAK: $($belowBreak.Count) (threshold $BreakThreshold%)"
    Write-Host "MUTATION_THRESHOLD_FAILURES: $($thresholdFailures.Count)"
    if ($belowBreak.Count -gt 0) {
        $warningMessage = "Mutation analysis completed, but $($belowBreak.Count) project(s) scored below the configured break threshold."
        Write-Warning "$warningMessage Threshold: $BreakThreshold%."
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::warning title=Mutation score warning::$warningMessage"
        }
    }
    if ($noScoreProjects.Count -gt 0) {
        $warningMessage = "Mutation analysis completed, but $($noScoreProjects.Count) project(s) produced no mutation score."
        Write-Warning $warningMessage
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::warning title=Mutation score unavailable::$warningMessage"
        }
    }
    if ($failedProjects.Count -gt 0) {
        Write-Warning "Mutation analysis did not complete successfully for $($failedProjects.Count) project(s)."
    }

    Write-GitHubMutationSummary -Summary ([pscustomobject]$summary)

    return [pscustomobject]$summary
}

function Invoke-StrykerMutationTestPerProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string[]]$TestProjects,
        [string]$Configuration = 'Release',
        [switch]$ReportOnly,
        [switch]$ApplyThresholdAfterReport
    )

    $resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedProject)
    $projectOutputPath = New-AutomationRunDirectory -Root ([System.IO.Path]::GetFullPath((Join-Path $OutputPath $projectName)))
    $repoRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedProject)
    $configPath = Join-Path $repoRoot 'stryker-config.json'
    
    Write-Host "  Running Stryker for project: $projectName" -ForegroundColor ([ConsoleColor]::Cyan)
    # MTP executes xUnit v3 test applications; complete reports remain mandatory after any tool exit status.
    $arguments = @('stryker', '--project', [System.IO.Path]::GetFileName($resolvedProject), '--config-file', $configPath, '--configuration', $Configuration, '--output', $projectOutputPath, '--test-runner', 'mtp', '--disable-bail', '--break-on-initial-test-failure')
    foreach ($testProject in $TestProjects) {
        $arguments += @('--test-project', $testProject)
    }
    if ($ReportOnly -or $ApplyThresholdAfterReport) {
        # Keep full-solution/report-only exits focused on execution/report generation; apply thresholds after validating the report.
        $arguments += @('--break-at', '0')
    }
    # MTP reuses test servers; serialize mutants to isolate process-global state and integration fixtures.
    $arguments += @('--concurrency', '1')
    # Stryker's multiple-test-project mode runs from the source project directory.
    Push-Location -LiteralPath (Split-Path -Parent $resolvedProject)
    try {
        # Use the SDK selected by global.json instead of an older Visual Studio MSBuild.
        $msBuildDirectories = @(Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('msbuild', $resolvedProject, '-getProperty:MSBuildBinPath', '-nologo') -SuppressCommandEcho)
        if ($msBuildDirectories.Count -ne 1 -or [string]::IsNullOrWhiteSpace($msBuildDirectories[0])) {
            throw 'Expected one MSBuildBinPath value from the selected SDK.'
        }
        $msBuildPath = Join-Path $msBuildDirectories[0] 'MSBuild.dll'
        if (-not (Test-Path -LiteralPath $msBuildPath -PathType Leaf)) { throw "Selected SDK MSBuild.dll was not found: $msBuildPath" }
        $arguments += @('--msbuild-path', $msBuildPath)
        $processError = $null
        try {
            Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $arguments -ErrorMessage "Stryker mutation testing failed for project $projectName. Reports: $projectOutputPath" -SuppressCommandEcho | Out-Host
        }
        catch {
            $processError = $_
        }
        try {
            $reportPath = Get-MutationReportPath -OutputPath $projectOutputPath
        }
        catch {
            if ($processError) {
                $processError.Exception.Data['ReportError'] = $_.Exception.Message
                throw $processError
            }
            throw
        }
        if ($processError) {
            $processError.Exception.Data['ReportPath'] = $reportPath
            throw $processError
        }
    }
    catch {
        $_.Exception.Data['OutputPath'] = $projectOutputPath
        throw
    }
    finally {
        Pop-Location
    }
    
    return $projectOutputPath
}

function Get-MutationTargets {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$SolutionPath)

    $projects = @(Get-SolutionProjectPaths -SolutionPath $SolutionPath)
    $sourceRoot = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $SolutionPath) 'src')) + [System.IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $targets = @{}
    foreach ($project in $projects) {
        if ($project.StartsWith($sourceRoot, $comparison)) {
            $targets[$project] = [pscustomobject]@{ Project = $project; Tests = @(); HasSource = $false }
        }
    }
    foreach ($testProject in @($projects | Where-Object { $_ -like '*Tests.csproj' })) {
        [xml]$definition = Get-Content -LiteralPath $testProject -Raw
        foreach ($reference in $definition.SelectNodes('//ProjectReference')) {
            $relativePath = $reference.GetAttribute('Include').Replace('\', '/')
            $path = (Resolve-Path -LiteralPath (Join-Path (Split-Path -Parent $testProject) $relativePath)).Path
            if ($targets.ContainsKey($path)) { $targets[$path].Tests += $testProject }
        }
    }
    foreach ($target in $targets.Values) {
        $source = @(Get-ChildItem -LiteralPath (Split-Path -Parent $target.Project) -Recurse -File -Filter '*.cs' -ErrorAction Stop |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Select-Object -First 1)
        $target.HasSource = $source.Count -gt 0
        $target.Tests = @($target.Tests | Sort-Object -Unique)
    }
    return @($targets.Values | Sort-Object Project)
}

function Set-MutationResultMetrics {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][string]$ReportPath
    )

    $metrics = Get-MutationReportMetrics -ReportPath $ReportPath
    $ProjectResult.MutationScore = $metrics.Score
    $ProjectResult.RawMutationScore = $metrics.RawScore
    $ProjectResult.ValidMutants = $metrics.Valid
    $ProjectResult.DetectedMutants = $metrics.Detected
}

function Set-MutationFailureResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][object]$Failure
    )

    $ProjectResult.Output = $Failure.Exception.Data['OutputPath']
    $ProjectResult.ReportPath = $Failure.Exception.Data['ReportPath']
    $ProjectResult.Status = 'Failed'
    $ProjectResult.Error = $Failure.Exception.Message
    $ProjectResult.ReportError = $Failure.Exception.Data['ReportError']
    if ($ProjectResult.ReportPath -and (Test-Path -LiteralPath $ProjectResult.ReportPath -PathType Leaf)) {
        try {
            Set-MutationResultMetrics -ProjectResult $ProjectResult -ReportPath $ProjectResult.ReportPath
        }
        catch {
            if (-not $ProjectResult.ReportError) { $ProjectResult.ReportError = $_.Exception.Message }
        }
    }
}

function Invoke-MutationTarget {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Target,
        [Parameter(Mandatory)][object]$ProjectResult,
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string]$Configuration,
        [double]$BreakThreshold,
        [switch]$ReportOnly
    )

    if (-not $Target.HasSource) {
        $ProjectResult.Status = 'Skipped'
        $ProjectResult.Success = $true
        $ProjectResult.Reason = 'No authored C# source'
        return
    }

    try {
        if ($Target.Tests.Count -eq 0) { throw "Authored source project has no declared test mapping: $($Target.Project)" }
        $projectOutput = Invoke-StrykerMutationTestPerProject -ProjectPath $Target.Project -TestProjects $Target.Tests -OutputPath $OutputPath -Configuration $Configuration -ReportOnly:$ReportOnly -ApplyThresholdAfterReport
        $reportPath = Get-MutationReportPath -OutputPath $projectOutput
        Set-MutationResultMetrics -ProjectResult $ProjectResult -ReportPath $reportPath
        $ProjectResult.Output = $projectOutput
        $ProjectResult.ReportPath = $reportPath
        if (-not $ReportOnly -and $BreakThreshold -gt 0 -and $null -ne $ProjectResult.RawMutationScore -and
            $ProjectResult.RawMutationScore -lt $BreakThreshold) {
            $ProjectResult.Status = 'ThresholdFailed'
            $ProjectResult.ThresholdFailure = $true
            $ProjectResult.Success = $false
            $ProjectResult.Error = "Mutation score $($ProjectResult.RawMutationScore)% is below the configured break threshold $BreakThreshold%."
            Write-Warning "  ! Threshold not met: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project)) - score $($ProjectResult.RawMutationScore)% (threshold $BreakThreshold%)"
            return
        }
        $ProjectResult.Success = $true
        $ProjectResult.Status = 'Completed'
        Write-Host "  ✓ Completed: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project))" -ForegroundColor ([ConsoleColor]::Green)
    }
    catch {
        Write-Warning "  ✗ Failed: $([System.IO.Path]::GetFileNameWithoutExtension($Target.Project)) - $($_.Exception.Message)"
        Set-MutationFailureResult -ProjectResult $ProjectResult -Failure $_
    }
}

function Invoke-StrykerMutationTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SolutionPath,
        [Parameter(Mandatory)][string]$OutputPath,
        [string]$Configuration = 'Release',
        [switch]$ReportOnly
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    if (-not (Test-Path -LiteralPath $outputFullPath)) {
        $null = New-Item -ItemType Directory -Path $outputFullPath -Force
    }

    $repoRoot = Get-RepositoryRoot -StartPath (Split-Path -Parent $resolvedSolution.Path)
    $configPath = Join-Path $repoRoot 'stryker-config.json'
    $breakThreshold = Get-StrykerBreakThreshold -ConfigPath $configPath
    $targets = @(Get-MutationTargets -SolutionPath $resolvedSolution.Path)
    if (@($targets | Where-Object HasSource).Count -eq 0) {
        throw "No source projects with tests were found in '$SolutionPath'."
    }

    $projectResults = @($targets | ForEach-Object {
        @{ Project = $_.Project; Output = $null; ReportPath = $null; Success = $false;
           Status = 'Pending'; Error = $null; ReportError = $null; Reason = $null;
           MutationScore = $null; RawMutationScore = $null; ValidMutants = 0; DetectedMutants = 0;
           ThresholdFailure = $false }
    })
    $manifestPath = Join-Path $outputFullPath 'project-results.json'
    $manifest = @{ Scope = 'Solution'; Solution = $resolvedSolution.Path; Projects = $projectResults }
    ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath
    for ($index = 0; $index -lt $targets.Count; $index++) {
        $target = $targets[$index]
        $result = $projectResults[$index]
        Invoke-MutationTarget -Target $target -ProjectResult $result -OutputPath $outputFullPath -Configuration $Configuration -BreakThreshold $breakThreshold -ReportOnly:$ReportOnly
        ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath
        Write-Host
    }
    ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath

    $null = Show-MutationRunSummary -OutputPath $outputFullPath -ProjectResults $projectResults -BreakThreshold $breakThreshold

    # Check if any projects failed
    $failedProjects = @($projectResults | Where-Object { -not $_.Success })
    if ($failedProjects.Count -gt 0) {
        $thresholdFailures = @($failedProjects | Where-Object { $_.Status -eq 'ThresholdFailed' })
        $executionFailures = @($failedProjects | Where-Object { $_.Status -ne 'ThresholdFailed' })
        if ($thresholdFailures.Count -gt 0) {
            Write-Warning "$($thresholdFailures.Count) project(s) completed but did not meet the configured mutation score threshold."
            foreach ($failed in $thresholdFailures) {
                Write-Host "  - $([System.IO.Path]::GetFileNameWithoutExtension($failed.Project)): $($failed.MutationScore)%" -ForegroundColor ([ConsoleColor]::Yellow)
            }
        }
        if ($executionFailures.Count -gt 0) {
            Write-Warning "$($executionFailures.Count) project(s) failed mutation execution."
            foreach ($failed in $executionFailures) {
                Write-Host "  - $([System.IO.Path]::GetFileNameWithoutExtension($failed.Project)): $($failed.Error)" -ForegroundColor ([ConsoleColor]::Yellow)
            }
        }
        throw "Stryker mutation testing failed for $($failedProjects.Count) project(s). Reports: $outputFullPath"
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
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
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
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $solutionPath -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
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
    Invoke-SolutionBuild -SolutionPath $mississippi -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: Mississippi solution built with zero warnings' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host "[5/5] Building samples.slnx with ZERO TOLERANCE for warnings..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Configuration: $Configuration"
    Write-Host 'Build flags: --no-restore --no-incremental --warnaserror'
    Invoke-SolutionBuild -SolutionPath $samples -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
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
        [string[]]$TestLevels = @('L0Tests', 'L1Tests'),
        [switch]$PassThru
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
    Write-Host 'Test flags: --no-restore --coverlet --report-xunit-trx'
    $testResult = (Invoke-SolutionTests -SolutionPath $solutionPath -Configuration $Configuration -ResultsRoot $resultsRoot -CollectCoverage -TestLevels $TestLevels -Quiet | Select-Object -Last 1)
    $runDirectory = $testResult.ResultsDirectory
    Write-Host "Results directory: $runDirectory"
    Write-Host 'Logger: xUnit TRX reports, one per test module'

    $coverageFiles = @(Get-ChildItem -Path $runDirectory -Recurse -Filter '*cobertura*.xml' -ErrorAction SilentlyContinue)
    if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
        throw "Unit tests completed but no coverage reports were produced in '$runDirectory'."
    }

    $reportsArg = ($coverageFiles | ForEach-Object { '"{0}"' -f $_.FullName }) -join ';'
    $mergedOutputDir = Join-Path $runDirectory 'coverage-merged'
    if (Test-Path -LiteralPath $mergedOutputDir) {
        Remove-Item -LiteralPath $mergedOutputDir -Recurse -Force
    }

    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments @('tool','run','reportgenerator', "-reports:$reportsArg", "-targetdir:$mergedOutputDir", '-reporttypes:Cobertura') -ErrorMessage 'ReportGenerator failed while merging coverage reports.' -SuppressCommandEcho | Out-Host

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
    $resultsFile = Join-Path $runDirectory '*/test_results*.trx'
    Write-Host "All tests passed | Results saved to: $resultsFile"
    Write-Host 'Coverage report ready for summarize-coverage-gaps.ps1' -ForegroundColor ([ConsoleColor]::Green)
    if ($PassThru) {
        return [pscustomobject][ordered]@{
            ResultsDirectory = $runDirectory
            CoverageReportPath = $finalCoveragePath
            TestLevels = @($TestLevels)
            Configuration = $Configuration
        }
    }
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
    Write-Host 'Logger: TRX format (unique test_results prefix per project/framework)'
    Write-Host 'NOTE: Sample tests are for demonstration purposes only'

    Write-Host 'SUCCESS: All sample tests passed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION UNIT TESTING COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    $resultsFile = Join-Path $testResult.ResultsDirectory 'test_results*.trx'
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
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string]$Configuration = 'Release',
        [switch]$ReportOnly
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
    Invoke-SolutionBuild -SolutionPath $generatedSln -Configuration $Configuration -NoRestore -NoIncremental -WarnAsError -Quiet | Out-Host
    Write-Host 'SUCCESS: NuGet packages restored for mutation testing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[4/4] Executing Stryker.NET mutation testing...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'Stryker will inject mutations into source code and verify tests detect them'
    Write-Host 'This process validates the effectiveness of the test suite'
    Write-Host "Target solution: $generatedSln"

    $outputDirectory = New-AutomationRunDirectory -Root $mutationRoot
    Invoke-StrykerMutationTest -SolutionPath $generatedSln -OutputPath $outputDirectory -Configuration $Configuration -ReportOnly:$ReportOnly | Out-Null

    $summaryPath = Join-Path $outputDirectory 'mutation-summary.json'
    $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
    if ($summary.MutationResult -eq 'WARN') {
        Write-Host 'COMPLETED WITH WARNINGS: Mutation analysis finished; score thresholds were not met for some projects.' -ForegroundColor ([ConsoleColor]::Yellow)
    }
    else {
        Write-Host 'SUCCESS: Mutation analysis completed.' -ForegroundColor ([ConsoleColor]::Green)
    }
    Write-Host "Reports: $($summary.CompleteReportCount)/$($summary.ProjectCount) complete (skipped: $($summary.SkippedProjectCount)) | Below break threshold: $($summary.BelowBreakThresholdCount)"
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION MUTATION ANALYSIS COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
}

function Invoke-SolutionsPipeline {
    [CmdletBinding()]
    param(
        [string]$Configuration = 'Release',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [switch]$SkipCleanup,
        [switch]$IncludeMutation,
        [string]$LeaseDirectory
    )

    $executionLease = Enter-RepositoryExecutionLease -RepoRoot $RepoRoot -OperationId "pipeline-$([guid]::NewGuid().ToString('N'))" -LeaseDirectory $LeaseDirectory
    try {
    $automationScriptsRoot = Join-Path (Join-Path (Join-Path $RepoRoot 'eng') 'src') 'agent-scripts'
    $coverageScript = Join-Path $automationScriptsRoot 'summarize-coverage-gaps.ps1'
    $mutationSummaryScript = Join-Path $automationScriptsRoot 'summarize-mutation-survivors.ps1'

    Write-AutomationBanner -Message '=== STARTING COMPLETE BUILD AND TEST PIPELINE ===' -ForegroundColor ([ConsoleColor]::Magenta) -InsertBlankLine
    Write-Host 'Pipeline will execute Mississippi solution followed by Sample solution'
    Write-Host 'Each step must complete successfully before proceeding to the next'
    if (-not $IncludeMutation) {
        Write-Host 'Mutation testing skipped (use -IncludeMutation to enable)'
    }
    if ($SkipCleanup) {
        Write-Host 'Cleanup skipped; validation results are provisional for this intermediate run.' -ForegroundColor ([ConsoleColor]::Yellow)
    }
    Write-Host

    $step = 1

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Mississippi Solution' -StepNumber ($step++) -Action { Invoke-MississippiSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Mississippi Code Style' -StepNumber ($step++) -Action { Invoke-MississippiSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }
    $mississippiTestResult = Invoke-AutomationStep -Name 'Run Mississippi Unit Tests' -StepNumber ($step++) -Action { Invoke-MississippiSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot -PassThru } -SilentSuccess
    if ($null -eq $mississippiTestResult -or [string]::IsNullOrWhiteSpace([string]$mississippiTestResult.CoverageReportPath)) {
        throw 'Mississippi unit-test operation did not return an aggregated coverage report path.'
    }
    Invoke-AutomationStep -Name 'Summarize Coverage Gaps' -StepNumber ($step++) -Action { Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments @('-NoProfile', '-File', $coverageScript, '-CoverageReportPath', $mississippiTestResult.CoverageReportPath, '-EmitTasks') | Out-Host }
    if ($IncludeMutation) {
        Invoke-AutomationStep -Name 'Run and Summarize Mississippi Mutation Tests' -StepNumber ($step++) -Action { Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments @('-NoProfile', '-File', $mutationSummaryScript, '-Configuration', $Configuration, '-GenerateTasks') | Out-Host }
    }

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION PIPELINE ===' -ForegroundColor ([ConsoleColor]::Cyan)
    Invoke-AutomationStep -Name 'Build Sample Solution' -StepNumber ($step++) -Action { Invoke-SampleSolutionBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess
    if (-not $SkipCleanup) {
        Invoke-AutomationStep -Name 'Cleanup Sample Code Style' -StepNumber ($step++) -Action { Invoke-SampleSolutionCleanup -RepoRoot $RepoRoot } -SilentSuccess
    }
    Invoke-AutomationStep -Name 'Run Sample Unit Tests' -StepNumber ($step++) -Action { Invoke-SampleSolutionUnitTests -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess

    Invoke-AutomationStep -Name 'Final Build with Warnings as Errors' -StepNumber ($step++) -Action { Invoke-FinalSolutionsBuild -Configuration $Configuration -RepoRoot $RepoRoot } -SilentSuccess

    Write-Host '=== PIPELINE COMPLETED SUCCESSFULLY ===' -ForegroundColor ([ConsoleColor]::Green)
    if ($SkipCleanup) {
        Write-Host 'Local build, test, coverage and final-build checks completed; cleanup was intentionally skipped, so this is not final handoff evidence.'
    }
    else {
        Write-Host 'Local build, test, coverage, cleanup and final-build checks completed. Deployment, browser and external CI checks are outside this command.'
    }
    }
    finally {
        Exit-RepositoryExecutionLease -Lease $executionLease
    }
}

function Get-SpringTestResult {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    [xml]$report = Get-Content -LiteralPath $Path -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ($report.TestRun.ResultSummary.outcome -ne 'Completed' -or
        [int]$counters.total -le 0 -or [int]$counters.passed -ne [int]$counters.total -or
        [int]$counters.executed -ne [int]$counters.total) {
        throw 'Spring validation requires a completed run with at least one test and every test passing.'
    }
    return [int]$counters.passed
}

function Install-SpringBrowser {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Project,
        [string]$Configuration = 'Release',
        [switch]$InstallBrowserDependencies
    )

    $targetDirectory = (Invoke-RepositoryProcess -FilePath dotnet -SuppressCommandEcho `
        -Arguments @('msbuild', $Project, "-property:Configuration=$Configuration", '-getProperty:TargetDir') | Out-String).Trim()
    $playwrightScript = Join-Path $targetDirectory 'playwright.ps1'
    if (-not (Test-Path -LiteralPath $playwrightScript -PathType Leaf)) { throw "Playwright installer missing: $playwrightScript" }
    $browserArguments = @('-NoProfile', '-File', $playwrightScript, 'install', 'chromium')
    if ($InstallBrowserDependencies) { $browserArguments += '--with-deps' }
    Invoke-RepositoryProcess -FilePath (Get-PowerShellExecutable) -Arguments $browserArguments
}

function Invoke-SpringValidation {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [ValidateSet('L2', 'L3')][string]$TestLevel = 'L3',
        [ValidateSet('Smoke', 'Full')][string]$Suite = 'Smoke',
        [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
        [switch]$Doctor,
        [switch]$InstallBrowserDependencies
    )

    $TestLevel = $TestLevel.ToUpperInvariant()
    $Suite = if ($Suite -eq 'Smoke') { 'Smoke' } else { 'Full' }
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
    $runDirectory = New-AutomationRunDirectory -Root (Join-Path $RepoRoot 'artifacts/spring') -Prefix "$TestLevel-$Suite-$([guid]::NewGuid().ToString('N'))"
    $project = Join-Path $RepoRoot "samples/Spring/Spring.${TestLevel}Tests/Spring.${TestLevel}Tests.csproj"
    $summary = [ordered]@{ schemaVersion = 1; status = 'FAIL'; phase = 'selection'; testLevel = $TestLevel; suite = $Suite; project = $project; passed = 0; artifacts = $runDirectory }
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $previousPath = $env:PATH
    $previousArtifacts = $env:SPRING_TEST_ARTIFACTS
    $previousBrowsers = $env:PLAYWRIGHT_BROWSERS_PATH
    Push-Location $RepoRoot
    try {
        if ($TestLevel -eq 'L2' -and $Suite -eq 'Smoke' -and -not $Doctor) {
            throw 'Spring smoke journeys are L3. Use -TestLevel L2 -Suite Full for API/infrastructure tests.'
        }
        $summary.phase = 'prerequisites'
        $summary.sdk = (Invoke-RepositoryProcess -FilePath dotnet -Arguments @('--version') `
            -ErrorMessage 'Install the SDK selected by global.json.' | Out-String).Trim()
        $dockerOs = (Invoke-RepositoryProcess -FilePath docker -Arguments @('info', '--format', '{{.OSType}}') `
            -ErrorMessage 'Start Docker with Linux containers and grant this user access to its daemon.' | Out-String).Trim()
        if ($dockerOs -ne 'linux') { throw 'Spring requires Docker running Linux containers.' }
        [xml]$packages = Get-Content -LiteralPath (Join-Path $RepoRoot 'Directory.Packages.props') -Raw
        $aspireVersion = ($packages.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Aspire.Hosting.AppHost').Version
        if ([string]::IsNullOrWhiteSpace($aspireVersion)) { throw 'Aspire.Hosting.AppHost version is missing from Directory.Packages.props.' }
        $summary.aspireVersion = $aspireVersion
        if ($Doctor) {
            $summary.status = 'READY'
            return
        }

        $summary.phase = 'tooling'
        $toolPath = Join-Path $RepoRoot "artifacts/tools/aspire-$aspireVersion"
        $aspire = Join-Path $toolPath $(if ($IsWindows) { 'aspire.cmd' } else { 'aspire' })
        if (-not (Test-Path -LiteralPath $aspire -PathType Leaf)) {
            Invoke-RepositoryProcess -FilePath dotnet -Arguments @('tool', 'install', 'Aspire.Cli', '--version', $aspireVersion, '--tool-path', $toolPath)
        }
        $env:PATH = $toolPath + [System.IO.Path]::PathSeparator + $previousPath
        $installedVersion = (Invoke-RepositoryProcess -FilePath $aspire -Arguments @('--version') | Out-String).Trim()
        if ($installedVersion.Split('+')[0] -ne $aspireVersion) { throw "Expected Aspire CLI $aspireVersion; found $installedVersion." }
        $summary.phase = 'restore'
        Invoke-RepositoryProcess -FilePath dotnet -Arguments @('restore', $project, '--locked-mode') |
            Tee-Object -FilePath (Join-Path $runDirectory 'restore.log') | Out-Host
        $summary.phase = 'build'
        Invoke-SolutionBuild -SolutionPath $project -Configuration $Configuration -WarnAsError -NoRestore |
            Tee-Object -FilePath (Join-Path $runDirectory 'build.log') | Out-Host
        if ($TestLevel -eq 'L3') {
            $summary.phase = 'browser'
            $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $RepoRoot 'artifacts/tools/playwright'
            Install-SpringBrowser -Project $project -Configuration $Configuration -InstallBrowserDependencies:$InstallBrowserDependencies
        }
        $env:SPRING_TEST_ARTIFACTS = $runDirectory
        $summary.phase = 'test'
        $testArguments = @('test', '--project', $project, '--configuration', $Configuration, '--no-build', '--no-restore',
            '--report-xunit-trx', '--report-xunit-trx-filename', 'spring.trx', '--results-directory', $runDirectory,
            '--minimum-expected-tests', '1', '--timeout', '15m')
        if ($Suite -eq 'Smoke') { $testArguments += @('--filter', 'Category=Smoke') }
        Invoke-RepositoryProcess -FilePath dotnet -Arguments $testArguments |
            Tee-Object -FilePath (Join-Path $runDirectory 'test.log') | Out-Host
        $summary.passed = Get-SpringTestResult -Path (Join-Path $runDirectory 'spring.trx')
        $summary.phase = 'complete'
        $summary.status = 'PASS'
    }
    catch {
        $summary.error = $_.Exception.Message
        throw
    }
    finally {
        $env:PATH = $previousPath
        $env:SPRING_TEST_ARTIFACTS = $previousArtifacts
        $env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowsers
        Pop-Location
        $summary.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)
        $summaryPath = Join-Path $runDirectory 'summary.json'
        $summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
        Write-Output "RESULT: $($summary.status) | LEVEL: $TestLevel | SUITE: $Suite | PHASE: $($summary.phase) | PASSED: $($summary.passed)"
        Write-Output "SUMMARY: $summaryPath"
    }
}

Export-ModuleMember -Function Get-RepositoryRoot, Get-RepositoryExecutionLeasePath, Enter-RepositoryExecutionLease, Exit-RepositoryExecutionLease, Write-AutomationBanner, Invoke-AutomationStep, Invoke-DotnetToolRestore, Invoke-SolutionRestore, Invoke-SolutionBuild, New-AutomationRunDirectory, Invoke-SolutionTests, Invoke-SlnGeneration, Invoke-ReSharperCleanup, Get-TestProjects, Read-MutationReport, Get-MutationReportPath, Invoke-StrykerMutationTestPerProject, Invoke-StrykerMutationTest, Invoke-MississippiSolutionBuild, Invoke-SampleSolutionBuild, Invoke-FinalSolutionsBuild, Invoke-MississippiSolutionUnitTests, Invoke-SampleSolutionUnitTests, Invoke-MississippiSolutionCleanup, Invoke-SampleSolutionCleanup, Invoke-MississippiSolutionMutationTests, Invoke-SolutionsPipeline, Invoke-SpringValidation








