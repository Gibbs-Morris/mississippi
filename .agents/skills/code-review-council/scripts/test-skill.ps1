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

function Invoke-CrcValidationTest {
    param([string]$ScopePath,[string]$ReviewersPath,[string]$AdjudicationPath,[string]$OutputPath,[string]$MarkdownOutput)
    $arguments = @('-NoProfile','-File',(Join-Path $PSScriptRoot 'validate-review.ps1'),'-Scope',$ScopePath,'-Reviewers',$ReviewersPath,'-Adjudication',$AdjudicationPath,'-Output',$OutputPath)
    if ($MarkdownOutput) { $arguments += @('-MarkdownOutput',$MarkdownOutput) }
    $diagnostics = & pwsh @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $result = Get-Content -LiteralPath $OutputPath -Raw | ConvertFrom-Json
    return [pscustomobject]@{exit_code=$exitCode;result=$result;diagnostics=@($diagnostics)}
}

function Write-CrcReviewerJsonl {
    param([string]$Path,[object[]]$Records)
    $jsonl = @($Records | ForEach-Object { $_ | ConvertTo-Json -Depth 100 -Compress })
    Set-Content -LiteralPath $Path -Value ($jsonl -join [Environment]::NewLine) -Encoding utf8
}

function Test-CrcGitFsmonitorIsolation {
    param([string]$Repository,[string]$TemporaryRoot)

    $hookPath = Join-Path $TemporaryRoot 'fsmonitor-hook.ps1'
    $markerPath = Join-Path $TemporaryRoot 'fsmonitor-invoked.txt'
    $oldMarker = $env:CRC_TEST_FSMONITOR_MARKER
    $markerLiteral = $markerPath.Replace("'", "''")
    $hookSource = @'
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
Set-Content -LiteralPath __MARKER_PATH__ -Value 'invoked' -Encoding ascii | Out-Null
[Console]::WriteLine('test-token')
'@ -replace '__MARKER_PATH__', "'$markerLiteral'"
    Set-Content -LiteralPath $hookPath -Value $hookSource -Encoding utf8
    try {
        $env:CRC_TEST_FSMONITOR_MARKER = $markerPath
        $hookCommand = "pwsh -NoProfile -File `"$hookPath`""
        & git -C $Repository config core.fsmonitor $hookCommand
        $null = & git -C $Repository status --short 2>$null
        Assert-Crc (Test-Path -LiteralPath $markerPath) 'fsmonitor test fixture did not prove the configured command can execute'
        Remove-Item -LiteralPath $markerPath -Force

        $null = Invoke-CrcGit -Repository $Repository -Arguments @('status','--short')
        Assert-Crc (-not (Test-Path -LiteralPath $markerPath)) 'text Git wrapper executed configured core.fsmonitor'
        $null = Invoke-CrcGitBytes -Repository $Repository -Arguments @('status','--porcelain','-z')
        Assert-Crc (-not (Test-Path -LiteralPath $markerPath)) 'byte Git wrapper executed configured core.fsmonitor'
    }
    finally {
        $gitDirectory = Join-Path $Repository '.git'
        & git "--git-dir=$gitDirectory" config --unset core.fsmonitor 2>$null
        if ($null -eq $oldMarker) { Remove-Item Env:CRC_TEST_FSMONITOR_MARKER -ErrorAction SilentlyContinue } else { $env:CRC_TEST_FSMONITOR_MARKER = $oldMarker }
    }
}

function Test-CrcGitWorktreeRootValidation {
    param([string]$Repository)

    $escapedRoot = [System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($Repository))
    try {
        & git -C $Repository config core.worktree $escapedRoot
        $wasRejected = $false
        try { $null = Resolve-CrcRepository -Path $Repository } catch { $wasRejected = $_.Exception.Message -match 'repository root path must be within' }
        Assert-Crc $wasRejected 'repository core.worktree escaped the permitted root check'
    }
    finally {
        $gitDirectory = Join-Path $Repository '.git'
        & git "--git-dir=$gitDirectory" config --unset core.worktree 2>$null
    }
}

function Test-CrcGitReplacementIsolation {
    param([string]$Repository)

    $baseRevision = (& git -C $Repository rev-parse HEAD).Trim()
    try {
        Set-Content -LiteralPath (Join-Path $Repository 'tracked.txt') -Value 'replacement' -Encoding utf8
        & git -C $Repository add -- tracked.txt
        & git -C $Repository commit --quiet -m replacement
        $replacementRevision = (& git -C $Repository rev-parse HEAD).Trim()
        & git -C $Repository switch --quiet --detach $baseRevision
        & git -C $Repository replace $baseRevision $replacementRevision

        $nativeContent = (& git -C $Repository show "$baseRevision`:tracked.txt") -join "`n"
        Assert-Crc ($nativeContent -match 'replacement') 'replacement-object test fixture did not mask the original tree'
        $textContent = Invoke-CrcGit -Repository $Repository -Arguments @('show',"$baseRevision`:tracked.txt")
        $byteContent = [System.Text.Encoding]::UTF8.GetString((Invoke-CrcGitBytes -Repository $Repository -Arguments @('show',"$baseRevision`:tracked.txt")))
        Assert-Crc ($textContent -match 'base' -and $byteContent -match 'base') 'Git wrappers followed a replacement object while freezing evidence'
    }
    finally {
        if ($baseRevision) {
            & git -C $Repository replace -d $baseRevision 2>$null
            & git -C $Repository switch --quiet --detach $baseRevision 2>$null
        }
    }
}

function Test-CrcSpecialUntrackedRejection {
    param([string]$Repository,[string]$TemporaryRoot)

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { return }
    if ($null -eq (Get-Command mkfifo -ErrorAction SilentlyContinue)) { return }
    $fifoPath = Join-Path $Repository 'untracked-review-fifo'
    $outputPath = Join-Path $TemporaryRoot 'special-untracked-scope.json'
    & mkfifo $fifoPath
    try {
        $diagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode worktree -Repo $Repository -Changes all -Output $outputPath 2>&1
        $exitCode = $LASTEXITCODE
        $diagnosticText = $diagnostics -join [Environment]::NewLine
        Assert-Crc ($exitCode -eq 2 -and $diagnosticText -match 'not a regular file') 'collector did not reject an untracked FIFO before reading it'
    }
    finally { Remove-Item -LiteralPath $fifoPath -Force -ErrorAction SilentlyContinue }
}

function Test-CrcPatchBytePreservation {
    param([string]$TemporaryRoot)

    $patchRepository = Join-Path $TemporaryRoot 'raw-patch-repository'
    New-Item -ItemType Directory -Path $patchRepository -Force | Out-Null
    & git -C $patchRepository init --quiet
    $emptyPatchHooks = Join-Path $TemporaryRoot 'empty-patch-hooks'
    New-Item -ItemType Directory -Path $emptyPatchHooks -Force | Out-Null
    & git -C $patchRepository config core.hooksPath $emptyPatchHooks
    & git -C $patchRepository config user.name 'Council Test'
    & git -C $patchRepository config user.email 'council@example.invalid'
    & git -C $patchRepository config commit.gpgSign false
    Set-Content -LiteralPath (Join-Path $patchRepository 'baseline.txt') -Value 'base' -Encoding utf8
    & git -C $patchRepository add -- baseline.txt
    & git -C $patchRepository commit --quiet -m base
    $binaryPath = Join-Path $patchRepository 'binary.dat'
    [System.IO.File]::WriteAllBytes($binaryPath,[byte[]]@(0,255,254,10,65))
    & git -C $patchRepository add -- binary.dat
    $submoduleRepository = Join-Path $TemporaryRoot 'gitlink-target'
    New-Item -ItemType Directory -Path $submoduleRepository -Force | Out-Null
    & git -C $submoduleRepository init --quiet
    $emptySubmoduleHooks = Join-Path $TemporaryRoot 'empty-submodule-hooks'
    New-Item -ItemType Directory -Path $emptySubmoduleHooks -Force | Out-Null
    & git -C $submoduleRepository config core.hooksPath $emptySubmoduleHooks
    & git -C $submoduleRepository config commit.gpgSign false
    & git -C $submoduleRepository config user.name 'Council Test'
    & git -C $submoduleRepository config user.email 'council@example.invalid'
    Set-Content -LiteralPath (Join-Path $submoduleRepository 'submodule.txt') -Value 'gitlink' -Encoding utf8
    & git -C $submoduleRepository add -- submodule.txt
    & git -C $submoduleRepository commit --quiet -m gitlink
    $submoduleRevision = (& git -C $submoduleRepository rev-parse HEAD).Trim()
    & git -C $patchRepository config diff.ignoreSubmodules all
    & git -C $patchRepository config diff.mnemonicPrefix true
    & git -C $patchRepository update-index --add --cacheinfo "160000,$submoduleRevision,submodule"
    $expectedArguments = @('diff','--cached','--binary','--find-renames','--default-prefix','--ignore-submodules=none','--no-ext-diff','--no-textconv')
    $expectedBytes = Invoke-CrcGitBytes -Repository $patchRepository -Arguments $expectedArguments
    $scopePath = Join-Path $TemporaryRoot 'raw-patch-scope.json'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode worktree -Repo $patchRepository -Changes staged -Output $scopePath
    $scope = Get-Content -LiteralPath $scopePath -Raw | ConvertFrom-Json
    $actualBytes = [Convert]::FromBase64String([string]$scope.selected.staged.patch.content_base64)
    Assert-Crc (([Convert]::ToBase64String($actualBytes)) -eq ([Convert]::ToBase64String($expectedBytes))) 'patch collector changed native Git diff bytes'
    Assert-Crc ((Get-CrcSha256 -Bytes $actualBytes) -eq $scope.selected.staged.patch.sha256) 'patch byte hash does not match persisted native bytes'
    Assert-Crc (@($scope.selected.staged.files | Where-Object path -eq 'submodule').Count -eq 1) 'diff.ignoreSubmodules=all hid a staged gitlink change'
    $patchText = [System.Text.Encoding]::ASCII.GetString($actualBytes)
    Assert-Crc ($patchText -match '(?m)^diff --git a/binary.dat b/binary.dat$') 'collector did not force canonical diff prefixes'
}

