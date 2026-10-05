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

Ordinary core dispatch invokes listeners after its `ActionDispatchedEvent`, even when reduction retains every state reference. Reset and restore entering directly through `Store.Dispatch` invoke them only when their action's notification flag is true. A middleware replacement passed to `nextAction` reaches ordinary `CoreDispatch` after the system-action check, so that path does not honor the restoration notification flag.

For each notification, the store copies the current listener list, then calls it in order. Removing a listener during a callback does not remove it from that already-captured list; adding one affects later notifications. This ordering applies within one notification only. Concurrent dispatches can capture separate lists and invoke the same listener concurrently outside the lock; notification sequences can interleave.

## Store Event Observers

[`StoreEvents`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs) accepts observer subscriptions through the [subject](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/StoreEventSubject.cs). It rejects a null observer and returns a disposable that removes the first equality-matching observer from future publication snapshots. Removal uses `List<IObserver<T>>.Remove` and the default equality comparer, rather than reference identity. If distinct observers compare equal, disposing a later subscription can remove an earlier observer and leave the later one subscribed. A publication that already captured it can still invoke `OnNext` after subscription disposal; removal does not wait for in-flight callbacks.

The subject copies its subscriber list under a lock for each publication, then calls `OnNext` synchronously in list order outside that lock. It does not serialize concurrent publications: callbacks from concurrent dispatches can overlap and their event pairs can interleave. `ActionDispatchedEvent` snapshots the shared store after reduction without a dispatch-wide lock. Overlapping dispatches can contribute another action's writes or mixed feature versions to that payload; these snapshots do not isolate each action's transition. Coordinate dispatch calls, including reentrant calls, without overlap when using them as serialized action history. It does not retain or replay earlier events. The DI store constructor publishes `StoreInitializedEvent` before construction returns, so an observer subscribing afterward does not receive that earlier initialization event.

Ordinary dispatch enters pre/post-reduction observation only when middleware reaches `CoreDispatch`; middleware that returns without calling `nextAction` produces neither event. The post-event is emitted only after pre-event callbacks and reduction finish successfully. A pre-event callback or reducer exception can therefore leave a dispatching event without its dispatched counterpart. Recognized restoration actions entering through `Store.Dispatch` expose pre-dispatch and restoration events. Use their payloads for the relevant boundary and read the current store separately when needed.

## Callback And Disposal Boundaries

An observer or listener exception propagates at its callback site, interrupts later callbacks in that captured list, and can interrupt the surrounding dispatch. The store does not translate reducer or callback failures into an automatic `OnError` event. Keep callbacks short and observational. A listener or event observer can call `Dispatch` synchronously. That nested dispatch runs before the outer callback returns and can nest notification lists and pre/post event pairs on the same thread; concurrent callers are not required for interleaving.

On normal store disposal, the subject calls `OnCompleted` on its captured observers, then the store clears listeners and stored state. If a completion callback throws, later observers are not completed and store clearing is skipped. Both disposed flags were already set, so another `Dispose` returns early and does not finish that partial cleanup.

For calls beginning after disposal completes, a new event-stream subscription invokes `OnCompleted` immediately and returns a no-op disposable only if that callback completes normally. A throwing callback propagates its exception without returning a handle. Ordinary state subscription, `Dispatch`, and `GetState` reject a disposed store with `ObjectDisposedException`.

Those guards run before acquiring subscription locks. A concurrent subscribe/dispose race can add a subscriber after the list was cleared without completing it; the post-disposal behavior above is not a guarantee for overlapping lifecycle calls. A publication or listener notification that already captured its list can also resume after disposal: an observer may receive `OnNext` after `OnCompleted`, or a state listener may run after store disposal. Completion does not ensure callback quiescence for overlapping calls. Coordinate subscription, disposal, and in-flight dispatch ownership.

`GetStateSnapshot` has no disposed-store guard. After normal disposal with no overlapping dispatch, it returns a copy of the cleared feature dictionary. A dispatch that already captured a feature state can finish reduction and assign its result after disposal clears that dictionary, repopulating a later snapshot. Disposal is therefore not a barrier guaranteeing empty snapshots when dispatch overlaps. Retained earlier snapshots still hold their original feature-object references.

The [store tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/StoreTests.cs) cover state-listener notification, unsubscribe, repeated subscription disposal, and null/disposed guards. Event replay and observer delivery rules above are verified from the subject implementation.

## Summary

Choose state listeners for notification callbacks and event observers for dispatch boundaries. Neither subscription replays past state changes, and synchronous callback failures can affect dispatch or disposal.

## Next Steps

- Read [Reservoir Reference](./reference.md) for store and feature registration.
- Read [Reservoir State Flow](../concepts/state-flow.md) for dispatch timing and reentrant callbacks.
