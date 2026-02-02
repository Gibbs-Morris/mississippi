# Learned

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

## Requirements Captured (from task.md)

- Sagas are aggregates; reuse aggregate infrastructure and patterns.
- Discovery MUST use attributes/types (no namespace conventions).
- State must be records; state changes only via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.

## Evidence To Collect (remaining)

- Locate existing workflow/saga-like constructs, if any, to avoid duplication.
- Identify any existing saga-related naming or attributes in `Inlet.Generators.Abstractions`.

## Risks/Constraints

- Must not use namespace conventions for discovery; use attributes/types only.
- State must be records; all state changes via events and reducers.