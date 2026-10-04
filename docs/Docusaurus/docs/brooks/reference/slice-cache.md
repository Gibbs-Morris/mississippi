---
title: Brooks Slice Cache Reads
description: Reference incomplete slice-cache refresh, read failures, and recovery when a later storage query is complete.
sidebar_position: 11
sidebar_label: Slice Cache Reads
---

# Brooks Slice Cache Reads

A Brooks slice reader caches the events returned for its fixed range. If a requested upper position exceeds that cache, the read can fail even though a later complete storage query would allow the same grain to recover.

## Applies To

- `Mississippi.Brooks.Runtime.Reader.IBrookSliceReaderGrain`, used internally by Brooks readers
- The built-in slice reader's streaming and batch methods

## Initial Cache And Refresh

The [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs) enumerates the storage range on activation and caches the returned records. On a read, it queries that full range again when all three conditions hold:

- The cache contains fewer records than the slice's configured count.
- The requested upper position is within the slice's inclusive end.
- That upper position reaches beyond the cache's inferred coverage.

A refresh replaces the cache only if it contains exactly the slice's expected count and is larger than the current cache. A larger but still incomplete result is discarded: it may have omitted an earlier position and therefore shift positional indexing.

Once the cache is complete, ordinary covered reads do not query storage again. A delayed short refresh also cannot replace a complete cache populated by an overlapping read.

## Requested Range Coverage

The cache's last position is inferred as `sliceStart + cacheLength - 1`. An empty cache covers no position at or after the slice start. The reader derives each returned position from its array index; it does not independently verify a position field in every payload.

After any eligible refresh, an upper position beyond that inferred last position throws `InvalidOperationException` with `exceeds cached range` in the message. This check runs before yielding a record, so that read does not silently return the cached prefix as a partial success.

For a slice starting at 10 with count 3, a cache containing one record covers position 10. Reading through position 12 triggers a refresh. A two-record refresh is still incomplete and is discarded; a three-record refresh can populate the full slice and satisfy the read.

## Recovery And Cancellation

A later read of the same grain can retry the storage query and recover when the query returns the full slice. Persistently short queries keep producing the coverage failure. The cache behavior does not guarantee when storage will return a complete result.

The read token is forwarded to a refresh query and checked during cached enumeration. `ReadBatchAsync` collects `ReadAsync`, so both methods share the refresh and coverage rules. The [grain interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/IBrookSliceReaderGrain.cs) also exposes `DeactivateAsync`, which clears the local cache and requests idle deactivation; it does not repair storage.

The [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Reader/BrookSliceReaderGrainUnitTests.cs) cover incomplete activation, later recovery, persistently short results, shifted incomplete refreshes, complete cache reuse, and overlapping delayed refreshes. Recovery cases exercise streaming and batch reads.

## Summary

A missing upper position fails before partial results are returned. An eligible later read can recover through a complete refresh, while incomplete refreshes cannot replace the existing cache.

## Next Steps

- Read [Brooks Storage Providers](../storage-providers/index.md) for range-read contracts.
- Use [Brooks Troubleshooting](../troubleshooting/troubleshooting.md) for the wider diagnostic scope.
