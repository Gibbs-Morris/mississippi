# Implementation Plan v2

**Updated based on persona review feedback (2026-01-24)**

## Changes from v1

| Area | v1 | v2 | Rationale |
|------|----|----|-----------|
| Effect resolution | `IEffectRegistry` (runtime reflection) | **Source generator** | Matches existing patterns; AOT-compatible |
| Effect context | `aggregateKey` string only | **`EffectContext`** record | Richer metadata (correlation, position, timestamp) |
| Observability | Minimal logging | **Full observability** | Metrics, structured logs, trace propagation |
| Idempotency | Not addressed | **Guidance + optional base class** | DevOps concern for pod restarts |
| Graceful shutdown | Not addressed | **`OnDeactivateAsync` handling** | Cancel in-flight effects |
| Documentation | Basic | **Comprehensive** (including anti-patterns) | Dev Manager requirement |

## Size Assessment: Large

- Multiple new types across abstractions and implementations
- New grain (`IEffectDispatcherGrain`) for throughput isolation
- Source generator modifications (discovery + registration)
- Changes to GenericAggregateGrain
- New folder convention in domain projects
- Comprehensive tests required
- Documentation required

---

## Phase 1: Abstractions (EventSourcing.Aggregates.Abstractions)

### Step 1.1: Add EffectContext Record

**File:** `src/EventSourcing.Aggregates.Abstractions/EffectContext.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Context information provided to event effects.
/// </summary>
/// <param name="AggregateKey">The aggregate key (brook key).</param>
/// <param name="AggregateTypeName">The full type name of the aggregate.</param>
/// <param name="BrookName">The brook name where events were persisted.</param>
/// <param name="EventPosition">The position of the first event in this batch.</param>
/// <param name="Timestamp">When the events were persisted (UTC).</param>
/// <param name="CorrelationId">Correlation ID for distributed tracing.</param>
/// <param name="CausationId">The command/operation that caused these events.</param>
[GenerateSerializer]
[Immutable]
public sealed record EffectContext(
    string AggregateKey,
    string AggregateTypeName,
    string BrookName,
    long EventPosition,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    string? CausationId
);
```

### Step 1.2: Add IEventEffect Interface

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
    /// <param name="eventData">The event that was persisted.</param>
    /// <param name="context">Context information about the aggregate and event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(
        object eventData,
        EffectContext context,
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
        EffectContext context,
        CancellationToken cancellationToken
    );
}
```

### Step 1.3: Add EventEffectBase

**File:** `src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for event effects that handle a specific event type.
/// </summary>
public abstract class EventEffectBase<TEvent, TAggregate> : IEventEffect<TEvent, TAggregate>
{
    /// <inheritdoc />
    public bool CanHandle(object eventData) => eventData is TEvent;

    /// <inheritdoc />
    public Task HandleAsync(
        object eventData,
        EffectContext context,
        CancellationToken cancellationToken)
    {
        if (eventData is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, context, cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles the event asynchronously.
    /// </summary>
    /// <param name="eventData">The typed event that was persisted.</param>
    /// <param name="context">Context information about the aggregate and event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract Task HandleAsync(
        TEvent eventData,
        EffectContext context,
        CancellationToken cancellationToken
    );
}
```

### Step 1.4: Add IdempotentEffectBase (Optional Helper)

**File:** `src/EventSourcing.Aggregates.Abstractions/IdempotentEffectBase.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for idempotent effects that track processed event positions.
/// </summary>
/// <remarks>
///     <para>
///         Use this base class when your effect has side effects that should not be
///         repeated if the effect runs again for the same event (e.g., pod restart
///         during effect execution).
///     </para>
///     <para>
///         Implement <see cref="HasBeenProcessedAsync"/> to check if this event
///         position has already been handled, and <see cref="MarkProcessedAsync"/>
///         to record that it has been handled.
///     </para>
/// </remarks>
public abstract class IdempotentEffectBase<TEvent, TAggregate> : EventEffectBase<TEvent, TAggregate>
{
    /// <inheritdoc />
    public sealed override async Task HandleAsync(
        TEvent eventData,
        EffectContext context,
        CancellationToken cancellationToken)
    {
        // Build idempotency key from aggregate + position
        var idempotencyKey = BuildIdempotencyKey(context);
        
        if (await HasBeenProcessedAsync(idempotencyKey, cancellationToken))
        {
            return; // Already processed, skip
        }

        await HandleIdempotentAsync(eventData, context, cancellationToken);
        await MarkProcessedAsync(idempotencyKey, cancellationToken);
    }

