---
id: effect-iterations
title: Aggregate Effect Iterations
description: Reference the awaited effect cascade's iteration limit, immediate event persistence, and limit outcomes.
sidebar_position: 4
sidebar_label: Effect Iterations
---

# Aggregate Effect Iterations

## Overview

Awaited aggregate effects can yield events that trigger another round of effects. `AggregateEffectOptions.MaxEffectIterations` limits those cascade rounds in the generic aggregate runtime.

## Applies To

- `Mississippi.DomainModeling.Abstractions.AggregateEffectOptions`
- Awaited effect processing in `Mississippi.DomainModeling.Runtime.GenericAggregateGrain<TAggregate>`

## Limit Meaning

The [options type](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/AggregateEffectOptions.cs) exposes an init-only `int MaxEffectIterations`, defaulting to `10`. The property has no validation guard.

In the [aggregate implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/GenericAggregateGrain.cs), the first round dispatches the original persisted events. Each round processes every event pending for that round. Newly yielded events form the next round's pending list.

The limit counts rounds, rather than individual events, handler calls, or yielded results. One round can process multiple events and produce multiple follow-up events. It does not bound elapsed time or stop an effect that never finishes its enumeration.

## Persistence And State

Each yielded object is converted and appended immediately, before it is added to the next round's pending list. The runtime records its assigned brook position and loads the snapshot at that position. A non-null updated snapshot becomes the state used for subsequent dispatch calls.

An already-running root dispatch received its state argument when that call began. Updating the aggregate's local state after a yield does not replace the state argument inside that same root call.

The initial events have already been persisted before the awaited cascade starts. Reaching the iteration limit does not roll back either those events or follow-up events appended during the completed rounds.

## Reaching The Limit

With a limit of two, the first round handles the original events and the second handles their yielded events. Objects yielded in the second round are still persisted, but are not dispatched in a third round by this cascade.

The runtime logs an iteration-limit warning and records a limit metric when its round counter is at or above the configured limit. That condition does not require a nonempty pending list, so the signal can occur even if the final permitted round finishes the chain.

Zero or negative values skip the dispatch loop and reach the warning/metric condition. They are not rejected by the options type. With no registered awaited effects, the aggregate skips this cascade path.

The limit branch itself does not throw or turn command success into failure. The [existing custom-limit test](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/GenericAggregateGrainTests.cs) verifies a successful command with two dispatch calls for a continuously yielding chain configured to stop after two rounds.

## Summary

The default permits ten awaited cascade rounds. Events yielded during permitted rounds are persisted immediately; the limit stops further dispatch rounds rather than undoing writes or imposing a total event or time budget.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for effect ownership.
- Read [Brook Append Outcomes](../../reference/brook-append-outcomes.md) for persistence and publication failure boundaries.
