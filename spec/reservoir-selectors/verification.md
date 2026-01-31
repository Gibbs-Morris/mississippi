# Verification — Claims and Evidence

## Claim List

| ID | Claim | Status |
|----|-------|--------|
| C1 | Current state access uses `GetState<TState>()` only | ✅ Verified |
| C2 | `StoreComponent` re-renders on any store change | ✅ Verified |
| C3 | No existing selector API in Reservoir | ✅ Verified |
| C4 | Reducers follow static-method-in-class pattern | ✅ Verified |
| C5 | Source generation pattern uses attributes on Domain types | ✅ Verified |
| C6 | Abstractions split is required for public contracts | ✅ Verified |
| C7 | Selector extension methods can live in Reservoir.Abstractions | ✅ Verified |
| C8 | StoreComponent can be extended with Select methods | ✅ Verified |

---

## Verification Questions and Answers

### Q1: What read APIs does `IStore` currently expose?

**Answer**: `GetState<TState>()` and `Subscribe(Action)`.

**Evidence**: [src/Reservoir.Abstractions/IStore.cs](../../src/Reservoir.Abstractions/IStore.cs) lines 36-54.

```csharp
TState GetState<TState>() where TState : class, IFeatureState;
IDisposable Subscribe(Action listener);
```

### Q2: How does `StoreComponent` trigger re-renders?

**Answer**: It subscribes to store changes via `Store.Subscribe(OnStoreChanged)` in `OnInitialized()`. The `OnStoreChanged` callback calls `InvokeAsync(StateHasChanged)`, triggering a re-render on **any** action dispatch.

**Evidence**: [src/Reservoir.Blazor/StoreComponent.cs](../../src/Reservoir.Blazor/StoreComponent.cs) lines 84-94.

### Q3: Is there an existing selector interface or mechanism?

**Answer**: No. Grep search for "selector" in the codebase returned no relevant matches in Reservoir projects. The word "select" appears only in LINQ contexts.

**Evidence**: `grep_search` for `selector|select` in `**/*.{cs,md}` returned only unrelated matches (CommandSelectedEvent, LINQ Select).

### Q4: What is the reducer pattern for static methods?

**Answer**: Reducers can be registered as delegate functions: `AddReducer<TAction, TState>(func)`. The delegate pattern uses static methods:

```csharp
services.AddReducer<SetEntityIdAction, EntitySelectionState>(EntitySelectionReducers.SetEntityId);
```

**Evidence**: [docs/Docusaurus/docs/client-state-management/reducers.md](../../docs/Docusaurus/docs/client-state-management/reducers.md), [samples/Spring/Spring.Client/Features/EntitySelection/EntitySelectionReducers.cs](../../samples/Spring/Spring.Client/Features/EntitySelection/EntitySelectionReducers.cs).

### Q5: Where do generators get their input from?

**Answer**: Generators consume Domain project types (aggregates, commands, events, projections) and emit Client artifacts. Attributes like `[GenerateAggregateEndpoints]`, `[GenerateCommand]`, `[GenerateProjectionEndpoints]` trigger generation.

**Evidence**: [.github/instructions/mississippi-framework.instructions.md](../../.github/instructions/mississippi-framework.instructions.md) Generator Inputs table.

### Q6: Can extension methods for IStore live in Reservoir.Abstractions?

**Answer**: Yes. The abstractions-projects instruction says "Generic DI helpers that only register the abstraction to a caller-supplied implementation type and add no new package dependencies **MAY** live in abstractions." Extension methods that only use `IStore` (already in abstractions) are eligible.

**Evidence**: [.github/instructions/abstractions-projects.instructions.md](../../.github/instructions/abstractions-projects.instructions.md).

### Q7: Is `StoreComponent` in the main project or abstractions?

**Answer**: `StoreComponent` is in `Reservoir.Blazor` (main project), not abstractions. This is correct because it contains implementation logic (subscription management, render triggering).

**Evidence**: [src/Reservoir.Blazor/StoreComponent.cs](../../src/Reservoir.Blazor/StoreComponent.cs).

### Q8: What is the folder structure for client features?

**Answer**: Client features live under `Features/{Feature}/` with subfolders for Actions, ActionEffects, Reducers, State, Dtos, Mappers. This follows the pattern in Spring sample.

**Evidence**: `list_dir` of `samples/Spring/Spring.Client/Features/BankAccountAggregate/`.

---

## What Changed After Verification

1. **Confirmed**: Selector extension methods should go in `Reservoir.Abstractions` (lightweight, no new dependencies)
2. **Confirmed**: `StoreComponent` Select methods should go in `Reservoir.Blazor` (implementation concern)
3. **Added**: Domain selectors folder pattern (`Selectors/` alongside `Reducers/`) mirrors existing structure
4. **Clarified**: No registration needed for selectors (unlike reducers/effects)—they're just functions