    /// <summary>
    ///     Builds the idempotency key for this effect execution.
    /// </summary>
    protected virtual string BuildIdempotencyKey(EffectContext context) =>
        $"{GetType().FullName}:{context.BrookName}:{context.EventPosition}";

    /// <summary>
    ///     Checks if the effect has already been processed for this key.
    /// </summary>
    protected abstract Task<bool> HasBeenProcessedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Marks the effect as processed for this key.
    /// </summary>
    protected abstract Task MarkProcessedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Handles the event with idempotency guarantee.
    /// </summary>
    protected abstract Task HandleIdempotentAsync(
        TEvent eventData,
        EffectContext context,
        CancellationToken cancellationToken);
}
```

### Step 1.5: Add IEffectDispatcherGrain Interface

**File:** `src/EventSourcing.Aggregates.Abstractions/IEffectDispatcherGrain.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Stateless worker grain that dispatches event effects asynchronously.
/// </summary>
/// <remarks>
///     <para>
///         This grain receives fire-and-forget calls via [OneWay], allowing the
///         aggregate grain to return immediately while effects run in the background.
///     </para>
///     <para>
///         <b>Delivery guarantee:</b> Effects have at-most-once semantics. If the
///         silo crashes during effect execution, the effect will not be retried.
///         For effects requiring guaranteed execution, use <see cref="IdempotentEffectBase{TEvent,TAggregate}"/>
///         or implement your own outbox/retry pattern.
///     </para>
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
        EffectContext context,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken = default
    );
}
```

### ~~Step 1.6: Add IAggregateCommandGateway Interface~~

**DECISION NEEDED:** See "Conflicts Requiring Input" section below.

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
    private EffectDispatcherMetrics Metrics { get; }
    private CancellationTokenSource? ShutdownCts { get; set; }

    public EffectDispatcherGrain(
        IServiceProvider serviceProvider,
        ILogger<EffectDispatcherGrain> logger,
        EffectDispatcherMetrics metrics)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        Metrics = metrics;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ShutdownCts = new CancellationTokenSource();
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // Cancel in-flight effects on graceful shutdown
        ShutdownCts?.Cancel();
        ShutdownCts?.Dispose();
        return Task.CompletedTask;
    }

    public async Task DispatchAsync(
        EffectContext context,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken = default)
    {
        using var activity = EffectDispatcherDiagnostics.StartDispatch(context);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            ShutdownCts?.Token ?? CancellationToken.None);

        Metrics.RecordDispatch(context.AggregateTypeName, events.Count);

        // Resolve effects by aggregate type name from DI
        // Effects are registered as IEventEffect<TAggregateType> by source generator
        var effects = ResolveEffects(context.AggregateTypeName);

        foreach (var eventData in events)
        {
            foreach (var effect in effects)
            {
                if (!effect.CanHandle(eventData))
                {
                    continue;
                }

                var effectTypeName = effect.GetType().Name;
                var eventTypeName = eventData.GetType().Name;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    Logger.EffectExecuting(effectTypeName, eventTypeName, context.AggregateKey);
                    
                    await effect.HandleAsync(eventData, context, linkedCts.Token);
                    
                    stopwatch.Stop();
                    Logger.EffectCompleted(effectTypeName, eventTypeName, context.AggregateKey, stopwatch.ElapsedMilliseconds);
                    Metrics.RecordSuccess(effectTypeName, stopwatch.Elapsed);
                }
                catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
                {
                    Logger.EffectCancelled(effectTypeName, context.AggregateKey);
                    Metrics.RecordCancellation(effectTypeName);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    Logger.EffectFailed(effectTypeName, context.AggregateKey, ex);
                    Metrics.RecordError(effectTypeName, stopwatch.Elapsed);
                    // Continue with other effects - don't let one failure stop others
                }
            }
        }
    }

    private IEnumerable<object> ResolveEffects(string aggregateTypeName)
    {
        // Source generator registers effects with keyed services:
        // services.AddKeyedTransient<IEventEffect<TAggregate>>(aggregateTypeName, effect);
        // This allows us to resolve by type name without runtime reflection
        return ServiceProvider.GetKeyedServices<object>($"EventEffects:{aggregateTypeName}");
    }
}
```

### Step 2.2: Add Observability Infrastructure

