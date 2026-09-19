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

function Test-ContextPatternExpansionBudget {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Pattern,
        [int]$MaxExpansions = 4096
    )

    $braceCount = @($Pattern.ToCharArray() | Where-Object { $_ -eq '{' }).Count
    if ($braceCount -gt 12) { return $false }
    [long]$estimatedExpansions = 1
    $remaining = $Pattern
    while ($true) {
        $brace = [regex]::Match($remaining, '\{(?<Values>[^{}]+)\}')
        if (-not $brace.Success) { return $true }

        $alternativeCount = @($brace.Groups['Values'].Value -split ',').Count
        if ($alternativeCount -gt 0 -and $estimatedExpansions -gt [math]::Floor($MaxExpansions / $alternativeCount)) {
            return $false
        }
        $estimatedExpansions *= $alternativeCount
        $remaining = $remaining.Substring($brace.Index + $brace.Length)
    }
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
        # Keep the matcher compatible with the repository's PowerShell 7.0
        # baseline. NonBacktracking was added after the minimum runtime.
        $options = [System.Text.RegularExpressions.RegexOptions]::None
        if ($IsWindows) { $options = $options -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase }
        try {
            $boundedRegex = [regex]::new($regex, $options, [TimeSpan]::FromMilliseconds(100))
            if ($boundedRegex.IsMatch($Path)) { return $true }
        }
        catch { return $false }
    }

    return $false
}

