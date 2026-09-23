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

    $findingFingerprint = 'sha256:' + ('2'*64); $reviewerRecords = @(); $reviewerIndex = 0
    foreach ($persona in $script:CrcPersonas) {
        $reviewerIndex++
        $findings = @()
        if ($reviewerIndex -eq 1) { $findings = @([ordered]@{fingerprint=$findingFingerprint;persona_ids=@($persona);category='correctness';severity='P2';snapshot_id=$scopeObject.snapshot_id;path='tracked.txt';symbol='tracked';line=1;scenario='staged scenario';trigger='test trigger';impact='test impact';evidence=@('test evidence');remediation='test remediation';uncertainty='low';change_relation='introduced'}) }
        $reviewerRecords += (([ordered]@{review_id="review-$reviewerIndex";persona_id=$persona;snapshot_id=$scopeObject.snapshot_id;status='complete';requested_model='test-model';effective_model='test-model';requested_concurrency=10;effective_concurrency=4;completed_at_utc=(Get-CrcUtcNow);findings=$findings} | ConvertTo-Json -Depth 100 -Compress))
    }
    Set-Content -LiteralPath $reviewers -Value ($reviewerRecords -join [Environment]::NewLine) -Encoding utf8
    Write-CrcJson -Path $adjudication -Value ([ordered]@{adjudicator='test-coordinator';created_at_utc=(Get-CrcUtcNow);snapshot_id=$scopeObject.snapshot_id;dispositions=@([ordered]@{fingerprint=$findingFingerprint;disposition='validated';rationale='test evidence supports the finding';snapshot_id=$scopeObject.snapshot_id})})
    $markdownReview = Join-Path $root 'review.md'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'validate-review.ps1') -Scope $scope -Reviewers $reviewers -Adjudication $adjudication -Output $review -MarkdownOutput $markdownReview
    $validatedReview = Get-Content -LiteralPath $review -Raw | ConvertFrom-Json
    Assert-Crc ($validatedReview.status -eq 'PASS') 'complete review did not produce PASS'
    Assert-Crc ((Get-Content -LiteralPath $markdownReview -Raw) -match 'staged scenario' -and (Get-Content -LiteralPath $markdownReview -Raw) -match 'test evidence') 'Markdown review omitted finding evidence'

    $invalidReview = Join-Path $root 'invalid-review.json'; $invalidLedger = Join-Path $root 'invalid-ledger.json'; $invalidOutput = Join-Path $root 'invalid-publication.json'
    Write-CrcJson -Path $invalidReview -Value ([ordered]@{schema_version=$script:CrcSchemaVersion;status='PASS';snapshot_id=$scopeObject.snapshot_id;scope_manifest=$scopeObject;reviewers=@();findings=@();dispositions=@();execution=[ordered]@{reviewer_count=0;required_reviewer_count=10;adjudicator='test-coordinator';adjudicated_at_utc=(Get-CrcUtcNow);adjudication_snapshot_id=$scopeObject.snapshot_id};publication=[ordered]@{status='not-requested'};errors=@()})
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $invalidReview -Provider mock -Ledger $invalidLedger -Output $invalidOutput
    $invalidPublication = Get-Content -LiteralPath $invalidOutput -Raw | ConvertFrom-Json
    Assert-Crc ($LASTEXITCODE -eq 2 -and $invalidPublication.status -eq 'blocked' -and $invalidPublication.error -match 'all ten reviewer records') 'publisher accepted an incomplete PASS result'
    Assert-Crc (-not (Test-Path -LiteralPath $invalidLedger)) 'invalid review changed the publication ledger'

    $readOnlyOutput = Join-Path $root 'read-only-output.json'; Set-Content -LiteralPath $readOnlyOutput -Value 'existing' -Encoding utf8
    (Get-Item -LiteralPath $readOnlyOutput -Force).IsReadOnly = $true
    $fakeBin = Join-Path $root 'fake-bin'; New-Item -ItemType Directory -Path $fakeBin -Force | Out-Null
    $fakeGhLog = Join-Path $root 'fake-gh-called.txt'
    $oldPath = $env:PATH; $oldPathExt = $env:PATHEXT; $oldGhLog = $env:CRC_GH_STUB_LOG
    try {
        $env:CRC_GH_STUB_LOG = $fakeGhLog
        if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            $fakeGhPath = Join-Path $fakeBin 'gh.cmd'
            Set-Content -LiteralPath $fakeGhPath -Value @('@echo off','echo called>"%CRC_GH_STUB_LOG%"','exit /b 97') -Encoding ascii
            $env:PATH = "$fakeBin;$oldPath"
            $env:PATHEXT = ".CMD;$oldPathExt"
        }
        else {
            $fakeGhPath = Join-Path $fakeBin 'gh'
            [System.IO.File]::WriteAllText($fakeGhPath,"#!/usr/bin/env pwsh`nSet-Content -LiteralPath `$env:CRC_GH_STUB_LOG -Value 'called' -Encoding utf8`nexit 97`n",[System.Text.UTF8Encoding]::new($false))
            [System.IO.File]::SetUnixFileMode($fakeGhPath,[System.IO.UnixFileMode]::UserRead -bor [System.IO.UnixFileMode]::UserWrite -bor [System.IO.UnixFileMode]::UserExecute)
            $env:PATH = "$fakeBin$([System.IO.Path]::PathSeparator)$oldPath"
        }
        $resolvedGh = Get-Command gh -ErrorAction Stop | Select-Object -First 1
        Assert-Crc ($resolvedGh.Source -eq $fakeGhPath) 'GitHub test did not resolve the fake gh command'
        $preflightOutput = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $review -Provider github -Repo 'example.invalid/council-test' -Pr 1 -Execute -Output $readOnlyOutput 2>&1
        $preflightExit = $LASTEXITCODE
    }
    finally {
        $env:PATH = $oldPath; $env:PATHEXT = $oldPathExt
        if ($null -eq $oldGhLog) { Remove-Item Env:CRC_GH_STUB_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_LOG = $oldGhLog }
        $readOnlyItem = Get-Item -LiteralPath $readOnlyOutput -Force; $readOnlyItem.IsReadOnly = $false
    }
    $preflightText = $preflightOutput -join [Environment]::NewLine
    Assert-Crc ($preflightExit -eq 2 -and $preflightText -match 'read-only') 'publisher did not reject an unwritable output before GitHub access'
    Assert-Crc (-not (Test-Path -LiteralPath $fakeGhLog)) 'publisher invoked gh before validating an unwritable output path'

    $noChangeMaterial = [ordered]@{ mode='branch'; status='NO_CHANGES'; base=('a'*40); head=('a'*40); merge_base=('a'*40); changed_files=@(); patch=[ordered]@{sha256=('0'*64);bytes=0;encoding='base64';content_base64=''} }
    $noChange = [ordered]@{schema_version=$script:CrcSchemaVersion;mode='branch';status='NO_CHANGES';snapshot_id=(Get-CrcHashJson $noChangeMaterial);repository=[ordered]@{root=$repo};captured_at_utc=(Get-CrcUtcNow);changed_files=@();snapshot_material=$noChangeMaterial}
    $noChangePath = Join-Path $root 'no-change.json'; $emptyReviewers = Join-Path $root 'empty.jsonl'; $emptyAdjudication=Join-Path $root 'empty-adjudication.json'; $noChangeOutput=Join-Path $root 'no-change-result.json'
    Write-CrcJson -Path $noChangePath -Value $noChange; Set-Content -LiteralPath $emptyReviewers -Value '' -Encoding utf8; Write-CrcJson -Path $emptyAdjudication -Value ([ordered]@{adjudicator='test-coordinator';created_at_utc=(Get-CrcUtcNow);snapshot_id=$noChange.snapshot_id;dispositions=@()})
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'validate-review.ps1') -Scope $noChangePath -Reviewers $emptyReviewers -Adjudication $emptyAdjudication -Output $noChangeOutput
    Assert-Crc ((Get-Content -LiteralPath $noChangeOutput -Raw | ConvertFrom-Json).status -eq 'NO_CHANGES') 'NO_CHANGES was not terminal'

    Write-CrcJson -Path $review -Value $validatedReview
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