**File:** `src/EventSourcing.Aggregates/Observability/EffectDispatcherMetrics.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Observability;

/// <summary>
///     Metrics for effect dispatcher.
/// </summary>
public sealed class EffectDispatcherMetrics
{
    private readonly Counter<long> dispatchCounter;
    private readonly Counter<long> successCounter;
    private readonly Counter<long> errorCounter;
    private readonly Counter<long> cancellationCounter;
    private readonly Histogram<double> durationHistogram;

    public EffectDispatcherMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Mississippi.EventSourcing.Aggregates");
        
        dispatchCounter = meter.CreateCounter<long>(
            "effect.dispatched.total",
            description: "Total number of effect dispatch calls");
        
        successCounter = meter.CreateCounter<long>(
            "effect.execution.success",
            description: "Total successful effect executions");
        
        errorCounter = meter.CreateCounter<long>(
            "effect.execution.errors",
            description: "Total failed effect executions");
        
        cancellationCounter = meter.CreateCounter<long>(
            "effect.execution.cancelled",
            description: "Total cancelled effect executions");
        
        durationHistogram = meter.CreateHistogram<double>(
            "effect.execution.duration",
            unit: "ms",
            description: "Effect execution duration in milliseconds");
    }

    public void RecordDispatch(string aggregateType, int eventCount) =>
        dispatchCounter.Add(1, new("aggregate.type", aggregateType), new("event.count", eventCount));

    public void RecordSuccess(string effectType, TimeSpan duration)
    {
        successCounter.Add(1, new("effect.type", effectType));
        durationHistogram.Record(duration.TotalMilliseconds, new("effect.type", effectType), new("outcome", "success"));
    }

    public void RecordError(string effectType, TimeSpan duration)
    {
        errorCounter.Add(1, new("effect.type", effectType));
        durationHistogram.Record(duration.TotalMilliseconds, new("effect.type", effectType), new("outcome", "error"));
    }

    public void RecordCancellation(string effectType) =>
        cancellationCounter.Add(1, new("effect.type", effectType));
}
```

**File:** `src/EventSourcing.Aggregates/Observability/EffectDispatcherDiagnostics.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates.Observability;

/// <summary>
///     Distributed tracing support for effect dispatcher.
/// </summary>
public static class EffectDispatcherDiagnostics
{
    private static readonly ActivitySource ActivitySource = new("Mississippi.EventSourcing.Aggregates.Effects");

    public static Activity? StartDispatch(EffectContext context)
    {
        var activity = ActivitySource.StartActivity("effect.dispatch", ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("aggregate.type", context.AggregateTypeName);
            activity.SetTag("aggregate.key", context.AggregateKey);
            activity.SetTag("brook.name", context.BrookName);
            activity.SetTag("event.position", context.EventPosition);
            
            if (context.CorrelationId is not null)
            {
                activity.SetTag("correlation.id", context.CorrelationId);
            }
        }
        return activity;
    }
}
```

### Step 2.3: Add Logger Extensions

