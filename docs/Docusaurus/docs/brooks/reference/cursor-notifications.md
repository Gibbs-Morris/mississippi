---
id: cursor-notifications
title: Brooks Cursor Notifications
description: Reference cursor-movement payloads, position filtering, sequence tokens, and client-delivery boundaries.
sidebar_position: 7
sidebar_label: Cursor Notifications
---

# Brooks Cursor Notifications

## Overview

`BrookCursorMovedEvent` reports a published brook position to stream observers. Publication can repeat the same position and does not itself verify persisted progress. The payload carries a brook key and position, without event payload bytes.

## Applies To

- `Mississippi.Brooks.Abstractions.Streaming.BrookCursorMovedEvent`
- The built-in Brooks cursor, UX projection cursor, and Inlet projection-subscription observers

## Payload

The [sealed positional record](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Streaming/BrookCursorMovedEvent.cs) has two properties:

| Property | Meaning |
|----------|---------|
| `BrookKey` | A `string` identifying the brook |
| `NewPosition` | The published `BrookPosition` |

The Orleans serialization alias is `Mississippi.Brooks.Abstractions.Streaming.BrookCursorMovedEvent`. Its field IDs are `0` for `BrookKey` and `1` for `NewPosition`; property order alone does not define that wire contract.

The [built-in writer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Writer/BrookWriterGrain.cs) supplies the brook key, in `brookName|entityId` form, and the position being published. It separately selects the configured `OrleansStreamProviderName` and the `BrookCursorUpdates` stream namespace; the payload key alone does not identify that complete route. Its [existing publication tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Writer/BrookWriterGrainUnitTests.cs) verify these values.

The record stores its supplied properties without parsing the key or checking that the position represents persisted progress. For append commitment and safe publication retry, use [Brook Append Outcomes](../../reference/brook-append-outcomes.md).

## Observer Filtering

The [Brooks cursor grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs) initializes its tracked position from storage on activation. For notifications:

- Once a stream sequence token has been recorded, a tokenless delivery or an older-token delivery is ignored. An equal token passes this filter; the position must still increase to advance the cursor.
- Among accepted deliveries, the tracked position advances only when `NewPosition` is strictly greater than its current value.
- The grain uses its own key and subscribed stream to identify the brook; it does not cross-check the payload's `BrookKey` against its grain key.

The [cursor unit tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Cursor/BrookCursorGrainUnitTests.cs) verify that a tokenless delivery does not clear an established sequence watermark. A sequence token is separate provider-dependent metadata passed to the observer callback, not a property of `BrookCursorMovedEvent`. The built-in writer calls `OnNextAsync` without an explicit token. If all delivered callbacks are tokenless, no watermark is recorded and the token filter remains inactive; the increasing-position filter still applies.

The [UX projection cursor](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs) applies the same token and increasing-position filters. It also does not inspect the payload's `BrookKey`; it relies on its subscribed stream identity, so a custom publisher's mismatched payload key does not prevent position advancement. Both cursor implementations keep the sequence watermark only in their current activation. They record an accepted newer token before comparing positions, so an equal or stale position can still advance the token watermark. A later delivery with an older token is then ignored even if its position would advance the cursor. Reactivation reloads the position from storage and starts without a recorded token.

The [Inlet subscription grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs) uses the payload's exact key string to find interested subscriptions. On the first subscription to a brook, it initializes the position from `ReadCursorPositionAsync` without notifying that initial version. It ignores unknown brooks and positions at or below the recorded value; its sequence-token parameter is unused.

For a newer position, the implementation sends a projection path, entity ID, and version for each interested subscription. Several subscriptions can produce several notifications for one connection. The [existing subscription tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Runtime.L0Tests/InletSubscriptionSetupTests.cs) assert notification counts and paths; the entity/version forwarding described here comes from the implementation.

## Delivery Boundary

These filters govern local observer state. Inlet records the newer position before attempting client notifications, so repeating that position does not by itself retry a failed client send. `OrleansException` and `InvalidOperationException` from a send are logged and fan-out continues to later subscriptions. Other exceptions escape the callback and stop the remaining sends. Fan-out follows a `HashSet<string>` without sorting, so subscription/insertion order does not predict which sends precede such a failure. Because the position was already recorded, repeating it skips those remaining subscriptions too. Successful publication does not prove that every projection or client has processed the update. The Brooks cursor has `[ImplicitStreamSubscription]`; its activation-time `SubscribeAsync` attaches the observer to that existing implicit subscription, rather than creating a new subscription after the storage read. See [Orleans implicit subscriptions](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-quick-start?pivots=orleans-7-0). UX cursor activation and Inlet first-subscription setup instead read storage before creating their explicit subscriptions, without a second read or atomic handoff. If the provider does not replay an update published in that explicit-subscription gap, those observers can retain an earlier position until a later accepted notification or reactivation/setup reloads storage.

On a stream error, both cursor implementations request idle deactivation, allowing a later activation to subscribe again. The UX cursor does not resume existing handles or unsubscribe its prior explicit subscription. [Explicit Orleans subscriptions](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis#writing-subscription-logic) survive activation loss; each successful activation subscribes again and can accumulate additional subscriptions. Brooks cursor completion uses the default observer no-op and neither deactivates nor resubscribes. UX cursor completion only logs; Inlet error and completion callbacks also only log and do not themselves resubscribe. Ordinary Inlet reactivation also loses its activation-local subscription entries, brook mappings, positions, and handles. It has no activation hook to resume retained explicit subscriptions or restore client fan-out. Re-subscribing rebuilds routing in the new activation but can add another durable subscription alongside those retained from earlier activations.

When the last Inlet subscription to a brook is removed, or `ClearAllAsync` runs, an `OrleansException` or `InvalidOperationException` from `UnsubscribeAsync` is logged before the handle and local routing state are discarded. The durable subscription can survive that failed cleanup without a retained handle for retry; subscribing again can create an additional subscription.

## Summary

A cursor notification identifies a brook and published position. The built-in observers apply their own position and token filters; receiving the payload does not establish completed projection or client delivery.

## Next Steps

- Read [Brook Append Outcomes](../../reference/brook-append-outcomes.md) for publication failures and confirmed-position retries.
- Read [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the wider projection and client flow.
