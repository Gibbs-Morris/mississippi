# Implementation Plan (Revised 2025-06-10)

> **Status**: Updated to reflect the `SagaOrchestrationEffect<TSaga>` design.
> See [rfc.md](./rfc.md) for the authoritative design reference.

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
    Guid SagaId { get; }
    SagaPhase Phase { get; }
    int LastCompletedStepIndex { get; }
    string? CorrelationId { get; }
    DateTimeOffset? StartedAt { get; }
    string? StepHash { get; }
}

public interface ISagaStep<TSaga> where TSaga : class, ISagaState
{
    Task<StepResult> ExecuteAsync(TSaga state, CancellationToken ct);
}

public interface ICompensatable<TSaga> where TSaga : class, ISagaState
{
    Task<CompensationResult> CompensateAsync(TSaga state, CancellationToken ct);
}
```

**Attributes:**
- `[SagaStep(int order)]` with optional `Saga` type property - step discovery; order determines execution sequence

**Result Types:**
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

**Infrastructure Events (provided by framework):**
- `SagaStartedEvent`
- `SagaStepCompleted`
- `SagaStepFailed`
- `SagaCompensating`
- `SagaStepCompensated`
- `SagaCompleted`
- `SagaCompensated`
- `SagaFailed`

**Step Metadata:**
- `SagaStepInfo` (index, name, type, has compensation)
- `AddSagaStepInfo<TSaga>(SagaStepInfo[] steps)` registration helper

### 2. Runtime Implementation

Create `src/EventSourcing.Sagas` with:

**Single Orchestration Effect:**
```csharp
public sealed class SagaOrchestrationEffect<TSaga> : RootEventEffect<TSaga>
    where TSaga : class, ISagaState
{
    private IReadOnlyList<SagaStepInfo> Steps { get; }
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
            case SagaStepCompleted e:
                await ExecuteNextOrCompleteAsync(state, e.StepIndex, sagaId, ct);
                break;
            case SagaStepFailed e:
                await StartCompensationAsync(state, e.StepIndex, sagaId, ct);
                break;
            case SagaCompensating:
                await CompensatePreviousOrFailAsync(state, sagaId, ct);
                break;
            case SagaStepCompensated e:
                await CompensatePreviousOrFailAsync(state, e.StepIndex, sagaId, ct);
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
public static IServiceCollection AddSagaOrchestration<TSaga, TInput>(
    this IServiceCollection services)
    where TSaga : class, ISagaState
{
    // Register step metadata
    // Register SagaOrchestrationEffect<TSaga>
    // Register discovered steps via DI
    // Register infrastructure event reducers
}
```

### 3. Infrastructure Reducers (provided by framework)

Built-in reducers that apply infrastructure events to update saga state:

```csharp
// Example: SagaStepCompletedReducer applies to any ISagaState
public sealed class SagaStepCompletedReducer<TSaga> 
    : EventReducerBase<SagaStepCompleted, TSaga>
    where TSaga : ISagaState
{
    public override TSaga Apply(TSaga state, SagaStepCompleted @event)
        => state with
        {
            LastCompletedStepIndex = @event.StepIndex
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
        services.AddSagaOrchestration<TransferSagaState, TransferSagaInput>();
        
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
    public Guid SagaId { get; init; }
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;
    public int LastCompletedStepIndex { get; init; } = -1;
    public string? CorrelationId { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public string? StepHash { get; init; }
    
    // Business data (IDs only)
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    
    // Transient external data (if needed)
    public string? TransferReference { get; init; }
}

// Step (POCO with attribute)
[SagaStep(Order = 0, Saga = typeof(TransferSagaState))]
public sealed class DebitSourceStep : ISagaStep<TransferSagaState>, ICompensatable<TransferSagaState>
{
    private IAccountGrainFactory AccountGrainFactory { get; }
    
    public async Task<StepResult> ExecuteAsync(TransferSagaState state, CancellationToken ct)
    {
        var grain = AccountGrainFactory.GetGrain(state.SourceAccountId);
        var result = await grain.DebitAsync(state.Amount, ct);
        return result.IsSuccess
            ? StepResult.Succeeded(new SourceDebited { Amount = state.Amount })
            : StepResult.Failed(result.Error);
    }
    
    public async Task<CompensationResult> CompensateAsync(TransferSagaState state, CancellationToken ct)
    {
        var grain = AccountGrainFactory.GetGrain(state.SourceAccountId);
        await grain.CreditAsync(state.Amount, ct); // Reverse the debit
        return CompensationResult.Succeeded();
    }
}
```

### 8. Tests

**Generator L0 Tests:**
- Client: [tests/Inlet.Client.Generators.L0Tests](../../tests/Inlet.Client.Generators.L0Tests)
- Server: [tests/Inlet.Server.Generators.L0Tests](../../tests/Inlet.Server.Generators.L0Tests)
- Silo: [tests/Inlet.Silo.Generators.L0Tests](../../tests/Inlet.Silo.Generators.L0Tests)

**Runtime L0 Tests:**
- `SagaStepInfo` registration ordering and step hash consistency
- `SagaOrchestrationEffect<T>` state machine logic
- Infrastructure reducers

**L2 Integration Tests:**
- End-to-end saga flow in `samples/Spring/Spring.L2Tests`
- Happy path: all steps succeed
- Compensation path: step fails, compensation runs
- Timeout/retry scenarios

**Testing Support:**
- Minimal POCO step doubles registered via DI for orchestration tests

## Data Model / API Changes

- New saga endpoints under `/api/sagas/{saga-name}`
- `StartSagaCommand<TInput>` input contract for saga starts
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