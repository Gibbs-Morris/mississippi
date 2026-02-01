# Task 05: Domain Saga Definition

## Objective

Define the `TransferFunds` saga in `Spring.Domain` with input, state, events, steps, compensations, and reducers.

## Rationale

This is the core business logic for the money transfer feature. Once defined with proper attributes, the generators will produce all infrastructure code.

## Folder Structure

```text
Spring.Domain/
└── Sagas/
    └── TransferFunds/
        ├── TransferFundsSagaInput.cs
        ├── TransferFundsSagaState.cs
        ├── Events/
        │   ├── TransferInitiated.cs
        │   ├── SourceDebited.cs
        │   ├── DestinationCredited.cs
        │   └── TransferCompleted.cs
        ├── Steps/
        │   ├── DebitSourceAccountStep.cs
        │   └── CreditDestinationAccountStep.cs
        ├── Compensations/
        │   └── RefundSourceAccountCompensation.cs
        └── Reducers/
            ├── TransferInitiatedReducer.cs
            ├── SourceDebitedReducer.cs
            ├── DestinationCreditedReducer.cs
            └── TransferCompletedReducer.cs
```

## Deliverables

### 1. `TransferFundsSagaInput.cs`

```csharp
namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     Input data required to start a TransferFunds saga.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.TransferFundsSagaInput")]
public sealed record TransferFundsSagaInput
{
    /// <summary>
    ///     Gets the account to withdraw funds from.
    /// </summary>
    [Id(0)]
    public required string SourceAccountId { get; init; }
    
    /// <summary>
    ///     Gets the account to deposit funds into.
    /// </summary>
    [Id(1)]
    public required string DestinationAccountId { get; init; }
    
    /// <summary>
    ///     Gets the amount to transfer.
    /// </summary>
    [Id(2)]
    public required decimal Amount { get; init; }
}
```

### 2. `TransferFundsSagaState.cs`

```csharp
namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     Saga state for the TransferFunds saga.
/// </summary>
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATE")]
[SagaOptions(CompensationStrategy = CompensationStrategy.Immediate)]
[GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.TransferFundsSagaState")]
public sealed record TransferFundsSagaState : ISagaDefinition, ISagaState
{
    /// <inheritdoc />
    public static string SagaName => "TransferFunds";
    
    // Business data (populated from TransferInitiated event)
    [Id(0)]
    public string SourceAccountId { get; init; } = string.Empty;
    
    [Id(1)]
    public string DestinationAccountId { get; init; } = string.Empty;
    
    [Id(2)]
    public decimal Amount { get; init; }
    
    [Id(3)]
    public bool SourceDebited { get; init; }
    
    [Id(4)]
    public bool DestinationCredited { get; init; }
    
    // ISagaState implementation
    [Id(10)]
    public Guid SagaId { get; init; }
    
    [Id(11)]
    public string? CorrelationId { get; init; }
    
    [Id(12)]
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;
    
    [Id(13)]
    public int LastCompletedStepIndex { get; init; } = -1;
    
    [Id(14)]
    public int CurrentStepAttempt { get; init; } = 1;
    
    [Id(15)]
    public DateTimeOffset? StartedAt { get; init; }
    
    [Id(16)]
    public string? StepHash { get; init; }
    
    // ISagaState methods...
}
```

### 3. Events

**`TransferInitiated.cs`** - Emitted in Step 1, captures input data:

```csharp
[EventStorageName("SPRING", "BANKING", "TRANSFERINITIATED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.TransferInitiated")]
internal sealed record TransferInitiated
{
    [Id(0)] public required string SourceAccountId { get; init; }
    [Id(1)] public required string DestinationAccountId { get; init; }
    [Id(2)] public required decimal Amount { get; init; }
}
```

**`SourceDebited.cs`** - Emitted when source account withdraw succeeds:

