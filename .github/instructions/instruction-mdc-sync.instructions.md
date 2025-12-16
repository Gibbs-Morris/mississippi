---
applyTo: '**'
---

# Instruction ↔ Cursor MDC Sync Policy

Governing thought: Markdown in `.github/instructions/` is canonical; `.cursor/rules/*.mdc` must stay semantically identical in the same commit.

## Rules (RFC 2119)

- Authors **MUST** edit instruction Markdown first, then mirror the change into the matching `.mdc`; both **MUST** be committed together with semantic parity.
- Section mapping between Markdown and `.mdc` **MUST** remain clear; removals/renames **MUST** be applied to both files.
- Authors **SHOULD** use `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` to reduce errors.
- If parity cannot be completed immediately, authors **SHOULD** open a focused `.scratchpad/tasks/pending` item detailing the sections to sync.

## Quick Start

- Edit Markdown, run `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1`, verify parity, commit both files together.

## Review Checklist

- [ ] Markdown and `.mdc` updated together with matching meaning.
- [ ] Mapping between sections is obvious; removals/renames applied in both.
- [ ] Sync helper used or equivalent parity verified; outstanding sync work tracked if deferred.
