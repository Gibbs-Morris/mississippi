---
id: cursor-notifications
title: Brooks Cursor Notifications
description: Reference cursor-movement payloads, position filtering, sequence tokens, and client-delivery boundaries.
sidebar_position: 7
sidebar_label: Cursor Notifications
---

# Brooks Cursor Notifications

## Overview

`BrookCursorMovedEvent` reports a published brook position to stream observers. Publication can repeat the same position and does not itself verify persisted progress. The payload carries a stream identity and position, without event payload bytes.

## Applies To

- `Mississippi.Brooks.Abstractions.Streaming.BrookCursorMovedEvent`
- The built-in Brooks cursor, UX projection cursor, and Inlet projection-subscription observers

## Payload

The [sealed positional record](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Streaming/BrookCursorMovedEvent.cs) has two properties:

| Property | Meaning |
|----------|---------|
| `BrookKey` | A `string` identifying the brook |
| `NewPosition` | The published `BrookPosition` |

The [built-in writer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Writer/BrookWriterGrain.cs) supplies the complete stream key, in `brookName|entityId` form, and the position being published. Its [existing publication tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Writer/BrookWriterGrainUnitTests.cs) verify these values.

The record stores its supplied properties without parsing the key or checking that the position represents persisted progress. For append commitment and safe publication retry, use [Brook Append Outcomes](../../reference/brook-append-outcomes.md).

## Observer Filtering

The [Brooks cursor grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs) initializes its tracked position from storage on activation. For notifications:

- Once a stream sequence token has been recorded, a tokenless delivery or an older-token delivery is ignored.
- Among accepted deliveries, the tracked position advances only when `NewPosition` is strictly greater than its current value.
- The grain uses its own key and subscribed stream to identify the brook; it does not cross-check the payload's `BrookKey` against its grain key.

The [cursor unit tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Cursor/BrookCursorGrainUnitTests.cs) verify that a tokenless delivery does not clear an established sequence watermark.

The [UX projection cursor](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs) applies the same token and increasing-position filters. Both cursor implementations keep the sequence watermark only in their current activation. Reactivation reloads the position from storage and starts without a recorded token.

The [Inlet subscription grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs) uses the payload's exact key string to find interested subscriptions. On the first subscription to a brook, it initializes the position from `ReadCursorPositionAsync` without notifying that initial version. It ignores unknown brooks and positions at or below the recorded value; its sequence-token parameter is unused.

For a newer position, the implementation sends a projection path, entity ID, and version for each interested subscription. Several subscriptions can produce several notifications for one connection. The [existing subscription tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Runtime.L0Tests/InletSubscriptionSetupTests.cs) assert notification counts and paths; the entity/version forwarding described here comes from the implementation.

## Delivery Boundary

These filters govern local observer state. Inlet records the newer position before attempting client notifications, so repeating that position does not by itself retry a failed client send. Successful publication does not prove that every projection or client has processed the update.

On a stream error, both cursor implementations request idle deactivation, allowing a later activation to subscribe again. UX cursor completion only logs; Inlet error and completion callbacks also only log and do not themselves resubscribe.

## Summary

A cursor notification identifies a brook and published position. The built-in observers apply their own position and token filters; receiving the payload does not establish completed projection or client delivery.

## Next Steps

- Read [Brook Append Outcomes](../../reference/brook-append-outcomes.md) for publication failures and confirmed-position retries.
- Read [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the wider projection and client flow.
