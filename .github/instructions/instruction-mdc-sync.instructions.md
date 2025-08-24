---
applyTo: '**'
---

# Instruction ↔ Cursor MDC Sync Policy

Keep our written instructions and Cursor AI rule files in lockstep. Any change to one must be reflected in the other in the same change set.

## At-a-Glance Quick-Start

- Edit the instruction Markdown first (canonical source).
- Mirror the change into the corresponding `.mdc` file(s) so meaning matches.
- Add sync metadata to both Markdown and `.mdc` files.
- Commit Markdown and `.mdc` changes together in the same PR/commit.
- Prefer using the sync helper script to reduce human error.

### Quick Command
```powershell
pwsh ./scripts/sync-instructions-to-mdc.ps1
```

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
3) Record sync metadata in both places (see “Sync Metadata” below).
4) Commit both changes together in the same PR/commit.

Preferred automated flow: run the repository helper script which mirrors instruction Markdown into Cursor `.mdc` files and inserts the required sync metadata. This reduces human error and keeps sync metadata consistent:

```pwsh
pwsh ./scripts/sync-instructions-to-mdc.ps1
```

If you cannot run the script, follow the manual steps above and add sync metadata by hand.

When you change a Cursor `.mdc` rule file:

1) Update the corresponding instruction Markdown to match the intended meaning.
2) Keep wording clear for humans in the Markdown; keep it concise/operational in `.mdc`.
3) Record sync metadata in both places.
4) Commit both changes together in the same PR/commit.

## Sync Metadata (lightweight, manual)

Add a short, machine-friendly header or footer in both files whenever a sync occurs:

- In the instruction Markdown (at top or bottom):
  <!-- sync: mirrors → MDC:.cursor/rules/instruction-mdc-sync.mdc ; synced: 2025-08-24 ; commit: {short-sha} -->

- In the `.mdc` file (as a top comment if supported, or a header line):
  // sync: source ← MD:.github/instructions/instruction-mdc-sync.instructions.md ; synced: 2025-08-24 ; commit: {short-sha}

Notes:

- Use the current date (UTC) and the short commit SHA you’re creating.
- If multiple `.mdc` files mirror the same instruction, list them all in the instruction file.
- If a rule block is intentionally `.mdc`-only or Markdown-only, explicitly mark it with `sync: excluded` and a brief justification.

## Mapping and Parity

- Maintain a clear section-to-section mapping between Markdown headers and `.mdc` rule blocks (matching titles or explicit labels).
- Keep semantics identical. Formatting may be adapted, but rules/constraints must not diverge.
- On removals/renames, apply the same operation in both files and update mapping labels/comments.

## PR Checklist (must-pass)

- [ ] Changed instruction Markdown and matching `.mdc` updated in the same commit/PR
- [ ] Sync metadata updated in both files (date + short SHA + counterpart reference)
- [ ] Semantics parity verified (no rules exist in one place without a justified `sync: excluded` note)

## Quick Validation Tips

- Diff review: compare the changed sections side-by-side (Markdown ↔ `.mdc`) to confirm parity.
- Grep for labels or headers to ensure every rule block has a counterpart.
- Prefer small, focused commits to make parity obvious in review.

- Quick check: you can run the sync helper to validate parity before opening a PR.


