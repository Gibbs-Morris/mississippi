# Updated Saga Design: SagaEffect&lt;T&gt; Model

This document captures the refined saga orchestration design following gap analysis and design discussions.

---

## Design Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Step implementation | POCO with `[SagaStep]` attribute | Testable, DI-friendly, minimal boilerplate |
| Compensation | Method on step class (`ICompensatable`) | Single class per step, simpler DX |
| Next step orchestration | Infrastructure effect (SagaOrchestrationEffect) | Steps don't know about each other |
| Data passing | IDs only + transient external data | Query aggregates/projections for system data |
| Context object | Just use saga state directly | `ISagaState` already has saga info |

---

## Core Concept: SagaEffect&lt;T&gt;

Instead of complex effect chains, use a single infrastructure effect that wraps step logic:

```csharp
// Step is a simple POCO - just business logic
[SagaStep(Order = 1, Saga = typeof(TransferFundsSagaState))]
public sealed class DebitSourceStep
{
    public DebitSourceStep(IAggregateGrainFactory grainFactory, ILogger<DebitSourceStep> logger)
    {
        GrainFactory = grainFactory;
        Logger = logger;
    }
    
    private IAggregateGrainFactory GrainFactory { get; }
    private ILogger<DebitSourceStep> Logger { get; }
    
    public async Task<StepResult> ExecuteAsync(TransferFundsSagaState state, CancellationToken ct)
    {
        var grain = GrainFactory.GetGenericAggregate<BankAccountAggregate>(state.SourceAccountId);
        var result = await grain.ExecuteAsync(new WithdrawFunds { Amount = state.Amount }, ct);
        
        return result.Success
            ? StepResult.Succeeded(new SourceDebited { Amount = state.Amount })
            : StepResult.Failed(result.ErrorCode);
    }
}
```

The framework provides `SagaOrchestrationEffect<TSaga>` that:
1. Reacts to saga lifecycle events
2. Resolves the appropriate step from DI
3. Calls `ExecuteAsync` on the step
4. Yields business events + infrastructure events

---

## Compensation Model

Compensation is an optional method on the step class:

```csharp
[SagaStep(Order = 1, Saga = typeof(TransferFundsSagaState))]
public sealed class DebitSourceStep : ICompensatable<TransferFundsSagaState>
{
    // ... constructor and ExecuteAsync ...
    
    public async Task<CompensationResult> CompensateAsync(TransferFundsSagaState state, CancellationToken ct)
    {
        // Skip if step never executed successfully
        if (!state.SourceDebited)
        {
            return CompensationResult.Skipped("Source was not debited");
        }
        
        var grain = GrainFactory.GetGenericAggregate<BankAccountAggregate>(state.SourceAccountId);
        var result = await grain.ExecuteAsync(new DepositFunds { Amount = state.Amount }, ct);
        
        return result.Success
            ? CompensationResult.Succeeded()
            : CompensationResult.Failed(result.ErrorCode);
    }
}
```

Steps without compensation simply don't implement `ICompensatable<TSaga>`.

---

## Event Flow (Simplified)

```
StartSagaCommand
    ↓
CommandHandler emits: SagaStartedEvent
    ↓
Reducer applies: state with { Phase = Running, SagaId = ..., StartedAt = ... }
    ↓
SagaOrchestrationEffect handles SagaStartedEvent
    ↓ Resolves Step0 from DI
    ↓ Calls Step0.ExecuteAsync(state, ct)
    ↓
Step0 returns: StepResult.Succeeded([BusinessEvent1, BusinessEvent2])
    ↓
SagaOrchestrationEffect yields: [BusinessEvent1, BusinessEvent2, SagaStepCompleted(0)]
    ↓
Reducers apply: state with { ... business data ... }
    ↓
SagaOrchestrationEffect handles SagaStepCompleted(0)
    ↓ Looks up next step (index 1)
    ↓ Resolves Step1 from DI
    ↓ Calls Step1.ExecuteAsync(state, ct)
    ↓
... continues until final step ...
    ↓
SagaOrchestrationEffect yields: SagaCompleted
    ↓
Reducer applies: state with { Phase = Completed }
```

### Failure Path

```
Step2.ExecuteAsync returns: StepResult.Failed("INSUFFICIENT_FUNDS")
    ↓
SagaOrchestrationEffect yields: [SagaStepFailed(2, "INSUFFICIENT_FUNDS"), SagaCompensating]
    ↓
Reducer applies: state with { Phase = Compensating }
    ↓
SagaOrchestrationEffect handles SagaCompensating
    ↓ Walks backwards from Step1 to Step0
    ↓ For each step implementing ICompensatable, calls CompensateAsync
    ↓
All compensations succeed → yields: SagaCompensated
    ↓
Any compensation fails → yields: SagaFailed("COMPENSATION_FAILED")
```

---

## Built-In Infrastructure

### Events (Framework-Provided)