function Test-CrcSha256RepositorySupport {
    param([string]$TemporaryRoot)

    $repository = Join-Path $TemporaryRoot 'sha256-repository'
    New-Item -ItemType Directory -Path $repository -Force | Out-Null
    & git -C $repository init --quiet --object-format=sha256 2>$null
    if ($LASTEXITCODE -ne 0) { return }
    $hooks = Join-Path $TemporaryRoot 'sha256-empty-hooks'
    New-Item -ItemType Directory -Path $hooks -Force | Out-Null
    & git -C $repository config core.hooksPath $hooks
    & git -C $repository config commit.gpgSign false
    & git -C $repository config user.name 'Council Test'
    & git -C $repository config user.email 'council@example.invalid'
    Set-Content -LiteralPath (Join-Path $repository 'sha256.txt') -Value 'sha256 repository' -Encoding utf8
    & git -C $repository add -- sha256.txt
    & git -C $repository commit --quiet -m sha256
    $scopePath = Join-Path $TemporaryRoot 'sha256-scope.json'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode codebase -Repo $repository -Output $scopePath
    $scope = Get-Content -LiteralPath $scopePath -Raw | ConvertFrom-Json
    Assert-Crc ($scope.status -eq 'READY' -and $scope.revision.Length -eq 64) 'collector rejected a Git SHA-256 repository'
}

