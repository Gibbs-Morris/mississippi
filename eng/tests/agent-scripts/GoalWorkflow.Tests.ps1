#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Issue-driven goal workflow' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $fixtureRepoRoot = Join-Path $TestDrive 'goal-fixture-repo'
        $fixtureScriptDirectory = Join-Path $fixtureRepoRoot 'eng/src/agent-scripts'
        New-Item -ItemType Directory -Path $fixtureScriptDirectory -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $fixtureRepoRoot '.github/instructions') -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot 'eng/src/agent-scripts/invoke-github-issue-goal.ps1') -Destination $fixtureScriptDirectory
        Copy-Item -LiteralPath (Join-Path $repoRoot 'eng/src/agent-scripts/test-issue-spec.ps1') -Destination $fixtureScriptDirectory
        Set-Content -LiteralPath (Join-Path $fixtureRepoRoot '.github/instructions/issue-tracking.instructions.md') -Value '# Fixture issue tracking guidance' -Encoding utf8
        Set-Content -LiteralPath (Join-Path $fixtureRepoRoot 'README.md') -Value '# Fixture repository' -Encoding utf8
        Set-Content -LiteralPath (Join-Path $fixtureRepoRoot 'fixture.txt') -Value 'base' -Encoding utf8
        $fixtureSafeRoot = $fixtureRepoRoot.Replace('\', '/')
        $fixtureGit = @('-c', "safe.directory=$fixtureSafeRoot", '-C', $fixtureRepoRoot)
        & git @fixtureGit init --quiet | Out-Null
        & git @fixtureGit config user.email 'pester-fixture@example.invalid' | Out-Null
        & git @fixtureGit config user.name 'Pester fixture' | Out-Null
        & git @fixtureGit remote add origin 'https://github.com/Gibbs-Morris/mississippi.git' | Out-Null
        & git @fixtureGit add -- . | Out-Null
        & git @fixtureGit commit --quiet -m 'fixture base' | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRepoRoot 'fixture.txt') -Value 'middle' -Encoding utf8
        & git @fixtureGit add -- fixture.txt | Out-Null
        & git @fixtureGit commit --quiet -m 'fixture middle' | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRepoRoot 'fixture.txt') -Value 'head' -Encoding utf8
        & git @fixtureGit add -- fixture.txt | Out-Null
        & git @fixtureGit commit --quiet -m 'fixture head' | Out-Null
        $scriptPath = Join-Path $fixtureRepoRoot 'eng/src/agent-scripts/invoke-github-issue-goal.ps1'
        $checkpoint = Join-Path $TestDrive 'goals/741/checkpoint.json'
        $headCommit = (& git @fixtureGit rev-parse HEAD).Trim()
        $baseCommit = (& git @fixtureGit rev-parse HEAD~1).Trim()
        $validIssueBody = Get-Content -LiteralPath (Join-Path $repoRoot 'eng/tests/agent-scripts/fixtures/issue-spec-bug-fix.md') -Raw
        function New-IssueJson {
            param(
                [Parameter(Mandatory)][string]$Body,
                [string]$Url = 'https://github.com/Gibbs-Morris/mississippi/issues/741',
                [AllowNull()][object[]]$Comments
            )
            $issue = [ordered]@{
                number = 741
                title = '[Task] Goal route fixture'
                state = 'open'
                body = $Body
                html_url = $Url
            } | ConvertTo-Json -Depth 6 -Compress
            if ($null -ne $Comments) {
                $issue = [ordered]@{
                    number = 741
                    title = '[Task] Goal route fixture'
                    state = 'open'
                    body = $Body
                    html_url = $Url
                    comments = $Comments
                } | ConvertTo-Json -Depth 6 -Compress
            }
            return $issue
        }
        function Invoke-Goal {
            param(
                [ValidateSet('start', 'resume')][string]$Action = 'start',
                [string]$Body = $validIssueBody,
                [string]$IssueUrl = 'https://github.com/Gibbs-Morris/mississippi/issues/741',
                [string]$Head = $headCommit,
                [string]$Base = $baseCommit,
                [string]$Operation = '',
                [object[]]$Comments,
                [string]$ExpectedDigest = '',
                [string]$ExpectedContractDigest = '',
                [string[]]$ExpectedAcceptance,
                [string[]]$Decision,
                [string[]]$Evidence,
                [string[]]$Fix,
                [string[]]$Review,
                [string[]]$ResolvedReview,
                [switch]$OmitAuthorization,
                [switch]$EvidenceValidated
            )
            $arguments = @('-NoProfile', '-File', $scriptPath, '-Action', $Action,
                '-RepositoryOwner', 'Gibbs-Morris', '-RepositoryName', 'mississippi',
                '-IssueNumber', '741', '-IssueJson', (New-IssueJson -Body $Body -Url $IssueUrl -Comments $Comments),
                '-CheckpointPath', $checkpoint, '-RepositoryRoot', $fixtureRepoRoot,
                '-HeadRevision', $Head, '-BaseRevision', $Base, '-Json')
            if ($Operation) { $arguments += @('-OperationStateJson', $Operation) }
            if ($ExpectedContractDigest) { $arguments += @('-ExpectedContractBodyDigest', $ExpectedContractDigest) }
            if ($Decision) { $arguments += @('-Decisions', (ConvertTo-Json -InputObject ([object[]]$Decision) -Compress)) }
            if ($Evidence) { $arguments += @('-AcceptanceEvidence', (ConvertTo-Json -InputObject ([object[]]$Evidence) -Compress)) }
            if ($Fix) { $arguments += @('-AttemptedFixes', (ConvertTo-Json -InputObject ([object[]]$Fix) -Compress)) }
            if ($Review) { $arguments += @('-OutstandingReviewWork', (ConvertTo-Json -InputObject ([object[]]$Review) -Compress)) }
            if ($ResolvedReview) { $arguments += @('-ResolvedReviewWork', (ConvertTo-Json -InputObject ([object[]]$ResolvedReview) -Compress)) }
            if (-not $OmitAuthorization) {
                if (-not $ExpectedDigest) {
                    $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
                    $bodyHash = [System.Security.Cryptography.SHA256]::HashData($bodyBytes)
                    $ExpectedDigest = 'SHA256:' + (($bodyHash | ForEach-Object { $_.ToString('x2') }) -join '')
                }
                if (-not $ExpectedAcceptance) {
                    $ExpectedAcceptance = @([regex]::Matches($Body, '(?m)^\s*-\s*\[(AC\d+)\]') | ForEach-Object { $_.Groups[1].Value })
                }
            }
            if ($ExpectedDigest) { $arguments += @('-ExpectedIssueBodyDigest', $ExpectedDigest) }
            if ($ExpectedAcceptance) { $arguments += @('-ExpectedAcceptanceCriteria', (ConvertTo-Json -InputObject ([object[]]$ExpectedAcceptance) -Compress)) }
            if ($EvidenceValidated) { $arguments += '-EvidenceValidated' }
            $output = & $powerShellPath @arguments 2>&1 | Out-String
            [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output; Result = $output | ConvertFrom-Json }
        }
    }

    BeforeEach {
        Remove-Item -LiteralPath $checkpoint -Force -ErrorAction SilentlyContinue
    }

    It 'starts from one issue and records a non-authorizing checkpoint' {
        $outcome = Invoke-Goal

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'started'
        Test-Path -LiteralPath $checkpoint -PathType Leaf | Should -BeTrue
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
        $saved.Contract.IssueTextIsData | Should -BeTrue
        $saved.Contract.CommandsExecutedFromIssueText | Should -BeFalse
        @($saved.Contract.AcceptanceCriteria).Count | Should -Be 3
        $saved.Contract.DependenciesAndReadiness | Should -Not -BeNullOrEmpty
        $saved.MergeBoundary | Should -Be 'PR_READY_NOT_MERGED'
    }

    It 'resumes without claiming evidence is fresh before revalidation' {
        $null = Invoke-Goal
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'resumed'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'detects edited issue scope before implementation continues' {
        $null = Invoke-Goal
        $changedBody = $validIssueBody -replace 'Empty identifiers', 'Changed identifiers'
        $outcome = Invoke-Goal -Action resume -Body $changedBody

        $outcome.ExitCode | Should -Be 2
        $outcome.Result.Status | Should -Be 'scope-changed'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'invalidates evidence when the selected base revision changes' {
        $null = Invoke-Goal
        $outcome = Invoke-Goal -Action resume -Base 'HEAD~2'

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'evidence-stale'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'retains the original issue baseline until explicit reconciliation' {
        $null = Invoke-Goal
        $changedBody = $validIssueBody -replace 'Empty identifiers', 'Changed identifiers'
        $first = Invoke-Goal -Action resume -Body $changedBody
        $second = Invoke-Goal -Action resume -Body $changedBody

        $first.Result.Status | Should -Be 'scope-changed'
        $second.ExitCode | Should -Be 2
        $second.Result.Status | Should -Be 'scope-changed'
    }

    It 'promotes a changed baseline only after explicit revalidation' {
        $null = Invoke-Goal
        $null = Invoke-Goal -Action resume -Base 'HEAD~2'
        $outcome = Invoke-Goal -Action resume -Base 'HEAD~2' -EvidenceValidated

        $outcome.Result.EvidenceFresh | Should -BeTrue
        $outcome.Result.Status | Should -Be 'resumed'
    }

    It 'rejects validation for a head other than the checked-out revision' {
        $outcome = Invoke-Goal -Head 'HEAD~1'

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
        $outcome.Result.Error | Should -Match 'checked-out HEAD'
    }

    It 'waits on a running operation handle instead of duplicating it' {
        $operation = '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $null = Invoke-Goal -Operation $operation
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 2
        $outcome.Result.Status | Should -Be 'operation-running'
        $outcome.Result.NextAction | Should -Match 'job-123'
    }

    It 'rejects a running operation without an authoritative handle' {
        $outcome = Invoke-Goal -Operation '{"Status":"running","Name":"validation"}'

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
        $outcome.Result.Error | Should -Match 'nonempty authoritative handle'
    }

    It 'rejects an unknown operation status' {
        $outcome = Invoke-Goal -Operation '{"Status":"paused","Handle":"job-123","Name":"validation"}'

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
        $outcome.Result.Error | Should -Match 'Unsupported goal operation status'
    }

    It 'rejects a replacement handle for a running operation' {
        $null = Invoke-Goal -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $outcome = Invoke-Goal -Action resume -Operation '{"Status":"running","Handle":"job-999","Name":"validation"}'

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Error | Should -Match 'authoritative handle'
    }

    It 'prioritizes waiting on a running operation over edited scope' {
        $null = Invoke-Goal -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $changedBody = $validIssueBody -replace 'Empty identifiers', 'Changed identifiers'
        $outcome = Invoke-Goal -Action resume -Body $changedBody -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'

        $outcome.ExitCode | Should -Be 2
        $outcome.Result.Status | Should -Be 'operation-running'
        $outcome.Result.NextAction | Should -Match 'job-123'
    }

    It 'does not promote failed operation evidence' {
        $null = Invoke-Goal -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $outcome = Invoke-Goal -Action resume -Operation '{"Status":"failed","Handle":"job-123","Name":"validation"}' -EvidenceValidated

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Error | Should -Match 'failed operation evidence'
    }

    It 'rejects a completed operation after a failed operation' {
        $null = Invoke-Goal -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $failed = Invoke-Goal -Action resume -Operation '{"Status":"failed","Handle":"job-123","Name":"validation"}'
        $failed.Result.NextAction | Should -Be 'inspect-failed-operation-and-retry'
        $outcome = Invoke-Goal -Action resume -Operation '{"Status":"completed","Handle":"job-123","Name":"validation"}' -EvidenceValidated

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Error | Should -Match 'failed operation cannot transition directly to completed'
    }

    It 'does not promote completed operation evidence from a changed snapshot' {
        $null = Invoke-Goal -Operation '{"Status":"running","Handle":"job-123","Name":"validation"}'
        $outcome = Invoke-Goal -Action resume -Base 'HEAD~2' -Operation '{"Status":"completed","Handle":"job-123","Name":"validation"}' -EvidenceValidated

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'evidence-stale'
        $outcome.Result.EvidenceFresh | Should -BeFalse
    }

    It 'preserves the issue digest when a valid contract comes from a comment' {
        $comment = [pscustomobject]@{ body = $validIssueBody; created_at = '2026-09-19T00:00:00Z' }
        $invalidBody = 'The issue body is intentionally invalid.'
        $commentDigestBytes = [System.Text.Encoding]::UTF8.GetBytes($validIssueBody)
        $commentDigestHash = [System.Security.Cryptography.SHA256]::HashData($commentDigestBytes)
        $commentDigest = 'SHA256:' + (($commentDigestHash | ForEach-Object { $_.ToString('x2') }) -join '')
        $null = Invoke-Goal -Body $invalidBody -Comments @($comment) -ExpectedContractDigest $commentDigest -ExpectedAcceptance @('AC1', 'AC2', 'AC3')
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
        $expectedBytes = [System.Text.Encoding]::UTF8.GetBytes($invalidBody)
        $expectedHash = [System.Security.Cryptography.SHA256]::HashData($expectedBytes)
        $expectedDigest = 'SHA256:' + (($expectedHash | ForEach-Object { $_.ToString('x2') }) -join '')

        $saved.IssueBodyDigest | Should -Be $expectedDigest
        $saved.ContractSource | Should -Be 'issue-comment'
        $saved.ContractBodyDigest | Should -Not -Be $saved.IssueBodyDigest
    }

    It 'selects the fallback contract matching the authorized digest' {
        $olderBody = $validIssueBody
        $newerBody = $validIssueBody -replace 'Keep the existing exception type\.', 'Keep the existing exception type and message.'
        $digestBytes = [System.Text.Encoding]::UTF8.GetBytes($olderBody)
        $digestHash = [System.Security.Cryptography.SHA256]::HashData($digestBytes)
        $olderDigest = 'SHA256:' + (($digestHash | ForEach-Object { $_.ToString('x2') }) -join '')
        $comments = @(
            [pscustomobject]@{ body = $newerBody; created_at = '2026-09-20T00:00:00Z' }
            [pscustomobject]@{ body = $olderBody; created_at = '2026-09-19T00:00:00Z' }
        )
        $outcome = Invoke-Goal -Body 'The issue body is intentionally invalid.' -Comments $comments -ExpectedContractDigest $olderDigest -ExpectedAcceptance @('AC1', 'AC2', 'AC3')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'started'
    }

    It 'removes explicitly resolved review work from the checkpoint collection' {
        $null = Invoke-Goal -Review @('review-a', 'review-b')
        $outcome = Invoke-Goal -Action resume -ResolvedReview 'review-a'
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json

        $outcome.ExitCode | Should -Be 0
        @($saved.OutstandingReviewWork) | Should -Contain 'review-b'
        @($saved.OutstandingReviewWork) | Should -Not -Contain 'review-a'
    }

    It 'resolves review work using case-sensitive identities' {
        $null = Invoke-Goal -Review @('review-a', 'REVIEW-A')
        $null = Invoke-Goal -Action resume -ResolvedReview 'review-a'
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
        $remaining = @($saved.OutstandingReviewWork | ForEach-Object { [string]$_ })

        @($remaining | Where-Object { $_ -ceq 'review-a' }).Count | Should -Be 0
        @($remaining | Where-Object { $_ -ceq 'REVIEW-A' }).Count | Should -Be 1
    }

    It 'requires an authorized baseline for a first checkpoint' {
        $outcome = Invoke-Goal -OmitAuthorization

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
        $outcome.Result.Error | Should -Match 'first checkpoint requires an expected issue body digest'
    }

    It 'rejects issue URLs outside canonical github.com' {
        $outcome = Invoke-Goal -IssueUrl 'https://example.test/Gibbs-Morris/mississippi/issues/741'

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
        $outcome.Result.Error | Should -Match 'Issue repository mismatch'
    }

    It 'extracts dependencies after ignoring tilde-fenced examples' {
        $content = $validIssueBody -replace '(?m)^## Decisions and non-goals', ('~~~markdown' + [Environment]::NewLine + '### Dependencies and readiness' + [Environment]::NewLine + 'example only' + [Environment]::NewLine + '~~~' + [Environment]::NewLine + [Environment]::NewLine + '## Decisions and non-goals')
        $null = Invoke-Goal -Body $content
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json

        $saved.Contract.DependenciesAndReadiness | Should -Match 'parser project and its L0 test project'
        $saved.Contract.DependenciesAndReadiness | Should -Not -Match 'example only'
    }

    It 'binds a first checkpoint to the authorized local plan' {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($validIssueBody)
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $digest = 'SHA256:' + (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
        $outcome = Invoke-Goal -ExpectedDigest $digest -ExpectedAcceptance @('AC1', 'AC2', 'AC3')

        $outcome.ExitCode | Should -Be 0
        $outcome.Result.Status | Should -Be 'started'
    }

    It 'records typed milestone updates in the checkpoint' {
        $null = Invoke-Goal -Decision 'decision-a' -Evidence 'evidence-a' -Fix 'fix-a' -Review 'review-a'
        $saved = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json

        $saved.Decisions -is [array] | Should -BeTrue
        $saved.AcceptanceEvidence -is [array] | Should -BeTrue
        $saved.AttemptedFixes -is [array] | Should -BeTrue
        $saved.OutstandingReviewWork -is [array] | Should -BeTrue
        @($saved.Decisions) | Should -Contain 'decision-a'
        @($saved.AcceptanceEvidence) | Should -Contain 'evidence-a'
        @($saved.AttemptedFixes) | Should -Contain 'fix-a'
        @($saved.OutstandingReviewWork) | Should -Contain 'review-a'
    }

    It 'returns a structured JSON error for a missing resume checkpoint' {
        Remove-Item -LiteralPath $checkpoint -Force -ErrorAction SilentlyContinue
        $outcome = Invoke-Goal -Action resume

        $outcome.ExitCode | Should -Be 1
        $outcome.Result.Status | Should -Be 'ERROR'
    }
}
