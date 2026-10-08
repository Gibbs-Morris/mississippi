#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-SonarPublishedBranch {
    param([object]$Source)
    $branches = Read-SonarServiceMetadata 'project_branches/list?project=Gibbs-Morris_mississippi&organization=gibbs-morris'
    $branch = @($branches.branches | Where-Object { $_.name -ceq $Source.HeadRef })
    if ($branch.Count -ne 1 -or $branch[0].commit.sha -cne $Source.HeadSha -or $branch[0].status.qualityGateStatus -cne 'OK') { throw 'Published Sonar branch does not identify the successful source revision.' }
    if ($Source.HeadRef -ceq $Source.TargetRef -and ($branch[0].isMain -ne $true -or $branch[0].type -cne 'LONG')) { throw 'Published default branch must remain the LONG Sonar main branch.' }
    if ($Source.Mode -ceq 'Queue' -and ($branch[0].type -cne 'SHORT' -or $branch[0].isMain -ne $false)) { throw 'Published queue analysis has the wrong branch classification.' }
}

function Assert-SonarQueuePolicyBaseline {
    param([object]$Source,[string]$Pattern,[object[]]$Branches)
    if ($Pattern -cne '(branch|release)-.*') { throw 'Queue branch classification no longer matches the reviewed policy.' }
    $main = @($Branches | Where-Object { $_.name -ceq $Source.TargetRef -and $_.isMain -eq $true -and $_.type -ceq 'LONG' })
    if ($main.Count -ne 1 -or $main[0].commit.sha -cne $Source.TargetSha) { throw 'Queue analysis requires the exact current target baseline in Sonar.' }
    $candidate = @($Branches | Where-Object { $_.name -ceq $Source.HeadRef })
    if ($candidate.Count -gt 1 -or ($candidate.Count -eq 1 -and ($candidate[0].type -cne 'SHORT' -or $candidate[0].isMain -ne $false))) { throw 'Queue candidate must be a distinct short-lived Sonar branch.' }
}

function Read-SonarGitHubMetadata {
    param([string]$Path)

    $output = & gh api $Path 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw 'Trusted Sonar GitHub metadata request failed.' }
    return $output | ConvertFrom-Json
}

function Assert-TrustedSonarControllerOrigin {
    param([string]$Repository, [string]$DefaultBranch, [string]$WorkflowRef, [string]$WorkflowSha, [string]$CheckoutSha)

    $expected = "$Repository/.github/workflows/sonar-trusted-analysis.yml@refs/heads/$DefaultBranch"
    if ($WorkflowRef -cne $expected -or $WorkflowSha -cnotmatch '^[0-9a-f]{40}$' -or $CheckoutSha -cne $WorkflowSha) {
        throw 'Sonar controller must execute its immutable default-branch definition.'
    }
    $current = Read-SonarGitHubMetadata "repos/$Repository/git/ref/heads/$([Uri]::EscapeDataString($DefaultBranch))"
    if ($current.ref -cne "refs/heads/$DefaultBranch" -or $current.object.type -cne 'commit' -or $current.object.sha -cne $WorkflowSha) {
        throw 'Sonar controller revision must match the current default-branch tip.'
    }
}

function Get-SonarSourceRunPaths {
    param([object]$Run)
    $workflow = '.github/workflows/sonar-cloud.yml'
    $qualifiers = @([string]$Run.head_branch,"refs/heads/$($Run.head_branch)",[string]$Run.head_sha)
    if ($Run.event -ceq 'pull_request') {
        foreach ($pr in @($Run.pull_requests | Where-Object { $_.head.sha -ceq $Run.head_sha -and $_.head.ref -ceq $Run.head_branch })) {
            if (($pr.number -is [int] -or $pr.number -is [long]) -and $pr.number -gt 0) { $qualifiers += "refs/pull/$($pr.number)/merge" }
        }
    }
    return @($workflow) + @($qualifiers | ForEach-Object { "$workflow@$_" })
}