```csharp
[EventStorageName("SPRING", "BANKING", "SOURCEDEBITED")]
[GenerateSerializer]
internal sealed record SourceDebited
{
    [Id(0)] public required decimal Amount { get; init; }
}
```

**`DestinationCredited.cs`** - Emitted when destination deposit succeeds:

```csharp
[EventStorageName("SPRING", "BANKING", "DESTINATIONCREDITED")]
[GenerateSerializer]
internal sealed record DestinationCredited
{
    [Id(0)] public required decimal Amount { get; init; }
}
```

**`TransferCompleted.cs`** - Final success event:

```csharp
[EventStorageName("SPRING", "BANKING", "TRANSFERCOMPLETED")]
[GenerateSerializer]
internal sealed record TransferCompleted
{
    [Id(0)] public required DateTimeOffset CompletedAt { get; init; }
}
```

### 4. Steps

**`DebitSourceAccountStep.cs`** (Order = 1):

```csharp
[SagaStep(1)]
internal sealed class DebitSourceAccountStep : SagaStepBase<TransferFundsSagaState>
{
    public DebitSourceAccountStep(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<DebitSourceAccountStep> logger)
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }
    
    private IAggregateGrainFactory AggregateGrainFactory { get; }
    private ILogger<DebitSourceAccountStep> Logger { get; }
    
    public override async Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken)
    {
        // First step: emit TransferInitiated to capture input in saga state
        // This assumes input was passed via context or we read from StartSagaCommand
        
        IGenericAggregateGrain<BankAccountAggregate> grain = 
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(
                state.SourceAccountId);
        
        OperationResult result = await grain.ExecuteAsync(
            new WithdrawFunds { Amount = state.Amount }, 
            cancellationToken);
        
        if (!result.Success)
        {
            Logger.DebitFailed(context.SagaId, state.SourceAccountId, result.ErrorMessage);
            return StepResult.Failed(
                result.ErrorCode ?? "DEBIT_FAILED", 
                result.ErrorMessage ?? "Failed to debit source account");
        }
        
        Logger.DebitSucceeded(context.SagaId, state.SourceAccountId, state.Amount);
        return StepResult.Succeeded(new SourceDebited { Amount = state.Amount });
    }
}
```

**`CreditDestinationAccountStep.cs`** (Order = 2):

```csharp
[SagaStep(2)]
internal sealed class CreditDestinationAccountStep : SagaStepBase<TransferFundsSagaState>
{
    public CreditDestinationAccountStep(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<CreditDestinationAccountStep> logger)
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }
    
    private IAggregateGrainFactory AggregateGrainFactory { get; }
    private ILogger<CreditDestinationAccountStep> Logger { get; }
    
    public override async Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken)
    {
        IGenericAggregateGrain<BankAccountAggregate> grain = 
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(
                state.DestinationAccountId);
        
        OperationResult result = await grain.ExecuteAsync(
            new DepositFunds { Amount = state.Amount }, 
            cancellationToken);
        
        if (!result.Success)
        {
            Logger.CreditFailed(context.SagaId, state.DestinationAccountId, result.ErrorMessage);
            return StepResult.Failed(
                result.ErrorCode ?? "CREDIT_FAILED", 
                result.ErrorMessage ?? "Failed to credit destination account");
        }
        
        Logger.CreditSucceeded(context.SagaId, state.DestinationAccountId, state.Amount);
        return StepResult.Succeeded(
            new DestinationCredited { Amount = state.Amount },
            new TransferCompleted { CompletedAt = DateTimeOffset.UtcNow });
    }
}
```

### 5. Compensation

**`RefundSourceAccountCompensation.cs`**:

