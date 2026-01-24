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

### Client-Side Redux Store (Reservoir)

**Flow:** Action → Middleware → Reducers → Effects → (more Actions)

| Component | Location | Pattern |
|-----------|----------|---------|
| Store | `src/Reservoir/Store.cs` | Central state container |
| Actions | `Spring.Client/.../Actions/*.cs` | Records implementing `IAction` |
| ActionReducers | `Spring.Client/.../Reducers/*.cs` | Static methods or classes extending `ActionReducerBase<TAction, TState>` |
| Effects | `Spring.Client/.../Effects/*.cs` | Classes implementing `IEffect` |

**Key Files:**
- [IEffect.cs](../../src/Reservoir.Abstractions/IEffect.cs) - Effect interface (CanHandle + HandleAsync returning IAsyncEnumerable<IAction>)
- [IActionReducer.cs](../../src/Reservoir.Abstractions/IActionReducer.cs) - Client reducer interface
- [ActionReducerBase.cs](../../src/Reservoir.Abstractions/ActionReducerBase.cs) - Base class for reducers
- [CommandEffectBase.cs](../../src/Inlet.Blazor.WebAssembly.Abstractions/Effects/CommandEffectBase.cs) - Base for command-posting effects
- [Store.cs](../../src/Reservoir/Store.cs) - Dispatches actions, runs reducers, triggers effects async

### Source Generators

**Silo-Side:**
- [AggregateSiloRegistrationGenerator.cs](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs)
  - Generates `Add{Aggregate}Aggregate()` extension methods
  - Scans `Handlers/` sub-namespace for handlers extending `CommandHandlerBase<,>`
  - Scans `Reducers/` sub-namespace for reducers extending `EventReducerBase<,>`
  - Registers: event types, command handlers, reducers, snapshot converters

**Client-Side:**
- [CommandClientEffectsGenerator.cs](../../src/Inlet.Client.Generators/CommandClientEffectsGenerator.cs) - Generates effect classes extending `CommandEffectBase`
- [CommandClientReducersGenerator.cs](../../src/Inlet.Client.Generators/CommandClientReducersGenerator.cs) - Generates aggregate reducer classes
- [CommandClientRegistrationGenerator.cs](../../src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs) - Generates feature registration

### Naming Conventions Observed

| Side | Reducer Type | Effect Type |
|------|-------------|-------------|
| Server (Events) | `EventReducerBase<TEvent, TProjection>` | **DOES NOT EXIST** |
| Client (Actions) | `ActionReducerBase<TAction, TState>` | `IEffect` / `CommandEffectBase` |

## Registration Patterns

### Server Reducer Registration
```csharp
services.AddReducer<AccountOpened, BankAccountAggregate, AccountOpenedReducer>();
// Uses ReducerRegistrations.AddReducer<TEvent, TProjection, TReducer>()
```

### Server Handler Registration
```csharp
services.AddCommandHandler<OpenAccount, BankAccountAggregate, OpenAccountHandler>();
```

### Client Effect Registration
```csharp
services.AddEffect<DepositFundsEffect>();
// Uses ReservoirRegistrations.AddEffect<TEffect>()
```

## Key Design Observations

1. **Handlers produce events synchronously** - `CommandHandlerBase.Handle()` returns `OperationResult<IReadOnlyList<object>>` (events)
2. **Reducers are pure** - Transform state based on events, no side effects
3. **Client effects are async** - Return `IAsyncEnumerable<IAction>` to allow streaming multiple actions
4. **Client effects run AFTER reducers** - In `Store.CoreDispatch()`: reducers → notify listeners → trigger effects
5. **GenericAggregateGrain** - The only grain-level component; handles command execution and event persistence
6. **No server-side effect concept exists** - Post-event side effects would need to be added

## Event Lifecycle in GenericAggregateGrain

```
ExecuteInternalAsync():
  1. Get current brook position
  2. Check optimistic concurrency
  3. Get current state from snapshot grain
  4. RootCommandHandler.Handle(command, state) → events
  5. BrookEventConverter.ToStorageEvents(events)
  6. BrookWriterGrain.AppendEventsAsync(events)
  7. Update lastKnownPosition
  8. Return OperationResult.Ok()
```

**MISSING:** No hook for running side effects after step 6 (events persisted).
