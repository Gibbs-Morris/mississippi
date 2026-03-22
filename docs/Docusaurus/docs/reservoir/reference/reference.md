---
id: reservoir-reference
title: Reservoir Reference
sidebar_label: Reference
sidebar_position: 1
description: Reference the current Reservoir builder entry points, feature-builder contracts, and verified client registration extensions.
---

# Reservoir Reference

## Overview

Reservoir is the Mississippi client-state management subsystem.

## Applies To

- `Mississippi.Reservoir.Abstractions`
- `Mississippi.Reservoir.Core`
- `Mississippi.Reservoir.Client`
- `Mississippi.Reservoir.TestHarness`

## Public Registration Entry Points

| Entry point | Receiver | Returns | Purpose |
|-------------|----------|---------|---------|
| `AddReservoir()` | `IServiceCollection` | `IReservoirBuilder` | Create the top-level Reservoir builder from DI |
| `AddReservoir()` | `WebAssemblyHostBuilder` | `IReservoirBuilder` | Create the same builder from Blazor WebAssembly startup |

Source code:

- [ReservoirRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/ReservoirRegistrations.cs)
- [ReservoirWebAssemblyHostBuilderRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirWebAssemblyHostBuilderRegistrations.cs)

## IReservoirBuilder

`IReservoirBuilder` is the top-level public registration contract.

| Member | Purpose |
|--------|---------|
| `Services` | Advanced access to the underlying `IServiceCollection` |
| `AddFeatureState<TState>()` | Register a feature state without extra reducers or effects |
| `AddFeatureState<TState>(configure)` | Register a feature state and configure reducers or effects in one callback |
| `AddMiddleware<TMiddleware>()` | Add middleware to the Reservoir dispatch pipeline |

The `Services` property is marked advanced in the public contract. The normal direction is to compose through builder extension methods instead of writing more direct service registrations in application startup.

## IReservoirFeatureBuilder

`IReservoirFeatureBuilder<TState>` is the feature-scoped public contract used inside `AddFeatureState<TState>(configure)`.

| Member | Purpose |
|--------|---------|
| `Services` | Advanced access to the underlying `IServiceCollection` |
| `AddActionEffect<TEffect>()` | Register a feature-scoped action effect |
| `AddReducer<TAction>(Func<TState, TAction, TState> reduce)` | Register a reducer delegate |
| `AddReducer<TAction, TReducer>()` | Register a reducer implementation type |

Source code:

- [IReservoirBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IReservoirBuilder.cs)
- [IReservoirFeatureBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IReservoirFeatureBuilder.cs)

## Verified Client Builder Extensions

The current client-side extensions that build on `IReservoirBuilder` are:

| Method | Package | Purpose |
|--------|---------|---------|
| `AddReservoirBlazorBuiltIns()` | `Mississippi.Reservoir.Client` | Register both built-in navigation and lifecycle features |
| `AddBuiltInNavigation()` | `Mississippi.Reservoir.Client` | Register the navigation feature only |
| `AddBuiltInLifecycle()` | `Mississippi.Reservoir.Client` | Register the lifecycle feature only |
| `AddReservoirDevTools(...)` | `Mississippi.Reservoir.Client` | Register Redux DevTools integration |

Source code:

- [ReservoirBlazorBuiltInRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs)
- [NavigationFeatureRegistration.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/Navigation/NavigationFeatureRegistration.cs)
- [LifecycleFeatureRegistration.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs)
- [ReservoirDevToolsRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirDevToolsRegistrations.cs)

## Verified Ownership Boundary

- Store and dispatch pipeline abstractions
- Feature state, actions, reducers, selectors, effects, and middleware
- Client integration and testing support for that model
- Public registration builders for top-level and feature-level composition

## Related But Separate Areas

- [Refraction](../../refraction/index.md) owns the Blazor UX layer.
- [Inlet](../../inlet/index.md) owns generated full-stack alignment.

## Defaults And Constraints

This reference covers the verified subsystem boundary and the current public registration surface. See [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for preserved deep API material that has not yet been rewritten into the active docs set.

## Startup Direction

Builder-based composition is the direction of the public Reservoir registration model going forward.

Application startup should begin with `AddReservoir()` and then compose package or feature extensions on the returned `IReservoirBuilder`.

## Failure Behavior

For runtime and API-level failure behavior, refer to the [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) and the [Reservoir Concepts](../concepts/concepts.md) page.

## Summary

Use this page as the current active reference for Reservoir's builder entry points, feature-builder contracts, and verified client registration extensions.

## Next Steps

- Read [Reservoir Concepts](../concepts/concepts.md).
- Read [Inlet Reference](../../inlet/reference/reference.md) for the client-sync extensions that compose on top of Reservoir.
- Use [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for preserved deep material.
