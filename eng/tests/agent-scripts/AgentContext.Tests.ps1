#requires -Module Pester

$ErrorActionPreference = 'Stop'

Describe 'Agent context selection' {
    BeforeAll {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
        $powerShellPath = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
        $modulePath = Join-Path $repoRoot 'eng/src/agent-scripts/AgentContext.psm1'
        $scriptPath = Join-Path $repoRoot 'eng/src/agent-scripts/get-agent-context.ps1'
        Import-Module -Name $modulePath -Force

        $fixtureRoot = Join-Path $TestDrive 'context-repository'
        New-Item -ItemType Directory -Path (Join-Path $fixtureRoot '.github/instructions'), (Join-Path $fixtureRoot '.github/agents'), (Join-Path $fixtureRoot '.scratchpad/deep'), (Join-Path $fixtureRoot 'nested/feature'), (Join-Path $fixtureRoot 'foo'), (Join-Path $fixtureRoot 'foobar'), (Join-Path $fixtureRoot 'src'), (Join-Path $fixtureRoot 'case-probe'), (Join-Path $fixtureRoot 'docs/Docusaurus/docs/adr') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'AGENTS.md') -Value '# Root guidance'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'case-probe/agents.md') -Value '# Lowercase non-entrypoint'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'nested/AGENTS.md') -Value '# Nested guidance'
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/copilot-instructions.md') -Value @'
---
applyTo: '**'
---

# Copilot entrypoint
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/global.instructions.md') -Value @'
---
applyTo: '**'
---

# Global guidance

