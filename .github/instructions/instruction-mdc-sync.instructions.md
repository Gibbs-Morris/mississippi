---
applyTo: '**'
---

# Instruction ↔ Cursor MDC Sync

Governing thought: Markdown instructions are canonical; Cursor `.mdc` files must stay semantically identical and be updated together.

> Drift check: Open `eng/src/agent-scripts/sync-instructions-to-mdc.ps1` before use; the script is authoritative for mapping and output.

## Rules (RFC 2119)

- Authors **MUST** edit instruction Markdown first and **MUST** commit matching `.mdc` updates in the same PR/commit. Why: Prevents drift.
- Markdown and `.mdc` content **MUST** stay semantically equivalent even if phrasing differs; section mapping **MUST** remain clear. Why: Keeps human and AI guidance aligned.
- Removals/renames **MUST** apply to both Markdown and `.mdc`, updating labels/comments as needed. Why: Avoids orphaned rules.
- Authors **SHOULD** use `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` to reduce human error. Why: Automates parity.
- If parity cannot be completed immediately, a focused `.scratchpad/tasks/pending` item **SHOULD** be created. Why: Tracks outstanding sync work.

## Scope and Audience

Instruction and Cursor rule maintainers.

## At-a-Glance Quick-Start

- Edit Markdown → run sync script → commit Markdown + `.mdc` together.
- Keep semantics identical; update mappings when sections change.
- Open a scratchpad task if you cannot finish the sync immediately.

## Core Principles

- One canonical source (Markdown) with mirrored AI rules.
- Small, synchronized changes make parity obvious in review.

## References

- Authoring: `.github/instructions/authoring.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
