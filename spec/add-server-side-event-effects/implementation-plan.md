# Implementation Plan

## Overview

Add server-side event effects to the aggregate system, allowing users to define asynchronous side effects that run after events are persisted. Effects run via a stateless worker grain for throughput isolation.

## Size Assessment: Large

- Multiple new types across abstractions and implementations
- New grain (`IEffectDispatcherGrain`) for throughput isolation
- New gateway (`IAggregateCommandGateway`) for saga patterns
- Source generator modifications
- Changes to GenericAggregateGrain
- New folder convention in domain projects
- Comprehensive tests required

---

## Phase 1: Abstractions (EventSourcing.Aggregates.Abstractions)

### Step 1.1: Add IEventEffect Interface

**File:** `src/EventSourcing.Aggregates.Abstractions/IEventEffect.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines an event effect that handles side effects for domain events.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
public interface IEventEffect<in TAggregate>
{
    /// <summary>
    ///     Determines whether this effect can handle the given event.
    /// </summary>
    bool CanHandle(object eventData);

    /// <summary>
    ///     Handles the event asynchronously.
    /// </summary>
    Task HandleAsync(
        object eventData,
        string aggregateKey,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Defines an event effect that handles a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type to handle.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
public interface IEventEffect<in TEvent, in TAggregate> : IEventEffect<TAggregate>
{
    /// <summary>
    ///     Handles the typed event asynchronously.
    /// </summary>
    Task HandleAsync(
        TEvent eventData,
        string aggregateKey,
        CancellationToken cancellationToken
    );
}
```

### Step 1.2: Add EventEffectBase

**File:** `src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for event effects that handle a specific event type.
/// </summary>
public abstract class EventEffectBase<TEvent, TAggregate> : IEventEffect<TEvent, TAggregate>
{
    public bool CanHandle(object eventData) => eventData is TEvent;

    public Task HandleAsync(object eventData, string aggregateKey, CancellationToken cancellationToken)
    {
        if (eventData is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, aggregateKey, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public abstract Task HandleAsync(
        TEvent eventData,
        string aggregateKey,
        CancellationToken cancellationToken
    );
}
```

### Step 1.3: Add IEffectDispatcherGrain Interface

**File:** `src/EventSourcing.Aggregates.Abstractions/IEffectDispatcherGrain.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Stateless worker grain that dispatches event effects asynchronously.
/// </summary>
/// <remarks>
///     This grain receives fire-and-forget calls via [OneWay], allowing the
///     aggregate grain to return immediately while effects run in the background.
/// </remarks>
[StatelessWorker]
[Alias("Mississippi.EventSourcing.Aggregates.Abstractions.IEffectDispatcherGrain")]
public interface IEffectDispatcherGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Dispatches effects for the given events asynchronously.
    /// </summary>
    [Alias("DispatchAsync")]
    [OneWay]
    Task DispatchAsync(
        string aggregateTypeName,
        string aggregateKey,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken = default
    );
}
```

### Step 1.4: Add IAggregateCommandGateway Interface (Saga Support)

**File:** `src/EventSourcing.Aggregates.Abstractions/IAggregateCommandGateway.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Gateway for sending commands to aggregates from within effects.
/// </summary>
/// <remarks>
///     This enables saga patterns where effects orchestrate cross-aggregate workflows.
/// </remarks>
public interface IAggregateCommandGateway
{
    /// <summary>
    ///     Sends a command to the specified aggregate.
    /// </summary>
    Task<TResult> SendCommandAsync<TAggregate, TResult>(
        string aggregateKey,
        ICommand<TAggregate, TResult> command,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Sends a command to the specified aggregate (fire-and-forget).
    /// </summary>
    Task SendCommandAsync<TAggregate>(
        string aggregateKey,
        ICommand<TAggregate, Unit> command,
        CancellationToken cancellationToken = default
    );
}
```

---

## Phase 2: Implementation (EventSourcing.Aggregates)

### Step 2.1: Add EffectDispatcherGrain

