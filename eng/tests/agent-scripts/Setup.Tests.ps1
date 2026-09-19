#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Canonical repository setup' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $scriptPath = Join-Path $repoRoot 'setup.ps1'
        function Get-SetupPlan {
            param([string]$Profile, [switch]$InstallPester)
            $json = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $repoRoot -Profile $Profile -InstallPester:$InstallPester -PlanOnly 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Plan = $json | ConvertFrom-Json }
        }
    }

    It 'plans locked core and sample restores without mutating the checkout' {
        $outcome = Get-SetupPlan -Profile Core

        $outcome.ExitCode | Should -Be 0
        @($outcome.Plan.Steps | Where-Object Name -EQ 'restore-core-solution').Arguments | Should -Contain '--locked-mode'
        @($outcome.Plan.Steps | Where-Object Name -EQ 'restore-sample-solution').Arguments | Should -Contain '--locked-mode'
        @($outcome.Plan.Steps | Where-Object Name -EQ 'install-pester').Count | Should -Be 0
    }

    It 'plans docs restore from the lockfile and no lifecycle scripts' {
        $outcome = Get-SetupPlan -Profile Docs

        $outcome.ExitCode | Should -Be 0
        @($outcome.Plan.Steps | Where-Object Name -EQ 'restore-docs').Arguments | Should -Contain 'ci'
        @($outcome.Plan.Steps | Where-Object Name -EQ 'restore-docs').Arguments | Should -Contain '--ignore-scripts'
        @($outcome.Plan.Steps | Where-Object Name -EQ 'install-markdownlint').Arguments | Should -Contain 'markdownlint-cli@0.45.0'
    }

    It 'keeps browser setup diagnostic-only' {
        $outcome = Get-SetupPlan -Profile Browser

        $outcome.ExitCode | Should -Be 0
        @($outcome.Plan.Steps | Where-Object Name -EQ 'browser-doctor').Arguments | Should -Contain '-Doctor'
        @($outcome.Plan.Steps | Where-Object Executable -EQ 'docker').Count | Should -Be 0
    }

    It 'makes Pester installation explicit' {
        $outcome = Get-SetupPlan -Profile Core -InstallPester

        $outcome.ExitCode | Should -Be 0
        @($outcome.Plan.Steps | Where-Object Name -EQ 'install-pester').Count | Should -Be 1
    }

    It 'honors Pester installation for a non-core profile' {
        $outcome = Get-SetupPlan -Profile Docs -InstallPester

        $outcome.ExitCode | Should -Be 0
        @($outcome.Plan.Steps | Where-Object Name -EQ 'install-pester').Count | Should -Be 1
    }

    It 'shares the setup entry point from Copilot workflow source' {
        $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/copilot-setup-steps.yml') -Raw

        $workflow | Should -Match 'setup\.ps1'
        $workflow | Should -Not -Match 'dotnet tool restore'
    }
}
