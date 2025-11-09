---
applyTo: ".scratchpad/**/*"
---

# Agent Scratchpad Workflow

## Scope
Ephemeral task coordination in `.scratchpad/`. Not source/tests. Untracked by Git except `.scratchpad/.gitignore`.

## Quick-Start
File-per-task pattern. Lifecycle: `tasks/pending` → `tasks/claimed` → `tasks/done|deferred`. Atomic move to claim. JSON schema with stable keys.

## Core Principles
One file = one task. Ownership via folder path. Claim by atomic move. ULID naming: `yyyyMMddTHHmmss_slug_ulid.json`. Status: `pending`, `claimed`, `done`, `deferred`, `cancelled`, `superseded`. Priority: `P0` (urgent), `P1`, `P2`, `P3`. Max 5 attempts before defer.

## Schema
```json
{"schemaVersion":"1.0","id":"ulid","title":"Task","createdAt":"ISO","priority":"P2","status":"pending","claimedBy":null,"attempts":0}
```

## Helpers
Scripts in `eng/src/agent-scripts/tasks/`: `new-scratchpad-task.ps1`, `claim-scratchpad-task.ps1`, `complete-scratchpad-task.ps1`, `defer-scratchpad-task.ps1`.

## Anti-Patterns
❌ Secrets. ❌ References from source. ❌ Large files. ❌ Editing claimed tasks by non-owner.

## Enforcement
Never commit `.scratchpad/` content. Use helpers for consistency. Keep tasks small (≤15 min).