**File:** `src/EventSourcing.Aggregates/EffectDispatcherGrain.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Stateless worker grain that dispatches event effects asynchronously.
/// </summary>
[StatelessWorker]
public sealed class EffectDispatcherGrain : Grain, IEffectDispatcherGrain
{
    private IServiceProvider ServiceProvider { get; }
    private ILogger<EffectDispatcherGrain> Logger { get; }

    public EffectDispatcherGrain(
        IServiceProvider serviceProvider,
        ILogger<EffectDispatcherGrain> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    public async Task DispatchAsync(
        string aggregateTypeName,
        string aggregateKey,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken = default)
    {
        // Resolve effects by aggregate type name from DI
        // Use a registry that maps type names to effect collections
        var effectRegistry = ServiceProvider.GetRequiredService<IEffectRegistry>();
        var effects = effectRegistry.GetEffects(aggregateTypeName);

        foreach (var eventData in events)
        {
            foreach (var effect in effects)
            {
                if (!effect.CanHandle(eventData))
                {
                    continue;
                }

                try
                {
                    await effect.HandleAsync(eventData, aggregateKey, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected; don't log as error
                }
                catch (Exception ex)
                {
                    Logger.EffectFailed(effect.GetType().Name, aggregateKey, ex);
                }
            }
        }
    }
}
```

### Step 2.2: Add IEffectRegistry Interface and Implementation

**File:** `src/EventSourcing.Aggregates.Abstractions/IEffectRegistry.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Registry for resolving event effects by aggregate type name.
/// </summary>
public interface IEffectRegistry
{
    /// <summary>
    ///     Gets effects registered for the specified aggregate type.
    /// </summary>
    IEnumerable<object> GetEffects(string aggregateTypeName);
}
```

**File:** `src/EventSourcing.Aggregates/EffectRegistry.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Default implementation of <see cref="IEffectRegistry"/>.
/// </summary>
public sealed class EffectRegistry : IEffectRegistry
{
    private readonly IServiceProvider serviceProvider;
    private readonly ConcurrentDictionary<string, Type> aggregateTypeCache = new();

    public EffectRegistry(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IEnumerable<object> GetEffects(string aggregateTypeName)
    {
        // Build generic type IEventEffect<TAggregate> and resolve from DI
        // Cache type lookups for performance
        var aggregateType = aggregateTypeCache.GetOrAdd(
            aggregateTypeName,
            name => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == name)
                ?? throw new InvalidOperationException($"Aggregate type '{name}' not found"));

        var effectType = typeof(IEventEffect<>).MakeGenericType(aggregateType);
        return serviceProvider.GetServices(effectType).Cast<object>();
    }
}
```

### Step 2.3: Add AggregateCommandGateway (Saga Support)

**File:** `src/EventSourcing.Aggregates/AggregateCommandGateway.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Gateway for sending commands to aggregates from effects.
/// </summary>
public sealed class AggregateCommandGateway : IAggregateCommandGateway
{
    private IGrainFactory GrainFactory { get; }

    public AggregateCommandGateway(IGrainFactory grainFactory)
    {
        GrainFactory = grainFactory;
    }

    public async Task<TResult> SendCommandAsync<TAggregate, TResult>(
        string aggregateKey,
        ICommand<TAggregate, TResult> command,
        CancellationToken cancellationToken = default)
    {
        var grain = GrainFactory.GetGrain<IGenericAggregateGrain<TAggregate>>(aggregateKey);
        return await grain.ExecuteCommandAsync(command, cancellationToken);
    }

    public async Task SendCommandAsync<TAggregate>(
        string aggregateKey,
        ICommand<TAggregate, Unit> command,
        CancellationToken cancellationToken = default)
    {
        var grain = GrainFactory.GetGrain<IGenericAggregateGrain<TAggregate>>(aggregateKey);
        await grain.ExecuteCommandAsync(command, cancellationToken);
    }
}
```