function Test-CrcAmbiguousMergeBaseRejection {
    param([string]$TemporaryRoot)

    $repository = Join-Path $TemporaryRoot 'criss-cross-repository'
    New-Item -ItemType Directory -Path $repository -Force | Out-Null
    & git -C $repository init --quiet
    $hooks = Join-Path $TemporaryRoot 'merge-empty-hooks'
    New-Item -ItemType Directory -Path $hooks -Force | Out-Null
    & git -C $repository config core.hooksPath $hooks
    & git -C $repository config commit.gpgSign false
    & git -C $repository config user.name 'Council Test'
    & git -C $repository config user.email 'council@example.invalid'
    Set-Content -LiteralPath (Join-Path $repository 'root.txt') -Value 'root' -Encoding utf8
    & git -C $repository add -- root.txt
    & git -C $repository commit --quiet -m root
    $rootRevision = (& git -C $repository rev-parse HEAD).Trim()
    & git -C $repository checkout --quiet -b branch-a
    Set-Content -LiteralPath (Join-Path $repository 'a.txt') -Value 'a' -Encoding utf8
    & git -C $repository add -- a.txt
    & git -C $repository commit --quiet -m branch-a
    $aRevision = (& git -C $repository rev-parse HEAD).Trim()
    & git -C $repository checkout --quiet -b branch-b $rootRevision
    Set-Content -LiteralPath (Join-Path $repository 'b.txt') -Value 'b' -Encoding utf8
    & git -C $repository add -- b.txt
    & git -C $repository commit --quiet -m branch-b
    $bRevision = (& git -C $repository rev-parse HEAD).Trim()
    & git -C $repository merge --quiet --no-ff --no-edit $aRevision
    $mergeTwo = (& git -C $repository rev-parse HEAD).Trim()
    & git -C $repository checkout --quiet branch-a
    & git -C $repository merge --quiet --no-ff --no-edit $bRevision
    $mergeOne = (& git -C $repository rev-parse HEAD).Trim()
    $bestBases = @(Get-CrcGitLines -Repository $repository -Arguments @('merge-base','--all',$mergeOne,$mergeTwo))
    Assert-Crc ($bestBases.Count -eq 2) 'criss-cross test fixture did not produce multiple best merge bases'
    $scopePath = Join-Path $TemporaryRoot 'ambiguous-merge-scope.json'
    $diagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode branch -Repo $repository -Base $mergeOne -Head $mergeTwo -Output $scopePath 2>&1
    $blockedScope = Get-Content -LiteralPath $scopePath -Raw | ConvertFrom-Json
    Assert-Crc ($LASTEXITCODE -eq 2 -and $blockedScope.error -match 'exactly one best merge base') 'collector selected one of multiple best merge bases'
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
    $emptyGitHooks = Join-Path $root 'empty-git-hooks'
    New-Item -ItemType Directory -Path $emptyGitHooks -Force | Out-Null
    & git config core.hooksPath $emptyGitHooks
    & git config commit.gpgSign false
    & git config user.name 'Council Test'
    & git config user.email 'council@example.invalid'
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'base' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'multiple file.txt') -Value 'base' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'café.txt') -Value 'base' -Encoding utf8
    & git add -- tracked.txt 'multiple file.txt' 'café.txt'
    & git commit --quiet -m base
    Test-CrcGitFsmonitorIsolation -Repository $repo -TemporaryRoot $root
    Test-CrcGitWorktreeRootValidation -Repository $repo
    Test-CrcGitReplacementIsolation -Repository $repo
    Test-CrcPatchBytePreservation -TemporaryRoot $root
    Test-CrcSha256RepositorySupport -TemporaryRoot $root
    Test-CrcAmbiguousMergeBaseRejection -TemporaryRoot $root
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'staged defect' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'multiple file.txt') -Value 'staged change' -Encoding utf8
    & git add -- tracked.txt 'multiple file.txt'
    Set-Content -LiteralPath (Join-Path $repo 'tracked.txt') -Value 'unstaged correction' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'untracked file.txt') -Value 'new' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $repo 'café untracked.txt') -Value 'new' -Encoding utf8
    $posixUntrackedPaths = @()
    if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $posixUntrackedPaths = @('literal\backslash.txt',"tab$([char]9)file.txt","line$([char]10)break.txt")
        foreach($unusualPath in $posixUntrackedPaths){Set-Content -LiteralPath (Join-Path $repo $unusualPath) -Value 'new' -Encoding utf8}
    }
    Pop-Location

    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode worktree -Repo $repo -Changes all -Output $scope
    Assert-Crc ((Get-Content -LiteralPath $scope -Raw | ConvertFrom-Json).status -eq 'READY') 'worktree collection did not produce READY'
    $scopeObject = Get-Content -LiteralPath $scope -Raw | ConvertFrom-Json
    Assert-Crc ([string]::IsNullOrWhiteSpace([string]$scopeObject.selected.staged.patch.content_base64) -eq $false) 'staged patch bytes were not persisted'
    Assert-Crc (@($scopeObject.selected.staged.files).Count -eq 2) 'NUL-delimited staged diff lost a file record'
    Assert-Crc (@($scopeObject.selected.untracked | Where-Object path -eq 'café untracked.txt').Count -eq 1) 'NUL-delimited status changed a non-ASCII untracked path'
    Assert-Crc (@($scopeObject.selected.untracked).Count -eq (2 + $posixUntrackedPaths.Count)) 'NUL-delimited status lost an untracked file record'
    foreach($unusualPath in $posixUntrackedPaths){Assert-Crc (@($scopeObject.selected.untracked | Where-Object path -ceq $unusualPath).Count -eq 1) "NUL-delimited status changed an unusual path: $unusualPath"}
    $codebaseScope = Join-Path $root 'codebase.json'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode codebase -Repo $repo -Output $codebaseScope
    $codebaseManifest = Get-Content -LiteralPath $codebaseScope -Raw | ConvertFrom-Json
    Assert-Crc (@($codebaseManifest.files).Count -eq 3) 'NUL-delimited tree parser lost a tracked file record'
    Push-Location $repo
    $baseRevision = (& git rev-parse HEAD).Trim()
    & git add -A
    & git commit --quiet -m feature
    $headRevision = (& git rev-parse HEAD).Trim()
    Pop-Location
    $branchScopePath = Join-Path $root 'branch.json'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode branch -Repo $repo -Base $baseRevision -Head $headRevision -Output $branchScopePath
    $branchManifest = Get-Content -LiteralPath $branchScopePath -Raw | ConvertFrom-Json
    Assert-Crc (@($branchManifest.changed_files).Count -eq (4 + $posixUntrackedPaths.Count)) 'NUL-delimited branch diff lost file records'
    Assert-Crc (@($branchManifest.changed_files | Where-Object path -eq 'café untracked.txt').Count -eq 1) 'branch diff changed a non-ASCII path'
    Test-CrcSpecialUntrackedRejection -Repository $repo -TemporaryRoot $root

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

    $injectedReviewers = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    $injectedReviewers[0].findings[0].scenario = "scenario`n# forged heading"
    $injectedReviewers[0].findings[0].evidence = @('@all <https://example.invalid> [link](https://example.invalid)')
    $injectedReviewersPath = Join-Path $root 'markdown-injection-reviewers.jsonl'; $injectedAdjudicationPath = Join-Path $root 'markdown-injection-adjudication.json'; $injectedReviewPath = Join-Path $root 'markdown-injection-review.json'; $injectedMarkdownPath = Join-Path $root 'markdown-injection-review.md'
    Write-CrcReviewerJsonl -Path $injectedReviewersPath -Records $injectedReviewers
    $injectedAdjudication = Get-Content -LiteralPath $adjudication -Raw | ConvertFrom-Json
    $injectedAdjudication.dispositions[0].rationale = "@team`n# forged disposition"
    Write-CrcJson -Path $injectedAdjudicationPath -Value $injectedAdjudication
    $injectedResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $injectedReviewersPath -AdjudicationPath $injectedAdjudicationPath -OutputPath $injectedReviewPath -MarkdownOutput $injectedMarkdownPath
    $injectedMarkdown = Get-Content -LiteralPath $injectedMarkdownPath -Raw
    Assert-Crc ($injectedResult.result.status -eq 'PASS' -and $injectedMarkdown.Contains('\@all') -and $injectedMarkdown.Contains('\<') -and $injectedMarkdown -notmatch '(?m)^# forged heading|(?m)^# forged disposition') 'generated Markdown exposed reviewer-controlled formatting or mentions'

    $malformedScopePath = Join-Path $root 'malformed-scope.json'; $malformedResultPath = Join-Path $root 'malformed-scope-review.json'
    Set-Content -LiteralPath $malformedScopePath -Value '{invalid json' -Encoding utf8
    $malformedResult = Invoke-CrcValidationTest -ScopePath $malformedScopePath -ReviewersPath $reviewers -AdjudicationPath $adjudication -OutputPath $malformedResultPath
    Assert-Crc ($malformedResult.exit_code -eq 2 -and $malformedResult.result.status -eq 'INCOMPLETE' -and $null -ne $malformedResult.result.scope_manifest) 'validator failure result omitted the required scope manifest'
    Assert-Crc ($malformedResult.result.snapshot_id -eq $malformedResult.result.scope_manifest.snapshot_id -and (Get-CrcHashJson -Value $malformedResult.result.scope_manifest.snapshot_material) -eq $malformedResult.result.scope_manifest.snapshot_id) 'validator failure scope was not schema-consistent and hash-valid'

    $hashMaterialA = [ordered]@{mode='branch';status='READY';nested=[ordered]@{second=2;first=1};items=@('one','two')}
    $hashMaterialB = [ordered]@{items=@('one','two');nested=[ordered]@{first=1;second=2};status='READY';mode='branch'}
    Assert-Crc ((Get-CrcHashJson -Value $hashMaterialA) -eq (Get-CrcHashJson -Value $hashMaterialB)) 'snapshot hashing changed with JSON object key order'
    $quotedDiff = 'diff --git "a/caf\303\251 file.txt" "b/caf\303\251 file.txt"' + "`n@@ -1 +1 @@`n-old`n+new`n"
    $quotedPaths = Get-CrcDiffHeaderPaths -DiffText $quotedDiff
    Assert-Crc ($quotedPaths.Contains('café file.txt') -and (Test-CrcPatchContainsLine -Patch $quotedDiff -Path 'café file.txt' -Line 1)) 'quoted Git diff paths did not decode to their exact path'

    $staleReviewersPath = Join-Path $root 'stale-reviewers.jsonl'; $staleOutput = Join-Path $root 'stale-review.json'
    $staleReviewerObjects = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json }); $staleReviewerObjects[0].snapshot_id = 'sha256:' + ('3'*64)
    Write-CrcReviewerJsonl -Path $staleReviewersPath -Records $staleReviewerObjects
    $staleResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $staleReviewersPath -AdjudicationPath $adjudication -OutputPath $staleOutput
    Assert-Crc ($staleResult.result.status -eq 'INCOMPLETE' -and @($staleResult.result.errors | Where-Object { $_ -match 'snapshot_id differs from scope' }).Count -gt 0) 'stale reviewer snapshot was accepted'

    $invalidMetadataPath = Join-Path $root 'invalid-metadata.jsonl'; $invalidMetadataOutput = Join-Path $root 'invalid-metadata.json'
    $invalidMetadata = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json }); $invalidMetadata[0].review_id = ''; $invalidMetadata[1].effective_concurrency = 0
    Write-CrcReviewerJsonl -Path $invalidMetadataPath -Records $invalidMetadata
    $invalidMetadataResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $invalidMetadataPath -AdjudicationPath $adjudication -OutputPath $invalidMetadataOutput
    Assert-Crc ($invalidMetadataResult.result.status -eq 'INCOMPLETE' -and @($invalidMetadataResult.result.errors | Where-Object { $_ -match 'review_id is required|positive integer' }).Count -gt 0) 'invalid reviewer metadata was accepted'

    $duplicateIdRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json }); $duplicateIdRecords[1].review_id = $duplicateIdRecords[0].review_id
    $duplicateIdReviewers = Join-Path $root 'duplicate-reviewer-ids.jsonl'; $duplicateIdOutput = Join-Path $root 'duplicate-reviewer-ids-review.json'
    Write-CrcReviewerJsonl -Path $duplicateIdReviewers -Records $duplicateIdRecords
    $duplicateIdResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $duplicateIdReviewers -AdjudicationPath $adjudication -OutputPath $duplicateIdOutput
    Assert-Crc ($duplicateIdResult.result.status -eq 'INCOMPLETE' -and @($duplicateIdResult.result.errors | Where-Object { $_ -match 'duplicate reviewer ID' }).Count -gt 0) 'validator accepted duplicate reviewer run IDs'

    $invalidAnchorRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json }); $invalidAnchorRecords[0].findings[0].line = 999
    $invalidAnchorPath = Join-Path $root 'invalid-anchor.jsonl'; $invalidAnchorOutput = Join-Path $root 'invalid-anchor-review.json'
    Write-CrcReviewerJsonl -Path $invalidAnchorPath -Records $invalidAnchorRecords
    $invalidAnchorResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $invalidAnchorPath -AdjudicationPath $adjudication -OutputPath $invalidAnchorOutput
    Assert-Crc ($invalidAnchorResult.result.status -eq 'INCOMPLETE' -and @($invalidAnchorResult.result.errors | Where-Object { $_ -match 'outside the captured evidence' }).Count -gt 0) 'finding line outside captured patch was accepted'

    $untrackedFindingRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    $untrackedFindingRecords[0].findings[0].path = 'café untracked.txt'
    $untrackedFindingReviewers = Join-Path $root 'untracked-finding-reviewers.jsonl'; $untrackedFindingOutput = Join-Path $root 'untracked-finding-review.json'
    Write-CrcReviewerJsonl -Path $untrackedFindingReviewers -Records $untrackedFindingRecords
    $untrackedFindingResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $untrackedFindingReviewers -AdjudicationPath $adjudication -OutputPath $untrackedFindingOutput
    Assert-Crc ($untrackedFindingResult.result.status -eq 'PASS') 'validator rejected a finding anchored in captured untracked content'

    $invalidLineCases = @(
        [pscustomobject]@{name='boolean';value=$true},
        [pscustomobject]@{name='fraction';value=[double]1.5}
    )
    foreach ($lineCase in $invalidLineCases) {
        $invalidLineRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
        $invalidLineRecords[0].findings[0].line = $lineCase.value
        $invalidLineReviewers = Join-Path $root "invalid-line-$($lineCase.name).jsonl"
        $invalidLineOutput = Join-Path $root "invalid-line-$($lineCase.name)-review.json"
        Write-CrcReviewerJsonl -Path $invalidLineReviewers -Records $invalidLineRecords
        $invalidLineResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $invalidLineReviewers -AdjudicationPath $adjudication -OutputPath $invalidLineOutput
        Assert-Crc ($invalidLineResult.result.status -eq 'INCOMPLETE' -and @($invalidLineResult.result.errors | Where-Object { $_ -match 'line must be a positive integer' }).Count -gt 0) "finding line accepted a $($lineCase.name) value"
    }

    $invalidClassificationCases = @(
        [pscustomobject]@{name='severity';value='p2'},
        [pscustomobject]@{name='change_relation';value='INTRODUCED'}
    )
    foreach ($classificationCase in $invalidClassificationCases) {
        $classificationRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
        $classificationRecords[0].findings[0].PSObject.Properties[$classificationCase.name].Value = $classificationCase.value
        $classificationReviewers = Join-Path $root "invalid-$($classificationCase.name)-case.jsonl"
        $classificationOutput = Join-Path $root "invalid-$($classificationCase.name)-case-review.json"
        Write-CrcReviewerJsonl -Path $classificationReviewers -Records $classificationRecords
        $classificationResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $classificationReviewers -AdjudicationPath $adjudication -OutputPath $classificationOutput
        Assert-Crc ($classificationResult.result.status -eq 'INCOMPLETE' -and @($classificationResult.result.errors | Where-Object { $_ -match "$($classificationCase.name) is invalid" }).Count -gt 0) "validator accepted noncanonical $($classificationCase.name) casing"
    }

    $sharedEvidenceRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    $sharedFinding = $sharedEvidenceRecords[0].findings[0] | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $sharedFinding.persona_ids = @($sharedEvidenceRecords[1].persona_id); $sharedFinding.evidence = @('independent second-persona evidence'); $sharedFinding.change_relation = 'worsened'
    $sharedEvidenceRecords[1].findings = @($sharedFinding)
    $sharedEvidencePath = Join-Path $root 'shared-evidence.jsonl'; $sharedEvidenceOutput = Join-Path $root 'shared-evidence-review.json'
    Write-CrcReviewerJsonl -Path $sharedEvidencePath -Records $sharedEvidenceRecords
    $sharedEvidenceResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $sharedEvidencePath -AdjudicationPath $adjudication -OutputPath $sharedEvidenceOutput
    $sharedFindingResult = $sharedEvidenceResult.result.findings | Where-Object fingerprint -eq $findingFingerprint
    Assert-Crc ($sharedEvidenceResult.result.status -eq 'PASS' -and $sharedFindingResult.persona_ids.Count -eq 2 -and $sharedFindingResult.evidence.Count -eq 2 -and $sharedFindingResult.reviewer_evidence.Count -eq 2 -and $sharedFindingResult.change_relation -eq 'unknown') 'shared-fingerprint evidence was lost or collapsed incorrectly'

    $severityConflictPath = Join-Path $root 'severity-conflict.jsonl'; $severityConflictOutput = Join-Path $root 'severity-conflict.json'
    $severityConflict = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    $secondFinding = $severityConflict[0].findings[0] | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $secondFinding.persona_ids = @($severityConflict[1].persona_id); $secondFinding.severity = 'P1'; $severityConflict[1].findings = @($secondFinding)
    Write-CrcReviewerJsonl -Path $severityConflictPath -Records $severityConflict
    $severityConflictResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $severityConflictPath -AdjudicationPath $adjudication -OutputPath $severityConflictOutput
    Assert-Crc ($severityConflictResult.result.status -eq 'INCOMPLETE' -and @($severityConflictResult.result.errors | Where-Object { $_ -match 'conflicting severity' }).Count -gt 0) 'conflicting shared-fingerprint severity was accepted'

    $branchReviewerObjects = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    foreach ($record in $branchReviewerObjects) { $record.snapshot_id=$branchManifest.snapshot_id; foreach ($finding in $record.findings) { $finding.snapshot_id=$branchManifest.snapshot_id } }
    $branchReviewerPath = Join-Path $root 'branch-reviewers.jsonl'; Write-CrcReviewerJsonl -Path $branchReviewerPath -Records $branchReviewerObjects
    $branchAdjudication = Get-Content -LiteralPath $adjudication -Raw | ConvertFrom-Json
    $branchAdjudication.snapshot_id=$branchManifest.snapshot_id; foreach ($entry in $branchAdjudication.dispositions) { $entry.snapshot_id=$branchManifest.snapshot_id }
    $branchAdjudicationPath = Join-Path $root 'branch-adjudication.json'; Write-CrcJson -Path $branchAdjudicationPath -Value $branchAdjudication
    $tamperedBranch = $branchManifest | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $tamperedBranch.patch.content_base64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes('forged patch'))
    $tamperedBranchPath = Join-Path $root 'tampered-branch-patch.json'; $tamperedBranchOutput = Join-Path $root 'tampered-branch-review.json'
    Write-CrcJson -Path $tamperedBranchPath -Value $tamperedBranch
    $tamperedBranchResult = Invoke-CrcValidationTest -ScopePath $tamperedBranchPath -ReviewersPath $branchReviewerPath -AdjudicationPath $branchAdjudicationPath -OutputPath $tamperedBranchOutput
    Assert-Crc ($tamperedBranchResult.result.status -eq 'INCOMPLETE' -and @($tamperedBranchResult.result.errors | Where-Object { $_ -match 'mirrored field differs: patch' }).Count -gt 0) 'validator accepted an unhashed branch patch mirror'

    $duplicateDispositionPath = Join-Path $root 'duplicate-disposition.json'; $duplicateDispositionOutput = Join-Path $root 'duplicate-disposition-result.json'
    $duplicateDisposition = Get-Content -LiteralPath $adjudication -Raw | ConvertFrom-Json
    $duplicateDisposition.dispositions = @($duplicateDisposition.dispositions) + @([pscustomobject]@{fingerprint=$findingFingerprint;disposition='rejected';rationale='conflicting duplicate test disposition';snapshot_id=$scopeObject.snapshot_id})
    Write-CrcJson -Path $duplicateDispositionPath -Value $duplicateDisposition
    $duplicateDispositionResult = Invoke-CrcValidationTest -ScopePath $scope -ReviewersPath $reviewers -AdjudicationPath $duplicateDispositionPath -OutputPath $duplicateDispositionOutput
    Assert-Crc ($duplicateDispositionResult.result.status -eq 'INCOMPLETE' -and @($duplicateDispositionResult.result.errors | Where-Object { $_ -match 'duplicate disposition fingerprint' }).Count -gt 0) 'duplicate adjudication fingerprints were accepted'

    $tamperedScopePath = Join-Path $root 'tampered-scope.json'; $tamperedScopeOutput = Join-Path $root 'tampered-scope-result.json'
    $tamperedScope = $scopeObject | ConvertTo-Json -Depth 100 | ConvertFrom-Json; $tamperedScope.status = 'NO_CHANGES'
    Write-CrcJson -Path $tamperedScopePath -Value $tamperedScope
    $tamperedScopeResult = Invoke-CrcValidationTest -ScopePath $tamperedScopePath -ReviewersPath $reviewers -AdjudicationPath $adjudication -OutputPath $tamperedScopeOutput
    Assert-Crc ($tamperedScopeResult.result.status -eq 'INCOMPLETE' -and @($tamperedScopeResult.result.errors | Where-Object { $_ -match 'snapshot_id does not match|mirrored field differs: status' }).Count -gt 0) 'tampered scope status was accepted'

    $missingHashedStatus = $scopeObject | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $missingHashedStatus.snapshot_material.PSObject.Properties.Remove('status')
    $missingHashedStatus.snapshot_id = Get-CrcHashJson -Value $missingHashedStatus.snapshot_material
    $missingHashedStatusPath = Join-Path $root 'missing-hashed-status.json'; $missingHashedStatusOutput = Join-Path $root 'missing-hashed-status-review.json'
    Write-CrcJson -Path $missingHashedStatusPath -Value $missingHashedStatus
    $missingHashedStatusResult = Invoke-CrcValidationTest -ScopePath $missingHashedStatusPath -ReviewersPath $reviewers -AdjudicationPath $adjudication -OutputPath $missingHashedStatusOutput
    Assert-Crc ($missingHashedStatusResult.result.status -eq 'INCOMPLETE' -and @($missingHashedStatusResult.result.errors | Where-Object { $_ -match 'snapshot_material.status is missing' }).Count -gt 0) 'scope without a hashed status was accepted'

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
$fakeGhPostLog = Join-Path $root 'fake-gh-posted.txt'
$fakeGhBodyLog = Join-Path $root 'fake-gh-body.json'
    $fakeGhScriptPath = Join-Path $fakeBin 'gh-stub.ps1'
    Set-Content -LiteralPath $fakeGhScriptPath -Value @'
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$argumentText = $Arguments -join ' '
Add-Content -LiteralPath $env:CRC_GH_STUB_LOG -Value $argumentText -Encoding utf8
if ($Arguments -contains 'POST') {
    Add-Content -LiteralPath $env:CRC_GH_STUB_POST_LOG -Value 'POST' -Encoding utf8
    Set-Content -LiteralPath $env:CRC_GH_STUB_BODY_LOG -Value ([Console]::In.ReadToEnd()) -Encoding utf8
    Write-Output '{"id":1}'
    exit 0
}
$route = $Arguments | Where-Object { $_ -like 'repos/*' } | Select-Object -First 1
if ($argumentText -match 'application/vnd.github.diff') {
    $diffPath = if ($env:CRC_GH_STUB_DIFF_MODE -eq 'mismatched-files') { 'ghost.txt' } else { 'tracked.txt' }
    $line = if ($env:CRC_GH_STUB_DIFF_MODE -in @('valid-anchor','stale-revision')) { 1 } else { 999 }
    Write-Output "diff --git a/$diffPath b/$diffPath"
    Write-Output 'index 1111111..2222222 100644'
    Write-Output "--- a/$diffPath"
    Write-Output "+++ b/$diffPath"
    Write-Output "@@ -$line +$line @@"
    Write-Output '-old'
    Write-Output '+new'
    exit 0
}
if ($route -like 'repos/*/pulls/1/files') {
    if ($env:CRC_GH_STUB_DIFF_MODE -eq 'too-many-files') {
        $apiFiles = [System.Collections.Generic.List[object]]::new()
        $apiFiles.Add([pscustomobject]@{filename='tracked.txt';status='modified'})
        for ($index=1; $index -lt 3000; $index++) { $apiFiles.Add([pscustomobject]@{filename="other-$index.txt";status='modified'}) }
        ConvertTo-Json -InputObject $apiFiles.ToArray() -Depth 5 -Compress
    }
    else { Write-Output '[[{"filename":"tracked.txt","status":"modified"}]]' }
    exit 0
}
if ($route -like 'repos/*/pulls/1') {
    $prReads = @((Get-Content -LiteralPath $env:CRC_GH_STUB_LOG -ErrorAction SilentlyContinue) | Where-Object { $_ -match 'repos/.*/pulls/1$' }).Count
    $reportedHead = if ($env:CRC_GH_STUB_DIFF_MODE -eq 'stale-revision' -and $prReads -ge 2) { 'c'*40 } else { $env:CRC_GH_STUB_HEAD }
    [ordered]@{state='open';base=[ordered]@{sha=$env:CRC_GH_STUB_BASE};head=[ordered]@{sha=$reportedHead}} | ConvertTo-Json -Compress
    exit 0
}
if ($Arguments -contains 'user') { Write-Output '{"login":"council-test"}'; exit 0 }
if ($route -like 'repos/*/issues/1/comments') {
    if ($env:CRC_GH_STUB_COMMENT_BODY) {
        $bodyJson = ConvertTo-Json -InputObject $env:CRC_GH_STUB_COMMENT_BODY -Compress
        Write-Output "[[{`"user`":{"`"login`":"`"rotated-publisher`"},`"body`":$bodyJson}]]"
    }
    else { Write-Output '[[]]' }
    exit 0
}
Write-Error "Unexpected fake gh request: $argumentText"
exit 97
'@ -Encoding utf8
    $oldPath = $env:PATH; $oldPathExt = $env:PATHEXT; $oldGhLog = $env:CRC_GH_STUB_LOG
    $oldGhPostLog = $env:CRC_GH_STUB_POST_LOG; $oldGhBodyLog = $env:CRC_GH_STUB_BODY_LOG; $oldGhBase = $env:CRC_GH_STUB_BASE; $oldGhHead = $env:CRC_GH_STUB_HEAD; $oldGhDiffMode = $env:CRC_GH_STUB_DIFF_MODE; $oldGhCommentBody = $env:CRC_GH_STUB_COMMENT_BODY
    try {
        $env:CRC_GH_STUB_LOG = $fakeGhLog
        $env:CRC_GH_STUB_POST_LOG = $fakeGhPostLog
        $env:CRC_GH_STUB_BODY_LOG = $fakeGhBodyLog
        if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            $fakeGhPath = Join-Path $fakeBin 'gh.cmd'
            Set-Content -LiteralPath $fakeGhPath -Value @('@echo off','pwsh -NoProfile -File "%~dp0gh-stub.ps1" %*','exit /b %ERRORLEVEL%') -Encoding ascii
            $env:PATH = "$fakeBin;$oldPath"
            $env:PATHEXT = ".CMD;$oldPathExt"
        }
        else {
            $fakeGhPath = Join-Path $fakeBin 'gh'
            [System.IO.File]::WriteAllText($fakeGhPath,"#!/usr/bin/env pwsh`n& (Join-Path `$PSScriptRoot 'gh-stub.ps1') @args`nexit `$LASTEXITCODE`n",[System.Text.UTF8Encoding]::new($false))
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
        if ($null -eq $oldGhPostLog) { Remove-Item Env:CRC_GH_STUB_POST_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_POST_LOG = $oldGhPostLog }
        if ($null -eq $oldGhBodyLog) { Remove-Item Env:CRC_GH_STUB_BODY_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_BODY_LOG = $oldGhBodyLog }
        if ($null -eq $oldGhBase) { Remove-Item Env:CRC_GH_STUB_BASE -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_BASE = $oldGhBase }
        if ($null -eq $oldGhHead) { Remove-Item Env:CRC_GH_STUB_HEAD -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_HEAD = $oldGhHead }
        if ($null -eq $oldGhDiffMode) { Remove-Item Env:CRC_GH_STUB_DIFF_MODE -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_DIFF_MODE = $oldGhDiffMode }
        $readOnlyItem = Get-Item -LiteralPath $readOnlyOutput -Force; $readOnlyItem.IsReadOnly = $false
    }
    $preflightText = $preflightOutput -join [Environment]::NewLine
    Assert-Crc ($preflightExit -eq 2 -and $preflightText -match 'read-only') 'publisher did not reject an unwritable output before GitHub access'
    Assert-Crc (-not (Test-Path -LiteralPath $fakeGhLog)) 'publisher invoked gh before validating an unwritable output path'

    $fakeBase = $baseRevision; $fakeHead = $headRevision
    $prDiff = @('diff --git a/tracked.txt b/tracked.txt','index 1111111..2222222 100644','--- a/tracked.txt','+++ b/tracked.txt','@@ -1 +1 @@','-old','+new') -join "`n"
    $prSnapshotPath = Join-Path $root 'pull-request-snapshot.json'; $prScopePath = Join-Path $root 'pull-request-scope.json'
    Write-CrcJson -Path $prSnapshotPath -Value ([ordered]@{repository='example.invalid/council-test';number=1;base_sha=$fakeBase;head_sha=$fakeHead;changed_files=@([ordered]@{status='M';path='tracked.txt'});diff=$prDiff;discussion=@();checks=@()})
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode pull-request -Repo $repo -PullRequestSnapshot $prSnapshotPath -Output $prScopePath
    $prScope = Get-Content -LiteralPath $prScopePath -Raw | ConvertFrom-Json
    $fabricatedPrSnapshot = Get-Content -LiteralPath $prSnapshotPath -Raw | ConvertFrom-Json
    $fabricatedPrSnapshot.changed_files[0].path = 'ghost.txt'; $fabricatedPrSnapshot.diff += "`n+ghost.txt is only mentioned in this added line"
    $fabricatedPrPath = Join-Path $root 'fabricated-pr-snapshot.json'; $fabricatedPrScopePath = Join-Path $root 'fabricated-pr-scope.json'
    Write-CrcJson -Path $fabricatedPrPath -Value $fabricatedPrSnapshot
    $fabricatedPrDiagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode pull-request -Repo $repo -PullRequestSnapshot $fabricatedPrPath -Output $fabricatedPrScopePath 2>&1
    $fabricatedPrExit = $LASTEXITCODE; $fabricatedPrScope = Get-Content -LiteralPath $fabricatedPrScopePath -Raw | ConvertFrom-Json
    Assert-Crc ($fabricatedPrExit -eq 2 -and $fabricatedPrScope.status -eq 'BLOCKED' -and $fabricatedPrScope.error -match 'diff headers do not contain changed path') "collector PR path validation did not block: $($fabricatedPrScope.status); $($fabricatedPrScope.error)"
    Assert-Crc ($null -ne $fabricatedPrScope.snapshot_material -and $fabricatedPrScope.snapshot_id -eq (Get-CrcHashJson -Value $fabricatedPrScope.snapshot_material)) 'blocked PR collection did not emit a schema-valid hashed snapshot material'
    $originalPrSnapshot = Get-Content -LiteralPath $prSnapshotPath -Raw
    $aliasOutputText = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode pull-request -Repo $repo -PullRequestSnapshot $prSnapshotPath -Output $prSnapshotPath 2>&1
    $aliasOutputExit = $LASTEXITCODE
    Assert-Crc ($aliasOutputExit -eq 2 -and ($aliasOutputText -join [Environment]::NewLine) -match 'output must differ') 'collector allowed output to overwrite its pull-request snapshot'
    Assert-Crc ((Get-Content -LiteralPath $prSnapshotPath -Raw) -eq $originalPrSnapshot) 'collector overwrote the aliased pull-request snapshot'
    $missingPrRevision = Get-Content -LiteralPath $prSnapshotPath -Raw | ConvertFrom-Json
    $missingPrRevision.base_sha = 'f'*40
    $missingPrRevisionPath = Join-Path $root 'missing-pr-commit.json'; $missingPrOutput = Join-Path $root 'missing-pr-commit-scope.json'
    Write-CrcJson -Path $missingPrRevisionPath -Value $missingPrRevision
    $missingPrDiagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'collect-scope.ps1') -Mode pull-request -Repo $repo -PullRequestSnapshot $missingPrRevisionPath -Output $missingPrOutput 2>&1
    $missingPrExit = $LASTEXITCODE
    $missingPrResult = Get-Content -LiteralPath $missingPrOutput -Raw | ConvertFrom-Json
    $missingPrBlocked = $missingPrExit -eq 2 -and $missingPrResult.status -eq 'BLOCKED' -and $missingPrResult.error -match 'Git command failed.*rev-parse|Revision did not resolve'
    Assert-Crc $missingPrBlocked "collector did not fail closed for a missing pull-request commit: exit=$missingPrExit; status=$($missingPrResult.status); error=$($missingPrResult.error)"
    $prReviewersPath = Join-Path $root 'pull-request-reviewers.jsonl'; $prReviewerRecords = @($reviewerRecords | ForEach-Object { $_ | ConvertFrom-Json })
    foreach ($record in $prReviewerRecords) { $record.snapshot_id=$prScope.snapshot_id; foreach ($finding in $record.findings) { $finding.snapshot_id=$prScope.snapshot_id } }
    Write-CrcReviewerJsonl -Path $prReviewersPath -Records $prReviewerRecords
    $prAdjudicationPath = Join-Path $root 'pull-request-adjudication.json'; $prAdjudication=Get-Content -LiteralPath $adjudication -Raw | ConvertFrom-Json
    $prAdjudication.snapshot_id=$prScope.snapshot_id; foreach($entry in $prAdjudication.dispositions){$entry.snapshot_id=$prScope.snapshot_id}
    Write-CrcJson -Path $prAdjudicationPath -Value $prAdjudication
    $githubReviewPath = Join-Path $root 'github-review.json'; $githubReviewOutput = Join-Path $root 'github-review-output.json'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'validate-review.ps1') -Scope $prScopePath -Reviewers $prReviewersPath -Adjudication $prAdjudicationPath -Output $githubReviewPath
    $githubReview = Get-Content -LiteralPath $githubReviewPath -Raw | ConvertFrom-Json
    Assert-Crc ($githubReview.status -eq 'PASS' -and $githubReview.scope_manifest.mode -eq 'pull-request') 'offline PR review fixture did not produce a complete pull-request result'
    $tamperedConsolidation = Get-Content -LiteralPath $githubReviewPath -Raw | ConvertFrom-Json
    $tamperedConsolidation.status = 'PASS'; $tamperedConsolidation.findings = @(); $tamperedConsolidation.dispositions = @()
    $tamperedConsolidationPath = Join-Path $root 'tampered-consolidation.json'; $tamperedConsolidationOutput = Join-Path $root 'tampered-consolidation-output.json'; $tamperedConsolidationLedger = Join-Path $root 'tampered-consolidation-ledger.json'
    Write-CrcJson -Path $tamperedConsolidationPath -Value $tamperedConsolidation
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $tamperedConsolidationPath -Provider mock -Ledger $tamperedConsolidationLedger -Output $tamperedConsolidationOutput | Out-Null
    $tamperedConsolidationResult = Get-Content -LiteralPath $tamperedConsolidationOutput -Raw | ConvertFrom-Json
    Assert-Crc ($LASTEXITCODE -eq 2 -and $tamperedConsolidationResult.error -match 'consolidated findings do not match reviewer fingerprint set') 'publisher accepted removed reviewer findings after a forged PASS status'
    Assert-Crc (-not (Test-Path -LiteralPath $tamperedConsolidationLedger)) 'forged consolidated result changed the mock publication ledger'

    $aliasReviewOriginal = Get-Content -LiteralPath $githubReviewPath -Raw
    $aliasReviewOutput = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider mock -Ledger (Join-Path $root 'alias-review-ledger.json') -Output $githubReviewPath 2>&1
    Assert-Crc ($LASTEXITCODE -eq 2 -and ($aliasReviewOutput -join [Environment]::NewLine) -match 'paths must not alias') 'publisher allowed output to overwrite its review input'
    Assert-Crc ((Get-Content -LiteralPath $githubReviewPath -Raw) -eq $aliasReviewOriginal) 'publisher overwrote the aliased review input'
    $aliasLedgerPath = Join-Path $root 'aliased-ledger-output.json'
    $aliasLedgerOutput = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider mock -Ledger $aliasLedgerPath -Output $aliasLedgerPath 2>&1
    Assert-Crc ($LASTEXITCODE -eq 2 -and ($aliasLedgerOutput -join [Environment]::NewLine) -match 'paths must not alias') 'publisher allowed output to overwrite its idempotency ledger'
    Assert-Crc (-not (Test-Path -LiteralPath $aliasLedgerPath)) 'publisher created the aliased ledger/output destination'
    $unboundReview = $githubReview | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $unboundMaterial = [ordered]@{mode='pull-request';status='READY'}
    $unboundSnapshotId = Get-CrcHashJson -Value $unboundMaterial
    $unboundReview.snapshot_id = $unboundSnapshotId; $unboundReview.scope_manifest.snapshot_id = $unboundSnapshotId; $unboundReview.scope_manifest.snapshot_material = $unboundMaterial
    foreach($record in $unboundReview.reviewers){$record.snapshot_id=$unboundSnapshotId;foreach($finding in $record.findings){$finding.snapshot_id=$unboundSnapshotId}}
    foreach($finding in $unboundReview.findings){$finding.snapshot_id=$unboundSnapshotId}
    foreach($entry in $unboundReview.dispositions){$entry.snapshot_id=$unboundSnapshotId}
    $unboundReview.execution.adjudication_snapshot_id = $unboundSnapshotId
    $unboundReviewPath = Join-Path $root 'unbound-github-review.json'; $unboundOutput = Join-Path $root 'unbound-github-output.json'
    Write-CrcJson -Path $unboundReviewPath -Value $unboundReview
    $liveAnchorOutput = Join-Path $root 'live-anchor-publication.json'
    $env:PATH = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { "$fakeBin;$oldPath" } else { "$fakeBin$([System.IO.Path]::PathSeparator)$oldPath" }
    $env:PATHEXT = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { ".CMD;$oldPathExt" } else { $oldPathExt }
    $env:CRC_GH_STUB_LOG = $fakeGhLog; $env:CRC_GH_STUB_POST_LOG = $fakeGhPostLog; $env:CRC_GH_STUB_BODY_LOG = $fakeGhBodyLog; $env:CRC_GH_STUB_BASE = $fakeBase; $env:CRC_GH_STUB_HEAD = $fakeHead
    try {
        $env:CRC_GH_STUB_DIFF_MODE = 'invalid-anchor'
        $unboundOutputText = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $unboundReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $unboundOutput 2>&1
        $unboundExit = $LASTEXITCODE
        $unboundResult = Get-Content -LiteralPath $unboundOutput -Raw | ConvertFrom-Json
        Assert-Crc ($unboundExit -eq 2 -and $unboundResult.error -match 'hashed pull-request scope snapshot') 'GitHub publisher trusted an unhashed top-level pull-request identity'
        Assert-Crc (-not (Test-Path -LiteralPath $fakeGhLog)) 'publisher invoked gh for a review with no hashed pull-request snapshot'
        $env:CRC_GH_STUB_DIFF_MODE = 'mismatched-files'
        $mismatchedDiffOutputPath = Join-Path $root 'mismatched-live-diff.json'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $mismatchedDiffOutputPath | Out-Null
        $mismatchedDiffResult = Get-Content -LiteralPath $mismatchedDiffOutputPath -Raw | ConvertFrom-Json
        Assert-Crc ($LASTEXITCODE -eq 2 -and $mismatchedDiffResult.error -match 'live diff is missing PR file') 'publisher accepted live diff headers that differ from the file list'
        Assert-Crc (-not (Test-Path -LiteralPath $fakeGhPostLog)) 'publisher POSTed after live diff/file-list mismatch'
        Remove-Item -LiteralPath $fakeGhLog -ErrorAction SilentlyContinue
        $env:CRC_GH_STUB_DIFF_MODE = 'stale-revision'
        $staleRevisionOutputPath = Join-Path $root 'stale-live-revision.json'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $staleRevisionOutputPath | Out-Null
        $staleRevisionResult = Get-Content -LiteralPath $staleRevisionOutputPath -Raw | ConvertFrom-Json
        Assert-Crc ($LASTEXITCODE -eq 2 -and $staleRevisionResult.error -match 'base/head differs') 'publisher did not reject a PR revision change during full-diff validation'
        Assert-Crc (-not (Test-Path -LiteralPath $fakeGhPostLog)) 'publisher POSTed after the live PR revision changed'
        Remove-Item -LiteralPath $fakeGhLog -ErrorAction SilentlyContinue
        $liveDiffFailureCases = @(
            [pscustomobject]@{mode='mismatched-files';expected='live diff is missing PR file'},
            [pscustomobject]@{mode='stale-revision';expected='base/head differs'},
            [pscustomobject]@{mode='invalid-anchor';expected='finding line missing from complete live diff'},
            [pscustomobject]@{mode='too-many-files';expected='file-list limit'}
        )
        foreach ($failureCase in $liveDiffFailureCases) {
            Remove-Item -LiteralPath $fakeGhLog,$fakeGhPostLog -ErrorAction SilentlyContinue
            $env:CRC_GH_STUB_DIFF_MODE = $failureCase.mode
            $failedPublicationPath = Join-Path $root ("failed-{0}.json" -f $failureCase.mode)
            $null = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $failedPublicationPath 2>&1
            $failedPublicationExit = $LASTEXITCODE
            $failedPublication = Get-Content -LiteralPath $failedPublicationPath -Raw | ConvertFrom-Json
            Assert-Crc ($failedPublicationExit -eq 2 -and $failedPublication.status -eq 'blocked' -and $failedPublication.error -match $failureCase.expected) "publisher did not block $($failureCase.mode): $($failedPublication.error)"
            Assert-Crc (-not (Test-Path -LiteralPath $fakeGhPostLog)) "publisher POSTed for $($failureCase.mode)"
        }
        $env:CRC_GH_STUB_DIFF_MODE = 'valid-anchor'
        Remove-Item -LiteralPath $fakeGhLog,$fakeGhPostLog -ErrorAction SilentlyContinue
        $verifiedFallbackOutput = Join-Path $root 'verified-fallback-publication.json'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $verifiedFallbackOutput | Out-Null
        $verifiedFallbackResult = Get-Content -LiteralPath $verifiedFallbackOutput -Raw | ConvertFrom-Json
        Assert-Crc ($LASTEXITCODE -eq 0 -and $verifiedFallbackResult.status -eq 'published' -and (Test-Path -LiteralPath $fakeGhPostLog)) 'publisher did not permit a top-level report backed by the verified live diff'
        $postedPayload = Get-Content -LiteralPath $fakeGhBodyLog -Raw | ConvertFrom-Json
        Assert-Crc ($postedPayload.body -match '<!-- code-review-council:v1:[0-9a-f]+ -->') 'fake GitHub POST did not contain its idempotency marker'
        $env:CRC_GH_STUB_COMMENT_BODY = [string]$postedPayload.body
        $alreadyPublishedOutput = Join-Path $root 'rotated-credentials-already-published.json'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'publish-review.ps1') -Review $githubReviewPath -Provider github -Repo 'example.invalid/council-test' -Pr 1 -ExpectedBase $fakeBase -ExpectedHead $fakeHead -Execute -Output $alreadyPublishedOutput | Out-Null
        $alreadyPublishedExit = $LASTEXITCODE
        $alreadyPublishedResult = Get-Content -LiteralPath $alreadyPublishedOutput -Raw | ConvertFrom-Json
        $postCount = if (Test-Path -LiteralPath $fakeGhPostLog) { @(Get-Content -LiteralPath $fakeGhPostLog).Count } else { 0 }
        $alreadyPublishedError = if ($null -ne $alreadyPublishedResult.PSObject.Properties['error']) { [string]$alreadyPublishedResult.error } else { '' }
        Assert-Crc ($alreadyPublishedExit -eq 0 -and $alreadyPublishedResult.status -eq 'already-published' -and $postCount -eq 1) "publisher duplicated a marker found under a different authenticated account: exit=$alreadyPublishedExit; status=$($alreadyPublishedResult.status); postCount=$postCount; error=$alreadyPublishedError"
    }
    finally {
        $env:PATH = $oldPath; $env:PATHEXT = $oldPathExt
        if ($null -eq $oldGhLog) { Remove-Item Env:CRC_GH_STUB_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_LOG = $oldGhLog }
        if ($null -eq $oldGhPostLog) { Remove-Item Env:CRC_GH_STUB_POST_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_POST_LOG = $oldGhPostLog }
        if ($null -eq $oldGhBodyLog) { Remove-Item Env:CRC_GH_STUB_BODY_LOG -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_BODY_LOG = $oldGhBodyLog }
        if ($null -eq $oldGhBase) { Remove-Item Env:CRC_GH_STUB_BASE -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_BASE = $oldGhBase }
        if ($null -eq $oldGhHead) { Remove-Item Env:CRC_GH_STUB_HEAD -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_HEAD = $oldGhHead }
        if ($null -eq $oldGhCommentBody) { Remove-Item Env:CRC_GH_STUB_COMMENT_BODY -ErrorAction SilentlyContinue } else { $env:CRC_GH_STUB_COMMENT_BODY = $oldGhCommentBody }
    }
    Assert-Crc (Test-Path -LiteralPath $fakeGhLog) 'missing-patch publisher path did not use the fake gh command'

    $noChangeMaterial = [ordered]@{ mode='branch'; status='NO_CHANGES'; base=('a'*40); head=('a'*40); merge_base=('a'*40); changed_files=@(); patch=[ordered]@{sha256=('0'*64);bytes=0;encoding='base64';content_base64=''} }
    $noChange = [ordered]@{schema_version=$script:CrcSchemaVersion;mode='branch';status='NO_CHANGES';snapshot_id=(Get-CrcHashJson $noChangeMaterial);repository=[ordered]@{root=$repo};captured_at_utc=(Get-CrcUtcNow);changed_files=@();patch=$noChangeMaterial.patch;snapshot_material=$noChangeMaterial}
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
    $evaluationObject = Get-Content -LiteralPath $evaluation -Raw | ConvertFrom-Json
    $nestedEvaluationOutput = Join-Path $root 'nested-evaluation/results/metrics.json'
    $nestedEvaluationMarkdown = Join-Path $root 'nested-evaluation/results/report.md'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-evaluation.ps1') -Fixtures $fixturePath -Output $nestedEvaluationOutput -MarkdownOutput $nestedEvaluationMarkdown | Out-Null
    Assert-Crc ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $nestedEvaluationMarkdown)) 'evaluation did not create a new Markdown output directory'
    Assert-Crc ($evaluationObject.case_count -eq 24 -and $evaluationObject.set_counts.development -eq 13 -and $evaluationObject.set_counts.'held-out' -eq 11) 'evaluation case or split counts changed'
    $expectedMetrics = @{
        'single-reviewer' = [ordered]@{cases=24;precision=0.4286;recall=0.25;p0_p1_recall=0.25;false_blocker_rate=0.2308;duplicate_publication_rate=0.5;invalid_anchor_rate=0.2857;scope_completeness=0.875;policy_compliance=0.9583;skill_trigger_accuracy=0.9583;latency_ms_total=21600;tokens_total=28800;unique_validated_contribution=0}
        'all-lenses' = [ordered]@{cases=24;precision=0.6923;recall=0.75;p0_p1_recall=0.75;false_blocker_rate=0.2308;duplicate_publication_rate=0.5;invalid_anchor_rate=0.0714;scope_completeness=0.875;policy_compliance=0.9583;skill_trigger_accuracy=0.9583;latency_ms_total=57600;tokens_total=86400;unique_validated_contribution=0}
        'council' = [ordered]@{cases=24;precision=1.0;recall=1.0;p0_p1_recall=1.0;false_blocker_rate=0;duplicate_publication_rate=0;invalid_anchor_rate=0;scope_completeness=0.875;policy_compliance=1.0;skill_trigger_accuracy=1.0;latency_ms_total=122400;tokens_total=220800;unique_validated_contribution=3}
    }
    $expectedSetMetrics = @{
        development = @{
            'single-reviewer' = [ordered]@{cases=13;precision=0.5;recall=0.2;p0_p1_recall=0;false_blocker_rate=0.3333;duplicate_publication_rate=0;invalid_anchor_rate=0.25;scope_completeness=0.9231;policy_compliance=1;skill_trigger_accuracy=1;latency_ms_total=11700;tokens_total=15600;unique_validated_contribution=0}
            'all-lenses' = [ordered]@{cases=13;precision=0.7778;recall=0.7;p0_p1_recall=0.6667;false_blocker_rate=0.3333;duplicate_publication_rate=0;invalid_anchor_rate=0;scope_completeness=0.9231;policy_compliance=1;skill_trigger_accuracy=1;latency_ms_total=31200;tokens_total=46800;unique_validated_contribution=0}
            'council' = [ordered]@{cases=13;precision=1;recall=1;p0_p1_recall=1;false_blocker_rate=0;duplicate_publication_rate=0;invalid_anchor_rate=0;scope_completeness=0.9231;policy_compliance=1;skill_trigger_accuracy=1;latency_ms_total=66300;tokens_total=119600;unique_validated_contribution=3}
        }
        'held-out' = @{
            'single-reviewer' = [ordered]@{cases=11;precision=0.3333;recall=0.5;p0_p1_recall=1;false_blocker_rate=0.2;duplicate_publication_rate=0.5;invalid_anchor_rate=0.3333;scope_completeness=0.8182;policy_compliance=0.9091;skill_trigger_accuracy=0.9091;latency_ms_total=9900;tokens_total=13200;unique_validated_contribution=0}
            'all-lenses' = [ordered]@{cases=11;precision=0.5;recall=1;p0_p1_recall=1;false_blocker_rate=0.2;duplicate_publication_rate=0.5;invalid_anchor_rate=0.25;scope_completeness=0.8182;policy_compliance=0.9091;skill_trigger_accuracy=0.9091;latency_ms_total=26400;tokens_total=39600;unique_validated_contribution=0}
            'council' = [ordered]@{cases=11;precision=1;recall=1;p0_p1_recall=1;false_blocker_rate=0;duplicate_publication_rate=0;invalid_anchor_rate=0;scope_completeness=0.8182;policy_compliance=1;skill_trigger_accuracy=1;latency_ms_total=56100;tokens_total=101200;unique_validated_contribution=0}
        }
    }
    foreach ($approachName in @('single-reviewer','all-lenses','council')) {
        $actual = $evaluationObject.overall.PSObject.Properties[$approachName].Value
        foreach ($metricName in $expectedMetrics[$approachName].Keys) {
            Assert-Crc ([double]$actual.$metricName -eq [double]$expectedMetrics[$approachName][$metricName]) "evaluation metric changed: $approachName.$metricName"
        }
        $expectedMetricNames = @($expectedMetrics[$approachName].Keys | Sort-Object)
        foreach ($setName in @('development','held-out')) {
            $actualSet = $evaluationObject.by_set.PSObject.Properties[$setName].Value.PSObject.Properties[$approachName].Value
            $expectedSet = $expectedSetMetrics[$setName][$approachName]
            Assert-Crc ((@($actualSet.PSObject.Properties.Name | Sort-Object) -join ',') -eq ($expectedMetricNames -join ',')) "$setName metrics are incomplete: $approachName"
            foreach ($metricName in $expectedSet.Keys) { Assert-Crc ([double]$actualSet.$metricName -eq [double]$expectedSet[$metricName]) "evaluation metric changed: $setName.$approachName.$metricName" }
        }
    }
    $unknownSetFixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
    $unknownSetFixture.cases[0].set = 'heldout'
    $unknownSetPath = Join-Path $root 'unknown-set-fixture.json'; $unknownSetOutput = Join-Path $root 'unknown-set-evaluation.json'
    Write-CrcJson -Path $unknownSetPath -Value $unknownSetFixture
    $unknownSetDiagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-evaluation.ps1') -Fixtures $unknownSetPath -Output $unknownSetOutput 2>&1; $unknownSetExit = $LASTEXITCODE
    Assert-Crc ($unknownSetExit -eq 2 -and (($unknownSetDiagnostics -join [Environment]::NewLine) -match 'unknown evaluation set')) 'evaluation accepted an unknown fixture set'
    $badBooleanFixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
    $targetCase = $badBooleanFixture.cases | Where-Object { @($_.approaches.council.findings).Count -gt 0 } | Select-Object -First 1
    $targetCase.approaches.council.findings[0].valid_anchor = 'false'
    $badBooleanPath = Join-Path $root 'bad-boolean-fixture.json'; $badBooleanOutput = Join-Path $root 'bad-boolean-evaluation.json'
    Write-CrcJson -Path $badBooleanPath -Value $badBooleanFixture
    $badBooleanDiagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-evaluation.ps1') -Fixtures $badBooleanPath -Output $badBooleanOutput 2>&1; $badBooleanExit = $LASTEXITCODE
    Assert-Crc ($badBooleanExit -eq 2 -and (($badBooleanDiagnostics -join [Environment]::NewLine) -match 'must be a boolean')) 'evaluation accepted a string-valued boolean flag'
    $duplicateTruthFixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
    $duplicateTruthFixture.cases[0].truth = @('repeated-truth','repeated-truth')
    $duplicateTruthFixture.cases[0].high_severity_truth = @()
    $duplicateTruthPath = Join-Path $root 'duplicate-truth-fixture.json'; $duplicateTruthOutput = Join-Path $root 'duplicate-truth-evaluation.json'
    Write-CrcJson -Path $duplicateTruthPath -Value $duplicateTruthFixture
    $duplicateTruthDiagnostics = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-evaluation.ps1') -Fixtures $duplicateTruthPath -Output $duplicateTruthOutput 2>&1; $duplicateTruthExit = $LASTEXITCODE
    Assert-Crc ($duplicateTruthExit -eq 2 -and (($duplicateTruthDiagnostics -join [Environment]::NewLine) -match 'truth contains duplicate fingerprints')) 'evaluation counted duplicate truth fingerprints'
    Write-Output 'Code Review Council PowerShell tests passed.'
    exit 0
}
finally {
    if ((Get-Location).Path -eq $repo) { Pop-Location }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
