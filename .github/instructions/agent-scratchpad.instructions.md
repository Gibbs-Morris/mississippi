---
applyTo: '**'
---

# Agent Scratchpad: Local State Between Prompts

Governing thought: `.scratchpad/` is an ephemeral, git-ignored task board coordinated through atomic file moves.

## Rules (RFC 2119)

- `.scratchpad/` **MUST NOT** contain secrets/PII and **MUST NOT** be referenced from source, projects, or tests.
- Tasks **MUST** be claimed by atomic move `tasks/pending/ → tasks/claimed/`; only the owner edits claimed files. Timestamps **MUST** be UTC ISO-8601. Each task **MUST NOT** exceed five focused attempts before moving to `tasks/deferred/`.
- Task filenames **MUST** follow `<yyyyMMddHHmmss>_<slug>_<ulid>.json`; payloads **SHOULD** stay small/text-only; large binaries **SHOULD NOT** be stored.
- Workers **SHOULD** process by priority then FIFO; producers **SHOULD** slice work into ≤15-minute items. Agents **MAY** prune `runs/` or completed tasks anytime.
- Instruction Markdown changes **MUST** be mirrored to Cursor `.mdc` via `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` (Instruction ↔ Cursor MDC sync).

## Quick Start

- Layout: `tasks/pending|claimed|done|deferred`, optional `runs/` and `testing/` summaries.
- Claim by moving the JSON file, then update status/owner/timestamps; move to `done` or `deferred` with result/reason/next steps.
- Use helper scripts under `eng/src/agent-scripts/tasks/` to create/claim/complete/defer tasks consistently.

## Review Checklist

- [ ] No secrets or code references to `.scratchpad/`.
- [ ] Tasks follow naming/timestamp/attempt rules; ownership enforced via atomic moves.
- [ ] Deferred items recorded; instruction Markdown/`.mdc` kept in sync when edited.
