---
title: Reservoir Action Effect Reference
description: Reference typed action effects, feature registration, dispatch matching, cancellation, and failure actions in Reservoir.
sidebar_position: 3
sidebar_label: Action Effects
---

# Reservoir Action Effect Reference

## Overview

Action effects handle work triggered by client actions and can emit further actions. Use them to keep I/O and other reactions explicit while reducers remain pure state transitions.

## Applies To

- `Mississippi.Reservoir.Abstractions`: effect contracts and base classes.
- `Mississippi.Reservoir.Core`: feature-scoped effect composition and store dispatch.
- `Mississippi.Reservoir.TestHarness`: controlled effect verification.

## Choose a Contract

| Contract | Implement | Use |
| --- | --- | --- |
| `ActionEffectBase<TAction, TState>` | `HandleAsync(TAction, TState, CancellationToken)` returning `IAsyncEnumerable<IAction>` | Yield progress, result, or follow-up actions |
| `SimpleActionEffectBase<TAction, TState>` | `HandleAsync(TAction, TState, CancellationToken)` returning `Task` | Perform work with no emitted actions |
| `IActionEffect<TState>` | `CanHandle(IAction)` and the untyped `HandleAsync` contract | Handle a custom set of action types |

The typed bases require `TAction : IAction` and a reference-type `TState : IFeatureState`. They supply the type check and delegate to your typed method. For an async iterator, use `[EnumeratorCancellation]` on the token parameter when the implementation needs cancellation during enumeration.

Definitions: [ActionEffectBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/ActionEffectBase.cs), [SimpleActionEffectBase](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/SimpleActionEffectBase.cs), and [IActionEffect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IActionEffect%7BTState%7D.cs).

## Register an Effect

Register an effect through `IReservoirFeatureBuilder<TState>.AddActionEffect<TEffect>()` inside the feature's `AddFeatureState<TState>(...)` callback. `TEffect` is a class implementing `IActionEffect<TState>`.

The implementation registers effect services as transient and resolves them into the feature's effect pipeline. Keep effects stateless with respect to individual requests: carry request inputs and identifiers in actions and use injected services for external work.

See [IReservoirFeatureBuilder](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IReservoirFeatureBuilder.cs) and [ReservoirBuilderRegistrations](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/ReservoirBuilderRegistrations.cs).

## Dispatch and State

The normal store pipeline reduces the originating action before triggering effects. An effect receives the feature state supplied when that effect pipeline is invoked. Use the action's inputs for the request itself, and make result handling explicit when other actions can change the feature while work is in progress.

Typed effects are indexed by their declared action type and selected using the dispatched action's exact runtime type. Custom implementations outside those typed bases use the fallback `CanHandle` path. Matching effects are enumerated in sequence for a dispatch; separate dispatches can overlap while asynchronous work awaits completion.

The live store dispatches yielded actions back through its normal pipeline. A result action can therefore update state or trigger another effect. Design a terminating action sequence, with distinct request and result actions where that makes progress clear.

Implementation: [RootActionEffect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/RootActionEffect.cs) and [Store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs).

## Cancellation and Failure Handling

`IStore.Dispatch` returns `void`; asynchronous completion is observed through later actions and state. The store passes `CancellationToken.None` to started effects. Give cancelable external work an explicit lifetime owned by the effect or its service.

Publish expected failures as actions that reducers can turn into visible error state. The store boundary catches effect exceptions; exception throwing by itself does not create a feature error action. Handle the expected failures of your injected service at the appropriate boundary and preserve useful request context in the result action.

The test harness's `WhenAsync` accepts an explicit token and passes it to effects. That is a controlled testing surface; use it to verify how an effect responds to cancellation separately from the live store's lifetime policy.

## Testing

[Test a Reservoir feature and effect](../how-to/test-feature.md) provides complete, executable example files. The example verifies both effect input state and the distinction between captured output and applying a result action.

Named request, result, and failure actions give an AI assistant inspectable behavior to implement and test. Treat their expected state transitions as the acceptance contract; asynchronous scheduling is a separate execution concern.

## Summary

Choose a typed effect base for a single action type, register it with its feature, and expose progress and results through explicit actions. Keep request lifetime and failure reporting intentional.

## Next Steps

- [Test a feature and effect](../how-to/test-feature.md).
- [Add a feature](../how-to/create-feature.md).
- [State flow](../concepts/state-flow.md).