function Get-ContextPathComparison {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    if ($IsWindows) { return [System.StringComparison]::OrdinalIgnoreCase }
    if (-not $IsMacOS) { return [System.StringComparison]::Ordinal }

    # macOS may use either a case-sensitive or case-insensitive volume. Probe
    # the mounted repository instead of assuming that the operating system
    # determines the filesystem semantics.
    try {
        $probe = @(
            Get-ChildItem -LiteralPath $RepositoryRoot -Force -ErrorAction Stop |
                Where-Object { $_.Name -match '[A-Za-z]' } |
                Select-Object -First 1
        )
        if ($probe.Count -eq 1) {
            $alternateName = -join ($probe[0].Name.ToCharArray() | ForEach-Object {
                    if ([char]::IsUpper($_)) { [char]::ToLowerInvariant($_) } else { [char]::ToUpperInvariant($_) }
                })
            if ($alternateName -and $alternateName -cne $probe[0].Name -and
                (Test-Path -LiteralPath (Join-Path $RepositoryRoot $alternateName))) {
                return [System.StringComparison]::OrdinalIgnoreCase
            }
        }
    }
    catch {
        # A failed probe is safer as case-sensitive: it cannot select a path
        # that the filesystem did not prove equivalent.
    }

    return [System.StringComparison]::Ordinal
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

    $applyToLines = @($lines[1..($closingIndex - 1)] | Where-Object { $_ -cmatch '^applyTo\s*:' })
    if ($applyToLines.Count -ne 1) {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'Frontmatter must contain exactly one applyTo field.' }
    }

    $value = ($applyToLines[0] -creplace '^\s*applyTo\s*:\s*', '').Trim()
    if ($value -match '^(?:>|\|)[-+]?\s*$') {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo block-scalar values require direct inspection.' }
    }
    if ($value.Length -gt 0 -and ($value[0] -eq '''' -or $value[0] -eq '"')) {
        $quote = $value[0]
        $closingQuoteIndex = $value.IndexOf($quote, 1)
        if ($closingQuoteIndex -lt 0) {
            return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains an unclosed quoted scalar.' }
        }
        $trailing = $value.Substring($closingQuoteIndex + 1).Trim()
        if ($trailing -and -not $trailing.StartsWith('#', [System.StringComparison]::Ordinal)) {
            return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains unsupported trailing syntax.' }
        }
        $value = $value.Substring(1, $closingQuoteIndex - 1)
    }
    else {
        $commentIndex = $value.IndexOf(' #', [System.StringComparison]::Ordinal)
        if ($commentIndex -ge 0) {
            $value = $value.Substring(0, $commentIndex).Trim()
        }
    }
    $braceDepth = 0
    foreach ($character in $value.ToCharArray()) {
        if ($character -eq '{') { $braceDepth++ }
        elseif ($character -eq '}') {
            $braceDepth--
            if ($braceDepth -lt 0) { return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains reversed braces.' } }
        }
    }
    if ($braceDepth -ne 0) {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains unbalanced braces.' }
    }
    $patterns = @(Split-ApplyToPatterns -Value $value)
    if ($patterns.Count -eq 0 -or $patterns -contains '') {
        return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains no usable pattern.' }
    }
    foreach ($pattern in $patterns) {
        if (@($pattern.ToCharArray() | Where-Object { $_ -eq '[' }).Count -ne @($pattern.ToCharArray() | Where-Object { $_ -eq ']' }).Count) {
            return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo contains unbalanced character classes.' }
        }
        if (-not (Test-ContextPatternExpansionBudget -Pattern $pattern)) {
            return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo brace expansion exceeds the safety limit.' }
        }
        try { [regex]::new('^' + (Convert-ContextGlobToRegex -Pattern $pattern) + '$') | Out-Null }
        catch { return [pscustomobject]@{ Status = 'unknown'; Patterns = @(); Reason = 'applyTo produces an invalid regular expression.' } }
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
        $rootFullPath = [System.IO.Path]::GetFullPath($RepositoryRoot)
        $root = if ([string]::Equals($rootFullPath, [System.IO.Path]::GetPathRoot($rootFullPath), [System.StringComparison]::OrdinalIgnoreCase)) { $rootFullPath } else { $rootFullPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) }
        $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) { [System.IO.Path]::GetFullPath($Path) } else { [System.IO.Path]::GetFullPath((Join-Path $root $Path)) }
        $relative = [System.IO.Path]::GetRelativePath($root, $fullPath)
        $relative = $relative.Replace('\', '/')
        if ($relative.StartsWith('./', [System.StringComparison]::Ordinal)) {
            $relative = $relative.Substring(2)
        }
        if ([string]::IsNullOrWhiteSpace($relative) -or [System.IO.Path]::IsPathRooted($relative) -or $relative -eq '..' -or $relative.StartsWith('../', [System.StringComparison]::Ordinal)) {
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
    param(
        [Parameter(Mandatory)][string]$Content,
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$SourcePath
    )

    $routes = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt ($Content.Length - 1); $index++) {
        if ($Content[$index] -ne ']' -or $Content[$index + 1] -ne '(') { continue }

        $routeStart = $index + 2
        $depth = 1
        $routeEnd = -1
        for ($cursor = $routeStart; $cursor -lt $Content.Length; $cursor++) {
            if ($Content[$cursor] -eq '\' -and $cursor + 1 -lt $Content.Length) {
                $cursor++
                continue
            }
            if ($Content[$cursor] -eq '(') { $depth++ }
            elseif ($Content[$cursor] -eq ')') {
                $depth--
                if ($depth -eq 0) {
                    $routeEnd = $cursor
                    break
                }
            }
            elseif ($Content[$cursor] -match '[\r\n]') {
                break
            }
        }
        if ($routeEnd -lt 0) { continue }
        $route = $Content.Substring($routeStart, $routeEnd - $routeStart).Trim()
        $destinationMatch = if ($route.StartsWith('<', [System.StringComparison]::Ordinal)) {
            [regex]::Match($route, '^<(?<Destination>[^>]+)>(?:\s+(?:"[^"]*"|''[^'']*''))?$')
        }
        else {
            [regex]::Match($route, '^(?<Destination>\S+?)(?:\s+(?:"[^"]*"|''[^'']*''))?$')
        }
        if (-not $destinationMatch.Success) { continue }
        $destination = $destinationMatch.Groups['Destination'].Value
        $routePath = ($destination -split '#', 2)[0]
        if ($destination -and $destination -notmatch '^(?:[A-Za-z][A-Za-z0-9+.-]*:|//)' -and $destination -match '(?:\.md|\.mdx|SKILL\.md)(?:$|#)' -and -not [System.IO.Path]::IsPathRooted($routePath)) {
            $candidatePath = Join-Path (Split-Path -Parent $SourcePath) $routePath
            $candidateRelative = ConvertTo-ContextRelativePath -RepositoryRoot $RepositoryRoot -Path $candidatePath
            $safeRoute = -not (Test-Path -LiteralPath $candidatePath) -or (Test-ContextPathWithoutReparsePoints -RepositoryRoot $RepositoryRoot -RelativePath $candidateRelative)
            if ($null -ne $candidateRelative -and $safeRoute) {
                if (-not $routes.Contains($destination)) { $routes.Add($destination) }
            }
        }
        $index = $routeEnd
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
        if ($gitExitCode -eq 0 -and $revision -match '^[0-9a-fA-F]{7,64}$') { return $revision }
    }
    catch {
        # An unversioned or git-inaccessible fixture remains explicit as unknown.
    }

    return 'unknown'
}

function Get-ContextFilesByFilter {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Filter,
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$Errors
    )

    $results = [System.Collections.Generic.List[object]]::new()
    $pending = [System.Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($Root)
    $excludedDirectories = @('.git', 'bin', 'obj', 'node_modules', '.scratchpad')
    $pathComparison = Get-ContextPathComparison -RepositoryRoot $Root
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        try { $children = @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop) }
        catch {
            $null = $Errors.Add("Unable to enumerate '$current': $($_.Exception.Message)")
            continue
        }
        foreach ($child in $children) {
            $isReparsePoint = [bool]($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint)
            if ($child.PSIsContainer) {
                if ($isReparsePoint) {
                    $null = $Errors.Add("Skipped reparse-point guidance directory '$($child.FullName)'.")
                    continue
                }
                $excluded = @($excludedDirectories | Where-Object { [string]::Equals($_, $child.Name, $pathComparison) }).Count -gt 0
                if (-not $excluded) { $pending.Enqueue($child.FullName) }
            }
            else {
                $matchesFilter = if ($Filter -eq 'AGENTS.md') { [string]::Equals($child.Name, $Filter, [System.StringComparison]::Ordinal) } else { $child.Name -clike $Filter }
                if (-not $matchesFilter) { continue }
                if ($isReparsePoint) {
                    $null = $Errors.Add("Skipped reparse-point guidance file '$($child.FullName)'.")
                    continue
                }
                $results.Add($child)
            }
        }
    }

    $uniqueResults = [System.Collections.Generic.List[object]]::new()
    $seenResults = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($result in @($results | Sort-Object FullName)) { if ($seenResults.Add([string]$result.FullName)) { $uniqueResults.Add($result) } }
    return @($uniqueResults)
}

function Get-ContextCandidates {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $files = [System.Collections.Generic.List[object]]::new()
    $scanErrors = [System.Collections.Generic.List[string]]::new()
    $instructionRoot = Join-Path $RepositoryRoot '.github/instructions'
    $instructionRootExists = Test-Path -LiteralPath $instructionRoot -PathType Container
    $instructionRootSafe = $instructionRootExists -and (Test-ContextPathWithoutReparsePoints -RepositoryRoot $RepositoryRoot -RelativePath '.github/instructions')
    if ($instructionRootExists -and -not $instructionRootSafe) {
        $null = $scanErrors.Add("Skipped reparse-point instruction root '$instructionRoot'.")
    }
    elseif ($instructionRootExists) {
        try {
            $instructionItem = Get-Item -LiteralPath $instructionRoot -Force -ErrorAction Stop
            if ([bool]($instructionItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                $null = $scanErrors.Add("Skipped reparse-point instruction root '$instructionRoot'.")
            }
            else {
                $instructionFiles = Get-ContextFilesByFilter -Root $instructionRoot -Filter '*.instructions.md' -Errors $scanErrors
                foreach ($file in $instructionFiles) { $files.Add([pscustomobject]@{ FullName = $file.FullName; Kind = 'instruction' }) }
            }
        }
        catch {
            $null = $scanErrors.Add("Unable to inspect instruction root '$instructionRoot': $($_.Exception.Message)")
        }
    }
    else {
        $null = $scanErrors.Add("Required instruction root is missing or not a directory: '$instructionRoot'.")
    }

    $entrypointFiles = Get-ContextFilesByFilter -Root $RepositoryRoot -Filter 'AGENTS.md' -Errors $scanErrors
    foreach ($file in $entrypointFiles) {
        $files.Add([pscustomobject]@{ FullName = $file.FullName; Kind = 'AGENTS' })
    }
    $pathComparison = Get-ContextPathComparison -RepositoryRoot $RepositoryRoot
    if (@($entrypointFiles | Where-Object { [string]::Equals($_.FullName, (Join-Path $RepositoryRoot 'AGENTS.md'), $pathComparison) }).Count -eq 0) {
        $null = $scanErrors.Add("Required root AGENTS entrypoint is missing or unreadable: '$(Join-Path $RepositoryRoot 'AGENTS.md')'.")
    }

    $copilotPath = Join-Path $RepositoryRoot '.github/copilot-instructions.md'
    $copilotExists = Test-Path -LiteralPath $copilotPath -PathType Leaf
    $copilotPathSafe = $copilotExists -and (Test-ContextPathWithoutReparsePoints -RepositoryRoot $RepositoryRoot -RelativePath '.github/copilot-instructions.md')
    if ($copilotExists -and -not $copilotPathSafe) {
        $null = $scanErrors.Add("Skipped reparse-point Copilot entrypoint '$copilotPath'.")
    }
    elseif ($copilotExists) {
        try {
            $copilotItem = Get-Item -LiteralPath $copilotPath -Force -ErrorAction Stop
            if ([bool]($copilotItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                $null = $scanErrors.Add("Skipped reparse-point Copilot entrypoint '$copilotPath'.")
            }
            else {
                $files.Add([pscustomobject]@{ FullName = $copilotPath; Kind = 'entrypoint' })
            }
        }
        catch {
            $null = $scanErrors.Add("Unable to inspect Copilot entrypoint '$copilotPath': $($_.Exception.Message)")
        }
    }
    else {
        $null = $scanErrors.Add("Required Copilot entrypoint is missing or unreadable: '$copilotPath'.")
    }

    $uniqueFiles = [System.Collections.Generic.List[object]]::new()
    $seenFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in @($files | Sort-Object FullName)) { if ($seenFiles.Add([string]$file.FullName)) { $uniqueFiles.Add($file) } }
    return [pscustomobject]@{ Files = @($uniqueFiles); Errors = @($scanErrors) }
}

function Test-ContextPathWithoutReparsePoints {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $current = $RepositoryRoot
    foreach ($segment in ($RelativePath -split '[\\/]')) {
        if ([string]::IsNullOrWhiteSpace($segment) -or $segment -eq '.') { continue }
        if ($segment -eq '..') { return $false }
        $current = Join-Path $current $segment
        try {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if ([bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) { return $false }
        }
        catch {
            return $false
        }
    }

    return $true
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
                if ($group.Name -eq 'required') {
                    $requiredFullPath = Join-Path $resolvedRoot $relative
                    if (-not (Test-Path -LiteralPath $requiredFullPath -PathType Leaf) -or
                        -not (Test-ContextPathWithoutReparsePoints -RepositoryRoot $resolvedRoot -RelativePath $relative)) {
                        $unresolved.Add("Required context path is missing or unreadable: '$relative'.")
                    }
                    else {
                        try { $null = Get-Content -LiteralPath $requiredFullPath -Raw -ErrorAction Stop }
                        catch { $unresolved.Add("Required context path is missing or unreadable: '$relative'.") }
                    }
                }
            }
        }
    }

    $candidateResult = Get-ContextCandidates -RepositoryRoot $resolvedRoot
    foreach ($scanError in @($candidateResult.Errors)) { $unresolved.Add($scanError) }
    $sourceRevision = Get-ContextSourceRevision -RepositoryRoot $resolvedRoot
    $fullInventory = @($requested | Where-Object { $_.Path -cmatch '^\.github/instructions/.*\.instructions\.md$' -or $_.Path -cmatch '(^|/)AGENTS\.md$' -or $_.Path -ceq '.github/copilot-instructions.md' }).Count -gt 0
    $pathComparison = Get-ContextPathComparison -RepositoryRoot $resolvedRoot
    $domainProbePaths = @{
        'csharp' = @('__domain__.cs')
        'c#' = @('__domain__.cs')
        'powershell' = @('__domain__.ps1')
        'docs' = @('docs/Docusaurus/docs/__domain__.md', 'docs/Docusaurus/docs/__domain__.mdx', 'docs/Docusaurus/docs/adr/0001-example.md')
        'documentation' = @('docs/Docusaurus/docs/__domain__.md', 'docs/Docusaurus/docs/__domain__.mdx', 'docs/Docusaurus/docs/adr/0001-example.md')
        'markdown' = @('__domain__.md', 'docs/Docusaurus/docs/__domain__.md')
        'css' = @('__domain__.css')
        'razor' = @('__domain__.razor')
        'blazor' = @('__domain__.razor')
        'testing' = @('tests/__domain__.cs')
        'serialization' = @('__domain__.cs')
        'orleans' = @('__domain__.cs')
    }
    foreach ($domain in @($ContentDomain)) {
        if (-not $domainProbePaths.ContainsKey($domain.ToLowerInvariant())) { $unresolved.Add("Unsupported content domain hint: '$domain'.") }
    }
    $entrypointProbePaths = [System.Collections.Generic.List[string]]::new()
    foreach ($request in @($requested | Where-Object { $_.Kind -ne 'required' })) {
        $entrypointProbePaths.Add($request.Path)
    }
    foreach ($domain in @($ContentDomain)) {
        $domainKey = $domain.ToLowerInvariant()
        if ($domainProbePaths.ContainsKey($domainKey)) {
            foreach ($probe in @($domainProbePaths[$domainKey])) { $entrypointProbePaths.Add($probe) }
        }
    }
    foreach ($role in @($WorkflowRole)) {
        $roleToken = $role.ToLowerInvariant()
        $entrypointProbePaths.Add(".github/agents/example-$roleToken.agent.md")
        $entrypointProbePaths.Add(".github/agents/example-$roleToken.md")
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
        if ($fullInventory) {
            $isSelected = $true
            $reasons.Add('full-inventory')
        }
        elseif ($candidate.Kind -eq 'AGENTS') {
            $entryDirectory = if ($relative -eq 'AGENTS.md') { '' } else { $relative.Substring(0, $relative.Length - '/AGENTS.md'.Length) }
            $entryPrefix = if ($entryDirectory) { "$entryDirectory/" } else { '' }
            $isSelected = [string]::Equals($relative, 'AGENTS.md', $pathComparison) -or @($entrypointProbePaths | Where-Object {
                $_.Equals($entryDirectory, $pathComparison) -or
                ($entryPrefix -and $_.StartsWith($entryPrefix, $pathComparison))
            }).Count -gt 0
            if ($isSelected) { $reasons.Add('scoped-entrypoint') }
        }
        elseif ($candidate.Kind -eq 'entrypoint') {
            $isSelected = $true
            $reasons.Add('host-entrypoint')
        }
        else {
            $expandedPatterns = @($frontMatter.Patterns | ForEach-Object { Expand-ContextPattern -Pattern $_ })
            if ($frontMatter.Status -eq 'valid' -and $expandedPatterns -contains '**') {
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
                $domainKey = $domain.ToLowerInvariant()
                if (-not $domainProbePaths.ContainsKey($domainKey)) {
                    $unresolved.Add("Unsupported content domain hint: '$domain'.")
                    continue
                }
                $probes = @($domainProbePaths[$domainKey])
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
                    $roleProbes = @($candidateResult.Files | Where-Object Kind -EQ 'entrypoint' | ForEach-Object { ConvertTo-ContextRelativePath -RepositoryRoot $resolvedRoot -Path $_.FullName } | Where-Object { $_ -and $_ -match [regex]::Escape($roleVariant) })
                    if (@($roleProbes).Count -eq 0) { $roleProbes = @(".github/agents/$roleVariant.agent.md", ".github/agents/$roleVariant.md") }
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
            ReferencedRoutes = if ($null -ne $content) { @(Get-ContextRoutes -Content $content -RepositoryRoot $resolvedRoot -SourcePath $candidate.FullName) } else { @() }
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
        $safePath = ([string]$entry.Path -replace '[\x00-\x1F\x7F]', '?')
        $reasons = (($entry.Reasons -join ',') -replace '[\x00-\x1F\x7F]', '?')
        $lines.Add("$safePath | scope=$($entry.ScopeStatus) | reasons=$reasons | bytes=$($entry.ByteCount) | words=$($entry.WordCount) | hash=$($entry.ContentHash)")
    }
    foreach ($unresolved in @($Context.Unresolved)) { $lines.Add("UNRESOLVED: $(([string]$unresolved -replace '[\x00-\x1F\x7F]', '?'))") }
    return @($lines)
}

Export-ModuleMember -Function Get-AgentContext, Format-AgentContextText
