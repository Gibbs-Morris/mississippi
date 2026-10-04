---
title: Event Effect Dispatch
description: Reference root effect matching, sequential ordering, streamed results, and failure isolation.
sidebar_position: 3
---

# Event Effect Dispatch

`RootEventEffect<TAggregate>` composes awaited event effects. It invokes all selected effects whose `CanHandle` returns true and streams their yielded objects to its caller.

## Applies To

- `Mississippi.DomainModeling.Abstractions.IRootEventEffect<TAggregate>`
- `Mississippi.DomainModeling.Runtime.RootEventEffect<TAggregate>`

## Matching

The [root implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/RootEventEffect.cs) materializes its registered effects once at construction. It recognizes inherited `EventEffectBase<TEvent, TAggregate>` and `SimpleEventEffectBase<TEvent, TAggregate>` types for the root's aggregate type and indexes them by `TEvent`. Other implementations enter a fallback group.

For each event, the root looks up its exact runtime type, then visits the fallback group. It calls `CanHandle` on each effect in those selected groups. It does not search every indexed base event type for a derived event.

The [typed base](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/EventEffectBase.cs) uses an assignability check in `CanHandle`, but that check does not broaden the root's exact-type index lookup. An effect indexed for a base event type is not selected merely because a derived event would satisfy its `CanHandle` method.

`EffectCount` counts the registered effects, and `HasEffects` reports whether that count is nonzero. Neither property reports the number matching a particular event.

## Ordering And Results

Indexed effects run first, followed by fallback effects. Registration order is preserved within each group; it is not one global order across both groups.

Matching effects run sequentially. The root enumerates all results from one effect before moving to the next and preserves each effect's yield order. It forwards the event, aggregate state, brook key, event position, and cancellation token to the handler.

Dispatch returns an asynchronous enumerable. A null event is rejected immediately with `ArgumentNullException`; handler work runs as that enumerable is consumed. The root yields objects rather than persisting them itself.

The [root tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/RootEventEffectTests.cs) cover matching, fallback dispatch, multiple matching effects, unmatched events, and continuation after an effect fails.

## Failure Isolation

For handler invocation and enumeration, the root catches ordinary exceptions from obtaining the enumerable/enumerator, advancing it, reading its current item, and disposing it. It logs those failures and records error metrics; a failed effect does not automatically skip the remaining effects.

An `OperationCanceledException` at those protected boundaries ends the affected effect without ordinary-error reporting. The root can continue to the next effect with the same token; cancellation is not an unconditional stop of the entire pipeline.

`OutOfMemoryException`, `StackOverflowException`, and `ThreadInterruptedException` propagate through those catches. `CanHandle` runs outside the protected enumeration boundary, so its exceptions can also propagate rather than being isolated as handler failures.

The [lifecycle tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/EventEffects/RootEventEffectLifecycleTests.cs) exercise ordinary failures, cancellation, critical faults, disposal ownership, and successful yield ordering for indexed and fallback effects.

## Summary

Root dispatch uses exact-type indexing plus fallback matching, processes all matches sequentially, and isolates ordinary handler lifecycle failures. Selection predicates and critical failures have a different propagation boundary.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for domain behavior and effect ownership.
- Read [Spring Key Concepts](../../samples/spring-sample/concepts/key-concepts.md) for the sample's awaited and worker effect roles.
