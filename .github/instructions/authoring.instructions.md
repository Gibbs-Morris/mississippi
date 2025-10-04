---
applyTo: '**'
---

# Instruction Authoring Guide

This guide teaches contributors how to write and maintain instruction files (`*.instructions.md`) in the Mississippi repository. Follow these standards to ensure consistency, discoverability, and alignment with existing documentation patterns.

## Purpose

Instruction files are the canonical source of truth for development practices, patterns, and workflows in this repository. They serve as both human-readable documentation and machine-readable rules for AI coding assistants (via Cursor `.mdc` files). This guide ensures all instruction files follow a consistent structure, tone, and quality standard.

## Scope and Audience

**Audience:** Contributors who create, update, or maintain `*.instructions.md` files in `.github/instructions/`.

**In scope:**
- Naming and placement conventions
- Structure and formatting standards
- Content organization patterns
- Cross-referencing and linking rules
- Validation and quality gates
- Change control and review requirements

**Out of scope:**
- Specific technical content for individual instruction files (covered in those files)
- General Markdown authoring (see `markdown.instructions.md`)
- Repository-wide contribution guidelines (see `README.md`)

## When to Create or Update an Instruction File

**Create a new instruction file when:**
- Introducing a new technology, framework, or pattern used across multiple components
- Establishing team conventions that require consistent application
- Documenting mandatory practices that must be enforced by code review or tooling
- Providing guidance that AI agents need to follow deterministically

**Update an existing instruction file when:**
- Correcting factual errors or outdated information
- Adding clarifying examples or troubleshooting steps
- Aligning with repository changes (new scripts, updated tools, workflow modifications)
- Resolving conflicts discovered between instruction files

**Why:** Instruction files are high-leverage documentation. Creating them only when patterns are established prevents premature standardization. Updating them promptly prevents drift and maintains trust.

## Naming and Placement Rules

### Rule 1: File Name Pattern

Use lowercase kebab-case with the `.instructions.md` suffix.

**Pattern:** `<topic>.instructions.md`

**Examples:**
- `build-rules.instructions.md`
- `testing.instructions.md`
- `logging-rules.instructions.md`
- `orleans-serialization.instructions.md`

**Why:** Lowercase kebab-case is shell-friendly, sorts predictably, and aligns with GitHub conventions. The `.instructions.md` suffix enables tooling to discover and process these files automatically (e.g., `scripts/sync-instructions-to-mdc.ps1`).

### Rule 2: Location

Place all instruction files in `.github/instructions/`.

**Why:** Centralized location makes discovery trivial. The `.github/` prefix follows GitHub conventions for repository metadata, and `instructions/` clearly distinguishes these from workflows, templates, or issue forms.

### Rule 3: One Topic Per File

Each instruction file should cover one cohesive topic. Split large topics into focused files rather than creating monolithic documents.

**Examples of good scope:**
- `build-rules.instructions.md` — build pipeline and quality gates
- `testing.instructions.md` — test strategy and conventions
- `authoring.instructions.md` — instruction file authoring (this file)

**Examples of poor scope:**
- `everything.instructions.md` — avoid catch-all files
- `build-and-test-and-deploy.instructions.md` — too broad

**Why:** Focused files are easier to maintain, review, and reference. They reduce merge conflicts and allow contributors to become experts in specific areas.

## Standard Section Template

Use this template when creating a new instruction file. Adapt sections to your topic, but maintain this structure for consistency.

```markdown
---
applyTo: '**'
---

# [Topic] Best Practices

This document defines [brief purpose statement]. All [audience] must follow these guidelines to ensure [goals].

## At-a-Glance Quick-Start

- [Most common command or action]
- [Second most common command or action]
- [Third most common command or action]

> **Drift check:** Before running any PowerShell script referenced here, open the script in `scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

## Core Principles

- **[Principle 1]** — [brief explanation]
- **[Principle 2]** — [brief explanation]
- **[Principle 3]** — [brief explanation]

