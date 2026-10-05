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

`ToString()` and the implicit conversion to `string` produce `brookName|entityId|version`. `FromBrookKey` and `FromCursorKey` construct that identity from the supplied key components and a version. `ToBrookKey` and `ToCursorKey` retain the name and entity ID while dropping the version.

The similarly named [`UxProjectionVersionedKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/UxProjectionVersionedKey.cs) is a separate entity/version value type. Its two-part encoding is not the input key accepted by the built-in versioned-cache factory overload.

## Constructor Constraints

- Null brook names or entity IDs throw `ArgumentNullException`.
- Empty or whitespace-only components, or components containing `|`, throw `ArgumentException`. Valid supplied text is not trimmed.
- `BrookPosition.NotSet`, with value `-1`, throws `ArgumentOutOfRangeException`. Version zero is valid.
- The combined name, entity ID, two separators, and invariant numeric version representation may contain at most 4192 UTF-16 code units; a longer constructed key throws `ArgumentException`.

[`BrookPosition`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookPosition.cs) itself rejects values below `-1`. A key's valid version identifies a requested position; construction does not verify that storage has that position.

## Parsing

`Parse` requires exactly three pipe-separated parts. A null input throws `ArgumentNullException`; a wrong part count or invalid `long` version throws `FormatException`. Numeric parsing uses `NumberStyles.Integer` with invariant culture, allowing a sign and surrounding numeric whitespace.

After numeric parsing, it calls the normal constructors. Empty components, negative versions, and excessive constructed length therefore produce argument exceptions rather than format exceptions. Length validation uses the reconstructed numeric representation, rather than the raw numeric input's leading zeros or whitespace.

`TryParse` returns false for null or empty input, wrong part counts, invalid numbers, or constructor argument failures. On failure its output is the default struct.

The default struct bypasses component validation: its name and entity ID are null, and its version field has the zero-initialized value `0`. Its encoded form `||0` fails normal parsing because the components are empty.

## Factory Routing

The [factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrainFactory.cs) accepts either this key or an entity ID plus version. The entity/version overload derives the brook name from `TProjection`'s `[BrookName]` attribute before constructing the key; a missing attribute throws `InvalidOperationException` before grain resolution. The key overload does not read that attribute or compare it with `key.BrookName`. Activation uses the supplied key name for snapshot stream identity, so callers constructing keys must select the intended brook.

The [projection runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrain.cs) uses this three-part identity for versioned reads. Latest projection grains use the entity ID as their primary key. Versioned caches are typed by `TProjection`, so the same encoded string does not erase the projection type's grain identity.

Activation of the [versioned cache](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionVersionedCacheGrain.cs) also derives snapshot storage identity from `TProjection`'s `[SnapshotStorageName]` attribute. A missing attribute throws `InvalidOperationException`; `[BrookName]` alone does not make this cache path usable.

The [cache-key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/UxProjectionVersionedCacheKeyTests.cs) cover validation, parsing, conversions, zero versions, and string encoding. The [factory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/UxProjectionGrainFactoryTests.cs) verify routing.

## Summary

The active versioned cache key combines brook, entity, and version under a typed projection grain. Parsing reuses constructor validation, while a valid identity alone does not establish available projection state.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md) for projection ownership.
- Read [Tributary Cosmos Storage](../../tributary/storage-providers/cosmos.md) for the separate snapshot identity and provider boundary.
