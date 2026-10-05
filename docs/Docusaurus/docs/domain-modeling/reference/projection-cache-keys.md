---
id: projection-cache-keys
title: Projection Cache Keys
description: Reference the active versioned projection cache identity, parser constraints, and factory routing.
sidebar_position: 8
sidebar_label: Projection Cache Keys
---

# Projection Cache Keys

## Overview

`UxProjectionVersionedCacheKey` identifies a versioned projection cache using a brook name, entity ID, and brook position. The projection type is supplied separately as the grain factory's generic type argument.

## Applies To

- `Mississippi.DomainModeling.Abstractions.UxProjectionVersionedCacheKey`
- `IUxProjectionGrainFactory.GetUxProjectionVersionedCacheGrain<TProjection>`

## Fields And Encoding

The [readonly record struct](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/UxProjectionVersionedCacheKey.cs) stores read-only `BrookName`, `EntityId`, and `Version` properties. `Version` is a `BrookPosition`.

`ToString()` and the implicit conversion to `string` produce `brookName|entityId|version`. `FromBrookKey` and `FromCursorKey` construct that identity from the supplied components and version, rerunning the destination checks below. A valid `BrookKey` with empty or whitespace-only components is rejected with `ArgumentException`; adding the separator/version can also exceed the destination length limit. `ToBrookKey` and `ToCursorKey` drop the version and rerun their destination constructors. On a default cache key, its null components make those conversions throw `ArgumentNullException`.

The similarly named [`UxProjectionVersionedKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/UxProjectionVersionedKey.cs) is a separate entity/version value type. Its two-part encoding is not the input key accepted by the built-in versioned-cache factory overload.

## Constructor Constraints

- Null brook names or entity IDs throw `ArgumentNullException`.
- Empty or whitespace-only components, or components containing `|`, throw `ArgumentException`. Valid supplied text is not trimmed.
- `BrookPosition.NotSet`, with value `-1`, throws `ArgumentOutOfRangeException`. Version zero is valid.
- The combined name, entity ID, two separators, and invariant numeric version representation may contain at most 4192 UTF-16 code units; a longer constructed key throws `ArgumentException`.

[`BrookPosition`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookPosition.cs) itself rejects values below `-1`. A key's valid version identifies a requested position; construction does not verify that storage has that position. If no usable exact snapshot exists, the built-in replay route reads through the requested position. Its [bounded slice reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs) throws `InvalidOperationException` when storage ends earlier, failing cache activation rather than substituting the latest state or a nullable missing result.

Versioned-cache activation derives a [`SnapshotStreamKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/SnapshotStreamKey.cs) with a separate limit: `brookName.Length + snapshotStorageName.Length + entityId.Length + reducersHash.Length + 3 <= 4192`. The built-in root reducer hash has 64 characters. Even a constructible cache key can therefore fail activation with `ArgumentException` when that downstream identity is too long.

## Parsing

`Parse` requires exactly three pipe-separated parts. A null input throws `ArgumentNullException`; a wrong part count or invalid `long` version throws `FormatException`. Numeric parsing uses `NumberStyles.Integer` with invariant culture, allowing a sign and surrounding numeric whitespace.

After numeric parsing, it calls the normal constructors. Empty components, negative versions, and excessive constructed length therefore produce argument exceptions rather than format exceptions. Length validation uses the reconstructed numeric representation, rather than the raw numeric input's leading zeros or whitespace.

`TryParse` returns false for null or empty input, wrong part counts, invalid numbers, or constructor argument failures. On failure its output is the default struct.

The default struct bypasses component validation: its name and entity ID are null, and its version field has the zero-initialized value `0`. Its encoded form `||0` fails normal parsing because the components are empty.

## Factory Routing

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrainFactory.cs) accepts either this key or an entity ID plus version. The entity/version overload derives the brook name from `TProjection`'s `[BrookName]` attribute before constructing the key; a missing attribute throws `InvalidOperationException` before grain resolution. The key overload does not read that attribute or compare it with `key.BrookName`. Activation uses the supplied key name for snapshot stream identity, so callers constructing keys must select the intended brook.

The [projection runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrain.cs) uses this three-part identity for versioned reads. Latest projection grains use the entity ID as their primary key. Versioned caches are typed by `TProjection`, so the same encoded string does not erase the projection type's grain identity. That type/key pair identifies a logical grain, not one process-wide cache object. Typed grain identities do not isolate downstream snapshots: `SnapshotStreamKey` contains no projection CLR type, and `SnapshotStorageNameHelper` does not incorporate generic arguments. Closed constructions sharing brook name, entity ID, storage name, and reducer hash address the same snapshot stream and can read or overwrite incompatible snapshots. Give incompatible constructions distinct snapshot stream identities. This implementation is an Orleans [stateless worker](https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains), which can have several activations on one or several silos. Each activation loads and retains its own `cachedProjection`; callers cannot infer one shared in-memory instance or one activation load per key.

Activation of the [versioned cache](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionVersionedCacheGrain.cs) also derives snapshot storage identity from `TProjection`'s `[SnapshotStorageName]` attribute. That identity also includes `IRootReducer<TProjection>.GetReducerHash()`. Changing the deployed reducer-type set can select a different snapshot lineage on a later activation even with the same projection type and encoded cache key. A missing attribute throws `InvalidOperationException`; `[BrookName]` alone does not make this cache path usable. The built-in snapshot-cache route also needs a nonabstract projection class with a public parameterless constructor (`new()`); the factory constraints do not enforce that activation requirement. Activation also resolves `IRootReducer<TProjection>`, and the nested snapshot cache resolves `ISnapshotStateConverter<TProjection>`. Register the root/reducers and snapshot converter; `AddUxProjections()` supplies the projection grain factory only. The [generated `Add{Projection}Projection()` helper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime.Generators/ProjectionSiloRegistrationGenerator.cs) registers discovered reducers and the converter, while host-owned storage, serialization, and snapshot infrastructure still need composition. Call [`AddSnapshotCaching()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotRegistrations.cs), or supply equivalent services, for the shared `ISnapshotGrainFactory` and retention options used by this route. The generated per-projection helper does not register them. The nested snapshot cache also resolves `IBrookEventConverter`. [`AddAggregateSupport()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/AggregateRegistrations.cs) supplies its default converter and event registry; alternatively compose equivalent services and populate the replay-event mappings explicitly.

The [cache-key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/UxProjectionVersionedCacheKeyTests.cs) cover validation, parsing, conversions, zero versions, and string encoding. The [factory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/UxProjectionGrainFactoryTests.cs) verify routing.

When an exact snapshot cannot be used, successful reconstruction can request a background write. `SnapshotCacheGrain` serializes and sends one-way `PersistAsync` only for retention-selected versions or when `ShouldPersistAllSnapshots` is enabled. A versioned read can therefore need snapshot writer permissions; its success does not confirm that the background write completed.

## Summary

The active versioned cache key combines brook, entity, and version under a typed projection grain. Parsing reuses constructor validation, while a valid identity alone does not establish available projection state.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for projection ownership.
- Read [Tributary Cosmos Storage](../../tributary/storage-providers/cosmos.md) for the separate snapshot identity and provider boundary.
