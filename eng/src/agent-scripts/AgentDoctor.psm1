Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DoctorRepositoryRoot {
    param([Parameter(Mandatory)][string]$RepositoryRoot)
    return (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
}

function Add-DoctorCheck {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Checks,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('ready', 'missing', 'unsupported', 'unknown', 'not-required')][string]$State,
        [Parameter(Mandatory)][bool]$Required,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Details,
        [string]$Remediation = ''
    )

    $Checks.Add([pscustomobject][ordered]@{
        Name = $Name
        State = $State
        Required = $Required
        Details = $Details
        Remediation = $Remediation
    })
}

function Invoke-DoctorProbe {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory = '',
        [ValidateRange(1, 300)][int]$TimeoutSeconds = 15,
        [hashtable]$ProbeOverrides = @{}
    )

    if ($ProbeOverrides.ContainsKey($Name)) {
        return $ProbeOverrides[$Name]
    }
    $command = Resolve-DoctorCommand -FilePath $FilePath
    if ($null -eq $command) {
        return [pscustomobject]@{ Available = $false; Output = ''; ExitCode = 127; Error = "Command '$FilePath' was not found." }
    }
    $locationPushed = $false
    $process = $null
    try {
        if ($WorkingDirectory) {
            Push-Location -LiteralPath $WorkingDirectory
            $locationPushed = $true
        }

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $command.FilePath
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        if ($WorkingDirectory) { $startInfo.WorkingDirectory = $WorkingDirectory }
        foreach ($argument in @($command.PrefixArguments) + $Arguments) { $null = $startInfo.ArgumentList.Add($argument) }

        $process = [System.Diagnostics.Process]::new()
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            return [pscustomobject]@{ Available = $true; Output = ''; ExitCode = 1; Error = "Command '$FilePath' could not be started."; TimedOut = $false }
        }

        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()
        $completed = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $completed) {
            $terminationErrors = [System.Collections.Generic.List[string]]::new()
            try { $process.Kill($true) }
            catch {
                $terminationErrors.Add($_.Exception.Message)
                try { $process.Kill() }
                catch { $terminationErrors.Add($_.Exception.Message) }
            }
            $terminated = $process.WaitForExit(1000)
            $null = $standardOutputTask.Wait(1000)
            $null = $standardErrorTask.Wait(1000)
            $timedOutOutput = [System.Collections.Generic.List[string]]::new()
            $timedOutStdout = if ($standardOutputTask.IsCompleted) { $standardOutputTask.GetAwaiter().GetResult() } else { '' }
            $timedOutStderr = if ($standardErrorTask.IsCompleted) { $standardErrorTask.GetAwaiter().GetResult() } else { '' }
            if ($timedOutStdout) { $timedOutOutput.Add($timedOutStdout.TrimEnd()) }
            $timeoutDetails = "Command '$FilePath' timed out after $TimeoutSeconds seconds."
            if (-not $terminated) { $timeoutDetails += ' The process did not terminate during the 1 second grace period.' }
            if ($timedOutStderr) { $timeoutDetails += " Native error output: $($timedOutStderr.TrimEnd())" }
            if ($terminationErrors.Count -gt 0) { $timeoutDetails += " Termination error: $($terminationErrors -join '; ')" }
            return [pscustomobject]@{
                Available = $true
                Output = ($timedOutOutput -join [Environment]::NewLine).Trim()
                ExitCode = 124
                Error = $timeoutDetails
                TimedOut = $true
            }
        }

        $process.WaitForExit()
        $stdout = $standardOutputTask.GetAwaiter().GetResult()
        $stderr = $standardErrorTask.GetAwaiter().GetResult()
        return [pscustomobject]@{ Available = $true; Output = $stdout.Trim(); ExitCode = $process.ExitCode; Error = $stderr.Trim(); TimedOut = $false }
    }
    catch {
        return [pscustomobject]@{ Available = $true; Output = ''; ExitCode = 1; Error = $_.Exception.Message; TimedOut = $false }
    }
    finally {
        if ($null -ne $process) { $process.Dispose() }
        if ($locationPushed) { Pop-Location }
    }
}

function Test-DoctorPowerShellVersion {
    param([Parameter(Mandatory)][version]$Version)
    return $Version -ge [version]'7.0'
}

