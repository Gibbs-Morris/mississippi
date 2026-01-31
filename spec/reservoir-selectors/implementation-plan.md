# Implementation Plan — Reservoir Selectors

## Overview

Phased rollout prioritizing developer experience and minimal risk. Each phase is independently valuable.

---

## Phase 1: Core API (Manual Selectors)

**Goal**: Enable developers to write and use selectors today with zero code generation.

### Step 1.1: Add Selector Extension Methods to Reservoir.Abstractions

**Files to create/modify**:
- `src/Reservoir.Abstractions/SelectorExtensions.cs` (new)

**Implementation**:
```csharp
namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Extension methods for selecting derived state from the store.
/// </summary>
public static class SelectorExtensions
{
    /// <summary>
    ///     Selects a derived value from a single feature state.
    /// </summary>
    public static TResult Select<TState, TResult>(
        this IStore store,
        Func<TState, TResult> selector)
        where TState : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        return selector(store.GetState<TState>());
    }

    /// <summary>
    ///     Selects a derived value from two feature states.
    /// </summary>
    public static TResult Select<TState1, TState2, TResult>(
        this IStore store,
        Func<TState1, TState2, TResult> selector)
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        return selector(store.GetState<TState1>(), store.GetState<TState2>());
    }

    /// <summary>
    ///     Selects a derived value from three feature states.
    /// </summary>
    public static TResult Select<TState1, TState2, TState3, TResult>(
        this IStore store,
        Func<TState1, TState2, TState3, TResult> selector)
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState
        where TState3 : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        return selector(
            store.GetState<TState1>(),
            store.GetState<TState2>(),
            store.GetState<TState3>());
    }
}
```

**Tests**:
- `tests/Reservoir.Abstractions.L0Tests/SelectorExtensionsTests.cs`

### Step 1.2: Add Select Methods to StoreComponent

**Files to modify**:
- `src/Reservoir.Blazor/StoreComponent.cs`

**Implementation**:
```csharp
/// <summary>
///     Selects a derived value from a single feature state.
/// </summary>
protected TResult Select<TState, TResult>(Func<TState, TResult> selector)
    where TState : class, IFeatureState
    => Store.Select(selector);

/// <summary>
///     Selects a derived value from two feature states.
/// </summary>
protected TResult Select<TState1, TState2, TResult>(
    Func<TState1, TState2, TResult> selector)
    where TState1 : class, IFeatureState
    where TState2 : class, IFeatureState
    => Store.Select(selector);

/// <summary>
///     Selects a derived value from three feature states.
/// </summary>
protected TResult Select<TState1, TState2, TState3, TResult>(
    Func<TState1, TState2, TState3, TResult> selector)
    where TState1 : class, IFeatureState
    where TState2 : class, IFeatureState
    where TState3 : class, IFeatureState
    => Store.Select(selector);
```

**Tests**:
- `tests/Reservoir.Blazor.L0Tests/StoreComponentSelectorTests.cs`

### Step 1.3: Add Documentation

**Files to create**:
- `docs/Docusaurus/docs/client-state-management/selectors.md`

**Content outline**:
1. What are selectors?
2. Why use selectors?
3. Writing selectors (static methods)
4. Using selectors in components
5. Composing selectors
6. Testing selectors
7. Best practices and rules

**Files to modify**:
- `docs/Docusaurus/docs/client-state-management/reservoir.md` — Add selectors to Learn More section
- `docs/Docusaurus/docs/client-state-management/store.md` — Add Select method documentation
- `docs/Docusaurus/docs/client-state-management/store-component.md` — Add Select method documentation

### Step 1.4: Add Spring Sample Selectors

**Files to create**:
- `samples/Spring/Spring.Client/Features/EntitySelection/EntitySelectionSelectors.cs`

