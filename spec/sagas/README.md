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

## DX Principles

Sagas follow the same patterns as the rest of Mississippi:

| Pattern | Existing | Saga Equivalent |
|---------|----------|-----------------|
| One core method | `CommandHandlerBase.HandleCore()` | `SagaStepBase.ExecuteAsync()` |
| Dependency injection | Constructor injection | Constructor injection |
| Type discovery | Source generator + attributes | Source generator + `[SagaStep]` |
| Registration | `AddCommandHandler<T>()` | `AddSaga<T>()` |
| Separation of concerns | Handlers, reducers, effects | Steps, compensations, effects |

**No grab-bag contexts** — inject what you need via constructor, like everywhere else.

**One method per class** — steps have `ExecuteAsync`, compensations have `CompensateAsync`.

**Infrastructure handles lifecycle** — steps don't emit started/completed events.

---

## Design Decisions

### 1. Step Execution Model

**Decision**: One method per step, matching `CommandHandlerBase` pattern

Steps have a single `ExecuteAsync` method. Verification is declarative via attributes.

**Rationale**: 
- Matches existing patterns (`CommandHandlerBase` has one `HandleCore`, `EventReducerBase` has one `ReduceCore`)
- Simpler DX — developers implement one method
- Verification is infrastructure concern, not business logic

**API Shape**:
```csharp
/// <summary>
///     Base class for saga steps. Mirrors CommandHandlerBase pattern.
/// </summary>
public abstract class SagaStepBase<TSaga> where TSaga : class
{
    /// <summary>
    ///     Execute the step action. Return business events if saga state needs updating.
    /// </summary>
    public abstract Task<StepResult> ExecuteAsync(
        TSaga state,
        CancellationToken cancellationToken);
}

public sealed record StepResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<object> Events { get; init; } = [];
    
    public static StepResult Succeeded() => new() { Success = true };
    public static StepResult Succeeded(params object[] events) 
        => new() { Success = true, Events = events };
    public static StepResult Failed(string errorCode, string? message = null) 
        => new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}
```

**Verification via attributes** (infrastructure handles polling):
```csharp
[SagaStep(Order = 1)]
[AwaitCondition<PaymentProjection>(
    p => p.Status == PaymentStatus.Reserved,
    RetryDelay = "00:00:02",
    Timeout = "00:05:00")]
public sealed class ReservePaymentStep : SagaStepBase<OrderSagaState> { ... }

// Or simpler — just await aggregate state
[SagaStep(Order = 1)]
[AwaitAggregateState<HotelReservationState>(
    s => s.Status == ReservationStatus.Confirmed)]
public sealed class ReserveHotelStep : SagaStepBase<HolidayBookingSagaState> { ... }
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

**Decision**: Use declarative verification attributes (same as aggregate verification)

**Rationale**:
- Sagas and aggregates are the same core object
- Verification is infrastructure concern, not step business logic
- Consistent mechanism reduces cognitive load

**Implementation**:
```csharp
[SagaStep(Order = 2)]
[AwaitAggregateState<ChildSagaState>(s => s.Phase == SagaPhase.Completed)]
public sealed class InvokeChildSagaStep(
    IAggregateGrainFactory aggregateFactory
) : SagaStepBase<ParentSagaState>
{
    public override async Task<StepResult> ExecuteAsync(
        ParentSagaState state,
        CancellationToken ct)
    {
        // Start child saga
        var result = await aggregateFactory
            .GetGenericAggregate<ChildSagaState>(state.ChildSagaId)
            .ExecuteAsync(new StartChildCommand(state.CorrelationId), ct);
        
        return result.Success 
            ? StepResult.Succeeded() 
            : StepResult.Failed(result.ErrorCode!, result.ErrorMessage);
    }
}
```

Infrastructure handles:
- Polling child saga state via reminder
- Checking `Phase == Completed` condition
- Proceeding to next step or triggering compensation on failure

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

**Decision**: Attribute-based with source generator validation, constructor injection for dependencies

**Rationale**:
- Consistent with existing Mississippi patterns (commands, reducers, projections)
- Constructor injection matches `CommandHandlerBase` — no grab-bag context
- Source generator can detect conflicts (duplicate order, missing steps) at compile time

**API Shape**:
```csharp
[SagaStep(Order = 1)]
public sealed class ReservePaymentStep(
    IAggregateGrainFactory aggregateFactory  // DI, not context
) : SagaStepBase<OrderSagaState>
{
    public override async Task<StepResult> ExecuteAsync(
        OrderSagaState state,
        CancellationToken ct)
    {
        var result = await aggregateFactory
            .GetGenericAggregate<PaymentAggregate>(state.PaymentId)
            .ExecuteAsync(new ReservePaymentCommand(state.OrderId, state.Amount), ct);
        
        return result.Success
            ? StepResult.Succeeded()
            : StepResult.Failed(result.ErrorCode!, result.ErrorMessage);
    }
}

