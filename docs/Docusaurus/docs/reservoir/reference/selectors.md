---
title: Reservoir Selector Reference
description: Reference store selection, multi-feature selectors, and reference-based memoization in Reservoir.
sidebar_position: 2
sidebar_label: Selectors
---

# Reservoir Selector Reference

## Overview

Selectors are pure functions that derive a value from one or more feature states. Reuse them to keep display logic consistent across pages and tests.

## Applies To

- `Mississippi.Reservoir.Abstractions`: `IStore.Select` extension methods.
- `Mississippi.Reservoir.Core`: `Memoize.Create`.
- `Mississippi.Reservoir.Client`: protected `StoreComponent.Select` helpers.

## Store Selection

| Method shape | Selector input | Result |
| --- | --- | --- |
| `Select<TState, TResult>(selector)` | One registered feature state | `TResult` |
| `Select<TState1, TState2, TResult>(selector)` | Two registered feature states | `TResult` |
| `Select<TState1, TState2, TState3, TResult>(selector)` | Three registered feature states | `TResult` |

Each state type is a class implementing `IFeatureState`. The extension retrieves the states from the store and invokes the supplied `Func` once for that call. `StoreComponent` exposes matching protected overloads.

Use a selector that answers one question. Spring's [DualEntitySelectionSelectors](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Features/DualEntitySelection/Selectors/DualEntitySelectionSelectors.cs) provides ID getters and a presence predicate. Its [BankAccountCompositeSelectors](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Features/BankAccountAggregate/Selectors/BankAccountCompositeSelectors.cs) combines command and projection state for display.

Multi-state selection reads each feature separately. Schedule the complete selection together with dispatch in the same serialized execution context when the values must represent one coherent state. Concurrent selection and reduction can otherwise combine values from different points in the update.

## Memoization

`Memoize.Create` accepts a pure selector with one, two, or three reference-type inputs and returns a function with the same input/result shape.

| Property | Behavior |
| --- | --- |
| Cache ownership | Each returned selector function owns its cache |
| Cache key | The last input reference, or tuple of input references |
| Cache hit | Every input reference is the same object as the cached input |
| New reference | Recompute, even when property values compare equal |
| Cache size | One latest input/result entry per returned function |
| Result type | Unconstrained generic `TResult` |

Create a memoized selector once and reuse it where that derived value is needed. Immutable updates give the cache a reliable reference-change signal. This is useful when other features update while the selector's own inputs stay unchanged.

Memoization stores one cache entry atomically. Concurrent callers can evaluate a pure selector more than once for the same inputs; use the result for derivation rather than for triggering work.

## Constraints and Errors

- Validate that registered feature keys are unique. Store construction assigns state by key, so a later duplicate replaces the earlier state and can misassociate its processing registrations. `GetState<TState>()` for the displaced type can then throw `InvalidCastException`.
- A null store or selector is rejected with `ArgumentNullException` by the store extension methods.
- `GetState<TState>()` reports an unregistered feature with `InvalidOperationException`.
- Use the default store within its lifetime. After disposal, its `GetState<TState>()` throws `ObjectDisposedException`, including when a valid store-selection call reaches it.
- `Memoize.Create` rejects a null selector with `ArgumentNullException`.
- Keep selectors free of mutation, I/O, time-dependent reads, and dispatch. Supply the facts they need through their inputs.

Selectors execute synchronously. If the selector throws, the exception propagates through selection and can interrupt a component render. A failed memoized evaluation is not stored as a cache result. Keep expected outcomes representable as values and handle unexpected selector failures at the calling boundary.

## Source and Verification

The signatures and behavior are defined by [SelectorExtensions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/SelectorExtensions.cs), [Memoize](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Selectors/Memoize.cs), and [StoreComponent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/StoreComponent.cs). [MemoizeTests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/Selectors/MemoizeTests.cs) covers reference reuse, changed inputs, multiple states, and concurrent access.

A named pure selector gives an AI assistant a small contract to implement and verify: specified input state maps to an expected display value. Keep that assertion separate from whether a page rerenders or an effect completes.

## Summary

Use store selection to read derived values and a reused memoized selector when repeated calculations can share the same input references.

## Next Steps

- [Add a Reservoir feature](../how-to/create-feature.md) for feature and page integration.
- [State flow](../concepts/state-flow.md) for dispatch and notification timing.
