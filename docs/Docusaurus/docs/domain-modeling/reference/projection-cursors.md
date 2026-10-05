---
id: projection-cursors
title: Projection Cursors
description: Reference shared brook/entity cursor identity, cached positions, and notification filtering.
sidebar_position: 9
sidebar_label: Projection Cursors
---

# Projection Cursors

## Overview

`IUxProjectionCursorGrain` exposes the latest brook position known to its activation. Projection readers use that cached position to select a version to read; the cursor itself does not build projection state.

## Applies To

- `Mississippi.DomainModeling.Abstractions.UxProjectionCursorKey`
- `Mississippi.DomainModeling.Abstractions.IUxProjectionCursorGrain`
- The built-in cursor and projection grain factory

## Shared Identity

The [cursor key](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/UxProjectionCursorKey.cs) contains `BrookName` and `EntityId`, encoded as `brookName|entityId`. It contains no projection CLR type or version.

Successful constructor and `Parse` results have non-null, nonempty, non-whitespace, pipe-free components. The combined name, separator, and entity ID may contain at most 4192 UTF-16 code units. `Parse` requires exactly two parts and reuses constructor validation; `TryParse` returns false with a default output for invalid input. The default struct bypasses validation and has null components. `FromBrookKey` and `ToBrookKey` preserve components only when the destination constructor accepts them. `BrookKey` permits empty and whitespace-only components, but `FromBrookKey` rejects those with `ArgumentException` through cursor validation. `ToBrookKey` on a default cursor throws `ArgumentNullException` because its components are null.

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrainFactory.cs) derives the brook name from `TProjection` for its typed convenience overload, then resolves the non-generic cursor grain by that key. That overload requires `[BrookName]`; a missing attribute throws `InvalidOperationException` before grain resolution. Projection types consuming the same brook for the same entity therefore share this cursor identity.

## Activation And Reads

The [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs) starts with a tracked position of `-1`. On activation, it parses its key and reads the brook cursor from storage before subscribing to cursor notifications. The activation token is forwarded only to that storage read. The subsequent `SubscribeAsync(this)` receives no token, and the implementation does not recheck cancellation before subscribing; this token does not cancel the subscription wait or remaining activation work. There is no atomic read/subscription handoff or second read. If the provider does not replay an update published between those operations, this activation can retain the earlier position until a later accepted notification or reactivation reloads storage.

It obtains the subscription stream ID from `IStreamIdFactory` and selects the Orleans provider through `BrookProviderOptions.OrleansStreamProviderName`. The [built-in writer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Writer/BrookWriterGrain.cs) constructs `StreamId.Create(BrookCursorUpdates, fullBrookKey)` directly. A custom factory must reproduce that namespace/key when consuming this writer's updates; changing subscriber identity does not redirect publication.

[`GetPositionAsync()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/IUxProjectionCursorGrain.cs) returns the current in-memory position without a fresh storage query. An append can commit and then throw `BrookCursorPublicationException` when notification publication fails, leaving an already-active cursor behind persisted progress. Reconcile the confirmed committed position and retry `IBrookWriterGrain.PublishCursorAsync(position)` rather than appending the same events again; see [Brook Append Outcomes](../../reference/brook-append-outcomes.md). It has no cancellation-token parameter and does not persist a separate last-processed projection position.

The [projection reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrain.cs) uses it in `GetLatestVersionAsync`. That method's token is currently reserved and unused. The returned version is the cached storage or accepted-notification position; it does not prove current persisted progress or refreshed projection/client state. `PublishCursorAsync` can publish an arbitrary nonnegative position without checking storage, and the cursor can accept a position ahead of persisted events. Publish only a confirmed committed position when using the notification as a storage-progress signal.

## Notification Filtering

- A null notification throws `ArgumentNullException`.
- After a sequence token has been recorded, tokenless or older-token deliveries are ignored.
- For an accepted delivery within an activation, the position advances only when `NewPosition` is strictly greater than the current value.
- The handler does not cross-check the payload's `BrookKey` against its own cursor key.

Accepted sequence tokens can update the watermark even when the position does not advance. Within one activation, equal or older positions cannot move the tracked position backward. A new activation reloads storage and starts with a fresh sequence watermark; this can move the reported position backward when a previously accepted notification was ahead of storage. For example, a cursor that accepted position `100` can return `5` after reactivation if storage is still at `5`.

The [cursor unit tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/UxProjectionCursorGrainUnitTests.cs) cover monotonic positions and retaining a sequence watermark across a tokenless delivery. The [key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/UxProjectionCursorKeyTests.cs) cover encoding, parsing, conversions, and invalid components.

## Lifecycle

`DeactivateAsync` requests idle deactivation. A stream error logs the error and makes the same request. Stream completion is logged and returns without deactivation, resubscription, or a storage refresh. A cursor kept active by reads can therefore continue returning its last cached position after completion. Request deactivation to obtain a fresh storage read on activation, and arrange completed-stream/subscription recovery appropriate to the provider; completion does not trigger that recovery here. A later activation reads its starting position from brook storage again.

Each activation calls `SubscribeAsync(this)` without retaining, resuming, or removing its previous handle. Orleans [explicit subscriptions outlive activations](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis); subscribing again can accumulate durable subscriptions and duplicate callbacks. Monotonic position filtering does not remove those subscriptions. Deactivation/reload is therefore not a guarantee of restoring one subscription; account for existing handles and duplicate delivery in the host/provider lifecycle.

## Summary

The shared cursor caches a storage or accepted-notification position and filters stream deliveries by token and position. Notifications are not verified against storage, so that position does not establish persisted brook progress. Fetching its position is separate from reconstructing projection state or confirming client delivery.

## Next Steps

- Read [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the wider read and notification flow.
- Read [Runtime Composition](../../reference/runtime-composition.md) for host-owned stream provider setup.
