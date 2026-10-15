---
title: Native Reference Sets
description: Reference the source-typed membership contracts, limits, replay and registration of native unordered reference sets.
sidebar_position: 17
---

# Native Reference Sets

A native reference set stores unique source IDs and their versions observed at addition inside a named aggregate, with optional named read-only projections using the same membership events and immutable replay.

## Applies to

- `Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet`: command contracts, fixed limits and `State.ReferenceSetState<TSource>`.
- `Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations`: `AddReferenceSet<TAggregate, TSource, TAddCommand, TRemoveCommand>()` and `AddReferenceSetProjection<TProjection, TSource>()`.
- Existing aggregate, Brooks event-storage and Tributary snapshot infrastructure.

## Contract

Define a sealed aggregate record with the usual `BrookName`, `SnapshotStorageName`, `GenerateSerializer` and `Alias` attributes. Its membership property has type `ReferenceSetState<TSource>`, where `TSource` is the application's source projection type. No source object is persisted or queried.

Use positional command records for the currently verified generated consumer. Concrete add commands implement `IAddReference<TSource>`; remove commands implement `IRemoveReference<TSource>`. Declare serializable command properties directly on each record with `Id` attributes. Existing `GenerateCommand` consumers inspect declared properties; inherited properties and generator-to-generator discovery are not required.

| Input or property | Meaning |
| --- | --- |
| `SourceId` | Exact source identity, compared ordinally without trimming or case folding. |
| `ObservedSourceVersion` | Nonnegative `Int64` source version supplied on addition. A loading floor for later integration, not a pinned historical join. |
| `Members` | Immutable dictionary from source ID to its version observed at addition. Enumeration order has no contract. |
| `SourceType` | Source projection type metadata; excluded from membership persistence. |

## Defaults and constraints

An untouched collection has no aggregate snapshot. Registration callbacks must interpret a null aggregate as empty membership.

- Maximum membership: `ReferenceSetLimits.MaximumMembers = 1000`.
- Maximum ID length: `ReferenceSetLimits.MaximumSourceIdLength = 256` UTF-16 code units.
- Null, empty and whitespace-only IDs are invalid. Accepted IDs retain leading/trailing whitespace and exact case.
- Observed source versions range from zero through `Int64.MaxValue`. The source's unset position `-1` is invalid.
- These bounds apply to this primitive and do not guarantee a payload size accepted by every storage provider.

## Behavior and errors

Inputs are validated before checking membership or a possible no-op.

| Command | Result | Persisted events |
| --- | --- | --- |
| Valid add of an absent ID below capacity | Stores the ID and supplied observed version. | One membership-added event. |
| Valid add of an existing ID, including at capacity | Keeps the original observed version. | None; succeeds. |
| Valid remove of an existing ID | Removes membership. | One membership-removed event. |
| Valid remove of an absent ID | Keeps membership unchanged. | None; succeeds. |
| Remove followed by a new add | Stores the newly supplied observed version. | One event for each state-changing command. |
| Invalid ID or negative source version | `AggregateErrorCodes.InvalidCommand`. | None. |
| Valid add of a new ID at capacity | `AggregateErrorCodes.InvalidState`. | None. |

Application policy owns source existence and permission checks. Applications must apply those checks explicitly before admitting commands; shared membership behavior performs no hidden source-grain reads. Source-only changes do not update membership versions or write collection events.

The normal aggregate path checks expected collection-stream versions before command dispatch, including commands that would otherwise be no-ops. An untouched collection stream remains at `-1` after an absent remove. Its first persisted membership event is at zero. Command retries have the stated current-membership no-op behavior; no exactly-once guarantee across intervening removes and additions is added.

## Registration example

This excerpt matches the compiled `SavedItemsAggregate` and `PinnedItemsAggregate` consumers in `tests/DomainModeling.Runtime.L0Tests`. Each named record contains a `References` property. The same concrete commands and shared decision/replay algorithms can serve both named collections.

```csharp
services.AddReferenceSet<SavedItemsAggregate, CatalogItemProjection,
    AddCatalogItem, RemoveCatalogItem>(
    state => state?.References ?? new(),
    (state, references) => (state ?? new()) with { References = references });
```

The extension registers event identities, command handlers, reducers, the named snapshot type and its state converter. Supply the usual serialization/storage providers and runtime hosting separately. The read and write callbacks must be pure; the write callback returns a new aggregate record. Send the concrete command through `IGenericAggregateGrain<TAggregate>.ExecuteAsync`.

## Read-only membership projection

Define a sealed projection record containing `ReferenceSetState<TSource>` and the usual serialization attributes. The projection must have a public parameterless constructor; the helper enforces the `class, new()` constraint. Its `BrookName` must match the collection aggregate so it reads that collection's events. Give it a distinct, stable `SnapshotStorageName`; aggregate and projection snapshots have separate identities.

This excerpt matches the compiled `SavedItemsMembershipProjection` consumer in `tests/DomainModeling.Runtime.L0Tests`. It uses the same `CatalogItemProjection` source type as `SavedItemsAggregate`:

```csharp
services.AddReferenceSetProjection<SavedItemsMembershipProjection,
    CatalogItemProjection>(
    state => state?.References ?? new(),
    (state, references) => (state ?? new()) with { References = references });
```

The helper registers the existing membership event types, named typed reducers, the projection snapshot type, its state converter and the UX projection grain factory. It installs no projection command handlers or effects. Supply the usual serialization/storage providers and runtime hosting separately. The callbacks are pure accessors and immutable composition; they do not query source objects.

Read through `IUxProjectionGrainFactory.GetUxProjectionGrain<SavedItemsMembershipProjection>(collectionId)`:

| Read | Result |
| --- | --- |
| `GetAsync()` | Membership at the version currently known to the collection cursor. |
| `GetLatestVersionAsync()` | Cached cursor position; it may lag or lead persisted progress. `NotSet` (`-1`) while no events are known. |
| `GetAtVersionAsync(new BrookPosition(version))` | Membership at the requested collection event position when its stored snapshot or replay is available. |
| Latest read with a `NotSet` cursor, or exact read at `-1` | Null; no event or version-zero snapshot is invented. |

Latest reads use the cached cursor; they do not query storage afresh or return an atomic state/version pair. Explicit versions beyond available events, including zero for an empty brook, can fail with `InvalidOperationException`. Key, storage and reconstruction failures propagate. Event positions do not pin reducer code: replay uses the deployed reducers. See [Projection Reads](./projection-reads.md) for the existing freshness, failure and snapshot-persistence boundaries.

Versions identify the collection's events, independently of the observed source versions stored in membership. Duplicate adds and missing removes append no event and therefore do not advance that version. Reads preserve the observed source version until removal and re-addition; later collection changes leave earlier projection state unchanged. Source-only changes append no collection event, refresh no membership fact, and require no source hydration.

Each named projection uses its own typed reducer and snapshot registrations. The compiled saved and pinned membership consumers read their respective collection brooks without sharing membership state. This helper returns the whole membership projection. Bounded runtime reads require the explicit opt-in below; HTTP/MCP exposure, client subscriptions/following and business ordering/filtering remain separate.

## Bounded runtime pages

Opt in with `AddReferenceSetProjectionPages<TProjection,TSource>()` from `Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages` in place of the ordinary membership registration. It composes that registration and adds `IReferenceSetPageReader<TProjection,TSource>`. The reader and result contracts are in `Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages`. The ordinary three-method UX grain contract and whole-projection reads remain available.

This excerpt is exercised by the compiled `ReferenceSetPageTests` consumer. Supply the usual runtime hosting, Orleans, serialization and storage services:

```csharp
services.AddReferenceSetProjectionPages<SavedItemsMembershipProjection,
    CatalogItemProjection>(
    state => state?.References ?? new(),
    (state, references) => (state ?? new()) with { References = references });

IReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection> reader =
    provider.GetRequiredService<
        IReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection>>();
ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> first =
    await reader.ReadAsync("collection", limit: 2);
ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> next =
    await reader.ReadAsync("collection", offset: 2, limit: 100, version: first.Version);
```

| Parameter or result | Contract |
| --- | --- |
| `offset` / `Offset` | Zero-based `Int32`, from zero through `Int32.MaxValue`; omitted means zero. |
| `limit` / `Limit` | From 1 through 100; omitted means 50. Invalid values are rejected rather than clamped. |
| `version` | Omitted/null captures the cached cursor once. A supplied `BrookPosition` pins that collection position. |
| `Version` | The captured or requested collection version used for all returned data and metadata. |
| `TotalCount` | Membership count at that version, before selection. |
| `References` | Bounded immutable `ReferenceSetMember` entries ordered by ordinal `SourceId`, retaining `ObservedSourceVersion`. |
| `ProjectionType` / `SourceType` | Computed type metadata; excluded from JSON, with no source instance or content. |

A valid offset beyond the end returns an empty array with the requested offset/limit, unchanged total and actual version. Untouched history returns an empty page with version `-1`; an exact request at `-1` also selects empty history. A collection emptied by removals retains its nonnegative event version.

A latest read does not refresh storage: its cursor may lag or lead persisted progress. It captures once and uses that exact immutable version even if writes occur during the read. Pin the returned version while traversing several pages to avoid shifting membership. New latest reads can shift after collection writes. Observed source versions remain loading floors, independently of collection versions.

Negative offsets, limits outside 1–100 and versions above the captured cursor throw `ArgumentOutOfRangeException`; `BrookPosition` rejects values below `-1`. Invalid entity keys, unavailable snapshots/replay, cancellation and storage errors propagate. A nonempty version whose projection is unavailable fails with `InvalidOperationException`. Positions pin membership events, not deployed reducer code.

The runtime page-cache activation uses the existing projection/version key and immutable whole-projection cache, including its snapshot/reducer identity. It loads membership inside the runtime and prepares an ordinal index once per activation. Each request derives a bounded result; no page variants are retained or prebuilt. Stateless workers can have multiple activations, so preparation is not globally once-only. Paging bounds the returned references, while internal membership and index costs remain proportional to the finite collection.

Page reads append no events and load no referenced sources. The application still owns tenant/entity authorization and source permissions; a reference grants no access. No HTTP/MCP adapter, client subscription, source following or business filtering/sorting is installed.

## Replay and compatibility

Replay applies saved membership facts without consulting sources, permissions or current capacity policy. Every reduction produces a new aggregate snapshot; earlier membership remains unchanged. JSON storage and Orleans transport serialize IDs and observed versions.

The internal event storage names are `MISSISSIPPI.REFERENCESET.ADDED.V1` and `MISSISSIPPI.REFERENCESET.REMOVED.V1`. Named aggregate brook/snapshot identities remain application-owned. Existing aggregate concurrency and empty-stream behavior remain unchanged.

Client following, business ordering/filtering and automatic cleanup of unavailable sources are outside this primitive.

## Next steps

- [Aggregate Keys](./aggregate-keys.md) describes collection entity identity.
- [Domain Operation Results](./operation-results.md) describes success and failure results.
- [Persisted Type Registries](./type-registries.md) describes event and snapshot identity.