**Example implementation**:
```csharp
namespace Spring.Client.Features.EntitySelection;

/// <summary>
///     Pure selector functions for entity selection state.
/// </summary>
public static class EntitySelectionSelectors
{
    /// <summary>
    ///     Gets whether an entity is currently selected.
    /// </summary>
    public static bool HasSelection(EntitySelectionState state)
        => !string.IsNullOrEmpty(state.EntityId);

    /// <summary>
    ///     Gets the selected entity ID or a default value.
    /// </summary>
    public static string GetEntityIdOrDefault(EntitySelectionState state, string defaultValue = "")
        => state.EntityId ?? defaultValue;
}
```

### Step 1.5: Update Framework Instructions

**Files to modify**:
- `.github/instructions/mississippi-framework.instructions.md`

**Add section**: Selectors (after Action Effects section)

---

## Phase 2: Selector Conventions and Analyzer

**Goal**: Enforce purity and best practices via tooling.

### Step 2.1: Create Roslyn Analyzer for Selector Purity

**Files to create**:
- `src/Reservoir.Analyzers/SelectorPurityAnalyzer.cs`
- `src/Reservoir.Analyzers/DiagnosticIds.cs`

**Rules to enforce**:
- Selector methods must be static
- Selector methods must not call instance methods on injected services
- Selector methods must not access mutable static state
- Selector methods must return a value (not void)

**Diagnostic IDs**:
- `MISS001`: Selector method must be static
- `MISS002`: Selector method must not have side effects
- `MISS003`: Selector method must return a value

### Step 2.2: Add EditorConfig Rules

**Files to modify**:
- `.editorconfig` or `Directory.Build.props`

**Configuration**:
```ini
# Selector purity rules
dotnet_diagnostic.MISS001.severity = error
dotnet_diagnostic.MISS002.severity = warning
dotnet_diagnostic.MISS003.severity = error
```

---

## Phase 3: Source Generation

**Goal**: Generate common selector scaffolding from Domain types.

### Step 3.1: Add GenerateSelectors Attribute

**Files to create**:
- `src/Inlet.Generators.Abstractions/GenerateSelectorsAttribute.cs`

**Implementation**:
```csharp
namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Triggers selector scaffolding generation for a projection or feature state.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateSelectorsAttribute : Attribute
{
}
```

### Step 3.2: Create Client Selector Generator

**Files to create**:
- `src/Inlet.Client.Generators/SelectorGenerator.cs`

**Generated output for projection**:
```csharp
// <auto-generated />
namespace Spring.Client.Features.BankAccountBalance.Selectors;

/// <summary>
///     Auto-generated selectors for BankAccountBalance projection.
/// </summary>
public static partial class BankAccountBalanceSelectors
{
    /// <summary>
    ///     Gets the current projection data.
    /// </summary>
    public static BankAccountBalanceDto? GetProjection(BankAccountBalanceState state)
        => state.Projection;

    /// <summary>
    ///     Gets whether the projection is loading.
    /// </summary>
    public static bool IsLoading(BankAccountBalanceState state)
        => state.IsLoading;

    /// <summary>
    ///     Gets the error message if any.
    /// </summary>
    public static string? GetError(BankAccountBalanceState state)
        => state.Error;

    /// <summary>
    ///     Gets whether the projection has data.
    /// </summary>
    public static bool HasData(BankAccountBalanceState state)
        => state.Projection is not null;
}
```

### Step 3.3: Generator Tests

**Files to create**:
- `tests/Inlet.Client.Generators.L0Tests/SelectorGeneratorTests.cs`

---

## Phase 4: Memoization (Optional)

**Goal**: Provide opt-in caching for expensive selectors.

### Step 4.1: Add Memoization Utilities

**Files to create**:
- `src/Reservoir/Selectors/Memoize.cs`

**Implementation**:
```csharp
namespace Mississippi.Reservoir.Selectors;

/// <summary>
///     Utilities for creating memoized selectors.
/// </summary>
public static class Memoize
{
    /// <summary>
    ///     Creates a memoized version of a selector that caches the result
    ///     when the input state reference is unchanged.
    /// </summary>
    public static Func<TState, TResult> Create<TState, TResult>(
        Func<TState, TResult> selector)
        where TState : class
    {
        TState? lastInput = null;
        TResult? lastResult = default;

        return state =>
        {
            if (ReferenceEquals(state, lastInput))
            {
                return lastResult!;
            }

            lastInput = state;
            lastResult = selector(state);
            return lastResult;
        };
    }
}
```

