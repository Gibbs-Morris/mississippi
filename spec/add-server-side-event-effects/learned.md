# Learned Facts

## Current Architecture

### Server-Side Aggregates (EventSourcing.Aggregates)

**Flow:** Command → Handler → Events → Reducers → Snapshot

| Component | Location | Pattern |
|-----------|----------|---------|
| Aggregate | `Spring.Domain.Aggregates.BankAccount/BankAccountAggregate.cs` | Record with `[GenerateAggregateEndpoints]` |
| Commands | `{Aggregate}/Commands/*.cs` | Records with `[GenerateCommand(Route="...")]` |
| Events | `{Aggregate}/Events/*.cs` | Records with `[EventStorageName(...)]` |
| Handlers | `{Aggregate}/Handlers/*.cs` | Classes extending `CommandHandlerBase<TCommand, TAggregate>` |
| Reducers | `{Aggregate}/Reducers/*.cs` | Classes extending `EventReducerBase<TEvent, TAggregate>` |

**Key Files:**
- [GenericAggregateGrain.cs](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs) - Grain that processes commands
- [RootCommandHandler.cs](../../src/EventSourcing.Aggregates/RootCommandHandler.cs) - Dispatches to command handlers
- [RootReducer.cs](../../src/EventSourcing.Reducers/RootReducer.cs) - Dispatches to event reducers
- [AggregateRegistrations.cs](../../src/EventSourcing.Aggregates/AggregateRegistrations.cs) - DI registration
- [ReducerRegistrations.cs](../../src/EventSourcing.Reducers/ReducerRegistrations.cs) - Reducer DI registration

### Orleans Throughput Patterns (Verified)

**Grain Concurrency:**
- Orleans grains are **single-threaded** and **non-reentrant** by default
- A grain blocked on `await` cannot process other requests
- `[Reentrant]` allows interleaving but requires careful state management
- `[AlwaysInterleave]` on a method allows it to interleave with other calls

**Fire-and-Forget Pattern (exists in codebase):**
- [ISnapshotPersisterGrain.cs](../../src/EventSourcing.Snapshots.Abstractions/ISnapshotPersisterGrain.cs):
  - Uses `[StatelessWorker]` for auto-scaling
  - Uses `[OneWay]` attribute for fire-and-forget
  - Caller uses `_ = grain.MethodAsync()` pattern
