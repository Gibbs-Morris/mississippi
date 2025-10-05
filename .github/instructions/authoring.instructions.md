---
applyTo: '**/*.instructions.md'
---

# Instruction Authoring Guide

This guide teaches contributors how to write and maintain instruction files (`*.instructions.md`) in this repository. Follow these standards to ensure consistency, discoverability, and alignment with existing documentation patterns.

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
- General Markdown authoring (covered in separate markdown guidelines if present)
- Repository-wide contribution guidelines (see `README.md` or `CONTRIBUTING.md`)

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
- `coding-standards.instructions.md`
- `api-design.instructions.md`

**Why:** Lowercase kebab-case is shell-friendly, sorts predictably, and aligns with GitHub conventions. The `.instructions.md` suffix enables tooling to discover and process these files automatically.

### Rule 2: Location

Place all instruction files in `.github/instructions/`.

**Why:** Centralized location makes discovery trivial. The `.github/` prefix follows GitHub conventions for repository metadata, and `instructions/` clearly distinguishes these from workflows, templates, or issue forms.

### Rule 3: One Topic Per File

Each instruction file should cover one cohesive topic. Split large topics into focused files rather than creating monolithic documents.

**Examples of good scope:**

- `build.instructions.md` — build pipeline and quality gates
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

> **Drift check:** Before running any script referenced here, open the script in your repository's script directory (e.g., `scripts/`, `eng/src/agent-scripts/`, `tools/`, etc.) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

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
**Bad:** `[Testing Strategy](https://github.com/your-org/your-repo/blob/main/.github/instructions/testing.instructions.md)`

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
pwsh ./eng/src/agent-scripts/build-solution.ps1
```

**Bad:**

```powershell
pwsh ./eng/src/agent-scripts/build-all.ps1  # (if this script doesn't exist)
```

**Why:** Readers copy-paste examples directly. Fictional examples break trust and waste time when they fail.

### Rule 2: OS-Specific Blocks

When commands differ by OS, provide both PowerShell and Bash examples.

**Example:**

```markdown
**PowerShell:**
```powershell
pwsh ./eng/src/agent-scripts/build.ps1
```

```bash
pwsh ./eng/src/agent-scripts/build.ps1
```

**Why:** Readers use different development environments. Explicit OS sections prevent confusion about which syntax to use.

### Rule 3: Expected Outputs

For commands with significant output, show what success looks like.

**Example:**
```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation
```

**Expected output:**

```text
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

Document the key automation scripts in your repository here. Common patterns include scripts for build, test, lint, and quality checks.

### Prerequisites

List any prerequisites for running repository scripts. For example:

```bash
# Install project dependencies
npm install
# or
dotnet tool restore
# or
pip install -r requirements.txt
```

**Why:** Clear prerequisites help contributors run scripts successfully without trial and error.

### Key Scripts for Instruction Authors

Create a table documenting scripts that are frequently referenced in instruction files. Adjust paths to match your repository structure (e.g., `scripts/`, `eng/src/agent-scripts/`, `tools/`, etc.):

| Script | Purpose | Usage |
|--------|---------|-------|
| `build.sh` | Build the project | `./eng/src/agent-scripts/build.sh` |
| `test.sh` | Run all tests | `./eng/src/agent-scripts/test.sh` |
| `lint.sh` | Run linters and code quality checks | `./eng/src/agent-scripts/lint.sh` |

**Why:** Documenting scripts here provides a quick reference for instruction authors and ensures consistency across instruction files.

### Verifying Script Commands

Before referencing a script in an instruction file:

1. Open the script to confirm its parameters and behavior
2. Run the script in a test environment
3. Document the exact command and expected output
4. Include the "Drift check" note in your Quick-Start section

**Why:** Scripts evolve. The drift check note reminds readers to verify behavior rather than blindly trust documentation.

### Running Local Checks

Document how to run local quality checks before committing. Examples:

**Check Markdown files:**

```bash
# If using markdownlint:
npx markdownlint-cli2 "**/*.md"

