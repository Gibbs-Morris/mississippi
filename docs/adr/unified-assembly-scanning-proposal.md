# ADR: Unified Reducer Pattern and Assembly Scanning for Mississippi

## Status

Proposed

## Context

Mississippi has two primary usage contexts:

1. **Server-side (EventSourcing)**: Orleans grains, aggregates, reducers, command handlers, projections
2. **Client-side (Ripples)**: Blazor state management with reducers, effects, middleware

Currently, these contexts use **different patterns**:

```csharp
// Server-side: One reducer per event, DI registration
services.AddReducer<ChannelCreated, ChannelAggregate, ChannelCreatedReducer>();

// Client-side: One reducer handles all actions via switch, instance registration
store.RegisterReducer(new SidebarReducer());  // IReducer<TState>.Reduce(state, IAction)
```

**Problems with current approach:**

1. Different mental models for server vs client
2. Different registration APIs
3. Client reducer uses switch statement (harder to test, violates SRP)
4. No assembly scanning on either side

**Design goals:**

1. **Unified DevEx**: Same patterns for client and server
2. **Same registration API**: `services.AddReducer<TInput, TState, TReducer>()`
3. **One reducer per input type**: Single responsibility, easier testing
4. **Record defaults for initial state**: No `GetInitialState()` method
5. **Assembly scanning**: Automatic discovery on both sides
6. **Federated stores**: Feature isolation on client side
7. **AI-friendly**: Single opinionated pattern for code generation

## Decision

### 1. Unified Reducer Interface

A single `IReducer<TInput, TState>` interface used on both server and client:

```csharp
// ═══════════════════════════════════════════════════════════════
// SHARED ABSTRACTION (Core.Abstractions)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// A pure function that reduces input to produce new state.
/// Same interface for server events and client actions.
/// </summary>
public interface IReducer<in TInput, TState>
{
    TState Reduce(TState state, TInput input);
}

/// <summary>
/// Base class with mutation guard.
/// </summary>
public abstract class Reducer<TInput, TState> : IReducer<TInput, TState>
{
    public TState Reduce(TState state, TInput input)
    {
        var result = ReduceCore(state, input);
        // Mutation guard prevents returning same reference
        return result;
    }
    
    protected abstract TState ReduceCore(TState state, TInput input);
}
```

### 2. Same Pattern Server and Client

```csharp
// ═══════════════════════════════════════════════════════════════
// SERVER: Event → Reducer → Aggregate/Projection
// ═══════════════════════════════════════════════════════════════

public sealed record ChannelAggregate
{
    public bool IsCreated { get; init; } = false;
    public string ChannelId { get; init; } = "";
    public string Name { get; init; } = "";
}

internal sealed class ChannelCreatedReducer : Reducer<ChannelCreated, ChannelAggregate>
{
    protected override ChannelAggregate ReduceCore(ChannelAggregate state, ChannelCreated evt) =>
        new() { IsCreated = true, ChannelId = evt.ChannelId, Name = evt.Name };
}

// Registration
services.AddReducer<ChannelCreated, ChannelAggregate, ChannelCreatedReducer>();


// ═══════════════════════════════════════════════════════════════
// CLIENT: Action → Reducer → State (SAME PATTERN!)
// ═══════════════════════════════════════════════════════════════

public sealed record SidebarState
{
    public bool IsOpen { get; init; } = false;
    public string ActivePanel { get; init; } = "home";
}

public sealed record ToggleSidebarAction : IAction;

internal sealed class ToggleSidebarReducer : Reducer<ToggleSidebarAction, SidebarState>
{
    protected override SidebarState ReduceCore(SidebarState state, ToggleSidebarAction _) =>
        state with { IsOpen = !state.IsOpen };
}

// Registration - SAME API AS SERVER!
services.AddReducer<ToggleSidebarAction, SidebarState, ToggleSidebarReducer>();
```

### 3. Initial State from Record Defaults

**No `GetInitialState()` method.** Initial state comes from record property defaults:

```csharp
// Server: new TState() uses record defaults
TState state = snapshot ?? new TState();

// Client: new TState() uses record defaults  
TState state = new TState();

// Both use the same mechanism - record defaults
public sealed record SidebarState
{
    public bool IsOpen { get; init; } = false;        // Default: closed
    public string ActivePanel { get; init; } = "home"; // Default: home panel
}
```

**Rationale:**
- Reducers have single responsibility (reduce only)
- State types define their own defaults
- Same pattern on server and client
- Testable: `new TState()` gives predictable initial state
- Future: Can add explicit `IInitialState<T>` to both sides if needed

### 4. Federated Stores (Client)

Client uses federated stores for feature isolation:

```csharp
// Each feature has its own store (like aggregates on server)
services.AddMississippiClient(o => o
    .AddFeatureStore<SidebarStore>()
    .AddFeatureStore<ChatStore>()
    .AddFeatureStore<AdminStore>()
    .ScanAssemblies(typeof(SidebarState).Assembly)
    .WithReduxDevTools());

// Feature store marker
public interface IFeatureStore
{
    static abstract string FeatureKey { get; }
}

public sealed class SidebarStore : FeatureStore<SidebarState>, IFeatureStore
{
    public static string FeatureKey => "sidebar";
}
```

### 5. Unified Registration API