function Resolve-DoctorCommand {
    param([Parameter(Mandatory)][string]$FilePath)

    $commands = @(Get-Command $FilePath -All -ErrorAction SilentlyContinue)
    if ($commands.Count -eq 0) { return $null }

    $scriptCommand = @($commands | Where-Object { [System.IO.Path]::GetExtension($_.Source) -ieq '.ps1' } | Select-Object -First 1)
    if ($scriptCommand.Count -gt 0) {
        $hostCommand = @(Get-Command pwsh -ErrorAction SilentlyContinue | Select-Object -First 1)
        if ($hostCommand.Count -gt 0) {
            return [pscustomobject]@{
                FilePath = $hostCommand[0].Source
                PrefixArguments = @('-NoProfile', '-File', $scriptCommand[0].Source)
            }
        }
    }

    $applicationCommand = @($commands | Where-Object { $_.CommandType -eq 'Application' } | Select-Object -First 1)
    $selected = if ($applicationCommand.Count -gt 0) { $applicationCommand[0] } else { $commands[0] }
    return [pscustomobject]@{ FilePath = $selected.Source; PrefixArguments = @() }
}

function Get-DoctorProbeDetails {
    param([Parameter(Mandatory)][object]$Probe)

    if ($Probe.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace([string]$Probe.Output)) { return [string]$Probe.Output }
    if (-not [string]::IsNullOrWhiteSpace([string]$Probe.Error)) { return [string]$Probe.Error }
    if (-not [string]::IsNullOrWhiteSpace([string]$Probe.Output)) { return [string]$Probe.Output }
    return 'Probe returned no diagnostic output.'
}

function Test-DoctorSdkCompatibility {
    param([string]$Expected, [string]$Actual)
    if ($Actual -eq $Expected) { return $true }
    $expectedMatch = [regex]::Match($Expected, '^(?<Prefix>\d+\.\d+\.)(?<Feature>\d)\d\d$')
    $actualMatch = [regex]::Match($Actual, '^(?<Prefix>\d+\.\d+\.)(?<Feature>\d)\d\d$')
    if ($expectedMatch.Success -and $actualMatch.Success) {
        return $expectedMatch.Groups['Prefix'].Value -eq $actualMatch.Groups['Prefix'].Value -and $expectedMatch.Groups['Feature'].Value -eq $actualMatch.Groups['Feature'].Value
    }
    return $false
}

