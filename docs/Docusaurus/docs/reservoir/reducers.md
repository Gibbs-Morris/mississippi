---
sidebar_position: 3
title: Reducers
description: Transform state in response to actions with pure, predictable functions
---

# Reducers

Reducers are pure functions that specify how the application state changes in response to actions. Given the current state and an action, a reducer returns the new state. Reducers are the only place where state transitions occur, making them the single source of truth for how your application behaves.

## Core Principles

Reducers must follow these fundamental principles:

1. **Pure functions** — Given the same state and action, always return the same new state
2. **No side effects** — No API calls, no logging, no mutations outside the function
3. **Immutable updates** — Return a new state object; never mutate the existing state
4. **Synchronous** — Execute instantly without `await` or callbacks

## The IActionReducer Interface

Reservoir provides two reducer interfaces:

```csharp
/// <summary>
/// Defines a reducer that transforms a specific action type into a new state.
/// </summary>
/// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
/// <typeparam name="TState">The state type produced by the reducer.</typeparam>
public interface IActionReducer<in TAction, TState> : IActionReducer<TState>
    where TAction : IAction
    where TState : class
{
    /// <summary>
    /// Reduces the action into the current state, producing a new state.
    /// </summary>
    TState Reduce(TState state, TAction action);
}
```

The strongly-typed `IActionReducer<TAction, TState>` is the primary interface for implementing reducers. Each reducer handles exactly one action type, making them easy to test and reason about.

## Implementing Reducers

### Class-Based Reducers

Inherit from `ReducerBase<TAction, TState>` for type-safe, testable reducers:

```csharp
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;

public sealed record CounterState : IFeatureState
{
    public static string FeatureKey => "counter";
    public int Count { get; init; }
}

public sealed record IncrementAction : IAction;

public sealed class IncrementReducer : ActionReducerBase<IncrementAction, CounterState>
{
    public override CounterState Reduce(CounterState state, IncrementAction action)
        => state with { Count = state.Count + 1 };
}
```

Register class-based reducers with dependency injection:

```csharp
services.AddReducer<IncrementAction, CounterState, IncrementReducer>();
```

### Delegate Reducers

For simple reducers, use inline delegates:

```csharp
services.AddReducer<IncrementAction, CounterState>(
    (state, action) => state with { Count = state.Count + 1 });

services.AddReducer<DecrementAction, CounterState>(
    (state, action) => state with { Count = state.Count - 1 });

services.AddReducer<SetCountAction, CounterState>(
    (state, action) => state with { Count = action.Value });
```

### Grouped Static Reducers

For feature organization, group related reducers in a static class:

```csharp
using Mississippi.Reservoir.Abstractions.State;

public sealed record CartState : IFeatureState
{
    public static string FeatureKey => "cart";
    
    public ImmutableList<CartItem> Items { get; init; } = [];
    public ImmutableList<string> AvailableProducts { get; init; } = [];
    public bool IsLoadingProducts { get; init; }
    public string? ProductsError { get; init; }
}

/// <summary>
/// Contains reducer functions for the shopping cart feature.
/// </summary>
internal static class CartReducers
{
    public static CartState AddItem(CartState state, AddItemAction action)
    {
        var newItem = new CartItem(Guid.NewGuid().ToString("N"), action.ItemName, 1);
        return state with { Items = state.Items.Add(newItem) };
    }

    public static CartState RemoveItem(CartState state, RemoveItemAction action)
        => state with { Items = state.Items.RemoveAll(item => item.Id == action.ItemId) };

    public static CartState ProductsLoading(CartState state, ProductsLoadingAction action)
        => state with { IsLoadingProducts = true, ProductsError = null };

    public static CartState ProductsLoaded(CartState state, ProductsLoadedAction action)
        => state with 
        { 
            IsLoadingProducts = false, 
            AvailableProducts = action.Products,
            ProductsError = null 
        };

    public static CartState ProductsLoadFailed(CartState state, ProductsLoadFailedAction action)
        => state with { IsLoadingProducts = false, ProductsError = action.Error };
}
```

Register grouped reducers by referencing the static methods:

```csharp
services.AddReducer<AddItemAction, CartState>(CartReducers.AddItem);
services.AddReducer<RemoveItemAction, CartState>(CartReducers.RemoveItem);
services.AddReducer<ProductsLoadingAction, CartState>(CartReducers.ProductsLoading);
services.AddReducer<ProductsLoadedAction, CartState>(CartReducers.ProductsLoaded);
services.AddReducer<ProductsLoadFailedAction, CartState>(CartReducers.ProductsLoadFailed);
```

## Feature State

Feature states must implement `IFeatureState`:

```csharp
public interface IFeatureState
{
    /// <summary>
    /// Gets the unique key identifying this feature state in the store.
    /// </summary>
    static abstract string FeatureKey { get; }
}
```

### Defining Feature State

Feature states should be immutable records:

```csharp
public sealed record SidebarState : IFeatureState
{
    public static string FeatureKey => "sidebar";
    
    public bool IsOpen { get; init; }
    public string ActivePanel { get; init; } = string.Empty;
}
```

### State Design Guidelines

| Guideline | Reason |
|-----------|--------|
| Use records | Provides immutability and value equality |
| Use `init`-only properties | Enables `with` expressions for immutable updates |
| Use immutable collections | `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableDictionary<K,V>` |
| Provide sensible defaults | Initialize properties to prevent null reference issues |
| Keep state flat | Avoid deep nesting; normalize data when possible |

