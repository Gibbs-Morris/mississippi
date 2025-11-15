#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [string]$TestProject,

    [string]$Configuration = "Release",

    [switch]$SkipMutation,

    [string]$SourceProject,

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-TestProjectPath {
    param(
        [string]$InputValue
    )

    # If a direct path was provided
    if (Test-Path -LiteralPath $InputValue) {
        if ((Get-Item -LiteralPath $InputValue).PSIsContainer) {
            $csproj = Get-ChildItem -LiteralPath $InputValue -Filter *.csproj | Select-Object -First 1
            if (-not $csproj) { throw "No .csproj found under directory '$InputValue'" }
            return $csproj.FullName
        } else {
            if ($InputValue -like "*.csproj") { return (Resolve-Path -LiteralPath $InputValue).Path }
            throw "Provided path is not a .csproj: '$InputValue'"
        }
    }

    # Try conventional path tests/<Name>/<Name>.csproj
    $expected = Join-Path -Path (Join-Path -Path (Get-Location) -ChildPath "tests") -ChildPath (Join-Path $InputValue ("$InputValue.csproj"))
    if (Test-Path -LiteralPath $expected) { return (Resolve-Path -LiteralPath $expected).Path }

    # Fallback: search under tests for matching .csproj by file name
    $matches = Get-ChildItem -Path (Join-Path (Get-Location) "tests") -Recurse -Filter *.csproj |
        Where-Object { $_.BaseName -ieq $InputValue }
    if ($matches.Count -eq 1) { return $matches[0].FullName }
    if ($matches.Count -gt 1) { throw "Multiple test projects matched name '$InputValue': $($matches.FullName -join ', ')" }
    throw "Could not resolve test project from input '$InputValue'"
}

function Resolve-SourceProjectPathFromTest {
    param(
        [string]$TestProjectPath
    )

    [xml]$proj = Get-Content -LiteralPath $TestProjectPath
    $dir = Split-Path -Parent $TestProjectPath
    $projectRefs = @()
    # Use XPath to robustly locate all <ProjectReference> elements regardless of ItemGroup layout
    $nodes = $proj.SelectNodes('//Project/ItemGroup/ProjectReference')
    if ($nodes) {
        foreach ($n in $nodes) {
            if ($n.Attributes -and $n.Attributes['Include']) {
                $projectRefs += $n.Attributes['Include'].Value
            }
        }
    }
    if (-not $projectRefs -or @($projectRefs).Count -eq 0) {
        throw "Test project has no <ProjectReference>; cannot infer source project. Use -SourceProject."
    }

    # Prefer refs that live under /src/
    $srcRefs = $projectRefs | Where-Object { $_ -match "[\\/]src[\\/]" }
    if (@($srcRefs).Count -eq 1) {
        return (Resolve-Path -LiteralPath (Join-Path $dir (@($srcRefs)[0]))).Path
    }
    if (@($srcRefs).Count -gt 1) {
        throw "Test project references multiple source projects under 'src': $(@($srcRefs) -join ', '). Use -SourceProject."
    }

    # Otherwise, if there's exactly one reference at all, use it
    if (@($projectRefs).Count -eq 1) {
        return (Resolve-Path -LiteralPath (Join-Path $dir (@($projectRefs)[0]))).Path
    }

    throw "Ambiguous <ProjectReference> entries: $(@($projectRefs) -join ', '). Use -SourceProject to specify the target."
}

function Parse-TrxSummary {
    param(
        [string]$TrxPath
    )
    try {
        [xml]$xml = Get-Content -LiteralPath $TrxPath
        $summary = $xml.TestRun.ResultSummary
        $counters = $summary.Counters
        return [PSCustomObject]@{
            Outcome   = [string]$summary.outcome
            Total     = [int]$counters.total
            Executed  = [int]$counters.executed
            Passed    = [int]$counters.passed
            Failed    = [int]$counters.failed
            Skipped   = [int]($counters.notExecuted)
        }
    } catch {
        return $null
    }
}

function Parse-CoberturaCoveragePercent {
    param(
        [string]$CoberturaPath
    )
    try {
        [xml]$xml = Get-Content -LiteralPath $CoberturaPath
        $lineRate = [double]$xml.coverage.'line-rate'
        return [Math]::Round($lineRate * 100, 2)
    } catch {
        return $null
    }
}

