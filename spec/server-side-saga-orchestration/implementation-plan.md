# Implementation Plan (Revised 2025-06-10)

> **Status**: Updated to reflect the `SagaOrchestrationEffect<TSaga>` design.
> See [updated-design.md](./updated-design.md) for the authoritative design reference.

## Requirements (from task.md)

- Sagas are aggregates; reuse existing aggregate infrastructure and patterns.
- Discovery MUST use types/attributes, never namespace conventions.
- State MUST be a record; all state changes via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.
- Provide server, client, and silo generation with a consistent DX.

## Constraints

- Follow repository guardrails (logging, DI, zero warnings, CPM, abstractions).
- No new public API breaking changes without approval.
- ISagaState MUST have properties only—no Apply methods (events→reducers principle).

## Key Design Decisions

| Decision | Choice |
|----------|--------|
| Step implementation | POCO with `[SagaStep(Order, typeof(TSaga))]` attribute implementing `ISagaStep<TSaga>` |
| Compensation | `ICompensatable<TSaga>` interface on step class |
| Orchestration | Single `SagaOrchestrationEffect<TSaga>` handles step dispatch, compensation, completion |
| Data passing | IDs only + transient external data in saga state; query aggregates for system data |
| ISagaState | Properties only—no Apply methods |

## Detailed Plan

### 1. Abstractions (new projects)

Create `src/EventSourcing.Sagas.Abstractions` containing:

**Core Interfaces:**
```csharp
public interface ISagaState
{
    SagaPhase Phase { get; }
    int CurrentStep { get; }
    int TotalSteps { get; }
    string? CurrentStepName { get; }
    string? FailureReason { get; }
}

public interface ISagaStep<TSaga> where TSaga : ISagaState
{
    Task<StepResult> ExecuteAsync(TSaga state, CancellationToken ct);
}

public interface ICompensatable<TSaga> where TSaga : ISagaState
{
    Task<CompensationResult> CompensateAsync(TSaga state, CancellationToken ct);
}
```

**Attributes:**
- `[SagaStep(int Order, Type SagaType)]` - step discovery; order determines execution sequence
- `[SagaOptions]` - saga-level configuration (max retries, timeout, etc.)
- `[DelayAfterStep(TimeSpan)]` - optional delay after step completion

**Result Types:**
```csharp
public abstract record StepResult;
public sealed record StepSucceeded(object? Data = null) : StepResult;
public sealed record StepFailed(string Reason, bool Compensate = true) : StepResult;

public abstract record CompensationResult;
public sealed record CompensationSucceeded : CompensationResult;
public sealed record CompensationFailed(string Reason) : CompensationResult;
```

**Infrastructure Events (provided by framework):**
- `SagaStartedEvent`
- `SagaStepStartedEvent`
- `SagaStepSucceededEvent`
- `SagaStepFailedEvent`
- `SagaCompensatingEvent`
- `SagaCompensationStepSucceededEvent`
- `SagaCompensationStepFailedEvent`
- `SagaCompletedEvent`
- `SagaFailedEvent`

**Step Registry:**
- `ISagaStepRegistry<TSaga>` - discovers and orders steps by attribute
- `ISagaStepInfo` - metadata about a step (order, type, has compensation)

### 2. Runtime Implementation

Create `src/EventSourcing.Sagas` with:

**Single Orchestration Effect:**
```csharp
public sealed class SagaOrchestrationEffect<TSaga> : RootEventEffect<TSaga>
    where TSaga : class, ISagaState
{
    private ISagaStepRegistry<TSaga> StepRegistry { get; }
    private IServiceProvider ServiceProvider { get; }

    protected override async Task HandleAsync(
        TSaga state,
        IEventEnvelope envelope,
        string sagaId,
        long position,
        CancellationToken ct)
    {
        switch (envelope.Event)
        {
            case SagaStartedEvent:
                await ExecuteStepAsync(state, 0, sagaId, ct);
                break;
            case SagaStepSucceededEvent e:
                await ExecuteNextOrCompleteAsync(state, e.Step, sagaId, ct);
                break;
            case SagaStepFailedEvent e when e.Compensate:
                await StartCompensationAsync(state, e.Step, sagaId, ct);
                break;
            case SagaCompensationStepSucceededEvent e:
                await CompensatePreviousOrFailAsync(state, e.Step, sagaId, ct);
                break;
            // ... other cases
        }
    }
}
```

**Step Discovery:**
- Attribute-based only: scan for `[SagaStep(Order, typeof(TSaga))]`
- Order determines execution sequence (0, 1, 2, ...)
- Steps resolved via DI at execution time

**Registration Extension:**
```csharp
public static IServiceCollection AddSaga<TSaga>(
    this IServiceCollection services)
    where TSaga : class, ISagaState
{
    // Register step registry
    // Register SagaOrchestrationEffect<TSaga>
    // Register discovered steps via DI
    // Register infrastructure event reducers
}
```

### 3. Infrastructure Reducers (provided by framework)

Built-in reducers that apply infrastructure events to update saga state:

```csharp
// Example: SagaStepSucceededReducer applies to any ISagaState
public sealed class SagaStepSucceededReducer<TSaga> 
    : EventReducerBase<SagaStepSucceededEvent, TSaga>
    where TSaga : ISagaState
{
    public override TSaga Apply(TSaga state, SagaStepSucceededEvent @event)
        => state with 
        { 
            CurrentStep = @event.Step + 1,
            CurrentStepName = @event.NextStepName
        };
}
```

