---
title: Reservoir State Restoration
description: Reference local reset and restore actions, snapshot compatibility, notifications, and dispatch events.
sidebar_position: 5
---

# Reservoir State Restoration

Reset and restore actions replace local store values through a dedicated system-action path. They do not run the application's ordinary middleware, reducers, or effects.

## Applies To

- `Mississippi.Reservoir.Abstractions.Actions.ResetToInitialStateAction`
- `RestoreStateAction` and `StateRestoredEvent`
- `Mississippi.Reservoir.Core.Store`

## Action Inputs

[`ResetToInitialStateAction`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Actions/ResetToInitialStateAction.cs) supplies `NotifyListeners`, defaulting to true. Reset offers the initial feature objects retained by the store at registration to its state-replacement logic; it does not construct new feature objects.

[`RestoreStateAction`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Actions/RestoreStateAction.cs) supplies a feature-keyed `Snapshot` and the same notification flag, also defaulting to true. Supply a non-null snapshot: the record does not validate it before the store enumerates it.

## Replacement Rules

The [store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs) applies both actions through the same replacement logic:

- A key must already exist in the current feature dictionary.
- Its supplied value must be non-null.
- The current feature object's runtime type must accept the supplied value through `IsInstanceOfType`.
- Accepted entries replace their current values; unknown, null, incompatible, or omitted entries leave current values unchanged.

Restoration can therefore be partial. The check uses the current object's runtime type, rather than only the feature's declared interface. Reset values are subject to that compatibility check too.

`GetStateSnapshot` creates a new dictionary while retaining feature-object references. Applying a snapshot also retains its supplied objects. Keep their object graphs immutable if a retained snapshot must continue to describe earlier values; these operations do not deep-copy data.

## Events And Notifications

After ordinary dispatch guards, `Store.Dispatch` recognizes `ISystemAction` before building its middleware pipeline. For reset or restore it emits `ActionDispatchingEvent`, applies the values, and emits [`StateRestoredEvent`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Events/StateRestoredEvent.cs) with previous/new snapshots and the causing action.

It then calls state listeners only when `NotifyListeners` is true. Setting the flag false suppresses those listener calls, rather than the restoration events. No ordinary `ActionDispatchedEvent` is emitted, and user reducers/effects are not invoked by this path.

An unrecognized `ISystemAction` emits only the dispatching event and returns without restoration or listener notification. Observer or listener exceptions propagate at their call site, so a callback failure can interrupt this sequence.

The [store tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/StoreTests.cs) cover resetting to registered initial values, restoring supplied values, and ignoring an incompatible supplied value. The event and notification sequence above is verified from the implementation.

## Summary

Use reset and restore for compatible local state replacement. Decide explicitly whether to notify listeners, and observe `StateRestoredEvent` for the recognized restoration path.

## Next Steps

- Read [Reservoir Reference](./reference.md) for feature registration.
- Read [Reservoir State Flow](../concepts/state-flow.md) for ordinary dispatch and snapshot ownership.
