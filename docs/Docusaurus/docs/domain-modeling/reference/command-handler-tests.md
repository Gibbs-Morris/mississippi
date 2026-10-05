---
id: command-handler-tests
title: Command Handler Test Assertions
description: Reference isolated command-handler execution, event assertions, and failure-result checks.
sidebar_position: 11
sidebar_label: Command Handler Test Assertions
---

# Command Handler Test Assertions

## Overview

`CommandHandlerTestExtensions` executes a command handler directly and checks its returned operation result. These helpers support focused in-memory tests without running an aggregate grain.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Aggregates.CommandHandlerTestExtensions`
- `ICommandHandler<TCommand, TAggregate>` implementations
- Aggregate types satisfying `new()` (value types or nonabstract reference types with a public parameterless constructor), and reference-type commands

## Execution And State

[`Handle(state, command)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Aggregates/CommandHandlerTestExtensions.cs) rejects null handlers and commands. A null state is replaced with a new aggregate instance; a supplied state is passed through. It returns the handler's `OperationResult<IReadOnlyList<object>>` unchanged.

`HandleEvents` returns the emitted list on success and an empty list on failure. An empty list alone therefore cannot distinguish rejection from a successful result containing no events. Use `Handle` when the error details or success flag matter.

Each assertion helper executes the handler for that call. Chaining separate helper calls does not reuse one captured result. These methods do not persist emitted events, reduce them into state, or run runtime effects; handler exceptions propagate.

## Successful Results And Events

| Helper | Assertion |
| --- | --- |
| `ShouldSucceed` | Requires success and at least one event; returns the list for further checks. |
| `ShouldEmit` | Requires success and exactly one structurally equivalent event. |
| `ShouldEmitEvents` | Requires success and the expected event count and sequence, including nested sequence ordering. |

`ShouldEmitEvents` can verify a successful empty event list. `ShouldSucceed` deliberately rejects that same list because it also requires nonempty output.

The [structural comparer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/StructuralAssertions.cs) compares expected public data members recursively and permits additional actual members. Properties can match fields with the same name; member-based comparison does not require identical CLR types. Add an explicit type assertion when the event's type is part of the test's intent.

`ShouldEmit` uses unordered matching for ordinary expected nested sequences, preserving collection counts and duplicate occurrences. Expected byte arrays remain ordered. `ShouldEmitEvents` enables ordering throughout its structural comparison. Expected values implementing non-generic `System.Collections.IDictionary` select key/count matching rather than insertion order. A generic-only or read-only dictionary that does not implement that interface follows the `IEnumerable` branch; under `ShouldEmitEvents`, its enumeration order is compared.

## Failed Results

- `ShouldFail` requires `Success == false`.
- Its error-code overload also requires the exact expected code.
- `ShouldFailWithMessage` requires failure and an ordinal, case-sensitive message substring; another overload also checks the exact code.

Success assertions check the success flag before inspecting failed result data. Their assertion messages include the returned error code and message. Null expected events, event arrays, codes, or message strings are rejected by the corresponding helpers.

The [contract tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) cover failed-result assertions, expected-member subsets, dictionary contents, sequence order, duplicates, and cyclic-data failures.

## Summary

Choose a result assertion that matches the business outcome: success, emitted events, or rejection details. Event comparison checks expected structure; runtime persistence and exact CLR-type checks need separate evidence.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for command and event ownership.
- Read [Building an Aggregate](../../samples/spring-sample/tutorials/building-an-aggregate.md) for the Spring domain model.
