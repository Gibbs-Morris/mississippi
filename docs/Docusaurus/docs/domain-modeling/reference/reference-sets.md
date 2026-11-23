---
title: Native Reference Sets
description: Reference the source-typed membership contracts, limits, replay and registration of native unordered reference sets.
sidebar_position: 17
---

# Native Reference Sets

A native reference set stores unique source IDs and their versions observed at addition inside a named aggregate, using ordinary commands, membership events and immutable replay.

## Applies to

- `Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet`: command contracts, fixed limits and `State.ReferenceSetState<TSource>`.
- `Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations`: `AddReferenceSet<TAggregate, TSource, TAddCommand, TRemoveCommand>()`.
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

## Replay and compatibility

Replay applies saved membership facts without consulting sources, permissions or current capacity policy. Every reduction produces a new aggregate snapshot; earlier membership remains unchanged. JSON storage and Orleans transport serialize IDs and observed versions.

The internal event storage names are `MISSISSIPPI.REFERENCESET.ADDED.V1` and `MISSISSIPPI.REFERENCESET.REMOVED.V1`. Named aggregate brook/snapshot identities remain application-owned. Existing aggregate concurrency and empty-stream behavior remain unchanged.

Membership projections, bounded pages, client following, ordering/filtering and automatic cleanup of unavailable sources are outside this primitive.

## Next steps

- [Aggregate Keys](./aggregate-keys.md) describes collection entity identity.
- [Domain Operation Results](./operation-results.md) describes success and failure results.
- [Persisted Type Registries](./type-registries.md) describes event and snapshot identity.