See [the shared route](../skills/example/SKILL.md#section).
Ignore [a file URI](file:///etc/policy.md) and [an FTP URI](ftp://example.com/policy.md).
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/csharp.instructions.md') -Value @'
---
applyTo: '**/*.cs'
---

# C# guidance
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/inline-comment.instructions.md') -Value @'
---
applyTo: '**/*.cs' # C# guidance with an inline comment
---

# Inline-comment guidance
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/markdown.instructions.md') -Value @'
---
applyTo: '**/*.{md,mdx}'
---

# Markdown guidance
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/planner-route.instructions.md') -Value @'
---
applyTo: '.github/agents/*planner*.agent.md'
---

# Planner route
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/build-route.instructions.md') -Value @'
---
applyTo: '.github/agents/*build*.agent.md'
---

# Build route
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/adr-route.instructions.md') -Value @'
---
applyTo: 'docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md'
---

# ADR route
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/documentation-route.instructions.md') -Value @'
---
applyTo: 'docs/Docusaurus/docs/**/*.{md,mdx}'
---

# Documentation route
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/style-route.instructions.md') -Value @'
---
applyTo: '**/*.css'
---

# Style route
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/malformed.instructions.md') -Value @'
applyTo: '**/*.cs'

# Missing frontmatter
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/unbalanced.instructions.md') -Value @'
---
applyTo: '**/*.{cs,razor'
---

# Unbalanced scope
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/unbalanced-class.instructions.md') -Value @'
---
applyTo: '**/[abc.md'
---

# Unbalanced class
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/nested-applyto.instructions.md') -Value @'
---
metadata:
  applyTo: '**/*.cs'
---

# Nested metadata
'@
        $expensivePattern = ('{a,b}' * 13) -join ''
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/expensive-expansion.instructions.md') -Value "---`napplyTo: '**/*.$expensivePattern'`n---`n`n# Expensive expansion"
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'src/Example.cs') -Value 'class Example { }'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/guide.md') -Value '# Guide'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'docs/Docusaurus/docs/adr/0001-example.md') -Value '# ADR'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'nested/feature/example.ps1') -Value 'Write-Output data'
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.scratchpad/AGENTS.md') -Value '# Excluded guidance'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'foo/AGENTS.md') -Value '# Foo guidance'
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'foobar/example.cs') -Value 'class Sibling { }'
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/agents/example-planner.agent.md') -Value '# Planner'
    }

    It 'selects globals, matching paths, domains, roles, and nested entrypoints' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs' -IntendedPath 'docs/guide.md' -ReviewedPath 'nested/feature/example.ps1' -ContentDomain CSharp -WorkflowRole planner

        $context.Complete | Should -BeTrue
        $context.SourceRevision | Should -Be 'unknown'
        $context.SelectedFileCount | Should -BeGreaterThan 0
        $context.Selected.Path | Should -Contain 'AGENTS.md'
        $context.Selected.Path | Should -Contain 'nested/AGENTS.md'
        $context.Selected.Path | Should -Contain '.github/instructions/csharp.instructions.md'
        $context.Selected.Path | Should -Contain '.github/instructions/planner-route.instructions.md'
        $csharp = @($context.Selected | Where-Object Path -EQ '.github/instructions/csharp.instructions.md')[0]
        $csharp.Reasons | Should -Contain 'path:changed:src/Example.cs'
        $csharp.Reasons | Should -Contain 'content-domain:CSharp'
    }

    It 'includes every discovered global instruction dynamically' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $globals = @($context.Entries | Where-Object { $_.Kind -eq 'instruction' -and $_.ScopeStatus -eq 'valid' -and $_.Scope -contains '**' })

        $globals.Count | Should -BeGreaterThan 0
        foreach ($global in $globals) {
            $global.Selected | Should -BeTrue
            $global.Reasons | Should -Contain 'global-scope'
        }
    }

    It 'keeps malformed metadata selected for direct inspection' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $malformed = @($context.Selected | Where-Object Path -EQ '.github/instructions/malformed.instructions.md')[0]

        $malformed.ScopeStatus | Should -Be 'unknown'
        $malformed.Reasons | Should -Contain 'unknown-scope-requires-inspection'
        $malformed.ScopeNote | Should -Match 'frontmatter'
    }

    It 'keeps unbalanced scope metadata selected for direct inspection' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $unbalanced = @($context.Selected | Where-Object Path -EQ '.github/instructions/unbalanced.instructions.md')[0]

        $unbalanced.ScopeStatus | Should -Be 'unknown'
        $unbalanced.ScopeNote | Should -Match 'unbalanced braces'
    }

    It 'keeps unmatched character-class metadata selected for direct inspection' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $unbalanced = @($context.Selected | Where-Object Path -EQ '.github/instructions/unbalanced-class.instructions.md')[0]

        $unbalanced.ScopeStatus | Should -Be 'unknown'
        $unbalanced.ScopeNote | Should -Match 'character classes'
    }

    It 'does not treat nested applyTo keys as top-level scope metadata' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $nested = @($context.Selected | Where-Object Path -EQ '.github/instructions/nested-applyto.instructions.md')[0]

        $nested.ScopeStatus | Should -Be 'unknown'
        $nested.ScopeNote | Should -Match 'exactly one applyTo'
    }

    It 'rejects excessive brace expansion before matching' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $expensive = @($context.Selected | Where-Object Path -EQ '.github/instructions/expensive-expansion.instructions.md')[0]

        $expensive.ScopeStatus | Should -Be 'unknown'
        $expensive.ScopeNote | Should -Match 'expansion exceeds'
    }

    It 'parses inline comments in quoted applyTo scalars' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $inline = @($context.Selected | Where-Object Path -EQ '.github/instructions/inline-comment.instructions.md')[0]

        $inline.ScopeStatus | Should -Be 'valid'
        $inline.Reasons | Should -Contain 'path:changed:src/Example.cs'
    }

    It 'supports brace globs and treats deleted changed paths as data' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'docs/deleted.md'

        $context.Complete | Should -BeTrue
        $context.Selected.Path | Should -Contain '.github/instructions/markdown.instructions.md'
        $context.Unresolved | Should -HaveCount 0
    }

    It 'normalizes internal traversal before matching scopes' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/../docs/guide.md'

        $context.Complete | Should -BeTrue
        $context.Selected.Path | Should -Contain '.github/instructions/markdown.instructions.md'
        @($context.Selected | Where-Object Path -EQ '.github/instructions/markdown.instructions.md').Reasons | Should -Contain 'path:changed:docs/guide.md'
    }

    It 'rejects rooted paths returned from cross-volume conversion' {
        if (-not $IsWindows) {
            Set-ItResult -Skipped -Because 'Cross-volume rooted relative paths are Windows-specific.'
            return
        }

        $currentDrive = ([System.IO.Path]::GetPathRoot($fixtureRoot)).Substring(0, 1).ToUpperInvariant()
        $otherDrive = if ($currentDrive -eq 'Z') { 'Y' } else { 'Z' }
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath "${otherDrive}:\outside\file.cs"

        $context.Complete | Should -BeFalse
        $context.Unresolved | Should -Contain "changed path is outside the repository or invalid: '${otherDrive}:\outside\file.cs'."
    }

    It 'matches AGENTS guidance on directory boundaries' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'foobar/example.cs'

        $context.Selected.Path | Should -Not -Contain 'foo/AGENTS.md'
    }

    It 'discovers AGENTS filenames case-sensitively' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

        $context.Entries.Path | Should -Not -Contain 'case-probe/agents.md'
    }

    It 'reports a missing Copilot entrypoint as unresolved' {
        $copilotPath = Join-Path $fixtureRoot '.github/copilot-instructions.md'
        $backupPath = Join-Path $fixtureRoot '.github/copilot-instructions.backup.md'
        Move-Item -LiteralPath $copilotPath -Destination $backupPath
        try {
            $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

            $context.Complete | Should -BeFalse
            $context.Unresolved | Should -Contain "Required Copilot entrypoint is missing or unreadable: '$copilotPath'."
        }
        finally {
            Move-Item -LiteralPath $backupPath -Destination $copilotPath
        }
    }

    It 'supports character classes in instruction globs' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'docs/Docusaurus/docs/adr/0001-example.md'

        $context.Selected.Path | Should -Contain '.github/instructions/adr-route.instructions.md'
    }

    It 'matches workflow roles against declared agent scopes' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -WorkflowRole build

        $context.Selected.Path | Should -Contain '.github/instructions/build-route.instructions.md'
    }

    It 'routes documentation domains through nested documentation probes' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ContentDomain docs

        $context.Selected.Path | Should -Contain '.github/instructions/documentation-route.instructions.md'
    }

    It 'supports known style domains and reports unknown domains' {
        $known = Get-AgentContext -RepositoryRoot $fixtureRoot -ContentDomain css
        $unknown = Get-AgentContext -RepositoryRoot $fixtureRoot -ContentDomain mystery

        $known.Complete | Should -BeTrue
        $known.Selected.Path | Should -Contain '.github/instructions/style-route.instructions.md'
        $unknown.Complete | Should -BeFalse
        $unknown.Unresolved | Should -Contain "Unsupported content domain hint: 'mystery'."
    }

    It 'prunes excluded trees before discovery' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

        $context.Entries.Path | Should -Not -Contain '.scratchpad/AGENTS.md'
        $context.Unresolved | Should -HaveCount 0
    }

    It 'skips reparse-point guidance directories' {
        if (-not $IsWindows) {
            Set-ItResult -Skipped -Because 'Directory junctions are Windows reparse points.'
            return
        }

        $linkPath = Join-Path $fixtureRoot 'linked-guidance'
        New-Item -ItemType Junction -Path $linkPath -Target (Join-Path $fixtureRoot 'foo') | Out-Null
        try {
            $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

            $context.Unresolved | Should -Contain "Skipped reparse-point guidance directory '$linkPath'."
            $context.Entries.Path | Should -Not -Contain 'linked-guidance/AGENTS.md'
        }
        finally {
            Remove-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
        }
    }

    It 'rejects a reparse-point Copilot entrypoint' {
        $copilotPath = Join-Path $fixtureRoot '.github/copilot-instructions.md'
        $originalContent = Get-Content -LiteralPath $copilotPath -Raw
        $linkCreated = $false
        try {
            Remove-Item -LiteralPath $copilotPath -Force
            New-Item -ItemType SymbolicLink -Path $copilotPath -Target (Join-Path $fixtureRoot 'AGENTS.md') -ErrorAction Stop | Out-Null
            $linkCreated = $true
            $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

            $context.Complete | Should -BeFalse
            $context.Unresolved | Should -Contain "Skipped reparse-point Copilot entrypoint '$copilotPath'."
        }
        catch {
            if (-not $linkCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create file symbolic links.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $copilotPath -Force -ErrorAction SilentlyContinue
            Set-Content -LiteralPath $copilotPath -Value $originalContent
        }
    }

    It 'rejects a reparse-point instruction root' {
        $instructionRoot = Join-Path $fixtureRoot '.github/instructions'
        $backupRoot = Join-Path $fixtureRoot '.github/instructions-original'
        $junctionCreated = $false
        try {
            Move-Item -LiteralPath $instructionRoot -Destination $backupRoot
            New-Item -ItemType Junction -Path $instructionRoot -Target $backupRoot -ErrorAction Stop | Out-Null
            $junctionCreated = $true
            $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

            $context.Complete | Should -BeFalse
            $context.Unresolved | Should -Contain "Skipped reparse-point instruction root '$instructionRoot'."
        }
        catch {
            if (-not $junctionCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create directory junctions.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $instructionRoot -Force -ErrorAction SilentlyContinue
            if (Test-Path -LiteralPath $backupRoot -PathType Container) {
                Move-Item -LiteralPath $backupRoot -Destination $instructionRoot
            }
        }
    }

    It 'ignores unrelated reparse-point files during discovery' {
        $linkPath = Join-Path $fixtureRoot 'docs/latest.md'
        $linkCreated = $false
        try {
            New-Item -ItemType SymbolicLink -Path $linkPath -Target (Join-Path $fixtureRoot 'docs/guide.md') -ErrorAction Stop | Out-Null
            $linkCreated = $true
            $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

            $context.Complete | Should -BeTrue
            $context.Unresolved | Should -Not -Contain "Skipped reparse-point guidance file '$linkPath'."
        }
        catch {
            if (-not $linkCreated) {
                Set-ItResult -Skipped -Because 'The test host cannot create file symbolic links.'
            }
            else {
                throw
            }
        }
        finally {
            Remove-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
        }
    }

    It 'reports missing required context instead of returning an empty pass' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -RequiredPath 'missing/required.md'

        $context.Complete | Should -BeFalse
        $context.Unresolved | Should -Contain "Required context path is missing or unreadable: 'missing/required.md'."
    }

    It 'reads required context files before reporting complete' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -RequiredPath 'src/Example.cs'

        $context.Complete | Should -BeTrue
        $context.Unresolved | Should -HaveCount 0
    }

    It 'emits hashes, byte counts, word counts, and explicit route references' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'
        $global = @($context.Selected | Where-Object Path -EQ '.github/instructions/global.instructions.md')[0]

        $global.ContentHash | Should -Match '^SHA256:[0-9a-f]{64}$'
        $global.ByteCount | Should -BeGreaterThan 0
        $global.WordCount | Should -BeGreaterThan 0
        $global.ReferencedRoutes | Should -Contain '../skills/example/SKILL.md#section'
        $global.ReferencedRoutes | Should -Not -Contain 'file:///etc/policy.md'
        $global.ReferencedRoutes | Should -Not -Contain 'ftp://example.com/policy.md'
        $context.PSObject.Properties.Name | Should -Not -Contain 'TokenCount'
        $context.PSObject.Properties.Name | Should -Not -Contain 'Latency'
    }

    It 'accepts SHA-256 Git revisions' {
        if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
            Set-ItResult -Skipped -Because 'Git is required for revision evidence.'
            return
        }

        $gitRoot = Join-Path $TestDrive 'sha256-repository'
        New-Item -ItemType Directory -Path $gitRoot -Force | Out-Null
        & git init --quiet --object-format=sha256 $gitRoot 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Set-ItResult -Skipped -Because 'The installed Git does not support SHA-256 repositories.'
            return
        }

        & git -C $gitRoot config user.email 'agent-context@example.invalid'
        & git -C $gitRoot config user.name 'Agent Context Test'
        Set-Content -LiteralPath (Join-Path $gitRoot 'README.md') -Value '# SHA-256 fixture'
        & git -C $gitRoot add -- README.md
        & git -C $gitRoot commit --quiet -m 'Create SHA-256 fixture'

        $context = Get-AgentContext -RepositoryRoot $gitRoot

        $context.SourceRevision | Should -Match '^[0-9a-f]{64}$'
    }

    It 'produces a concise text index and JSON wrapper output' {
        $text = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs' -OutputFormat Text 2>&1 | Out-String
        $textExit = $LASTEXITCODE
        $json = & $powerShellPath -NoProfile -File $scriptPath -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs' -OutputFormat Json 2>&1 | Out-String
        $jsonExit = $LASTEXITCODE

        $textExit | Should -Be 0
        $text | Should -Match 'AGENT_CONTEXT: COMPLETE'
        $text | Should -Match 'bytes='
        $text | Should -Not -Match 'token|latency'
        $jsonExit | Should -Be 0
        ($json | ConvertFrom-Json).SchemaVersion | Should -Be '1.0'
        ($json | ConvertFrom-Json).Entries.Count | Should -BeGreaterThan 0
    }

    It 'uses the current directory when the wrapper omits RepositoryRoot' {
        Push-Location -LiteralPath $fixtureRoot
        try {
            $json = & $powerShellPath -NoProfile -File $scriptPath -ChangedPath 'src/Example.cs' -OutputFormat Json 2>&1 | Out-String
            $exitCode = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }

        $exitCode | Should -Be 0
        ($json | ConvertFrom-Json).Complete | Should -BeTrue
    }
}
