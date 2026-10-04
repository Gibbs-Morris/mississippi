---
id: effect-tests
title: Effect Test Capture
description: Reference effect harness invocation, captured aggregate commands, and dispatch assertions.
sidebar_position: 14
sidebar_label: Effect Test Capture
---

# Effect Test Capture

## Overview

Domain Modeling's effect test harnesses invoke effects in memory and capture calls to mocked aggregate grains. They let consumers inspect command targets separately from any events yielded by the effect.

## Applies To

- `Mississippi.DomainModeling.TestHarness.Effects.EffectTestHarness<TEffect, TEvent, TAggregate>`
- `FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate>`
- `EffectTestResult` and `EffectTestExtensions`

## Configure And Invoke

The [general harness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Effects/EffectTestHarness.cs) supplies a strict mocked `IAggregateGrainFactory`, grain context, and logger through `Build`. Its defaults are brook name `TEST.DOMAIN.AGGREGATE`, entity ID `test-entity`, and event position `1`.

`InvokeAsync` looks for the five-parameter `HandleSimpleAsync` method first and awaits its task when present. Otherwise it looks for `HandleAsync`, enumerates its `IAsyncEnumerable<object>` with the supplied token, and returns the collected yielded objects. If neither supported path is found, it throws `InvalidOperationException`.

The [worker-effect harness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Effects/FireAndForgetEffectTestHarness.cs) invokes `IFireAndForgetEventEffect<TEvent, TAggregate>.HandleAsync` directly and awaits it. Its default brook key is `TEST.DOMAIN.ENTITY|test-entity`, with position `1`; its setters can replace those inputs.

These direct calls do not exercise root-effect matching, Orleans worker dispatch, one-way delivery, or aggregate persistence. Even the worker-effect harness awaits the effect locally.

## Captured Commands And Results

In either harness, `WithAggregateGrainResponse<TTargetAggregate>(entityId, response)` configures the factory's string-entity-ID overload and the grain's two-argument `ExecuteAsync(command, cancellationToken)` overload. Calls through those setups record `(AggregateType, EntityId, Command)` before returning the configured response.

The factory overload taking an `AggregateKey` is not configured and fails through the strict factory mock. The grain's overload taking an expected version is not part of this capture setup; it does not record a command through this callback.

A captured command therefore proves a call was made even when the configured operation result reports failure. It does not prove that the target accepted or persisted the command. An unconfigured factory lookup fails through the strict mock.

The general harness's yielded objects are a separate result from `DispatchedCommands`. Both harnesses accumulate captured commands across invocations; neither invocation clears its list.

[`EffectTestResult`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Effects/EffectTestResult.cs) stores the supplied list without copying it. `DispatchCount` and `HasDispatches` read that list's current contents. The general harness's `ToResult` consequently exposes a live capture list rather than a frozen snapshot.

## Dispatch Assertions

The [extensions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/Effects/EffectTestExtensions.cs) work on a command list or an `EffectTestResult`:

- `ShouldHaveDispatched<TCommand>` returns the first command assignable to `TCommand`. It does not require exactly one match.
- `ShouldHaveDispatchedTo<TAggregate>` returns the first tuple with exactly that aggregate CLR type and, when supplied, the exact entity ID. It does not restrict the command's type.
- `ShouldHaveNoDispatches` requires an empty list. The result overload returns the same result for chaining.

Missing matches throw assertion failures before a command or default tuple can be returned. Add explicit count, ordering, command-data, and operation-result assertions when those are part of the test's intent.

The [contract tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.TestHarness.L0Tests/AssertionContractTests.cs) cover target selection, missing dispatches, and empty capture assertions.

## Summary

Use captured commands to verify which aggregate was called and yielded objects to verify effect output. These in-memory observations remain separate from target success and runtime delivery guarantees.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for effect ownership.
- Read [Building an Aggregate](../../samples/spring-sample/tutorials/building-an-aggregate.md) for the Spring command model.
