# Server-Side Saga Orchestration Plan

Status: **Gap Analysis Complete - Awaiting Design Decisions**

## Index

- [learned.md](learned.md)
- [rfc.md](rfc.md)
- [verification.md](verification.md)
- [implementation-plan.md](implementation-plan.md)
- [progress.md](progress.md)
- [**gap-analysis.md**](gap-analysis.md) ← **NEW: Critical findings**

## Task Size

- Size: Large
- Approval checkpoint: Yes (new public APIs and cross-cutting generator/runtime changes)

## Current Status

**The first draft has critical gaps that must be resolved before implementation.**

See [gap-analysis.md](gap-analysis.md) for detailed findings including:

| Gap | Severity | Issue |
|-----|----------|-------|
| #1 | **Blocking** | `ISagaState` has mutable Apply methods that violate events→reducers principle |
| #2 | **Blocking** | Saga input handling is underspecified (where stored, how accessed) |
| #3 | **Blocking** | Step discovery association unclear (how steps link to sagas) |
| #4 | Medium | Infrastructure reducer implementation approach unclear |
| #5 | **Blocking** | Effect orchestration chain not fully specified |
| #6 | Medium | SignalR projection key format undefined |
| #7 | Medium | Step registry runtime vs generation unclear |
| #8 | DX | Significant boilerplate for simple sagas |

## Key Contradiction Found

**task.md explicitly forbids namespace-based discovery**, but the implementation plan still references namespace scanning for steps/compensations. This must be reconciled.

## Summary

Plan and design review for implementing server-side saga orchestration in Mississippi, based on [task.md](../../task.md), with emphasis on reuse of aggregate infrastructure, attribute/type-based discovery (no namespace conventions), and event-sourced state transitions.