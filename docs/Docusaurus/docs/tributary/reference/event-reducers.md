---
id: event-reducers
title: Event Reducer Composition
description: Reference event reducer matching, first-match dispatch, reference-reuse validation, registration, and hash identity.
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

The guard does not deeply compare members, roll back mutations, or require every result to be non-null. Value-type projections and null input state bypass the reference-reuse check. When no tried candidate succeeds, the root returns the original state reference. A custom `TryReduce` can mutate that object before returning false; the root does not restore its contents, and later candidates observe that mutation.

## Registration

[`AddReducer<TEvent, TProjection, TReducer>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/ReducerRegistrations.cs) adds transient registrations for both reducer interfaces. The delegate overload registers its adapter and both interfaces as transient services. Resolving those interfaces separately does not promise one shared reducer instance.

Both overloads call `AddRootReducer<TProjection>`, which uses `TryAddTransient` for `IRootReducer<TProjection>`. An existing root registration is preserved. Repeated reducer registrations can add more candidates; they are not deduplicated by these helpers. For repeated delegate-overload calls with the same closed `TEvent, TProjection`, each interface factory resolves the last registered `DelegateEventReducer<TEvent, TProjection>` under the default Microsoft DI container. The root receives repeated adapters wrapping that last delegate, rather than one candidate for each supplied delegate. The implementation-type overload retains separately registered implementations; its first-candidate priority does not imply per-delegate priority for this collision.

## Reducer Hash

The built-in root computes `GetReducerHash()` once. Its input starts with the ordinal-sorted CLR full names of all registered reducers, preserving duplicates and falling back to `Name`. It then appends priority tokens for indexed event types with more than one candidate. These groups are sorted by event type full name and defining assembly simple name; each group preserves reducer registration order. Each priority token contains the reducer's full name and defining assembly simple name. The root joins the input with `|`, hashes its UTF-8 bytes with SHA-256, and returns uppercase hexadecimal.

Reordering competing indexed reducers with distinct full names or defining assembly simple names changes the hash and snapshot stream identity. Reordering independent event types preserves the hash; moving an independent reducer around a competing group also preserves it when that group's priority stays the same. Roots without competing indexed groups retain the previous sorted-full-name fingerprint. The [priority tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/ReducerOrderTests.cs) cover replay and saved-snapshot compatibility; the [assembly tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/AssemblyReducerOrderTests.cs) cover same-named types from differently named assemblies and the historical fingerprint for independent routes.

The defining assembly version is excluded from priority tokens. For closed generic reducers, [`Type.FullName`](https://learn.microsoft.com/en-us/dotnet/api/system.type.fullname?view=net-10.0) still includes the generic arguments' full assembly names; changing those identities can change the hash. Method bodies, configured instance values, delegate contents, and fallback priority are not fingerprinted. Reducers sharing both the full name and defining assembly simple name remain indistinguishable. The hash does not prove that all reduction behavior is unchanged.

The [root tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/RootReducerTests.cs) cover indexed dispatch before a fallback registered later, first-match behavior, unmatched identity, reference-reuse rejection, and order-independent hashing. They do not establish priority over a fallback registered earlier; that broader ordering rule above is verified from the implementation. The [registration tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/ReducerRegistrationsTests.cs) cover transient services and root registration.

## Summary

The root tries exact-type reducers before fallbacks and stops at the first successful result. Immutable state remains the consumer's responsibility, and the current hash measures type identities and competing indexed priority rather than all behavior.

## Next Steps

- Read [Snapshot Retention](./snapshot-retention.md) for checkpoint reuse and reconstruction.
- Read [Building Projections](../../samples/spring-sample/tutorials/building-projections.md) for reducer composition in Spring.