### Step 2.4: Add Effect Registration Methods

**File:** `src/EventSourcing.Aggregates/AggregateRegistrations.cs` (extend existing)

Add methods:
```csharp
public static IServiceCollection AddEventEffect<TEvent, TAggregate, TEffect>(
    this IServiceCollection services
)
    where TEffect : class, IEventEffect<TEvent, TAggregate>
{
    services.AddTransient<IEventEffect<TAggregate>, TEffect>();
    services.AddTransient<IEventEffect<TEvent, TAggregate>, TEffect>();
    return services;
}

public static IServiceCollection AddEffectInfrastructure(
    this IServiceCollection services
)
{
    services.TryAddSingleton<IEffectRegistry, EffectRegistry>();
    services.TryAddSingleton<IAggregateCommandGateway, AggregateCommandGateway>();
    return services;
}
```

### Step 2.5: Modify GenericAggregateGrain

**File:** `src/EventSourcing.Aggregates/GenericAggregateGrain.cs`

Changes:
1. After `AppendEventsAsync()` succeeds, dispatch effects via `IEffectDispatcherGrain`
2. Use fire-and-forget (`[OneWay]` + `_ =`) pattern (matching snapshot persistence)

```csharp
// After events persisted and reduced
if (events.Count > 0)
{
    // Fire-and-forget: dispatcher grain is [StatelessWorker] with [OneWay]
    var dispatcherGrain = GrainFactory.GetGrain<IEffectDispatcherGrain>(Guid.NewGuid().ToString());
    _ = dispatcherGrain.DispatchAsync(
        typeof(TAggregate).FullName!,
        this.GetPrimaryKeyString(),
        events,
        CancellationToken.None  // Don't pass caller's token to fire-and-forget
    );
}
```

### Step 2.6: Add Logger Extensions

**File:** `src/EventSourcing.Aggregates/EffectDispatcherLoggerExtensions.cs`

Add logging methods for:
- `DispatchingEffects(aggregateType, aggregateKey, eventCount)`
- `EffectMatched(effectType, eventType)`  
- `EffectCompleted(effectType, durationMs)`
- `EffectFailed(effectType, aggregateKey, exception)`

## Phase 3: Source Generator Updates

### Step 3.1: Update AggregateSiloRegistrationGenerator

**File:** `src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs`

Changes:
1. Add `FindEffectsForAggregate()` method (similar to handlers/reducers)
   - Look in `{AggregateNamespace}.Effects` namespace
   - Find types extending `EventEffectBase<,>` with matching aggregate type
2. Add `EffectInfo` record to track discovered effects
3. Update `AggregateRegistrationInfo` to include effects list
4. Update `GenerateRegistration()` to emit effect registrations:
   ```csharp
   // Register effects for side operations
   foreach (EffectInfo effect in aggregate.Effects)
   {
       sb.AppendLine($"services.AddEventEffect<{effect.EventTypeName}, {aggregate.Model.TypeName}, {effect.TypeName}>();");
   }
   ```

### Step 3.2: Add EffectInfo Record

```csharp
private sealed record EffectInfo(
    string FullTypeName,
    string TypeName,
    string EventFullTypeName,
    string EventTypeName
);
```

## Phase 4: Sample Implementation

### Step 4.1: Add Effects Folder to BankAccount Aggregate

**Folder:** `samples/Spring/Spring.Domain/Aggregates/BankAccount/Effects/`

### Step 4.2: Add Sample Effect

**File:** `samples/Spring/Spring.Domain/Aggregates/BankAccount/Effects/AccountOpenedEffect.cs`

```csharp
namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Effect that runs when an account is opened.
/// </summary>
internal sealed class AccountOpenedEffect : EventEffectBase<AccountOpened, BankAccountAggregate>
{
    private ILogger<AccountOpenedEffect> Logger { get; }

    public AccountOpenedEffect(ILogger<AccountOpenedEffect> logger)
    {
        Logger = logger;
    }

    protected override Task HandleAsync(
        AccountOpened eventData,
        string aggregateKey,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation(
            "Account opened for {HolderName} with initial deposit {InitialDeposit}. Key: {AggregateKey}",
            eventData.HolderName,
            eventData.InitialDeposit,
            aggregateKey);
        return Task.CompletedTask;
    }
}
```