function Assert-SonarSourceRun {
    param([object]$Run, [string]$Repository, [long]$RunId)

    if ($Run.id -ne $RunId) { throw 'Source run ID does not match the requested run.' }
    if ($Run.workflow_id -ne 141036039 -or $Run.path -cnotin @(Get-SonarSourceRunPaths -Run $Run) -or $Run.repository.full_name -ine $Repository -or $Run.head_repository.full_name -ine $Repository) {
        throw 'Source run does not identify the approved repository workflow.'
    }
    if ($Run.status -cne 'completed' -or $Run.head_sha -cnotmatch '^[0-9a-f]{40}$' -or -not $Run.head_branch) { throw 'Source run identity is incomplete.' }
}

function Get-SonarBranchSource {
    param([object]$Run, [string]$Repository, [string]$DefaultBranch)

    $branch = [string]$Run.head_branch
    $ref = Read-SonarGitHubMetadata -Path "repos/$Repository/git/ref/heads/$([Uri]::EscapeDataString($branch))"
    if ($ref.object.sha -cne $Run.head_sha) { throw 'Source branch no longer identifies this run.' }
    return [pscustomobject][ordered]@{
        RunId = $Run.id; Mode = 'Branch'; HeadSha = $Run.head_sha; BuildSha = $Run.head_sha
        HeadRef = $branch; TargetRef = $DefaultBranch; TargetSha = $null; PullRequest = $null; Queue = $null
    }
}

function Assert-SonarPullRequestMergeRevision {
    param([object]$PullRequest, [string]$Repository)

    if ($PullRequest.mergeable -isnot [bool] -or -not $PullRequest.mergeable) { throw 'Source pull request merge revision is not confirmed mergeable.' }
    $commit = Read-SonarGitHubMetadata -Path "repos/$Repository/git/commits/$($PullRequest.merge_commit_sha)"
    $parents = @($commit.parents)
    if ($commit.sha -cne $PullRequest.merge_commit_sha -or $parents.Count -ne 2 -or $parents[0].sha -cne $PullRequest.base.sha -or $parents[1].sha -cne $PullRequest.head.sha) {
        throw 'Source pull request merge revision does not have the exact current base and head parents.'
    }
}

function Get-SonarPullRequestSource {
    param([object]$Run, [string]$Repository)

    $references = @($Run.pull_requests | Where-Object { $_.head.sha -ceq $Run.head_sha -and $_.head.ref -ceq $Run.head_branch })
    if ($references.Count -ne 1) { throw 'Source run does not identify exactly one pull request.' }
    $number = $references[0].number
    if (($number -isnot [int] -and $number -isnot [long]) -or $number -le 0) { throw 'Source pull request number is invalid.' }
    $pr = Read-SonarGitHubMetadata -Path "repos/$Repository/pulls/$number"
    if ($pr.state -cne 'open' -or $pr.head.sha -cne $Run.head_sha -or $pr.head.ref -cne $Run.head_branch -or $pr.head.repo.full_name -ine $Repository -or $pr.base.repo.full_name -ine $Repository) {
        throw 'Source pull request no longer identifies this run.'
    }
    if ($pr.merge_commit_sha -cnotmatch '^[0-9a-f]{40}$' -or $pr.base.sha -cnotmatch '^[0-9a-f]{40}$') { throw 'Source pull request has no immutable merge revision.' }
    Assert-SonarPullRequestMergeRevision -PullRequest $pr -Repository $Repository
    return [pscustomobject][ordered]@{
        RunId = $Run.id; Mode = 'PullRequest'; HeadSha = $pr.head.sha; BuildSha = $pr.merge_commit_sha
        HeadRef = $pr.head.ref; TargetRef = $pr.base.ref; TargetSha = $pr.base.sha; PullRequest = $number; Queue = $null
    }
}

