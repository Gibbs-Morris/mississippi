# Verification

## Claim List (initial)

1. Saga orchestration can reuse existing aggregate grain infrastructure without introducing parallel hosting.
2. Saga state can be defined as records implementing a new `ISagaState` interface without breaking existing serialization patterns.
3. Step/compensation discovery can be performed via attributes/types (not namespaces) using existing generator infrastructure patterns.
4. Saga lifecycle transitions can be expressed entirely via events and reducers, with no direct state mutation in steps/effects.
5. Server, client, and silo source generators can be adapted from aggregate equivalents with minimal divergence.
6. Saga status updates can reuse existing SignalR projection mechanisms without new infrastructure.
7. Adding saga abstractions in `*.Abstractions` projects and implementations in main projects preserves layering rules.
8. Proposed changes are additive and do not break existing public APIs.
9. Tests can follow existing L0/L2 patterns for generators and runtime orchestration.

## Verification Questions (TBD)

- Pending.