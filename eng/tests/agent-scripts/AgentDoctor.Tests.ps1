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
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.config/dotnet-tools.json') -Value '{"version":1,"tools":{"example":{"version":"1.0.0"}}}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/Docusaurus/package.json') -Value '{}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/Docusaurus/package-lock.json') -Value '{}'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'samples/Spring/Spring.AppHost/Spring.AppHost.csproj') -Value '<Project />'

        $readyProbes = @{
            'dotnet-version' = [pscustomobject]@{ Available = $true; Output = '10.0.401'; ExitCode = 0; Error = '' }
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

    It 'reports docs readiness independently of Docker' {
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile Docs -ProbeOverrides $readyProbes

        $report.Status | Should -Be 'READY'
        @($report.Checks | Where-Object Name -EQ 'docker-linux').Count | Should -Be 0
        @($report.Checks | Where-Object Name -EQ 'spring-profile').State | Should -Be 'not-required'
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

    It 'reports denied GitHub access as unknown without exposing credentials' {
        $probes = @{} + $readyProbes
        $probes['github-repository'] = [pscustomobject]@{ Available = $true; Output = ''; ExitCode = 1; Error = 'permission denied' }
        $report = Get-AgentDoctorReport -RepositoryRoot $fixtureRoot -Profile GitHub -ProbeOverrides $probes

        $report.Status | Should -Be 'INCOMPLETE'
        @($report.Checks | Where-Object Name -EQ 'github-repository').State | Should -Be 'unknown'
        ($report | ConvertTo-Json -Depth 8) | Should -Not -Match 'token|secret|credential'
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
