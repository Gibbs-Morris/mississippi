---
id: snapshot-persistence
title: Snapshot Persistence
description: Reference the one-way snapshot persister, storage forwarding, and write failure observation.
sidebar_position: 5
sidebar_label: Snapshot Persistence
---

# Snapshot Persistence

## Overview

`ISnapshotPersisterGrain` receives an already-serialized snapshot envelope and forwards it to storage. Its Orleans method is one-way, so a cache caller does not wait for the storage write to complete.

## Applies To

- `Mississippi.Tributary.Abstractions.ISnapshotPersisterGrain`
- The built-in `SnapshotPersisterGrain` implementation
- `Mississippi.Tributary.Runtime.Storage.Abstractions.ISnapshotStorageWriter`

## Identity And Write Call

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotGrainFactory.cs) resolves a persister by `SnapshotKey`, matching the cache's version identity. On activation, the [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotPersisterGrain.cs) parses that key from the grain's string identity.

`PersistAsync(SnapshotEnvelope, CancellationToken = default)` forwards the stored key, supplied envelope, and token to one `ISnapshotStorageWriter.WriteAsync` call. It awaits that call inside the implementation.

The persister does not rebuild state, serialize it, compare the envelope's reducer hash with the key, or validate the envelope's format and byte-size metadata. Those fields are passed to the selected storage writer.

## Completion And Failures

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/ISnapshotPersisterGrain.cs) marks `PersistAsync` with Orleans `[OneWay]`. The [cache caller](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotCacheGrain.cs) discards its returned task after constructing an eligible envelope.

A successful cache read does not confirm completion of a newly requested background write. A cache hit loaded from snapshot storage already used a stored snapshot. Awaiting the implementation directly in a unit test exercises its storage await; that is a different observation boundary from the remote one-way call.

Orleans [one-way calls](https://learn.microsoft.com/en-us/dotnet/orleans/grains/oneway) provide no receipt or completion signal and can lose the message before the writer runs. Provider retries cannot cover a handoff the provider never receives; this path has no separate handoff retry queue.

- A null envelope throws `ArgumentNullException` before the write's metric/logging try/catch.
- A successful write records a successful persistence metric and completion log.
- A writer exception, including cancellation, records failure, logs the exception, and rethrows inside the persister implementation.

The persister has no retry loop or cancellation precheck of its own. Retry behavior and honoring the forwarded token belong to the chosen writer/provider. The method does not delete or prune older snapshots.

The [existing persister tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/SnapshotPersisterGrainTests.cs) cover activation key parsing and forwarding an envelope to the writer. They call the implementation directly and do not establish remote one-way delivery or recovery guarantees.

## Summary

The snapshot persister forwards an envelope under its activated version key and observes the writer's outcome locally. Cache completion and snapshot write completion remain separate.

## Next Steps

- Read [Snapshot Retention](./snapshot-retention.md) for which reconstructed versions request persistence.
- Read [Tributary Storage Providers](../storage-providers/index.md) for provider-owned storage behavior.
