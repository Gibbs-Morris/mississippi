---
id: action-reducers
title: Reservoir Action Reducers
description: Reference root reducer indexing, chaining, fallback order, and delegate reducer behavior.
sidebar_position: 6
sidebar_label: Reservoir Action Reducers
---

# Reservoir Action Reducers

## Overview

`RootReducer<TState>` applies all successful reducers selected for an action, passing each result to the next reducer. It builds its dispatch index when constructed, preserving order within the typed and fallback groups.

## Applies To

- `Mississippi.Reservoir.Core.RootReducer<TState>`
- `DelegateActionReducer<TAction, TState>`
- `IActionReducer` implementations used for local feature state

## Index And Reduction Order

The [root reducer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/RootReducer.cs) rejects a null reducer collection and captures its enumeration into an array. It identifies a compatible typed `IActionReducer<TAction, TState>` interface for indexing; reducers without a discovered typed action contract go into the fallback group.

For each non-null action, reduction proceeds in two stages:

1. Reducers indexed for the action's exact runtime type run in registration order.
2. Fallback reducers run in their own registration order.

Each call receives the current result. A `TryReduce` result of true replaces that result with its output; false leaves it unchanged. Successful reducers continue chaining through the rest of both groups.

A fallback registered before a typed reducer still runs after the typed group. If nothing supplies a replacement, the root returns the original state reference.

Typed indexing uses the exact runtime action type. A reducer indexed for a base action type is not selected for a derived action by that index, even when its direct `TryReduce` implementation accepts the derived instance.

## Delegate Reducers

[`DelegateActionReducer<TAction, TState>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/DelegateActionReducer.cs) requires a non-null callback. Its `Reduce` method forwards state and action directly to that callback.

Its `TryReduce` method calls the callback when the action is assignable to `TAction` and returns true with the callback's result. Otherwise, it returns false and the original state. This direct assignability check is separate from the root's exact-type index.

## State And Failure Boundaries

Reducers own their returned state values. The root does not clone state, detect in-place mutation, or validate a successful reducer's output for null. It rejects a null action, while callback/reducer exceptions propagate from direct reduction.

Returning the same reference is a supported way to retain the current state, but it does not stop later reducers. Keep reducer inputs and outputs immutable so chaining remains predictable.

The [root tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/RootReducerTests.cs) cover typed and fallback application, multiple successful reducers, no-match reference preservation, and guards. The [delegate tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/DelegateActionReducerTests.cs) cover callback invocation and matching/nonmatching `TryReduce` results.

## Summary

Design reducers for an ordered chain: exact-type reducers first, then fallbacks. Each successful output becomes the input to later reducers, including when the reference is unchanged.

## Next Steps

- Read [Reservoir Reference](./reference.md) for reducer registration methods.
- Read [Reservoir State Flow](../concepts/state-flow.md) for store notifications, effects, and interrupted dispatch.
