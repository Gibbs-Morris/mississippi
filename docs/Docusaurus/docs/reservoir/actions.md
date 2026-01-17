---
sidebar_position: 2
title: Actions
description: Define events and intents that trigger state changes in Reservoir
---

# Actions

Actions are the sole mechanism for triggering state changes in a Reservoir application. They represent events that have occurred or intents from users and systems. Actions flow through the store, are processed by reducers to update state, and may trigger effects for asynchronous operations.

## The IAction Interface

All actions must implement the `IAction` marker interface:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;

namespace Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
/// Marker interface for all actions in the Reservoir state management system.
/// </summary>
public interface IAction
{
}
```

The interface is intentionally empty—it serves as a type marker that enables the store to accept any action while providing compile-time type safety.

## Defining Actions

Actions should be defined as immutable C# records. Records provide value equality semantics and a concise syntax that makes actions easy to read and reason about.

### Simple Actions

For actions that carry no data, use a simple record declaration:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
/// Dispatched when the user clicks the increment button.
/// </summary>
public sealed record IncrementCounterAction : IAction;

/// <summary>
/// Dispatched to reset the counter to its initial state.
/// </summary>
public sealed record ResetCounterAction : IAction;
```

### Actions with Payloads

When an action needs to carry data, use primary constructor parameters:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
/// Dispatched when the user sets a specific counter value.
/// </summary>
/// <param name="Value">The new counter value to set.</param>
public sealed record SetCounterValueAction(int Value) : IAction;

/// <summary>
/// Dispatched when a user submits their profile information.
/// </summary>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
public sealed record UpdateUserProfileAction(string DisplayName, string Email) : IAction;
```

### Actions with Complex Payloads

For actions with many properties or optional data, consider using `init`-only properties:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
/// Dispatched when search criteria are updated.
/// </summary>
public sealed record UpdateSearchCriteriaAction : IAction
{
    /// <summary>
    /// Gets the search query text.
    /// </summary>
    public required string Query { get; init; }
    
    /// <summary>
    /// Gets the category filter, if any.
    /// </summary>
    public string? Category { get; init; }
    
    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int PageSize { get; init; } = 20;
    
    /// <summary>
    /// Gets the page number for pagination.
    /// </summary>
    public int Page { get; init; } = 1;
}
```

## Action Naming Conventions

Follow these naming conventions for clarity and consistency:

| Pattern | Use Case | Examples |
|---------|----------|----------|
| `{Noun}{Verb}Action` | User-initiated actions | `CounterIncrementAction`, `CartItemAddAction` |
| `{Verb}{Noun}Action` | Command-style actions | `IncrementCounterAction`, `AddCartItemAction` |
| `{Noun}{State}Action` | State transition actions | `ProductsLoadingAction`, `UserAuthenticatedAction` |
| `{Noun}{Event}Action` | Event-driven actions | `ProductsLoadedAction`, `ConnectionLostAction` |

Choose a convention and apply it consistently within your application. The examples in this documentation use the `{Verb}{Noun}Action` pattern.

## Action Categories

### Command Actions

Command actions represent user intents and typically trigger both reducers and effects:

```csharp
/// <summary>
/// Dispatched when the user requests to load products from the API.
/// </summary>
public sealed record LoadProductsAction : IAction;

/// <summary>
/// Dispatched when the user adds an item to their shopping cart.
/// </summary>
/// <param name="ItemName">The name of the item to add.</param>
public sealed record AddItemAction(string ItemName) : IAction;

/// <summary>
/// Dispatched when the user removes an item from their shopping cart.
/// </summary>
/// <param name="ItemId">The unique identifier of the item to remove.</param>
public sealed record RemoveItemAction(string ItemId) : IAction;
```

### Event Actions

Event actions represent things that have happened, typically dispatched by effects:

