#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Repository prerequisite doctor' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/AgentDoctor.psm1'
        Import-Module -Name $modulePath -Force
        $fixtureRoot = Join-Path $TestDrive 'doctor-repository'
        New-Item -ItemType Directory -Path (Join-Path $fixtureRoot '.config'), (Join-Path $fixtureRoot 'docs/Docusaurus'), (Join-Path $fixtureRoot 'samples/Spring/Spring.AppHost') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'global.json') -Value '{"sdk":{"version":"10.0.400","rollForward":"patch"}}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.config/dotnet-tools.json') -Value '{"version":1,"tools":{"example":{"version":"1.0.0","commands":["example"]}}}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/Docusaurus/package.json') -Value '{}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/Docusaurus/package-lock.json') -Value '{}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'samples/Spring/Spring.AppHost/Spring.AppHost.csproj') -Value '<Project />'

        $readyProbes = @{
            'dotnet-version' = [pscustomobject]@{ Available = $true; Output = '10.0.401'; ExitCode = 0; Error = '' }
            'dotnet-tools' = [pscustomobject]@{ Available = $true; Output = 'Resolver metadata and executable paths verified.'; ExitCode = 0; Error = '' }
            'git-root' = [pscustomobject]@{ Available = $true; Output = $fixtureRoot; ExitCode = 0; Error = '' }
            'docker-ostype' = [pscustomobject]@{ Available = $true; Output = 'linux'; ExitCode = 0; Error = '' }
            'node-version' = [pscustomobject]@{ Available = $true; Output = 'v22.0.0'; ExitCode = 0; Error = '' }
            'npm-version' = [pscustomobject]@{ Available = $true; Output = '10.0.0'; ExitCode = 0; Error = '' }
            'git-remote' = [pscustomobject]@{ Available = $true; Output = 'https://github.com/example/repo.git'; ExitCode = 0; Error = '' }
            'github-repository' = [pscustomobject]@{ Available = $true; Output = '{"nameWithOwner":"example/repo"}'; ExitCode = 0; Error = '' }
        }
    }

    It 'reports core prerequisites ready without claiming tests passed' {
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $readyProbes

        $report.Status | Should -Be 'READY'
        $report.IsPrerequisiteReady | Should -BeTrue
        @($report.Checks | Where-Object State -EQ 'missing').Count | Should -Be 0
        $report.PSObject.Properties.Name | Should -Not -Contain 'TestsPassed'
    }

    It 'requires PowerShell 7 or later' {
        (InModuleScope AgentDoctor { Test-DoctorPowerShellVersion -Version ([version]'6.2') }) | Should -BeFalse
        (InModuleScope AgentDoctor { Test-DoctorPowerShellVersion -Version ([version]'7.0') }) | Should -BeTrue
    }

    It 'reports docs readiness independently of Docker' {
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Docs -ProbeOverrides $readyProbes

        $report.Status | Should -Be 'READY'
        @($report.Checks | Where-Object Name -EQ 'docker-linux').Count | Should -Be 0
        @($report.Checks | Where-Object Name -EQ 'spring-profile').State | Should -Be 'not-required'
    }

    It 'treats repository paths with wildcard characters literally' {
        $wildcardRoot = Join-Path $TestDrive 'doctor[fixture]'
        [System.IO.Directory]::CreateDirectory((Join-Path $wildcardRoot '.config')) | Out-Null
        [System.IO.Directory]::CreateDirectory((Join-Path $wildcardRoot 'docs/Docusaurus')) | Out-Null
        Set-Content -LiteralPath (Join-Path $wildcardRoot 'global.json') -Value '{"sdk":{"version":"10.0.400"}}'
        Set-Content -LiteralPath (Join-Path $wildcardRoot '.config/dotnet-tools.json') -Value '{"version":1,"tools":{"example":{"version":"1.0.0","commands":["example"]}}}'
        Set-Content -LiteralPath (Join-Path $wildcardRoot 'docs/Docusaurus/package.json') -Value '{}'
        Set-Content -LiteralPath (Join-Path $wildcardRoot 'docs/Docusaurus/package-lock.json') -Value '{}'

        $report = Get-AgentDoctorReport -RepositoryRoot $wildcardRoot -Profile Docs -ProbeOverrides $readyProbes

        @($report.Checks | Where-Object Name -EQ 'docs-manifests').State | Should -Be 'ready'
    }

    It 'reports Spring Docker failure as an explicit prerequisite failure' {
        $probes = @{} + $readyProbes
        $probes['docker-ostype'] = [pscustomobject]@{ Available = $true; Output = 'windows'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Spring -ProbeOverrides $probes

        $report.Status | Should -Be 'INCOMPLETE'
        @($report.Checks | Where-Object Name -EQ 'docker-linux').State | Should -Be 'unsupported'
        $report.RequiredFailures | Should -Contain 'docker-linux'
    }

    It 'distinguishes an SDK mismatch' {
        $probes = @{} + $readyProbes
        $probes['dotnet-version'] = [pscustomobject]@{ Available = $true; Output = '9.0.100'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'dotnet-sdk').State | Should -Be 'unsupported'
        $report.RequiredFailures | Should -Contain 'dotnet-sdk'
    }

    It 'honors a latest-feature SDK roll-forward' {
        $globalJsonPath = Join-Path $fixtureRoot 'global.json'
        $originalGlobalJson = Get-Content -LiteralPath $globalJsonPath -Raw
        try {
            Set-Content -LiteralPath $globalJsonPath -Value '{"sdk":{"version":"10.0.400","rollForward":"latestFeature"}}'
            $probes = @{} + $readyProbes
            $probes['dotnet-version'] = [pscustomobject]@{ Available = $true; Output = '10.0.500'; ExitCode = 0; Error = '' }
            $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes
        }
        finally {
            Set-Content -LiteralPath $globalJsonPath -Value $originalGlobalJson
        }

        @($report.Checks | Where-Object Name -EQ 'dotnet-sdk').State | Should -Be 'ready'
    }

    It 'accepts a prerelease SDK selected exactly by global.json' {
        InModuleScope AgentDoctor {
            Test-DoctorSdkCompatibility -Expected '10.0.100-preview.1' -Actual '10.0.100-preview.1' -RollForward 'patch' | Should -BeTrue
        }
    }

    It 'rejects an unsupported Docs Node version' {
        $probes = @{} + $readyProbes
        $probes['node-version'] = [pscustomobject]@{ Available = $true; Output = 'v18.20.0'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Docs -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'node').State | Should -Be 'unsupported'
        $report.RequiredFailures | Should -Contain 'node'
    }

    It 'reports an incomplete global.json without throwing' {
        $globalJsonPath = Join-Path $fixtureRoot 'global.json'
        $originalGlobalJson = Get-Content -LiteralPath $globalJsonPath -Raw
        try {
            Set-Content -LiteralPath $globalJsonPath -Value '{"sdk":{}}'
            $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $readyProbes
        }
        finally {
            Set-Content -LiteralPath $globalJsonPath -Value $originalGlobalJson
        }

        $report.Status | Should -Be 'INCOMPLETE'
        @($report.Checks | Where-Object Name -EQ 'global.json').State | Should -Be 'unsupported'
        $report.RequiredFailures | Should -Contain 'global.json'
    }

    It 'distinguishes declared but unrestored local tools' {
        $probes = @{} + $readyProbes
        $probes['dotnet-tools'] = [pscustomobject]@{ Available = $true; Output = 'Run "dotnet tool restore" to restore local tool metadata.'; ExitCode = 1; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'dotnet-tools').State | Should -Be 'missing'
        @($report.Checks | Where-Object Name -EQ 'dotnet-tools').Details | Should -Match 'dotnet tool restore'
        $report.RequiredFailures | Should -Contain 'dotnet-tools'
    }

    It 'reports a tool manifest without a tools object' {
        $manifestPath = Join-Path $fixtureRoot '.config/dotnet-tools.json'
        $originalManifest = Get-Content -LiteralPath $manifestPath -Raw
        try {
            Set-Content -LiteralPath $manifestPath -Value '{"version":1}'
            $probes = @{} + $readyProbes
            $probes.Remove('dotnet-tools')
            $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes
        }
        finally {
            Set-Content -LiteralPath $manifestPath -Value $originalManifest
        }

        $report.Status | Should -Be 'INCOMPLETE'
        @($report.Checks | Where-Object Name -EQ 'dotnet-tools-manifest').State | Should -Be 'unsupported'
        @($report.Checks | Where-Object Name -EQ 'dotnet-tools').State | Should -Be 'unsupported'
    }

    It 'indexes restored tool metadata by declared command name' {
        $manifestPath = Join-Path $fixtureRoot '.config/dotnet-tools.json'
        $originalManifest = Get-Content -LiteralPath $manifestPath -Raw
        $originalHome = $env:DOTNET_CLI_HOME
        $toolHome = Join-Path $TestDrive 'dotnet-home'
        $resolverRoot = Join-Path $toolHome '.dotnet/toolResolverCache/1'
        $executable = Join-Path $toolHome 'example-cli.exe'
        try {
            New-Item -ItemType Directory -Path $resolverRoot -Force | Out-Null
            Set-Content -LiteralPath $executable -Value 'fixture executable'
            $record = @([pscustomobject]@{ Version = '1.0.0'; Commands = @([pscustomobject]@{ PathToExecutable = $executable }) })
            $record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $resolverRoot 'example-cli')
            Set-Content -LiteralPath $manifestPath -Value '{"version":1,"tools":{"Example.Package":{"version":"1.0.0","commands":["example-cli"]}}}'
            $env:DOTNET_CLI_HOME = $toolHome
            $probes = @{} + $readyProbes
            $probes.Remove('dotnet-tools')
            $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes
        }
        finally {
            Set-Content -LiteralPath $manifestPath -Value $originalManifest
            if ($null -eq $originalHome) { Remove-Item Env:DOTNET_CLI_HOME -ErrorAction SilentlyContinue } else { $env:DOTNET_CLI_HOME = $originalHome }
        }

        @($report.Checks | Where-Object Name -EQ 'dotnet-tools').State | Should -Be 'ready'
    }

    It 'reports denied GitHub access as unknown without exposing credentials' {
        $probes = @{} + $readyProbes
        $probes['github-repository'] = [pscustomobject]@{ Available = $true; Output = ''; ExitCode = 1; Error = 'permission denied' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -ProbeOverrides $probes

        $report.Status | Should -Be 'INCOMPLETE'
        @($report.Checks | Where-Object Name -EQ 'github-repository').State | Should -Be 'unknown'
        ($report | ConvertTo-Json -Depth 8) | Should -Not -Match 'token|secret|credential'
    }

    It 'reports a missing Git executable before GitHub identity' {
        $probes = @{} + $readyProbes
        $probes['git-remote'] = [pscustomobject]@{ Available = $false; Output = ''; ExitCode = 127; Error = "Command 'git' was not found." }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'github-repository').State | Should -Be 'missing'
        @($report.Checks | Where-Object Name -EQ 'github-repository').Details | Should -Match "Command 'git' was not found"
    }

    It 'parses URI-style SSH GitHub remotes' {
        $remote = InModuleScope AgentDoctor { ConvertTo-DoctorGitHubRemote -Remote 'ssh://git@github.com/owner/repo.git' }

        $remote.Parsed | Should -BeTrue
        $remote.Host | Should -Be 'github.com'
        $remote.Target | Should -Be 'github.com/owner/repo'
    }

    It 'rejects an untrusted GitHub remote before invoking gh' {
        $probes = @{} + $readyProbes
        $probes['git-remote'] = [pscustomobject]@{ Available = $true; Output = 'https://attacker.invalid/example/repo.git'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'github-repository').State | Should -Be 'unsupported'
        @($report.Checks | Where-Object Name -EQ 'github-repository').Details | Should -Match 'not trusted'
    }

    It 'requires explicit trust for an enterprise SSH remote' {
        $probes = @{} + $readyProbes
        $probes['git-remote'] = [pscustomobject]@{ Available = $true; Output = 'ssh://git@ghe.example/owner/repo.git'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -TrustedGitHubHost 'ghe.example' -ProbeOverrides $probes

        @($report.Checks | Where-Object Name -EQ 'github-repository').State | Should -Be 'ready'
    }

    It 'preserves native output when a probe exits unsuccessfully' {
        $probes = @{} + $readyProbes
        $probes['git-root'] = [pscustomobject]@{ Available = $true; Output = 'fatal: not a git repository'; ExitCode = 128; Error = '' }
        $probes['github-repository'] = [pscustomobject]@{ Available = $true; Output = 'gh: authentication required'; ExitCode = 1; Error = '' }

        $coreReport = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $probes
        $githubReport = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -ProbeOverrides $probes

        @($coreReport.Checks | Where-Object Name -EQ 'git-worktree').Details | Should -Be 'fatal: not a git repository'
        @($githubReport.Checks | Where-Object Name -EQ 'github-repository').Details | Should -Be 'gh: authentication required'
    }

    It 'keeps stderr separate from semantic probe output' {
        $probe = InModuleScope AgentDoctor {
            Invoke-DoctorProbe -Name 'stderr-test' -FilePath 'pwsh' -Arguments @('-NoProfile', '-Command', "[Console]::Error.WriteLine('warning'); Write-Output 'value'")
        }

        $probe.Output | Should -Be 'value'
        $probe.Error | Should -Be 'warning'
    }

    It 'includes remediation in the text report for incomplete checks' {
        $probes = @{} + $readyProbes
        $probes['docker-ostype'] = [pscustomobject]@{ Available = $true; Output = 'windows'; ExitCode = 0; Error = '' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Spring -ProbeOverrides $probes
        $text = Format-AgentDoctorText -Report $report | Out-String

        $text | Should -Match 'Remediation: Start Docker with Linux containers'
    }

    It 'omits remediation from successful checks' {
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $readyProbes
        $text = Format-AgentDoctorText -Report $report | Out-String

        $text | Should -Not -Match 'Remediation:'
    }

    It 'bounds a hanging external probe' {
        $probe = InModuleScope AgentDoctor {
            Invoke-DoctorProbe -Name 'timeout-test' -FilePath 'pwsh' -Arguments @('-NoProfile', '-Command', 'Start-Sleep -Seconds 3') -TimeoutSeconds 1
        }

        $probe.TimedOut | Should -BeTrue
        $probe.ExitCode | Should -Be 124
        $probe.Error | Should -Match 'timed out after 1 seconds'
    }

    It 'resolves PowerShell command shims through a compatible host' {
        if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
            Set-ItResult -Skipped -Because 'npm is required to exercise the command-shim resolution path.'
            return
        }

        $resolved = InModuleScope AgentDoctor { Resolve-DoctorCommand -FilePath 'npm' }

        $commands = @(Get-Command npm -All -ErrorAction SilentlyContinue)
        if (@($commands | Where-Object { [System.IO.Path]::GetExtension($_.Source) -ieq '.ps1' }).Count -gt 0) {
            $resolved.FilePath | Should -Not -Match '\.ps1$'
            $resolved.PrefixArguments | Should -Contain '-NoProfile'
        }
    }

    It 'does not mutate the checkout while probing' {
        $marker = Join-Path $fixtureRoot 'marker.txt'
        Set-Content -LiteralPath $marker -Value 'unchanged'
        $before = (Get-FileHash -LiteralPath $marker).Hash
        $null = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Core -ProbeOverrides $readyProbes
        $after = (Get-FileHash -LiteralPath $marker).Hash

        $after | Should -Be $before
        (Get-Content -LiteralPath $marker -Raw).Trim() | Should -Be 'unchanged'
    }
}
