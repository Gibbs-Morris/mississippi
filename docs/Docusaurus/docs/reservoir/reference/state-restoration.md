---
id: state-restoration
title: Reservoir State Restoration
description: Reference local reset and restore actions, snapshot compatibility, notifications, and dispatch events.
sidebar_position: 5
sidebar_label: Reservoir State Restoration
---

# Reservoir State Restoration

## Overview

Reset and restore actions entering through `Store.Dispatch` replace local store values through a dedicated system-action path. That path bypasses the application's ordinary middleware, reducers, and effects.

## Applies To

- `Mississippi.Reservoir.Abstractions.Actions.ResetToInitialStateAction`
- `RestoreStateAction` and `StateRestoredEvent`
- `Mississippi.Reservoir.Core.Store`

## Action Inputs

[`ResetToInitialStateAction`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Actions/ResetToInitialStateAction.cs) supplies `NotifyListeners`, defaulting to true. Reset offers the retained reset-baseline objects to its state-replacement logic; it does not construct new feature objects per reset. The store constructor evaluates each registration's `InitialState` twice, retaining the first result as live startup state and the second as its reset baseline. The [default registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/State/FeatureStateRegistration.cs) returns `new TState()` on each read. Nondeterministic constructors can therefore produce baseline values different from those initially exposed; repeated resets reuse that retained second result.

[`RestoreStateAction`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Actions/RestoreStateAction.cs) supplies a feature-keyed `Snapshot` and the same notification flag, also defaulting to true. Supply a non-null snapshot: the record does not validate it before the store enumerates it.

## Replacement Rules

The [store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs) applies both actions through the same replacement logic:

- A key must already exist in the current feature dictionary with exactly matching casing. Preserve the registered `IFeatureState.FeatureKey`; differently cased keys are unknown and are ignored.
- Its supplied value must be non-null.
- The current feature object's runtime type must accept the supplied value through `IsInstanceOfType`.
- Accepted entries replace their current values; unknown, null, incompatible, or omitted entries leave current values unchanged.

Restoration can therefore be partial. The check uses the current object's runtime type, rather than only the feature's declared interface. Reset values are subject to that compatibility check too.

Replacement writes feature entries individually, without a transaction or a shared lock with other dispatches or snapshot creation. Multi-feature restoration is not atomic: concurrent readers/dispatches can observe mixed values. Serialize restoration with other store activity when a consistent transition is required; `StateRestoredEvent` does not prove intermediate states were unobservable.

`GetStateSnapshot` creates a new dictionary while retaining feature-object references. Applying a snapshot also retains its supplied objects. Keep their object graphs immutable if a retained snapshot must continue to describe earlier values; these operations do not deep-copy data.

## Events And Notifications

After ordinary dispatch guards, `Store.Dispatch` recognizes `ISystemAction` before building its middleware pipeline. For reset or restore it emits `ActionDispatchingEvent`, applies the values, and emits [`StateRestoredEvent`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Events/StateRestoredEvent.cs) with previous/new snapshots and the causing action. The previous snapshot is captured after `ActionDispatchingEvent` observers return. A synchronous nested dispatch from such an observer can change it, so it need not represent the state at entry to the outer `Dispatch` call.

Middleware calling `nextAction` with a replacement reset/restore action does not re-enter this check. The pipeline ends at ordinary `CoreDispatch`, so that replacement runs the ordinary reduction/listener/effect path without restoring state. Dedicated restoration requires entry through `Store.Dispatch`.

It then calls state listeners only when `NotifyListeners` is true. Setting the flag false suppresses those listener calls, rather than the restoration events. No ordinary `ActionDispatchedEvent` is emitted, and user reducers/effects are not invoked by this path. Already-running effects are not canceled or awaited by reset/restore. Ordinary dispatch starts them without awaiting completion and supplies `CancellationToken.None`; their later result actions are dispatched normally and can change restored state. Coordinate in-flight effects when restoration must remain stable.

An unrecognized `ISystemAction` emits only the dispatching event and returns without restoration or listener notification. Callback exceptions propagate without rollback. A dispatching-event observer can fail before any replacement; a restoration-event observer can fail after replacement but before listeners; a listener can fail after replacement and prevent later listeners from running. A failed call therefore does not by itself show whether restoration occurred.

A `StateRestoredEvent` observer can also dispatch synchronously before the outer restoration reaches its listeners. The nested action can update state and notify listeners first; the outer listeners can then observe state newer than that event's captured `NewSnapshot`. Schedule follow-up dispatch after the observer callback returns when listener ordering must preserve the restored transition.

The [store tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/StoreTests.cs) cover resetting to registered initial values, restoring supplied values, and ignoring an incompatible supplied value. The event and notification sequence above is verified from the implementation.

## Summary

Use reset and restore for compatible local state replacement. Decide explicitly whether to notify listeners, and observe `StateRestoredEvent` for the recognized restoration path.

## Next Steps

- Read [Reservoir Reference](./reference.md) for feature registration.
- Read [Reservoir State Flow](../concepts/state-flow.md) for ordinary dispatch and snapshot ownership.
