#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][AllowEmptyString()][string]$BaseRevision,
    [Parameter(Mandatory)][AllowEmptyString()][string]$HeadRevision,
    [string[]]$ChangedPath = @(),
    [string[]]$RiskHint = @(),
    [string]$ChangedPathJson,
    [string]$RiskHintJson,
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '../../..'),
    [ValidateSet('Text', 'Json')][string]$OutputFormat = 'Text'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-PlanRelativePath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Path)
    try {
        $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $fullRoot $Path)) }
        $relative = [System.IO.Path]::GetRelativePath($fullRoot, $fullPath)
        if ($IsWindows) { $relative = $relative.Replace('\', '/') }
        if ([System.IO.Path]::IsPathRooted($relative) -or $relative -match '^[A-Za-z]:[\\/]' -or $relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) { return $null }
        return $relative
    }
    catch { return $null }
}

function Add-PlanCheck {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Selected,
        [Parameter(Mandatory)][object]$Check,
        [Parameter(Mandatory)][string]$Reason,
        [Parameter(Mandatory)][AllowNull()][AllowEmptyCollection()][string[]]$MarkdownPaths
    )

    $existing = @($Selected | Where-Object Id -EQ $Check.id)
    if ($existing.Count -gt 0) {
        if (@($existing[0].Reasons) -notcontains $Reason) { $existing[0].Reasons = @($existing[0].Reasons) + $Reason }
        return
    }
    $arguments = @($Check.arguments | ForEach-Object { if ($_ -eq '{{CHANGED_MARKDOWN_PATHS}}') { $MarkdownPaths } else { $_ } })
    $Selected.Add([pscustomobject][ordered]@{
        Id = $Check.id
        Mode = $Check.mode
        Executable = $Check.executable
        Arguments = $arguments
        Purpose = $Check.purpose
        Prerequisites = @($Check.prerequisites)
        ExpectedArtifacts = @($Check.expectedArtifacts)
        Reasons = @($Reason)
    })
}

function Format-PlanArgument {
    param([AllowEmptyString()][string]$Value)

    return "'$(($Value -replace "'", "''"))'"
}

