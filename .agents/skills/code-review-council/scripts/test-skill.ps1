#!/usr/bin/env pwsh

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

function Assert-Crc {
    param([bool]$Condition,[string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Join-Path ([System.IO.Path]::GetTempPath()) ("code-review-council-tests-{0}" -f ([guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $root -Force | Out-Null
$repo = Join-Path $root 'repo'
New-Item -ItemType Directory -Path $repo -Force | Out-Null
$scope = Join-Path $root 'scope.json'
$reviewers = Join-Path $root 'reviewers.jsonl'
$adjudication = Join-Path $root 'adjudication.json'
$review = Join-Path $root 'review.json'
$ledger = Join-Path $root 'ledger.json'
$evaluation = Join-Path $root 'evaluation.json'

try {
    Push-Location $repo
    & git init --quiet
    & git config user.name 'Council Test'
    & git config user.email 'council@example.invalid'
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'base' -Encoding utf8
    & git add tracked.txt
    & git commit --quiet -m base
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'staged defect' -Encoding utf8
    & git add tracked.txt
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'unstaged correction' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'untracked.txt') -Value 'new' -Encoding utf8
    Pop-Location

    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode worktree -Repo $repo -Changes all -Output $scope
    Assert-Crc ((Get-Content -LiteralPath $scope -Raw | ConvertFrom-Json).status -eq 'READY') 'worktree collection did not produce READY'
    $scopeObject = Get-Content -LiteralPath $scope -Raw | ConvertFrom-Json
    Assert-Crc ([string]::IsNullOrWhiteSpace([string]$scopeObject.selected.staged.patch.content_base64) -eq $false) 'staged patch bytes were not persisted'

    $noChangeMaterial = [ordered]@{ mode='branch'; base=('a'*40); head=('a'*40); merge_base=('a'*40); changed_files=@(); patch=[ordered]@{sha256=('0'*64);bytes=0;encoding='base64';content_base64=''} }
    $noChange = [ordered]@{schema_version=$script:CrcSchemaVersion;mode='branch';status='NO_CHANGES';snapshot_id=(Get-CrcHashJson $noChangeMaterial);repository=[ordered]@{root=$repo};captured_at_utc=(Get-CrcUtcNow);changed_files=@();snapshot_material=$noChangeMaterial}
    $noChangePath = Join-Path $root 'no-change.json'; $emptyReviewers = Join-Path $root 'empty.jsonl'; $emptyAdjudication=Join-Path $root 'empty-adjudication.json'; $noChangeOutput=Join-Path $root 'no-change-result.json'
    Write-CrcJson -Path $noChangePath -Value $noChange; Set-Content -LiteralPath $emptyReviewers -Value '' -Encoding utf8; Write-CrcJson -Path $emptyAdjudication -Value ([ordered]@{adjudicator='test-coordinator';created_at_utc=(Get-CrcUtcNow);snapshot_id=$noChange.snapshot_id;dispositions=@()})
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'validate-review.ps1') -Scope $noChangePath -Reviewers $emptyReviewers -Adjudication $emptyAdjudication -Output $noChangeOutput
    Assert-Crc ((Get-Content -LiteralPath $noChangeOutput -Raw | ConvertFrom-Json).status -eq 'NO_CHANGES') 'NO_CHANGES was not terminal'

    $reviewObject = [ordered]@{schema_version=$script:CrcSchemaVersion;status='PASS';snapshot_id='sha256:' + ('1'*64);scope_manifest=[ordered]@{snapshot_id='sha256:' + ('1'*64);base=('a'*40);head=('b'*40);changed_files=@()};findings=@();errors=@()}
    Write-CrcJson -Path $review -Value $reviewObject
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $review -Provider mock -Ledger $ledger
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $review -Provider mock -Ledger $ledger
    Assert-Crc ((@((Get-Content -LiteralPath $ledger -Raw | ConvertFrom-Json).published)).Count -eq 1) 'mock publication was not idempotent'

    $fixturePath = (Resolve-Path (Join-Path $PSScriptRoot '..\fixtures\evaluation.json')).Path
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-evaluation.ps1') -Fixtures $fixturePath -Output $evaluation
    Assert-Crc ((Get-Content -LiteralPath $evaluation -Raw | ConvertFrom-Json).case_count -eq 24) 'evaluation case count was not 24'
    Write-Output 'Code Review Council PowerShell tests passed.'
    exit 0
}
finally {
    if ((Get-Location).Path -eq $repo) { Pop-Location }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