```csharp
// In EventSourcing.Sagas.Abstractions

public sealed record SagaStartedEvent
{
    [Id(0)] public required Guid SagaId { get; init; }
    [Id(1)] public required string StepHash { get; init; }
    [Id(2)] public required DateTimeOffset StartedAt { get; init; }
    [Id(3)] public string? CorrelationId { get; init; }
}

public sealed record SagaStepCompleted
{
    [Id(0)] public required int StepIndex { get; init; }
    [Id(1)] public required string StepName { get; init; }
    [Id(2)] public required DateTimeOffset CompletedAt { get; init; }
}

public sealed record SagaStepFailed
{
    [Id(0)] public required int StepIndex { get; init; }
    [Id(1)] public required string StepName { get; init; }
    [Id(2)] public required string ErrorCode { get; init; }
    [Id(3)] public string? ErrorMessage { get; init; }
}

public sealed record SagaCompensating
{
    [Id(0)] public required int FromStepIndex { get; init; }
}

public sealed record SagaStepCompensated
{
    [Id(0)] public required int StepIndex { get; init; }
    [Id(1)] public required string StepName { get; init; }
}

public sealed record SagaCompleted
{
    [Id(0)] public required DateTimeOffset CompletedAt { get; init; }
}

public sealed record SagaCompensated
{
    [Id(0)] public required DateTimeOffset CompletedAt { get; init; }
}

public sealed record SagaFailed
{
    [Id(0)] public required string ErrorCode { get; init; }
    [Id(1)] public string? ErrorMessage { get; init; }
    [Id(2)] public required DateTimeOffset FailedAt { get; init; }
}
```

### Commands

```csharp
public sealed record StartSagaCommand<TInput>
{
    [Id(0)] public required Guid SagaId { get; init; }
    [Id(1)] public required TInput Input { get; init; }
    [Id(2)] public string? CorrelationId { get; init; }
}
```

### Result Types

```csharp
public sealed record StepResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<object> Events { get; init; } = [];
    
    public static StepResult Succeeded(params object[] events) => 
        new() { Success = true, Events = events };
    
    public static StepResult Failed(string errorCode, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}

public sealed record CompensationResult
{
    public bool Success { get; init; }
    public bool Skipped { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static CompensationResult Succeeded() => new() { Success = true };
    public static CompensationResult Skipped(string? reason = null) => new() { Skipped = true };
    public static CompensationResult Failed(string errorCode, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}
```

---

## Saga Data Rules

### What to Store in Saga State

| Data Type | Store in Saga State? | Example |
|-----------|---------------------|---------|
| **IDs/References** | ✅ Yes | `HotelBookingId`, `FlightId`, `CustomerId` |
| **Transient external data** | ✅ Yes | API correlation token, exchange rate snapshot |
| **System data** | ❌ No - Query it | Hotel address, customer email, account balance |

### Rationale

1. **Single source of truth** - System data lives in aggregates/projections, not duplicated
2. **Minimal state** - Saga state stays small and focused
3. **Fresh data** - Queries get current values, not stale snapshots
4. **Clear guidance** - Easy to teach and follow

### Example: Travel Booking Saga

```csharp
public sealed record BookTripSagaState : ISagaState
{
    // === Saga Infrastructure ===
    [Id(0)] public Guid SagaId { get; init; }
    [Id(1)] public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;
    [Id(2)] public int LastCompletedStepIndex { get; init; } = -1;
    [Id(3)] public string? CorrelationId { get; init; }
    [Id(4)] public DateTimeOffset? StartedAt { get; init; }
    [Id(5)] public string? StepHash { get; init; }
    
    // === Input Data (from StartSagaCommand) ===
    [Id(10)] public DateOnly TravelDate { get; init; }
    [Id(11)] public string Destination { get; init; } = string.Empty;
    [Id(12)] public string CustomerId { get; init; } = string.Empty;
    
    // === IDs from Steps (minimal, for compensation) ===
    [Id(20)] public string? HotelBookingId { get; init; }
    [Id(21)] public string? FlightBookingId { get; init; }
    [Id(22)] public string? TransferId { get; init; }
    
    // === Transient External Data (only saga needs it) ===
    [Id(30)] public string? PaymentGatewayToken { get; init; }
    [Id(31)] public decimal? ExchangeRateSnapshot { get; init; }
}
```

### Querying System Data in Steps

```csharp
[SagaStep(Order = 3, Saga = typeof(BookTripSagaState))]
public sealed class ArrangeTransferStep
{
    public ArrangeTransferStep(
        IProjectionService projections,
        ITransferService transfers)
    { ... }
    
    public async Task<StepResult> ExecuteAsync(BookTripSagaState state, CancellationToken ct)
    {
        // Query system data from projections - NOT stored in saga state
        var hotel = await Projections.GetAsync<HotelBookingProjection>(state.HotelBookingId);
        var flight = await Projections.GetAsync<FlightBookingProjection>(state.FlightBookingId);
        
        // Use transient saga data directly from state
        var exchangeRate = state.ExchangeRateSnapshot;
        
        var transferId = await Transfers.ArrangeAsync(
            pickupAddress: hotel.Address,      // Queried
            dropoffTerminal: flight.Terminal,  // Queried
            date: state.TravelDate,            // From input
            exchangeRate: exchangeRate);       // Transient
            
        return StepResult.Succeeded(new TransferArranged { TransferId = transferId });
    }
}
```

