---
id: projection-tests
title: Projection Test Scenarios
description: Reference projection harness setup, event replay, scenario state, and assertion boundaries.
sidebar_position: 12
sidebar_label: Projection Test Scenarios
---

# Projection Test Scenarios

## Overview

`ReducerTestHarness<TProjection>` composes reducers for in-memory projection tests. `ProjectionScenario<TProjection>` accumulates state as historical and new events are applied.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Projections.ReducerTestHarness<TProjection>`
- `Mississippi.DomainModeling.TestHarness.Projections.ProjectionScenario<TProjection>`
- Projection types satisfying `new()`: value types, or nonabstract reference types with a public parameterless constructor

## Setup And State Ownership

The [harness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Projections/ReducerTestHarness.cs) starts with a new projection instance. `WithInitialState` replaces it with the supplied non-null value. `WithReducer<TReducer>` requires `new()` and constructs a reducer using its public parameterless constructor; the instance overload accepts a supplied non-null reducer. Although that overload accepts `IEventReducer<TProjection>`, all replay paths select typed `IEventReducer<TEvent, TProjection>` interfaces. A reducer implementing only the untyped interface can register but never match an event; the harness does not fall back to its `TryReduce` method.

`CreateScenario` captures the initial state value/reference at creation, without cloning it, and shares the harness's reducer list. Later `WithInitialState` replacement does not change an existing scenario's starting state; later reducer registration is visible to it. Finish configuration before creating scenarios and preserve immutable state.

Direct harness runs start from the configured initial state each time:

- `ApplyEvent<TEvent>` requires `TEvent : class`; value-type events cannot use this single-event API. It uses `OfType<IEventReducer<TEvent, TProjection>>` and takes the first assignable reducer. Event-type contravariance can select an earlier base-event reducer for a derived `TEvent`, ahead of a later exact-type reducer.
- `ApplyEvents` applies its events in order to a local state variable, selecting the first registered reducer with an interface for each event's exact runtime type.

These calls return their result without replacing the harness's configured initial state. They reuse that state reference: a reducer that mutates it in place can affect later runs. Independent replay requires nonmutating reducers or a fresh initial state/harness.

## Given And When

The [scenario](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Projections/ProjectionScenario.cs) keeps its current result in `State`. Both `Given` and `When` immediately apply an event using the same runtime-type selection as `ApplyEvents`. They do not defer replay until an assertion runs.

`Given(params object[])` applies events one at a time. It is not atomic: earlier events remain applied if a later event fails. Null event arrays and null individual events are rejected.

`AppliedEvents` records each non-null event before reduction is attempted. A missing reducer or reducer failure can therefore leave the attempted event in that list while `State` retains the prior assignment. Treat the list as recorded attempts rather than proof that every reduction succeeded.

Missing matching reducers throw `InvalidOperationException`. The runtime-type route invokes the typed `Reduce` method through reflection, which wraps reducer-thrown exceptions in `TargetInvocationException`. Direct `ApplyEvent<TEvent>` instead lets them propagate directly. Neither route runs the production root reducer's indexed/fallback `TryReduce` pipeline.

## Assertions

- `ThenAssert` and the action overload of `ThenShouldSatisfy` invoke a supplied callback with current state.
- The predicate overload of `ThenShouldSatisfy` requires true and uses the supplied reason in its xUnit assertion.
- `ThenEquals` and its `ThenShouldBe` alias compare expected public members structurally, allowing additional actual members.

Structural assertions require matching collection counts and duplicate occurrences. The expected value selects comparison: expected byte arrays are ordered, ordinary expected sequences are unordered, and expected dictionaries match keys and counts. That dictionary path requires both expected and actual values to implement nongeneric `System.Collections.IDictionary`. An actual value exposing only `IDictionary<TKey, TValue>` fails that assignability check even if its entries match. Use explicit callback assertions when sequence order or exact CLR type is part of the outcome.

The [contract tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) demonstrate expected-member subsets, unordered projection collections, and rejection of incomplete collections. The [source README](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/README.md) links executable Spring fixtures.

## Summary

Use a scenario for accumulating event history and direct harness methods for replay from the configured initial state. These helpers verify in-memory reducer outcomes; routing, mutable state, and the event ledger have their own boundaries.

## Next Steps

- Read [Building Projections](../../samples/spring-sample/tutorials/building-projections.md) for Spring's projection model.
- Read [Domain Modeling Concepts](../concepts/concepts.md) for the domain-facing boundaries.
