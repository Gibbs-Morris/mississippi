# Implementation Plan

## Overview

Add server-side event effects to the aggregate system, allowing users to define asynchronous side effects that run after events are persisted.

## Size Assessment: Large

- Multiple new types across abstractions and implementations
- Source generator modifications
- Changes to GenericAggregateGrain
- New folder convention in domain projects
- Comprehensive tests required

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

    protected abstract Task HandleAsync(
        TEvent eventData,
        string aggregateKey,
        CancellationToken cancellationToken
    );
}
```

### Step 1.3: Add IRootEventEffectDispatcher Interface

**File:** `src/EventSourcing.Aggregates.Abstractions/IRootEventEffectDispatcher.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Dispatches domain events to registered event effects.
/// </summary>
public interface IRootEventEffectDispatcher<TAggregate>
{
    /// <summary>
    ///     Dispatches events to applicable effects.
    /// </summary>
    Task DispatchAsync(
        IReadOnlyList<object> events,
        string aggregateKey,
        CancellationToken cancellationToken
    );
}
```

## Phase 2: Implementation (EventSourcing.Aggregates)

### Step 2.1: Add RootEventEffectDispatcher

**File:** `src/EventSourcing.Aggregates/RootEventEffectDispatcher.cs`

- Implement `IRootEventEffectDispatcher<TAggregate>`
- Inject `IEnumerable<IEventEffect<TAggregate>>`
- Build frozen dictionary index by event type (like RootReducer)
- Iterate events, find matching effects, await each effect
- Catch and log exceptions per effect (don't let one failure stop others)

### Step 2.2: Add Effect Registration Methods

**File:** `src/EventSourcing.Aggregates/AggregateRegistrations.cs`

Add methods:
```csharp
public static IServiceCollection AddEventEffect<TEvent, TAggregate, TEffect>(
    this IServiceCollection services
)
    where TEffect : class, IEventEffect<TEvent, TAggregate>
{
    services.AddTransient<IEventEffect<TAggregate>, TEffect>();
    services.AddTransient<IEventEffect<TEvent, TAggregate>, TEffect>();
    services.AddRootEventEffectDispatcher<TAggregate>();
    return services;
}

public static IServiceCollection AddRootEventEffectDispatcher<TAggregate>(
    this IServiceCollection services
)
{
    services.TryAddTransient<IRootEventEffectDispatcher<TAggregate>, RootEventEffectDispatcher<TAggregate>>();
    return services;
}
```

### Step 2.3: Modify GenericAggregateGrain

**File:** `src/EventSourcing.Aggregates/GenericAggregateGrain.cs`

Changes:
1. Inject `IRootEventEffectDispatcher<TAggregate>` (optional - may not be registered)
2. After `AppendEventsAsync()` succeeds, call dispatcher
3. Use fire-and-forget or timeout pattern to avoid blocking

```csharp
// After events persisted successfully
if (events.Count > 0 && RootEventEffectDispatcher is not null)
{
    // Fire-and-forget with error handling
    _ = DispatchEffectsAsync(events, brookKey, cancellationToken);
}
```

**Decision needed:** Should this be fire-and-forget or awaited?
- Proposal: Add configuration option, default to fire-and-forget for performance

### Step 2.4: Add Logger Extensions

**File:** `src/EventSourcing.Aggregates/RootEventEffectDispatcherLoggerExtensions.cs`

Add logging methods for:
- Effect dispatching started
- Effect matched
- Effect completed
- Effect failed (with exception)

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

## Open Design Decisions

1. **Fire-and-forget vs awaited effects**
   - Current proposal: fire-and-forget by default
   - Could add: `[AwaitEffect]` attribute or configuration

2. **Effect ordering**
   - Current proposal: no guaranteed order
   - Could add: priority attribute

3. **Effect retry/resilience**
   - Current proposal: no retry, effects handle their own resilience
   - Could add: integration with Polly via DI

4. **Client-side naming change (ActionEffect)**
   - Defer to separate PR to avoid scope creep
