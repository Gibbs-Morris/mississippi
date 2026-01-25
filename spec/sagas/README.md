# Server-Side Sagas Design Specification

> **Status**: Design Phase  
> **Created**: 2026-01-25  
> **Branch**: `design/server-side-sagas`

This specification documents the design for server-side saga support in Mississippi.

## Overview

Sagas are event-sourced aggregates with additional orchestration capabilities for coordinating long-running, multi-step workflows across multiple aggregates and external services.

## Core Principle

**A saga IS an aggregate.** It uses the same building blocks (commands, events, reducers, effects) with additional orchestration features layered on top. This keeps the mental model consistent and allows saga infrastructure to benefit aggregates for advanced use cases.

---

## Design Decisions

### 1. Step Execution Model

**Decision**: Synchronous with optional verification

Steps wait for the aggregate command to return, then optionally run a **verification function** before marking complete.

**Rationale**: 
- Commands succeeding doesn't always mean the operation is complete (e.g., long-running external services)
- Developers need a hook to check projections, poll external state, or implement custom completion logic
- Verification enables patterns like "wait for projection to reflect expected state"

**API Shape**:
```csharp
public abstract class SagaStepBase<TSaga>
{
    /// <summary>
    ///     Execute the step action (e.g., dispatch command).
    /// </summary>
    public abstract Task<StepExecutionResult> ExecuteAsync(...);
    
    /// <summary>
    ///     Verify the step completed successfully. Called after ExecuteAsync succeeds.
    ///     Return true when verified, false to retry verification.
    /// </summary>
    public virtual Task<StepVerificationResult> VerifyAsync(
        TSaga state,
        ISagaContext context,
        CancellationToken cancellationToken)
    {
        // Default: no verification needed
        return Task.FromResult(StepVerificationResult.Verified());
    }
}

public sealed record StepVerificationResult
{
    public bool IsVerified { get; init; }
    public bool ShouldRetry { get; init; }
    public TimeSpan? RetryDelay { get; init; }
    
    public static StepVerificationResult Verified() => new() { IsVerified = true };
    public static StepVerificationResult NotYet(TimeSpan? delay = null) 
        => new() { IsVerified = false, ShouldRetry = true, RetryDelay = delay };
    public static StepVerificationResult Failed(string reason) 
        => new() { IsVerified = false, ShouldRetry = false };
}
```

**Example**: Verify projection updated
```csharp
public override async Task<StepVerificationResult> VerifyAsync(
    OrderSagaState state,
    ISagaContext context,
    CancellationToken ct)
{
    // Check if payment projection shows reserved status
    var projection = await ProjectionReader.GetAsync<PaymentProjection>(state.PaymentId, ct);
    
    if (projection?.Status == PaymentStatus.Reserved)
        return StepVerificationResult.Verified();
    
    // Not ready yet, retry in 2 seconds
    return StepVerificationResult.NotYet(TimeSpan.FromSeconds(2));
}
```

---

### 2. Compensation Trigger

**Decision**: Default to immediate compensation on failure, configurable per saga via attribute

**Rationale**:
- Immediate compensation is the safest default (fail fast, clean up)
- Some sagas may want retries before compensating
- Configuration at saga level keeps step logic simple

**API Shape**:
```csharp
[SagaOptions(
    CompensationStrategy = CompensationStrategy.Immediate,  // Default
    // Or: CompensationStrategy.RetryThenCompensate with MaxRetries = 3
    // Or: CompensationStrategy.Manual
)]
[BrookName("order-fulfillment-saga")]
public sealed record OrderFulfillmentSagaState { ... }

public enum CompensationStrategy
{
    /// <summary>First step failure triggers immediate compensation of completed steps.</summary>
    Immediate,
    
    /// <summary>Retry failed step N times before compensating.</summary>
    RetryThenCompensate,
    
    /// <summary>Only compensate when explicitly requested via CancelSagaCommand.</summary>
    Manual
}
```

---

### 3. Saga State Location

**Decision**: Store step tracking in saga state only if commands need it; projections handle external visibility

