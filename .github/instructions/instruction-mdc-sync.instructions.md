---
applyTo: '**'
---

# Instruction ↔ Cursor MDC Sync Policy

Keep our written instructions and Cursor AI rule files in lockstep. Any change to one must be reflected in the other in the same change set.

## At-a-Glance Quick-Start

- Edit the instruction Markdown first (canonical source).
- Mirror the change into the corresponding `.mdc` file(s) so meaning matches.
- Commit Markdown and `.mdc` changes together in the same PR/commit.
- Prefer using the sync helper script to reduce human error.

### Quick Command
```powershell
pwsh ./scripts/sync-instructions-to-mdc.ps1
```

> **Drift check:** Before running any PowerShell script referenced here, open the script in `scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.

## Scope

- Instruction files: all guidance under `.github/instructions/` (this repository’s source of truth)
- Cursor rule files: any `.mdc` file(s) used by Cursor for rules in this repository

## Canonical Source

- The instruction Markdown in `.github/instructions/` is canonical.
- Cursor `.mdc` files are mirrored derivatives tailored for Cursor consumption (format/phrasing may differ, semantics must not).

## Required Workflow

When you change an instruction file:

1) Edit the instruction Markdown first (canonical).
2) Mirror the change into the corresponding `.mdc` rule file(s) so the meaning matches.
3) Commit both changes together in the same PR/commit.

Preferred automated flow: run the repository helper script which mirrors instruction Markdown into Cursor `.mdc` files. This reduces human error and keeps parity consistent:

```pwsh
pwsh ./scripts/sync-instructions-to-mdc.ps1
```

When you change a Cursor `.mdc` rule file:

1) Update the corresponding instruction Markdown to match the intended meaning.
2) Keep wording clear for humans in the Markdown; keep it concise/operational in `.mdc`.
3) Commit both changes together in the same PR/commit.

## Mapping and Parity

- Maintain a clear section-to-section mapping between Markdown headers and `.mdc` rule blocks (matching titles or explicit labels).
- Keep semantics identical. Formatting may be adapted, but rules/constraints must not diverge.
- On removals/renames, apply the same operation in both files and update mapping labels/comments.

## PR Checklist (must-pass)

- [ ] Changed instruction Markdown and matching `.mdc` updated in the same commit/PR
- [ ] Semantics parity verified (no rules exist in one place without an intentional exception noted in the PR description)

- If parity cannot be completed immediately, create a focused task in `.scratchpad/tasks/pending` with the specific Markdown ↔ `.mdc` sections to sync and a short due date. See `.github/instructions/agent-scratchpad.instructions.md`.

## Quick Validation Tips

- Diff review: compare the changed sections side-by-side (Markdown ↔ `.mdc`) to confirm parity.
- Grep for labels or headers to ensure every rule block has a counterpart.
- Prefer small, focused commits to make parity obvious in review.

- Quick check: you can run the sync helper to validate parity before opening a PR.


