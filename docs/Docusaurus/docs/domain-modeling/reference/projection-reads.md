---
id: projection-reads
title: Projection Reads
description: Reference latest and explicitly versioned UX projection reads, empty results, and cancellation boundaries.
sidebar_position: 10
sidebar_label: Projection Reads
---

# Projection Reads

## Overview

`IUxProjectionGrain<TProjection>` returns projection state at a selected brook position. Its latest read selects the position known to a cursor, then fetches that specific version.

## Applies To

- `Mississippi.DomainModeling.Abstractions.IUxProjectionGrain<TProjection>`
- The built-in `UxProjectionGrain<TProjection>` implementation
- The built-in projection grains and constrained factory overloads require `TProjection : class`; the public grain interfaces themselves have no generic constraint. Their built-in snapshot-cache route additionally needs a nonabstract projection type with a public parameterless constructor (`new()`). Projection state and its object graph also need compatible Orleans serialization for grain copying and cross-silo calls. Follow the repository state contract with `[GenerateSerializer]`, a stable `[Alias]`, and stable numbered `[Id]` members; registration alone does not provide codecs for arbitrary payload types.

## Read Methods

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/IUxProjectionGrain.cs) exposes three methods, each with an optional cancellation token:

| Method | Result and selection |
| --- | --- |
| `GetAsync` | `ValueTask<TProjection?>`; selects the cursor's known position, then reads that version. |
| `GetAtVersionAsync` | `ValueTask<TProjection?>`; reads the supplied `BrookPosition` directly. |
| `GetLatestVersionAsync` | `ValueTask<BrookPosition>`; returns the cached storage or accepted-notification position without fetching projection state or verifying current storage. |

The [implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionGrain.cs) obtains the brook name from `TProjection`'s `BrookNameAttribute`. Activation validates that attribute and reads the entity ID from the grain's string key. A missing attribute throws `InvalidOperationException`.

The [versioned cache](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionVersionedCacheGrain.cs) also needs `[SnapshotStorageName]`; a missing storage-name attribute throws `InvalidOperationException` during activation. Routed keys require nonblank, pipe-free entity IDs and a combined encoded length of at most 4192 UTF-16 code units.

Register the per-projection `IRootReducer<TProjection>` and `ISnapshotStateConverter<TProjection>` services used during activation. The built-in [`AddReducer`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/ReducerRegistrations.cs) overloads add the root reducer; [`AddSnapshotStateConverter<TProjection>()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotRegistrations.cs) adds the converter. [`AddUxProjections()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionRegistrations.cs) adds only the projection grain factory, so it does not satisfy these services alone. Missing services can prevent cache activation. The route also needs [`AddSnapshotCaching()`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotRegistrations.cs), or equivalent snapshot-grain factory and retention-policy registrations. Per-projection reducer and converter registrations alone do not supply that shared infrastructure. Use [Runtime Composition](../../reference/runtime-composition.md) for the host-owned providers and runtime infrastructure.

## Latest And Explicit Versions

`GetLatestVersionAsync` resolves the cursor by brook name and entity ID, then calls `GetPositionAsync`. An already-active cursor returns its in-memory position without a fresh storage query; first activation reads storage before subscribing. The method's cancellation token is currently reserved and unused.

Activation has no atomic read-and-subscribe handoff or second storage read. If a provider does not replay an update published between the initial read and subscription, the cursor can retain the older position until a later accepted notification or reactivation reloads storage. Latest reads report a cached position, which can be behind or ahead of persisted progress: cursor publications are not checked against storage.

`GetAtVersionAsync` constructs a versioned cache key from brook name, entity ID, and the requested position. It resolves the cache for `TProjection`, forwards the token to its `GetAsync`, and returns the resulting state. This event position does not pin reducer code. Each cache activation resolves its deployed root reducer and hash; differing reducer deployments can produce different state at the same event position.

The built-in cache's `GetAsync` ignores that caller token. Its state load occurs during activation using a separate activation token, so forwarding the method argument does not cancel that load.

`GetAsync` first calls `GetLatestVersionAsync`, then calls `GetAtVersionAsync` for that selected position. From this call sequence, the cursor can advance between selection and retrieval; the method does not loop until it observes every subsequent advance.

The returned value contains state without a paired version. Separate calls for state and latest version therefore do not form an atomic state/version result.

## Empty Results And Failures

These null outcomes describe the built-in class-constrained implementation. The unconstrained public interface exposes `TProjection?`; that is not a promise of null for every value-type implementation. A custom implementation owns its empty-result behavior.

- A latest position of `-1` (`BrookPosition.NotSet`) makes `GetAsync` return null without resolving a versioned cache.
- An explicitly requested `NotSet` position makes `GetAtVersionAsync` return null before constructing a cache key.
- Position `0` is a valid version and follows the normal cache route. A requested position beyond available events, including zero for an empty brook, can fail with `InvalidOperationException` from a slice read instead of returning null.
- Other key-construction, factory, and cache exceptions propagate; these methods do not convert every failure into null.

Passing a canceled token does not itself add a cancellation check to the built-in cache read, cursor lookup, or early null-return paths.

Reading can trigger snapshot reconstruction and retention-dependent background persistence. An exact snapshot hit requests no new write, and reconstructed versions are persisted only when selected by retention. Read permissions cover retrieval; write permissions are needed when requested background persistence is expected to succeed. Successful retrieval does not await that new write's storage result. See the [snapshot cache implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/SnapshotCacheGrain.cs).

The [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/UxProjectionGrainTests.cs) cover direct version routing, latest-position delegation, a changed cursor position on a later read, and skipping cache resolution for `NotSet`.

## Summary

Use `GetAsync` for the cursor's currently known version and `GetAtVersionAsync` when a particular version is required. A latest-version read returns that cached position separately from retrieving projection state.

## Next Steps

- Read [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the wider read flow.
- Read [Tributary Concepts](../../tributary/concepts/concepts.md) for the reconstruction layer.
- Read [Projection Cursors](./projection-cursors.md) for shared identity and known-position filtering.
- Read [Projection Cache Keys](./projection-cache-keys.md) for versioned identity and activation prerequisites.
