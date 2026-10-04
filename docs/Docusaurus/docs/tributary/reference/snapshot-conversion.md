---
title: Snapshot Conversion
description: Reference snapshot state conversion, envelope metadata, and serializer failure boundaries.
sidebar_position: 3
---

# Snapshot Conversion

`ISnapshotStateConverter<TSnapshot>` converts state to a `SnapshotEnvelope` and back. The built-in converter uses the configured `ISerializationProvider` for synchronous payload encoding.

## Applies To

- `Mississippi.Tributary.Abstractions.ISnapshotStateConverter<TSnapshot>`
- `Mississippi.Tributary.Abstractions.SnapshotEnvelope`
- The built-in `SnapshotStateConverter<TSnapshot>` implementation

## Methods And Envelope Fields

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/ISnapshotStateConverter.cs) exposes `ToEnvelope(state, reducerHash)` and `FromEnvelope(envelope)`. These methods have no cancellation token and do not read or write snapshot storage.

[`ToEnvelope`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotStateConverter.cs) calls the provider's `Serialize` method and creates these fields:

| Field | Value assigned by the converter |
| --- | --- |
| `Data` | A copy of the serialized bytes in an immutable array. |
| `DataContentType` | The provider's `Format` string. |
| `ReducerHash` | The caller-supplied hash, unchanged. |
| `DataSizeBytes` | The serialized byte count, stored as a `long`. |

The converter does not compute the hash. It also does not enforce MIME syntax for the format label. For example, the built-in JSON provider uses `System.Text.Json` as its format identifier.

## Reading An Envelope

`FromEnvelope` passes `Data.AsMemory()` to the configured provider's `Deserialize<TSnapshot>` method. It does not choose a provider from `DataContentType`, compare `ReducerHash`, or verify `DataSizeBytes` against the payload length.

The [snapshot cache caller](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotCacheGrain.cs) checks that a stored envelope's hash is nonempty and equals the current root reducer hash before using the converter. That compatibility check belongs to the cache's load path.

The [envelope record](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/SnapshotEnvelope.cs) defaults to empty data, empty format and hash strings, and size zero. Its properties can be initialized independently; constructing the record does not validate consistency between those fields.

## Failures

- `ToEnvelope` rejects null state and a null reducer hash with `ArgumentNullException`; an empty hash is accepted by this converter.
- `FromEnvelope` rejects a null envelope with `ArgumentNullException`.
- Provider serialization and deserialization exceptions propagate to the caller.

The [converter tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs) cover payload conversion, format/hash assignment, and the method null guards. They use a mocked serialization provider rather than proving compatibility between different formats.

## Summary

Snapshot conversion copies encoded state and records metadata. The configured provider owns payload decoding, while the cache caller owns the reducer-hash compatibility decision.

## Next Steps

- Read [Snapshot Retention](./snapshot-retention.md) for checkpoint selection and persistence eligibility.
- Read [Tributary Storage Providers](../storage-providers/index.md) for storage contracts and host composition.
