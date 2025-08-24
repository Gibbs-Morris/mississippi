#!/usr/bin/env pwsh

<#
Regenerates Cursor .mdc rule files from the canonical instruction Markdown files.
- Deletes existing .cursor/rules/*.mdc (configurable)
- For each .github/instructions/*.instructions.md, creates a corresponding .cursor/rules/<name>.mdc
- Maps YAML front matter `applyTo` to MDC front-matter (globs/alwaysApply)
- Adds Source/Last synced metadata and a sync comment including the short git SHA
- Copies the human-readable content into MDC body (MDC supports Markdown in Cursor)
#>

[CmdletBinding()]
param(
    [string]$InstructionsDir,
    [string]$MdcDir,
    [string]$LastSynced,
    [switch]$DryRun,
    [switch]$PreserveUnknown
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve repo-root relative to this script (scripts/..)
$defaultRepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$repoRoot = $defaultRepoRoot

# Resolve directories
if (-not $InstructionsDir) { $InstructionsDir = (Join-Path $repoRoot '.github/instructions') }
if (-not $MdcDir) { $MdcDir = (Join-Path $repoRoot '.cursor/rules') }

if (-not (Test-Path -LiteralPath $InstructionsDir)) { throw "Instructions directory not found: $InstructionsDir" }
if (-not (Test-Path -LiteralPath $MdcDir)) { if ($DryRun) { Write-Verbose "[DryRun] Would create $MdcDir" } else { New-Item -ItemType Directory -Path $MdcDir | Out-Null } }

# Compute LastSynced
if (-not $LastSynced) { $LastSynced = (Get-Date).ToString('yyyy-MM-dd') }

# Try to get short SHA
$shortSha = '{short-sha}'
try {
    $shaOut = (& git rev-parse --short HEAD) 2>$null
    if ($LASTEXITCODE -eq 0 -and $shaOut) { $shortSha = ($shaOut | Select-Object -First 1).Trim() }
} catch { }

Write-Host "Deleting existing MDC files..." -ForegroundColor Cyan

# Determine target MDC names that we will regenerate
function Get-MdcFileName {
    param([string]$InstructionFileName)
    return ($InstructionFileName -replace '\.instructions\.md$', '.mdc')
}

$instructionFiles = Get-ChildItem -LiteralPath $InstructionsDir -Filter *.instructions.md -File | Sort-Object Name
$targetMdcNames = @($instructionFiles | ForEach-Object { Get-MdcFileName -InstructionFileName $_.Name })

$existingMdc = Get-ChildItem -LiteralPath $MdcDir -Filter *.mdc -File -ErrorAction SilentlyContinue
$toDelete = $existingMdc
if ($PreserveUnknown) {
    $toDelete = $existingMdc | Where-Object { $targetMdcNames -contains $_.Name }
}

foreach ($f in $toDelete) {
    if ($DryRun) { Write-Verbose ("[DryRun] Would delete: {0}" -f $f.FullName) }
    else { Remove-Item -LiteralPath $f.FullName -Force -ErrorAction SilentlyContinue }
}

# Helper: parse YAML front matter block at file start (--- ... ---)
function Get-YamlFrontMatter {
    param([string[]]$Lines)
    if ($Lines.Length -lt 3) { return @{} }
    if ($Lines[0].Trim() -ne '---') { return @{} }

    $yamlLines = New-Object System.Collections.Generic.List[string]
    for ($i = 1; $i -lt $Lines.Length; $i++) {
        $line = $Lines[$i]
        if ($line.Trim() -eq '---') { break }
        $yamlLines.Add($line)
    }

    # Very light YAML parsing: key: value pairs only
    $map = @{}
    foreach ($l in $yamlLines) {
        if ($l -match '^\s*([^:#]+)\s*:\s*(.+)\s*$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            $map[$k] = $v
        }
    }
    return $map
}

# Helper: strip front matter and return index where content starts
function Get-ContentStartIndex {
    param([string[]]$Lines)
    if ($Lines.Length -ge 3 -and $Lines[0].Trim() -eq '---') {
        for ($i = 1; $i -lt $Lines.Length; $i++) {
            if ($Lines[$i].Trim() -eq '---') { return ($i + 1) }
        }
    }
    return 0
}

# Derive MDC front matter from instruction YAML and title
function New-MdcFrontMatter {
    param(
        [hashtable]$Yaml,
        [string]$Title
    )
    $globs = $null
    $alwaysApply = $false

    if ($Yaml.ContainsKey('applyTo')) {
        $applyTo = $Yaml['applyTo']
        # Normalize quotes and value
        $applyTo = $applyTo.Trim('"', "'")
        if ($applyTo -eq '**') { $alwaysApply = $true }
        elseif ($applyTo -match '\*\.cs') { $globs = '["**/*.cs"]' }
        else {
            # Convert basic patterns to globs array (best-effort)
            $applyToPattern = $applyTo.Replace('"', '')
            $globs = ('["' + $applyToPattern + '"]')
        }
    }

    $desc = if ($Title) { $Title } else { 'Repository rules' }

    $frontLines = @()
    $frontLines += '---'
    $frontLines += ("description: $desc")
    if ($globs) { $frontLines += ("globs: $globs") }
    if ($alwaysApply) { $frontLines += 'alwaysApply: true' } else { $frontLines += 'alwaysApply: false' }
    $frontLines += '---'
    return ($frontLines -join "`n")
}

