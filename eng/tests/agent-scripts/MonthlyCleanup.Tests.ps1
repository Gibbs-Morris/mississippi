#!/usr/bin/env pwsh

#requires -Module Pester

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$monthlyModulePath = [IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, '..', '..', 'src', 'agent-scripts', 'MonthlyCleanupAutomation.psm1'))
Import-Module -Name $monthlyModulePath -Force

Describe 'Monthly cleanup pull request publishing' {
    BeforeEach {
        $script:monthlyPatchPath = Join-Path $TestDrive 'cleanup.patch'
        Set-Content -LiteralPath $script:monthlyPatchPath -Value 'fixture patch' -Encoding utf8
        $script:publisherParameters = @{
            DriftState      = 'true'
            PatchPath       = $script:monthlyPatchPath
            DefaultBranch   = 'main'
            RepositoryOwner = 'Gibbs-Morris'
            PullRequestTitle = 'Monthly cleanup'
            RunId           = '123'
            RunAttempt      = '1'
        }
        $script:monthlyScenario = 'Existing'
        $script:monthlyLeaseFails = $false
        Mock -CommandName Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -MockWith {
            if ($FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'list') {
                $output = if ($script:monthlyScenario -eq 'New') {
                    '[]'
                }
                else {
                    '[{"number":42,"title":"Monthly cleanup","headRefName":"automation/monthly-cleanup-old","url":"https://example.test/42","isCrossRepository":false,"headRepositoryOwner":{"login":"Gibbs-Morris"}}]'
                }
                return [pscustomobject]@{ ExitCode = 0; Output = @($output) }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'diff' -and $Arguments -contains '--quiet') {
                $exitCode = if ($script:monthlyScenario -eq 'NoDrift') { 0 } else { 1 }
                return [pscustomobject]@{ ExitCode = $exitCode; Output = @() }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'diff') {
                return [pscustomobject]@{ ExitCode = 0; Output = @('src/Foo.cs') }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'rev-parse') {
                $sha = if ($Arguments[1] -eq 'HEAD') { 'cleanup-sha' } else { 'remote-sha' }
                return [pscustomobject]@{ ExitCode = 0; Output = @($sha) }
            }
            if ($FilePath -eq 'git' -and $Arguments[0] -eq 'push' -and $script:monthlyLeaseFails) {
                throw 'force-with-lease rejected'
            }
            return [pscustomobject]@{ ExitCode = 0; Output = @() }
        }
    }

    It 'closes an existing automation pull request when drift has disappeared' {
        $script:monthlyScenario = 'NoDrift'
        $script:publisherParameters.DriftState = 'false'

        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'NoDrift'
        $result.Branch | Should -BeNullOrEmpty
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'close' -and
            $Arguments -contains '42' -and $Arguments -contains '--delete-branch'
        }
    }

    It 'replaces an existing automation branch with a guarded lease' {
        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'Updated'
        $result.Branch | Should -Be 'automation/monthly-cleanup-old'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'push' -and
            $Arguments -contains '--force-with-lease=refs/heads/automation/monthly-cleanup-old:remote-sha'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'edit'
        }
    }

    It 'creates a new automation branch and pull request when none exists' {
        $script:monthlyScenario = 'New'

        $result = Publish-MonthlyCleanupPullRequest @script:publisherParameters

        $result.Action | Should -Be 'Created'
        $result.Branch | Should -Be 'automation/monthly-cleanup-123-1'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'switch' -and
            $Arguments -contains 'automation/monthly-cleanup-123-1'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'create'
        }
    }

    It 'stops without editing the pull request when the replacement lease fails' {
        $script:monthlyLeaseFails = $true

        { Publish-MonthlyCleanupPullRequest @script:publisherParameters } |
            Should -Throw '*force-with-lease rejected*'
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 0 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'pr' -and $Arguments[1] -eq 'edit'
        }
    }

    It 'reconciles the branch with the latest base before dispatching validation' {
        $result = Invoke-MonthlyCleanupPullRequestValidation `
            -CleanupBranch 'automation/monthly-cleanup-123-1' `
            -DefaultBranch 'main' `
            -Workflows @('cleanup.yml', 'full-build.yml')

        $result.WorkflowCount | Should -Be 2
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'git' -and $Arguments[0] -eq 'merge' -and
            $Arguments -contains 'refs/remotes/origin/main'
        }
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 2 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'run' -and
            $Arguments -contains '--ref' -and $Arguments -contains 'automation/monthly-cleanup-123-1'
        }
    }

    It 'dispatches mutation testing as part of the default validation set' {
        $result = Invoke-MonthlyCleanupPullRequestValidation `
            -CleanupBranch 'automation/monthly-cleanup-123-1' `
            -DefaultBranch 'main'

        $result.WorkflowCount | Should -BeGreaterThan 2
        Should -Invoke Invoke-MonthlyCleanupCommand -ModuleName MonthlyCleanupAutomation -Times 1 -ParameterFilter {
            $FilePath -eq 'gh' -and $Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'run' -and
            $Arguments -contains 'stryker.yml'
        }
    }
}