function Get-SonarQueueSource {
    param([object]$Run, [string]$Repository, [string]$DefaultBranch)

    $prefix = "gh-readonly-queue/$DefaultBranch/"
    if (-not ([string]$Run.head_branch).StartsWith($prefix, [StringComparison]::Ordinal)) { throw 'Source queue ref does not target the default branch.' }
    $snapshot = Get-MergeQueueSnapshot -Repository $Repository -Branch "refs/heads/$DefaultBranch"
    $entries = @($snapshot.Entries | Where-Object { $null -ne $_.headCommit -and $_.headCommit.oid -ceq $Run.head_sha })
    if ($entries.Count -ne 1) { throw 'Source run does not identify exactly one live queue candidate.' }
    $group = [pscustomobject]@{ head_sha = $Run.head_sha; base_sha = $entries[0].baseCommit.oid; base_ref = "refs/heads/$DefaultBranch" }
    $queue = Resolve-MergeGroupIssueMembers -MergeGroup $group -Repository $Repository -CandidateSha $Run.head_sha
    return [pscustomobject][ordered]@{
        RunId = $Run.id; Mode = 'Queue'; HeadSha = $Run.head_sha; BuildSha = $Run.head_sha
        HeadRef = $Run.head_branch; TargetRef = $DefaultBranch; TargetSha = $queue.TargetSha; PullRequest = $null; Queue = $queue
    }
}

function Get-TrustedSonarSource {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$Repository,
        [Parameter(Mandatory)][ValidateRange(1,[long]::MaxValue)][long]$RunId,
        [Parameter(Mandatory)][string]$DefaultBranch
    )

    $run = Read-SonarGitHubMetadata -Path "repos/$Repository/actions/runs/$RunId"
    Assert-SonarSourceRun -Run $run -Repository $Repository -RunId $RunId
    switch ([string]$run.event) {
        'pull_request' { return Get-SonarPullRequestSource -Run $run -Repository $Repository }
        'merge_group' { return Get-SonarQueueSource -Run $run -Repository $Repository -DefaultBranch $DefaultBranch }
        'push' {
            if ($run.head_branch -cne $DefaultBranch) { throw 'Push analysis must identify the default branch.' }
            return Get-SonarBranchSource -Run $run -Repository $Repository -DefaultBranch $DefaultBranch
        }
        'workflow_dispatch' {
            if (([string]$run.head_branch).StartsWith('gh-readonly-queue/',[StringComparison]::Ordinal)) {
                return Get-SonarQueueSource -Run $run -Repository $Repository -DefaultBranch $DefaultBranch
            }
            return Get-SonarBranchSource -Run $run -Repository $Repository -DefaultBranch $DefaultBranch
        }
        default { throw 'Source workflow event is unsupported.' }
    }
}

function Assert-TrustedSonarSourceUnchanged {
    param([Parameter(Mandatory)][object]$Before,[Parameter(Mandatory)][object]$After)

    $fields = @('RunId','Mode','HeadSha','BuildSha','HeadRef','TargetRef','PullRequest')
    $old = $Before | Select-Object -Property $fields | ConvertTo-Json -Compress
    $new = $After | Select-Object -Property $fields | ConvertTo-Json -Compress
    if ($old -cne $new) { throw 'Sonar source identity changed during analysis.' }
    if ($Before.Mode -ceq 'Queue') { Assert-MergeGroupIssueMembersUnchanged -Before $Before.Queue -After $After.Queue }
    elseif ($Before.TargetSha -cne $After.TargetSha) { throw 'Sonar source target changed during analysis.' }
}

