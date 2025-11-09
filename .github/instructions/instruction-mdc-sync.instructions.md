---
applyTo: ".github/instructions/**/*,.cursor/rules/**/*"
---

# Instruction ↔ Cursor Sync

## Scope
Keep instruction `.md` and Cursor `.mdc` files in sync. Run `sync-instructions-to-mdc.ps1` after instruction changes.

## Core Principles
Instruction `.md` is canonical. `.mdc` mirrors semantics, may differ in format. Commit both together.

## Anti-Patterns
❌ Changing `.mdc` without `.md`. ❌ Committing only one. ❌ Skipping sync script.

## Enforcement
PR reviews verify both changed together. Script run before merge.
