# Verification

> **Status**: Updated to reflect the `SagaOrchestrationEffect<TSaga>` design.
> See [rfc.md](./rfc.md) for the authoritative design reference.

## Claim List (revised after gap analysis)

1. Saga orchestration can reuse existing aggregate grain infrastructure without introducing parallel hosting.
2. Saga state can be defined as records implementing `ISagaState` (properties only—no Apply methods) without breaking existing serialization patterns.
3. Step discovery can be performed via `[SagaStep(Order, typeof(TSaga))]` attributes (not namespaces) using existing generator infrastructure patterns.
4. Saga lifecycle transitions can be expressed entirely via infrastructure events and reducers, with no direct state mutation in steps/effects.
5. Server, client, and silo source generators can be adapted from aggregate equivalents with minimal divergence.
6. Saga status updates can reuse existing SignalR projection mechanisms without new infrastructure.
7. Adding saga abstractions in `*.Abstractions` projects and implementations in main projects preserves layering rules.
8. Proposed changes are additive and do not break existing public APIs.
9. Tests can follow existing L0/L2 patterns for generators and runtime orchestration.
10. **Steps as POCOs with `ISagaStep<TSaga>` interface** can be resolved via DI and executed by the orchestration effect.
11. **Compensation via `ICompensatable<TSaga>` interface** on step classes provides a clear, discoverable pattern without separate compensation classes.
12. **Single `SagaOrchestrationEffect<TSaga>`** can handle step dispatch, compensation, and completion without multiple specialized effects.
13. **IDs-only data passing** (no large payloads in saga state) works because steps can query aggregates/projections for system data.
14. **Infrastructure events** are limited to the set in updated-design (`SagaStartedEvent`, `SagaStepCompleted`, `SagaStepFailed`, `SagaCompensating`, `SagaStepCompensated`, `SagaCompleted`, `SagaCompensated`, `SagaFailed`) and cover all state transitions.
15. **StepResult/CompensationResult** shapes match updated-design (success flags, error codes/messages, and event list for steps).

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
11. Do any existing event types or reducer patterns conflict with the new saga infrastructure event set?
12. Are there existing result/operation types that should be reused for `StepResult` or `CompensationResult` semantics?

## Verification Answers

1. Aggregate controllers and server DTOs are generated in [src/Inlet.Server.Generators/AggregateControllerGenerator.cs](../../src/Inlet.Server.Generators/AggregateControllerGenerator.cs) and [src/Inlet.Server.Generators/CommandServerDtoGenerator.cs](../../src/Inlet.Server.Generators/CommandServerDtoGenerator.cs). The controller generator scans for `[GenerateAggregateEndpoints]` and discovers commands in a `Commands` sub-namespace; action methods map DTOs to commands and call `IGenericAggregateGrain<T>.ExecuteAsync`.
2. Client command generators are implemented in [src/Inlet.Client.Generators/CommandClientActionsGenerator.cs](../../src/Inlet.Client.Generators/CommandClientActionsGenerator.cs), [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs](../../src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs), [src/Inlet.Client.Generators/CommandClientReducersGenerator.cs](../../src/Inlet.Client.Generators/CommandClientReducersGenerator.cs), [src/Inlet.Client.Generators/CommandClientStateGenerator.cs](../../src/Inlet.Client.Generators/CommandClientStateGenerator.cs), and [src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs](../../src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs). These follow naming conventions and register mappers, reducers, and action effects.
3. Aggregate silo registrations are generated in [src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs) and discover handlers, reducers, and effects via `Handlers`, `Reducers`, and `Effects` sub-namespaces plus base types like `CommandHandlerBase` and `EventReducerBase`.
4. Aggregate orchestration runtime is centered on [src/EventSourcing.Aggregates/GenericAggregateGrain.cs](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs), which composes `IRootCommandHandler<T>`, `IRootReducer<T>`, and `IRootEventEffect<T>`. Dispatch logic lives in [src/EventSourcing.Aggregates/RootCommandHandler.cs](../../src/EventSourcing.Aggregates/RootCommandHandler.cs) and [src/EventSourcing.Aggregates/RootEventEffect.cs](../../src/EventSourcing.Aggregates/RootEventEffect.cs).
5. Projections are exposed via `UxProjectionControllerBase` and the projection endpoints generator in [src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs](../../src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs) and [src/EventSourcing.UxProjections.Api/UxProjectionControllerBase{TProjection,TDto}.cs](../../src/EventSourcing.UxProjections.Api/UxProjectionControllerBase%7BTProjection,TDto%7D.cs). Client updates are handled by SignalR action effects in [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs](../../src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) and stored in [src/Inlet.Client.Abstractions/State/ProjectionsFeatureState.cs](../../src/Inlet.Client.Abstractions/State/ProjectionsFeatureState.cs).
6. Core abstractions include `CommandHandlerBase` and `EventEffectBase` in [src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs](../../src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs) and [src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs](../../src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs), `EventReducerBase` in [src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs](../../src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs), and aggregate grain access via [src/EventSourcing.Aggregates.Abstractions/IAggregateGrainFactory.cs](../../src/EventSourcing.Aggregates.Abstractions/IAggregateGrainFactory.cs).
7. Logging patterns in aggregate orchestration use LoggerExtensions such as [src/EventSourcing.Aggregates/RootCommandHandlerLoggerExtensions.cs](../../src/EventSourcing.Aggregates/RootCommandHandlerLoggerExtensions.cs) and [src/EventSourcing.Aggregates.Api/AggregateControllerLoggerExtensions.cs](../../src/EventSourcing.Aggregates.Api/AggregateControllerLoggerExtensions.cs).
8. Generator tests exist under L0 projects and use Roslyn compilation helpers, e.g. [tests/Inlet.Client.Generators.L0Tests/CommandClientActionsGeneratorTests.cs](../../tests/Inlet.Client.Generators.L0Tests/CommandClientActionsGeneratorTests.cs), [tests/Inlet.Server.Generators.L0Tests/AggregateControllerGeneratorTests.cs](../../tests/Inlet.Server.Generators.L0Tests/AggregateControllerGeneratorTests.cs), and [tests/Inlet.Silo.Generators.L0Tests](../../tests/Inlet.Silo.Generators.L0Tests).
9. Aggregate state uses Brook/storage/serializer attributes as in [samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs](../../samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs).
10. Existing orchestration infrastructure includes root event effects and fire-and-forget effect workers in [src/EventSourcing.Aggregates/RootEventEffect.cs](../../src/EventSourcing.Aggregates/RootEventEffect.cs) and [src/EventSourcing.Aggregates/FireAndForgetEffectWorkerGrain.cs](../../src/EventSourcing.Aggregates/FireAndForgetEffectWorkerGrain.cs). No saga-specific constructs were located yet.
11. UNVERIFIED: No repository evidence checked yet for conflicts with the updated saga infrastructure event set.
12. UNVERIFIED: No repository evidence checked yet for existing result/operation types that should be reused for `StepResult` or `CompensationResult`.