```csharp
[SagaCompensation(typeof(DebitSourceAccountStep))]
internal sealed class RefundSourceAccountCompensation 
    : SagaCompensationBase<TransferFundsSagaState>
{
    public RefundSourceAccountCompensation(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<RefundSourceAccountCompensation> logger)
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }
    
    private IAggregateGrainFactory AggregateGrainFactory { get; }
    private ILogger<RefundSourceAccountCompensation> Logger { get; }
    
    public override async Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken)
    {
        // Only compensate if we actually debited
        if (!state.SourceDebited)
        {
            Logger.SkippingCompensation(context.SagaId, "Source was not debited");
            return CompensationResult.Skipped("Source account was not debited");
        }
        
        IGenericAggregateGrain<BankAccountAggregate> grain = 
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(
                state.SourceAccountId);
        
        OperationResult result = await grain.ExecuteAsync(
            new DepositFunds { Amount = state.Amount }, 
            cancellationToken);
        
        if (!result.Success)
        {
            Logger.RefundFailed(context.SagaId, state.SourceAccountId, result.ErrorMessage);
            return CompensationResult.Failed(
                result.ErrorCode ?? "REFUND_FAILED",
                result.ErrorMessage);
        }
        
        Logger.RefundSucceeded(context.SagaId, state.SourceAccountId, state.Amount);
        return CompensationResult.Succeeded();
    }
}
```

### 6. Reducers

Standard reducers to apply events to saga state:

```csharp
// TransferInitiatedReducer.cs
internal sealed class TransferInitiatedReducer 
    : EventReducerBase<TransferInitiated, TransferFundsSagaState>
{
    protected override TransferFundsSagaState ApplyEvent(
        TransferInitiated evt, 
        TransferFundsSagaState state) =>
        state with
        {
            SourceAccountId = evt.SourceAccountId,
            DestinationAccountId = evt.DestinationAccountId,
            Amount = evt.Amount
        };
}

// SourceDebitedReducer.cs
internal sealed class SourceDebitedReducer 
    : EventReducerBase<SourceDebited, TransferFundsSagaState>
{
    protected override TransferFundsSagaState ApplyEvent(
        SourceDebited evt, 
        TransferFundsSagaState state) =>
        state with { SourceDebited = true };
}

// DestinationCreditedReducer.cs  
internal sealed class DestinationCreditedReducer 
    : EventReducerBase<DestinationCredited, TransferFundsSagaState>
{
    protected override TransferFundsSagaState ApplyEvent(
        DestinationCredited evt, 
        TransferFundsSagaState state) =>
        state with { DestinationCredited = true };
}
```

## Saga Input Capture Challenge

**Problem:** The saga input is passed via `StartSagaCommand<TInput>` but the first step receives the current saga state which doesn't have the input data yet.

**Solution:** The `StartSagaCommandHandler` should emit a custom event that captures the input. We need to verify/enhance the framework to support this:

1. Option A: First step receives input from context (requires framework change)
2. Option B: Custom handler emits `TransferInitiated` with input data
3. Option C: Input is available via `ISagaContext` (requires framework change)

**Recommendation:** Enhance `ISagaContext` to include `TInput GetInput<TInput>()` method. This is cleaner than embedding input capture in step logic.

## Acceptance Criteria

- [ ] All files created in correct locations
- [ ] Saga state implements both `ISagaDefinition` and `ISagaState`
- [ ] All attributes correct (`BrookName`, `SnapshotStorageName`, etc.)
- [ ] Steps inject `IAggregateGrainFactory` and call aggregate commands
- [ ] Compensation deposits funds back on failure
- [ ] Reducers correctly update saga state
- [ ] Orleans serialization attributes on all types
- [ ] XML documentation complete
- [ ] Compiles with zero warnings

## Dependencies

- Existing `BankAccountAggregate` with `WithdrawFunds`/`DepositFunds` commands
- Saga framework (`SagaStepBase`, `SagaCompensationBase`, etc.)

## Blocked By

- [02-server-generators](02-server-generators.md) (for full registration)
- [03-client-generators](03-client-generators.md) (for client invocation)

## Blocks

- [07-transfer-page](07-transfer-page.md)
- [09-delay-effect](09-delay-effect.md)
