# Server-Side Saga Orchestration Plan

Status: Plan ready for approval

## Index

- [learned.md](learned.md)
- [rfc.md](rfc.md)
- [verification.md](verification.md)
- [implementation-plan.md](implementation-plan.md)
- [progress.md](progress.md)

## Task Size

- Size: Large
- Approval checkpoint: Yes (new public APIs and cross-cutting generator/runtime changes)

## Summary

Plan and design review for implementing server-side saga orchestration in Mississippi, based on [task.md](../../task.md), with emphasis on reuse of aggregate infrastructure, attribute/type-based discovery (no namespace conventions), and event-sourced state transitions.