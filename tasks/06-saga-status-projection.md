# Task 06: Saga Status Projection Subscription

## Objective

Enable the Blazor client to subscribe to real-time saga status updates via SignalR, allowing the UI to show saga progress (Step 1/2, Running, Completed, Failed).

## Rationale

Users need to see the saga progressing in real-time, especially with the 10-second delay between steps. Without status updates, the UI would appear frozen during the transfer.

## Architecture

```
┌─────────────────┐     SignalR      ┌─────────────────┐     Orleans      ┌─────────────────┐
│  Blazor Client  │◀────────────────▶│    API Server   │◀────────────────▶│  Saga Grain     │
│                 │                  │                 │                  │                 │
│ Subscribe to    │   Status Update  │ ProjectionHub   │  Grain Observer  │ SagaStatus      │
│ SagaStatusDto   │◀─────────────────│                 │◀─────────────────│ Projection      │
└─────────────────┘                  └─────────────────┘                  └─────────────────┘
```

## Deliverables

### 1. Server-Side Saga Status Projection

The `SagaStatusProjection` already exists in `EventSourcing.Sagas.Abstractions.Projections`. We need to:

1. Add `[GenerateProjectionEndpoints]` attribute to enable SignalR streaming
2. Create a projection grain that subscribes to saga events

**Verify/Add to `SagaStatusProjection.cs`:**
```csharp
[ProjectionPath("saga-status")]
[BrookName("MISSISSIPPI", "SAGAS", "SAGASTATUS")]  // May need saga-specific brook
[SnapshotStorageName("MISSISSIPPI", "SAGAS", "SAGASTATUSPROJECTION")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
public sealed record SagaStatusProjection
{
    // Existing properties...
}
```

**Challenge:** Saga status is per-saga-type (TransferFunds), not a global projection. We may need:
- `TransferFundsSagaStatusProjection` in `Spring.Domain`
- Or a generic pattern with saga type as part of the projection path

### 2. Client-Side Status DTO

**Location:** `Spring.Client/Features/TransferFundsSaga/Dtos/SagaStatusDto.cs`

```csharp
namespace Spring.Client.Features.TransferFundsSaga.Dtos;

/// <summary>
///     Client-side DTO for saga status updates.
/// </summary>
[ProjectionPath("saga-status")]  // Or "transfer-funds-saga-status"
public sealed record SagaStatusDto
{
    /// <summary>
    ///     Gets the saga instance identifier.
    /// </summary>
    public required string SagaId { get; init; }
    
    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    public required string Phase { get; init; }
    
    /// <summary>
    ///     Gets the currently executing step name, if any.
    /// </summary>
    public string? CurrentStepName { get; init; }
    
    /// <summary>
    ///     Gets the current step order (e.g., 1, 2).
    /// </summary>
    public int? CurrentStepOrder { get; init; }
    
    /// <summary>
    ///     Gets the total number of steps in the saga.
    /// </summary>
    public int TotalSteps { get; init; }
    
    /// <summary>
    ///     Gets when the saga started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }
    
    /// <summary>
    ///     Gets when the saga completed, if finished.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
    
    /// <summary>
    ///     Gets the failure reason, if the saga failed.
    /// </summary>
    public string? FailureReason { get; init; }
    
    /// <summary>
    ///     Gets whether compensation is in progress.
    /// </summary>
    public bool IsCompensating { get; init; }
}
```

### 3. Saga Status Reducers for SagaStatusProjection

If not already present, add reducers that build `SagaStatusProjection` from saga events:

**Location:** `src/EventSourcing.Sagas/Reducers/` (framework-level)

```csharp
// SagaStartedEventStatusReducer.cs
internal sealed class SagaStartedEventStatusReducer 
    : EventReducerBase<SagaStartedEvent, SagaStatusProjection>
{
    protected override SagaStatusProjection ApplyEvent(
        SagaStartedEvent evt, 
        SagaStatusProjection state) =>
        state with
        {
            SagaId = evt.SagaId,
            SagaType = evt.SagaType,
            Phase = SagaPhase.Running,
            StartedAt = evt.StartedAt
        };
}

// SagaStepStartedEventStatusReducer.cs
internal sealed class SagaStepStartedEventStatusReducer 
    : EventReducerBase<SagaStepStartedEvent, SagaStatusProjection>
{
    protected override SagaStatusProjection ApplyEvent(
        SagaStepStartedEvent evt, 
        SagaStatusProjection state) =>
        state with
        {
            CurrentStep = new SagaStepRecord 
            { 
                Name = evt.StepName, 
                Order = evt.StepOrder, 
                StartedAt = evt.StartedAt 
            }
        };
}

// etc. for StepCompleted, StepFailed, SagaCompleted, SagaFailed, Compensating
```

### 4. Client Subscription Pattern

Enable subscribing to saga status by saga ID:

```csharp
// In Transfer.razor.cs
private void TransferFunds()
{
    Guid sagaId = Guid.NewGuid();
    
    // Subscribe to status updates for this saga
    SubscribeToProjection<SagaStatusDto>(sagaId.ToString());
    
    // Dispatch the saga start action
    Dispatch(new StartTransferFundsSagaAction(
        sagaId,
        sourceAccountId,
        destinationAccountId,
        amount));
}

// Access status in component
private SagaStatusDto? SagaStatus => 
    currentSagaId.HasValue 
        ? GetProjection<SagaStatusDto>(currentSagaId.Value.ToString()) 
        : null;
```

### 5. Framework Enhancement: Per-Saga-Instance Projections

**Problem:** Current projection infrastructure may not support projections keyed by saga ID.

**Solution Options:**

A. **Reuse aggregate projection pattern** - Sagas ARE aggregates, so their projections work the same way. The entity ID is the saga ID.

B. **Add saga-specific projection routing** - If projections don't naturally work with saga brook naming, we need routing.

**Investigation Required:** Check if `SagaStatusProjection` can be registered with a brook that uses saga ID as entity key.

## Acceptance Criteria

- [ ] `SagaStatusProjection` has `[GenerateProjectionEndpoints]` or equivalent
- [ ] Client can subscribe to saga status by saga ID
- [ ] Real-time updates flow via SignalR when saga events occur
- [ ] Client DTO matches server projection structure
- [ ] Status shows: Phase, CurrentStep, Progress (1/2), StartedAt, CompletedAt, FailureReason
- [ ] Compensation status visible when saga is rolling back

## Design Questions to Resolve

1. Is `SagaStatusProjection` per-saga-type or global?
2. How is the projection keyed? By saga ID? By brook key?
3. Do we need `TransferFundsSagaStatusProjection` or can we reuse the framework one?
4. Are the framework saga status reducers already registered?

## Dependencies

- SignalR projection infrastructure (existing)
- Saga event types (`SagaStartedEvent`, `SagaStepStartedEvent`, etc.)
- Projection endpoint generators (existing for aggregates)

## Blocked By

- [05-domain-saga](05-domain-saga.md) - Need saga definition first

## Blocks

- [07-transfer-page](07-transfer-page.md) - UI needs status to display
