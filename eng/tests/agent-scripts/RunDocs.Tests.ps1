#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Documentation runtime prerequisites' {
    BeforeAll {
        $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
        $scriptPath = Join-Path $repoRoot 'run-docs.ps1'

        function Invoke-DocsScript {
            param([string]$Version = 'v24.0.0', [int]$NodeExit = 0, [switch]$MissingNode, [switch]$OmitNodeExit)

            $shimRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $shimRoot | Out-Null
            $npmLog = Join-Path $shimRoot 'npm-arguments.json'
            if (-not $MissingNode) {
                $nodeScript = "Write-Output '$Version'" + [Environment]::NewLine + "exit $NodeExit"
                if ($OmitNodeExit) { $nodeScript = "Write-Output '$Version'" }
                Set-Content -LiteralPath (Join-Path $shimRoot 'node.ps1') -Value $nodeScript
            }
            Set-Content -LiteralPath (Join-Path $shimRoot 'npm.ps1') -Value ('$args | ConvertTo-Json -AsArray -Compress | Set-Content -LiteralPath $env:MISSISSIPPI_RUN_DOCS_TEST_LOG' + [Environment]::NewLine + 'exit 0')
            $originalPath = $env:PATH
            $originalLog = $env:MISSISSIPPI_RUN_DOCS_TEST_LOG
            $env:PATH = $shimRoot + [IO.Path]::PathSeparator + $PSHOME
            $env:MISSISSIPPI_RUN_DOCS_TEST_LOG = $npmLog
            try {
                try {
                    $output = & pwsh -NoProfile -File $scriptPath -Mode Build -SkipInstall 2>&1 | Out-String
                    $exitCode = $LASTEXITCODE
                }
                catch {
                    $output = $_.Exception.Message
                    $exitCode = 1
                }
                $npmArguments = if (Test-Path -LiteralPath $npmLog) {
                    @(Get-Content -LiteralPath $npmLog -Raw | ConvertFrom-Json)
                }
                else {
                    @()
                }
                [pscustomobject]@{ ExitCode = $exitCode; Output = $output; NpmArguments = @($npmArguments) }
            }
            finally {
                $env:PATH = $originalPath
                $env:MISSISSIPPI_RUN_DOCS_TEST_LOG = $originalLog
            }
        }
    }

    It 'rejects a missing node executable before invoking npm' {
        $outcome = Invoke-DocsScript -MissingNode

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js was not found on PATH'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'rejects unsupported or malformed node output <Version> before invoking npm' -ForEach @(
        @{ Version = 'v20.0.0' }
        @{ Version = 'v22.0.0' }
        @{ Version = 'v23.0.0' }
        @{ Version = 'not-a-version' }
        @{ Version = 'v24.0' }
    ) {
        $outcome = Invoke-DocsScript -Version $Version

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js 24 or newer is required'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'rejects a failed node version command even when its output is supported' {
        $outcome = Invoke-DocsScript -NodeExit 7

        $outcome.ExitCode | Should -Be 1
        $outcome.Output | Should -Match 'Node.js 24 or newer is required'
        $outcome.NpmArguments.Count | Should -Be 0
    }

    It 'runs npm when a supported PowerShell node shim has no explicit exit' {
        $outcome = Invoke-DocsScript -OmitNodeExit

        $outcome.ExitCode | Should -Be 0
        $outcome.NpmArguments.Count | Should -Be 2
        $outcome.NpmArguments[0] | Should -Be 'run'
        $outcome.NpmArguments[1] | Should -Be 'build'
    }

    It 'runs the requested npm command for supported node version <Version>' -ForEach @(
        @{ Version = 'v24.0.0' }
        @{ Version = 'v25.0.0' }
        @{ Version = '24.0.0' }
    ) {
        $outcome = Invoke-DocsScript -Version $Version

        $outcome.ExitCode | Should -Be 0
        $outcome.NpmArguments.Count | Should -Be 2
        $outcome.NpmArguments[0] | Should -Be 'run'
        $outcome.NpmArguments[1] | Should -Be 'build'
    }
}