function Find-MutationScoreFromJson {
    param(
        [string]$JsonPath
    )
    try {
        $json = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json

        # Newer Stryker JSON does not expose a top-level mutationScore. Compute from mutants.
        $killed = 0; $timeout = 0; $survived = 0; $noCoverage = 0; $ignored = 0
        if ($json -and $json.PSObject.Properties.Name -contains 'files') {
            foreach ($fileProp in $json.files.PSObject.Properties) {
                $file = $fileProp.Value
                if ($file -and $file.PSObject.Properties.Name -contains 'mutants') {
                    foreach ($mut in $file.mutants) {
                        switch ($mut.status) {
                            'Killed' { $killed++ }
                            'Timeout' { $timeout++ }
                            'Survived' { $survived++ }
                            'NoCoverage' { $noCoverage++ }
                            'Ignored' { $ignored++ }
                            default { }
                        }
                    }
                }
            }
        }

        $denominator = $killed + $timeout + $survived + $noCoverage
        if ($denominator -gt 0) {
            $score = 100.0 * ($killed + $timeout) / $denominator
            return [Math]::Round($score, 2)
        }
        return $null
    } catch {
        return $null
    }
}

function Find-MutationScoreFromMarkdown {
    param(
        [string]$MarkdownPath
    )
    try {
        $pattern = "mutation score[^0-9]*([0-9]+(?:\.[0-9]+)?)\s*%"
        $line = Select-String -Path $MarkdownPath -Pattern $pattern -AllMatches -CaseSensitive:$false | Select-Object -First 1
        if ($line -and $line.Matches.Count -gt 0) {
            $val = $line.Matches[0].Groups[1].Value
            return [Math]::Round([double]$val, 2)
        }
        return $null
    } catch {
        return $null
    }
}

Write-Host "=== TEST PROJECT QUALITY ===" -ForegroundColor Yellow
Write-Host "Project under test (tests): $TestProject" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration  |  SkipMutation: $SkipMutation  |  NoBuild: $NoBuild" -ForegroundColor Yellow
Write-Host "" 

$testFailed = $false
$mutationFailed = $false