function Get-AgentDoctorReport {
    [CmdletBinding()]
    param(
        [string]$RepositoryRoot = (Get-Location).Path,
        [ValidateSet('Core', 'Docs', 'Spring', 'GitHub', 'All')][string]$Profile = 'Core',
        [hashtable]$ProbeOverrides = @{}
    )

    $root = Get-DoctorRepositoryRoot -RepositoryRoot $RepositoryRoot
    $checks = [System.Collections.Generic.List[object]]::new()
    $profiles = if ($Profile -eq 'All') { @('Core', 'Docs', 'Spring', 'GitHub') } else { @($Profile) }
    $powerShellVersion = [version]$PSVersionTable.PSVersion
    $powerShellReady = Test-DoctorPowerShellVersion -Version $powerShellVersion
    Add-DoctorCheck -Checks $checks -Name 'powershell' -State $(if ($powerShellReady) { 'ready' } else { 'unsupported' }) -Required $true -Details "PowerShell $powerShellVersion is $($(if ($powerShellReady) { 'supported' } else { 'below the required 7.0 minimum' }))." -Remediation $(if ($powerShellReady) { '' } else { 'Run the doctor with PowerShell 7.0 or later (pwsh).' })

    $globalJsonPath = Join-Path $root 'global.json'
    $globalJson = $null
    $globalJsonReady = $false
    if (Test-Path -LiteralPath $globalJsonPath -PathType Leaf) {
        try {
            $globalJson = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
            $sdkVersion = if ($null -ne $globalJson.sdk) { [string]$globalJson.sdk.version } else { '' }
            $hasLocalSdkPaths = $null -ne $globalJson.sdk -and $null -ne $globalJson.sdk.PSObject.Properties['paths']
            if ($hasLocalSdkPaths) {
                Add-DoctorCheck -Checks $checks -Name 'global.json' -State unsupported -Required $true -Details 'global.json uses checkout-local SDK search paths, which the read-only doctor will not execute.' -Remediation 'Remove sdk.paths or run validation from a trusted SDK installation.'
            }
            elseif ([string]::IsNullOrWhiteSpace($sdkVersion)) {
                Add-DoctorCheck -Checks $checks -Name 'global.json' -State unsupported -Required $true -Details 'global.json does not declare sdk.version.' -Remediation 'Add the repository SDK version to global.json.'
            }
            else {
                $globalJsonReady = $true
            }
        }
        catch { Add-DoctorCheck -Checks $checks -Name 'global.json' -State unsupported -Required $true -Details $_.Exception.Message -Remediation 'Repair global.json before starting a task.' }
    }
    else {
        Add-DoctorCheck -Checks $checks -Name 'global.json' -State missing -Required $true -Details 'SDK selection file is missing.' -Remediation 'Restore global.json from the repository.'
    }

    if ($profiles -contains 'Core' -or $profiles -contains 'Spring') {
        $dotnet = Invoke-DoctorProbe -Name 'dotnet-version' -FilePath 'dotnet' -Arguments @('--version') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        if (-not $dotnet.Available) {
            Add-DoctorCheck -Checks $checks -Name 'dotnet-sdk' -State missing -Required $true -Details $dotnet.Error -Remediation 'Install the SDK selected by global.json.'
        }
        elseif ($dotnet.ExitCode -ne 0) {
            Add-DoctorCheck -Checks $checks -Name 'dotnet-sdk' -State unknown -Required $true -Details (Get-DoctorProbeDetails -Probe $dotnet) -Remediation 'Run dotnet --version from the repository root and repair SDK selection.'
        }
        else {
            if (-not $globalJsonReady) {
                Add-DoctorCheck -Checks $checks -Name 'dotnet-sdk' -State unknown -Required $true -Details 'The selected SDK cannot be compared because global.json does not declare sdk.version.' -Remediation 'Repair global.json before starting a task.'
            }
            else {
                $expectedSdk = [string]$globalJson.sdk.version
                $actualSdk = $dotnet.Output.Trim()
                $compatible = Test-DoctorSdkCompatibility -Expected $expectedSdk -Actual $actualSdk
                Add-DoctorCheck -Checks $checks -Name 'dotnet-sdk' -State $(if ($compatible) { 'ready' } else { 'unsupported' }) -Required $true -Details "Selected SDK reports $actualSdk; global.json requests $expectedSdk." -Remediation $(if ($compatible) { '' } else { 'Install the requested SDK feature band or use an approved roll-forward.' })
            }
        }

        $toolsManifest = Join-Path $root '.config/dotnet-tools.json'
        $toolData = $null
        if (Test-Path -LiteralPath $toolsManifest -PathType Leaf) {
            try {
                $toolData = Get-Content -LiteralPath $toolsManifest -Raw | ConvertFrom-Json
                $toolProperties = @($toolData.tools.PSObject.Properties)
                if ($toolProperties.Count -eq 0) { throw [System.IO.InvalidDataException]::new('Local tool manifest must contain a nonempty tools object.') }
                Add-DoctorCheck -Checks $checks -Name 'dotnet-tools-manifest' -State ready -Required $true -Details "Pinned tool manifest contains $($toolProperties.Count) tools."
            }
            catch {
                $toolData = $null
                Add-DoctorCheck -Checks $checks -Name 'dotnet-tools-manifest' -State unsupported -Required $true -Details $_.Exception.Message -Remediation 'Repair .config/dotnet-tools.json.'
            }
        }
        else {
            Add-DoctorCheck -Checks $checks -Name 'dotnet-tools-manifest' -State missing -Required $true -Details 'Pinned local tool manifest is missing.' -Remediation 'Restore .config/dotnet-tools.json.'
        }

        if ($ProbeOverrides.ContainsKey('dotnet-tools')) {
            $toolProbe = $ProbeOverrides['dotnet-tools']
            $toolProbeDetails = Get-DoctorProbeDetails -Probe $toolProbe
            $toolState = if ($toolProbe.Available -and $toolProbe.ExitCode -eq 0) { 'ready' } elseif (-not $toolProbe.Available -or $toolProbeDetails -match '(?i)restore|not installed|not available|not found') { 'missing' } else { 'unknown' }
            Add-DoctorCheck -Checks $checks -Name 'dotnet-tools' -State $toolState -Required $true -Details $toolProbeDetails -Remediation $(if ($toolState -eq 'ready') { '' } else { 'Run dotnet tool restore from the repository root and retry the doctor.' })
        }
        elseif ($null -eq $toolData) {
            Add-DoctorCheck -Checks $checks -Name 'dotnet-tools' -State unsupported -Required $true -Details 'Local tools cannot be verified because the tool manifest is unavailable.' -Remediation 'Repair .config/dotnet-tools.json before running dotnet tool restore.'
        }
        else {
            $dotnetHome = if ([string]::IsNullOrWhiteSpace($env:DOTNET_CLI_HOME)) { [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile) } else { $env:DOTNET_CLI_HOME }
            $resolverRoot = Join-Path $dotnetHome '.dotnet/toolResolverCache/1'
            $toolFailures = [System.Collections.Generic.List[string]]::new()
            $hasMissingTool = $false
            $hasUnknownTool = $false
            foreach ($tool in @($toolData.tools.PSObject.Properties)) {
                $packageId = [string]$tool.Name
                $expectedVersion = [string]$tool.Value.version
                $cacheFile = @(Get-ChildItem -LiteralPath $resolverRoot -File -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $packageId } | Select-Object -First 1)
                if ($cacheFile.Count -eq 0) {
                    $toolFailures.Add("${packageId}@${expectedVersion}: resolver metadata is missing.")
                    $hasMissingTool = $true
                    continue
                }

                try {
                    $records = @(Get-Content -LiteralPath $cacheFile[0].FullName -Raw -ErrorAction Stop | ConvertFrom-Json)
                    $matchingRecord = @($records | Where-Object {
                        if ([string]$_.Version -ne $expectedVersion) { return $false }
                        $paths = @()
                        if ($null -ne $_.PSObject.Properties['PathToExecutable']) { $paths += [string]$_.PathToExecutable }
                        foreach ($commandRecord in @($_.Commands)) {
                            if ($null -ne $commandRecord.PSObject.Properties['PathToExecutable']) { $paths += [string]$commandRecord.PathToExecutable }
                        }
                        @($paths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -gt 0
                    })
                    if ($matchingRecord.Count -eq 0) {
                        $toolFailures.Add("${packageId}@${expectedVersion}: restored executable metadata is missing.")
                        $hasMissingTool = $true
                    }
                }
                catch {
                    $toolFailures.Add("${packageId}@${expectedVersion}: resolver metadata is unreadable: $($_.Exception.Message)")
                    $hasUnknownTool = $true
                }
            }
            $toolState = if ($hasMissingTool) { 'missing' } elseif ($hasUnknownTool) { 'unknown' } else { 'ready' }
            $toolDetails = if ($toolFailures.Count -eq 0) { "Verified $(@($toolData.tools.PSObject.Properties).Count) local tool resolver records and executable paths without running tool code." } else { $toolFailures -join '; ' }
            Add-DoctorCheck -Checks $checks -Name 'dotnet-tools' -State $toolState -Required $true -Details $toolDetails -Remediation $(if ($toolState -eq 'ready') { '' } else { 'Run dotnet tool restore from the repository root and retry the doctor.' })
        }

        $git = Invoke-DoctorProbe -Name 'git-root' -FilePath 'git' -Arguments @('-C', $root, 'rev-parse', '--show-toplevel') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        Add-DoctorCheck -Checks $checks -Name 'git-worktree' -State $(if ($git.Available -and $git.ExitCode -eq 0) { 'ready' } elseif (-not $git.Available) { 'missing' } else { 'unknown' }) -Required $true -Details (Get-DoctorProbeDetails -Probe $git) -Remediation $(if ($git.ExitCode -eq 0) { '' } else { 'Run the doctor from a readable Git checkout.' })
        $pester = @(Get-Module -ListAvailable -Name Pester | Where-Object { $_.Version -ge [version]'5.0.0' } | Sort-Object Version -Descending | Select-Object -First 1)
        Add-DoctorCheck -Checks $checks -Name 'pester' -State $(if ($pester.Count -gt 0) { 'ready' } else { 'missing' }) -Required $false -Details $(if ($pester.Count -gt 0) { "Pester $($pester[0].Version) is available." } else { 'Pester 5 or later is not available.' }) -Remediation 'Install Pester 5 or later only when running the PowerShell validation harness.'
    }

    if ($profiles -contains 'Docs') {
        $packageJson = Join-Path $root 'docs/Docusaurus/package.json'
        $lockFile = Join-Path $root 'docs/Docusaurus/package-lock.json'
        $node = Invoke-DoctorProbe -Name 'node-version' -FilePath 'node' -Arguments @('--version') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        $npm = Invoke-DoctorProbe -Name 'npm-version' -FilePath 'npm' -Arguments @('--version') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        $nodeVersionMatch = if ($node.Available -and $node.ExitCode -eq 0) { [regex]::Match($node.Output.Trim(), '^v?(?<Major>\d+)(?:\.(?<Minor>\d+))?(?:\.(?<Patch>\d+))?') } else { $null }
        $nodeState = if (-not $node.Available) { 'missing' } elseif ($node.ExitCode -ne 0) { 'unknown' } elseif (-not $nodeVersionMatch.Success) { 'unknown' } elseif ([int]$nodeVersionMatch.Groups['Major'].Value -lt 20) { 'unsupported' } else { 'ready' }
        $nodeDetails = Get-DoctorProbeDetails -Probe $node
        $nodeRemediation = if ($nodeState -eq 'ready') { '' } elseif ($nodeState -eq 'unsupported') { 'Install Node.js 20 or later for the documentation profile.' } else { 'Install Node.js 20 or later and verify node --version.' }
        Add-DoctorCheck -Checks $checks -Name 'node' -State $nodeState -Required $true -Details $nodeDetails -Remediation $nodeRemediation
        Add-DoctorCheck -Checks $checks -Name 'npm' -State $(if ($npm.Available -and $npm.ExitCode -eq 0) { 'ready' } elseif (-not $npm.Available) { 'missing' } else { 'unknown' }) -Required $true -Details (Get-DoctorProbeDetails -Probe $npm) -Remediation 'Install npm for the documentation profile.'
        $docsManifestState = 'missing'
        $docsManifestDetails = "package.json=$((Test-Path -LiteralPath $packageJson -PathType Leaf)); package-lock.json=$((Test-Path -LiteralPath $lockFile -PathType Leaf))."
        if ((Test-Path -LiteralPath $packageJson -PathType Leaf) -and (Test-Path -LiteralPath $lockFile -PathType Leaf)) {
            try { Get-Content -LiteralPath $packageJson -Raw | ConvertFrom-Json | Out-Null; Get-Content -LiteralPath $lockFile -Raw | ConvertFrom-Json | Out-Null; $docsManifestState = 'ready' }
            catch { $docsManifestState = 'unsupported'; $docsManifestDetails = $_.Exception.Message }
        }
        Add-DoctorCheck -Checks $checks -Name 'docs-manifests' -State $docsManifestState -Required $true -Details $docsManifestDetails -Remediation 'Restore valid Docusaurus package manifests.'
    }
    else { Add-DoctorCheck -Checks $checks -Name 'docs-profile' -State not-required -Required $false -Details 'Documentation prerequisites were not requested.' }

    if ($profiles -contains 'Spring') {
        $docker = Invoke-DoctorProbe -Name 'docker-ostype' -FilePath 'docker' -Arguments @('info', '--format', '{{.OSType}}') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        $dockerState = if (-not $docker.Available) { 'missing' } elseif ($docker.ExitCode -ne 0) { 'unknown' } elseif ($docker.Output.Trim() -eq 'linux') { 'ready' } else { 'unsupported' }
        Add-DoctorCheck -Checks $checks -Name 'docker-linux' -State $dockerState -Required $true -Details (Get-DoctorProbeDetails -Probe $docker) -Remediation 'Start Docker with Linux containers and grant this user access.'
        $appHost = Join-Path $root 'samples/Spring/Spring.AppHost/Spring.AppHost.csproj'
        Add-DoctorCheck -Checks $checks -Name 'spring-apphost' -State $(if (Test-Path -LiteralPath $appHost -PathType Leaf) { 'ready' } else { 'missing' }) -Required $true -Details $appHost -Remediation 'Restore the Spring AppHost project.'
        $packageProps = Join-Path $root 'Directory.Packages.props'
        $aspireState = 'missing'
        $aspireDetails = 'Directory.Packages.props or Aspire.Hosting.AppHost is missing.'
        if (Test-Path -LiteralPath $packageProps -PathType Leaf) {
            try {
                [xml]$packageXml = Get-Content -LiteralPath $packageProps -Raw
                $aspire = @($packageXml.Project.ItemGroup.PackageVersion | Where-Object Include -EQ 'Aspire.Hosting.AppHost')[0]
                if ($null -ne $aspire -and -not [string]::IsNullOrWhiteSpace([string]$aspire.Version)) { $aspireState = 'ready'; $aspireDetails = [string]$aspire.Version } else { $aspireState = 'unsupported' }
            } catch { $aspireState = 'unsupported'; $aspireDetails = $_.Exception.Message }
        }
        Add-DoctorCheck -Checks $checks -Name 'spring-aspire-package' -State $aspireState -Required $true -Details $aspireDetails -Remediation 'Restore a valid Aspire.Hosting.AppHost PackageVersion.'
        $playwright = Join-Path $root 'artifacts/tools/playwright'
        Add-DoctorCheck -Checks $checks -Name 'playwright-browsers' -State $(if (Test-Path -LiteralPath $playwright -PathType Container) { 'ready' } else { 'unknown' }) -Required $false -Details 'Browser binaries are checked separately by the L3 setup.' -Remediation 'Run test-spring.ps1 once for the L3 profile if browser binaries are needed.'
    }
    else { Add-DoctorCheck -Checks $checks -Name 'spring-profile' -State not-required -Required $false -Details 'Spring prerequisites were not requested.' }

    if ($profiles -contains 'GitHub') {
        $remote = Invoke-DoctorProbe -Name 'git-remote' -FilePath 'git' -Arguments @('-C', $root, 'config', '--get', 'remote.origin.url') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        $remoteSlug = (($remote.Output.Trim() -replace '\.git$','') -replace '^git@github\.com:', '' -replace '^https://github\.com/', '')
        $remoteSlug = $remoteSlug.TrimEnd('/')
        $gh = Invoke-DoctorProbe -Name 'github-repository' -FilePath 'gh' -Arguments @('repo', 'view', $remoteSlug, '--json', 'nameWithOwner') -WorkingDirectory $root -ProbeOverrides $ProbeOverrides
        $githubState = if (-not $remote.Available) { 'missing' } elseif ($remote.ExitCode -ne 0) { 'unknown' } elseif (-not $gh.Available) { 'missing' } elseif ($gh.ExitCode -eq 0) { 'ready' } else { 'unknown' }
        $githubDetails = if (-not $remote.Available -or $remote.ExitCode -ne 0) { Get-DoctorProbeDetails -Probe $remote } elseif ($gh.ExitCode -eq 0) { 'Repository identity resolved without exposing credentials.' } else { Get-DoctorProbeDetails -Probe $gh }
        Add-DoctorCheck -Checks $checks -Name 'github-repository' -State $githubState -Required $true -Details $githubDetails -Remediation 'Authenticate gh with read access to the current repository and verify Git is installed.'
    }
    else { Add-DoctorCheck -Checks $checks -Name 'github-profile' -State not-required -Required $false -Details 'GitHub delivery prerequisites were not requested.' }

    $requiredFailures = @($checks | Where-Object { $_.Required -and $_.State -notin @('ready') })
    [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Status = if ($requiredFailures.Count -eq 0) { 'READY' } else { 'INCOMPLETE' }
        Profile = $Profile
        RepositoryRoot = $root
        Checks = @($checks)
        RequiredFailures = @($requiredFailures | ForEach-Object { $_.Name })
        IsPrerequisiteReady = $requiredFailures.Count -eq 0
    }
}

function Format-AgentDoctorText {
    param([Parameter(Mandatory)][object]$Report)
    @(
        "DOCTOR: $($Report.Status)"
        "PROFILE: $($Report.Profile)"
        "REPOSITORY: $($Report.RepositoryRoot)"
        $(foreach ($check in @($Report.Checks)) {
            $remediation = if ($check.State -in @('ready', 'not-required') -or [string]::IsNullOrWhiteSpace([string]$check.Remediation)) { '' } else { " Remediation: $($check.Remediation)" }
            "[$($check.State)] $($check.Name): $($check.Details)$remediation"
        })
        $(foreach ($failure in @($Report.RequiredFailures)) { "REQUIRED_FAILURE: $failure" })
    )
}

Export-ModuleMember -Function Get-AgentDoctorReport, Format-AgentDoctorText
