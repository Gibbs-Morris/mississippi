---
id: event-sourcing
title: Event Sourcing Glossary
sidebar_label: Event Sourcing
description: Mississippi's event sourcing framework - Brooks, Aggregates, Commands, Events, Reducers, and Projections
keywords:
  - event sourcing
  - cqrs
  - aggregate
  - brook
  - command
  - event
  - reducer
  - projection
  - snapshot
  - saga
sidebar_position: 2
---

# Event Sourcing Glossary

Mississippi's event sourcing framework terminology. These are Mississippi-specific implementations of event sourcing and CQRS patterns.

## Commands & Handlers

### Command

An immutable data structure representing a user's intention to change aggregate state. Commands are requests (not facts) and may be rejected during validation. They typically follow imperative naming (e.g., `PlaceOrder`, `IncrementCounter`, `OpenAccount`) and carry only the data needed to perform the operation.

**Pattern in Mississippi**: Commands are sealed record types with required properties, dispatched to aggregates via `IGenericAggregateGrain<TAggregate>.ExecuteAsync(command)`. The grain routes commands to registered `ICommandHandler<TCommand, TSnapshot>` implementations.

**Key principle**: Commands express intent; events express facts. Commands may fail; events record what actually happened.

**Source**: [IGenericAggregateGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/IGenericAggregateGrain.cs), [Sample commands](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Crescent/Commands), [Spring commands](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Domain/Commands)

### Command Handler

A stateless component that validates commands against aggregate state and produces domain events. Command handlers implement `ICommandHandler<TCommand, TSnapshot>` and inherit from `CommandHandlerBase<TCommand, TSnapshot>` to encapsulate business logic for a specific command type.

**Responsibilities**: Business rule validation, state consistency checks, event emission, error handling.

**Pattern**: One handler per command type, registered via `AddCommandHandler<TCommand, TSnapshot, THandler>(services)` or as inline delegates. The `IRootCommandHandler<TSnapshot>` composes multiple handlers and dispatches commands by runtime type.

**Returns**: `OperationResult<IReadOnlyList<object>>` containing events on success or error details on failure.

**Source**: [CommandHandlerBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs), [ICommandHandler](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/ICommandHandler.cs), [Sample handler](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Crescent/Handlers/IncrementCounterHandler.cs)

## Aggregates

### Aggregate

The core domain entity in event sourcing that enforces business invariants and coordinates command handling. In Mississippi, aggregates are POCO classes hosted in `GenericAggregateGrain<TAggregate>` Orleans grains, identified by entity ID and decorated with `[BrookName]` attribute.

**State reconstruction**: Load latest snapshot from `SnapshotCacheGrain` → apply subsequent events from Brook via `IRootReducer` → result is current aggregate state.

**Command execution flow**: `ExecuteAsync(command)` → `IRootCommandHandler.Handle(command, state)` → emit events → `IRootReducer.Reduce(state, event)` for each event → persist events to Brook → update snapshot.

**Key characteristics**: Single-threaded execution per instance, optimistic concurrency control (expected version on writes), automatic activation/deactivation via Orleans grain lifecycle, Brook name derived from `[BrookName]` attribute.

**Source**: [GenericAggregateGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates/GenericAggregateGrain.cs), [IGenericAggregateGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/IGenericAggregateGrain.cs)

## Events & Reducers

### Event

An immutable record of a fact that occurred in the domain, stored in Brooks as the source of truth. Events use past-tense naming (e.g., `CounterIncremented`, `OrderPlaced`, `AccountOpened`) and contain all data needed to understand what happened.

**Properties**: Event type (fully-qualified name), data (serialized payload), data content type (MIME), position (sequence number in Brook), correlation ID, timestamp.

**Storage**: Events are appended to Brooks via `IBrookStorageProvider`, stored immutably (never updated or deleted), and read sequentially for state reconstruction.

**Usage**: Commands emit events → events persist to Brook → reducers fold events into projections/snapshots → projections update UIs.