**File:** `src/EventSourcing.Aggregates/EffectDispatcherLoggerExtensions.cs`

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Logger extensions for effect dispatcher.
/// </summary>
internal static partial class EffectDispatcherLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Executing effect {EffectType} for event {EventType} on aggregate {AggregateKey}")]
    public static partial void EffectExecuting(
        this ILogger logger,
        string effectType,
        string eventType,
        string aggregateKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Effect {EffectType} completed for event {EventType} on aggregate {AggregateKey} in {DurationMs}ms")]
    public static partial void EffectCompleted(
        this ILogger logger,
        string effectType,
        string eventType,
        string aggregateKey,
        long durationMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Effect {EffectType} was cancelled for aggregate {AggregateKey}")]
    public static partial void EffectCancelled(
        this ILogger logger,
        string effectType,
        string aggregateKey);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Effect {EffectType} failed for aggregate {AggregateKey}")]
    public static partial void EffectFailed(
        this ILogger logger,
        string effectType,
        string aggregateKey,
        Exception exception);
}
```

### Step 2.4: Add Effect Registration Methods

**File:** `src/EventSourcing.Aggregates/AggregateRegistrations.cs` (extend existing)

Add methods:
```csharp
/// <summary>
///     Registers an event effect for an aggregate using keyed services.
/// </summary>
/// <remarks>
///     <para>
///         This method is called by the source generator. Do not call directly.
///     </para>
///     <para>
///         Effects are registered with a keyed service key of "EventEffects:{AggregateTypeName}"
///         to allow resolution by aggregate type name at runtime without reflection.
///     </para>
/// </remarks>
public static IServiceCollection AddEventEffect<TEvent, TAggregate, TEffect>(
    this IServiceCollection services
)
    where TEffect : class, IEventEffect<TEvent, TAggregate>
{
    // Register as keyed service for resolution by aggregate type name
    var key = $"EventEffects:{typeof(TAggregate).FullName}";
    services.AddKeyedTransient<object, TEffect>(key);
    
    // Also register typed for direct injection if needed
    services.AddTransient<IEventEffect<TAggregate>, TEffect>();
    services.AddTransient<IEventEffect<TEvent, TAggregate>, TEffect>();
    return services;
}

/// <summary>
///     Adds effect dispatcher infrastructure.
/// </summary>
public static IServiceCollection AddEffectInfrastructure(
    this IServiceCollection services
)
{
    services.TryAddSingleton<EffectDispatcherMetrics>();
    return services;
}
```

### Step 2.5: Modify GenericAggregateGrain

**File:** `src/EventSourcing.Aggregates/GenericAggregateGrain.cs`

Changes:
1. After `AppendEventsAsync()` succeeds, build `EffectContext` and dispatch effects
2. Use fire-and-forget (`[OneWay]` + `_ =`) pattern (matching snapshot persistence)
3. Include correlation/causation IDs for tracing

```csharp
// After events persisted and reduced
if (events.Count > 0)
{
    // Build effect context with tracing information
    var effectContext = new EffectContext(
        AggregateKey: this.GetPrimaryKeyString(),
        AggregateTypeName: typeof(TAggregate).FullName!,
        BrookName: brookName,
        EventPosition: appendResult.StartPosition,
        Timestamp: DateTimeOffset.UtcNow,
        CorrelationId: Activity.Current?.Id,
        CausationId: command.GetType().Name
    );

    // Fire-and-forget: dispatcher grain is [StatelessWorker] with [OneWay]
    var dispatcherGrain = GrainFactory.GetGrain<IEffectDispatcherGrain>(Guid.NewGuid().ToString());
    _ = dispatcherGrain.DispatchAsync(effectContext, events, CancellationToken.None);
}
```

---

## Phase 3: Source Generator Updates

### Step 3.1: Update AggregateSiloRegistrationGenerator

**File:** `src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs`

Changes:
1. Add `FindEffectsForAggregate()` method (matching handlers/reducers pattern)
   - Look in `{AggregateNamespace}.Effects` namespace
   - Find types extending `EventEffectBase<,>` with matching aggregate type
2. Add `EffectInfo` record to track discovered effects
3. Update `AggregateRegistrationInfo` to include effects list
4. Update `GenerateRegistration()` to emit effect registrations

### Step 3.2: Add EffectInfo Record

```csharp
private sealed record EffectInfo(
    string FullTypeName,
    string TypeName,
    string EventFullTypeName,
    string EventTypeName
);
```

### Step 3.3: Add FindEffectsForAggregate Method

```csharp
/// <summary>
///     Finds effects for an aggregate in the Effects sub-namespace.
/// </summary>
private static List<EffectInfo> FindEffectsForAggregate(
    INamespaceSymbol aggregateNamespace,
    INamedTypeSymbol aggregateSymbol,
    INamedTypeSymbol? effectBaseSymbol
)
{
    List<EffectInfo> effects = [];
    if (effectBaseSymbol is null)
    {
        return effects;
    }

    // Look for Effects sub-namespace
    INamespaceSymbol? effectsNs = aggregateNamespace.GetNamespaceMembers()
        .FirstOrDefault(ns => ns.Name == "Effects");
    if (effectsNs is null)
    {
        return effects;
    }

    // Find all types that extend EventEffectBase<TEvent, TAggregate>
    foreach (INamedTypeSymbol typeSymbol in effectsNs.GetTypeMembers())
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        if (baseType is null || !baseType.IsGenericType)
        {
            continue;
        }

        // Check if it extends EventEffectBase<,>
        INamedTypeSymbol? constructedFrom = baseType.ConstructedFrom;
        if (constructedFrom is null ||
            (constructedFrom.MetadataName != "EventEffectBase`2") ||
            (constructedFrom.ContainingNamespace.ToDisplayString() !=
             "Mississippi.EventSourcing.Aggregates.Abstractions"))
        {
            continue;
        }

        // Verify the second type argument is our aggregate
        if ((baseType.TypeArguments.Length != 2) ||
            !SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[1], aggregateSymbol))
        {
            continue;
        }

        // Extract event type
        ITypeSymbol eventType = baseType.TypeArguments[0];
        effects.Add(
            new(typeSymbol.ToDisplayString(), typeSymbol.Name, eventType.ToDisplayString(), eventType.Name));
    }

    return effects;
}
```

### Step 3.4: Update GenerateRegistration to Emit Effects

```csharp
// In GenerateRegistration method, after reducers:

