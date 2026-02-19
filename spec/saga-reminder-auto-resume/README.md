# Aggregate Scheduled Commands Spec

> **Folder naming note:** The folder slug `saga-reminder-auto-resume` reflects the original motivation (saga auto-resume via reminders). The scope expanded to a generic aggregate scheduling feature during design. Phase 1 is aggregate-level scheduling; phase 2 applies it to saga auto-resume. The folder name is retained for continuity.

## Status

Draft

## Task Size

Large

## Approval Checkpoint

Yes

Rationale: this introduces aggregate-level scheduling contracts, reminder runtime behavior, and attribute-driven developer experience across the framework.

## Goal

Design a general aggregate feature for scheduling command execution via Orleans reminders, then apply that feature to saga auto-resume in phase 2.

## Scope

- Phase 1: add aggregate-level scheduled command capability with attribute-first developer experience.
- Phase 1: support reminder policy (initial delay, interval, jitter, backoff, max attempts) and command binding.
- Phase 1: support multiple reminders per aggregate instance via unique schedule names.
- Phase 1: no reminder auto-start; provide explicit API/command to start schedules.
- Phase 1: provide lifecycle API to start/update/cancel reminders and invoke mapped commands safely.
- Phase 1: add observability and idempotency guidance/enforcement for scheduled command handlers.
- Phase 2: implement saga auto-resume using the phase 1 scheduling feature.

## Documents

- Learned facts: [learned.md](learned.md)
- RFC: [rfc.md](rfc.md)
- Verification: [verification.md](verification.md)
- Implementation plan: [implementation-plan.md](implementation-plan.md)
- Code samples: [code-samples.md](code-samples.md)
- Grain interfaces: [grain-interfaces.md](grain-interfaces.md)
- Progress log: [progress.md](progress.md)

## Out of Scope

- New UI features.
- Non-Orleans schedulers/workflow engines.
- Cross-repo orchestration components.
