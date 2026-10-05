---
id: projection-cursors
title: Projection Cursors
description: Reference shared brook/entity cursor identity, cached progress, and notification filtering.
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

Successful constructor and `Parse` results have non-null, nonempty, non-whitespace, pipe-free components. The combined name, separator, and entity ID may contain at most 4192 UTF-16 code units. `Parse` requires exactly two parts and reuses constructor validation; `TryParse` returns false with a default output for invalid input. The default struct bypasses validation and has null components. `FromBrookKey` and `ToBrookKey` preserve the two supplied components through constructor validation.

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrainFactory.cs) derives the brook name from `TProjection` for its typed convenience overload, then resolves the non-generic cursor grain by that key. Projection types consuming the same brook for the same entity therefore share this cursor identity.

## Activation And Reads

The [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs) starts with a tracked position of `-1`. On activation, it parses its key and reads the brook cursor from storage before subscribing to cursor notifications. The activation token is forwarded to that storage read. There is no atomic read/subscription handoff or second read. If the provider does not replay an update published between those operations, this activation can retain the earlier position until a later accepted notification or reactivation reloads storage.

It obtains the subscription stream ID from `IStreamIdFactory` and selects the Orleans provider through `BrookProviderOptions.OrleansStreamProviderName`.

[`GetPositionAsync()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/IUxProjectionCursorGrain.cs) returns the current in-memory position without a fresh storage query. It has no cancellation-token parameter and does not persist a separate last-processed projection position.

The [projection reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrain.cs) uses it in `GetLatestVersionAsync`. That method's token is currently reserved and unused. The returned version represents known brook progress; it does not prove that projection state or a client view has already been refreshed.

## Notification Filtering

- A null notification throws `ArgumentNullException`.
- After a sequence token has been recorded, tokenless or older-token deliveries are ignored.
- For an accepted delivery, the position advances only when `NewPosition` is strictly greater than the current value.
- The handler does not cross-check the payload's `BrookKey` against its own cursor key.

Accepted sequence tokens can update the watermark even when the position does not advance. Equal or older positions cannot move the tracked position backward.

The [cursor unit tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/UxProjectionCursorGrainUnitTests.cs) cover monotonic positions and retaining a sequence watermark across a tokenless delivery. The [key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/UxProjectionCursorKeyTests.cs) cover encoding, parsing, conversions, and invalid components.

## Lifecycle

`DeactivateAsync` requests idle deactivation. A stream error logs the error and makes the same request. Stream completion is logged and returns without requesting deactivation. A later activation reads its starting position from brook storage again.

## Summary

The shared cursor caches known brook progress and filters stream deliveries by token and position. Fetching its position is separate from reconstructing projection state or confirming client delivery.

## Next Steps

- Read [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the wider read and notification flow.
- Read [Runtime Composition](../../reference/runtime-composition.md) for host-owned stream provider setup.
