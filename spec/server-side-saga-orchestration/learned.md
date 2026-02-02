# Learned

> **Status**: Updated to reflect the `SagaOrchestrationEffect<TSaga>` design.
> See [updated-design.md](./updated-design.md) for the authoritative design reference.

## Repository Facts (verified)

- Aggregate controller generation is implemented in [src/Inlet.Server.Generators/AggregateControllerGenerator.cs](../../src/Inlet.Server.Generators/AggregateControllerGenerator.cs) and scans for `[GenerateAggregateEndpoints]` aggregates plus `[GenerateCommand]` commands located in a `Commands` sub-namespace.
- Server DTOs and mappers for commands are generated in [src/Inlet.Server.Generators/CommandServerDtoGenerator.cs](../../src/Inlet.Server.Generators/CommandServerDtoGenerator.cs) with per-aggregate mapper registrations.
- Client command generators exist for actions, action effects, reducers, feature state, and registrations in [src/Inlet.Client.Generators](../../src/Inlet.Client.Generators).
- Aggregate silo registrations are generated in [src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs) and currently discover handlers/reducers/effects via `Handlers`, `Reducers`, and `Effects` sub-namespaces.
- Aggregate runtime orchestration uses `GenericAggregateGrain<T>` in [src/EventSourcing.Aggregates/GenericAggregateGrain.cs](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs) composed with `IRootCommandHandler<T>`, `IRootReducer<T>`, and `IRootEventEffect<T>` implementations.
- Root-level command, reducer, and effect dispatchers are implemented in [src/EventSourcing.Aggregates/RootCommandHandler.cs](../../src/EventSourcing.Aggregates/RootCommandHandler.cs), [src/EventSourcing.Reducers/RootReducer.cs](../../src/EventSourcing.Reducers/RootReducer.cs), and [src/EventSourcing.Aggregates/RootEventEffect.cs](../../src/EventSourcing.Aggregates/RootEventEffect.cs).
- Projection endpoints are generated in [src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs](../../src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs) and served via `UxProjectionControllerBase` in [src/EventSourcing.UxProjections.Api/UxProjectionControllerBase{TProjection,TDto}.cs](../../src/EventSourcing.UxProjections.Api/UxProjectionControllerBase%7BTProjection,TDto%7D.cs).
- Client-side projection updates are managed through SignalR in [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs](../../src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) and stored in [src/Inlet.Client.Abstractions/State/ProjectionsFeatureState.cs](../../src/Inlet.Client.Abstractions/State/ProjectionsFeatureState.cs).
- Aggregate state uses storage/serialization attributes (example: [samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs](../../samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs)).
- `EventEffectBase<TEvent, TState>` in [src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs](../../src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs) provides the base for effects triggered by events.
- `RootEventEffect<TState>` in [src/EventSourcing.Aggregates/RootEventEffect.cs](../../src/EventSourcing.Aggregates/RootEventEffect.cs) dispatches to typed effects; `SagaOrchestrationEffect<TSaga>` will follow this pattern.

## Requirements Captured (from task.md)

- Sagas are aggregates; reuse aggregate infrastructure and patterns.
- Discovery MUST use attributes/types (no namespace conventions).
- State must be records; state changes only via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.

## Design Decisions (from gap analysis and discussion)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Step implementation | POCO with `[SagaStep(Order, typeof(TSaga))]` implementing `ISagaStep<TSaga>` | Cleaner than base classes; attribute links step to saga type |
| Compensation | `ICompensatable<TSaga>` interface on step class | Keeps compensation colocated with step; no separate classes |
| Orchestration | Single `SagaOrchestrationEffect<TSaga>` | One class handles dispatch, compensation, completion |
| Data passing | IDs only + transient external data | Steps query aggregates for system data; reduces state bloat |
| ISagaState | Properties only—no Apply methods | Maintains events→reducers principle |
| Discovery | Attribute-based only | Aligns with task.md requirement; no namespace conventions |
| Infrastructure events | `SagaStartedEvent`, `SagaStepCompleted`, `SagaStepFailed`, `SagaCompensating`, `SagaStepCompensated`, `SagaCompleted`, `SagaCompensated`, `SagaFailed` | Compact event set for lifecycle transitions |
| Result types | `StepResult` + `CompensationResult` with success flags and error codes/messages | Simple, composable step outcomes |

## Evidence Collected (gap analysis)

- **Gap 1**: Initial draft contradicted task.md by mentioning namespace discovery → Resolved: attribute-based only.
- **Gap 2**: ISagaState had Apply methods → Resolved: removed; pure reducers only.
- **Gap 3**: Saga input handling underspecified → Resolved: input stored as IDs in state; step 0 extracts and stores.
- **Gap 4**: Effect orchestration chain unclear → Resolved: single `SagaOrchestrationEffect<TSaga>`.

## Risks/Constraints

- Must not use namespace conventions for discovery; use attributes/types only.
- State must be records; all state changes via events and reducers.
- ISagaState must have properties only—no Apply methods.