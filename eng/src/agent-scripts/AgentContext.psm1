Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Split-ApplyToPatterns {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Value)

    $patterns = [System.Collections.Generic.List[string]]::new()
    $builder = [System.Text.StringBuilder]::new()
    $braceDepth = 0
    for ($index = 0; $index -lt $Value.Length; $index++) {
        $character = $Value[$index]
        if ($character -eq '{') { $braceDepth++ }
        if ($character -eq '}') { $braceDepth-- }
        if ($character -eq ',' -and $braceDepth -eq 0) {
            $pattern = $builder.ToString().Trim()
            if ($pattern) { $patterns.Add($pattern) }
            $null = $builder.Clear()
            continue
        }

        $null = $builder.Append($character)
    }

    $lastPattern = $builder.ToString().Trim()
    if ($lastPattern) { $patterns.Add($lastPattern) }
    return @($patterns)
}

function Expand-ContextPattern {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Pattern)

    $brace = [regex]::Match($Pattern, '\{(?<Values>[^{}]+)\}')
    if (-not $brace.Success) { return @($Pattern) }

    $prefix = $Pattern.Substring(0, $brace.Index)
    $suffix = $Pattern.Substring($brace.Index + $brace.Length)
    $expanded = [System.Collections.Generic.List[string]]::new()
    foreach ($value in ($brace.Groups['Values'].Value -split ',')) {
        foreach ($tail in (Expand-ContextPattern -Pattern ($prefix + $value + $suffix))) {
            $expanded.Add($tail)
        }
    }

    return @($expanded)
}

function Convert-ContextGlobToRegex {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Pattern)

    $builder = [System.Text.StringBuilder]::new()
    for ($index = 0; $index -lt $Pattern.Length; $index++) {
        $character = $Pattern[$index]
        if ($character -eq '*') {
            if ($index + 1 -lt $Pattern.Length -and $Pattern[$index + 1] -eq '*') {
                if ($index + 2 -lt $Pattern.Length -and $Pattern[$index + 2] -eq '/') {
                    $null = $builder.Append('(?:.*/)?')
                    $index += 2
                }
                else {
                    $null = $builder.Append('.*')
                    $index++
                }
            }
            else {
                $null = $builder.Append('[^/]*')
            }
        }
        elseif ($character -eq '?') {
            $null = $builder.Append('[^/]')
        }
        elseif ($character -eq '[') {
            $closingIndex = $Pattern.IndexOf(']', $index + 1)
            if ($closingIndex -gt $index + 1) {
                $null = $builder.Append($Pattern.Substring($index, $closingIndex - $index + 1))
                $index = $closingIndex
            }
            else {
                $null = $builder.Append('\\[')
            }
        }
        else {
            $null = $builder.Append([regex]::Escape([string]$character))
        }
    }

    return $builder.ToString()
}

function Test-ContextGlob {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Path
    )

    foreach ($expanded in (Expand-ContextPattern -Pattern $Pattern)) {
        $regex = '^' + (Convert-ContextGlobToRegex -Pattern $expanded) + '$'
        if ([regex]::IsMatch($Path, $regex, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Read-ContextFrontMatter {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $lines = $Content -split '\r?\n'
    if ($lines.Count -eq 0 -or $lines[0].TrimStart([char]0xFEFF) -ne '---') {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'Missing opening YAML frontmatter delimiter.' }
    }

    $closingIndex = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index].Trim() -eq '---') {
            $closingIndex = $index
            break
        }
    }
    if ($closingIndex -lt 0) {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'Unclosed YAML frontmatter.' }
    }

    $applyToLines = @($lines[1..($closingIndex - 1)] | Where-Object { $_ -match '^\s*applyTo\s*:' })
    if ($applyToLines.Count -ne 1) {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'Frontmatter must contain exactly one applyTo field.' }
    }

    $value = ($applyToLines[0] -replace '^\s*applyTo\s*:\s*', '').Trim()
    if ($value.Length -ge 2 -and (($value[0] -eq '''' -and $value[$value.Length - 1] -eq '''') -or ($value[0] -eq '"' -and $value[$value.Length - 1] -eq '"'))) {
        $value = $value.Substring(1, $value.Length - 2)
    }
    $patterns = @(Split-ApplyToPatterns -Value $value)
    if ($patterns.Count -eq 0 -or $patterns -contains '') {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains no usable pattern.' }
    }

    return [pscustomobject]@{ Status = 'valid'; Patterns = $patterns; Reason = '' }
}

function ConvertTo-ContextRelativePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Path
    )

    try {
        $root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $root $Path.Trim())) }
        $relative = [System.IO.Path]::GetRelativePath($root, $fullPath)
        $relative = $relative.Replace('\', '/')
        if ($relative.StartsWith('./', [System.StringComparison]::Ordinal)) {
            $relative = $relative.Substring(2)
        }
        if ([string]::IsNullOrWhiteSpace($relative) -or $relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) {
            return $null
        }
        return $relative
    }
    catch {
        return $null
    }
}