```csharp
/// <summary>
/// Dispatched when products are being fetched from the API.
/// </summary>
public sealed record ProductsLoadingAction : IAction;

/// <summary>
/// Dispatched when products have been successfully loaded.
/// </summary>
/// <param name="Products">The list of loaded products.</param>
public sealed record ProductsLoadedAction(ImmutableList<string> Products) : IAction;

/// <summary>
/// Dispatched when product loading fails.
/// </summary>
/// <param name="Error">The error message describing the failure.</param>
public sealed record ProductsLoadFailedAction(string Error) : IAction;
```

## Complete Example

The following example shows a complete set of actions for a shopping cart feature:

```csharp
using System.Collections.Immutable;
using Mississippi.Reservoir.Abstractions.Actions;

namespace MyApp.Cart;

// Command actions (user intents)
public sealed record LoadProductsAction : IAction;
public sealed record AddItemAction(string ItemName) : IAction;
public sealed record RemoveItemAction(string ItemId) : IAction;
public sealed record UpdateItemQuantityAction(string ItemId, int Quantity) : IAction;
public sealed record ClearCartAction : IAction;
public sealed record CheckoutAction : IAction;

// Event actions (things that happened)
public sealed record ProductsLoadingAction : IAction;
public sealed record ProductsLoadedAction(ImmutableList<string> Products) : IAction;
public sealed record ProductsLoadFailedAction(string Error) : IAction;
public sealed record CheckoutStartedAction : IAction;
public sealed record CheckoutCompletedAction(string OrderId) : IAction;
public sealed record CheckoutFailedAction(string Error) : IAction;
```

## Rules and Limitations

### Rules

1. **Actions must be immutable.** Once created, an action's data must not change. Using C# records ensures this by default.

2. **Actions must implement `IAction`.** This is enforced at compile time by the reducer and effect type constraints.

3. **Actions should be self-describing.** The action type and properties should fully describe the event or intent without requiring external context.

4. **Actions should carry minimal data.** Include only the data needed for reducers and effects to do their work.

### Limitations

1. **Actions cannot contain behavior.** Actions are pure data—do not add methods that perform operations.

2. **Actions should not contain services.** Do not inject or reference services in actions; effects receive services via dependency injection.

3. **Actions must be serializable** if you need features like time-travel debugging or action logging (future consideration).

## Best Practices

### Do

- ✅ Use records for immutability and value equality
- ✅ Use `sealed` to prevent inheritance and enable compiler optimizations
- ✅ Include XML documentation comments describing when the action is dispatched
- ✅ Use `ImmutableList<T>`, `ImmutableArray<T>`, or other immutable collections for collection payloads
- ✅ Group related actions in the same file or namespace
- ✅ Use the `required` modifier for mandatory properties in complex actions

### Don't

- ❌ Add methods or behavior to actions
- ❌ Include mutable types (like `List<T>`) in action payloads
- ❌ Inject services or create side effects in action constructors
- ❌ Use inheritance hierarchies for actions (prefer composition via interfaces if grouping is needed)
- ❌ Include large objects or binary data directly in actions

## Dispatching Actions

Actions are dispatched through the store. In Blazor components that inherit from `StoreComponent`:

```razor
@inherits StoreComponent

<button @onclick="LoadProducts">Load Products</button>
<button @onclick="@(() => AddItem("Widget"))">Add Widget</button>

@code {
    private void LoadProducts() => Dispatch(new LoadProductsAction());
    
    private void AddItem(string name) => Dispatch(new AddItemAction(name));
}
```

Or directly through the `IStore` interface:

```csharp
public class ProductService
{
    private IStore Store { get; }
    
    public ProductService(IStore store) => Store = store;
    
    public void RefreshProducts() => Store.Dispatch(new LoadProductsAction());
}
```

## Next Steps

- Learn how [Reducers](./reducers.md) transform state in response to actions
- Understand how [Effects](./effects.md) handle asynchronous operations triggered by actions
- See how the [Store](./store.md) coordinates the action dispatch lifecycle