# Counters
$created = 0
$errors = 0

foreach ($file in $instructionFiles) {
    Write-Host ("Processing {0}" -f $file.Name) -ForegroundColor Green
    try {
        $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 -ErrorAction Stop
        $linesArray = $text -split "`n"

        $yaml = Get-YamlFrontMatter -Lines $linesArray
        $contentStart = Get-ContentStartIndex -Lines $linesArray
        $contentBody = ($linesArray[$contentStart..($linesArray.Length - 1)] -join "`n").TrimEnd()

        # Extract first H1 as title for description if present
        $title = ''
        $h1Match = [regex]::Match($contentBody, '^[ \t]*#\s+(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
        if ($h1Match.Success) { $title = $h1Match.Groups[1].Value.Trim() }

        $frontMatter = New-MdcFrontMatter -Yaml $yaml -Title $title

        $mdcPath = Join-Path $MdcDir (Get-MdcFileName -InstructionFileName $file.Name)

        $sourceLine = "**Source:** .github/instructions/$($file.Name)"
        $syncedLine = "**Last synced:** $LastSynced"
        $syncComment = "// sync: source 6 MD:.github/instructions/$($file.Name) ; synced: $LastSynced ; commit: $shortSha"

        $body = @()
        $body += $frontMatter
        $body += $syncComment
        # Inject metadata lines before the rest of the content
        if ($title) { $body += ("# $title") }
        $body += $sourceLine
        $body += $syncedLine
        $body += ''
        $body += $contentBody

        # Avoid duplicate top-level H1 if contentBody already starts with it
        if ($title -and $contentBody -match '^[ \t]*#\s+') {
            # Remove the inserted title, keep contentBody's original
            $body = @($frontMatter, $syncComment, $sourceLine, $syncedLine, '', $contentBody)
        }

        if ($DryRun) {
            Write-Verbose ("[DryRun] Would write: {0}" -f $mdcPath)
        } else {
            Set-Content -LiteralPath $mdcPath -Value ($body -join "`n") -NoNewline:$false -Encoding UTF8
        }
        $created++
    } catch {
        $errors++
        Write-Error "Failed to process $($file.Name): $_"
    }
}

# Summary and exit code
$expected = $instructionFiles.Count
Write-Host ""; Write-Host ("Summary: created={0}, errors={1}, instructions={2}" -f $created, $errors, $expected) -ForegroundColor Yellow
if ($errors -gt 0) { exit 1 } else { exit 0 }