**Source**: [Brook event abstraction](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookEvent.cs), [EventDocument storage model](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Cosmos/Storage/EventDocument.cs), [Sample events](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Crescent/Events)

### EventReducer

A pure function that transforms an event into a new projection state, implementing the "fold" operation in event sourcing. Event reducers inherit from `EventReducerBase<TEvent, TProjection>` and must be deterministic—given the same state and event, they always produce the same result.

**Signature**: `TProjection Reduce(TProjection state, TEvent eventData)` returns new state by applying the event.

**Immutability requirement**: Must use `with` expressions, record copying, or new instance creation—never mutate the input state. Reducers throw `InvalidOperationException` if the returned projection has the same reference as the input state.

**Composition**: `RootReducer<TProjection>` dispatches events to appropriate reducers by runtime type, using a precomputed index for performance. The reducer hash (`GetReducerHash()`) versions projections and invalidates stale snapshots when reducer logic changes.

**Source**: [EventReducerBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs), [RootReducer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers/RootReducer.cs), [Sample reducers](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Crescent/Reducers)

### EventEffect

An asynchronous side effect handler triggered by events, yielding additional events in response. Event effects implement `IRootEventEffect<TAggregate>` and run after reducers complete during command execution. Unlike reducers (which are pure and synchronous), effects can perform async operations and emit new events.

**Example scenarios**: Saga coordination (command in one aggregate triggers command in another), async notifications (event triggers email/push notification), compensation logic (event triggers rollback).

**Source**: [IRootEventEffect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/IRootEventEffect.cs)

## Brooks (Event Streams)

### Brook

An append-only, immutable event stream associated with a single entity, serving as the source of truth for that entity's state history. Brooks are identified by `BrookKey` (brook name + entity ID) and store events sequentially with monotonically increasing positions.

**Properties**: Immutable (events never updated/deleted), append-only (new events always at the end), per-entity isolation (one brook per aggregate/projection entity), sequential reads (events retrieved in order).

**Operations**: `AppendEventsAsync(brookId, events, expectedVersion)` with optimistic concurrency, `ReadEventsAsync(brookRange)` returns async enumerable, `ReadCursorPositionAsync(brookId)` returns latest position.

**Source**: [BrookKey abstraction](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/BrookKey.cs), [IBrookStorageProvider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageProvider.cs)

### Brook Storage Provider

The storage abstraction for persisting and reading Brook event streams. `IBrookStorageProvider` combines read (`IBrookStorageReader`) and write (`IBrookStorageWriter`) operations with a `Format` property identifying the provider implementation.

**Cosmos implementation details**:

- **Format identifier**: `"cosmos-db"`
- **Container**: Default "brooks" (configurable via `BrookStorageOptions.ContainerId`)
- **Partition key**: `/brookPartitionKey`
- **Transactional batch writes**: Optimistic concurrency via expected version checks
- **Event document**: Data (byte[]), DataContentType, EventType, Position, CorrelationId, Timestamp

**Registration**: `AddCosmosBrookStorageProvider(services)` registers provider and dependencies.

**Source**: [IBrookStorageProvider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageProvider.cs), [BrookStorageProvider (Cosmos)](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Cosmos/BrookStorageProvider.cs)

## Snapshots

### Snapshot

A cached, versioned copy of aggregate or projection state at a specific event position, used to avoid replaying all events on every activation. Snapshots are stored via `ISnapshotStorageProvider` and cached in memory by `SnapshotCacheGrain<TSnapshot>` Orleans grains.

**Cache key format**: `"brookName|entityId|version|snapshotStorageName|reducersHash"`. The reducer hash ensures snapshots are invalidated when reducer logic changes.

**Activation flow**:

1. Grain activates → reads snapshot from storage
2. Validates reducer hash matches current `RootReducer.GetReducerHash()`
3. If valid: uses cached state; if stale/missing: rebuilds from Brook
4. Caches state in memory for fast read access
5. Fire-and-forget call to `SnapshotPersisterGrain` for background persistence