### Step 4.2: Memoization Documentation

**Files to modify**:
- `docs/Docusaurus/docs/client-state-management/selectors.md` — Add memoization section

---

## Test Plan

### L0 Tests (Unit)

| Test Class | Coverage |
|------------|----------|
| `SelectorExtensionsTests` | Extension methods on IStore |
| `StoreComponentSelectorTests` | Select methods in component |
| `MemoizeTests` | Memoization utility |
| `SelectorGeneratorTests` | Source generation |
| `SelectorPurityAnalyzerTests` | Analyzer rules |

### L1 Tests (Light Integration)

| Test Class | Coverage |
|------------|----------|
| `SelectorIntegrationTests` | End-to-end selector flow with real store |

### Sample Validation

| Validation | Location |
|------------|----------|
| EntitySelectionSelectors usage | Spring.Client components |
| Generated selectors | BankAccountBalance feature |

---

## Rollout Plan

| Phase | Scope | Risk | Duration |
|-------|-------|------|----------|
| Phase 1 | Core API + Docs + Sample | Low | 1-2 days |
| Phase 2 | Analyzer | Low | 1 day |
| Phase 3 | Source Generation | Medium | 2-3 days |
| Phase 4 | Memoization | Low | 1 day |

**Recommended approach**: Ship Phase 1 first, gather feedback, then continue.

---

## Validation Checklist

- [ ] `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1` passes
- [ ] `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1` passes
- [ ] `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` passes
- [ ] `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` passes
- [ ] `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1` passes
- [ ] Documentation builds without errors
- [ ] Spring sample demonstrates selector usage

---

## Files Changed Summary

### Phase 1 (Core API)

| Operation | Path |
|-----------|------|
| Create | `src/Reservoir.Abstractions/SelectorExtensions.cs` |
| Modify | `src/Reservoir.Blazor/StoreComponent.cs` |
| Create | `tests/Reservoir.Abstractions.L0Tests/SelectorExtensionsTests.cs` |
| Create | `tests/Reservoir.Blazor.L0Tests/StoreComponentSelectorTests.cs` |
| Create | `docs/Docusaurus/docs/client-state-management/selectors.md` |
| Modify | `docs/Docusaurus/docs/client-state-management/reservoir.md` |
| Modify | `docs/Docusaurus/docs/client-state-management/store.md` |
| Modify | `docs/Docusaurus/docs/client-state-management/store-component.md` |
| Create | `samples/Spring/Spring.Client/Features/EntitySelection/EntitySelectionSelectors.cs` |
| Modify | `.github/instructions/mississippi-framework.instructions.md` |

### Phase 2 (Analyzer)

| Operation | Path |
|-----------|------|
| Create | `src/Reservoir.Analyzers/Reservoir.Analyzers.csproj` |
| Create | `src/Reservoir.Analyzers/SelectorPurityAnalyzer.cs` |
| Create | `src/Reservoir.Analyzers/DiagnosticIds.cs` |
| Create | `tests/Reservoir.Analyzers.L0Tests/SelectorPurityAnalyzerTests.cs` |

### Phase 3 (Generation)

| Operation | Path |
|-----------|------|
| Create | `src/Inlet.Generators.Abstractions/GenerateSelectorsAttribute.cs` |
| Modify | `src/Inlet.Client.Generators/...` |
| Create | `tests/Inlet.Client.Generators.L0Tests/SelectorGeneratorTests.cs` |

### Phase 4 (Memoization)

| Operation | Path |
|-----------|------|
| Create | `src/Reservoir/Selectors/Memoize.cs` |
| Create | `tests/Reservoir.L0Tests/Selectors/MemoizeTests.cs` |
