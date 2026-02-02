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

## Verification Questions

1. Where are the aggregate controller and DTO generators implemented, and what patterns do they follow (inputs, routing, DI, logging)?
2. Where are aggregate client action/effect/state/reducer generators implemented, and how do they register features?
3. Where is aggregate silo registration generation implemented, and how does it discover reducers and event types?
4. What is the current aggregate orchestration runtime (command handlers, effects, root handlers) and how does it execute effects?
5. What is the existing mechanism for projections and SignalR updates (types, registration, DTOs)?
6. What existing abstractions in `*.Abstractions` projects align with saga needs (command handler base, reducer base, grain factory, etc.)?
7. What logging patterns are used in aggregate orchestration (LoggerExtensions classes and method coverage)?
8. What test projects cover generators and infrastructure, and what patterns do those tests follow?
9. How are storage naming attributes and serializer aliases applied to aggregate state/events, and are there reusable patterns for saga equivalents?
10. Are there existing orchestration or workflow constructs in the repo that should be reused or avoided?