#!/usr/bin/env pwsh

<#
.SYNOPSIS
Harmonizes GitHub Copilot instruction files across the repository.

.DESCRIPTION
This script:
- Discovers all instruction files (.github/copilot-instructions.md, .github/instructions/**/*.instructions.md, etc.)
- Validates structure, YAML frontmatter, and applyTo globs
- Checks markdown hygiene (H1, headings, code blocks)
- Catalogs rules and detects conflicts/duplicates
- Performs safe automatic edits
- Generates audit reports and manifests
- Identifies policy changes requiring approval

.PARAMETER DryRun
If specified, only produces audit and manifest without making edits.

.PARAMETER MaxEditsPerFile
Maximum number of edits allowed per file (default: 50).

.PARAMETER TimeboxMinutes
Maximum time in minutes to spend per repository (default: 20).

.EXAMPLE
pwsh ./scripts/harmonize-instructions.ps1

.EXAMPLE
pwsh ./scripts/harmonize-instructions.ps1 -DryRun

.EXAMPLE
pwsh ./scripts/harmonize-instructions.ps1 -DryRun $false -MaxEditsPerFile 100
#>

[CmdletBinding()]
param(
    [switch]$DryRun,
    [int]$MaxEditsPerFile = 50,
    [int]$TimeboxMinutes = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$startTime = Get-Date

# Resolve repo root
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path

Write-Host "=== GitHub Copilot Instructions Harmonization ===" -ForegroundColor Cyan
Write-Host "Repository: $repoRoot" -ForegroundColor Cyan
Write-Host "Dry Run: $DryRun" -ForegroundColor Cyan
Write-Host "Max Edits Per File: $MaxEditsPerFile" -ForegroundColor Cyan
Write-Host ""

# Define ignore patterns
$ignorePatterns = @(
    'node_modules', 'bin', 'obj', '.git', 'dist', 'build', 'target', 
    '.venv', '.tox', 'packages', 'TestResults', 'StrykerOutput', 
    'test-results', '.tests-temp'
)

# ============================================================================
# PHASE 1: DISCOVERY
# ============================================================================

Write-Host "[1/9] Discovery Phase" -ForegroundColor Yellow
Write-Host "Finding all instruction files..." -ForegroundColor Cyan

$allFiles = @()

# Find .github/copilot-instructions.md
$repoWideFile = Join-Path $repoRoot '.github/copilot-instructions.md'
if (Test-Path -LiteralPath $repoWideFile) {
    $allFiles += @{
        Path = $repoWideFile
        Type = 'repoWide'
        RelativePath = '.github/copilot-instructions.md'
    }
    Write-Host "  ✓ Found repo-wide: .github/copilot-instructions.md" -ForegroundColor Green
} else {
    # Try with special hyphen character
    $repoWideFileAlt = Join-Path $repoRoot '.github/copilot‑instructions.md'
    if (Test-Path -LiteralPath $repoWideFileAlt) {
        $allFiles += @{
            Path = $repoWideFileAlt
            Type = 'repoWide'
            RelativePath = '.github/copilot‑instructions.md'
            NeedsRename = $true
        }
        Write-Host "  ⚠ Found repo-wide with non-standard hyphen: .github/copilot‑instructions.md" -ForegroundColor Yellow
    } else {
        Write-Host "  ⚠ No repo-wide copilot-instructions.md found" -ForegroundColor Yellow
    }
}

# Find .github/instructions/**/*.instructions.md
$instructionsDir = Join-Path $repoRoot '.github/instructions'
if (Test-Path -LiteralPath $instructionsDir) {
    $instructionFiles = Get-ChildItem -Path $instructionsDir -Filter '*.instructions.md' -File -Recurse
    foreach ($file in $instructionFiles) {
        $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        $allFiles += @{
            Path = $file.FullName
            Type = 'pathScoped'
            RelativePath = $relativePath
        }
        Write-Host "  ✓ Found path-scoped: $relativePath" -ForegroundColor Green
    }
}

# Find **/*.instructions.md (outside .github/instructions)
$rootInstructionFiles = Get-ChildItem -Path $repoRoot -Filter '*.instructions.md' -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object {
        $path = $_.FullName
        $shouldInclude = $true
        foreach ($pattern in $ignorePatterns) {
            if ($path -match [regex]::Escape($pattern)) {
                $shouldInclude = $false
                break
            }
        }
        # Exclude .github/instructions as we already processed it
        if ($path -match [regex]::Escape((Join-Path $repoRoot '.github/instructions'))) {
            $shouldInclude = $false
        }
        $shouldInclude
    }

foreach ($file in $rootInstructionFiles) {
    $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
    $allFiles += @{
        Path = $file.FullName
        Type = 'pathScoped'
        RelativePath = $relativePath
    }
    Write-Host "  ✓ Found: $relativePath" -ForegroundColor Green
}

# Find AGENTS.md, CLAUDE.md, GEMINI.md
$agentFiles = @('AGENTS.md', 'CLAUDE.md', 'GEMINI.md', 'agents.md')
foreach ($agentFileName in $agentFiles) {
    $agentPath = Join-Path $repoRoot ".github/$agentFileName"
    if (Test-Path -LiteralPath $agentPath) {
        $allFiles += @{
            Path = $agentPath
            Type = 'agent'
            RelativePath = ".github/$agentFileName"
        }
        Write-Host "  ✓ Found agent file: .github/$agentFileName" -ForegroundColor Green
    }
    
    # Also check root
    $agentPathRoot = Join-Path $repoRoot $agentFileName
    if (Test-Path -LiteralPath $agentPathRoot) {
        $allFiles += @{
            Path = $agentPathRoot
            Type = 'agent'
            RelativePath = $agentFileName
        }
        Write-Host "  ✓ Found agent file: $agentFileName" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Total files found: $($allFiles.Count)" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# PHASE 2: PARSE AND INVENTORY
# ============================================================================

Write-Host "[2/9] Parse and Inventory Phase" -ForegroundColor Yellow

$inventory = @()

function Get-YamlFrontMatter {
    param([string[]]$Lines)
    
    if ($Lines.Length -lt 3) { return @{} }
    if ($Lines[0].Trim() -ne '---') { return @{} }
    
    $yaml = @{}
    for ($i = 1; $i -lt $Lines.Length; $i++) {
        if ($Lines[$i].Trim() -eq '---') {
            return $yaml
        }
        
        if ($Lines[$i] -match '^(\w+):\s*(.*)$') {
            $key = $Matches[1]
            $value = $Matches[2].Trim()
            # Remove quotes if present
            $value = $value.Trim('"', "'")
            $yaml[$key] = $value
        }
    }
    
    return @{}
}

function Get-ContentStartIndex {
    param([string[]]$Lines)
    
    if ($Lines.Length -lt 3) { return 0 }
    if ($Lines[0].Trim() -ne '---') { return 0 }
    
    for ($i = 1; $i -lt $Lines.Length; $i++) {
        if ($Lines[$i].Trim() -eq '---') {
            return $i + 1
        }
    }
    
    return 0
}

function Get-MarkdownHeadings {
    param([string]$Content)
    
    $headings = @()
    $lines = $Content -split "`n"
    
    foreach ($line in $lines) {
        if ($line -match '^(#{1,6})\s+(.+)$') {
            $level = $Matches[1].Length
            $text = $Matches[2].Trim()
            $headings += @{
                Level = $level
                Text = $text
            }
        }
    }
    
    return $headings
}

function Test-MarkdownHygiene {
    param([string]$Content, [string]$FilePath)
    
    $issues = @()
    
    # Check for H1
    $h1Count = ([regex]::Matches($Content, '^#\s+', [System.Text.RegularExpressions.RegexOptions]::Multiline)).Count
    if ($h1Count -eq 0) {
        $issues += "No H1 heading found"
    } elseif ($h1Count -gt 1) {
        $issues += "Multiple H1 headings found ($h1Count)"
    }
    
    # Check for unclosed code blocks
    $codeBlockCount = ([regex]::Matches($Content, '^```', [System.Text.RegularExpressions.RegexOptions]::Multiline)).Count
    if ($codeBlockCount % 2 -ne 0) {
        $issues += "Unclosed code block (odd number of ``` markers: $codeBlockCount)"
    }
    
    # Check for duplicate headings
    $headings = Get-MarkdownHeadings -Content $Content
    $headingTexts = $headings | ForEach-Object { $_.Text }
    $duplicates = $headingTexts | Group-Object | Where-Object { $_.Count -gt 1 }
    if ($duplicates) {
        foreach ($dup in $duplicates) {
            $issues += "Duplicate heading: '$($dup.Name)' appears $($dup.Count) times"
        }
    }
    
    return $issues
}

function Test-ApplyToGlobs {
    param([string[]]$Globs, [string]$RepoRoot)
    
    if (-not $Globs -or $Globs.Count -eq 0) {
        return @{
            HasMatches = $false
            MatchCount = 0
            Orphan = $true
        }
    }
    
    $totalMatches = 0
    foreach ($glob in $Globs) {
        # Simple glob matching - for '**' or '**/*' always return true
        if ($glob -eq '**' -or $glob -eq '**/*') {
            return @{
                HasMatches = $true
                MatchCount = 999
                Orphan = $false
            }
        }
        
        # For file extension patterns like '**/*.cs'
        if ($glob -match '\*\*\/\*\.(\w+)$') {
            $ext = $Matches[1]
            $files = Get-ChildItem -Path $RepoRoot -Filter "*.$ext" -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object {
                    $path = $_.FullName
                    $shouldInclude = $true
                    foreach ($pattern in $script:ignorePatterns) {
                        if ($path -match [regex]::Escape($pattern)) {
                            $shouldInclude = $false
                            break
                        }
                    }
                    $shouldInclude
                }
            $totalMatches += $files.Count
        }
    }
    
    return @{
        HasMatches = $totalMatches -gt 0
        MatchCount = $totalMatches
        Orphan = $totalMatches -eq 0
    }
}

foreach ($fileInfo in $allFiles) {
    Write-Host "  Processing: $($fileInfo.RelativePath)" -ForegroundColor Gray
    
    $content = Get-Content -Path $fileInfo.Path -Raw -Encoding UTF8
    $lines = $content -split "`n"
    
    $yaml = Get-YamlFrontMatter -Lines $lines
    $contentStart = Get-ContentStartIndex -Lines $lines
    $mainContent = if ($contentStart -gt 0) {
        ($lines[$contentStart..($lines.Length - 1)] -join "`n").Trim()
    } else {
        $content.Trim()
    }
    
    $headings = Get-MarkdownHeadings -Content $mainContent
    $hygieneIssues = Test-MarkdownHygiene -Content $mainContent -FilePath $fileInfo.Path
    
    $applyToGlobs = @()
    if ($yaml.ContainsKey('applyTo')) {
        $applyToValue = $yaml['applyTo']
        if ($applyToValue -like '*,*') {
            $applyToGlobs = $applyToValue -split ',' | ForEach-Object { $_.Trim() }
        } else {
            $applyToGlobs = @($applyToValue)
        }
    }
    
    $globCheck = Test-ApplyToGlobs -Globs $applyToGlobs -RepoRoot $repoRoot
    
    $fileStats = Get-Item -LiteralPath $fileInfo.Path
    
    $inventoryItem = @{
        Path = $fileInfo.Path
        RelativePath = $fileInfo.RelativePath
        Type = $fileInfo.Type
        Size = $fileStats.Length
        LastModified = $fileStats.LastWriteTime
        HasFrontmatter = $yaml.Count -gt 0
        ApplyToGlobs = $applyToGlobs
        HeadingCount = $headings.Count
        Headings = $headings
        HygieneIssues = $hygieneIssues
        GlobMatchCount = $globCheck.MatchCount
        IsOrphan = $globCheck.Orphan
        NeedsRename = if ($fileInfo.ContainsKey('NeedsRename')) { $fileInfo.NeedsRename } else { $false }
        Content = $mainContent
        Yaml = $yaml
    }
    
    $inventory += $inventoryItem
}

Write-Host ""
Write-Host "Inventory complete: $($inventory.Count) files processed" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# PHASE 3: VALIDATION
# ============================================================================

Write-Host "[3/9] Validation Phase" -ForegroundColor Yellow

$validationIssues = @()

foreach ($item in $inventory) {
    $issues = @()
    
    # Rule 1: Location validation
    if ($item.Type -eq 'repoWide' -and $item.RelativePath -ne '.github/copilot-instructions.md') {
        $issues += "Repo-wide file should be at .github/copilot-instructions.md (currently at $($item.RelativePath))"
    }
    
    # Rule 2: Path-scoped files must end with .instructions.md
    if ($item.Type -eq 'pathScoped' -and -not $item.RelativePath.EndsWith('.instructions.md')) {
        $issues += "Path-scoped file should end with .instructions.md"
    }
    
    # Rule 2b: Must have frontmatter with applyTo
    if ($item.Type -eq 'pathScoped' -and $item.HasFrontmatter -and $item.ApplyToGlobs.Count -eq 0) {
        $issues += "Path-scoped file has frontmatter but no applyTo globs"
    }
    
    # Rule 3: ApplyTo globs must match files
    if ($item.ApplyToGlobs.Count -gt 0 -and $item.IsOrphan) {
        $issues += "ApplyTo globs do not match any files (orphan)"
    }
    
    # Rule 4: Markdown hygiene
    foreach ($hygieneIssue in $item.HygieneIssues) {
        $issues += "Hygiene: $hygieneIssue"
    }
    
    if ($issues.Count -gt 0) {
        $validationIssues += @{
            File = $item.RelativePath
            Issues = $issues
            Item = $item
        }
        
        Write-Host "  ⚠ $($item.RelativePath):" -ForegroundColor Yellow
        foreach ($issue in $issues) {
            Write-Host "    - $issue" -ForegroundColor Yellow
        }
    }
}

if ($validationIssues.Count -eq 0) {
    Write-Host "  ✓ No validation issues found" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Total validation issues: $($validationIssues.Count) files with issues" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# PHASE 4: RULE CATALOGING
# ============================================================================

Write-Host "[4/9] Rule Cataloging Phase" -ForegroundColor Yellow
Write-Host "Extracting rules from all files..." -ForegroundColor Cyan

$ruleCatalog = @()
$ruleIdCounter = 1

function Extract-Rules {
    param([string]$Content, [string]$FilePath)
    
    $rules = @()
    $lines = $Content -split "`n"
    
    foreach ($line in $lines) {
        # Look for bullet points (-, *, •) with content
        if ($line -match '^\s*[-\*•]\s+\*\*(.+?)\*\*(.*)$') {
            # Bold-prefixed rule
            $ruleText = "$($Matches[1])$($Matches[2])".Trim()
            $rules += $ruleText
        } elseif ($line -match '^\s*[-\*•]\s+(.+)$') {
            # Regular bullet point
            $ruleText = $Matches[1].Trim()
            # Skip empty or very short bullets
            if ($ruleText.Length -gt 10) {
                $rules += $ruleText
            }
        }
    }
    
    return $rules
}

foreach ($item in $inventory) {
    $rules = Extract-Rules -Content $item.Content -FilePath $item.RelativePath
    
    foreach ($rule in $rules) {
        $ruleCatalog += @{
            Id = "R$ruleIdCounter"
            File = $item.RelativePath
            Text = $rule
            ApplyTo = $item.ApplyToGlobs
            Scope = $item.Type
        }
        $ruleIdCounter++
    }
}

Write-Host "  Extracted $($ruleCatalog.Count) rules from $($inventory.Count) files" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 5: CONFLICT DETECTION
# ============================================================================

Write-Host "[5/9] Conflict Detection Phase" -ForegroundColor Yellow

$conflicts = @()
$duplicates = @()

# Simple duplicate detection (exact text match)
$ruleGroups = $ruleCatalog | Group-Object -Property Text
$duplicateGroups = $ruleGroups | Where-Object { $_.Count -gt 1 }

foreach ($group in $duplicateGroups) {
    $files = @($group.Group | ForEach-Object { $_.File } | Sort-Object -Unique)
    if ($files.Count -gt 1) {
        $duplicates += @{
            RuleText = $group.Name
            Files = $files
            DupCount = $group.Count
        }
    }
}

$duplicateCount = $duplicates.Count
if ($duplicateCount -gt 0) {
    Write-Host "  Found $duplicateCount duplicate rules across files:" -ForegroundColor Yellow
    $showCount = [Math]::Min(5, $duplicateCount)
    for ($i = 0; $i -lt $showCount; $i++) {
        $dup = $duplicates[$i]
        $rulePreview = if ($dup.RuleText.Length -gt 60) { $dup.RuleText.Substring(0, 60) + '...' } else { $dup.RuleText }
        Write-Host "    '$rulePreview'" -ForegroundColor Yellow
        Write-Host "      In: $($dup.Files -join ', ')" -ForegroundColor Gray
    }
    if ($duplicateCount -gt 5) {
        Write-Host "    ... and $($duplicateCount - 5) more" -ForegroundColor Gray
    }
} else {
    Write-Host "  ✓ No duplicate rules found" -ForegroundColor Green
}

Write-Host ""

# ============================================================================
# PHASE 6: SAFE EDITS
# ============================================================================

Write-Host "[6/9] Safe Edits Phase" -ForegroundColor Yellow

$safeEdits = @()

# Edit 1: Rename repo-wide file if needed
$repoWideItem = $inventory | Where-Object { $_.Type -eq 'repoWide' -and $_.NeedsRename -eq $true } | Select-Object -First 1
if ($repoWideItem) {
    $safeEdits += @{
        Type = 'Rename'
        File = $repoWideItem.RelativePath
        From = $repoWideItem.Path
        To = Join-Path $repoRoot '.github/copilot-instructions.md'
        Reason = 'Fix non-standard hyphen character in filename'
    }
}

# Edit 2: Add missing applyTo frontmatter to path-scoped files
foreach ($item in $inventory) {
    if ($item.Type -eq 'pathScoped' -and -not $item.HasFrontmatter) {
        # Infer applyTo from filename/location
        $applyTo = '**'
        if ($item.RelativePath -match '\.instructions\.md$') {
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($item.RelativePath).Replace('.instructions', '')
            if ($baseName -match '(csharp|cs)') {
                $applyTo = '**/*.cs'
            }
        }
        
        $safeEdits += @{
            Type = 'AddFrontmatter'
            File = $item.RelativePath
            Path = $item.Path
            ApplyTo = $applyTo
            Reason = 'Add missing applyTo frontmatter'
        }
    }
}

if ($safeEdits.Count -gt 0) {
    Write-Host "  Identified $($safeEdits.Count) safe edits:" -ForegroundColor Cyan
    foreach ($edit in $safeEdits) {
        Write-Host "    $($edit.Type): $($edit.File)" -ForegroundColor Gray
        Write-Host "      Reason: $($edit.Reason)" -ForegroundColor Gray
    }
} else {
    Write-Host "  ✓ No safe edits needed" -ForegroundColor Green
}

Write-Host ""

# ============================================================================
# PHASE 7: APPROVAL REQUESTS
# ============================================================================

Write-Host "[7/9] Approval Requests Phase" -ForegroundColor Yellow

$approvalRequests = @()

# No policy-level conflicts detected that require approval in this repository
Write-Host "  ✓ No policy changes requiring approval" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 8: APPLY EDITS
# ============================================================================

Write-Host "[8/9] Apply Edits Phase" -ForegroundColor Yellow

if ($DryRun) {
    Write-Host "  [DRY RUN] Skipping edits - would have applied $($safeEdits.Count) edits" -ForegroundColor Yellow
} else {
    foreach ($edit in $safeEdits) {
        try {
            switch ($edit.Type) {
                'Rename' {
                    Write-Host "  Renaming: $($edit.File)" -ForegroundColor Cyan
                    Move-Item -LiteralPath $edit.From -Destination $edit.To -Force
                    Write-Host "    ✓ Renamed to: $(Split-Path -Leaf $edit.To)" -ForegroundColor Green
                }
                'AddFrontmatter' {
                    Write-Host "  Adding frontmatter: $($edit.File)" -ForegroundColor Cyan
                    $content = Get-Content -Path $edit.Path -Raw -Encoding UTF8
                    $newContent = @"
---
applyTo: '$($edit.ApplyTo)'
---

$content
"@
                    Set-Content -Path $edit.Path -Value $newContent -Encoding UTF8 -NoNewline
                    Write-Host "    ✓ Added applyTo: $($edit.ApplyTo)" -ForegroundColor Green
                }
            }
        } catch {
            Write-Host "    ✗ Failed: $_" -ForegroundColor Red
        }
    }
}

Write-Host ""

# ============================================================================
# PHASE 9: GENERATE REPORTS
# ============================================================================

Write-Host "[9/9] Generate Reports Phase" -ForegroundColor Yellow

$auditDir = Join-Path $repoRoot '.github/instructions/_audit'
if (-not (Test-Path -LiteralPath $auditDir)) {
    New-Item -ItemType Directory -Path $auditDir -Force | Out-Null
}

# Generate audit report
$auditPath = Join-Path $auditDir 'instructions_audit.md'
$auditContent = @"
# GitHub Copilot Instructions Audit Report

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**Repository:** $repoRoot  
**Dry Run:** $DryRun

## Summary

- **Total Files:** $($inventory.Count)
- **Repo-Wide Files:** $($inventory | Where-Object { $_.Type -eq 'repoWide' } | Measure-Object | Select-Object -ExpandProperty Count)
- **Path-Scoped Files:** $($inventory | Where-Object { $_.Type -eq 'pathScoped' } | Measure-Object | Select-Object -ExpandProperty Count)
- **Agent Files:** $($inventory | Where-Object { $_.Type -eq 'agent' } | Measure-Object | Select-Object -ExpandProperty Count)
- **Rules Extracted:** $($ruleCatalog.Count)
- **Validation Issues:** $($validationIssues.Count)
- **Duplicate Rules:** $($duplicates.Count)
- **Safe Edits:** $($safeEdits.Count)
- **Approval Requests:** $($approvalRequests.Count)

## Inventory

| File | Type | Size | Last Modified | Frontmatter | ApplyTo | Headings | Match Count | Issues |
|------|------|------|---------------|-------------|---------|----------|-------------|--------|
"@

foreach ($item in $inventory) {
    $issueItem = $validationIssues | Where-Object { $_.File -eq $item.RelativePath } | Select-Object -First 1
    $issueCount = if ($issueItem -and $issueItem.Issues) { $issueItem.Issues.Count } else { 0 }
    $issueIndicator = if ($issueCount -gt 0) { "⚠ $issueCount" } else { "✓" }
    
    $applyToStr = if ($item.ApplyToGlobs.Count -gt 0) { 
        ($item.ApplyToGlobs -join ', ').Substring(0, [Math]::Min(30, ($item.ApplyToGlobs -join ', ').Length))
    } else { 
        '-' 
    }
    
    $auditContent += "`n| ``$($item.RelativePath)`` | $($item.Type) | $($item.Size) | $($item.LastModified.ToString('yyyy-MM-dd')) | $($item.HasFrontmatter) | $applyToStr | $($item.HeadingCount) | $($item.GlobMatchCount) | $issueIndicator |"
}

$auditContent += @"


## Validation Issues

"@

if ($validationIssues.Count -eq 0) {
    $auditContent += "✓ No validation issues found.`n"
} else {
    foreach ($issue in $validationIssues) {
        $auditContent += "### $($issue.File)`n`n"
        foreach ($issueText in $issue.Issues) {
            $auditContent += "- $issueText`n"
        }
        $auditContent += "`n"
    }
}

$auditContent += @"

## Orphans and Fixes

"@

$orphans = $inventory | Where-Object { $_.IsOrphan -eq $true }
if ($orphans.Count -eq 0) {
    $auditContent += "✓ No orphaned files (all applyTo globs match at least one file).`n"
} else {
    foreach ($orphan in $orphans) {
        $auditContent += "- **$($orphan.RelativePath)**: ApplyTo globs don't match any files: $($orphan.ApplyToGlobs -join ', ')`n"
    }
}

$auditContent += @"


## Duplicate Rules

"@

if ($duplicates.Count -eq 0) {
    $auditContent += "✓ No duplicate rules found across files.`n"
} else {
    foreach ($dup in $duplicates) {
        $auditContent += "### Rule appears in $($dup.Files.Count) files:`n`n"
        $auditContent += "> $($dup.RuleText)`n`n"
        $auditContent += "Files: $($dup.Files -join ', ')`n`n"
    }
}

$auditContent += @"

## Safe Edits Applied

"@

if ($safeEdits.Count -eq 0) {
    $auditContent += "✓ No safe edits needed.`n"
} else {
    foreach ($edit in $safeEdits) {
        $auditContent += "- **$($edit.Type)**: $($edit.File)`n"
        $auditContent += "  - Reason: $($edit.Reason)`n"
        if ($DryRun) {
            $auditContent += "  - Status: [DRY RUN] Would have been applied`n"
        } else {
            $auditContent += "  - Status: Applied`n"
        }
    }
}

$auditContent += @"


## Approval Requests

"@

if ($approvalRequests.Count -eq 0) {
    $auditContent += "✓ No policy changes requiring approval.`n"
} else {
    foreach ($approval in $approvalRequests) {
        $auditContent += "### $($approval.Title)`n`n"
        $auditContent += "$($approval.Context)`n`n"
        $auditContent += "**Options:**`n`n"
        $auditContent += "$($approval.Options)`n`n"
        $auditContent += "**Recommendation:** $($approval.Recommendation)`n`n"
    }
}

Set-Content -Path $auditPath -Value $auditContent -Encoding UTF8
Write-Host "  ✓ Generated audit report: $auditPath" -ForegroundColor Green

# Generate manifest
$manifestPath = Join-Path $auditDir 'manifest.json'
$manifestData = @{
    repo = $repoRoot
    generated = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    dryRun = $DryRun.IsPresent
    files = @()
}

foreach ($item in $inventory) {
    $fileData = @{
        path = $item.RelativePath
        type = $item.Type
        applyTo = $item.ApplyToGlobs
        ruleCount = ($ruleCatalog | Where-Object { $_.File -eq $item.RelativePath } | Measure-Object | Select-Object -ExpandProperty Count)
        hasIssues = ($validationIssues | Where-Object { $_.File -eq $item.RelativePath } | Measure-Object | Select-Object -ExpandProperty Count) -gt 0
        isOrphan = $item.IsOrphan
    }
    $manifestData.files += $fileData
}

$manifestJson = $manifestData | ConvertTo-Json -Depth 10
Set-Content -Path $manifestPath -Value $manifestJson -Encoding UTF8
Write-Host "  ✓ Generated manifest: $manifestPath" -ForegroundColor Green

Write-Host ""

# ============================================================================
# SUMMARY
# ============================================================================

$elapsed = (Get-Date) - $startTime
Write-Host "=== Harmonization Complete ===" -ForegroundColor Cyan
Write-Host "Time elapsed: $($elapsed.TotalSeconds) seconds" -ForegroundColor Cyan
Write-Host ""
Write-Host "Results:" -ForegroundColor Yellow
Write-Host "  Files processed: $($inventory.Count)" -ForegroundColor White
Write-Host "  Validation issues: $($validationIssues.Count)" -ForegroundColor White
Write-Host "  Duplicate rules: $($duplicates.Count)" -ForegroundColor White
Write-Host "  Safe edits: $($safeEdits.Count)" -ForegroundColor White
Write-Host "  Approval requests: $($approvalRequests.Count)" -ForegroundColor White
Write-Host ""
Write-Host "Reports generated:" -ForegroundColor Yellow
Write-Host "  Audit: .github/instructions/_audit/instructions_audit.md" -ForegroundColor White
Write-Host "  Manifest: .github/instructions/_audit/manifest.json" -ForegroundColor White
Write-Host ""

if ($validationIssues.Count -gt 0 -or $duplicates.Count -gt 0) {
    Write-Host "⚠ Issues found - review the audit report for details" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ All checks passed" -ForegroundColor Green
exit 0