### Failure Handling

If a query fails during step execution or compensation (aggregate unavailable, projection missing):
1. Step/compensation returns `Failed` result
2. Saga transitions to `Failed` state
3. This is a **system integrity problem** requiring operator intervention
4. The saga failing is the **correct signal** - don't try to work around it

---

## Step Discovery and Registration

### Attribute-Based Discovery (NOT Namespace)

Steps are discovered via `[SagaStep]` attribute and associated with sagas via the base type argument:

```csharp
// Generator finds this via [SagaStep] attribute
// Associates with TransferFundsSagaState via ISagaStep<TransferFundsSagaState>
[SagaStep(Order = 1, Saga = typeof(TransferFundsSagaState))]
public sealed class DebitSourceStep : ISagaStep<TransferFundsSagaState>
{
    // ...
}
```

### Generated Registration

```csharp
// Generated: TransferFundsSagaRegistrations.g.cs
public static class TransferFundsSagaRegistrations
{
    public static IServiceCollection AddTransferFundsSaga(this IServiceCollection services)
    {
        // Saga infrastructure
        services.AddSagaOrchestration<TransferFundsSagaState, TransferFundsSagaInput>();
        
        // Steps (DI registration)
        services.AddTransient<DebitSourceStep>();
        services.AddTransient<CreditDestinationStep>();
        
        // Step metadata for orchestration
        services.AddSagaStepInfo<TransferFundsSagaState>(new SagaStepInfo[]
        {
            new(0, "DebitSourceStep", typeof(DebitSourceStep), HasCompensation: true),
            new(1, "CreditDestinationStep", typeof(CreditDestinationStep), HasCompensation: false),
        });
        
        // Reducers (business + infrastructure)
        services.AddReducer<SourceDebited, TransferFundsSagaState, SourceDebitedReducer>();
        services.AddReducer<DestinationCredited, TransferFundsSagaState, DestinationCreditedReducer>();
        // Infrastructure reducers auto-registered
        
        return services;
    }
}
```

---

## ISagaState Interface

```csharp
public interface ISagaState
{
    Guid SagaId { get; }
    SagaPhase Phase { get; }
    int LastCompletedStepIndex { get; }
    string? CorrelationId { get; }
    DateTimeOffset? StartedAt { get; }
    string? StepHash { get; }
}

public enum SagaPhase
{
    NotStarted,
    Running,
    Compensating,
    Completed,
    Compensated,
    Failed
}
```

**Note:** No `Apply` methods on the interface. All state transitions happen through events → reducers. Infrastructure reducers handle phase transitions.

---

## Interfaces

### ISagaStep&lt;TSaga&gt;

```csharp
public interface ISagaStep<TSaga> where TSaga : class, ISagaState
{
    Task<StepResult> ExecuteAsync(TSaga state, CancellationToken cancellationToken);
}
```

### ICompensatable&lt;TSaga&gt;

```csharp
public interface ICompensatable<TSaga> where TSaga : class, ISagaState
{
    Task<CompensationResult> CompensateAsync(TSaga state, CancellationToken cancellationToken);
}
```

### SagaStepAttribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class SagaStepAttribute : Attribute
{
    public SagaStepAttribute(int order)
    {
        Order = order;
    }
    
    public int Order { get; }
    public Type? Saga { get; set; }  // Optional if step implements ISagaStep<T>
}
```

---

## Testing Pattern

Steps are POCOs - test them directly:

```csharp
[Fact]
public async Task DebitSourceStep_WithSufficientFunds_ReturnsSuccess()
{
    // Arrange
    var mockGrainFactory = new Mock<IAggregateGrainFactory>();
    var mockGrain = new Mock<IGenericAggregateGrain<BankAccountAggregate>>();
    mockGrain
        .Setup(g => g.ExecuteAsync(It.IsAny<WithdrawFunds>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(OperationResult.Ok());
    mockGrainFactory
        .Setup(f => f.GetGenericAggregate<BankAccountAggregate>(It.IsAny<string>()))
        .Returns(mockGrain.Object);
    
    var step = new DebitSourceStep(mockGrainFactory.Object, NullLogger<DebitSourceStep>.Instance);
    var state = new TransferFundsSagaState
    {
        SourceAccountId = "account-123",
        Amount = 100m
    };
    
    // Act
    var result = await step.ExecuteAsync(state, CancellationToken.None);
    
    // Assert
    Assert.True(result.Success);
    Assert.Single(result.Events);
    Assert.IsType<SourceDebited>(result.Events[0]);
}
```

---

## Summary of Changes from Original Design

| Original Design | New Design |
|-----------------|------------|
| `SagaStepBase<T>` base class | POCO with `ISagaStep<T>` interface |
| Separate compensation classes | `ICompensatable<T>` method on step |
| Complex effect chain | Single `SagaOrchestrationEffect<T>` |
| Unclear data passing | IDs only + transient external data rule |
| `ISagaState.Apply*` methods | Pure events → reducers |
| Namespace-based discovery | Attribute-based discovery |