function Get-ContextFileMetrics {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    $words = @($Content -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    return [pscustomobject]@{
        ContentHash = "SHA256:$hash"
        ByteCount = (Get-Item -LiteralPath $Path -ErrorAction Stop).Length
        WordCount = $words
    }
}

function Get-ContextRoutes {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Content)

    $routes = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($Content, '\]\((?<Route>[^)]+)\)')) {
        $route = $match.Groups['Route'].Value.Trim()
        if ($route -and $route -notmatch '^(?:https?|mailto):' -and $route -match '(?:\.md|\.mdx|SKILL\.md)(?:$|#)') {
            if (-not $routes.Contains($route)) { $routes.Add($route) }
        }
    }

    return @($routes | Sort-Object)
}

function Get-ContextSourceRevision {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    try {
        $null = Get-Command git -ErrorAction Stop
        $gitRoot = $RepositoryRoot.Replace('\', '/')
        $revisionOutput = @(& git -c "safe.directory=$gitRoot" -C $RepositoryRoot rev-parse HEAD 2>$null)
        $gitExitCode = $LASTEXITCODE
        $revision = (($revisionOutput | Select-Object -First 1) -as [string]).Trim()
        if ($gitExitCode -eq 0 -and $revision -match '^[0-9a-fA-F]{7,40}$') { return $revision }
    }
    catch {
        # An unversioned or git-inaccessible fixture remains explicit as unknown.
    }

    return 'unknown'
}

function Get-ContextCandidates {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $files = [System.Collections.Generic.List[object]]::new()
    $scanErrors = [System.Collections.Generic.List[string]]::new()
    $instructionRoot = Join-Path $RepositoryRoot '.github/instructions'
    if (Test-Path -LiteralPath $instructionRoot -PathType Container) {
        $errors = @()
        $instructionFiles = @(Get-ChildItem -LiteralPath $instructionRoot -Recurse -File -Filter '*.instructions.md' -Force -ErrorAction SilentlyContinue -ErrorVariable errors)
        foreach ($errorRecord in $errors) { $scanErrors.Add("Unable to enumerate instructions: $($errorRecord.Exception.Message)") }
        foreach ($file in $instructionFiles) { $files.Add([pscustomobject]@{ FullName = $file.FullName; Kind = 'instruction' }) }
    }

    $errors = @()
    $entrypointFiles = @(Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File -Filter 'AGENTS.md' -Force -ErrorAction SilentlyContinue -ErrorVariable errors)
    foreach ($errorRecord in $errors) { $scanErrors.Add("Unable to enumerate AGENTS.md files: $($errorRecord.Exception.Message)") }
    foreach ($file in $entrypointFiles | Where-Object { $_.FullName -notmatch '[\\/](?:\.git|bin|obj|node_modules|\.scratchpad)(?:[\\/]|$)' }) {
        $files.Add([pscustomobject]@{ FullName = $file.FullName; Kind = 'AGENTS' })
    }

    $copilotPath = Join-Path $RepositoryRoot '.github/copilot-instructions.md'
    if (Test-Path -LiteralPath $copilotPath -PathType Leaf) {
        $files.Add([pscustomobject]@{ FullName = $copilotPath; Kind = 'entrypoint' })
    }

    return [pscustomobject]@{ Files = @($files | Sort-Object FullName -Unique); Errors = @($scanErrors) }
}

function Get-AgentContext {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string[]]$ChangedPath = @(),
        [string[]]$IntendedPath = @(),
        [string[]]$ReviewedPath = @(),
        [string[]]$RequiredPath = @(),
        [string[]]$ContentDomain = @(),
        [string[]]$WorkflowRole = @()
    )

    $resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
    $unresolved = [System.Collections.Generic.List[string]]::new()
    $requested = [System.Collections.Generic.List[object]]::new()
    foreach ($group in @(
        [pscustomobject]@{ Name = 'changed'; Values = $ChangedPath },
        [pscustomobject]@{ Name = 'intended'; Values = $IntendedPath },
        [pscustomobject]@{ Name = 'reviewed'; Values = $ReviewedPath },
        [pscustomobject]@{ Name = 'required'; Values = $RequiredPath }
    )) {
        foreach ($path in @($group.Values)) {
            $relative = ConvertTo-ContextRelativePath -RepositoryRoot $resolvedRoot -Path $path
            if ($null -eq $relative) {
                $unresolved.Add("$($group.Name) path is outside the repository or invalid: '$path'.")
            }
            else {
                $requested.Add([pscustomobject]@{ Kind = $group.Name; Path = $relative })
                if ($group.Name -eq 'required' -and -not (Test-Path -LiteralPath (Join-Path $resolvedRoot $relative) -PathType Leaf)) {
                    $unresolved.Add("Required context path is missing or unreadable: '$relative'.")
                }
            }
        }
    }

    $candidateResult = Get-ContextCandidates -RepositoryRoot $resolvedRoot
    foreach ($scanError in @($candidateResult.Errors)) { $unresolved.Add($scanError) }
    $sourceRevision = Get-ContextSourceRevision -RepositoryRoot $resolvedRoot
    $domainProbePaths = @{
        'csharp' = @('__domain__.cs')
        'c#' = @('__domain__.cs')
        'powershell' = @('__domain__.ps1')
        'docs' = @('__domain__.md')
        'documentation' = @('__domain__.md')
        'markdown' = @('__domain__.md')
        'blazor' = @('__domain__.razor')
        'testing' = @('tests/__domain__.cs')
        'serialization' = @('__domain__.cs')
        'orleans' = @('__domain__.cs')
    }

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($candidate in @($candidateResult.Files)) {
        $relative = ConvertTo-ContextRelativePath -RepositoryRoot $resolvedRoot -Path $candidate.FullName
        if ($null -eq $relative) { continue }
        $content = $null
        $readError = $null
        try { $content = Get-Content -LiteralPath $candidate.FullName -Raw -ErrorAction Stop }
        catch { $readError = $_.Exception.Message }

        $frontMatter = if ($candidate.Kind -eq 'instruction' -or $candidate.Kind -eq 'entrypoint') {
            if ($null -eq $content) { [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'File could not be read.' } }
            else { Read-ContextFrontMatter -Content $content }
        }
        else {
            [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'Entrypoint guidance is selected for direct inspection.' }
        }

        $reasons = [System.Collections.Generic.List[string]]::new()
        $isSelected = $false
        if ($candidate.Kind -eq 'AGENTS') {
            $entryDirectory = if ($relative -eq 'AGENTS.md') { '' } else { $relative.Substring(0, $relative.Length - '/AGENTS.md'.Length) }
            $entryPrefix = if ($entryDirectory) { "$entryDirectory/" } else { '' }
            $isSelected = $relative -eq 'AGENTS.md' -or @($requested | Where-Object {
                $_.Path.Equals($entryDirectory, [System.StringComparison]::OrdinalIgnoreCase) -or
                ($entryPrefix -and $_.Path.StartsWith($entryPrefix, [System.StringComparison]::OrdinalIgnoreCase))
            }).Count -gt 0
            if ($isSelected) { $reasons.Add('scoped-entrypoint') }
        }
        elseif ($candidate.Kind -eq 'entrypoint') {
            $isSelected = $true
            $reasons.Add('host-entrypoint')
        }
        else {
            if ($frontMatter.Status -eq 'valid' -and $frontMatter.Patterns -contains '**') {
                $isSelected = $true
                $reasons.Add('global-scope')
            }
            if ($frontMatter.Status -eq 'unknown') {
                $isSelected = $true
                $reasons.Add('unknown-scope-requires-inspection')
            }
            foreach ($request in @($requested | Where-Object { $_.Kind -ne 'required' })) {
                if ($frontMatter.Status -eq 'valid' -and @($frontMatter.Patterns | Where-Object { Test-ContextGlob -Pattern $_ -Path $request.Path }).Count -gt 0) {
                    $isSelected = $true
                    $reasons.Add("path:$($request.Kind):$($request.Path)")
                }
            }
            foreach ($domain in @($ContentDomain)) {
                $probes = @($domainProbePaths[$domain.ToLowerInvariant()])
                foreach ($probe in $probes) {
                    if ($frontMatter.Status -eq 'valid' -and @($frontMatter.Patterns | Where-Object { Test-ContextGlob -Pattern $_ -Path $probe }).Count -gt 0) {
                        $isSelected = $true
                        $reasons.Add("content-domain:$domain")
                    }
                }
            }
            foreach ($role in @($WorkflowRole)) {
                $roleToken = $role.ToLowerInvariant()
                $roleTokens = [System.Collections.Generic.List[string]]::new()
                $roleTokens.Add($roleToken)
                if ($roleToken.EndsWith('er', [System.StringComparison]::Ordinal) -and $roleToken.Length -gt 3) {
                    $roleTokens.Add($roleToken.Substring(0, $roleToken.Length - 2))
                }
                foreach ($roleVariant in $roleTokens) {
                    $roleProbes = @(".github/agents/*$roleVariant*.agent.md", ".github/agents/*$roleVariant*.md")
                    $scopeMatchesRole = $false
                    foreach ($scopePattern in @($frontMatter.Patterns)) {
                        foreach ($roleProbe in $roleProbes) {
                            if (Test-ContextGlob -Pattern $scopePattern -Path $roleProbe) { $scopeMatchesRole = $true }
                        }
                    }
                    if ($relative -match [regex]::Escape($roleVariant) -or $scopeMatchesRole) {
                        $isSelected = $true
                        $reasons.Add("workflow-role:$role")
                    }
                }
            }
        }

        if ($readError) {
            $isSelected = $true
            $reasons.Add('read-failed')
            $unresolved.Add("Unable to read selected context '$relative': $readError")
        }
        if ($isSelected -and $reasons.Count -eq 0) { $reasons.Add('selected') }
        $metrics = if ($null -ne $content) { Get-ContextFileMetrics -Path $candidate.FullName -Content $content } else { $null }
        $entries.Add([pscustomobject][ordered]@{
            Path = $relative
            Kind = $candidate.Kind
            Scope = @($frontMatter.Patterns)
            ScopeStatus = $frontMatter.Status
            ScopeNote = $frontMatter.Reason
            Selected = $isSelected
            Reasons = @($reasons | Sort-Object -Unique)
            ContentHash = if ($null -ne $metrics) { $metrics.ContentHash } else { $null }
            ByteCount = if ($null -ne $metrics) { $metrics.ByteCount } else { $null }
            WordCount = if ($null -ne $metrics) { $metrics.WordCount } else { $null }
            ReferencedRoutes = if ($null -ne $content) { @(Get-ContextRoutes -Content $content) } else { @() }
        })
    }

    $sortedEntries = @($entries | Sort-Object Path)
    $selectedEntries = @($sortedEntries | Where-Object Selected)
    if ($sortedEntries.Count -eq 0) { $unresolved.Add('No guidance candidates were discovered.') }
    [pscustomobject][ordered]@{
        SchemaVersion = '1.0'
        Complete = $unresolved.Count -eq 0 -and $selectedEntries.Count -gt 0
        RepositoryRoot = $resolvedRoot
        SourceRevision = $sourceRevision
        RequestedPaths = @($requested | Sort-Object Kind, Path)
        ContentDomains = @($ContentDomain | Sort-Object)
        WorkflowRoles = @($WorkflowRole | Sort-Object)
        CandidateCount = $sortedEntries.Count
        SelectedFileCount = $selectedEntries.Count
        Entries = $sortedEntries
        Selected = $selectedEntries
        Unresolved = @($unresolved | Sort-Object -Unique)
    }
}

function Format-AgentContextText {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Context)

    $lines = [System.Collections.Generic.List[string]]::new()
    $status = if ($Context.Complete) { 'COMPLETE' } else { 'INCOMPLETE' }
    $lines.Add("AGENT_CONTEXT: $status")
    $lines.Add("SOURCE_REVISION: $($Context.SourceRevision)")
    $lines.Add("CANDIDATE_FILES: $($Context.CandidateCount)")
    $lines.Add("SELECTED_FILES: $($Context.SelectedFileCount)")
    foreach ($entry in @($Context.Selected)) {
        $reasons = ($entry.Reasons -join ',')
        $lines.Add("$($entry.Path) | scope=$($entry.ScopeStatus) | reasons=$reasons | bytes=$($entry.ByteCount) | words=$($entry.WordCount) | hash=$($entry.ContentHash)")
    }
    foreach ($unresolved in @($Context.Unresolved)) { $lines.Add("UNRESOLVED: $unresolved") }
    return @($lines)
}

Export-ModuleMember -Function Get-AgentContext, Format-AgentContextText
