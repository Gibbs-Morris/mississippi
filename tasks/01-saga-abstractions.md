# Task 01: Saga Client Abstractions

## Objective

Add base abstractions for saga actions and effects in `Inlet.Client.Abstractions` and `Inlet.Blazor.WebAssembly.Abstractions` to enable generated saga client code.

## Rationale

Aggregates have `ICommandAction` and `CommandActionEffectBase`. Sagas need equivalent abstractions for the generators to target.

## Deliverables

### 1. `ISagaAction` interface

**Location:** `src/Inlet.Client.Abstractions/Actions/ISagaAction.cs`

```csharp
/// <summary>
///     Marker interface for saga start actions.
/// </summary>
/// <remarks>
///     Unlike command actions which target an entity by string ID,
///     saga actions target a saga instance by Guid.
/// </remarks>
public interface ISagaAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for the saga instance.
    /// </summary>
    Guid SagaId { get; }
    
    /// <summary>
    ///     Gets an optional correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }
}
```

### 2. `ISagaExecutingAction`, `ISagaSucceededAction`, `ISagaFailedAction`

**Location:** `src/Inlet.Client.Abstractions/Actions/` (one file each or combined)

Similar pattern to command actions but with `SagaId` instead of `EntityId`.

### 3. `SagaActionEffectBase<TAction, TDto, TState>` base class

**Location:** `src/Inlet.Blazor.WebAssembly.Abstractions/ActionEffects/SagaActionEffectBase.cs`

```csharp
/// <summary>
///     Base class for saga start action effects.
/// </summary>
/// <typeparam name="TAction">The saga start action type.</typeparam>
/// <typeparam name="TDto">The server DTO type.</typeparam>
/// <typeparam name="TState">The client saga state type.</typeparam>
public abstract class SagaActionEffectBase<TAction, TDto, TState> : IActionEffect<TAction>
    where TAction : ISagaAction
    where TDto : class
    where TState : class, new()
{
    /// <summary>
    ///     Gets the saga route segment (e.g., "transfer-funds").
    /// </summary>
    protected abstract string SagaRoute { get; }
    
    /// <summary>
    ///     Maps the action to a server DTO.
    /// </summary>
    protected abstract TDto MapToDto(TAction action);
    
    // Implementation: POST to /api/sagas/{SagaRoute}/{action.SagaId}
    // Dispatch executing/succeeded/failed actions
}
```

## Acceptance Criteria

- [ ] `ISagaAction` interface exists with `SagaId` and `CorrelationId` properties
- [ ] Executing/Succeeded/Failed saga action interfaces exist
- [ ] `SagaActionEffectBase` compiles and follows same pattern as `CommandActionEffectBase`
- [ ] XML documentation complete
- [ ] Zero build warnings

## Dependencies

- None (pure abstractions)

## Blocked By

- Nothing

## Blocks

- [02-server-generators](02-server-generators.md)
- [03-client-generators](03-client-generators.md)