**Source**: [SnapshotCacheGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Snapshots/SnapshotCacheGrain.cs), [ISnapshotCacheGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Snapshots.Abstractions/ISnapshotCacheGrain.cs)

### Snapshot Storage Provider

The storage abstraction for persisting and reading snapshots. `ISnapshotStorageProvider` combines read and write operations, storing `SnapshotEnvelope` records containing serialized state and metadata.

**Registration**: `AddCosmosSnapshotStorageProvider(services)` registers Cosmos DB snapshot storage.

**Source**: [ISnapshotStorageProvider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageProvider.cs)

## Projections

### Projection

A read-optimized view (read model) derived from an event stream, designed for querying without modifying the underlying event data. Projections transform raw events into domain-specific structures optimized for particular read use cases.

**Key characteristics**: Derived from events (eventually consistent with source), optimized for specific queries (denormalized structure), rebuildable (can regenerate from event history), stateless computation (pure transformation).

**In Mississippi**: Projections are implemented via EventReducers that fold events into projection state. The state is cached in Orleans grains for efficient reads and versioned for cache-friendly HTTP responses (ETag support).

### UX Projection

A composable, read-optimized projection specifically designed for client UX state. UX projections are implemented as a **stateless worker grain family** coordinating cursor tracking, versioned caching, and read access.

**Grain architecture**:

- **`IUxProjectionGrain<TProjection>`**: Entry point grain (stateless worker), keyed by entity ID
- **`IUxProjectionCursorGrain`**: Tracks the brook cursor position (latest event version)
- **`IUxProjectionVersionedCacheGrain<TProjection>`**: Caches projection state at specific versions

**API surface** (all `[ReadOnly]`):

- `GetAsync()`: Returns current projection state at latest version
- `GetAtVersionAsync(BrookPosition version)`: Returns projection state at a specific historical version
- `GetLatestVersionAsync()`: Returns the latest version position without fetching state

**Attributes required**:

- `[BrookName]`: Identifies which brook the projection reads from
- `[ProjectionPath]`: Defines the path for Inlet subscriptions

**HTTP exposure**: `UxProjectionControllerBase<TProjection, TDto>` provides REST endpoints with ETag support:

- `GET /` - Latest projection (304 Not Modified if unchanged)
- `GET /version` - Latest version number
- `GET /at/{version}` - Specific version

**Source**: [IUxProjectionGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.UxProjections.Abstractions/IUxProjectionGrain.cs), [UxProjectionGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.UxProjections/UxProjectionGrain.cs), [UxProjectionControllerBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.UxProjections.Api/UxProjectionControllerBase%7BTProjection,TDto%7D.cs)

## Sagas

### Saga

A distributed transaction pattern for coordinating long-running business processes across multiple aggregates or services, using compensation (rollback) logic instead of traditional ACID transactions.

**Key concepts**:

- **Choreography**: Each service listens for events and decides locally what to do next (decentralized)
- **Orchestration**: A central coordinator directs the workflow explicitly (centralized)
- **Compensation**: Inverse operations that semantically undo previous steps

**In Mississippi**: Sagas are an **upcoming feature**. The EventEffect mechanism already supports saga-like patterns: an event in one aggregate can dispatch commands to other aggregates (cross-aggregate coordination).

**Current pattern** (via EventEffect):

```csharp
// Effect in BankAccountAggregate triggers command to another aggregate
protected override async Task HandleSimpleAsync(FundsDeposited eventData, ...)
{
    if (eventData.Amount > AmlThreshold)
    {
        IGenericAggregateGrain<TransactionInvestigationQueueAggregate> grain = ...;
        await grain.ExecuteAsync(new FlagTransaction { ... });
    }
}
```

---

## See Also

- [Industry Concepts Glossary](industry-concepts.md) — Standard technologies and patterns
- [Reservoir & Inlet Glossary](reservoir-inlet.md) — Client-side state and real-time subscriptions
- [Aqueduct & Server Glossary](aqueduct-server.md) — Server-side Mississippi components