try {
    if (Test-Path ".config/dotnet-tools.json") {
        Write-Host "[1/7] Restoring dotnet tools..." -ForegroundColor Cyan
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { throw "Failed to restore dotnet tools" }
    }

    Write-Host "[2/7] Resolving test project path..." -ForegroundColor Cyan
    $testProjectPath = Resolve-TestProjectPath -InputValue $TestProject
    $testProjectName = [IO.Path]::GetFileNameWithoutExtension($testProjectPath)
    Write-Host "Resolved test project: $testProjectName -> $testProjectPath" -ForegroundColor Green

    $scratchpadRoot = Join-Path (Get-Location) ".scratchpad"
    $resultsRoot = Join-Path $scratchpadRoot "coverage-test-results"
    if (-not (Test-Path -LiteralPath $resultsRoot)) { New-Item -ItemType Directory -Path $resultsRoot | Out-Null }
    $resultsDir = Join-Path -Path $resultsRoot -ChildPath $testProjectName
    if (-not (Test-Path -LiteralPath $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir | Out-Null }

    $mutationRoot = Join-Path $scratchpadRoot "mutation-test-results"
    if (-not (Test-Path -LiteralPath $mutationRoot)) { New-Item -ItemType Directory -Path $mutationRoot | Out-Null }

    Write-Host "[3/7] Running dotnet test with coverage..." -ForegroundColor Cyan
    $noBuildFlag = if ($NoBuild) { "--no-build" } else { "" }
    dotnet test "$testProjectPath" --configuration $Configuration $noBuildFlag --logger "trx;LogFileName=test_results.trx" --results-directory "$resultsDir" --collect "XPlat Code Coverage" --verbosity minimal
    if ($LASTEXITCODE -ne 0) { $testFailed = $true }

    # Parse test TRX and coverage
    Write-Host "[4/7] Parsing test and coverage results..." -ForegroundColor Cyan
    $trx = Get-ChildItem -Path $resultsDir -Recurse -Filter *.trx | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $trxSummary = $null
    if ($trx) { $trxSummary = Parse-TrxSummary -TrxPath $trx.FullName }

    $cobertura = Get-ChildItem -Path $resultsDir -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $coveragePercent = $null
    if ($cobertura) { $coveragePercent = Parse-CoberturaCoveragePercent -CoberturaPath $cobertura.FullName }

    # Prepare Stryker
    if (-not $SkipMutation) {
        Write-Host "[5/7] Ensuring mississippi.sln exists for Stryker..." -ForegroundColor Cyan
        $slnx = Join-Path (Get-Location) "mississippi.slnx"
        $sln = Join-Path (Get-Location) "mississippi.sln"
        if (-not (Test-Path -LiteralPath $sln)) {
            dotnet tool run slngen "$slnx" --solutionfile "$sln" --launch false
            if ($LASTEXITCODE -ne 0) { throw "Failed to generate solution file via SlnGen" }
        }

        Write-Host "[6/7] Resolving source project for mutation..." -ForegroundColor Cyan
        $sourceProjectPath = $SourceProject
        if (-not $sourceProjectPath) { $sourceProjectPath = Resolve-SourceProjectPathFromTest -TestProjectPath $testProjectPath }
        Write-Host "Source project inferred: $sourceProjectPath" -ForegroundColor Green

        Write-Host "[7/7] Running Stryker mutation testing..." -ForegroundColor Cyan
        $strykerStart = Get-Date
        $sourceProjectFileName = [IO.Path]::GetFileName($sourceProjectPath)
    $mutationOutput = Join-Path $mutationRoot (Get-Date -Format 'yyyy-MM-dd.HH-mm-ss')
    if (-not (Test-Path -LiteralPath $mutationOutput)) { New-Item -ItemType Directory -Path $mutationOutput | Out-Null }
    dotnet stryker --solution "$sln" --test-project "$testProjectPath" --project "$sourceProjectFileName" --output "$mutationOutput"
        if ($LASTEXITCODE -ne 0) { $mutationFailed = $true }

        # Find latest mutation report
        $mutationJson = Get-ChildItem -Path $mutationRoot -Recurse -Filter mutation-report.json -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -ge $strykerStart } |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1

        $mutationMd = Get-ChildItem -Path $mutationRoot -Recurse -Filter mutation-report.md -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -ge $strykerStart } |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1

        $mutationScore = $null
        if ($mutationJson) { $mutationScore = Find-MutationScoreFromJson -JsonPath $mutationJson.FullName }
        if (-not $mutationScore -and $mutationMd) { $mutationScore = Find-MutationScoreFromMarkdown -MarkdownPath $mutationMd.FullName }

        # Output concise summary for LLMs
        Write-Host ""; Write-Host "=== QUALITY SUMMARY ($testProjectName) ===" -ForegroundColor Yellow
        if ($trxSummary) {
            $resultFlag = if ($testFailed) { "FAIL" } else { "PASS" }
            Write-Host ("RESULT: {0}" -f $resultFlag)
            Write-Host ("TEST_TOTAL: {0}" -f $trxSummary.Total)
            Write-Host ("TEST_PASSED: {0}" -f $trxSummary.Passed)
            Write-Host ("TEST_FAILED: {0}" -f $trxSummary.Failed)
            Write-Host ("TEST_SKIPPED: {0}" -f $trxSummary.Skipped)
        } else {
            Write-Host "RESULT: UNKNOWN"
        }
    if ($null -ne $coveragePercent) { Write-Host ("COVERAGE: {0}%" -f $coveragePercent) } else { Write-Host "COVERAGE: N/A" }
        if (-not $SkipMutation) {
            if ($null -ne $mutationScore) { Write-Host ("MUTATION_SCORE: {0}%" -f $mutationScore) } else { Write-Host "MUTATION_SCORE: N/A" }
            Write-Host ("MUTATION_RESULT: {0}" -f ($(if ($mutationFailed) { "FAIL" } else { "PASS" })))
        }
    } else {
        # Output concise summary without mutation
        Write-Host ""; Write-Host "=== QUALITY SUMMARY ($testProjectName) ===" -ForegroundColor Yellow
        if ($trxSummary) {
            $resultFlag = if ($testFailed) { "FAIL" } else { "PASS" }
            Write-Host ("RESULT: {0}" -f $resultFlag)
            Write-Host ("TEST_TOTAL: {0}" -f $trxSummary.Total)
            Write-Host ("TEST_PASSED: {0}" -f $trxSummary.Passed)
            Write-Host ("TEST_FAILED: {0}" -f $trxSummary.Failed)
            Write-Host ("TEST_SKIPPED: {0}" -f $trxSummary.Skipped)
        } else {
            Write-Host "RESULT: UNKNOWN"
        }
    if ($null -ne $coveragePercent) { Write-Host ("COVERAGE: {0}%" -f $coveragePercent) } else { Write-Host "COVERAGE: N/A" }
    }

    if ($testFailed -or ($mutationFailed -and -not $SkipMutation)) { exit 1 } else { exit 0 }
}
catch {
    Write-Error "ERROR: $_"
    # Attempt to still print what we have for easier parsing
    try {
        if ($testProjectName) { Write-Host "=== QUALITY SUMMARY ($testProjectName) ===" -ForegroundColor Yellow } else { Write-Host "=== QUALITY SUMMARY ===" -ForegroundColor Yellow }
        Write-Host "RESULT: FAIL"
    } catch {}
    exit 1
}