**Rationale**:
- Saga state is internal to the aggregate
- If command handlers need to make decisions based on step history, store it
- External consumers (UI, monitoring) use the `SagaStatusProjection`
- Everything flows through the event stream—projections can derive any view

**Guidance**:
- Keep saga aggregate state domain-focused
- Add step tracking fields only when command logic requires them
- The prebuilt `SagaStatusProjection` provides standard visibility without polluting domain state

---

### 4. Child Saga Completion Pattern

**Decision**: Use the same mechanism as aggregate verification (polling via reminders)

**Rationale**:
- Sagas and aggregates are the same core object
- The verification step (Decision #1) already supports polling
- Polling is essential on day one; other patterns (callbacks, streams) can be added later
- Consistent mechanism reduces cognitive load

**Implementation**:
```csharp
public sealed class InvokeChildSagaStep : SagaStepBase<ParentSagaState>
{
    public override async Task<StepExecutionResult> ExecuteAsync(...)
    {
        // Start child saga
        await ChildSagaGrain.ExecuteAsync(new StartChildCommand(correlationId));
        return StepExecutionResult.Succeeded();
    }
    
    public override async Task<StepVerificationResult> VerifyAsync(...)
    {
        // Poll child saga state
        var childState = await ChildSagaGrain.GetStateAsync();
        
        return childState.Phase switch
        {
            SagaPhase.Completed => StepVerificationResult.Verified(),
            SagaPhase.Failed => StepVerificationResult.Failed(childState.FailureReason),
            _ => StepVerificationResult.NotYet(TimeSpan.FromSeconds(5))
        };
    }
}
```

**Future**: Consider adding event-stream subscription or callback patterns as optimizations.

---

### 5. Timeout Scope

**Decision**: Configured via attribute on the saga, applies to step execution + verification

**Rationale**:
- Consistent with other saga-level configuration (Decision #2)
- Timeout behavior is cross-cutting, not step-specific logic
- Keeps step implementations focused on business logic

**API Shape**:
```csharp
[SagaOptions(
    DefaultStepTimeout = "00:05:00",  // 5 minutes default
    TimeoutBehavior = TimeoutBehavior.FailAndCompensate  // Default
    // Or: TimeoutBehavior.RetryWithBackoff
    // Or: TimeoutBehavior.AwaitIntervention
)]
[BrookName("order-fulfillment-saga")]
public sealed record OrderFulfillmentSagaState { ... }

// Per-step override via attribute
[SagaStep(Order = 2, Timeout = "00:10:00")]  // 10 min for this step
public sealed class LongRunningStep : SagaStepBase<OrderFulfillmentSagaState> { ... }
```

---

### 6. Saga Grain Architecture

**Decision**: Saga is an aggregate; add polling/verification as aggregate-level capability

**Rationale**:
- Saga = aggregate + extra bits (steps, verification, compensation)
- Polling/verification is useful for advanced aggregate use cases too
- Keep aggregates and projections as building blocks for higher patterns
- May need to inherit from aggregate grain to add reminder-based polling

**Implementation Approach**:
1. Add optional `IRemindable` support to `GenericAggregateGrain<T>` (or subclass)
2. Saga-specific behavior (step orchestration) via saga-specific command handlers and effects
3. Framework provides base commands: `StartSagaCommand`, `ContinueSagaCommand`, `TimeoutCommand`, `CancelSagaCommand`
4. Framework provides base events: `SagaStarted`, `StepCompleted`, `StepFailed`, `SagaCompleted`, `SagaFailed`

**Open Question**: Should `GenericAggregateGrain` gain optional reminder support, or should `SagaGrain` inherit and extend it?

---

### 7. Step Definition API

**Decision**: Attribute-based with source generator validation

**Rationale**:
- Consistent with existing Mississippi patterns (commands, reducers, projections)
- Source generator can detect conflicts (duplicate order, missing steps) at compile time
- Explicit ordering via `Order` property

**API Shape**:
```csharp
[SagaStep(Order = 1)]
public sealed class ReservePaymentStep : SagaStepBase<OrderSagaState> { ... }

[SagaStep(Order = 2)]
public sealed class ReserveInventoryStep : SagaStepBase<OrderSagaState> { ... }

[SagaStep(Order = 3)]
public sealed class CreateShipmentStep : SagaStepBase<OrderSagaState> { ... }
```

**Source Generator Responsibilities**:
- Validate no duplicate `Order` values for same saga
- Validate step classes implement required methods
- Generate step registry for runtime discovery
- Emit compile error on conflicts

---

### 8. Saga Versioning

**Decision**: Needs further design; consider step-based versioning with hash (similar to reducer hash)

**Rationale**:
- Complex problem: step logic, order, and compensation can all change
- Reducer hash pattern already exists and works well
- In-flight sagas may need to complete with original logic

**Initial Thinking**:
- Compute hash from step definitions (order, types, configuration)
- Store hash when saga starts
- On saga resume, compare stored hash vs current
- Options on mismatch:
  - Fail saga (safe default)
  - Continue with warnings (opt-in)
  - Migration handler (advanced)

**Deferred**: Full design to be completed in follow-up spec. For v1, consider:
- Store step version/hash at saga start
- Fail saga on version mismatch (safe default)
- Log warning for operator intervention

---

### 9. Naming

**Decision**: Use "Saga" terminology

| Concept | Name |
|---------|------|
| Grain | `SagaGrain` (if separate) or reuse `GenericAggregateGrain` |
| Step base class | `SagaStepBase<TSaga>` |
| Namespace | `Mississippi.EventSourcing.Sagas` |
| Abstractions | `Mississippi.EventSourcing.Sagas.Abstractions` |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         SagaGrain<TSaga>                            │
│         (extends GenericAggregateGrain or implements IRemindable)   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Command ──► CommandHandler ──► Events                              │
│                                    │                                │
│                                    ▼                                │
│                            Persist to Brook                         │
│                                    │                                │
│                                    ▼                                │
│                         Run Server-Side Effects                     │
│                                    │                                │
│              ┌─────────────────────┼─────────────────────┐          │
│              ▼                     ▼                     ▼          │
│         SagaStep              SagaStep              SagaStep        │
│        (Execute)              (Verify)            (Compensate)      │
│              │                     │                     │          │
│              ▼                     ▼                     ▼          │
│     Call Other Aggregates    Poll/Check State    Undo Operations    │
│                                    │                                │
│                                    ▼                                │
│                         Set Reminder for Retry                      │
│                                    │                                │
│                                    ▼                                │
│                        ReceiveReminder ──► Continue                 │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Prebuilt Components

### Lifecycle Events (Framework-Provided)

```csharp
public sealed record SagaStartedEvent(
    string SagaId, 
    string SagaType, 
    string StepHash,
    DateTimeOffset Timestamp);

public sealed record SagaStepStartedEvent(
    string StepName, 
    int StepOrder, 
    DateTimeOffset Timestamp);

public sealed record SagaStepCompletedEvent(
    string StepName, 
    int StepOrder, 
    DateTimeOffset Timestamp);

public sealed record SagaStepFailedEvent(
    string StepName, 
    int StepOrder, 
    string ErrorCode, 
    string ErrorMessage, 
    DateTimeOffset Timestamp);

public sealed record SagaCompensatingEvent(
    string FromStep, 
    DateTimeOffset Timestamp);

public sealed record SagaStepCompensatedEvent(
    string StepName, 
    int StepOrder, 
    DateTimeOffset Timestamp);

public sealed record SagaCompletedEvent(DateTimeOffset Timestamp);

public sealed record SagaFailedEvent(
    string Reason, 
    DateTimeOffset Timestamp);
```

### Saga Status Projection (Auto-Registered)

```csharp
public sealed record SagaStatusProjection
{
    public string SagaId { get; init; } = "";
    public string SagaType { get; init; } = "";
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;
    public ImmutableList<SagaStepRecord> CompletedSteps { get; init; } = [];
    public SagaStepRecord? CurrentStep { get; init; }
    public ImmutableList<SagaStepRecord> FailedSteps { get; init; } = [];
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
}

public enum SagaPhase 
{ 
    NotStarted, 
    Running, 
    AwaitingVerification,
    Compensating, 
    Completed, 
    Failed 
}

public sealed record SagaStepRecord(
    string StepName,
    int StepOrder,
    DateTimeOffset Timestamp,
    StepOutcome Outcome,
    string? ErrorMessage = null);

public enum StepOutcome 
{ 
    Started,
    Succeeded, 
    Failed, 
    Compensated, 
    TimedOut 
}
```

### Base Step Classes

```csharp
/// <summary>
///     Base class for all saga steps.
/// </summary>
public abstract class SagaStepBase<TSaga> where TSaga : class
{
    public abstract string StepName { get; }
    
    public abstract bool ShouldExecute(TSaga state);
    
    public abstract Task<StepExecutionResult> ExecuteAsync(
        TSaga state,
        ISagaContext context,
        CancellationToken cancellationToken);
    
    public virtual Task<StepVerificationResult> VerifyAsync(
        TSaga state,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(StepVerificationResult.Verified());
    
    public abstract Task<StepExecutionResult> CompensateAsync(
        TSaga state,
        ISagaContext context,
        CancellationToken cancellationToken);
}

/// <summary>
///     Base step for calling another aggregate.
/// </summary>
public abstract class AggregateCommandStep<TSaga, TTarget> : SagaStepBase<TSaga>
    where TSaga : class
    where TTarget : class
{
    protected IAggregateGrainFactory AggregateFactory { get; }
    
    protected abstract string GetTargetEntityId(TSaga state);
    protected abstract object BuildCommand(TSaga state);
    protected abstract IEnumerable<object> BuildSuccessEvents(TSaga state);
    
    // ... implementation
}

/// <summary>
///     Base step for calling a child saga.
/// </summary>
public abstract class ChildSagaStep<TSaga, TChildSaga> : SagaStepBase<TSaga>
    where TSaga : class
    where TChildSaga : class
{
    protected abstract string GetChildSagaId(TSaga state);
    protected abstract object BuildStartCommand(TSaga state);
    
    // VerifyAsync polls child saga state by default
    // ... implementation
}

/// <summary>
///     Base step for calling external HTTP services.
/// </summary>
public abstract class HttpServiceStep<TSaga> : SagaStepBase<TSaga>
    where TSaga : class
{
    protected HttpClient Http { get; }
    
    protected abstract HttpRequestMessage BuildRequest(TSaga state);
    protected abstract IEnumerable<object> BuildSuccessEvents(TSaga state, HttpResponseMessage response);
    
    // ... implementation
}
```

---

## Registration Example

```csharp
// Saga state definition
[SagaOptions(
    CompensationStrategy = CompensationStrategy.Immediate,
    DefaultStepTimeout = "00:05:00",
    TimeoutBehavior = TimeoutBehavior.FailAndCompensate)]
[BrookName("order-fulfillment-saga")]
public sealed record OrderFulfillmentSagaState
{
    public string OrderId { get; init; } = "";
    public string PaymentId { get; init; } = "";
    public string InventoryId { get; init; } = "";
    public decimal Amount { get; init; }
    public bool PaymentReserved { get; init; }
    public bool InventoryReserved { get; init; }
}

// Step definitions with attributes
[SagaStep(Order = 1)]
public sealed class ReservePaymentStep 
    : AggregateCommandStep<OrderFulfillmentSagaState, PaymentAggregate>
{
    public override string StepName => "reserve-payment";
    
    public override bool ShouldExecute(OrderFulfillmentSagaState state)
        => !state.PaymentReserved;
    
    protected override string GetTargetEntityId(OrderFulfillmentSagaState state)
        => state.PaymentId;
    
    protected override object BuildCommand(OrderFulfillmentSagaState state)
        => new ReservePaymentCommand(state.OrderId, state.Amount);
    
    protected override IEnumerable<object> BuildSuccessEvents(OrderFulfillmentSagaState state)
    {
        yield return new PaymentReservedEvent(state.PaymentId);
    }
    
    public override async Task<StepVerificationResult> VerifyAsync(
        OrderFulfillmentSagaState state,
        ISagaContext context,
        CancellationToken ct)
    {
        // Verify payment projection shows reserved
        var projection = await context.GetProjectionAsync<PaymentProjection>(state.PaymentId, ct);
        
        return projection?.Status == PaymentStatus.Reserved
            ? StepVerificationResult.Verified()
            : StepVerificationResult.NotYet(TimeSpan.FromSeconds(2));
    }
    
    public override async Task<StepExecutionResult> CompensateAsync(
        OrderFulfillmentSagaState state,
        ISagaContext context,
        CancellationToken ct)
    {
        if (!state.PaymentReserved)
            return StepExecutionResult.Succeeded();
        
        await context.DispatchAsync<PaymentAggregate>(
            state.PaymentId,
            new ReleasePaymentCommand(state.PaymentId),
            ct);
        
        return StepExecutionResult.Succeeded(new PaymentReleasedEvent(state.PaymentId));
    }
}

[SagaStep(Order = 2)]
public sealed class ReserveInventoryStep 
    : AggregateCommandStep<OrderFulfillmentSagaState, InventoryAggregate>
{
    // ... similar pattern
}

[SagaStep(Order = 3, Timeout = "00:10:00")]  // Override timeout for this step
public sealed class CreateShipmentStep 
    : HttpServiceStep<OrderFulfillmentSagaState>
{
    // ... calls external shipping API
}

// Registration in Program.cs / Startup
services.AddSaga<OrderFulfillmentSagaState>();
// Source generator discovers steps via [SagaStep] attributes
// Auto-registers SagaStatusProjection
```

---

## Open Items for Future Specs

1. **Saga versioning details** — Full design for hash-based versioning and migration
2. **Aggregate reminder support** — Whether to add to base grain or create subclass
3. **Event-driven child saga completion** — Stream subscription as optimization
4. **Saga query API** — List active sagas, filter by status, saga management UI
5. **Retry policies** — Exponential backoff, jitter, circuit breaker patterns
6. **Distributed tracing** — Span propagation across saga steps
7. **Saga cleanup** — TTL, archival, completed saga pruning

---

## Server-Side Effect Integration

> **Based on**: `topic/server-effect` PR implementation

### Effect Pattern Summary

The `IEventEffect<TAggregate>` interface provides post-persistence hooks:

```csharp
public interface IEventEffect<TAggregate>
{
    bool CanHandle(object eventData);
    
    IAsyncEnumerable<object> HandleAsync(
        object eventData,
        TAggregate currentState,
        CancellationToken cancellationToken);
}
```

**Key behaviors** (from `GenericAggregateGrain`):
- Effects run **synchronously** after events are persisted
- Yielded events are persisted immediately to the same aggregate's brook
- Loop continues until no events yielded (max 10 iterations)
- Effects are resolved from DI with full constructor injection

### Two Effect Patterns

| Pattern | Base Class | Use Case | Yields Events? |
|---------|------------|----------|----------------|
| Event-enriching | `EventEffectBase<TEvent, TAggregate>` | Add computed events to same aggregate | Yes |
| Side-effect action | `SimpleEventEffectBase<TEvent, TAggregate>` | HTTP calls, notifications, cross-aggregate dispatch | No |

### Cross-Aggregate Dispatch (Option B Pattern)

Effects that call **other aggregates** use `SimpleEventEffectBase` and inject `IAggregateGrainFactory`:

```csharp
public sealed class HotelReservationHttpEffect 
    : SimpleEventEffectBase<HotelReservationRequestedEvent, HotelReservationState>
{
    private HttpClient Http { get; }
    private IAggregateGrainFactory GrainFactory { get; }
    
    public HotelReservationHttpEffect(
        IHttpClientFactory httpFactory,
        IAggregateGrainFactory grainFactory)
    {
        Http = httpFactory.CreateClient("HotelApi");
        GrainFactory = grainFactory;
    }
    
    protected override async Task HandleSimpleAsync(
        HotelReservationRequestedEvent @event,
        HotelReservationState state,
        CancellationToken ct)
    {
        // Read saga context from aggregate state (passed via command properties)
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/hotels/reserve");
        if (state.CorrelationId is not null)
            request.Headers.Add("X-Correlation-Id", state.CorrelationId);
        if (state.SagaId is not null)
            request.Headers.Add("X-Saga-Id", state.SagaId);
        
        var response = await Http.SendAsync(request, ct);
        
        // Dispatch result command back to this aggregate
        var resultCommand = response.IsSuccessStatusCode
            ? new HotelReservationConfirmedCommand(state.ReservationId, ...)
            : new HotelReservationFailedCommand(state.ReservationId, ...);
        
        await GrainFactory
            .GetGenericAggregate<HotelReservationState>(state.ReservationId)
            .ExecuteAsync(resultCommand, ct);
    }
}
```

### Context Propagation

Effects do **not** receive a context object—only `(TEvent, TAggregate, CancellationToken)`.

**Pattern**: Pass context through command properties → store in aggregate state → read from state in effects.

```csharp
// 1. Saga step builds command with context
protected override object BuildCommand(HolidayBookingSagaState state)
    => new ReserveHotelCommand(
        GetTargetEntityId(state),
        state.HotelRequest.HotelId,
        SagaId: state.BookingId,         // Pass saga context
        CorrelationId: state.CorrelationId);

// 2. Command handler stores context in aggregate state
return OperationResult.Ok<IReadOnlyList<object>>([
    new HotelReservationRequestedEvent(
        command.SagaId,
        command.CorrelationId,
        ...)
]);

// 3. Reducer updates state with context
=> state with { SagaId = @event.SagaId, CorrelationId = @event.CorrelationId };

// 4. Effect reads context from state (shown above)
```

### Effect Limitations

| Limitation | Reason | Workaround |
|------------|--------|------------|
| No reminder access | Effects run inside grain method, not `IRemindable` | Saga grain handles reminders, dispatches commands |
| No brook position | Not passed to effect signature | Store sequence in event if needed for idempotency |
| Synchronous execution | Blocks grain during effect | Keep effects fast; use async operations wisely |
| 10 iteration limit | Prevents infinite loops | Design event chains to terminate |

### Saga Steps vs Effects

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Saga Step                                   │
│    (Dispatches command, handles verification/compensation)         │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ ExecuteAsync(command)
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Target Aggregate Grain                           │
│                                                                     │
│  1. CommandHandler.Handle(cmd) → Events                             │
│  2. Persist events to brook                                         │
│  3. RootEventEffect.DispatchAsync(events)                          │
│       │                                                             │
│       ▼                                                             │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                     Server Effect                             │  │
│  │  - SimpleEventEffectBase: HTTP call, no yield                 │  │
│  │  - EventEffectBase: Yield additional events (same aggregate)  │  │
│  │  - May dispatch commands to OTHER aggregates                  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  4. Loop if events yielded (max 10)                                 │
│  5. Return OperationResult to caller                                │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Saga Step                                   │
│    (VerifyAsync polls aggregate state or projection)               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Dependencies

1. **Server-side aggregate effects** (`topic/server-effect` PR) — Prerequisite; provides `IEventEffect<T>`, `SimpleEventEffectBase`, registration
2. **Source generators** — For step discovery and validation
3. **Orleans reminders** — First use in Mississippi codebase

---

## References

- [GenericAggregateGrain.cs](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs) — Effect dispatch loop
- [IEventEffect.cs](../../src/EventSourcing.Aggregates.Abstractions/IEventEffect.cs) — Effect interface
- [EventEffectBase.cs](../../src/EventSourcing.Aggregates.Abstractions/EventEffectBase.cs) — Event-yielding base
- [SimpleEventEffectBase.cs](../../src/EventSourcing.Aggregates.Abstractions/SimpleEventEffectBase.cs) — Side-effect base
- [AggregateRegistrations.cs](../../src/EventSourcing.Aggregates/AggregateRegistrations.cs) — `AddEventEffect<T>()` registration
- [IActionEffect.cs](../../src/Reservoir.Abstractions/IActionEffect.cs) — Client-side effect (comparison)
- [UxProjectionGrain.cs](../../src/EventSourcing.UxProjections/UxProjectionGrain.cs) — Projection pattern
