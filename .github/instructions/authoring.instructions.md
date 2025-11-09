---
applyTo: '**/*.instructions.md'
---

# Instruction Authoring Guide

Governing thought: Every `*.instructions.md` file follows the Minto Pyramid. Put one consolidated RFC 2119 rules list near the top. Put examples near the end.

## Normative Rules (RFC 2119)

The keywords **MUST**, **SHOULD**, **MAY**, **MUST NOT**, and **SHOULD NOT** are as defined in RFC 2119.

### Structure and Minto Pyramid
- Files **MUST** follow the Minto Pyramid: start with the governing thought (answer), then key reasons, then evidence and examples.
- Each file **MUST** include a single consolidated **Rules** section near the top that contains all RFC 2119 statements for that file.
- Examples and long-form evidence **MUST** appear later in the document under dedicated sections (e.g., “Examples,” “References,” “Anti-Patterns”).
- Each file **MUST** include YAML front matter with an `applyTo` directive.
- Each file **MUST** include an H1 title that names the topic precisely.
- Each file **SHOULD** include an “At-a-Glance Quick-Start” for common commands.
- Each file **MUST** include a brief **Drift check** note near the top reminding readers that scripts remain authoritative.
- New instruction files **MUST** start from the [Standard Section Template](#standard-section-template-minto-aligned) in this document.
- When refactoring an existing instruction file, authors **MUST** migrate it to the [Standard Section Template](#standard-section-template-minto-aligned), preserving content while aligning structure.
- All edits to instruction files **MUST** keep the file aligned to the template (RFC 2119 rules centralized in the Rules section, Drift check near the top, section order intact).
- Every bullet in this Rules section **MUST** include at least one RFC 2119 keyword (MUST, MUST NOT, SHOULD, SHOULD NOT, MAY) to ensure clarity.

### Naming and Placement
- File names **MUST** be lowercase kebab-case with the `.instructions.md` suffix. Pattern: `<topic>.instructions.md`.
- Files **MUST** live in `.github/instructions/`.
- One cohesive topic per file **SHOULD** be maintained. Authors **SHOULD** split broad topics into focused files.

### Headings and Style
- Authors **SHOULD** write short, active sentences. Target 15–20 words per sentence.
- Numbered procedures **MUST** contain one action per step. Closely related sub-actions **MAY** use substeps.
- Heading levels **SHOULD** be consistent: H2 for major sections, H3 for subsections, H4 for sub-subsections; authors **MUST NOT** skip levels.
- US English spelling **MUST** be used throughout (e.g., “color,” “analyzer,” “organize”).
- After significant rules or recommendations, authors **MUST** include a brief “Why” line that states the rationale.

### Linking and Cross-References
- Internal references **MUST** use relative links and **MUST** be validated to resolve before commit.
- When linking to a specific target within a document, anchors **SHOULD** be used.
- Each topic **SHOULD** have one authoritative instruction file; related files **SHOULD** link to it rather than duplicate content.
- External documentation links **SHOULD** use permanent, stable URLs. Version-specific links **MAY** be used only when required.

### Scripts, Commands, and Outputs
- Command examples **MUST** reference real scripts and tools that exist in the repository. No placeholders.
- When commands differ by OS, authors **SHOULD** provide both PowerShell and Bash blocks.
- For commands with significant output, authors **SHOULD** show an expected success snippet.
- For scripts used in CI, expected exit codes **SHOULD** be documented.
- When examples create side effects, cleanup instructions **SHOULD** be included.
- Before referencing a script, authors **MUST** open and review the script, **MUST** run it in a test environment, and **MUST** document the exact command and expected outcome. The **Drift check** note **MUST** be included.

### Prerequisites and Local Checks
- Prerequisites required to run repository scripts **SHOULD** be listed.
- Guidance for local linting and quality checks **SHOULD** be provided. Global rule suppression **SHOULD NOT** be encouraged.

### Security and Secrets
- Secrets, API keys, passwords, and tokens **MUST NOT** be committed or shown in examples.
- Examples that require credentials **SHOULD** use environment variables.
- If a secret store is used (e.g., Azure Key Vault, GitHub Secrets), the location and access pattern **SHOULD** be documented.
- Potentially sensitive example outputs **SHOULD** be redacted.

### Versioning and Change Control
- Changes to instruction files **MUST** undergo review per repository policy (e.g., CODEOWNERS, branch protection).
- Pull requests that modify instruction files **MUST** include a “Docs impact” section describing effects and cross-file implications.
- A footer **MUST** record `Last verified: YYYY-MM-DD` and the default branch name.

---

## Purpose

This guide defines how to author and maintain `*.instructions.md` so they are consistent, machine-consumable, and useful to humans and AI agents. It specifies structure, placement, linking, validation, and change control.

**Why:** A single predictable format increases discoverability and reduces drift across teams and tools.

## Scope and Audience

**Audience:** Contributors who create, update, or maintain `*.instructions.md` in `.github/instructions/`.

**In scope:** Naming, placement, structure, linking, validation, and reviews.

**Out of scope:** Topic-specific technical content, general Markdown basics, or repository-wide contribution policy.

**Why:** This guide standardizes the wrapper and workflow so topic files can focus on content.

## Why This Format (Minto → Rules → Evidence)

- **Answer first:** The governing thought and consolidated rules let readers act immediately.
- **Reason next:** Short rationale lines explain trade-offs and help reviewers spot when rules no longer fit.
- **Evidence last:** Examples, procedures, and references live near the end for verification and learning.

**Why:** This mirrors how engineers read under time pressure and how CI agents parse documents.

## Standard Section Template (Minto-Aligned)

> Copy this template when creating a new `*.instructions.md`. Also use this template when refactoring an existing instruction file by migrating its content to match this structure. Align all future edits to keep the file conformant. Keep RFC 2119 statements only in the **Rules** section.

```markdown
---
applyTo: '**'
---

# [Topic] Instructions

Governing thought: [One-sentence answer that sets the intent and outcomes.]

## Rules (RFC 2119)

- All normative rule bullets in this section **MUST** use at least one RFC 2119 keyword (MUST, MUST NOT, SHOULD, SHOULD NOT, MAY) and be grouped by theme.
- RFC 2119 keywords **SHOULD NOT** appear outside this Rules section except when quoting standards or in clearly explanatory, non‑normative prose; avoid introducing new normative statements elsewhere.

## Scope and Audience

[Who this applies to and where it applies.]

## At-a-Glance Quick-Start

- [Most common command or action]
- [Second most common]
- [Third most common]

> **Drift check:** Scripts are the source of truth. Open referenced scripts and confirm behavior before use.

## Core Principles and Rationale

- **[Principle]** — [Short reason]
- **[Principle]** — [Short reason]

## Procedures

### [Procedure A]

1. [One action]
2. [One action]
   - a) [Optional substep]

**Why:** [Short rationale]

### [Procedure B]

[Steps]

**Why:** [Short rationale]

## Examples

[2–4 concrete examples that run in this repository.]

## Anti-Patterns

- ❌ [Bad practice] → ✅ [Better alternative]

## External References

- [External doc]: https://learn.microsoft.com/dotnet/core/
```

