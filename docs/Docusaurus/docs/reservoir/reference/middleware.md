---
title: Reservoir Middleware Reference
description: Reference synchronous middleware registration, next-action behavior, ordering, and the system-action boundary.
sidebar_position: 6
sidebar_label: Middleware
---

# Reservoir Middleware Reference

## Overview

Middleware wraps the synchronous dispatch path for ordinary Reservoir actions. Use it for an application-wide concern that belongs around dispatch, while feature reducers and effects retain their own responsibilities.

## Contract

`Mississippi.Reservoir.Abstractions.IMiddleware` defines `void Invoke(IAction action, Action<IAction> nextAction)`.

| Choice in `Invoke` | Effect |
| --- | --- |
| Call `nextAction(action)` | Continue with the current action |
| Call `nextAction` with another action | Continue with that replacement |
| Return without calling `nextAction` | Stop that ordinary action's downstream pipeline |

Call `nextAction` once for the normal pass-through pattern. Multiple calls deliberately execute the downstream pipeline multiple times and should be treated as multiple dispatch paths.

## Registration and Ordering

Register middleware with `IReservoirBuilder.AddMiddleware<TMiddleware>()`, where the type is a class implementing `IMiddleware`. The implementation registers middleware as transient and resolves it into the store's pipeline.

The first registered middleware is the outermost wrapper. Its before-next work runs first; its after-next work runs after the inner wrappers return. Configure middleware during startup before using the store.

The store resolves its middleware collection during construction and retains those instances across its dispatches. Treat mutable middleware fields as state shared by that store's operations; the transient DI registration does not create a fresh middleware instance for each action.

## Execution Boundary

`nextAction` is synchronous, but the store can start asynchronous effects during that call. Return from `nextAction` marks the synchronous downstream return, so observe effect completion through result actions and feature state.

For exact reduction boundaries, subscribe to `IStore.StoreEvents`: `ActionDispatchingEvent` occurs before reducers and `ActionDispatchedEvent` carries the resulting snapshot. Keep middleware's responsibility small so those observations remain useful to developers and AI-assisted debugging.

The store handles `ISystemAction` restoration/reset before building the user middleware pipeline. Those actions therefore use their dedicated path. Use [DevTools reference](./devtools.md) for local restoration behavior.

Handle middleware failures deliberately. An exception before `nextAction` prevents downstream dispatch; one after `nextAction` returns reaches the caller after reducers have run and effects may have started. Inspect the actual outcome before retrying an action, because a caller-visible exception can follow completed downstream work.

For recognized reset and restore system actions, observe `ActionDispatchingEvent` followed by `StateRestoredEvent`. Use `StateRestoredEvent` as the restoration completion boundary; those operations use the dedicated path instead of emitting the ordinary `ActionDispatchedEvent`.

## Source and Verification

- [IMiddleware](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IMiddleware.cs).
- [Builder registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/ReservoirBuilderRegistrations.cs).
- [Store dispatch and pipeline construction](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs).
- [Store tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Reservoir.Core.L0Tests/StoreTests.cs), including `MiddlewarePipelineExecutesInOrder`.

## Summary

Middleware controls whether and how an ordinary action proceeds through dispatch. Keep its synchronous boundary distinct from asynchronous effect completion and the dedicated system-action path.

## Next Steps

- [Action effect reference](./action-effects.md) for asynchronous feature work.
- [State flow](../concepts/state-flow.md) for reduction and notification timing.
