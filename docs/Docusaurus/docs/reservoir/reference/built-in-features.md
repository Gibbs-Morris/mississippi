---
title: Built-In Navigation and Lifecycle
description: Reference Reservoir navigation actions, browser-location observation, and application-supplied lifecycle milestones.
sidebar_position: 5
sidebar_label: Navigation and Lifecycle
---

# Built-In Navigation and Lifecycle

## Overview

Reservoir's built-in client features represent navigation and application milestones as actions and state. This gives pages and diagnostics the same explicit state model used by application features.

## Registration

All entry points extend `IReservoirBuilder` and are supplied by `Mississippi.Reservoir.Client`:

| Method | Registers |
| --- | --- |
| `AddReservoirBlazorBuiltIns()` | Navigation and lifecycle features |
| `AddBuiltInNavigation()` | Navigation state, location reducer, and navigation effect |
| `AddBuiltInLifecycle()` | Lifecycle state and milestone reducers |

Use the corresponding namespaces under `Mississippi.Reservoir.Client.BuiltIn`, `.Navigation`, or `.Lifecycle`. [Spring startup](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs) registers both features inside its Reservoir callback.

## Browser Location Observation

Render one `ReservoirNavigationProvider` at the application root to connect `NavigationManager.LocationChanged` with the store. This is the root-markup pattern used by Spring:

```razor
@using Mississippi.Reservoir.Client.BuiltIn.Components

<ReservoirNavigationProvider/>
```

The provider dispatches the current URI on initialization, forwards subsequent location changes, and unsubscribes when disposed. Its initial notification contributes to `NavigationCount`; that count represents handled location notifications rather than only user clicks.

## Navigation Actions

The action namespace is `Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions`.

| Action | Parameters and defaults | Purpose |
| --- | --- | --- |
| `NavigateAction` | `Uri`, `ForceLoad = false` | Navigate through `NavigationManager` |
| `ReplaceRouteAction` | `Uri`, `ForceLoad = false` | Navigate while replacing the history entry |
| `SetQueryParamsAction` | `Parameters`, `ReplaceHistory = true` | Update query parameters on the current URI |
| `ScrollToAnchorAction` | `AnchorId` without `#`, `ReplaceHistory = false` | Navigate to the current page's fragment |
| `LocationChangedAction` | `Location`, `IsNavigationIntercepted` | Record an observed browser location |

Use these actions for application navigation. The effect accepts relative application paths and absolute URIs on the same origin; use normal links for external destinations. Navigation follows Blazor's `NavigationManager` behavior, including history and force-load semantics.

`NavigationState` uses feature key `reservoir:navigation` and exposes `CurrentUri`, `PreviousUri`, `IsNavigationIntercepted`, and `NavigationCount`. The location reducer moves the current URI to previous, records the new URI/interception flag, and increments the count.

## Lifecycle Milestones

`LifecycleState` uses feature key `reservoir:lifecycle`. It starts in `NotStarted` with null timestamps.

| Application action | State update |
| --- | --- |
| `AppInitAction(InitializedAt)` | Sets `Phase = Initializing` and `InitializedAt` |
| `AppReadyAction(ReadyAt)` | Sets `Phase = Ready` and `ReadyAt` |

The application dispatches these actions at the milestones it defines. Supply timestamps in the payload, using the application's time source. The reducers use those values directly, keeping the transition deterministic and easy to test. Each reducer updates the fields shown above and preserves the other fields.

Use lifecycle state to explain what initialization has completed, and give an AI assistant precise milestone actions and expected state when adding startup behavior.

## Source and Verification

- [Built-in registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs).
- [Navigation provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/Components/ReservoirNavigationProvider.razor.cs), [actions](https://github.com/Gibbs-Morris/mississippi/tree/main/src/Reservoir.Client/BuiltIn/Navigation/Actions), and [effect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/Navigation/Effects/NavigationEffect.cs).
- [Lifecycle reducers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/Lifecycle/Reducers/LifecycleReducers.cs).
- [Built-in client tests](https://github.com/Gibbs-Morris/mississippi/tree/main/tests/Reservoir.Client.L0Tests/BuiltIn).

## Summary

Observe navigation through the root provider and supply lifecycle milestones from application code. Both become explicit actions and state that pages, tests, and development tools can inspect.

## Next Steps

- [Enable DevTools](../how-to/enable-devtools.md) to inspect these actions.
- [Middleware reference](./middleware.md) for dispatch interception.
