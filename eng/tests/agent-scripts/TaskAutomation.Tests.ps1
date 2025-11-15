#requires -Module Pester

$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$automationModulePath = [System.IO.Path]::GetFullPath($automationModulePath)
Import-Module -Name $automationModulePath -Force



$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/TaskAutomation.psm1'
if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
    throw "TaskAutomation module not found at '$modulePath'"
}

Import-Module -Name $modulePath -Force

Describe 'TaskAutomation module' {
    It 'produces deterministic hashes for identical input' {
        $value = 'sample-key'
        $first = Get-DeterministicTaskHash -Value $value
        $second = Get-DeterministicTaskHash -Value $value

    $first | Should -Not -BeNullOrEmpty
    $first | Should -Be $second
    }

    It 'produces different hashes for different input' {
        $hashA = Get-DeterministicTaskHash -Value 'alpha'
        $hashB = Get-DeterministicTaskHash -Value 'beta'

    $hashA | Should -Not -Be $hashB
    }

    It 'normalizes slug components and enforces max length' {
        $slug = Get-AutoTaskSlug -Components @('My Project', 'Feature#1', 'Line:200') -MaxLength 20

    $slug | Should -Be 'my-project-feature-1'
    ($slug.Length -le 20) | Should -Be $true
    }

    It 'creates task items with expected defaults' {
        $task = New-TaskItem -Category 'coverage' -UniqueKey 'unique-1' -Title 'Increase coverage' -Tags 'coverage','auto' -RelatedFiles 'src/File.cs' -References '.github/instructions/testing.instructions.md'

    $task.Category | Should -Be 'coverage'
    $task.UniqueKey | Should -Be 'unique-1'
    $task.Tags.Count | Should -Be 2
    $task.RelatedFiles.Count | Should -Be 1
    $task.EffortPoints | Should -Be 2
    $task.Notes | Should -BeNullOrEmpty
    $task.SlugComponents | Should -Not -BeNullOrEmpty
    }

    It 'syncs tasks to scratchpad pending folder and returns result metadata' {
        $testRepoRoot = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path $testRepoRoot | Out-Null

        $task = New-TaskItem -Category 'mutation' -UniqueKey 'key-123' -Title 'Kill mutant' -Notes 'Strengthen assertions' -Tags 'mutation' -RelatedFiles 'src/Example.cs' -AdditionalProperties @{ mutationKey = 'key-123' }

        $results = Sync-AutoTasks -Tasks @($task) -RepoRoot $testRepoRoot
    $results.Count | Should -Be 1
    $results[0].Status | Should -Be 'Created'
    $results[0].FilePath | Should -Not -BeNullOrEmpty
    Test-Path -LiteralPath $results[0].FilePath | Should -Be $true

        $json = Get-Content -LiteralPath $results[0].FilePath -Raw | ConvertFrom-Json
    $json.autoTaskKey | Should -Be 'key-123'
    $json.notes | Should -Be 'Strengthen assertions'
    $json.category | Should -Be 'mutation'

        $second = Sync-AutoTasks -Tasks @($task) -RepoRoot $testRepoRoot
    $second[0].Status | Should -Be 'Skipped'
    }

    It 'supports WhatIf mode without writing files' {
        $testRepoRoot = Join-Path $TestDrive 'repo-whatif'
        New-Item -ItemType Directory -Path $testRepoRoot | Out-Null

        $task = New-TaskItem -Category 'mutation' -UniqueKey 'key-whatif' -Title 'Preview task'

    $result = Sync-AutoTasks -Tasks @($task) -RepoRoot $testRepoRoot -WhatIf
    $result[0].Status | Should -Be 'Planned'
    Test-Path -Path (Join-Path $testRepoRoot '.scratchpad/tasks/pending') | Should -Be $false
    }

    It 'handles null or empty task arrays gracefully' {
    { Sync-AutoTasks -Tasks @() -RepoRoot (Join-Path $TestDrive 'repo-empty') } | Should -Not -Throw
    }
}