// Add using for effects namespace
string effectsNamespace = aggregate.Model.Namespace + ".Effects";
sb.AppendUsing(effectsNamespace);

// ... later in the method body:

// Register effects for side operations
if (aggregate.Effects.Count > 0)
{
    sb.AppendLine();
    sb.AppendLine("// Register effects for side operations");
    foreach (EffectInfo effect in aggregate.Effects)
    {
        sb.AppendLine($"services.AddEventEffect<{effect.EventTypeName}, {aggregate.Model.TypeName}, {effect.TypeName}>();");
    }
}

// Add effect infrastructure
sb.AppendLine();
sb.AppendLine("// Add effect infrastructure");
sb.AppendLine("services.AddEffectInfrastructure();");
```

---

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
/// <remarks>
///     This is a sample effect demonstrating the pattern. In a real application,
///     this might send a welcome email, create an audit log entry, or notify
///     an external system.
/// </remarks>
internal sealed class AccountOpenedEffect : EventEffectBase<AccountOpened, BankAccountAggregate>
{
    private ILogger<AccountOpenedEffect> Logger { get; }

    public AccountOpenedEffect(ILogger<AccountOpenedEffect> logger)
    {
        Logger = logger;
    }

    public override Task HandleAsync(
        AccountOpened eventData,
        EffectContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Account opened for {HolderName} with initial deposit {InitialDeposit}. " +
            "Key: {AggregateKey}, Position: {Position}, CorrelationId: {CorrelationId}",
            eventData.HolderName,
            eventData.InitialDeposit,
            context.AggregateKey,
            context.EventPosition,
            context.CorrelationId);

        return Task.CompletedTask;
    }
}
```

### Step 4.3: Add Sample Idempotent Effect (Optional)

**File:** `samples/Spring/Spring.Domain/Aggregates/BankAccount/Effects/LargeDepositAlertEffect.cs`

```csharp
namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Effect that sends an alert for large deposits.
/// </summary>
/// <remarks>
///     This demonstrates the idempotent effect pattern for effects that
///     should not run twice for the same event.
/// </remarks>
internal sealed class LargeDepositAlertEffect : IdempotentEffectBase<Deposited, BankAccountAggregate>
{
    private const decimal LargeDepositThreshold = 10000m;
    
    private ILogger<LargeDepositAlertEffect> Logger { get; }
    private IIdempotencyStore IdempotencyStore { get; }

    public LargeDepositAlertEffect(
        ILogger<LargeDepositAlertEffect> logger,
        IIdempotencyStore idempotencyStore)
    {
        Logger = logger;
        IdempotencyStore = idempotencyStore;
    }

    protected override Task<bool> HasBeenProcessedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        IdempotencyStore.ExistsAsync(idempotencyKey, cancellationToken);

    protected override Task MarkProcessedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        IdempotencyStore.StoreAsync(idempotencyKey, TimeSpan.FromDays(7), cancellationToken);

    protected override Task HandleIdempotentAsync(
        Deposited eventData,
        EffectContext context,
        CancellationToken cancellationToken)
    {
        if (eventData.Amount < LargeDepositThreshold)
        {
            return Task.CompletedTask;
        }

        Logger.LogWarning(
            "Large deposit detected: {Amount} on account {AggregateKey}",
            eventData.Amount,
            context.AggregateKey);

        // In a real app: call fraud detection API, send notification, etc.
        return Task.CompletedTask;
    }
}
```

---

## Phase 5: Testing

### Step 5.1: Unit Tests for EffectDispatcherGrain

**File:** `tests/EventSourcing.Aggregates.L0Tests/EffectDispatcherGrainTests.cs`

Test cases:
- Single effect handles single event
- Multiple effects handle same event
- Effect exception doesn't stop other effects
- Effect that doesn't match event is skipped
- Empty events list completes immediately
- Cancellation token is respected
- Graceful shutdown cancels in-flight effects
- Metrics are recorded correctly

### Step 5.2: Unit Tests for EventEffectBase

**File:** `tests/EventSourcing.Aggregates.Abstractions.L0Tests/EventEffectBaseTests.cs`

Test cases:
- CanHandle returns true for matching event type
- CanHandle returns false for non-matching type
- HandleAsync routes to typed method with context
- HandleAsync returns completed task for non-matching type