## Phase 5: Testing

### Step 5.1: Unit Tests for RootEventEffectDispatcher

**File:** `tests/EventSourcing.Aggregates.L0Tests/RootEventEffectDispatcherTests.cs`

Test cases:
- Single effect handles single event
- Multiple effects handle same event
- Effect exception doesn't stop other effects
- Effect that doesn't match event is skipped
- Empty events list completes immediately
- Cancellation token is respected

### Step 5.2: Unit Tests for EventEffectBase

**File:** `tests/EventSourcing.Aggregates.Abstractions.L0Tests/EventEffectBaseTests.cs`

Test cases:
- CanHandle returns true for matching event type
- CanHandle returns false for non-matching type
- HandleAsync routes to typed method
- HandleAsync returns completed task for non-matching type

### Step 5.3: Generator Tests

**File:** `tests/Inlet.Silo.Generators.L0Tests/AggregateSiloRegistrationGeneratorTests.cs`

Add test cases:
- Generator discovers effects in Effects sub-namespace
- Generator emits AddEventEffect calls
- Generator handles aggregates without effects

### Step 5.4: Integration Tests

**File:** `tests/EventSourcing.Aggregates.L2Tests/EffectIntegrationTests.cs`

Test cases:
- Effect is triggered after command execution
- Effect receives correct event data and aggregate key
- Multiple effects for same event all run
- Effect failure doesn't fail command

## Phase 6: Documentation

### Step 6.1: Update README

Add section on event effects to aggregate documentation.

### Step 6.2: XML Documentation

Ensure all public APIs have complete XML documentation.

## Validation Checklist

- [ ] All new code has XML documentation
- [ ] Unit tests pass with >80% coverage
- [ ] Mutation tests maintain or improve score
- [ ] Build completes with zero warnings
- [ ] Source generator produces valid code
- [ ] Sample project compiles and runs
- [ ] Effect is triggered in sample when account opened

## Rollout Plan

1. Implement abstractions (backwards compatible)
2. Implement dispatcher (backwards compatible, opt-in)
3. Update grain (backwards compatible, dispatcher is optional)
4. Update generator (backwards compatible, effects are optional)
5. Add sample (demonstrates feature)
6. Document

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Effect slows down command response | Use fire-and-forget by default |
| Effect failure surfaces as command failure | Catch exceptions in dispatcher |
| Generator breaks existing projects | Effects are opt-in; no effects = no change |
| Circular dependency in DI | Effects are transient, resolved at dispatch time |

## Resolved Design Decisions

1. **Fire-and-forget vs awaited effects**
   - **RESOLVED:** Fire-and-forget via `[StatelessWorker]` grain with `[OneWay]`
   - Matches existing pattern in `ISnapshotPersisterGrain`
   - Preserves aggregate throughput

2. **Effect execution context**
   - **RESOLVED:** Effects run in `EffectDispatcherGrain` (stateless worker)
   - Aggregate grain returns immediately after dispatching
   - Effects can await external calls without blocking aggregate

3. **Cross-aggregate communication (Saga support)**
   - **RESOLVED:** Effects inject `IAggregateCommandGateway`
   - Enables saga patterns where effects orchestrate multi-aggregate workflows
   - Gateway wraps `IGrainFactory` to send commands to other aggregates

4. **Effect ordering**
   - Current: No guaranteed order (parallel-safe implementation)
   - Future: Could add priority attribute if needed

5. **Effect retry/resilience**
   - Current: No retry; effects handle their own resilience via DI (Polly, etc.)
   - Future: Could add optional retry wrapper

6. **Client-side naming change (ActionEffect)**
   - Defer to separate PR to avoid scope creep
