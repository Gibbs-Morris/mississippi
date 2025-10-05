#requires -Module Pester

$ErrorActionPreference = 'Stop'

$automationModulePath = Join-Path $PSScriptRoot '..\..\src\agent-scripts\RepositoryAutomation.psm1'
$automationModulePath = [System.IO.Path]::GetFullPath($automationModulePath)
Import-Module -Name $automationModulePath -Force



$repoRoot = Get-RepositoryRoot -StartPath $PSScriptRoot
$scriptsRoot = Join-Path $repoRoot 'eng/src/agent-scripts/tasks'
$script:newScript = Join-Path $scriptsRoot 'new-scratchpad-task.ps1'
$script:listScript = Join-Path $scriptsRoot 'list-scratchpad-tasks.ps1'
$script:claimScript = Join-Path $scriptsRoot 'claim-scratchpad-task.ps1'
$script:completeScript = Join-Path $scriptsRoot 'complete-scratchpad-task.ps1'
$script:deferScript = Join-Path $scriptsRoot 'defer-scratchpad-task.ps1'

Describe 'Scratchpad task scripts' {
    BeforeAll {
        foreach ($script in @($newScript, $listScript, $claimScript, $completeScript, $deferScript)) {
            if (-not (Test-Path -LiteralPath $script -PathType Leaf)) {
                throw "Missing script under test: $script"
            }
        }
    }

    BeforeEach {
        $testDriveRoot = (Resolve-Path $TestDrive).ProviderPath
        $script:tempRoot = Join-Path $testDriveRoot ([Guid]::NewGuid().ToString())
        $script:scratchpadRoot = Join-Path $script:tempRoot '.scratchpad'
    }

    AfterEach {
        if ($script:tempRoot -and (Test-Path -LiteralPath $script:tempRoot)) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'creates a new pending task with expected schema' {
        $result = & $newScript -Title 'Unit Test Task' -Priority 'P1' -Tags 'build','tests' -Notes 'ensure coverage' -EffortPoints 5 -ScratchpadRoot $scratchpadRoot

        $result.Id | Should -Not -BeNullOrEmpty
        Test-Path -LiteralPath $result.Path | Should -Be $true

        $json = Get-Content -LiteralPath $result.Path -Raw | ConvertFrom-Json
        $json.status | Should -Be 'pending'
        $json.priority | Should -Be 'P1'
        @($json.tags).Count | Should -Be 2
        $json.effortPoints | Should -Be 5
    }

    It 'lists tasks across statuses and supports filters' {
        $task1 = & $newScript -Title 'First Task' -Priority 'P0' -Tags 'alpha','shared' -ScratchpadRoot $scratchpadRoot
        $task2 = & $newScript -Title 'Second Task' -Priority 'P2' -Tags 'beta' -ScratchpadRoot $scratchpadRoot

        $all = & $listScript -ScratchpadRoot $scratchpadRoot
        $all.Count | Should -Be 2

        $filtered = & $listScript -ScratchpadRoot $scratchpadRoot -Priority 'P0'
        $filtered.Count | Should -Be 1
        $filtered[0].Id | Should -Be $task1.Id

        $tagFiltered = & $listScript -ScratchpadRoot $scratchpadRoot -Tag 'beta'
        $tagFiltered.Count | Should -Be 1
        $tagFiltered[0].Id | Should -Be $task2.Id

        $withData = & $listScript -ScratchpadRoot $scratchpadRoot -IncludeData
        $withData[0].Data | Should -Not -BeNullOrEmpty
    }

    It 'claims a pending task and increments attempts' {
        $task = & $newScript -Title 'Claim Me' -ScratchpadRoot $scratchpadRoot
        $claimed = & $claimScript -Id $task.Id -Agent 'pester-agent' -ScratchpadRoot $scratchpadRoot

        $claimed.Status | Should -Be 'claimed'
        $claimed.Attempts | Should -Be 1
        $claimed.ClaimedBy | Should -Be 'pester-agent'
        Test-Path -LiteralPath $claimed.Path | Should -Be $true

        $json = Get-Content -LiteralPath $claimed.Path -Raw | ConvertFrom-Json
        $json.status | Should -Be 'claimed'
        $json.attempts | Should -Be 1
    }

    It 'prevents claims beyond the attempt limit' {
        $task = & $newScript -Title 'Attempt Limit' -ScratchpadRoot $scratchpadRoot
        $pendingPath = $task.Path

        $content = Get-Content -LiteralPath $pendingPath -Raw | ConvertFrom-Json
        $content.attempts = 5
        $content | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $pendingPath -Encoding UTF8

        $threw = $false
        try {
            & $claimScript -Id $task.Id -ScratchpadRoot $scratchpadRoot | Out-Null
        }
        catch {
            $threw = $true
        }

        $threw | Should -Be $true
    }

    It 'completes a claimed task and records result' {
        $task = & $newScript -Title 'Complete Me' -ScratchpadRoot $scratchpadRoot
        & $claimScript -Id $task.Id -ScratchpadRoot $scratchpadRoot -Agent 'runner' | Out-Null

        $done = & $completeScript -Id $task.Id -Result 'Task finished successfully' -ScratchpadRoot $scratchpadRoot
        $done.Status | Should -Be 'done'
        $done.Result | Should -Be 'Task finished successfully'
        Test-Path -LiteralPath $done.Path | Should -Be $true

        $json = Get-Content -LiteralPath $done.Path -Raw | ConvertFrom-Json
        $json.status | Should -Be 'done'
        $json.result | Should -Be 'Task finished successfully'
    }

    It 'defers a pending task with reason and next steps' {
        $task = & $newScript -Title 'Defer Pending' -ScratchpadRoot $scratchpadRoot
        $deferred = & $deferScript -Id $task.Id -Reason 'Blocked by dependency' -NextSteps 'Retry after dependency ready' -ScratchpadRoot $scratchpadRoot

        $deferred.Status | Should -Be 'deferred'
        $deferred.Reason | Should -Be 'Blocked by dependency'
        Test-Path -LiteralPath $deferred.Path | Should -Be $true

        $json = Get-Content -LiteralPath $deferred.Path -Raw | ConvertFrom-Json
        $json.status | Should -Be 'deferred'
        $json.reason | Should -Be 'Blocked by dependency'
        $json.nextSteps | Should -Be 'Retry after dependency ready'
    }

    It 'defers a claimed task and retains claim metadata' {
        $task = & $newScript -Title 'Defer Claimed' -ScratchpadRoot $scratchpadRoot
        & $claimScript -Id $task.Id -Agent 'specialist' -ScratchpadRoot $scratchpadRoot | Out-Null

        $deferred = & $deferScript -Id $task.Id -Reason 'Needs API schema' -ScratchpadRoot $scratchpadRoot
        $deferred.Status | Should -Be 'deferred'

        $json = Get-Content -LiteralPath $deferred.Path -Raw | ConvertFrom-Json
        $json.claimedBy | Should -Be 'specialist'
        $json.reason | Should -Be 'Needs API schema'
    }
}








