---
applyTo: '**'
---

# Instruction ↔ Cursor MDC Sync Policy

Governing thought: Instruction Markdown files are canonical; Cursor `.mdc` files are automatically synchronized derivatives that must maintain semantic parity in every commit.

## Rules (RFC 2119)

- Authors **MUST** edit instruction Markdown first before updating corresponding `.mdc` files.  
  Why: The instruction Markdown in `.github/instructions/` is the canonical source of truth.
- Authors **MUST** commit both instruction Markdown and matching `.mdc` changes together in the same PR/commit.  
  Why: Prevents drift between human-readable instructions and Cursor AI rules.
- Authors **MUST** maintain semantic parity between Markdown and `.mdc` content; formatting may differ but rules/constraints **MUST NOT** diverge.  
  Why: Ensures consistent behavior across human readers and AI agents.
- Authors **SHOULD** use the sync helper script `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` to reduce human error.  
  Why: Automated synchronization keeps parity consistent and reduces manual mistakes.
- Authors **MUST** maintain clear section-to-section mapping between Markdown headers and `.mdc` rule blocks.  
  Why: Enables reviewers to verify parity and trace content relationships.
- Authors **MUST** apply removals/renames to both files and update mapping labels/comments.  
  Why: Prevents orphaned or inconsistent rules across file types.
- Authors **SHOULD** create a scratchpad task in `.scratchpad/tasks/pending` if parity cannot be completed immediately.  
  Why: Tracks outstanding sync work and prevents long-term drift.

## Scope and Audience

**Audience:** Authors and reviewers of instruction files and Cursor AI rules.

**In scope:** Synchronization policy for `.github/instructions/*.instructions.md` and `.cursor/rules/*.mdc`.

**Out of scope:** Content authoring standards (see `authoring.instructions.md`), RFC 2119 keyword usage (see `rfc2119.instructions.md`).

## At-a-Glance Quick-Start

- Edit the instruction Markdown first (canonical source).
- Mirror the change into the corresponding `.mdc` file(s) so meaning matches.
- Commit Markdown and `.mdc` changes together in the same PR/commit.
- Prefer using the sync helper script to reduce human error.

### Quick Command

```powershell
pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1
```

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.



## Purpose

Keep written instructions and Cursor AI rule files in lockstep through automated synchronization.

## Core Principles

- Instruction Markdown is canonical; `.mdc` files are synchronized derivatives
- Cursor `.mdc` files are tailored for Cursor consumption (format/phrasing may differ, semantics must not)
- Automated sync reduces human error and maintains consistency

## Procedures

### Changing an instruction file

1. Edit the instruction Markdown first (canonical).
2. Mirror the change into the corresponding `.mdc` rule file(s) so the meaning matches.
3. Commit both changes together in the same PR/commit.

Preferred automated flow:

```pwsh
pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1
```

### Changing a Cursor `.mdc` rule file

1. Update the corresponding instruction Markdown to match the intended meaning.
2. Keep wording clear for humans in the Markdown; keep it concise/operational in `.mdc`.
3. Commit both changes together in the same PR/commit.






## PR Checklist (must-pass)

- [ ] Changed instruction Markdown and matching `.mdc` updated in the same commit/PR
- [ ] Semantics parity verified (no rules exist in one place without an intentional exception noted in the PR description)

- If parity cannot be completed immediately, create a focused task in `.scratchpad/tasks/pending` with the specific Markdown ↔ `.mdc` sections to sync and a short due date. See `.github/instructions/agent-scratchpad.instructions.md`.

## Quick Validation Tips

- Diff review: compare the changed sections side-by-side (Markdown ↔ `.mdc`) to confirm parity.
- Grep for labels or headers to ensure every rule block has a counterpart.
- Prefer small, focused commits to make parity obvious in review.

- Quick check: you can run the sync helper to validate parity before opening a PR.
