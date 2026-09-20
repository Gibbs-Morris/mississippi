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
        @($outcome.Plan.Steps | Where-Object Name -EQ 'install-markdownlint').Arguments | Should -Contain '--prefix'
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

    It 'executes the Docs profile with controlled node and npm shims' {
        $shimRoot = Join-Path $TestDrive 'setup-shims'
        New-Item -ItemType Directory -Path $shimRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $shimRoot 'node.ps1') -Value "Write-Output 'v22.0.0'"
        Set-Content -LiteralPath (Join-Path $shimRoot 'npm.ps1') -Value 'exit 0'
        $originalPath = $env:PATH
        $env:PATH = $shimRoot + [IO.Path]::PathSeparator + $originalPath
        try {
            $output = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $repoRoot -Profile Docs -OutputFormat Json 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
        }
        finally {
            $env:PATH = $originalPath
        }

        $exitCode | Should -Be 0
        ($output | ConvertFrom-Json).Status | Should -Be 'READY'
    }

    It 'fails without READY when a setup child exits nonzero' {
        $shimRoot = Join-Path $TestDrive 'setup-failure-shims'
        New-Item -ItemType Directory -Path $shimRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $shimRoot 'node.ps1') -Value "Write-Output 'v22.0.0'"
        Set-Content -LiteralPath (Join-Path $shimRoot 'npm.ps1') -Value 'exit 7'
        $originalPath = $env:PATH
        $env:PATH = $shimRoot + [IO.Path]::PathSeparator + $originalPath
        try {
            $output = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $repoRoot -Profile Docs -OutputFormat Json 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
        }
        finally {
            $env:PATH = $originalPath
        }

        $exitCode | Should -Not -Be 0
        $output | Should -Not -Match '"Status"\s*:\s*"READY"'
    }
}