function Get-TrustedSonarAnalysisArguments {
    param([Parameter(Mandatory)][object]$Source)

    $arguments = @("/d:sonar.scm.revision=$($Source.HeadSha)", '/d:sonar.qualitygate.wait=true')
    if ($Source.Mode -ceq 'PullRequest') {
        return $arguments + @("/d:sonar.pullrequest.key=$($Source.PullRequest)","/d:sonar.pullrequest.branch=$($Source.HeadRef)","/d:sonar.pullrequest.base=$($Source.TargetRef)")
    }
    if ($Source.HeadRef -ceq $Source.TargetRef) { return $arguments + @("/d:sonar.branch.name=$($Source.HeadRef)") }
    return $arguments + @("/d:sonar.branch.name=$($Source.HeadRef)","/d:sonar.branch.target=$($Source.TargetRef)")
}

Export-ModuleMember -Function Assert-TrustedSonarControllerOrigin, Get-TrustedSonarSource, Assert-TrustedSonarSourceUnchanged, Get-TrustedSonarAnalysisArguments

function Assert-SonarDeploymentBranchType {
    param($Branch,[string]$Repository,[string]$DefaultBranch)
    $type=$Branch.PSObject.Properties['type']
    if($null -eq $type -or $null -eq $type.Value){
        $id=$Branch.PSObject.Properties['id']
        if($null -eq $id -or ($id.Value -isnot [int] -and $id.Value -isnot [long]) -or $id.Value -le 0){throw 'Sonar environment policy lacks a valid policy ID.'}
        $detail=Read-SonarGitHubMetadata -Path "repos/$Repository/environments/sonar-analysis/deployment-branch-policies/$($id.Value)"
        if($detail.id -ne $id.Value -or $detail.name -cne $DefaultBranch){throw 'Sonar environment policy detail does not match its list entry.'}
        $type=$detail.PSObject.Properties['type']
    }
    if($null -eq $type -or $type.Value -cne 'branch'){throw 'Sonar environment policy must prove the exact default branch, not a tag.'}
}

function Assert-SonarCredentialDeployment {
    param([string]$Repository,[string]$DefaultBranch)
    $environment = Read-SonarGitHubMetadata -Path "repos/$Repository/environments/sonar-analysis"
    $policy = $environment.deployment_branch_policy
    if ($null -eq $policy -or $policy.protected_branches -ne $false -or $policy.custom_branch_policies -ne $true) { throw 'Sonar credentials require an exact default-branch environment policy.' }
    $branches = Read-SonarGitHubMetadata -Path "repos/$Repository/environments/sonar-analysis/deployment-branch-policies?per_page=100"
    if ($branches.total_count -ne 1 -or @($branches.branch_policies).Count -ne 1 -or $branches.branch_policies[0].name -cne $DefaultBranch) { throw 'Sonar environment must permit only the exact default branch.' }
    Assert-SonarDeploymentBranchType -Branch $branches.branch_policies[0] -Repository $Repository -DefaultBranch $DefaultBranch
}

Export-ModuleMember -Function Assert-SonarCredentialDeployment

function Read-SonarServiceMetadata {
    param([string]$Endpoint)
    return Invoke-RestMethod -Uri "https://sonarcloud.io/api/$Endpoint" -TimeoutSec 30
}

