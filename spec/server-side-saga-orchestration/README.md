# Server-Side Saga Orchestration Plan

Status: **Design Complete - Awaiting Approval**

## Index

- [**rfc.md**](rfc.md) ← **Authoritative design reference**
- [learned.md](learned.md)
- [verification.md](verification.md)
- [implementation-plan.md](implementation-plan.md)
- [progress.md](progress.md)

## Task Size

- Size: Large
- Approval checkpoint: Yes (new public APIs and cross-cutting generator/runtime changes)

## Current Status

**All gaps from the first draft have been resolved.** The design now uses:

| Decision | Choice |
|----------|--------|
| Step implementation | POCO with `[SagaStep(Order, typeof(TSaga))]` implementing `ISagaStep<TSaga>` |
| Compensation | `ICompensatable<TSaga>` interface on step class |
| Orchestration | Single `SagaOrchestrationEffect<TSaga>` |
| Data passing | IDs only + transient external data; query aggregates for system data |
| ISagaState | Properties only—no Apply methods |
| Discovery | Attribute-based only (no namespace conventions) |

## Gap Resolution Summary

| Gap | Status | Resolution |
|-----|--------|------------|
| #1 ISagaState Apply methods | ✅ Resolved | Removed; pure reducers only |
| #2 Saga input handling | ✅ Resolved | IDs stored in state; steps query aggregates |
| #3 Step discovery association | ✅ Resolved | `[SagaStep(Order, typeof(TSaga))]` attribute |
| #4 Infrastructure reducers | ✅ Resolved | Generic reducers provided by framework |
| #5 Effect orchestration | ✅ Resolved | Single `SagaOrchestrationEffect<TSaga>` |
| #6 SignalR projection key | ✅ Resolved | `saga/{sagaTypeName}/{sagaId}` |
| #7 Step registry timing | ✅ Resolved | Generated step metadata via `AddSagaStepInfo` registration |
| #8 Boilerplate | ✅ Resolved | POCO steps reduce ceremony significantly |

## Summary

Plan and design review for implementing server-side saga orchestration in Mississippi, based on [task.md](../../task.md), with emphasis on reuse of aggregate infrastructure, attribute/type-based discovery (no namespace conventions), and event-sourced state transitions.