[SagaStep(Order = 2)]
[AwaitProjection<InventoryProjection>(p => p.Reserved)]
public sealed class ReserveInventoryStep(...) : SagaStepBase<OrderSagaState> { ... }

[SagaStep(Order = 3, Timeout = "00:10:00")]  // Override timeout
public sealed class CreateShipmentStep(...) : SagaStepBase<OrderSagaState> { ... }
```

**Source Generator Responsibilities**:
- Validate no duplicate `Order` values for same saga
- Validate step classes have parameterless or DI-injectable constructor
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

### Base Step Class

```csharp
/// <summary>
///     Base class for saga steps. One method, like CommandHandlerBase.
/// </summary>
public abstract class SagaStepBase<TSaga> where TSaga : class
{
    /// <summary>
    ///     Execute the step. Return business events if saga state needs updating.
    ///     Lifecycle events (started/completed/failed) are emitted by infrastructure.
    /// </summary>
    public abstract Task<StepResult> ExecuteAsync(
        TSaga state,
        CancellationToken cancellationToken);
}

public sealed record StepResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<object> Events { get; init; } = [];
    
    public static StepResult Succeeded() => new() { Success = true };
    public static StepResult Succeeded(params object[] businessEvents) 
        => new() { Success = true, Events = businessEvents };
    public static StepResult Failed(string errorCode, string? message = null) 
        => new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}