### Step 5.3: Unit Tests for IdempotentEffectBase

**File:** `tests/EventSourcing.Aggregates.Abstractions.L0Tests/IdempotentEffectBaseTests.cs`

Test cases:
- Effect runs when not previously processed
- Effect skips when previously processed
- Effect marks as processed after successful run
- Idempotency key includes effect type, brook, and position

### Step 5.4: Generator Tests

**File:** `tests/Inlet.Silo.Generators.L0Tests/AggregateSiloRegistrationGeneratorTests.cs`

Add test cases:
- Generator discovers effects in Effects sub-namespace
- Generator emits AddEventEffect calls with correct types
- Generator handles aggregates without effects (no effect code emitted)
- Generator includes effects namespace in usings

### Step 5.5: Integration Tests

**File:** `tests/EventSourcing.Aggregates.L2Tests/EffectIntegrationTests.cs`

Test cases:
- Effect is triggered after command execution
- Effect receives correct context (aggregateKey, position, brookName)
- Multiple effects for same event all run
- Effect failure doesn't fail command
- Effect receives correlation ID from activity

---

## Phase 6: Documentation

### Step 6.1: Effects Guide

**File:** `docs/Docusaurus/docs/guides/effects.md`

Contents:
- What are effects?
- When to use effects
- Creating an effect (step-by-step)
- Folder structure convention
- Accessing context information
- Dependency injection in effects
- Error handling

### Step 6.2: Testing Effects Guide

**File:** `docs/Docusaurus/docs/guides/testing-effects.md`

Contents:
- Unit testing effects in isolation
- Mocking dependencies
- Testing idempotent effects
- Integration testing with Orleans

### Step 6.3: Anti-Patterns Documentation

**File:** `docs/Docusaurus/docs/guides/effects-anti-patterns.md`

Contents:
- ❌ Modifying aggregate state in effects
- ❌ Relying on effect execution for correctness
- ❌ Long-running synchronous work in effects
- ❌ Swallowing exceptions without logging
- ❌ Assuming effects run exactly once (at-most-once semantics)
- ❌ Using fire-and-forget for critical operations without idempotency

### Step 6.4: Idempotency Patterns

**File:** `docs/Docusaurus/docs/guides/effect-idempotency.md`

Contents:
- Understanding at-most-once semantics
- When to use IdempotentEffectBase
- Implementing idempotency stores
- Idempotency key design

### Step 6.5: XML Documentation

Ensure all public APIs have complete XML documentation with:
- Summary
- Remarks with usage guidance
- Param descriptions
- Example code where helpful

---

## Validation Checklist

- [ ] All new code has XML documentation
- [ ] Unit tests pass with >80% coverage
- [ ] Mutation tests maintain or improve score
- [ ] Build completes with zero warnings
- [ ] Source generator produces valid code
- [ ] Sample project compiles and runs
- [ ] Effect is triggered in sample when account opened
- [ ] Metrics are visible in OpenTelemetry
- [ ] Logs include correlation IDs
- [ ] Graceful shutdown tested (effect cancellation)
- [ ] Documentation reviewed

---

## Conflicts Requiring Input

### IAggregateCommandGateway Scope

**Current plan:** Include `IAggregateCommandGateway` to enable saga patterns

**Architect feedback:** Defer to separate saga PR (YAGNI - You Aren't Gonna Need It)

**Options:**
1. **Include in V1** - Enables unified mental model where effects can orchestrate
2. **Defer to saga PR** - Reduces scope, follows YAGNI, avoids potential misuse

**Question for user:** Do you have a near-term saga use case that requires `IAggregateCommandGateway`? Or should we defer it to keep scope smaller?

---

## Rollout Plan

1. Implement abstractions (backwards compatible)
2. Implement dispatcher grain with observability (backwards compatible, opt-in)
3. Update source generator (backwards compatible, effects are optional)
4. Update GenericAggregateGrain to dispatch effects (backwards compatible)
5. Add sample (demonstrates feature)
6. Write documentation
7. Review and merge

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Effect slows down command response | Fire-and-forget by default |
| Effect failure surfaces as command failure | Catch exceptions in dispatcher |
| Generator breaks existing projects | Effects are opt-in; no effects = no change |
| Pod crash loses in-flight effects | Document at-most-once; provide IdempotentEffectBase |
| Duplicate effect execution | IdempotentEffectBase pattern |
| Hard to debug fire-and-forget | Full observability (metrics, logs, traces) |