## The Root Reducer

Reservoir automatically composes individual reducers into a `RootReducer<TState>` for each feature state. The root reducer:

1. Receives dispatched actions from the store
2. Routes actions to the appropriate typed reducers using a precomputed type index
3. Applies each matching reducer in registration order
4. Returns the final state

```csharp
// The root reducer is registered automatically when you add reducers
services.AddReducer<IncrementAction, CounterState, IncrementReducer>();
// This internally calls: services.AddRootReducer<CounterState>();
```

You can also register it explicitly if needed:

```csharp
services.AddRootReducer<CounterState>();
```

## Registration Methods

| Method | Use Case |
|--------|----------|
| `AddReducer<TAction, TState>(delegate)` | Inline reducer logic for simple cases |
| `AddReducer<TAction, TState, TReducer>()` | Class-based reducers with testability |
| `AddRootReducer<TState>()` | Explicit root reducer registration (usually automatic) |

## Immutable State Updates

Always return new state objects instead of mutating existing state. C# records with `with` expressions make this natural:

### Simple Property Updates

```csharp
public override CounterState Reduce(CounterState state, IncrementAction action)
    => state with { Count = state.Count + 1 };
```

### Collection Updates

```csharp
// Adding to a collection
public override CartState Reduce(CartState state, AddItemAction action)
{
    var newItem = new CartItem(Guid.NewGuid().ToString("N"), action.ItemName, 1);
    return state with { Items = state.Items.Add(newItem) };
}

// Removing from a collection
public override CartState Reduce(CartState state, RemoveItemAction action)
    => state with { Items = state.Items.RemoveAll(item => item.Id == action.ItemId) };

// Updating an item in a collection
public override CartState Reduce(CartState state, UpdateQuantityAction action)
{
    var updatedItems = state.Items
        .Select(item => item.Id == action.ItemId 
            ? item with { Quantity = action.Quantity } 
            : item)
        .ToImmutableList();
    return state with { Items = updatedItems };
}
```

### Nested Object Updates

```csharp
public sealed record AppState : IFeatureState
{
    public static string FeatureKey => "app";
    public UserSettings Settings { get; init; } = new();
}

public sealed record UserSettings
{
    public string Theme { get; init; } = "light";
    public string Language { get; init; } = "en";
}

public override AppState Reduce(AppState state, ChangeThemeAction action)
    => state with { Settings = state.Settings with { Theme = action.Theme } };
```

## Rules and Limitations

### Rules

1. **One reducer per action-state pair.** Each reducer handles exactly one action type for one state type.

2. **Reducers must be pure.** Given the same inputs, always produce the same output with no side effects.

3. **Reducers must be synchronous.** Use effects for async operations.

4. **Return the original state if no changes.** When an action doesn't apply, return the unchanged state for efficiency.

### Limitations

1. **No async/await in reducers.** Reducers execute synchronously; use effects for async work.

2. **No service dependencies in delegate reducers.** Class-based reducers can accept constructor dependencies if needed, but keep reducers focused on state transformation.

3. **No direct store access.** Reducers receive only state and action—they cannot dispatch additional actions.

## Best Practices

### Do

- ✅ Keep reducers small and focused on a single action
- ✅ Use `with` expressions for immutable updates
- ✅ Group related reducers in static classes for organization
- ✅ Write unit tests for each reducer
- ✅ Use class-based reducers when logic is complex or requires testing
- ✅ Return the original state reference when no changes are needed

### Don't

- ❌ Mutate the incoming state object
- ❌ Perform API calls or I/O operations
- ❌ Access global or static mutable state
- ❌ Throw exceptions for normal control flow
- ❌ Include logging or side effects (use middleware instead)

## Testing Reducers

Reducers are pure functions, making them trivial to test:

```csharp
public sealed class IncrementReducerTests
{
    [Fact]
    public void Reduce_WithIncrementAction_IncrementsCounter()
    {
        // Arrange
        var sut = new IncrementReducer();
        var initialState = new CounterState { Count = 5 };
        var action = new IncrementAction();

        // Act
        var result = sut.Reduce(initialState, action);

        // Assert
        Assert.Equal(6, result.Count);
    }

    [Fact]
    public void Reduce_DoesNotMutateOriginalState()
    {
        // Arrange
        var sut = new IncrementReducer();
        var initialState = new CounterState { Count = 5 };
        var action = new IncrementAction();

        // Act
        var result = sut.Reduce(initialState, action);

        // Assert
        Assert.Equal(5, initialState.Count); // Original unchanged
        Assert.NotSame(initialState, result); // New instance returned
    }
}
```

For delegate reducers:

```csharp
[Fact]
public void AddItem_AddsNewItemToCart()
{
    // Arrange
    var initialState = new CartState();
    var action = new AddItemAction("Widget");

    // Act
    var result = CartReducers.AddItem(initialState, action);

    // Assert
    Assert.Single(result.Items);
    Assert.Equal("Widget", result.Items[0].Name);
}
```

## Next Steps

- Learn how [Effects](./effects.md) handle asynchronous operations
- See how the [Store](./store.md) coordinates reducers and state
- Review [Actions](./actions.md) for defining what triggers reducers
