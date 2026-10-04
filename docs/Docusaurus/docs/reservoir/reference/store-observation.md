---
id: store-observation
title: Reservoir Store Observation
description: Reference state-listener and event-stream subscriptions, synchronous callbacks, replay, and disposal.
sidebar_position: 7
sidebar_label: Reservoir Store Observation
---

# Reservoir Store Observation

## Overview

`IStore.Subscribe(Action)` registers a state listener, while `IStore.StoreEvents` exposes dispatch observations. This page describes synchronous delivery, replay, ordering, and disposal in the default `Store` and its subject; the `IStore` abstraction does not require alternate implementations to share all these details.

## Applies To

- `Mississippi.Reservoir.Abstractions.IStore`
- The default `Store` and its `StoreEventSubject`
- State listeners and `IObserver<StoreEventBase>` subscriptions

## State Listeners

The [store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs) rejects a null state listener and returns a disposable subscription. Subscribing does not immediately invoke the callback. Disposing that subscription removes the listener from future notification lists and can be repeated.

Ordinary core dispatch invokes listeners after its `ActionDispatchedEvent`, even when reduction retains every state reference. Reset and restore invoke them only when their action's notification flag is true.

For each notification, the store copies the current listener list, then calls it in order. Removing a listener during a callback does not remove it from that already-captured list; adding one affects later notifications.

## Store Event Observers

[`StoreEvents`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs) accepts observer subscriptions through the [subject](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/StoreEventSubject.cs). It rejects a null observer and returns a disposable that removes that observer.

The subject copies its subscriber list under a lock for each publication, then calls `OnNext` synchronously in list order outside that lock. It does not serialize concurrent publications: callbacks from concurrent dispatches can overlap and their event pairs can interleave. It does not retain or replay earlier events. The DI store constructor publishes `StoreInitializedEvent` before construction returns, so an observer subscribing afterward does not receive that earlier initialization event.

Ordinary dispatch exposes pre- and post-reduction events only when middleware reaches `CoreDispatch`; middleware that returns without calling `nextAction` produces neither event. Recognized restoration actions entering through `Store.Dispatch` expose pre-dispatch and restoration events. Use their payloads for the relevant boundary and read the current store separately when needed.

## Callback And Disposal Boundaries

An observer or listener exception propagates at its callback site, interrupts later callbacks in that captured list, and can interrupt the surrounding dispatch. The store does not translate reducer or callback failures into an automatic `OnError` event. Keep callbacks short and observational.

On normal store disposal, the subject calls `OnCompleted` on its captured observers, then the store clears listeners and stored state. If a completion callback throws, later observers are not completed and store clearing is skipped. Both disposed flags were already set, so another `Dispose` returns early and does not finish that partial cleanup.

For calls beginning after disposal completes, a new event-stream subscription receives `OnCompleted` immediately and a no-op disposable. Ordinary state subscription, `Dispatch`, and `GetState` reject a disposed store with `ObjectDisposedException`.

Those guards run before acquiring subscription locks. A concurrent subscribe/dispose race can add a subscriber after the list was cleared without completing it; the post-disposal behavior above is not a guarantee for overlapping lifecycle calls. Coordinate subscription and disposal ownership.

`GetStateSnapshot` has no disposed-store guard; after normal disposal it returns a dictionary copied from the cleared feature dictionary. Retained earlier snapshots still hold their original feature-object references.

The [store tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/StoreTests.cs) cover state-listener notification, unsubscribe, repeated subscription disposal, and null/disposed guards. Event replay and observer delivery rules above are verified from the subject implementation.

## Summary

Choose state listeners for notification callbacks and event observers for dispatch boundaries. Neither subscription replays past state changes, and synchronous callback failures can affect dispatch or disposal.

## Next Steps

- Read [Reservoir Reference](./reference.md) for store and feature registration.
- Read [Reservoir State Flow](../concepts/state-flow.md) for dispatch timing and reentrant callbacks.
