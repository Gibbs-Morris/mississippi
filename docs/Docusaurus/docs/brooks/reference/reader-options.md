---
id: reader-options
title: Brooks Reader Options
description: Look up the Brooks read-slice size, default, range partitioning, and read-time validation behavior.
sidebar_position: 2
sidebar_label: Reader Options
---

# Brooks Reader Options

## Overview

`BrookReaderOptions.BrookSliceSize` controls how Brooks divides a requested event range into slice reads. It does not limit the total number of events returned by a read.

## Applies To

- `Mississippi.Brooks.Runtime.Reader.BrookReaderOptions`
- Batch reads through `IBrookReaderGrain.ReadEventsBatchAsync()`
- Streaming reads through `IBrookAsyncReaderGrain.ReadEventsAsync()`

## Options And Defaults

`BrookSliceSize` is an init-only `long` property.

| Property | Default |
| --- | --- |
| `BrookSliceSize` | `100` |

The default and property shape are defined in [BrookReaderOptions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookReaderOptions.cs). Existing [option tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Reader/BrookReaderOptionsTests.cs) check the default and custom initial values.

## Registration

Runtime `AddEventSourcing()` registers `IOptions<BrookReaderOptions>` through the .NET options system. Its optional configuration callback configures `BrookProviderOptions`, which selects the stream provider name. Reader options are a separate options type.

The reader implementations consume `IOptions<BrookReaderOptions>`. The registration adds no startup validator for `BrookSliceSize`; the positive-size check occurs when the readers partition a nonempty range. See [BrooksRuntimeRegistrations](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/BrooksRuntimeRegistrations.cs).

## Read Behavior

The current readers partition inclusive position ranges into buckets aligned to multiples of `BrookSliceSize`. Each slice is clipped to the requested start and end, so the first and last slices can contain fewer positions than the configured size.

For example, with the default size of `100`, a request for positions `75` through `224` produces these three slice ranges:

| Slice | Inclusive positions | Count |
| --- | --- | --- |
| First | `75` through `99` | `25` |
| Second | `100` through `199` | `100` |
| Third | `200` through `224` | `25` |

The two readers use these slices differently:

- The [batch reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookReaderGrain.cs) starts the slice reads in parallel, awaits all results, and concatenates them in slice order into one immutable array. The total result can exceed `BrookSliceSize`.
- The [streaming reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookAsyncReaderGrain.cs) visits the slices in order and yields their events through an asynchronous enumerable. The option does not define a limit on the total stream length.

Both readers pass the caller's cancellation token to their slice reads. When the ending position is omitted, the cursor supplies it; an empty brook then returns an empty result. A resolved ending position before the start also returns an empty result before partitioning.

## Constraints And Failure Behavior

Use a positive `BrookSliceSize`. When a nonempty range is partitioned, a zero or negative size throws `ArgumentOutOfRangeException`. Creating the options object or registering it does not perform this check, and an empty-range read does not exercise it.

An explicit ending position bypasses cursor lookup. A nonnegative range beyond available events, including a range against an empty brook, can fail with `InvalidOperationException` from a slice read instead of returning an empty result.

Bucket calculation uses `double` arithmetic. It is not integer-exact across the full `long` range: for example, a size of `1` and start/end position `9007199254740995` can round to the wrong bucket. The table above describes ordinary-sized positions, not a guarantee for every representable `long`.

This option controls read partitioning. It does not configure storage retries or provide a throughput or latency guarantee.

## Summary

Brooks defaults to read slices of `100` positions. Batch reads combine all slices, streaming reads enumerate them in order, and a nonempty read requires a positive slice size.

## Next Steps

- Use [Brooks Reference](./reference.md) for the stream and package boundaries.
- Read [Runtime Composition](../../reference/runtime-composition.md) for the host builder and event-sourcing registration.
- Use [Brooks Storage Providers](../storage-providers/index.md) for the persistence boundary.
