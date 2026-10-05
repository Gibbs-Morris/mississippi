---
id: reducer-tests
title: Isolated Reducer Test Assertions
description: Reference direct reducer invocation, structural projection checks, and exception matching.
sidebar_position: 13
sidebar_label: Isolated Reducer Test Assertions
---

# Isolated Reducer Test Assertions

## Overview

`ReducerTestExtensions.Apply`, `ShouldProduce`, and `ShouldThrow` invoke one typed event reducer directly. Use these helpers when the test concerns that reducer's output or exception rather than a configured multi-reducer replay.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Projections.ReducerTestExtensions`
- `IEventReducer<TEvent, TProjection>` implementations
- Reference-type events and projection types satisfying `new()`: value types, or nonabstract reference types with a public parameterless constructor

## Apply And ShouldProduce

[`Apply(initialState, eventData)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Projections/ReducerTestExtensions.cs) rejects a null reducer or event. For reference-type projections, a null initial state constructs `new TProjection()`; the helper then calls typed `Reduce` and returns its result. With the `new()` constraint, `TProjection?` does not make a value-type projection nullable: struct callers supply a state value, and `default(TProjection)` can differ from an explicit parameterless constructor.

A supplied state is passed through without cloning. The helper does not select among reducers, persist events, or invoke a grain. Exceptions from the reducer propagate.

`ShouldProduce` uses the same invocation and compares the result with a non-null expected projection. The [structural comparison](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/StructuralAssertions.cs) checks expected public members recursively and allows additional actual members. It does not require identical CLR types for member-based comparison.

The expected value selects collection comparison. Ordinary expected sequences are unordered, with exact counts and duplicate occurrences; an expected `byte[]` is ordered. An expected non-generic `System.Collections.IDictionary` selects key/count matching and requires the actual value to implement that interface. Generic-only dictionaries instead follow unordered `IEnumerable` comparison and do not require actual dictionary-key lookup. An actual byte array compared with an ordinary expected sequence can therefore be treated as unordered. Use direct `Apply` followed by explicit assertions when exact type or collection order is part of the test.

## ShouldThrow

`ShouldThrow<TException, TEvent, TProjection>` uses the same reference-type null-state construction and supplied value-type state, then calls typed `Reduce`. It permits a null event to reach the reducer so its argument validation can be tested. A null reducer is rejected before invocation.

The assertion requires an exception assignable to `TException`, so derived exception types can satisfy it. When the thrown exception is an `AggregateException` that is not itself assignable to `TException`, the helper flattens it and selects matching inner exceptions. At least one matching inner exception is required.

If the aggregate itself satisfies the requested type, the helper checks that outer exception instead of flattening it. The helper returns no exception value.

## Message Patterns

The optional `expectedMessage` uses a case-insensitive, culture-invariant substring pattern:

- `*` matches any number of characters, including line breaks.
- `?` matches one character.
- Other characters are escaped as literals; the pattern is not anchored to the whole message.

For flattened aggregates, every selected matching inner exception must satisfy the message pattern. Inner exceptions of other types are not checked by that pattern. A null pattern skips message checking. Regex matching uses a one-second timeout.

The [contract tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) cover structural projection subsets, unordered collections, aggregate unwrapping, wildcard messages, and `TaskCanceledException` satisfying an `OperationCanceledException` expectation.

## Summary

Use `Apply` for custom checks, `ShouldProduce` for expected structure, and `ShouldThrow` for assignable exceptions and optional wildcard message checks. Each of those three methods tests a direct reducer invocation. `ForProjection<TProjection>()` instead constructs a `ReducerTestHarness<TProjection>` without invoking a reducer; see [Projection Test Harness](./projection-tests.md).

## Next Steps

- Read [Building Projections](../../samples/spring-sample/tutorials/building-projections.md) for Spring reducer examples.
- Read [Tributary Concepts](../../tributary/concepts/concepts.md) for runtime reduction ownership.
