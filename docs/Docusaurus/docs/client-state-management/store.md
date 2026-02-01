---
id: store
title: Store
sidebar_label: Store
sidebar_position: 7
description: The Store is the central hub that coordinates feature states, reducers, middleware, and effects in Reservoir's Redux-like architecture.
---

# Store

## Overview

The Store is the central state container for Reservoir. It coordinates feature states, dispatches actions through the middleware pipeline, invokes reducers to update state, notifies subscribers, and triggers effects for async operations.
([IStore](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs))

## What Is the Store?

The Store implements [`IStore`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs) and provides three core operations:

| Method | Description |
|--------|-------------|
| `Dispatch(IAction)` | Sends an action through the pipeline |
| `GetState<TState>()` | Retrieves current feature state |
| `Subscribe(Action)` | Registers a listener for state changes |

```csharp
public interface IStore : IDisposable
{
    void Dispatch(IAction action);

    TState GetState<TState>()
        where TState : class, IFeatureState;

    IDisposable Subscribe(Action listener);
}
```

([IStore](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs#L24-L55))

## Registering the Store

Register the Store via `AddReservoir()`:

```csharp
services.AddReservoir();
```

This registers `IStore` as scoped, resolving all `IFeatureStateRegistration` and `IMiddleware` instances from DI:

```csharp
public static IServiceCollection AddReservoir(
    this IServiceCollection services
)
{
    services.TryAddScoped<IStore>(sp => new Store(
        sp.GetServices<IFeatureStateRegistration>(),
        sp.GetServices<IMiddleware>()));
    return services;
}
```

([ReservoirRegistrations.AddReservoir](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/ReservoirRegistrations.cs#L139-L148))

:::note Scoped Lifetime
The Store is registered as **scoped**. Its lifetime follows the dependency-injection scope configured by the host.
:::

## Dispatch Pipeline

When you call `store.Dispatch(action)`, the action flows through a well-defined pipeline:

```mermaid
flowchart LR
    A[Dispatch] --> B[Middleware Pipeline]
    B --> C[Reducers]
    C --> D[Notify Subscribers]
    D --> E[Effects]
    E -.->|Returned Actions| A
    
    style A fill:#4a9eff,color:#fff
    style B fill:#f4a261,color:#fff
    style C fill:#50c878,color:#fff
    style D fill:#6c5ce7,color:#fff
    style E fill:#ff6b6b,color:#fff
```

### Pipeline Steps

1. **Middleware Pipeline** — Each registered middleware can inspect, modify, or short-circuit the action
2. **Reducers** — All root reducers process the action and update their feature states
3. **Notify Subscribers** — All registered listeners are invoked synchronously
4. **Effects** — Root effects handle the action asynchronously; returned actions are dispatched

```csharp
private void CoreDispatch(IAction action)
{
    // First, run reducers for feature states
    ReduceFeatureStates(action);

    // Hook for derived classes
    OnActionDispatched(action);

    // Notify listeners of state change
    NotifyListeners();

    // Finally, trigger action effects asynchronously
    _ = TriggerEffectsAsync(action);
}
```

([Store.CoreDispatch](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L214-L228))

## Dispatching Actions

Call `Dispatch` on the store to send actions through the pipeline.
([IStore.Dispatch](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs#L26-L33))

Components inheriting `StoreComponent` can call its protected `Dispatch` helper.
([StoreComponent.Dispatch](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/StoreComponent.cs#L48-L53))

### Dispatch Rules

- **Synchronous reducers and listeners** — Reducers run first, then subscribers are notified
- **Effects are async** — Effects are triggered asynchronously after the reducers and notifications
- **Null actions throw** — `Dispatch(null)` throws `ArgumentNullException`
- **Disposed throws** — Dispatching to a disposed store throws `ObjectDisposedException`

([Store.CoreDispatch](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L214-L228),
[StoreTests.DispatchAfterDisposeThrowsObjectDisposedException](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L327-L335),
[StoreTests.DispatchWithNullActionThrowsArgumentNullException](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L338-L345))

## Reading State

Use `GetState<TState>()` to retrieve the current value of a feature state:

```csharp
private string? SelectedEntityId => GetState<EntitySelectionState>().EntityId;
```

For derived values, prefer [selectors](selectors.md) to encapsulate logic. The Spring sample uses selectors for all state access:

```csharp
private string? SelectedEntityId => 
    Select<EntitySelectionState, string?>(EntitySelectionSelectors.GetEntityId);
```

([Spring.Index](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Pages/Index.razor.cs#L190),
[StoreComponent.GetState](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/StoreComponent.cs#L82-L84))

### GetState Rules

- Returns the current snapshot of the feature state
- Throws `InvalidOperationException` if the feature state is not registered:

```text
No feature state registered for 'entitySelection'.
Call AddFeatureState<EntitySelectionState>() during service registration.
```

([Store.GetState](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L112-L125))

## Subscribing to Changes

Use `Subscribe` to register a listener that runs after every dispatch.
([IStore.Subscribe](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs#L46-L53))

### Subscription Behavior

- Listeners are called synchronously after reducers complete and before effects run
- Listeners receive no parameters—query state via `GetState<TState>()`
- Dispose the returned `IDisposable` to unsubscribe
- Subscriptions can be disposed multiple times safely

([Store.CoreDispatch](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L214-L228),
[StoreTests.SubscriptionDisposeCanBeCalledMultipleTimes](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L600-L626))

For an example of unsubscribe behavior, see the unit test.
([StoreTests.UnsubscribedListenerDoesNotReceiveNotifications](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L637-L651))

## Blazor Integration

For Blazor components, inherit from [`StoreComponent`](store-component.md) instead of managing subscriptions manually:

`InletComponent` is a concrete example of a component that derives from `StoreComponent`.
([InletComponent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletComponent.cs#L13-L21))

[`StoreComponent`](store-component.md) handles:

- **Automatic subscription** — Subscribes to the store in `OnInitialized`
- **Automatic re-render** — Calls `StateHasChanged` when state changes
- **Automatic cleanup** — Disposes the subscription when the component is disposed

```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    storeSubscription = Store.Subscribe(OnStoreChanged);
}

private void OnStoreChanged()
{
    _ = InvokeAsync(StateHasChanged);
}
```

([StoreComponent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/StoreComponent.cs#L22-L157))

## Store Lifecycle

### Construction

The Store can be constructed two ways:

1. **Via DI (recommended)** — `AddReservoir()` registers the Store with feature registrations and middleware resolved from DI
2. **Manually** — Pass feature registrations and middleware directly to the constructor

Manual construction is used in tests to validate middleware behavior.
([Store constructor](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L58-L86),
[StoreTests.ConstructorWithMiddlewareCollectionRegistersMiddleware](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L253-L266))

### Disposal

The Store implements `IDisposable`. When disposed:

- All subscriptions are cleared
- All feature states, reducers, and effects are cleared
- Subsequent `Dispatch`, `GetState`, or `Subscribe` calls throw `ObjectDisposedException`

([StoreTests.DispatchAfterDisposeThrowsObjectDisposedException](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.L0Tests/StoreTests.cs#L327-L335),
[Store.Dispose](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L104-L108),
[Store.Dispose(bool)](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L156-L182))

## Effect Error Handling

Effects run asynchronously after dispatch. If an effect throws:

- The exception is **swallowed** to prevent breaking the dispatch pipeline
- Other effects continue to run
- Effects should handle their own errors by emitting error actions

([Store.TriggerEffectsAsync](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L278-L332))

## Store Internals

For advanced scenarios, understanding the Store's internal structure helps:

| Field | Type | Purpose |
|-------|------|---------|
| `featureStates` | `ConcurrentDictionary<string, object>` | Maps FeatureKey → current state |
| `rootReducers` | `ConcurrentDictionary<string, object>` | Maps FeatureKey → `IRootReducer<TState>` |
| `rootActionEffects` | `ConcurrentDictionary<string, object>` | Maps FeatureKey → `IRootActionEffect<TState>` |
| `middlewares` | `List<IMiddleware>` | Ordered middleware pipeline |
| `listeners` | `List<Action>` | Registered subscribers |

([Store fields](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L20-L38))

### Middleware Pipeline Building

Middleware wraps around the core dispatch in reverse registration order (last registered wraps first):

```csharp
private Action<IAction> BuildMiddlewarePipeline(Action<IAction> coreDispatch)
{
    Action<IAction> next = coreDispatch;

    // Build pipeline in reverse order (last middleware wraps first)
    for (int i = middlewares.Count - 1; i >= 0; i--)
    {
        IMiddleware middleware = middlewares[i];
        Action<IAction> currentNext = next;
        next = action => middleware.Invoke(action, currentNext);
    }

    return next;
}
```

([Store.BuildMiddlewarePipeline](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs#L199-L212))

## Summary

| Concept | Description |
|---------|-------------|
| **Store** | Central state container implementing `IStore` |
| **Dispatch** | Sends actions through middleware → reducers → notify → effects |
| **GetState** | Returns current feature state snapshot |
| **Subscribe** | Registers listener called after every dispatch |
| **Lifetime** | Scoped (per DI scope) |
| **Disposal** | Clears all state and subscriptions; subsequent calls throw |
| **Error handling** | Effects swallow exceptions; emit error actions instead |

([IStore](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs),
[Store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir/Store.cs),
[StoreComponent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/StoreComponent.cs))

## Next Steps

- [Reservoir Overview](./reservoir.md) — Understand the dispatch pipeline end-to-end
- [Actions](./actions.md) — Define what can happen in your application
- [Reducers](./reducers.md) — Update state in response to actions
- [Effects](./effects.md) — Handle async operations and side effects
- [Middleware](./middleware.md) — Intercept and transform actions
- [Feature State](./feature-state.md) — Organize state into modular slices
- [StoreComponent](./store-component.md) — Blazor base component for store integration
- [Selectors](./selectors.md) — Derive computed values from state