- [SnapshotCacheGrain.cs](../../src/EventSourcing.Snapshots/SnapshotCacheGrain.cs#L270):
  - `_ = persisterGrain.PersistAsync(envelope);` - fire-and-forget call

**StatelessWorker Grains:**
- Auto-scale based on load
- No affinity to specific silo
- Ideal for CPU/IO-bound work that doesn't need state

### Client-Side Redux Store (Reservoir)

**Flow:** Action → Middleware → Reducers → Effects → (more Actions)

| Component | Location | Pattern |
|-----------|----------|---------|
| Store | `src/Reservoir/Store.cs` | Central state container |
| Actions | `Spring.Client/.../Actions/*.cs` | Records implementing `IAction` |
| ActionReducers | `Spring.Client/.../Reducers/*.cs` | Static methods or classes extending `ActionReducerBase<TAction, TState>` |
| ActionEffects | `Spring.Client/.../ActionEffects/*.cs` | Classes implementing `IActionEffect` |

**Key Files:**
- [IActionEffect.cs](../../src/Reservoir.Abstractions/IActionEffect.cs) - Client effect interface (renamed from IEffect in PR #231)
- [IActionReducer.cs](../../src/Reservoir.Abstractions/IActionReducer.cs) - Client reducer interface
- [ActionReducerBase.cs](../../src/Reservoir.Abstractions/ActionReducerBase.cs) - Base class for reducers
- [CommandActionEffectBase.cs](../../src/Inlet.Blazor.WebAssembly.Abstractions/ActionEffects/CommandActionEffectBase.cs) - Base for command-posting effects
- [Store.cs](../../src/Reservoir/Store.cs) - Dispatches actions, runs reducers, triggers effects async

**Effect Triggering (verified in Store.cs):**
- `CoreDispatch()` calls `_ = TriggerEffectsAsync(action)` - fire-and-forget!
- Effects run asynchronously after reducers complete
- Effect exceptions are swallowed to prevent breaking dispatch
- **Note:** Client effects are fire-and-forget to keep UI responsive

### Source Generators

**Silo-Side:**
- [AggregateSiloRegistrationGenerator.cs](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs)
  - Generates `Add{Aggregate}Aggregate()` extension methods
  - Scans `Handlers/` sub-namespace for handlers extending `CommandHandlerBase<,>`
  - Scans `Reducers/` sub-namespace for reducers extending `EventReducerBase<,>`
  - **Will scan `Effects/` sub-namespace for effects extending `EventEffectBase<,>`**
  - Registers: event types, command handlers, reducers, snapshot converters

**Client-Side:**
- [CommandClientEffectsGenerator.cs](../../src/Inlet.Client.Generators/CommandClientEffectsGenerator.cs) - Generates effect classes
- [CommandClientReducersGenerator.cs](../../src/Inlet.Client.Generators/CommandClientReducersGenerator.cs) - Generates aggregate reducer classes
- [CommandClientRegistrationGenerator.cs](../../src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs) - Generates feature registration

### Naming Conventions (Final)

| Side | Reducer Type | Effect Type |
|------|-------------|-------------|
| Server (Events) | `EventReducerBase<TEvent, TProjection>` | `EventEffectBase<TEvent, TAggregate>` |
| Client (Actions) | `ActionReducerBase<TAction, TState>` | `IActionEffect` / `CommandActionEffectBase` |

## Registration Patterns

### Server Reducer Registration
```csharp
services.AddReducer<AccountOpened, BankAccountAggregate, AccountOpenedReducer>();
```

### Server Handler Registration
```csharp
services.AddCommandHandler<OpenAccount, BankAccountAggregate, OpenAccountHandler>();
```

### Server Effect Registration (NEW)
```csharp
services.AddEventEffect<AccountOpened, BankAccountAggregate, AccountOpenedEffect>();
services.AddEventEffectDispatcher<BankAccountAggregate>();
```

### Client Effect Registration
```csharp
services.AddActionEffect<DepositFundsActionEffect>();
```

## Key Design Decisions (Final)

1. **Server effects run inside grain** - Blocking, sequential, in grain context
2. **No state parameter** - Aligned with client-side IActionEffect pattern
3. **EffectContext provides brook info** - Can read events if state needed (edge case)
4. **Effects yield events via IAsyncEnumerable** - Supports LLM streaming scenarios
5. **Events persisted immediately on yield** - Real-time projection updates
6. **Concurrency differs from client** - Server blocks (grain model), client fire-and-forget (UI)
7. **Full observability** - Metrics, logging, warnings for >1s effects
8. **Recursion limit** - Max 10 iterations to prevent infinite loops

## Client vs Server Effect Comparison

| Aspect | Client (IActionEffect) | Server (IEventEffect) |
|--------|----------------------|----------------------|
| Trigger | `IAction` | `object` (event) |
| Yields | `IAction` | `object` (events) |
| State access | ❌ No | ❌ No |
| Context | None | `EffectContext` |
| Return type | `IAsyncEnumerable<IAction>` | `IAsyncEnumerable<object>` |
| Execution | Fire-and-forget | **Blocking** |
| Why | UI must stay responsive | Grain single-threaded model |

## Event Lifecycle in GenericAggregateGrain (To-Be)

```
ExecuteInternalAsync():
  1. Get current brook position
  2. Check optimistic concurrency
  3. Get current state from snapshot grain
  4. RootCommandHandler.Handle(command, state) → events
  5. BrookEventConverter.ToStorageEvents(events)
  6. BrookWriterGrain.AppendEventsAsync(events)
  7. RootReducer.Reduce(events, state) → newState
  8. **NEW: Build EffectContext(aggregateKey, brookName, aggregateTypeName)**
  9. **NEW: Loop effects until no more yielded events (max 10 iterations)**
     - RootEventEffectDispatcher.DispatchAsync(pendingEvents, context)
     - For each yielded event: persist immediately, reduce
  10. Update lastKnownPosition
  11. Return OperationResult.Ok()
```
