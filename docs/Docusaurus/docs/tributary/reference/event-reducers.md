---
id: event-reducers
title: Event Reducer Composition
description: Reference event reducer matching, first-match dispatch, immutable results, registration, and hash identity.
sidebar_position: 6
sidebar_label: Event Reducer Composition
---

# Event Reducer Composition

## Overview

`RootReducer<TProjection>` applies at most one successful reducer for each event and keeps the original state when none handles it. Replaying several events applies that choice repeatedly to the state returned by the previous event.

## Applies To

- `Mississippi.Tributary.Abstractions.IEventReducer<TProjection>` and its typed interface
- `Mississippi.Tributary.Runtime.RootReducer<TProjection>`
- `EventReducerBase<TEvent, TProjection>` and `DelegateEventReducer<TEvent, TProjection>`

## Matching And Priority

At construction, the [root implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/RootReducer.cs) materializes the supplied reducers. It indexes recognized `IEventReducer<TEvent, TProjection>` implementations by event type when their projection argument matches this root's `TProjection`.

For each reducer, extraction stops at the first matching typed interface returned by reflection. A reducer implementing several event types is indexed under only that first type; its other interfaces do not create additional indexed or fallback entries. Use separate reducer implementations when each event type needs routing.

For each `Reduce(state, eventData)` call:

1. Reject null event data.
2. Try indexed reducers for the event's exact runtime CLR type, preserving order within that group.
3. If none returns true from `TryReduce`, try fallback reducers whose event type was not recognized, preserving their order.
4. Return the first successful reducer's projection, or the original state if none matches.

Indexed reducers have priority over fallback reducers even when a fallback appeared earlier in the original list. Reducers indexed for another event type are not visited. A typed base-class event registration is not automatically visited for an event with a derived runtime type.

The root does not combine every matching reducer's output for one event. Reducer exceptions propagate immediately; it does not catch a failure and continue to the next candidate.

## Result Validation

For reference-type projections with non-null input state, the root rejects a successful reducer returning that same reference with `InvalidOperationException`. [`EventReducerBase`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/EventReducerBase.cs) and the [delegate adapter](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/DelegateEventReducer.cs) also apply this guard.

The guard does not deeply compare members, roll back mutations, or require every result to be non-null. Value-type projections and null input state bypass the reference-reuse check. An unmatched event intentionally preserves the original state.

## Registration

[`AddReducer<TEvent, TProjection, TReducer>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/ReducerRegistrations.cs) adds transient registrations for both reducer interfaces. The delegate overload registers its adapter and both interfaces as transient services. Resolving those interfaces separately does not promise one shared reducer instance.

Both overloads call `AddRootReducer<TProjection>`, which uses `TryAddTransient` for `IRootReducer<TProjection>`. An existing root registration is preserved. Repeated reducer registrations can add more candidates; they are not deduplicated by these helpers.

## Reducer Hash

The current root computes `GetReducerHash()` once from reducer CLR type names: sort them using ordinal order, join them with `|`, hash the UTF-8 bytes with SHA-256, and return uppercase hexadecimal.

The input preserves duplicate type names but excludes method bodies, delegate contents, and registration priority. Reordering the same reducer types can therefore change first-match behavior without changing this hash. Treat it as the current type-based identity, not proof that reduction behavior is unchanged.

The [root tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/RootReducerTests.cs) cover indexed dispatch before a fallback registered later, first-match behavior, unmatched identity, reference-reuse rejection, and order-independent hashing. They do not establish priority over a fallback registered earlier; that broader ordering rule above is verified from the implementation. The [registration tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/ReducerRegistrationsTests.cs) cover transient services and root registration.

## Summary

The root tries exact-type reducers before fallbacks and stops at the first successful result. Immutable state remains the consumer's responsibility, and the current hash measures reducer type names rather than all behavior.

## Next Steps

- Read [Snapshot Retention](./snapshot-retention.md) for checkpoint reuse and reconstruction.
- Read [Building Projections](../../samples/spring-sample/tutorials/building-projections.md) for reducer composition in Spring.