function Test-PlanPathMatch {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Pattern)
    $options = if ($IsWindows) { [System.Text.RegularExpressions.RegexOptions]::IgnoreCase } else { [System.Text.RegularExpressions.RegexOptions]::None }
    return [regex]::IsMatch($Path, $Pattern, $options)
}

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $catalogPath = Join-Path $root 'eng/src/agent-scripts/validation-command-catalog.json'
    $catalog = Get-Content -LiteralPath $catalogPath -Raw -ErrorAction Stop | ConvertFrom-Json
    $unresolved = [System.Collections.Generic.List[string]]::new()
    $normalizedPaths = [System.Collections.Generic.List[string]]::new()
    $inputChangedPaths = @($ChangedPath)
    if (-not [string]::IsNullOrWhiteSpace($ChangedPathJson)) {
        try {
            $decodedChangedPaths = ConvertFrom-Json -InputObject $ChangedPathJson -NoEnumerate
            if ($decodedChangedPaths -isnot [System.Array]) { throw 'JSON value is not an array.' }
            foreach ($path in $decodedChangedPaths) {
                if ($path -isnot [string]) { throw 'JSON array contains a non-string value.' }
            }
            $inputChangedPaths = @($decodedChangedPaths)
        }
        catch {
            $unresolved.Add("ChangedPathJson must be a JSON array of strings: $($_.Exception.Message)")
            $inputChangedPaths = @()
        }
    }
    $inputRiskHints = @($RiskHint)
    if (-not [string]::IsNullOrWhiteSpace($RiskHintJson)) {
        try {
            $decodedRiskHints = ConvertFrom-Json -InputObject $RiskHintJson -NoEnumerate
            if ($decodedRiskHints -isnot [System.Array]) { throw 'JSON value is not an array.' }
            foreach ($hint in $decodedRiskHints) {
                if ($hint -isnot [string]) { throw 'JSON array contains a non-string value.' }
            }
            $inputRiskHints = @($decodedRiskHints)
        }
        catch {
            $unresolved.Add("RiskHintJson must be a JSON array of strings: $($_.Exception.Message)")
            $inputRiskHints = @()
        }
    }
    foreach ($path in $inputChangedPaths) {
        if ($null -eq $path -or [string]::IsNullOrWhiteSpace([string]$path)) {
            $unresolved.Add('Changed path is empty or invalid.')
            continue
        }
        $relative = ConvertTo-PlanRelativePath -Root $root -Path ([string]$path)
        if ($null -eq $relative) { $unresolved.Add("Changed path is outside the repository or invalid: '$path'.") }
        else { $normalizedPaths.Add($relative) }
    }

    $markdownPaths = @($normalizedPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '\.(?:md|mdx)$' })
    $isMarkdownConfig = @($normalizedPaths | Where-Object { (Test-PlanPathMatch -Path $_ -Pattern '(^|/)(?:\.markdownlint-cli2\.jsonc|\.markdownlintignore)$') -or $_ -eq '.github/linters/.markdown-lint.yml' }).Count -gt 0
    $hasMarkdownGlobCharacter = @($markdownPaths | Where-Object { $_ -match '[*?\[\]]' }).Count -gt 0
    $markdownCheckPaths = if ($isMarkdownConfig -or $hasMarkdownGlobCharacter) { @('.') } elseif ($markdownPaths.Count -gt 0) { $markdownPaths } else { @() }
    $selected = [System.Collections.Generic.List[object]]::new()
    $powerShellPaths = @($normalizedPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '\.(?:ps1|psm1|psd1)$' })
    $validatedPowerShellPaths = @(
        'eng/src/agent-scripts/RepositoryAutomation.psm1',
        'eng/src/agent-scripts/get-validation-plan.ps1',
        'eng/tests/agent-scripts/ValidationPlan.Tests.ps1',
        'eng/tests/agent-scripts/PowerShellTestHarness.Tests.ps1',
        'eng/tests/agent-scripts/run-spring-validation-tests.ps1',
        'eng/tests/agent-scripts/run-scratchpad-task-tests.ps1',
        'eng/tests/agent-scripts/run-summarize-coverage-gaps-tests.ps1',
        'eng/tests/agent-scripts/run-task-automation-tests.ps1',
        'eng/tests/agent-scripts/verify-scratchpad-task-scripts.ps1',
        'eng/tests/agent-scripts/RepositoryAutomation.Tests.ps1',
        'eng/tests/agent-scripts/MutationAutomation.Tests.ps1',
        'eng/tests/agent-scripts/MutationSummary.Tests.ps1',
        'eng/tests/agent-scripts/PrFeedbackSkill.Tests.ps1',
        'eng/tests/agent-scripts/run-validation-plan-tests.ps1',
        'eng/tests/orchestrate-powershell-tests.ps1'
    )
    $pathComparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    $unvalidatedPowerShellPaths = @($powerShellPaths | Where-Object {
        $candidatePath = $_
        @($validatedPowerShellPaths | Where-Object { [string]::Equals($_, $candidatePath, $pathComparison) }).Count -eq 0 -and
            $candidatePath -notmatch '^eng/tests/agent-scripts/(?:run-[^/]+|[^/]+\.Tests)\.ps1$'
    })
    $isPowerShell = $powerShellPaths.Count -gt 0 -or @($normalizedPaths | Where-Object { $_ -eq 'eng/src/agent-scripts/validation-command-catalog.json' }).Count -gt 0
    $isMarkdown = $markdownPaths.Count -gt 0
    $isDocusaurus = @($normalizedPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '^docs/Docusaurus/' }).Count -gt 0
    $superLinterConfigPaths = @($normalizedPaths | Where-Object { $_ -eq '.github/linters/.markdown-lint.yml' })
    $browserPaths = @($normalizedPaths | Where-Object { (Test-PlanPathMatch -Path $_ -Pattern '(?:\.razor\.cs|\.(?:razor|css|html?|m?js|jsx|tsx?))$') -or (Test-PlanPathMatch -Path $_ -Pattern '(?:^|/)wwwroot/') -or (Test-PlanPathMatch -Path $_ -Pattern '^(?:src/[^/]+\.Client|samples/[^/]+/[^/]+\.Client)/.+\.cs$') })
    $springBrowserPaths = @($browserPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '^samples/Spring/' })
    $nonSpringBrowserPaths = @($browserPaths | Where-Object { -not (Test-PlanPathMatch -Path $_ -Pattern '^samples/Spring/') -and -not (Test-PlanPathMatch -Path $_ -Pattern '^docs/Docusaurus/') })
    $isSpringPath = @($normalizedPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '^samples/Spring/' }).Count -gt 0
    $nonSpringApplicationPaths = @($normalizedPaths | Where-Object { (Test-PlanPathMatch -Path $_ -Pattern '^(?:src|samples)/') -and -not (Test-PlanPathMatch -Path $_ -Pattern '^samples/Spring/') -and -not (Test-PlanPathMatch -Path $_ -Pattern '^docs/Docusaurus/') })
    $isBrowser = $springBrowserPaths.Count -gt 0 -or $isSpringPath
    $isDotnet = @($normalizedPaths | Where-Object { (Test-PlanPathMatch -Path $_ -Pattern '\.(?:cs|csproj|slnx)$') -or (Test-PlanPathMatch -Path $_ -Pattern '(?:Directory\.Build|Directory\.Packages|global\.json)') }).Count -gt 0
    $unmappedPaths = @($normalizedPaths | Where-Object {
        -not (Test-PlanPathMatch -Path $_ -Pattern '\.(?:ps1|psm1|psd1|md|mdx|razor|css|cs|csproj|slnx)$') -and
        -not (Test-PlanPathMatch -Path $_ -Pattern '(?:Directory\.Build|Directory\.Packages|global\.json)') -and
        -not (Test-PlanPathMatch -Path $_ -Pattern '^samples/Spring/') -and
        -not (Test-PlanPathMatch -Path $_ -Pattern '^docs/Docusaurus/')
    })
    $riskChecks = @{
        browser = 'spring-doctor,spring-smoke'
        infrastructure = 'spring-doctor,core-final'
        generated = 'core-iteration,core-final'
        build = 'core-iteration,core-final'
        documentation = 'markdown-lint'
        powershell = 'powershell-tests'
        'public-contract' = 'core-final'
    }
    $normalizedRiskHints = @($inputRiskHints | ForEach-Object { ([string]$_).Trim().ToLowerInvariant() } | Where-Object { $_ })
    foreach ($riskHint in $normalizedRiskHints) {
        if ($riskChecks.Keys -notcontains $riskHint) {
            $unresolved.Add("Unsupported risk hint '$riskHint'.")
        }
    }
    $isUnknown = $unmappedPaths.Count -gt 0
    $workflowPaths = @($normalizedPaths | Where-Object { Test-PlanPathMatch -Path $_ -Pattern '^\.github/workflows/.+\.ya?ml$' })

    Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'core-final') -Reason 'Required shared final gate.' -MarkdownPaths $markdownPaths
    # core-final invokes go.ps1 without -SkipCleanup, so it already owns the authoritative cleanup pass.
    if ($isPowerShell) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'powershell-tests') -Reason 'PowerShell source or harness path changed.' -MarkdownPaths $markdownPaths }
    if ($isDotnet -or $isUnknown) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'core-iteration') -Reason $(if ($isUnknown) { "Unknown mapping selects the broad .NET iteration gate conservatively for: $($unmappedPaths -join ', ')." } else { ' .NET source or project path changed.' }) -MarkdownPaths $markdownPaths }
    if ($isMarkdown -or $isMarkdownConfig) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'markdown-lint') -Reason 'Markdown, MDX, or Markdown-lint configuration changed.' -MarkdownPaths $markdownCheckPaths }
    if ($superLinterConfigPaths.Count -gt 0) { $unresolved.Add('No local validation gate consumes .github/linters/.markdown-lint.yml as the GitHub Super-Linter configuration.') }
    if ($isDocusaurus) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'docusaurus-final') -Reason 'Docusaurus content or site configuration changed.' -MarkdownPaths $markdownPaths }
    if ($isBrowser) {
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-doctor') -Reason 'Browser-facing or Spring path changed.' -MarkdownPaths $markdownPaths
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-smoke') -Reason 'Rendered/browser behavior may be affected.' -MarkdownPaths $markdownPaths
    }
    if ($isSpringPath) {
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-l2-full') -Reason 'Spring changes require the full L2 contract suite.' -MarkdownPaths $markdownPaths
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-l3-full') -Reason 'Spring changes require the full L3 browser suite.' -MarkdownPaths $markdownPaths
    }
    if ($nonSpringBrowserPaths.Count -gt 0) {
        $unresolved.Add("No application-specific browser validation gate is configured for non-Spring browser paths: $($nonSpringBrowserPaths -join ', ').")
    }
    if ($unvalidatedPowerShellPaths.Count -gt 0) {
        $unresolved.Add("PowerShell paths are outside the maintained parser/test gate: $($unvalidatedPowerShellPaths -join ', ').")
    }
    if ($workflowPaths.Count -gt 0) {
        $unresolved.Add("Workflow paths have no catalog-specific validation gate; verify GitHub Actions independently: $($workflowPaths -join ', ').")
    }
    foreach ($riskHint in $normalizedRiskHints) {
        if ($riskHint -eq 'browser') {
            if ($isSpringPath -and $nonSpringApplicationPaths.Count -gt 0) {
                $unresolved.Add("Browser risk hint is ambiguous across Spring and non-Spring application paths: $($nonSpringApplicationPaths -join ', ').")
            }
            elseif ($isSpringPath) {
                foreach ($checkId in @('spring-doctor', 'spring-smoke')) {
                    $riskCheck = @($catalog.checks | Where-Object { $_.id -eq $checkId } | Select-Object -First 1)
                    Add-PlanCheck -Selected $selected -Check $riskCheck[0] -Reason "Risk hint '$riskHint' selects this check for Spring paths." -MarkdownPaths @($markdownCheckPaths)
                }
            }
            elseif ($isDocusaurus -and $nonSpringApplicationPaths.Count -gt 0) {
                $unresolved.Add("Browser risk hint is ambiguous across Docusaurus and application paths: $($nonSpringApplicationPaths -join ', ').")
            }
            elseif ($isDocusaurus) {
                $riskCheck = @($catalog.checks | Where-Object { $_.id -eq 'docusaurus-final' } | Select-Object -First 1)
                Add-PlanCheck -Selected $selected -Check $riskCheck[0] -Reason "Risk hint '$riskHint' selects the Docusaurus browser gate." -MarkdownPaths @($markdownCheckPaths)
            }
            else {
                $unresolved.Add("Browser risk hint requires an application-specific browser context; no safe gate was selected for the supplied paths.")
            }
            continue
        }
        if ($riskHint -eq 'infrastructure' -and -not $isSpringPath) {
            $unresolved.Add("Infrastructure risk hint requires an application-specific L2 gate; no safe generic L2 mapping exists for the supplied paths.")
            continue
        }
        if ($riskHint -eq 'infrastructure' -and $nonSpringApplicationPaths.Count -gt 0) {
            $unresolved.Add("Infrastructure risk hint is ambiguous across Spring and non-Spring application paths: $($nonSpringApplicationPaths -join ', ').")
            continue
        }
        if ($riskChecks.Keys -contains $riskHint) {
            foreach ($checkId in ($riskChecks[$riskHint] -split ',')) {
                $riskCheck = @($catalog.checks | Where-Object { $_.id -eq [string]$checkId } | Select-Object -First 1)
                if ($riskCheck.Count -eq 0) {
                    $unresolved.Add("Risk hint '$riskHint' references missing catalog check '$checkId'.")
                }
                else {
                    $riskMarkdownPaths = if ($riskHint -eq 'documentation') { if (@($markdownCheckPaths).Count -gt 0) { @($markdownCheckPaths) } else { @('.') } } else { @($markdownPaths) }
                    Add-PlanCheck -Selected $selected -Check $riskCheck[0] -Reason "Risk hint '$riskHint' selects this check." -MarkdownPaths $riskMarkdownPaths
                }
            }
        }
    }

    foreach ($check in @($catalog.checks)) {
        foreach ($scriptPath in @($check.scriptPaths)) {
            if ($scriptPath -and -not (Test-Path -LiteralPath (Join-Path $root $scriptPath))) { $unresolved.Add("Catalog check '$($check.id)' references missing path '$scriptPath'.") }
        }
    }
    if ([string]::IsNullOrWhiteSpace($BaseRevision)) { $unresolved.Add('BaseRevision is required; no default base is assumed.') }
    if ([string]::IsNullOrWhiteSpace($HeadRevision)) { $unresolved.Add('HeadRevision is required; no default head is assumed.') }
    if ($normalizedPaths.Count -eq 0) { $unresolved.Add('No changed paths were supplied; provide explicit changed paths for a deterministic plan.') }

    $selected = @($selected | Sort-Object @{Expression = {
        switch ([string]$_.Id) {
            'core-iteration' { 0 }
            'powershell-tests' { 0 }
            'spring-doctor' { 1 }
            'spring-smoke' { 2 }
            'spring-l2-full' { 3 }
            'spring-l3-full' { 4 }
            'docusaurus-final' { 4 }
            'markdown-lint' { 4 }
            'core-final' { 5 }
            default { 6 }
        }
    }}, Id)

    $result = [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Complete = $unresolved.Count -eq 0
        RepositoryRoot = $root
        BaseRevision = $BaseRevision
        HeadRevision = $HeadRevision
        ChangedPaths = @($normalizedPaths)
        RiskHints = @($normalizedRiskHints)
        SelectedChecks = @($selected)
        OmittedChecks = @($catalog.checks | Where-Object { @($selected | Where-Object Id -EQ $_.id).Count -eq 0 } | ForEach-Object { [pscustomobject]@{ Id = $_.id; Reason = 'No applicable changed-path or risk signal.' } })
        Unresolved = @($unresolved | Sort-Object -Unique)
    }
    if ($OutputFormat -eq 'Json') { $result | ConvertTo-Json -Depth 10 -Compress } else {
        Write-Output "VALIDATION_PLAN: $(if ($result.Complete) { 'COMPLETE' } else { 'INCOMPLETE' })"
        Write-Output "WORKING_DIRECTORY: $($result.RepositoryRoot)"
        Write-Output "BASE: $BaseRevision"
        Write-Output "HEAD: $HeadRevision"
        foreach ($check in @($result.SelectedChecks)) {
            $formattedArguments = @($check.Arguments | ForEach-Object { Format-PlanArgument -Value ([string]$_) }) -join ' '
            Write-Output "$($check.Id) [$($check.Mode)] -> $($check.Executable) $formattedArguments | $($check.Reasons -join '; ')"
        }
        foreach ($item in @($result.Unresolved)) { Write-Output "UNRESOLVED: $item" }
    }
    if ($result.Complete) { exit 0 }
    exit 1
}
catch {
    Write-Error "Validation plan failed: $($_.Exception.Message)"
    exit 1
}
