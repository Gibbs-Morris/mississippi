---
applyTo: '**'
---

# Agent Scratchpad

Governing thought: `.scratchpad/` is an ephemeral, untracked workspace for task handoff—never store secrets or couple code to it.

> Drift check: If instructions change, sync Cursor rules via `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1`; scripts stay authoritative.

## Rules (RFC 2119)

- Secrets/PII **MUST NOT** live in `.scratchpad/`, and source/tests **MUST NOT** reference `.scratchpad/` paths. Why: Scratchpad is ephemeral and ignored by Git.
- Tasks **MUST** be claimed via atomic move `tasks/pending -> tasks/claimed`; only the owner **MUST** edit claimed files; timestamps **MUST** be UTC ISO-8601. Why: Prevents races and ambiguity.
- Each task file **MUST** follow `<yyyyMMddHHmmss>_<slug>_<ulid>.json`; producers **SHOULD** slice work into ~15-minute tasks; workers **SHOULD** sort by priority then FIFO. Why: Enables deterministic ownership.
- Attempts per task **MUST** cap at five before moving to `tasks/deferred/` with context. Why: Matches remediation policy.
- Files **SHOULD** stay small/text; large binaries **SHOULD NOT** be stored. Why: Keeps diffs manageable.
- Agents **MAY** prune old runs/done tasks anytime. Why: Scratchpad is disposable.

## Scope and Audience

All agents coordinating work locally.

## At-a-Glance Quick-Start

- Claim work: move JSON from `tasks/pending` to `tasks/claimed`, stamp `claimedBy/claimedAt/attempts`.
- Complete: update status/result and move to `tasks/done`; defer after five attempts with reason/next steps.
- Naming: `<yyyyMMddHHmmss>_<slug>_<ulid>.json`; keep JSON keys stable and timestamps UTC.

## Core Principles

- One file = one task; ownership is folder-based.
- Atomic moves prevent conflicts; no shared mutable files.
- Scratchpad is temporary—never part of builds or code.

## References

- Build remediation attempt cap: `.github/instructions/build-issue-remediation.instructions.md`