```csharp
// ═══════════════════════════════════════════════════════════════
// SERVER
// ═══════════════════════════════════════════════════════════════

services.AddMississippi(o => o
    .ScanAssemblies(typeof(ChannelAggregate).Assembly)
    .WithOrleans()
    .WithCosmos()
    .WithSignalR());

// Or explicit (still available):
services.AddReducer<ChannelCreated, ChannelAggregate, ChannelCreatedReducer>();
services.AddCommandHandler<CreateChannel, ChannelAggregate, CreateChannelHandler>();


// ═══════════════════════════════════════════════════════════════
// CLIENT
// ═══════════════════════════════════════════════════════════════

services.AddMississippiClient(o => o
    .ScanAssemblies(typeof(SidebarState).Assembly)
    .AddFeatureStore<SidebarStore>()
    .AddFeatureStore<ChatStore>()
    .WithReduxDevTools()
    .WithSignalR());

// Or explicit (SAME API AS SERVER!):
services.AddReducer<ToggleSidebarAction, SidebarState, ToggleSidebarReducer>();
services.AddReducer<SetActivePanelAction, SidebarState, SetActivePanelReducer>();
```

### 6. Assembly Scanning

Scanning discovers `Reducer<TInput, TState>` implementations on both sides:

```csharp
// Scanner finds all Reducer<,> base classes
foreach (Type type in assembly.ExportedTypes)
{
    Type? baseType = type.BaseType;
    while (baseType != null)
    {
        if (baseType.IsGenericType && 
            baseType.GetGenericTypeDefinition() == typeof(Reducer<,>))
        {
            Type inputType = baseType.GenericTypeArguments[0];  // TEvent or TAction
            Type stateType = baseType.GenericTypeArguments[1];  // TProjection or TState
            RegisterReducer(services, inputType, stateType, type);
            break;
        }
        baseType = baseType.BaseType;
    }
}
```

### 7. RootReducer Pattern (Both Sides)

Both server and client use `RootReducer<TState>` to route inputs:

```csharp
/// <summary>
/// Routes inputs to the correct typed reducer.
/// Used on both server (events) and client (actions).
/// </summary>
public sealed class RootReducer<TState> : IRootReducer<TState>
{
    private FrozenDictionary<Type, IReducer<TState>> ReducerIndex { get; }

    public RootReducer(IEnumerable<IReducer<TState>> reducers)
    {
        ReducerIndex = BuildIndex(reducers);
    }

    public TState Reduce(TState state, object input)
    {
        if (ReducerIndex.TryGetValue(input.GetType(), out var reducer))
        {
            return reducer.TryReduce(state, input, out var result) ? result : state;
        }
        return state; // No reducer for this input
    }
}
```

## Concept Mapping

| Concept | Server | Client |
|---------|--------|--------|
| **Input** | `TEvent` (domain event) | `TAction : IAction` (UI action) |
| **State** | `TProjection` / `TAggregate` | `TState` |
| **Reducer** | `Reducer<TEvent, TProjection>` | `Reducer<TAction, TState>` |
| **Initial State** | `new TProjection()` (record defaults) | `new TState()` (record defaults) |
| **Registration** | `AddReducer<E, P, R>()` | `AddReducer<A, S, R>()` |
| **Composition** | `RootReducer<TProjection>` | `RootReducer<TState>` |
| **Isolation** | Aggregate grain | Feature store |

## File Structure Convention

```
// Server: Feature/Aggregate structure
Channel/
├── ChannelAggregate.cs           // State with defaults
├── Events/
│   ├── ChannelCreated.cs
│   └── ChannelRenamed.cs
├── Commands/
│   ├── CreateChannel.cs
│   └── RenameChannel.cs
├── Reducers/
│   ├── ChannelCreatedReducer.cs  // One per event
│   └── ChannelRenamedReducer.cs
└── Handlers/
    ├── CreateChannelHandler.cs
    └── RenameChannelHandler.cs

// Client: Feature/Store structure (SAME PATTERN!)
Sidebar/
├── SidebarState.cs               // State with defaults
├── SidebarStore.cs               // Feature store marker
├── Actions/
│   ├── ToggleSidebarAction.cs
│   └── SetActivePanelAction.cs
└── Reducers/
    ├── ToggleSidebarReducer.cs   // One per action
    └── SetActivePanelReducer.cs
```

## Breaking Changes

| Current | New |
|---------|-----|
| `IReducer<TState>` with `GetInitialState()` | `IReducer<TAction, TState>` (no initial state method) |
| Single reducer handles all actions | One reducer per action type |
| `store.RegisterReducer(instance)` | `services.AddReducer<A, S, R>()` |
| Global single store | Federated feature stores |

## Migration Path

1. Existing `IReducer<TState>` continues to work (deprecation warning)
2. New `IReducer<TAction, TState>` is the recommended pattern
3. Gradual migration: split switch-based reducer into per-action reducers
4. Assembly scanning finds both patterns during transition

## Consequences

### Positive

- **Same DevEx**: `AddReducer<TInput, TState, TReducer>()` everywhere
- **Same mental model**: Event/Action → Reducer → State/Projection
- **Single responsibility**: One reducer class per input type
- **Better testing**: Test each reducer in isolation
- **AI-friendly**: One pattern to learn and generate
- **Feature isolation**: Federated stores prevent coupling

### Negative

- **More files**: One file per reducer (mitigated by clearer structure)
- **Breaking change**: Existing client reducers need migration
- **Learning curve**: Existing Ripples users need to adapt

### Neutral

- **Compile-time safety preserved**: Generic constraints catch mismatches
- **Explicit registration still works**: For edge cases and debugging

## References

- [Fluxor ScanAssemblies](https://github.com/mrpmorris/Fluxor)
- Existing server pattern: `samples/Cascade/Cascade.Domain/`
- Existing event scanning: `src/EventSourcing.Aggregates/AggregateRegistrations.cs`
