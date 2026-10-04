---
title: Projection Test Scenarios
description: Reference projection harness setup, event replay, scenario state, and assertion boundaries.
sidebar_position: 12
---

# Projection Test Scenarios

`ReducerTestHarness<TProjection>` composes reducers for in-memory projection tests. `ProjectionScenario<TProjection>` accumulates state as historical and new events are applied.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Projections.ReducerTestHarness<TProjection>`
- `Mississippi.DomainModeling.TestHarness.Projections.ProjectionScenario<TProjection>`
- Projection types with a parameterless constructor

## Setup And State Ownership

The [harness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Projections/ReducerTestHarness.cs) starts with a new projection instance. `WithInitialState` replaces it with the supplied non-null value. `WithReducer<TReducer>` constructs a reducer using its parameterless constructor; the instance overload accepts a supplied non-null reducer.

`CreateScenario` uses the configured initial state and reducer list without defensive cloning. Preserve immutable state and finish registration before creating scenarios; the scenario does not freeze a separate configuration snapshot.

Direct harness runs start from the configured initial state each time:

- `ApplyEvent<TEvent>` selects the first reducer implementing `IEventReducer<TEvent, TProjection>` for that generic argument.
- `ApplyEvents` applies its events in order to a local state variable, selecting the first registered reducer with an interface for each event's exact runtime type.

These calls return their result without replacing the harness's configured initial state.

## Given And When

The [scenario](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Projections/ProjectionScenario.cs) keeps its current result in `State`. Both `Given` and `When` immediately apply an event using the same runtime-type selection as `ApplyEvents`. They do not defer replay until an assertion runs.

`Given(params object[])` applies events one at a time. It is not atomic: earlier events remain applied if a later event fails. Null event arrays and null individual events are rejected.

`AppliedEvents` records each non-null event before reduction is attempted. A missing reducer or reducer failure can therefore leave the attempted event in that list while `State` retains the prior assignment. Treat the list as recorded attempts rather than proof that every reduction succeeded.

Missing matching reducers throw `InvalidOperationException`. The runtime-type route invokes the typed `Reduce` method through reflection; it does not run the production root reducer's indexed/fallback `TryReduce` pipeline.

## Assertions

- `ThenAssert` and the action overload of `ThenShouldSatisfy` invoke a supplied callback with current state.
- The predicate overload of `ThenShouldSatisfy` requires true and uses the supplied reason in its xUnit assertion.
- `ThenEquals` and its `ThenShouldBe` alias compare expected public members structurally, allowing additional actual members.

Structural assertions require matching collection counts and duplicate occurrences, but ordinary nested sequences are unordered. Byte arrays remain ordered; dictionaries match keys and counts. Use explicit callback assertions when sequence order or exact CLR type is part of the outcome.

The [contract tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) demonstrate expected-member subsets, unordered projection collections, and rejection of incomplete collections. The [source README](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/README.md) links executable Spring fixtures.

## Summary

Use a scenario for accumulating event history and direct harness methods for independent replay results. These helpers verify in-memory reducer outcomes; their routing and event ledger have their own contracts.

## Next Steps

- Read [Building Projections](../../samples/spring-sample/tutorials/building-projections.md) for Spring's projection model.
- Read [Domain Modeling Concepts](../concepts/concepts.md) for the domain-facing boundaries.
