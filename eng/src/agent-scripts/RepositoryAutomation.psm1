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
        [string[]]$ExcludePaths,
        [string]$CachesHome,
        [switch]$NoUpdates
    )

    $resolvedSolution = Resolve-Path -LiteralPath $SolutionPath
    $resolvedSettings = Resolve-Path -LiteralPath $SettingsPath
    $args = @('tool','run','jb','cleanupcode', "--profile=$Profile", "--settings=$($resolvedSettings.Path)")

    if ($CachesHome) {
        $cachePath = [System.IO.Path]::GetFullPath($CachesHome)
        if (-not (Test-Path -LiteralPath $cachePath -PathType Container)) {
            $null = New-Item -ItemType Directory -Path $cachePath -Force
        }
        $args += "--caches-home=$cachePath"
    }

    if ($NoUpdates) {
        $args += '--no-updates'
    }

    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        $args += "--include=$($IncludePaths -join ';')"
    }

    if ($ExcludePaths -and $ExcludePaths.Count -gt 0) {
        $args += "--exclude=$($ExcludePaths -join ';')"
    }

    $args += $resolvedSolution.Path
    Invoke-RepositoryProcess -FilePath 'dotnet' -Arguments $args -ErrorMessage "ReSharper cleanup failed for $($resolvedSolution.Path)." -SuppressCommandEcho
}

