#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
    $workflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/sonar-cloud.yml') -Raw
    $steps = @([regex]::Matches($workflow, '(?ms)^      - name: (?<name>[^\r\n]+)\r?\n(?<body>.*?)(?=^      - name: |\z)'))

    function Get-SonarStep {
        param([Parameter(Mandatory)][string]$Name)
        $matches = @($steps | Where-Object { $_.Groups['name'].Value -eq $Name })
        if ($matches.Count -ne 1) { throw "Expected one workflow step named '$Name'." }
        return $matches[0].Groups['body'].Value
    }
}

Describe 'Sonar workflow credential boundary' {
    It 'checks out the exact event commit without persisting GitHub credentials' {
        $checkout = Get-SonarStep -Name 'Checkout'
        $checkout | Should -Match 'ref: \$\{\{ github\.sha \}\}'
        $checkout | Should -Match 'persist-credentials: false'
        $workflow | Should -Match '(?m)^  contents: read\s*$'
        $workflow | Should -Match '(?m)^  pull-requests: read\s*$'
    }

    It 'keeps merge-group validation and its candidate-specific concurrency' {
        $workflow | Should -Match '(?m)^  merge_group:\s*\r?\n    types: \[checks_requested\]'
        $workflow | Should -Match 'github\.event\.pull_request\.number \|\| github\.ref'
        $workflow | Should -Match 'cancel-in-progress: \$\{\{ github\.event_name == ''pull_request'' \}\}'
    }

    It 'uses the empty environment for merge groups and non-main manual runs' {
        $workflow | Should -Match 'github\.event_name == ''merge_group'' \|\| \(github\.event_name == ''workflow_dispatch'' && github\.ref != ''refs/heads/main''\)'
        $workflow | Should -Match '&& ''sonar-no-token'' \|\| ''sonar-analysis'''
        $workflow | Should -Match '(?m)^      deployment: false\s*$'
    }

    It 'references the Sonar secret only in the two scanner steps' {
        $secretSteps = @($steps | Where-Object { $_.Groups['body'].Value -match 'secrets\.SONAR_TOKEN' })
        $secretSteps.Count | Should -Be 2
        @($secretSteps | ForEach-Object { $_.Groups['name'].Value }) | Should -Be @('Sonar begin', 'Sonar end')
        $workflow.Substring(0, $workflow.IndexOf('    steps:')) | Should -Not -Match 'secrets\.SONAR_TOKEN'
    }

    It 'guards both scanner steps against merge groups, non-main manual runs and fork PRs' -ForEach @(
        @{ Name = 'Sonar begin' },
        @{ Name = 'Sonar end' }
    ) {
        $step = Get-SonarStep -Name $Name
        $step | Should -Match 'if: >-\s+github\.event_name != ''merge_group'' &&'
        $step | Should -Match '\(github\.event_name != ''workflow_dispatch'' \|\| github\.ref == ''refs/heads/main''\) &&'
        $step | Should -Match '\(github\.event_name != ''pull_request'' \|\|\s+github\.event\.pull_request\.head\.repo\.full_name == github\.repository\)'
    }

    It 'builds and tests every candidate with a locked restore and without a Sonar token' {
        $build = Get-SonarStep -Name 'Restore, build, and test'
        $build | Should -Not -Match '(?m)^        (if|env|continue-on-error):'
        $build | Should -Not -Match 'SONAR_TOKEN|sonarscanner'
        $build | Should -Match 'dotnet restore .* --use-lock-file --locked-mode'
        $build | Should -Match 'dotnet build .* --no-restore --no-incremental'
        $build | Should -Match 'dotnet dotnet-coverage collect .*test-solution\.ps1'
        $build | Should -Not -Match 'continue-on-error|\|\|\s*true'
    }

    It 'retains the existing analysis settings and coverage configuration on ordinary analysis' {
        $begin = Get-SonarStep -Name 'Sonar begin'
        $begin | Should -Match '/s:"\$\{\{ github\.workspace \}\}/SonarQube\.Analysis\.xml"'
        $begin | Should -Match 'sonar\.cs\.vscoveragexml\.reportsPaths=coverage\.xml'
        $begin | Should -Not -Match 'sonar\.branch\.name|sonar\.branch\.target'
        Test-Path -LiteralPath (Join-Path $repoRoot 'SonarQube.Analysis.xml') -PathType Leaf | Should -BeTrue
    }
}