function Get-SonarQualityPolicySnapshot {
    param([object]$Source)
    $project = 'project=Gibbs-Morris_mississippi&organization=gibbs-morris'
    $assignment = Read-SonarServiceMetadata "qualitygates/get_by_project?$project"
    $definition = Read-SonarServiceMetadata "qualitygates/show?id=$($assignment.qualityGate.id)&organization=gibbs-morris"
    $required = @{
        new_security_rating=@('GT','1');new_reliability_rating=@('GT','1');new_maintainability_rating=@('GT','1')
        new_security_hotspots_reviewed=@('LT','100');new_code_smells=@('GT','0');new_violations=@('GT','0')
        new_duplicated_lines_density=@('GT','6');new_coverage=@('LT','60')
    }
    foreach ($metric in $required.Keys) {
        $matchesForMetric = @($definition.conditions | Where-Object { $_.metric -ceq $metric })
        if ($matchesForMetric.Count -ne 1 -or $matchesForMetric[0].op -cne $required[$metric][0] -or [string]$matchesForMetric[0].error -cne $required[$metric][1]) { throw 'Sonar quality gate no longer matches the reviewed repository criteria.' }
    }
    $classification = Read-SonarServiceMetadata 'settings/values?component=Gibbs-Morris_mississippi&keys=sonar.branch.longLivedBranches.regex'
    $patterns = @($classification.settings | Where-Object { $_.key -ceq 'sonar.branch.longLivedBranches.regex' })
    if ($patterns.Count -ne 1) { throw 'Sonar branch-classification policy is unavailable.' }
    $pattern = [string]$patterns[0].value
    $branches = Read-SonarServiceMetadata "project_branches/list?$project"
    if ($Source.Mode -ceq 'Queue') { Assert-SonarQueuePolicyBaseline -Source $Source -Pattern $pattern -Branches @($branches.branches) }
    return [pscustomobject]@{
        GateId=$assignment.qualityGate.id; LongLivedPattern=$pattern
        Conditions=($definition.conditions | Sort-Object metric | Select-Object metric,op,error | ConvertTo-Json -Compress)
    }
}

function Assert-SonarQualityPolicyUnchanged {
    param([object]$Before,[object]$After)
    if ($Before.GateId -ne $After.GateId -or $Before.Conditions -cne $After.Conditions -or $Before.LongLivedPattern -cne $After.LongLivedPattern) { throw 'Sonar quality-gate policy changed during analysis.' }
}

function Assert-SonarPublishedAnalysis {
    param([object]$Source,[string]$Repository,[datetimeoffset]$StartedAt)
    if ($Source.Mode -cne 'PullRequest') { Assert-SonarPublishedBranch -Source $Source }
    for ($attempt=0; $attempt -lt 12; $attempt++) {
        $response = Read-SonarGitHubMetadata "repos/$Repository/commits/$($Source.HeadSha)/check-runs?filter=latest&per_page=100"
        if ($response.total_count -gt 100 -or $response.total_count -ne @($response.check_runs).Count) { throw 'Sonar provider check response is incomplete.' }
        $provider = @($response.check_runs | Where-Object { $_.app.id -eq 12526 -and $_.head_sha -ceq $Source.HeadSha -and $_.status -ceq 'completed' -and $_.conclusion -ceq 'success' -and [datetimeoffset]$_.started_at -ge $StartedAt })
        if ($provider.Count -eq 1) { return }
        if ($attempt -lt 11) { Start-Sleep -Seconds 5 }
    }
    throw 'No fresh successful Sonar provider check was published on the exact source revision.'
}

Export-ModuleMember -Function Get-SonarQualityPolicySnapshot,Assert-SonarQualityPolicyUnchanged,Assert-SonarPublishedAnalysis
function Assert-TrustedSonarUploadCompletion {
    param([object]$Source,[object]$Policy,[string]$Repository,[string]$DefaultBranch,[datetimeoffset]$StartedAt)
    $published = Get-TrustedSonarSource -Repository $Repository -RunId $Source.RunId -DefaultBranch $DefaultBranch
    Assert-TrustedSonarSourceUnchanged -Before $Source -After $published
    Assert-SonarPublishedAnalysis -Source $published -Repository $Repository -StartedAt $StartedAt
    $currentPolicy = Get-SonarQualityPolicySnapshot -Source $published
    Assert-SonarQualityPolicyUnchanged -Before $Policy -After $currentPolicy
    $final = Get-TrustedSonarSource -Repository $Repository -RunId $Source.RunId -DefaultBranch $DefaultBranch
    Assert-TrustedSonarSourceUnchanged -Before $published -After $final
    if ($published.TargetSha -cne $final.TargetSha) { throw 'Sonar source target changed after the completed policy check.' }
    Assert-SonarCredentialDeployment -Repository $Repository -DefaultBranch $DefaultBranch
    return $final
}

Export-ModuleMember -Function Assert-TrustedSonarUploadCompletion
