#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory)][AllowEmptyString()][string]$BaseRevision,
    [Parameter(Mandatory)][AllowEmptyString()][string]$HeadRevision,
    [string[]]$ChangedPath = @(),
    [string[]]$RiskHint = @(),
    [string]$RepositoryRoot = (Get-Location).Path,
    [ValidateSet('Text', 'Json')][string]$OutputFormat = 'Text'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-PlanRelativePath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Path)
    try {
        $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $fullRoot $Path)) }
        $relative = [System.IO.Path]::GetRelativePath($fullRoot, $fullPath).Replace('\', '/')
        if ($relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) { return $null }
        return $relative
    }
    catch { return $null }
}

function Add-PlanCheck {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Selected,
        [Parameter(Mandatory)][object]$Check,
        [Parameter(Mandatory)][string]$Reason,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$MarkdownPaths
    )

    if (@($Selected | Where-Object Id -EQ $Check.id).Count -gt 0) { return }
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

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $catalogPath = Join-Path $root 'eng/src/agent-scripts/validation-command-catalog.json'
    $catalog = Get-Content -LiteralPath $catalogPath -Raw -ErrorAction Stop | ConvertFrom-Json
    $unresolved = [System.Collections.Generic.List[string]]::new()
    $normalizedPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @($ChangedPath)) {
        $relative = ConvertTo-PlanRelativePath -Root $root -Path $path
        if ($null -eq $relative) { $unresolved.Add("Changed path is outside the repository or invalid: '$path'.") }
        else { $normalizedPaths.Add($relative) }
    }

    $markdownPaths = @($normalizedPaths | Where-Object { $_ -match '\.(?:md|mdx)$' })
    $selected = [System.Collections.Generic.List[object]]::new()
    $isPowerShell = @($normalizedPaths | Where-Object { $_ -match '\.(?:ps1|psm1|psd1)$' }).Count -gt 0
    $isMarkdown = $markdownPaths.Count -gt 0
    $isBrowser = @($normalizedPaths | Where-Object { $_ -match '\.(?:razor|css)$' -or $_ -match '^samples/Spring/' }).Count -gt 0
    $isDotnet = @($normalizedPaths | Where-Object { $_ -match '\.(?:cs|csproj|slnx)$' -or $_ -match '(?:Directory\.Build|Directory\.Packages|global\.json)' }).Count -gt 0
    $isUnknown = -not ($isPowerShell -or $isMarkdown -or $isBrowser -or $isDotnet)

    Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'core-final') -Reason 'Required shared final gate.' -MarkdownPaths $markdownPaths
    Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'full-cleanup') -Reason 'Required final cleanup gate.' -MarkdownPaths $markdownPaths
    if ($isPowerShell) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'powershell-tests') -Reason 'PowerShell source or harness path changed.' -MarkdownPaths $markdownPaths }
    if ($isDotnet -or $isUnknown) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'core-iteration') -Reason $(if ($isUnknown) { 'Unknown mapping selects the broad .NET iteration gate conservatively.' } else { ' .NET source or project path changed.' }) -MarkdownPaths $markdownPaths }
    if ($isMarkdown) { Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'markdown-lint') -Reason 'Markdown or MDX content changed.' -MarkdownPaths $markdownPaths }
    if ($isBrowser) {
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-doctor') -Reason 'Browser-facing or Spring path changed.' -MarkdownPaths $markdownPaths
        Add-PlanCheck -Selected $selected -Check ($catalog.checks | Where-Object id -EQ 'spring-smoke') -Reason 'Rendered/browser behavior may be affected.' -MarkdownPaths $markdownPaths
    }

    foreach ($check in @($catalog.checks)) {
        foreach ($scriptPath in @($check.scriptPaths)) {
            if ($scriptPath -and -not (Test-Path -LiteralPath (Join-Path $root $scriptPath))) { $unresolved.Add("Catalog check '$($check.id)' references missing path '$scriptPath'.") }
        }
    }
    if ([string]::IsNullOrWhiteSpace($BaseRevision)) { $unresolved.Add('BaseRevision is required; no default base is assumed.') }
    if ([string]::IsNullOrWhiteSpace($HeadRevision)) { $unresolved.Add('HeadRevision is required; no default head is assumed.') }
    if ($normalizedPaths.Count -eq 0) { $unresolved.Add('No changed paths were supplied; provide explicit changed paths for a deterministic plan.') }

    $result = [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Complete = $unresolved.Count -eq 0
        RepositoryRoot = $root
        BaseRevision = $BaseRevision
        HeadRevision = $HeadRevision
        ChangedPaths = @($normalizedPaths)
        RiskHints = @($RiskHint)
        SelectedChecks = @($selected)
        OmittedChecks = @($catalog.checks | Where-Object { @($selected | Where-Object Id -EQ $_.id).Count -eq 0 } | ForEach-Object { [pscustomobject]@{ Id = $_.id; Reason = 'No applicable changed-path or risk signal.' } })
        Unresolved = @($unresolved | Sort-Object -Unique)
    }
    if ($OutputFormat -eq 'Json') { $result | ConvertTo-Json -Depth 10 -Compress } else {
        Write-Output "VALIDATION_PLAN: $(if ($result.Complete) { 'COMPLETE' } else { 'INCOMPLETE' })"
        Write-Output "BASE: $BaseRevision"
        Write-Output "HEAD: $HeadRevision"
        foreach ($check in @($result.SelectedChecks)) { Write-Output "$($check.Id) [$($check.Mode)] -> $($check.Executable) $($check.Arguments -join ' ') | $($check.Reasons -join '; ')" }
        foreach ($item in @($result.Unresolved)) { Write-Output "UNRESOLVED: $item" }
    }
    if ($result.Complete) { exit 0 }
    exit 1
}
catch {
    Write-Error "Validation plan failed: $($_.Exception.Message)"
    exit 1
}
