#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Source-bound validation evidence' {
    BeforeAll {
        $modulePath = Join-Path $PSScriptRoot '../../src/agent-scripts/ValidationEvidence.psm1'
        Import-Module $modulePath -Force
        $fixtureRoot = Join-Path $TestDrive 'evidence-repository'
        New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
        $inputFile = Join-Path $fixtureRoot 'source.cs'
        Set-Content -LiteralPath $inputFile -Value 'class Source { }'
    }

    It 'reads the Git revision in a fresh strict-mode PowerShell process' {
        $repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
        $scriptPath = Join-Path $TestDrive 'fresh-revision.ps1'
        Set-Content -LiteralPath $scriptPath -Value @'
param([string]$ModulePath, [string]$RepositoryRoot)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module -Name $ModulePath -Force
$fingerprint = Get-ValidationSourceFingerprint -RepositoryRoot $RepositoryRoot -InputPath @('global.json')
if ($fingerprint.Revision -notmatch '^[0-9a-f]{40}$') { throw 'Expected a Git revision.' }
'@
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })

        $output = @(& $powerShellPath -NoProfile -File $scriptPath $modulePath $repoRoot 2>&1)

        $LASTEXITCODE | Should -Be 0 -Because ($output -join [Environment]::NewLine)
    }

    It 'uses unknown when the evidence root is not a Git checkout' {
        $fingerprint = Get-ValidationSourceFingerprint -RepositoryRoot $fixtureRoot -InputPath @($inputFile)

        $fingerprint.Revision | Should -Be 'unknown'
    }

    It 'writes and verifies a passing result from the exact source fingerprint' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'result.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun><ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeTrue
        $result.Fresh | Should -BeTrue
        $result.Record.Status | Should -Be 'PASS'
    }

    It 'rejects a TRX artifact without completed execution counters' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'incomplete.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun />'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'completed ResultSummary|execution counters'
    }

    It 'rejects PASS evidence with a nonzero exit code' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'nonzero-exit.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun><ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 7 -ArtifactPath @($artifact) | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        $result.Errors | Should -Contain 'PASS evidence requires a zero exit code.'
    }

    It 'rejects PASS evidence with a null exit code' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'null-exit.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun><ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null
        $record = Get-Content -LiteralPath $run.Path -Raw | ConvertFrom-Json
        $record.ExitCode = $null
        $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $run.Path

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        $result.Errors | Should -Contain 'PASS evidence requires a numeric zero exit code.'
    }

    It 'rejects inconsistent TRX counters' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'inconsistent-counters.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun><ResultSummary outcome="Completed"><Counters total="0" executed="1" passed="0" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'inconsistent execution counters'
    }

    It 'invalidates a report after a source edit' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 | Out-Null
        Add-Content -LiteralPath $inputFile -Value 'class Changed { }'

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        $result.Errors | Should -Contain 'Evidence inputs are stale or changed.'
    }

    It 'refuses PASS when the source changes during the run' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        Add-Content -LiteralPath $inputFile -Value 'class CleanupChanged { }'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 | Out-Null

        (Get-Content -LiteralPath $run.Path -Raw | ConvertFrom-Json).Status | Should -Be 'INCOMPLETE'
    }

    It 'fails closed when a required artifact is missing' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @(Join-Path $fixtureRoot 'missing.trx') | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'Required artifact is missing'
    }

    It 'keeps READY and SKIPPED distinct from executed PASS' {
        $ready = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'prerequisite' -InputPath @($inputFile)
        Complete-ValidationEvidenceRun -Run $ready -Status READY -Phase prerequisites -Executed $false -TestCount 0 -ExitCode 0 | Out-Null
        $skipped = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'optional' -InputPath @($inputFile)
        Complete-ValidationEvidenceRun -Run $skipped -Status SKIPPED -Phase optional -Executed $false -TestCount 0 -ExitCode 0 | Out-Null

        (Get-Content -LiteralPath $ready.Path -Raw | ConvertFrom-Json).Status | Should -Be 'READY'
        (Get-Content -LiteralPath $skipped.Path -Raw | ConvertFrom-Json).Status | Should -Be 'SKIPPED'
        (Get-Content -LiteralPath $ready.Path -Raw | ConvertFrom-Json).Executed | Should -BeFalse
    }

    It 'rejects malformed evidence' {
        $path = Join-Path $fixtureRoot 'malformed.json'
        Set-Content -LiteralPath $path -Value '{not-json'

        $result = Test-ValidationEvidence -Path $path

        $result.Valid | Should -BeFalse
        $result.Errors | Should -Contain 'Evidence is missing or unreadable.'
    }

    It 'rejects malformed referenced JSON artifacts' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'summary.json'
        Set-Content -LiteralPath $artifact -Value '{broken'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'malformed or unreadable'
    }

    It 'fails closed without throwing for malformed nested artifact metadata' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $artifact = Join-Path $fixtureRoot 'nested-metadata.trx'
        Set-Content -LiteralPath $artifact -Value '<TestRun />'
        Complete-ValidationEvidenceRun -Run $run -Status PASS -Phase complete -Executed $true -TestCount 1 -ExitCode 0 -ArtifactPath @($artifact) | Out-Null
        $record = Get-Content -LiteralPath $run.Path -Raw | ConvertFrom-Json
        $record.ArtifactMetadata = @([pscustomobject]@{ Path = 'nested-metadata.trx' })
        $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $run.Path

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'metadata'
    }

    It 'rejects artifact paths outside the verification root' {
        $run = New-ValidationEvidenceRun -RepositoryRoot $fixtureRoot -Scope 'fixture' -InputPath @($inputFile)
        $record = Get-Content -LiteralPath $run.Path -Raw | ConvertFrom-Json
        $record.Status = 'FAIL'
        $record.Executed = $false
        $record.TestCount = 0
        $record.Artifacts = @('../../outside.trx')
        $record.ArtifactMetadata = @([pscustomobject]@{ Path = '../../outside.trx'; SHA256 = 'SHA256:' + ('0' * 64); Length = 1 })
        $record.SourceAfter = $record.SourceBefore
        $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $run.Path

        $result = Test-ValidationEvidence -Path $run.Path

        $result.Valid | Should -BeFalse
        ($result.Errors -join "`n") | Should -Match 'escapes the verification root'
    }
}