# If using another linter (adjust path to your repository structure):
./eng/src/agent-scripts/lint-markdown.sh
```

**Expected success:**

- Exit code `0`
- No errors or warnings reported

**On failure:**

- Fix reported violations in your files
- Re-run until clean
- Avoid suppressing rules globally

**Why:** Consistent linting prevents style drift. Local checks give fast feedback before pushing.

## Cross-Referencing Rules

### Rule 1: One Authoritative Source

Each topic should have exactly one authoritative instruction file. Other files may link to it but should not duplicate its content.

**Example:** If you have `logging.instructions.md` for logging patterns, other instruction files should reference it rather than re-explaining logging.

**Why:** Single source of truth prevents inconsistencies and simplifies maintenance. When the authoritative file updates, all links remain valid.

### Rule 2: Use Relative Links

Link to other instruction files using relative paths.

**Example:**

```markdown
See [Logging Guidelines](./logging.instructions.md) for logging best practices.
```

**Why:** Relative links work in all contexts (local, web, forks) and don't break if the repository URL changes.

### Rule 3: Link with Anchors for Specific Sections

When referencing a specific section, use anchor links.

**Example:**

```markdown
Follow the [unit testing guidelines](./testing.instructions.md#unit-tests) for writing unit tests.
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

```text
Cosmos DB connection: AccountEndpoint=https://[REDACTED].documents.azure.com;...
```

**Why:** Examples should look realistic without exposing actual credentials.

## Versioning and Change Control

### Rule 1: Review Requirements

All changes to instruction files should require review per your repository's review policy (e.g., `.github/CODEOWNERS` or branch protection rules).

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

**Last verified:** YYYY-MM-DD
**Default branch:** [your-default-branch]
```

**Why:** Dates help readers assess staleness. Files last verified years ago likely need review.

### Rule 4: Link to External Resources by Permanent URL

When linking to external documentation, use permanent URLs (avoid version-specific links unless necessary).

**Good:** `https://learn.microsoft.com/dotnet/core/`
**Bad:** `https://docs.microsoft.com/en-us/dotnet/core/` (old domain)

**Why:** Permanent URLs reduce broken links over time.

## Examples from This Repository

As you create instruction files in your repository, document good examples here to help future contributors understand effective patterns.

### Good Pattern: Build Instructions

A well-structured build instruction file should include:

- Clear "At-a-Glance Quick-Start" with the most common commands
- Step-by-step procedures for different build scenarios
- Code examples showing actual commands from your repository
- Cross-references to related instruction files

### Good Pattern: Testing Instructions

A comprehensive testing instruction file should include:

- Layered content (different test types or levels)
- Detailed subsections for each test category
- References to external resources when helpful
- Related Guidelines section linking to other instruction files

### Good Pattern: Tool-Specific Instructions

Tool or framework-specific instruction files should:

- Provide prescriptive guidance with clear rules
- Include quality enforcement sections with validation commands
- Use note blocks to highlight repository-specific configuration

### Pattern to Avoid: Overly Generic Instructions

Avoid instruction files that:

- Duplicate content from official documentation without adding value
- Mix multiple unrelated topics in a single file
- Lack concrete, repository-specific examples

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
- [ ] Footer includes "Last verified: YYYY-MM-DD" and default branch name
- [ ] No secrets or credentials in examples
- [ ] PR description includes "Docs impact" section
- [ ] Ran any applicable sync or generation scripts after changes

## Known Discrepancies

This section documents conflicts or inconsistencies discovered across instruction files during fact-checking. Empty if no discrepancies exist.

| File | Issue | Impact | Proposed Fix |
|------|-------|--------|--------------|
| *(none found)* | — | — | — |

**Note:** Discrepancies are resolved through focused updates to the conflicting files, not by changing this guide. This section exists to track issues until resolution.

## Glossary

Define repository-specific terms here as your instruction file collection grows. Common entries might include:

**Instruction file:** A Markdown file in `.github/instructions/` ending with `.instructions.md` that defines development practices, patterns, and workflows.

**Drift check:** A reminder note (typically in Quick-Start sections) to verify that script behavior matches documentation, acknowledging that scripts may evolve.

**At-a-Glance Quick-Start:** The opening section of an instruction file that provides immediate value with the most common commands or actions.

**Why lines:** Brief explanations following significant rules that explain the rationale, helping readers understand the reasoning behind practices.

Add additional terms specific to your repository, technology stack, and development practices as needed.

---

## Note for AI Agents

This instruction file is intentionally generic to support multiple repositories. When using this guide:

- **Consult your repository's actual structure** for authoritative file and folder locations
- **Follow existing patterns** in other instruction files already present in your repository
- **Adapt paths and examples** to match your repository's conventions (e.g., `scripts/`, `eng/src/agent-scripts/`, `tools/`, etc.)
- **Reference actual commands** from your repository's automation scripts, build systems, and tooling
- **Align with other instruction files** to maintain consistency within the repository

The guidance in this file provides a foundation, but the specific implementation details should always match the repository where it's being used.

---

**Last verified:** 2025-01-04
**Default branch:** main
