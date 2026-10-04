---
title: Snapshot Cache Reads
description: Reference snapshot activation, cached state reads, and failures during hydration and reconstruction.
sidebar_position: 4
---

# Snapshot Cache Reads

`ISnapshotCacheGrain<TSnapshot>` serves state for one exact snapshot version. The built-in implementation hydrates that state during activation, then returns it from memory.

## Applies To

- `Mississippi.Tributary.Abstractions.ISnapshotCacheGrain<TSnapshot>`
- The built-in `SnapshotCacheGrain<TSnapshot>` implementation
- Snapshot state types with a parameterless constructor

## Identity And Cached Reads

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotGrainFactory.cs) resolves a cache using `SnapshotKey`. Its encoded identity includes brook name, entity ID, version, snapshot storage name, and reducer hash. Requesting a different version selects a different cache identity.

[`GetStateAsync(CancellationToken = default)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/ISnapshotCacheGrain.cs) returns `ValueTask<TSnapshot>`. After activation, the built-in method returns the cached field without storage I/O or a cancellation check.

For reference-type state, repeated calls within that activation return the same cached instance. The method does not clone the result or deeply freeze its members. Consumers should preserve immutable state when using it within the runtime.

## Activation Load Decision

The [cache implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotCacheGrain.cs) parses its grain key, computes the current root reducer hash, and requests the exact snapshot from storage using the activation token.

- A stored envelope with a nonempty hash matching the current hash by ordinal comparison is converted into state and used immediately. That successful load does not request a rewrite.
- A missing envelope, empty hash, or mismatched hash starts reconstruction from the event stream.
- Reconstruction uses the base checkpoint selected by the existing [retention policy](./snapshot-retention.md), or a new state instance for target version zero.

Replay reads events after the base checkpoint through the requested version. Each event is converted to a domain event and passed to the root reducer in enumeration order. The activation token is forwarded to the base-state request and event reader.

## Failure And Persistence Boundaries

Storage reads, matching-envelope conversion, event conversion, and reducer failures propagate from activation. A matching envelope whose payload cannot be decoded does not automatically fall back to replay.

After reconstruction, eligible state is serialized before a one-way call to the snapshot persister. Envelope conversion or persister resolution can therefore fail activation before background work is dispatched.

The cache discards the persister call's task. Successful cache activation does not confirm completion of that storage write. Retention-skipped versions remain available in memory without serializing a persistence envelope.

The [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/SnapshotCacheGrainTests.cs) cover cached instance reuse, loading a matching envelope without a rewrite, replay for missing or incompatible envelopes, and using checkpoint zero for a later target.

## Summary

Snapshot cache reads return already-hydrated state at an exact version. Replay is selected by absence or hash incompatibility; other hydration failures propagate, and background persistence completes separately.

## Next Steps

- Read [Snapshot Retention](./snapshot-retention.md) for checkpoint selection and write eligibility.
- Read [Tributary Cosmos DB Provider](../storage-providers/cosmos.md) for storage identity and provider failures.