```

### Compensation — Separate Class (like handlers)

```csharp
/// <summary>
///     Compensation for a saga step. Registered separately, like command handlers.
/// </summary>
[SagaCompensation(ForStep = typeof(ReservePaymentStep))]
public sealed class ReservePaymentCompensation(
    IAggregateGrainFactory aggregateFactory
) : SagaCompensationBase<OrderSagaState>
{
    public override async Task<CompensationResult> CompensateAsync(
        OrderSagaState state,
        CancellationToken ct)
    {
        if (!state.PaymentReserved)
            return CompensationResult.Skipped();  // Nothing to undo
        
        var result = await aggregateFactory
            .GetGenericAggregate<PaymentAggregate>(state.PaymentId)
            .ExecuteAsync(new ReleasePaymentCommand(state.PaymentId), ct);
        
        return result.Success
            ? CompensationResult.Succeeded()
            : CompensationResult.Failed(result.ErrorCode!);
    }
}
```

### Verification — Declarative Attributes

```csharp
/// <summary>
///     Wait for projection to match condition before proceeding.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AwaitConditionAttribute<TProjection> : Attribute
{
    public Expression<Func<TProjection, bool>> Condition { get; }
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>
///     Wait for aggregate state to match condition before proceeding.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AwaitAggregateStateAttribute<TAggregate> : Attribute
{
    public Expression<Func<TAggregate, bool>> Condition { get; }
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
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

// Step: one method, constructor injection
[SagaStep(Order = 1)]
[AwaitProjection<PaymentProjection>(p => p.Status == PaymentStatus.Reserved)]
public sealed class ReservePaymentStep(
    IAggregateGrainFactory aggregateFactory
) : SagaStepBase<OrderFulfillmentSagaState>
{
    public override async Task<StepResult> ExecuteAsync(
        OrderFulfillmentSagaState state,
        CancellationToken ct)
    {
        var result = await aggregateFactory
            .GetGenericAggregate<PaymentAggregate>(state.PaymentId)
            .ExecuteAsync(new ReservePaymentCommand(state.OrderId, state.Amount), ct);
        
        if (!result.Success)
            return StepResult.Failed(result.ErrorCode!, result.ErrorMessage);
        
        // Business event if saga needs to capture data
        return StepResult.Succeeded(new PaymentReservationInitiatedEvent(state.PaymentId));
    }
}

// Compensation: separate class, same pattern
[SagaCompensation(ForStep = typeof(ReservePaymentStep))]
public sealed class ReservePaymentCompensation(
    IAggregateGrainFactory aggregateFactory
) : SagaCompensationBase<OrderFulfillmentSagaState>
{
    public override async Task<CompensationResult> CompensateAsync(
        OrderFulfillmentSagaState state,
        CancellationToken ct)
    {
        if (!state.PaymentReserved)
            return CompensationResult.Skipped();
        
        await aggregateFactory
            .GetGenericAggregate<PaymentAggregate>(state.PaymentId)
            .ExecuteAsync(new ReleasePaymentCommand(state.PaymentId), ct);
        
        return CompensationResult.Succeeded();
    }
}

[SagaStep(Order = 2)]
[AwaitProjection<InventoryProjection>(p => p.Reserved)]
public sealed class ReserveInventoryStep(...) : SagaStepBase<OrderFulfillmentSagaState> { ... }

[SagaCompensation(ForStep = typeof(ReserveInventoryStep))]
public sealed class ReserveInventoryCompensation(...) : SagaCompensationBase<OrderFulfillmentSagaState> { ... }

[SagaStep(Order = 3, Timeout = "00:10:00")]
public sealed class CreateShipmentStep(...) : SagaStepBase<OrderFulfillmentSagaState> { ... }

// Registration in Program.cs
services.AddSaga<OrderFulfillmentSagaState>();
// Source generator discovers steps and compensations via attributes
// Auto-registers SagaStatusProjection
```

---

## Event Emission Responsibility

### Lifecycle Events — Infrastructure (Automatic)

```csharp
// Saga orchestrator emits these automatically:
SagaStartedEvent          // Before first step
SagaStepStartedEvent      // Before each step.ExecuteAsync()
SagaStepCompletedEvent    // After step success + verification
SagaStepFailedEvent       // After step failure
SagaCompensatingEvent     // Before compensation starts
SagaStepCompensatedEvent  // After each compensation
SagaCompletedEvent        // All steps done
SagaFailedEvent           // Unrecoverable failure
```

### Business Events — Step Returns (When Needed)

```csharp
// Step returns business events ONLY when saga state needs data:
public override async Task<StepResult> ExecuteAsync(...)
{
    var response = await aggregateFactory
        .GetGenericAggregate<HotelReservationState>(state.ReservationId)
        .ExecuteAsync(new ReserveHotelCommand(...), ct);
    
    // Saga needs the confirmation ID? Return event with data
    return StepResult.Succeeded(
        new HotelConfirmationCapturedEvent(response.ConfirmationId));
}

// Saga doesn't need data from this step? Just return success
public override async Task<StepResult> ExecuteAsync(...)
{
    await notificationService.SendAsync(email, ct);
    return StepResult.Succeeded();  // No events
}
```

### Separation Summary

| Event Type | Emitted By | Example |
|------------|------------|---------|
| Lifecycle | Infrastructure | `SagaStepStartedEvent`, `SagaStepCompletedEvent` |
| Business data | Step (optional) | `HotelConfirmationCapturedEvent` |

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

### Single Effect Pattern

Always use `EventEffectBase<TEvent, TAggregate>`. Yield events when you have them, `yield break;` when you don't.

```csharp
// Effect that yields events
public override async IAsyncEnumerable<object> HandleAsync(...)
{
    var response = await Http.SendAsync(request, ct);
    yield return new HotelReservationConfirmedEvent(response.ConfirmationId);
}

// Fire-and-forget effect (no events to yield)
public override async IAsyncEnumerable<object> HandleAsync(...)
{
    await NotificationService.SendAsync(email, ct);
    yield break;
}
```

**Why one pattern?** Simpler DX—developers learn one base class, not two.

### Cross-Aggregate Dispatch (Option B Pattern)

Effects that call **other aggregates** use `EventEffectBase` and inject `IAggregateGrainFactory`:

```csharp
public sealed class HotelReservationHttpEffect 
    : EventEffectBase<HotelReservationRequestedEvent, HotelReservationState>
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
    
    public override async IAsyncEnumerable<object> HandleAsync(
        HotelReservationRequestedEvent @event,
        HotelReservationState state,
        [EnumeratorCancellation] CancellationToken ct)
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
        
        yield break; // No events to yield, command dispatch handles state update
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