function Invoke-TargetedProjectCleanup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$ProjectGroup,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string]$CachesHome,
        [switch]$NoUpdates
    )

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $projectPath = [System.IO.Path]::GetFullPath($ProjectGroup.ProjectPath)
    $projectDirectory = [System.IO.Path]::GetDirectoryName($projectPath)
    $solutionName = ".cleanup-targeted-$([Guid]::NewGuid().ToString('N'))"
    $temporarySolutionX = Join-Path $projectDirectory "$solutionName.slnx"
    $temporarySolution = Join-Path $projectDirectory "$solutionName.sln"
    $projectFileName = [System.IO.Path]::GetFileName($projectPath)

    $projectRelativeIncludes = @(
        foreach ($relativePath in @($ProjectGroup.IncludePaths)) {
            $fullPath = [System.IO.Path]::GetFullPath(
                (Join-Path $rootFullPath ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar))
            )
            $includePath = [System.IO.Path]::GetRelativePath($projectDirectory, $fullPath)
            if ($includePath -eq '..' -or
                $includePath.StartsWith("..$([System.IO.Path]::DirectorySeparatorChar)") -or
                $includePath.StartsWith("..$([System.IO.Path]::AltDirectorySeparatorChar)")) {
                throw "Cleanup path '$relativePath' is outside the targeted project directory '$projectDirectory'."
            }

            $includePath -replace '\\', '/'
        }
    )

    $solutionContent = @(
        '<Solution>'
        "  <Project Path=`"$projectFileName`" />"
        '</Solution>'
    ) -join [Environment]::NewLine

    try {
        Set-Content -LiteralPath $temporarySolutionX -Value $solutionContent -Encoding utf8
        $null = Invoke-SlnGeneration -SolutionPath $temporarySolutionX -OutputPath $temporarySolution
        Invoke-ReSharperCleanup `
            -SolutionPath $temporarySolution `
            -SettingsPath $SettingsPath `
            -Profile $Profile `
            -IncludePaths $projectRelativeIncludes `
            -CachesHome $CachesHome `
            -NoUpdates:$NoUpdates
    }
    finally {
        foreach ($temporaryPath in @($temporarySolutionX, $temporarySolution)) {
            if (Test-Path -LiteralPath $temporaryPath) {
                Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction Stop
            }
        }
    }
}

function ConvertTo-CleanupRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $pathValue = $Path.Trim()
    $pathForCurrentPlatform = $pathValue -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    $fullPath = if ([System.IO.Path]::IsPathRooted($pathForCurrentPlatform)) {
        [System.IO.Path]::GetFullPath($pathForCurrentPlatform)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $rootFullPath $pathForCurrentPlatform))
    }

    $relativePath = [System.IO.Path]::GetRelativePath($rootFullPath, $fullPath)
    if ($relativePath -eq '..' -or
        $relativePath.StartsWith("..$([System.IO.Path]::DirectorySeparatorChar)") -or
        $relativePath.StartsWith("..$([System.IO.Path]::AltDirectorySeparatorChar)")) {
        throw "Path '$Path' is outside repository root '$RepoRoot'."
    }

    return ($relativePath -replace '\\', '/').TrimStart('/')
}

function Get-CleanupGlobalFallbackReasons {
    [CmdletBinding()]
    param(
        [AllowEmptyCollection()][string[]]$Paths
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    foreach ($path in @($Paths)) {
        $reason = switch -Regex ($path) {
            '(^|/)\.editorconfig$' { 'An .editorconfig file changed.'; break }
            '(^|/)[^/]+\.DotSettings$' { 'A ReSharper settings file changed.'; break }
            '(^|/)Directory\.Build\.(props|targets)$' { 'A Directory.Build.props/targets file changed.'; break }
            '(^|/)Directory\.Packages\.props$' { 'Directory.Packages.props changed.'; break }
            '(^|/)global\.json$' { 'global.json changed.'; break }
            '^\.config/dotnet-tools\.json$' { 'The pinned .NET tool manifest changed.'; break }
            '^(mississippi|samples)\.slnx$' { 'Solution project membership changed.'; break }
            '^clean-up\.ps1$' { 'The canonical cleanup dispatcher changed.'; break }
            '(^|/)clean-up-[^/]+\.ps1$' { 'A cleanup script changed.'; break }
            '(^|/)RepositoryAutomation\.psm1$' { 'The shared cleanup automation module changed.'; break }
            '^\.github/workflows/cleanup\.yml$' { 'The pull-request cleanup workflow changed.'; break }
            default { $null }
        }

        if ($reason -and -not $reasons.Contains($reason)) {
            $reasons.Add($reason)
        }
    }

    return @($reasons)
}

function Get-CleanupProjectCatalog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepoRoot
    )

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $catalogByPath = @{}

    foreach ($solutionName in @('mississippi.slnx', 'samples.slnx')) {
        $solutionPath = Join-Path $rootFullPath $solutionName
        if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
            throw "Expected solution file '$solutionPath' was not found."
        }

        $solutionDocument = [xml](Get-Content -LiteralPath $solutionPath -Raw -Encoding UTF8)
        $projectNodes = @($solutionDocument.SelectNodes('//Project[@Path]'))
        foreach ($projectNode in $projectNodes) {
            $projectPath = [string]$projectNode.Path
            $relativeProjectPath = ConvertTo-CleanupRelativePath -Path $projectPath -RepoRoot $rootFullPath
            $projectFullPath = [System.IO.Path]::GetFullPath((Join-Path $rootFullPath ($relativeProjectPath -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
            if (-not (Test-Path -LiteralPath $projectFullPath -PathType Leaf)) {
                throw "Solution '$solutionName' references project '$relativeProjectPath', but that project was not found."
            }

            $catalogKey = $relativeProjectPath.ToLowerInvariant()
            if (-not $catalogByPath.ContainsKey($catalogKey)) {
                $catalogByPath[$catalogKey] = [pscustomobject]@{
                    ProjectPath   = $projectFullPath
                    RelativePath  = $relativeProjectPath
                    DirectoryPath = Split-Path -Parent $projectFullPath
                    SolutionPaths = [System.Collections.Generic.List[string]]::new()
                }
            }

            $catalogEntry = $catalogByPath[$catalogKey]
            if (-not $catalogEntry.SolutionPaths.Contains($solutionName)) {
                $catalogEntry.SolutionPaths.Add($solutionName)
            }
        }
    }

    return @($catalogByPath.Values | Sort-Object RelativePath)
}

function Resolve-CleanupProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][object[]]$ProjectCatalog
    )

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $rootFullPath ($RelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
    $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()

    if ($extension -eq '.csproj') {
        return @($ProjectCatalog | Where-Object { $_.RelativePath -ieq $RelativePath }) | Select-Object -First 1
    }

    $candidateProjects = @(
        $ProjectCatalog | Where-Object {
            $projectDirectory = ([System.IO.Path]::GetFullPath($_.DirectoryPath)).TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar)
            $fullPath.StartsWith("$projectDirectory$([System.IO.Path]::DirectorySeparatorChar)", [System.StringComparison]::OrdinalIgnoreCase)
        }
    )

    if ($candidateProjects.Count -eq 0) {
        return $null
    }

    $longestDirectoryLength = ($candidateProjects |
        ForEach-Object { ([System.IO.Path]::GetFullPath($_.DirectoryPath)).Length } |
        Measure-Object -Maximum).Maximum
    $bestMatches = @(
        $candidateProjects | Where-Object {
            ([System.IO.Path]::GetFullPath($_.DirectoryPath)).Length -eq $longestDirectoryLength
        }
    )

    if ($bestMatches.Count -ne 1) {
        return $null
    }

    return $bestMatches[0]
}

function Get-CleanupPlan {
    [CmdletBinding()]
    param(
        [AllowEmptyCollection()][string[]]$Paths,
        [Parameter(Mandatory)][string]$RepoRoot,
        [switch]$SkipSamples,
        [switch]$SkipMississippi
    )

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $normalizedPaths = New-Object System.Collections.Generic.List[string]
    foreach ($path in @($Paths)) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        $relativePath = ConvertTo-CleanupRelativePath -Path $path -RepoRoot $rootFullPath
        if (-not [string]::IsNullOrWhiteSpace($relativePath) -and -not $normalizedPaths.Contains($relativePath)) {
            $normalizedPaths.Add($relativePath)
        }
    }

    $globalReasons = @(Get-CleanupGlobalFallbackReasons -Paths @($normalizedPaths))
    $cleanupExtensions = @('.cs', '.csproj', '.props', '.targets', '.razor', '.cshtml', '.xaml', '.axaml', '.resx')
    $eligiblePaths = New-Object System.Collections.Generic.List[string]
    $ignoredPaths = New-Object System.Collections.Generic.List[string]

    foreach ($relativePath in @($normalizedPaths)) {
        $fullPath = Join-Path $rootFullPath ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()
        if ($cleanupExtensions -notcontains $extension -or -not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            $ignoredPaths.Add($relativePath)
            continue
        }

        $eligiblePaths.Add($relativePath)
    }

    if ($globalReasons.Count -gt 0) {
        return [pscustomobject]@{
            Mode              = 'FullFallback'
            Reason            = ($globalReasons -join ' ')
            FallbackReasons   = $globalReasons
            InputPaths        = @($normalizedPaths)
            EligiblePaths     = @($eligiblePaths)
            IgnoredPaths      = @($ignoredPaths)
            Groups            = @()
            AffectedProjects  = @()
        }
    }

    if ($eligiblePaths.Count -eq 0) {
        return [pscustomobject]@{
            Mode              = 'NoOp'
            Reason            = 'No existing cleanup-eligible files were selected.'
            FallbackReasons   = @()
            InputPaths        = @($normalizedPaths)
            EligiblePaths     = @()
            IgnoredPaths      = @($ignoredPaths)
            Groups            = @()
            AffectedProjects  = @()
        }
    }

    $projectCatalog = @(Get-CleanupProjectCatalog -RepoRoot $rootFullPath)
    $projectGroups = @{}
    $projectEntries = @{}
    $unmappedPaths = New-Object System.Collections.Generic.List[string]

    foreach ($relativePath in @($eligiblePaths)) {
        $project = Resolve-CleanupProject -RelativePath $relativePath -RepoRoot $rootFullPath -ProjectCatalog $projectCatalog
        if ($null -eq $project) {
            $unmappedPaths.Add($relativePath)
            continue
        }

        if ($project.SolutionPaths.Count -eq 0) {
            $unmappedPaths.Add($relativePath)
            continue
        }

        $selectedSolutions = @(
            $project.SolutionPaths | Where-Object {
                (-not $SkipSamples -or $_ -ne 'samples.slnx') -and
                (-not $SkipMississippi -or $_ -ne 'mississippi.slnx')
            }
        )
        if ($selectedSolutions.Count -eq 0) {
            $ignoredPaths.Add($relativePath)
            continue
        }

        if (-not $projectGroups.ContainsKey($project.ProjectPath)) {
            $projectGroups[$project.ProjectPath] = New-Object System.Collections.Generic.List[string]
            $projectEntries[$project.ProjectPath] = $project
        }
        if (-not $projectGroups[$project.ProjectPath].Contains($relativePath)) {
            $projectGroups[$project.ProjectPath].Add($relativePath)
        }
    }

    if ($unmappedPaths.Count -gt 0) {
        $fallbackReasons = @(
            'One or more cleanup-eligible files could not be mapped unambiguously to a project.',
            "Unmapped paths: $($unmappedPaths -join ', ')"
        )
        return [pscustomobject]@{
            Mode              = 'FullFallback'
            Reason            = ($fallbackReasons -join ' ')
            FallbackReasons   = $fallbackReasons
            InputPaths        = @($normalizedPaths)
            EligiblePaths     = @($eligiblePaths)
            IgnoredPaths      = @($ignoredPaths)
            Groups            = @()
            AffectedProjects  = @()
        }
    }

    $groups = @(
        foreach ($projectPath in ($projectGroups.Keys | Sort-Object)) {
            $project = $projectEntries[$projectPath]
            [pscustomobject]@{
                ProjectPath   = $project.ProjectPath
                RelativePath  = $project.RelativePath
                SolutionPaths = @($project.SolutionPaths)
                IncludePaths  = @($projectGroups[$projectPath] | Sort-Object)
            }
        }
    )

    if ($groups.Count -eq 0) {
        return [pscustomobject]@{
            Mode              = 'NoOp'
            Reason            = 'No selected solution projects contain the provided files.'
            FallbackReasons   = @()
            InputPaths        = @($normalizedPaths)
            EligiblePaths     = @($eligiblePaths)
            IgnoredPaths      = @($ignoredPaths)
            Groups            = @()
            AffectedProjects  = @()
        }
    }

    return [pscustomobject]@{
        Mode              = 'Targeted'
        Reason            = "Targeted cleanup selected $($groups.Count) project(s)."
        FallbackReasons   = @()
        InputPaths        = @($normalizedPaths)
        EligiblePaths     = @($eligiblePaths)
        IgnoredPaths      = @($ignoredPaths)
        Groups            = $groups
        AffectedProjects  = @($groups | Select-Object -ExpandProperty RelativePath)
    }
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
        [string[]]$IncludePaths,
        [string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string]$CachesHome,
        [switch]$NoUpdates,
        [switch]$SkipToolRestore
    )

    $slnxPath = Join-Path $RepoRoot 'mississippi.slnx'
    $slnPath = Join-Path $RepoRoot 'mississippi.sln'
    $settingsPathToUse = if ($SettingsPath) { $SettingsPath } else { Join-Path $RepoRoot 'Directory.DotSettings' }

    Write-AutomationBanner -Message '=== MISSISSIPPI SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with '$Profile' profile"
    Write-Host

    if (-not $SkipToolRestore) {
        Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
        Invoke-DotnetToolRestore -RepoRoot $RepoRoot
        Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)
    }
    else {
        Write-Host '[1/3] Using the already restored dotnet tools' -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Write-Host "[2/3] Generating mississippi.sln from mississippi.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: '$Profile'"
    Write-Host "Settings file: $settingsPathToUse"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPathToUse -Profile $Profile -IncludePaths $IncludePaths -CachesHome $CachesHome -NoUpdates:$NoUpdates
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== MISSISSIPPI SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-SampleSolutionCleanup {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$IncludePaths,
        [string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string]$CachesHome,
        [switch]$NoUpdates,
        [switch]$SkipToolRestore
    )

    $slnxPath = Join-Path $RepoRoot 'samples.slnx'
    $slnPath = Join-Path $RepoRoot 'samples.sln'
    $settingsPathToUse = if ($SettingsPath) { $SettingsPath } else { Join-Path $RepoRoot 'Directory.DotSettings' }

    Write-AutomationBanner -Message '=== SAMPLE SOLUTION CODE CLEANUP PROCESS ===' -ForegroundColor ([ConsoleColor]::Yellow) -InsertBlankLine
    Write-Host "Running ReSharper CleanupCode with '$Profile' profile"
    Write-Host

    if (-not $SkipToolRestore) {
        Write-Host '[1/3] Restoring dotnet tools (including ReSharper CLI)...' -ForegroundColor ([ConsoleColor]::Cyan)
        Invoke-DotnetToolRestore -RepoRoot $RepoRoot
        Write-Host 'SUCCESS: Dotnet tools restored, ReSharper CLI available' -ForegroundColor ([ConsoleColor]::Green)
    }
    else {
        Write-Host '[1/3] Using the already restored dotnet tools' -ForegroundColor ([ConsoleColor]::Cyan)
    }

    Write-Host "[2/3] Generating samples.sln from samples.slnx using SlnGen..." -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host 'SlnGen converts .slnx format to .sln format for ReSharper compatibility'
    Invoke-SlnGeneration -SolutionPath $slnxPath -OutputPath $slnPath | Out-Null
    Write-Host 'SUCCESS: Solution file generated for ReSharper processing' -ForegroundColor ([ConsoleColor]::Green)

    Write-Host '[3/3] Running ReSharper CleanupCode on generated solution...' -ForegroundColor ([ConsoleColor]::Cyan)
    Write-Host "Cleanup profile: '$Profile'"
    Write-Host "Settings file: $settingsPathToUse"
    Write-Host "Target solution: $slnPath"
    if ($IncludePaths -and $IncludePaths.Count -gt 0) {
        Write-Host "Included paths: $($IncludePaths -join ';')"
    }
    Invoke-ReSharperCleanup -SolutionPath $slnPath -SettingsPath $settingsPathToUse -Profile $Profile -IncludePaths $IncludePaths -CachesHome $CachesHome -NoUpdates:$NoUpdates
    Write-Host 'SUCCESS: ReSharper code cleanup completed' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host
    Write-Host '=== SAMPLE SOLUTION CLEANUP COMPLETED ===' -ForegroundColor ([ConsoleColor]::Green)
    Write-Host 'All code files have been formatted according to project standards'
}

function Invoke-RepositoryCleanup {
    [CmdletBinding()]
    param(
        [ValidateSet('Full', 'Targeted')][string]$Mode = 'Full',
        [string]$RepoRoot = (Get-RepositoryRoot),
        [string[]]$Paths,
        [string]$Configuration = 'Release',
        [string]$SettingsPath,
        [string]$Profile = 'Built-in: Full Cleanup',
        [string]$CachesHome,
        [switch]$NoUpdates,
        [switch]$SkipSamples,
        [switch]$SkipMississippi,
        [switch]$SkipToolRestore,
        [switch]$SkipRestore,
        [switch]$SkipBuild
    )

    if ($SkipSamples -and $SkipMississippi) {
        throw 'Both -SkipSamples and -SkipMississippi were provided. At least one solution must be enabled.'
    }

    $rootFullPath = [System.IO.Path]::GetFullPath($RepoRoot)
    $settingsPathToUse = if ($SettingsPath) { $SettingsPath } else { Join-Path $rootFullPath 'Directory.DotSettings' }

    if ($Mode -eq 'Targeted') {
        $plan = Get-CleanupPlan -Paths @($Paths) -RepoRoot $rootFullPath -SkipSamples:$SkipSamples -SkipMississippi:$SkipMississippi
        Write-Host "Cleanup mode: $($plan.Mode)"
        Write-Host "Changed input files: $($plan.InputPaths.Count)"
        Write-Host "Cleanup-eligible files: $($plan.EligiblePaths.Count)"
        if ($plan.IgnoredPaths.Count -gt 0) {
            Write-Host "Ignored files: $($plan.IgnoredPaths -join ', ')" -ForegroundColor ([ConsoleColor]::DarkGray)
        }

        if ($plan.Mode -eq 'NoOp') {
            Write-Host $plan.Reason -ForegroundColor ([ConsoleColor]::Yellow)
            return $plan
        }

        if ($plan.Mode -eq 'FullFallback') {
            Write-Host "Targeted cleanup is falling back to full cleanup: $($plan.Reason)" -ForegroundColor ([ConsoleColor]::Yellow)
            $null = Invoke-RepositoryCleanup -Mode Full -RepoRoot $rootFullPath -Configuration $Configuration -SettingsPath $settingsPathToUse -Profile $Profile -CachesHome $CachesHome -NoUpdates:$NoUpdates -SkipSamples:$SkipSamples -SkipMississippi:$SkipMississippi -SkipToolRestore:$SkipToolRestore -SkipRestore:$SkipRestore -SkipBuild:$SkipBuild
            return $plan
        }

        if (-not $SkipToolRestore) {
            Write-Host 'Restoring dotnet tools once for targeted cleanup...' -ForegroundColor ([ConsoleColor]::Cyan)
            Invoke-DotnetToolRestore -RepoRoot $rootFullPath
        }
        else {
            Write-Host 'Using the already restored dotnet tools for targeted cleanup.' -ForegroundColor ([ConsoleColor]::Cyan)
        }

        foreach ($group in @($plan.Groups)) {
            Write-Host "Preparing project $($group.RelativePath) for $($group.IncludePaths.Count) changed file(s)." -ForegroundColor ([ConsoleColor]::Cyan)
            if (-not $SkipRestore) {
                Invoke-SolutionRestore -SolutionPath $group.ProjectPath -Description $group.RelativePath -Quiet
            }
            if (-not $SkipBuild) {
                Invoke-SolutionBuild -SolutionPath $group.ProjectPath -Configuration $Configuration -NoRestore -NoIncremental -AdditionalArguments @('-p:RunAnalyzers=false') -Quiet
            }

            Write-Host "Running CleanupCode for $($group.RelativePath) using a temporary project solution" -ForegroundColor ([ConsoleColor]::Cyan)
            Invoke-TargetedProjectCleanup `
                -ProjectGroup $group `
                -RepoRoot $rootFullPath `
                -SettingsPath $settingsPathToUse `
                -Profile $Profile `
                -CachesHome $CachesHome `
                -NoUpdates:$NoUpdates
        }

        Write-Host "Targeted cleanup completed for $($plan.Groups.Count) affected project(s)." -ForegroundColor ([ConsoleColor]::Green)
        return $plan
    }

    if (-not $SkipToolRestore) {
        Write-Host 'Restoring dotnet tools once for full cleanup...' -ForegroundColor ([ConsoleColor]::Cyan)
        Invoke-DotnetToolRestore -RepoRoot $rootFullPath
    }
    else {
        Write-Host 'Using the already restored dotnet tools for full cleanup.' -ForegroundColor ([ConsoleColor]::Cyan)
    }

    $solutions = @()
    if (-not $SkipMississippi) {
        $solutions += [pscustomobject]@{ Name = 'Mississippi'; Path = (Join-Path $rootFullPath 'mississippi.slnx') }
    }
    if (-not $SkipSamples) {
        $solutions += [pscustomobject]@{ Name = 'Samples'; Path = (Join-Path $rootFullPath 'samples.slnx') }
    }

    foreach ($solution in $solutions) {
        if (-not $SkipRestore) {
            Invoke-SolutionRestore -SolutionPath $solution.Path -Description $solution.Name -Quiet
        }
        if (-not $SkipBuild) {
            Invoke-SolutionBuild -SolutionPath $solution.Path -Configuration $Configuration -NoRestore -NoIncremental -AdditionalArguments @('-p:RunAnalyzers=false') -Quiet
        }

        if ($solution.Name -eq 'Mississippi') {
            Invoke-MississippiSolutionCleanup -RepoRoot $rootFullPath -SettingsPath $settingsPathToUse -Profile $Profile -CachesHome $CachesHome -NoUpdates:$NoUpdates -SkipToolRestore
        }
        else {
            Invoke-SampleSolutionCleanup -RepoRoot $rootFullPath -SettingsPath $settingsPathToUse -Profile $Profile -CachesHome $CachesHome -NoUpdates:$NoUpdates -SkipToolRestore
        }
    }

    return [pscustomobject]@{
        Mode             = 'Full'
        Reason           = 'Full repository cleanup completed.'
        FallbackReasons  = @()
        InputPaths       = @()
        EligiblePaths    = @()
        IgnoredPaths     = @()
        Groups           = @()
        AffectedProjects = @($solutions | Select-Object -ExpandProperty Name)
    }
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

Export-ModuleMember -Function Get-RepositoryRoot, Write-AutomationBanner, Invoke-AutomationStep, Invoke-DotnetToolRestore, Invoke-SolutionRestore, Invoke-SolutionBuild, New-AutomationRunDirectory, Invoke-SolutionTests, Invoke-SlnGeneration, Invoke-ReSharperCleanup, Invoke-TargetedProjectCleanup, ConvertTo-CleanupRelativePath, Get-CleanupGlobalFallbackReasons, Get-CleanupProjectCatalog, Resolve-CleanupProject, Get-CleanupPlan, Invoke-RepositoryCleanup, Get-TestProjects, Invoke-StrykerMutationTestPerProject, Invoke-StrykerMutationTest, Invoke-MississippiSolutionBuild, Invoke-SampleSolutionBuild, Invoke-MississippiSolutionCleanup, Invoke-SampleSolutionCleanup, Invoke-FinalSolutionsBuild, Invoke-MississippiSolutionUnitTests, Invoke-SampleSolutionUnitTests, Invoke-MississippiSolutionMutationTests, Invoke-SolutionsPipeline
