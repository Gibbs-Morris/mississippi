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
        New-Item -ItemType Directory -Path (Join-Path $fixtureRoot '.github/instructions'), (Join-Path $fixtureRoot '.github/agents'), (Join-Path $fixtureRoot '.scratchpad/deep'), (Join-Path $fixtureRoot 'nested/feature'), (Join-Path $fixtureRoot 'foo'), (Join-Path $fixtureRoot 'foobar'), (Join-Path $fixtureRoot 'src'), (Join-Path $fixtureRoot 'docs/Docusaurus/docs/adr') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'AGENTS.md') -Value '# Root guidance'
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
'@
        Set-Content -LiteralPath (Join-Path $fixtureRoot '.github/instructions/csharp.instructions.md') -Value @'
---
applyTo: '**/*.cs'
---

# C# guidance
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

    It 'matches AGENTS guidance on directory boundaries' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'foobar/example.cs'

        $context.Selected.Path | Should -Not -Contain 'foo/AGENTS.md'
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

    It 'prunes excluded trees before discovery' {
        $context = Get-AgentContext -RepositoryRoot $fixtureRoot -ChangedPath 'src/Example.cs'

        $context.Entries.Path | Should -Not -Contain '.scratchpad/AGENTS.md'
        $context.Unresolved | Should -HaveCount 0
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
        $context.PSObject.Properties.Name | Should -Not -Contain 'TokenCount'
        $context.PSObject.Properties.Name | Should -Not -Contain 'Latency'
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