## [Primary Section 1]

[Detailed guidance with subsections as needed]

### [Subsection A]

[Content with code examples, tables, or lists]

**Why:** [Brief explanation of the rationale]

### [Subsection B]

[Content]

**Why:** [Rationale]

## [Primary Section 2]

[Content]

## Examples

[2-4 concrete, runnable examples from this repository]

## Anti-Patterns to Avoid

[Common mistakes with ❌ bad examples and ✅ good alternatives]

## Related Guidelines

This document should be read in conjunction with:

- **[Related Topic]** (`.github/instructions/[filename].instructions.md`) - For [cross-reference description]

## References

- [External link 1]: [URL]
- [External link 2]: [URL]

---

**Last verified:** YYYY-MM-DD
**Default branch:** main
```

### Section Purposes

- **YAML front matter:** Defines which files this instruction applies to (for Cursor `.mdc` generation)
- **H1 title:** Clear, descriptive topic name
- **At-a-Glance Quick-Start:** Immediate value for readers who just need the commands
- **Drift check note:** Reminds readers that scripts are authoritative
- **Core Principles:** High-level guidance before diving into details
- **Primary sections:** Organized by workflow, not alphabetically
- **Examples:** Real, copy-paste-ready code from this repository
- **Anti-Patterns:** Help prevent common mistakes
- **Related Guidelines:** Explicit cross-references to avoid duplication
- **References:** External links for deeper learning
- **Footer:** Verification date and branch for staleness detection

**Why:** This template provides cognitive scaffolding. Readers know where to find quick answers (Quick-Start), deep understanding (Core Principles and Primary sections), and practical application (Examples). The structure is consistent across all instruction files.

## Style Rules

### Rule 1: Short Sentences and Active Voice

Use short, declarative sentences in active voice. Target 15-20 words per sentence.

**Good:** "Run the script to validate your changes."
**Bad:** "Your changes should be validated by running the script."

**Why:** Short sentences reduce cognitive load. Active voice clarifies who does what, which is critical for task-oriented documentation.

### Rule 2: One Action Per Step

In procedural sections, limit each numbered step to one action. Use substeps for related actions.

**Good:**
```markdown
1. Open the file.
2. Edit the configuration.
3. Save and close the file.
```

**Bad:**
```markdown
1. Open the file, edit the configuration, then save and close it.
```

**Why:** Single-action steps are easier to follow and harder to misinterpret. They also work better for AI agents parsing instructions.

### Rule 3: Consistent Heading Levels

Use H2 for major sections, H3 for subsections, H4 for sub-subsections. Do not skip levels.

**Why:** Consistent hierarchy enables tools to generate accurate tables of contents and helps readers navigate.

### Rule 4: Relative Links

Use relative links for internal references. Always test that links resolve.

**Good:** `[Testing Strategy](./testing.instructions.md)`
**Bad:** `[Testing Strategy](https://github.com/Gibbs-Morris/mississippi/blob/main/.github/instructions/testing.instructions.md)`

**Why:** Relative links work in forks, branches, and local clones. They're portable and don't break when the repository is renamed or moved.

### Rule 5: US English Spelling

Use US English spelling conventions throughout.

**Examples:** "analyzer" (not "analyser"), "color" (not "colour"), "organize" (not "organise")

**Why:** Consistency with .NET documentation, Microsoft conventions, and the broader C# ecosystem.

### Rule 6: Include "Why" Lines

After significant rules or recommendations, add a brief "Why" line explaining the rationale.

**Why:** Explanations build understanding and buy-in. They also help reviewers evaluate whether rules still apply when circumstances change.

## Code Sample Policy

### Rule 1: Real Commands Only

Use only commands, scripts, and tools that exist in this repository. Never use placeholder names or fictional examples.

**Good:**
```powershell
pwsh ./scripts/build-mississippi-solution.ps1
```

**Bad:**
```powershell
pwsh ./scripts/build-all.ps1  # (if this script doesn't exist)
```

**Why:** Readers copy-paste examples directly. Fictional examples break trust and waste time when they fail.

### Rule 2: OS-Specific Blocks

When commands differ by OS, provide both PowerShell and Bash examples.

**Example:**
```markdown
**PowerShell:**
```powershell
pwsh ./scripts/orchestrate-solutions.ps1
```

**Bash:**
```bash
pwsh ./scripts/orchestrate-solutions.ps1
```
```

**Why:** Readers use different development environments. Explicit OS sections prevent confusion about which syntax to use.

### Rule 3: Expected Outputs

For commands with significant output, show what success looks like.

**Example:**
```powershell
pwsh ./scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation
```

**Expected output:**
```
=== QUALITY SUMMARY (Core.Abstractions.Tests) ===
RESULT: PASS
TEST_TOTAL: 42
TEST_PASSED: 42
TEST_FAILED: 0
COVERAGE: 95%
```

**Why:** Expected outputs help readers verify they're on the right track and diagnose failures.

### Rule 4: Exit Codes

Document expected exit codes for scripts, especially in CI contexts.

**Example:**
```markdown
Exit codes:
- `0`: All tests passed
- `1`: One or more tests failed or coverage below threshold
```

**Why:** CI workflows depend on exit codes. Documenting them prevents silent failures.

### Rule 5: Cleanup Notes

When examples create side effects (files, processes, state), note how to clean up.

**Example:**
```markdown
> **Cleanup:** This script creates files in `./test-results`. Remove them with `rm -rf ./test-results` or let `.gitignore` exclude them.
```

**Why:** Readers appreciate knowing how to reset their environment, especially when iterating.

## Running Scripts and Commands

This repository uses PowerShell scripts in `scripts/` for build, test, and quality automation. All scripts require PowerShell 7+ (`pwsh`).

### Prerequisites

Before running any scripts:

```powershell
dotnet tool restore
```

**Why:** Scripts depend on tools like GitVersion, SLNGen, ReSharper CLI, and Stryker.NET defined in `.config/dotnet-tools.json`.

### Key Scripts for Instruction Authors

| Script | Purpose | Usage |
|--------|---------|-------|
| `sync-instructions-to-mdc.ps1` | Regenerate Cursor `.mdc` rule files from instruction Markdown | `pwsh ./scripts/sync-instructions-to-mdc.ps1` |
| `build-mississippi-solution.ps1` | Build the core Mississippi library | `pwsh ./scripts/build-mississippi-solution.ps1` |
| `unit-test-mississippi-solution.ps1` | Run unit tests for Mississippi | `pwsh ./scripts/unit-test-mississippi-solution.ps1` |
| `orchestrate-solutions.ps1` | Full pipeline: build → test → mutate → cleanup | `pwsh ./scripts/orchestrate-solutions.ps1` |

**Why:** These scripts are frequently referenced in instruction files. Document them here so authors can verify commands before including them.

### Verifying Script Commands

Before referencing a script in an instruction file:

1. Open the script to confirm its parameters and behavior
2. Run the script in a test environment
3. Document the exact command and expected output
4. Include the "Drift check" note in your Quick-Start section

**Why:** Scripts evolve. The drift check note reminds readers to verify behavior rather than blindly trust documentation.

### Running Local Checks

This repository enforces Markdown quality via Super-Linter in CI. To run checks locally:

**Check all Markdown files:**
```bash
# Super-Linter runs in CI via .github/workflows/markdown-lint.yml
# For local checks, use markdownlint-cli2:
npx markdownlint-cli2 "**/*.md"
```

**Expected success:**
- Exit code `0`
- No output (or only informational messages)

**On failure:**
- Fix reported violations in your files
- Re-run until clean
- Do not suppress rules globally

**Why:** Consistent linting prevents style drift. Local checks give fast feedback before pushing.

## Cross-Referencing Rules

### Rule 1: One Authoritative Source

Each topic should have exactly one authoritative instruction file. Other files may link to it but should not duplicate its content.

**Example:** `logging-rules.instructions.md` is authoritative for logging patterns. `csharp.instructions.md` references it rather than re-explaining logging.

**Why:** Single source of truth prevents inconsistencies and simplifies maintenance. When the authoritative file updates, all links remain valid.

### Rule 2: Use Relative Links

Link to other instruction files using relative paths.

**Example:**
```markdown
See [Logging Rules](./logging-rules.instructions.md) for high-performance logging patterns.
```

**Why:** Relative links work in all contexts (local, web, forks) and don't break if the repository URL changes.

### Rule 3: Link with Anchors for Specific Sections

When referencing a specific section, use anchor links.

**Example:**
```markdown
Follow the [Testing L0 guidelines](./testing.instructions.md#l0--pure-unit-tests) for unit tests.
```

**Why:** Direct links save readers time and ensure they land on the relevant content.

### Rule 4: Validate Links

Before committing, verify all internal links resolve.

**Validation command:**
```bash
# Check that referenced files exist
ls -la .github/instructions/testing.instructions.md
```

**Why:** Broken links undermine trust. Validation catches typos and moved files.

## Security and Secrets

### Rule 1: Never Commit Secrets

Instruction files must never contain secrets, API keys, passwords, or tokens.

**Why:** Public repositories expose committed secrets permanently. Even private repositories risk exposure through accidental sharing or future open-sourcing.

### Rule 2: Use Environment Variables

When examples require credentials, show how to use environment variables.

**Example:**
```powershell
$connectionString = $env:COSMOS_CONNECTION_STRING
```

**Why:** Environment variables keep secrets out of source control and enable different configurations per environment.

### Rule 3: Document Secret Stores

If the repository uses a secret store (e.g., Azure Key Vault, GitHub Secrets), reference it explicitly.

**Example:**
```markdown
Connection strings are stored in Azure Key Vault and accessed via managed identity in production.
```

**Why:** Readers need to know where to find or configure secrets for real usage.

### Rule 4: Redaction Patterns

When showing example outputs that might contain sensitive data, use redaction patterns.

**Example:**
```
Cosmos DB connection: AccountEndpoint=https://[REDACTED].documents.azure.com;...
```

**Why:** Examples should look realistic without exposing actual credentials.

## Versioning and Change Control

### Rule 1: CODEOWNERS Review

All changes to instruction files require review by `@BenjaminLGibbs` (per `.github/CODEOWNERS`).

**Why:** Instruction files are high-impact. Review ensures changes are accurate, consistent, and aligned with repository goals.

### Rule 2: PR Descriptions Must Include "Docs Impact"

When modifying instruction files, include a "Docs impact" section in the PR description.

**Example:**
```markdown
## Docs impact

Updated `testing.instructions.md` to reflect new L4 test requirements.
Cross-references in `build-rules.instructions.md` remain accurate.
```

**Why:** Explicit documentation impact helps reviewers assess downstream effects and ensures related files stay synchronized.

### Rule 3: Version Verification Date

Include "Last verified: YYYY-MM-DD" in the footer of each instruction file. Update this date when you verify all content is current.

**Example:**
```markdown
---

**Last verified:** 2025-10-04
**Default branch:** main
```

**Why:** Dates help readers assess staleness. Files last verified years ago likely need review.

### Rule 4: Link to External Resources by Permanent URL

When linking to external documentation, use permanent URLs (avoid version-specific links unless necessary).

**Good:** `https://learn.microsoft.com/dotnet/core/`
**Bad:** `https://docs.microsoft.com/en-us/dotnet/core/` (old domain)

**Why:** Permanent URLs reduce broken links over time.

## Examples from This Repository

### Example 1: Build Rules

[build-rules.instructions.md](./build-rules.instructions.md) demonstrates:
- Clear "At-a-Glance Quick-Start" with commands
- Strong "Critical Rule" sections with emoji emphasis
- Code examples showing PowerShell commands from `scripts/`
- Cross-references to related files (`testing.instructions.md`, `csharp.instructions.md`)

### Example 2: Testing Strategy

[testing.instructions.md](./testing.instructions.md) demonstrates:
- Layered content (L0-L4 test levels with a table)
- Detailed subsections for each test level
- Cross-references to external Microsoft Learn articles
- Related Guidelines section at the end

### Example 3: Markdown Authoring

[markdown.instructions.md](./markdown.instructions.md) demonstrates:
- Mandatory rule enumeration (MD001-MD059)
- Prescriptive guidance with no ambiguity
- Quality enforcement section with commands
- Note block for repository-specific configuration

### Example 4: Instruction-MDC Sync

[instruction-mdc-sync.instructions.md](./instruction-mdc-sync.instructions.md) demonstrates:
- Concise scope definition
- Clear workflow steps
- Script reference with exact command
- PR checklist for reviewers

## Maintenance Checklist

Use this checklist when creating or updating an instruction file:

- [ ] File name follows `<topic>.instructions.md` pattern
- [ ] File placed in `.github/instructions/`
- [ ] YAML front matter includes `applyTo` directive
- [ ] H1 title is clear and specific
- [ ] At-a-Glance Quick-Start section provides immediate value
- [ ] Core Principles section articulates high-level guidance
- [ ] All script references verified by opening the script
- [ ] All commands are real and runnable in this repository
- [ ] Code blocks specify language (powershell, bash, csharp, json, etc.)
- [ ] All internal links use relative paths
- [ ] All internal links verified to resolve
- [ ] "Why" lines included for significant rules
- [ ] Examples from this repository (not fictional)
- [ ] Related Guidelines section cross-references other instruction files
- [ ] Footer includes "Last verified: YYYY-MM-DD" and "Default branch: main"
- [ ] No secrets or credentials in examples
- [ ] PR description includes "Docs impact" section
- [ ] Ran `pwsh ./scripts/sync-instructions-to-mdc.ps1` after changes (if applicable)

## Known Discrepancies

This section documents conflicts or inconsistencies discovered across instruction files during fact-checking. Empty if no discrepancies exist.

| File | Issue | Impact | Proposed Fix |
|------|-------|--------|--------------|
| *(none found)* | — | — | — |

**Note:** Discrepancies are resolved through focused updates to the conflicting files, not by changing this guide. This section exists to track issues until resolution.

## Glossary

**Mississippi Framework:** The core .NET library in `mississippi.slnx` that provides event sourcing, Orleans integration, and related infrastructure.

**Samples Solution:** The `samples.slnx` file that includes Mississippi projects plus sample applications demonstrating usage patterns.

**Instruction file:** A Markdown file in `.github/instructions/` ending with `.instructions.md` that defines development practices.

**Cursor `.mdc` file:** A machine-readable rule file in `.cursor/rules/` generated from instruction Markdown for Cursor AI assistant.

**L0-L4 tests:** Layered testing model where L0 = pure unit tests, L1 = unit tests with light infrastructure, L2 = functional tests, L3 = end-to-end tests, L4 = production tests. See `testing.instructions.md`.

**Zero warnings policy:** Repository standard that treats all compiler, analyzer, and StyleCop warnings as build errors. See `build-rules.instructions.md`.

**Stryker.NET:** Mutation testing tool that validates test quality by introducing defects and verifying tests catch them.

**ReSharper CLI:** Code formatting and inspection tool (`jb cleanupcode`) used in cleanup scripts.

**Orleans:** Microsoft's distributed actor framework used throughout Mississippi. See `orleans.instructions.md`.

**Drift check:** Reminder note in Quick-Start sections to verify script behavior before trusting documentation.

---

**Last verified:** 2025-10-04
**Default branch:** main
