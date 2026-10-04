---
id: projection-notifications
title: Projection Update Notifications
description: Reference the client projection notifier's dispatched actions, validation, and store integration boundary.
sidebar_position: 13
sidebar_label: Projection Update Notifications
---

# Projection Update Notifications

## Overview

`IProjectionUpdateNotifier` provides a dispatch bridge from a notification source to Reservoir. The default `ProjectionNotifier` constructs an action and calls the store; registered reducers determine the resulting state changes.

## Applies To

- `Mississippi.Inlet.Client.Abstractions.IProjectionUpdateNotifier`
- `Mississippi.Inlet.Client.ProjectionNotifier`
- Client registration through `IReservoirBuilder.AddInletClient()`

## Notification Methods

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/IProjectionUpdateNotifier.cs) exposes three methods. Each has a projection type parameter `T` constrained to `class`:

| Method | Dispatched action | Payload |
| --- | --- | --- |
| `NotifyConnectionChanged<T>` | `ProjectionConnectionChangedAction<T>` | Entity ID and connection flag |
| `NotifyError<T>` | `ProjectionErrorAction<T>` | Entity ID and exception |
| `NotifyProjectionUpdated<T>` | `ProjectionUpdatedAction<T>` | Entity ID, nullable projection data, and version |

The [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ProjectionNotifier.cs) calls `IStore.Dispatch` synchronously for each notification. It does not fetch a projection, contact the server, or update a separate mutable projection cache.

## Validation And Pass-Through

The constructor rejects a null store. Every notification method rejects a null entity ID; `NotifyError` also rejects a null exception. These guards throw `ArgumentNullException` before dispatch.

The notifier does not reject empty or whitespace entity IDs. It forwards the supplied ID, flag, exception, and data without normalization or copying.

`NotifyProjectionUpdated` accepts null data and forwards the supplied `long` version without a range or ordering check. It does not compare that version with the current store entry or discard an older update itself. The notification source must supply the data and version appropriate to its protocol.

Dispatch exceptions propagate to the caller. The notifier does not catch them, retry, or turn an update failure into a second error action automatically.

## Registration And Ownership

[`AddInletClient`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletClientRegistrations.cs) uses `TryAddScoped<IProjectionUpdateNotifier>` to create a `ProjectionNotifier` from the registered `IStore`. An earlier notifier registration is preserved by that registration call.

Resolving this service supplies the dispatch bridge. Connection establishment, subscriptions, projection retrieval, and recovery remain responsibilities of the configured integration. Calling a notifier method does not prove a transport is connected or that projection data was fetched successfully.

Existing [ProjectionNotifier tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/ProjectionNotifierTests.cs) capture dispatched action types and payloads, cover both connection flags and null data, and verify null guards. Version-ordering and retry boundaries above are verified from the implementation.

## Summary

Use the notifier to translate known connection, error, and data notifications into store actions. It passes the supplied payload through to dispatch; transport work and update ordering belong to the integration that calls it.

## Next Steps

- Read [Inlet Reference](./reference.md) for client registration and SignalR composition.
- Read [Reservoir State Flow](../../reservoir/concepts/state-flow.md) for dispatch and reducer execution.
