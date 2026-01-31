# Task 03: Client-Side Saga Generators

## Objective

Create source generators that produce client-side saga actions, effects, reducers, state, and registrations - enabling the same DX as aggregate commands.

## Rationale

After this task, developers can dispatch `StartTransferFundsSagaAction` from Blazor and everything flows automatically through generated infrastructure to the server.

## Deliverables

### 1. `SagaClientActionsGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientActionsGenerator.cs`

**Generates per saga:**
- `Start{SagaName}SagaAction.g.cs` - Primary action with saga input properties
- `Start{SagaName}SagaExecutingAction.g.cs` - Dispatched when HTTP call starts
- `Start{SagaName}SagaSucceededAction.g.cs` - Dispatched on 2xx response
- `Start{SagaName}SagaFailedAction.g.cs` - Dispatched on error

**Example output:**
```csharp
// Start{SagaName}SagaAction.g.cs
namespace Spring.Client.Features.TransferFundsSaga.Actions;

/// <summary>
///     Action to start a TransferFunds saga.
/// </summary>
public sealed record StartTransferFundsSagaAction(
    Guid SagaId,
    string SourceAccountId,
    string DestinationAccountId,
    decimal Amount,
    string? CorrelationId = null) : ISagaAction;
```

### 2. `SagaClientActionEffectsGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientActionEffectsGenerator.cs`

**Generates:** `Start{SagaName}SagaActionEffect.g.cs`

```csharp
namespace Spring.Client.Features.TransferFundsSaga.ActionEffects;

internal sealed class StartTransferFundsSagaActionEffect 
    : SagaActionEffectBase<
        StartTransferFundsSagaAction, 
        StartTransferFundsSagaDto, 
        TransferFundsSagaState>
{
    public StartTransferFundsSagaActionEffect(
        HttpClient httpClient,
        IMapper<StartTransferFundsSagaAction, StartTransferFundsSagaDto> mapper,
        IStore store)
        : base(httpClient, mapper, store)
    {
    }
    
    protected override string SagaRoute => "transfer-funds";
}
```

### 3. `SagaClientDtoGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientDtoGenerator.cs`

**Generates:** `Start{SagaName}SagaDto.g.cs`

Client-side DTO matching server DTO (same properties).

### 4. `SagaClientMappersGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientMappersGenerator.cs`

**Generates:** `Start{SagaName}SagaActionMapper.g.cs`

Maps action to DTO for HTTP serialization.

### 5. `SagaClientStateGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientStateGenerator.cs`

**Generates:** `{SagaName}SagaState.g.cs`

```csharp
namespace Spring.Client.Features.TransferFundsSaga.State;

/// <summary>
///     Client-side state for TransferFunds saga operations.
/// </summary>
public sealed record TransferFundsSagaState
{
    /// <summary>
    ///     Gets a value indicating whether a saga start is in progress.
    /// </summary>
    public bool IsExecuting { get; init; }
    
    /// <summary>
    ///     Gets the saga ID currently being executed, if any.
    /// </summary>
    public Guid? ExecutingSagaId { get; init; }
    
    /// <summary>
    ///     Gets the error message from the last failed saga start.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    ///     Gets the error code from the last failed saga start.
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    ///     Gets the last successfully started saga ID.
    /// </summary>
    public Guid? LastStartedSagaId { get; init; }
}
```

### 6. `SagaClientReducersGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientReducersGenerator.cs`

**Generates:** Reducers for executing/succeeded/failed actions

```csharp
namespace Spring.Client.Features.TransferFundsSaga.Reducers;

internal sealed class StartTransferFundsSagaExecutingReducer 
    : IReducer<StartTransferFundsSagaExecutingAction, TransferFundsSagaState>
{
    public TransferFundsSagaState Reduce(
        StartTransferFundsSagaExecutingAction action, 
        TransferFundsSagaState state)
    {
        return state with 
        { 
            IsExecuting = true, 
            ExecutingSagaId = action.SagaId,
            ErrorMessage = null,
            ErrorCode = null
        };
    }
}
```

### 7. `SagaClientRegistrationGenerator`

**Location:** `src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs`

**Generates:** `{SagaName}SagaFeatureRegistrations.g.cs`

```csharp
namespace Spring.Client.Features.TransferFundsSaga;

public static class TransferFundsSagaFeatureRegistrations
{
    public static IServiceCollection AddTransferFundsSagaFeature(
        this IServiceCollection services)
    {
        // Register state
        services.AddSingleton<TransferFundsSagaState>();
        
        // Register reducers
        services.AddReducer<StartTransferFundsSagaExecutingAction, 
            TransferFundsSagaState, StartTransferFundsSagaExecutingReducer>();
        services.AddReducer<StartTransferFundsSagaSucceededAction, 
            TransferFundsSagaState, StartTransferFundsSagaSucceededReducer>();
        services.AddReducer<StartTransferFundsSagaFailedAction, 
            TransferFundsSagaState, StartTransferFundsSagaFailedReducer>();
        
        // Register action effect
        services.AddActionEffect<StartTransferFundsSagaAction, 
            StartTransferFundsSagaActionEffect>();
        
        // Register mapper
        services.AddSingleton<IMapper<StartTransferFundsSagaAction, 
            StartTransferFundsSagaDto>, StartTransferFundsSagaActionMapper>();
        
        return services;
    }
}
```

## Generator Detection Logic

1. Scan referenced assemblies for types with `[GenerateSagaEndpoints]`
2. Extract `InputType` from attribute
3. Analyze input type properties
4. Generate all client artifacts in `{RootNamespace}.Client.Features.{SagaName}Saga.*`

## Namespace Mapping

| Domain Namespace | Client Namespace |
|-----------------|------------------|
| `Spring.Domain.Sagas.TransferFunds` | `Spring.Client.Features.TransferFundsSaga` |
| `.../TransferFundsSagaInput` | `.../Actions/StartTransferFundsSagaAction` |
| `.../TransferFundsSagaState` | `.../State/TransferFundsSagaState` |

## Acceptance Criteria

- [ ] All 7 generators compile and produce valid code
- [ ] Generated actions implement `ISagaAction`
- [ ] Generated effect inherits `SagaActionEffectBase`
- [ ] Generated reducers properly update state
- [ ] Registration method wires everything correctly
- [ ] Zero build warnings in generated code
- [ ] Naming follows existing aggregate patterns

## Dependencies

- [01-saga-abstractions](01-saga-abstractions.md) - `ISagaAction`, `SagaActionEffectBase`
- Existing `Inlet.Client.Generators` infrastructure

## Blocked By

- [01-saga-abstractions](01-saga-abstractions.md)

## Blocks

- [04-generator-tests](04-generator-tests.md)
- [07-transfer-page](07-transfer-page.md)