### 4. Server Generators

Add `SagaControllerGenerator` in [src/Inlet.Server.Generators](../../src/Inlet.Server.Generators):

- Discover saga types via `[GenerateSagaEndpoints]` + `ISagaState`
- Generate `POST /api/sagas/{saga-route}/{sagaId}` start endpoint
- Generate `GET /api/sagas/{saga-route}/{sagaId}/status` status endpoint

Add `SagaServerDtoGenerator`:
- Generate `Start{SagaName}SagaDto` and mapper

### 5. Client Generators

Add saga equivalents in [src/Inlet.Client.Generators](../../src/Inlet.Client.Generators):

- `SagaClientActionsGenerator` (Start/Executing/Succeeded/Failed actions)
- `SagaClientActionEffectsGenerator` (HTTP POST to saga endpoint)
- `SagaClientStateGenerator` and `SagaClientReducersGenerator`
- `SagaClientRegistrationGenerator`

### 6. Silo Generators

Add `SagaSiloRegistrationGenerator` in [src/Inlet.Silo.Generators](../../src/Inlet.Silo.Generators):

**Discovery (attribute-based only):**
- Saga state types: `[GenerateSagaEndpoints]` + implements `ISagaState`
- Steps: `[SagaStep(Order, typeof(TSaga))]` attribute
- Reducers: `EventReducerBase<TEvent, TSaga>` where `TSaga : ISagaState`

**No namespace discovery.** All discovery via types and attributes.

**Generated Registration:**
```csharp
public static class SagaRegistration
{
    public static IServiceCollection AddGeneratedSagas(
        this IServiceCollection services)
    {
        // For each discovered saga:
        services.AddSaga<TransferSagaState>();
        
        // Register step types (discovered by attribute)
        services.AddTransient<DebitSourceStep>();
        services.AddTransient<CreditDestinationStep>();
        // ...
        
        return services;
    }
}
```

### 7. Sample Saga (Spring)

Add TransferFunds saga in `samples/Spring/Spring.Domain`:

```csharp
// State (properties only - no Apply methods)
[GenerateSagaEndpoints("transfer")]
[CosmosStorage("sagas")]
public sealed record TransferSagaState : ISagaState
{
    public SagaPhase Phase { get; init; }
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public string? CurrentStepName { get; init; }
    public string? FailureReason { get; init; }
    
    // Business data (IDs only)
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    
    // Transient external data (if needed)
    public string? TransferReference { get; init; }
}

// Step (POCO with attribute)
[SagaStep(0, typeof(TransferSagaState))]
public sealed class DebitSourceStep : ISagaStep<TransferSagaState>, ICompensatable<TransferSagaState>
{
    private IAccountGrainFactory AccountGrainFactory { get; }
    
    public async Task<StepResult> ExecuteAsync(TransferSagaState state, CancellationToken ct)
    {
        var grain = AccountGrainFactory.GetGrain(state.SourceAccountId);
        var result = await grain.DebitAsync(state.Amount, ct);
        return result.IsSuccess 
            ? new StepSucceeded() 
            : new StepFailed(result.Error);
    }
    
    public async Task<CompensationResult> CompensateAsync(TransferSagaState state, CancellationToken ct)
    {
        var grain = AccountGrainFactory.GetGrain(state.SourceAccountId);
        await grain.CreditAsync(state.Amount, ct); // Reverse the debit
        return new CompensationSucceeded();
    }
}
```

### 8. Tests

**Generator L0 Tests:**
- Client: [tests/Inlet.Client.Generators.L0Tests](../../tests/Inlet.Client.Generators.L0Tests)
- Server: [tests/Inlet.Server.Generators.L0Tests](../../tests/Inlet.Server.Generators.L0Tests)
- Silo: [tests/Inlet.Silo.Generators.L0Tests](../../tests/Inlet.Silo.Generators.L0Tests)

**Runtime L0 Tests:**
- `SagaStepRegistry<T>` discovery and ordering
- `SagaOrchestrationEffect<T>` state machine logic
- Infrastructure reducers

**L2 Integration Tests:**
- End-to-end saga flow in `samples/Spring/Spring.L2Tests`
- Happy path: all steps succeed
- Compensation path: step fails, compensation runs
- Timeout/retry scenarios

**Testing Support:**
- `FakeSagaStep<TSaga>` for unit testing orchestration
- `SagaTestHarness<TSaga>` for driving saga through states

## Data Model / API Changes

- New saga endpoints under `/api/sagas/{saga-name}`
- New saga status projection DTOs
- New saga abstractions in `.Abstractions` projects

## Observability

Add LoggerExtensions classes for:
- Saga start (correlation ID, input summary)
- Step start/success/failure (step name, duration, error)
- Compensation start/success/failure
- Saga completion/failure (total duration, final state)

## Test Plan (commands)

```powershell
# Build
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1

# Cleanup
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1

# Unit tests
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Mutation tests
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

## Rollout / Backout

- Additive feature only; no data migrations expected.
- Backout by removing saga registrations and generated endpoints; keep existing aggregate APIs intact.