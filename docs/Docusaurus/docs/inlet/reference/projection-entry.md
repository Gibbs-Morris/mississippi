---
id: projection-entry
title: Client Projection Entries
description: Reference projection-entry defaults, lookup keys, and the fields changed by client projection actions.
sidebar_position: 7
sidebar_label: Client Projection Entries
---

# Client Projection Entries

## Overview

`ProjectionsFeatureState` stores a separate `ProjectionEntry<T>` for each projection type and entity ID. Read the entry's data, loading flag, connection flag, and error together: those fields change independently.

## Applies To

- `Mississippi.Inlet.Client.Abstractions.State.ProjectionEntry<T>`
- `ProjectionsFeatureState` and `ProjectionsReducer`
- Client projection actions dispatched into Reservoir

## Entry Defaults And Lookup

[`ProjectionEntry<T>.Empty`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/ProjectionEntry.cs) has null data, version `-1`, both flags false, and no error. The record requires a reference-type projection but does not validate consistency between its data, version, and flags.

| Property | Meaning |
|-------|---------|
| `Data` | The last assigned projection object, which can be null |
| `Version` | The last assigned version |
| `IsLoading` | Whether a loading action has set the flag without a later loaded, updated, or error action clearing it |
| `IsConnected` | The value supplied by the last connection-change action |
| `Error` | The exception supplied by an error action, exposed as `ErrorException` through `IProjectionState<T>` |

[`ProjectionsFeatureState`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/ProjectionsFeatureState.cs) uses feature key `projections`. Its lookup key combines the projection's CLR full name, a colon, and the entity ID. Types with different full names occupy separate entries for one entity. Assembly identity is not included, so types from different assemblies with the same full name can collide.

`GetEntry<T>` returns null when the key is absent or its stored object is not a `ProjectionEntry<T>`. The convenience getters then return null data/error, version `-1`, and false flags. These defaults describe an absent typed entry; a present entry can also contain null data.

`WithEntryTransform<T>` rejects a null entity ID or transform and supplies `Empty` when no typed entry exists. Updating the immutable dictionary creates a new state, but it does not clone the projection object stored in `Data`.

Those null inputs throw `ArgumentNullException`. Every `Reduce...` method also rejects null state or action with `ArgumentNullException` before applying a transition.

## Action Transitions

`ProjectionLoadingAction<T>`, `ProjectionErrorAction<T>`, `ProjectionLoadedAction<T>`, `ProjectionUpdatedAction<T>`, and `ProjectionConnectionChangedAction<T>` constructors reject a null entity ID with `ArgumentNullException`; empty IDs are accepted. The error-action constructor also rejects a null error. These constructor guards apply before the reducer transitions below.

The [reducers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/Reducers/ProjectionsReducer.cs) preserve every field except those listed below:

- Loading sets `IsLoading` to true and clears the error. Existing data, version, and connection state remain available.
- Error stores the action's exception and clears `IsLoading`. Existing data, version, and connection state remain available.
- Loaded or updated replaces data and version, clears `IsLoading`, and clears the error. It preserves the connection flag.
- Connection changed replaces only `IsConnected`.

Loaded and updated actions assign the supplied version directly; the reducers do not reject an older version. They also allow null data with a supplied version. A loading or error entry can retain earlier data, and a loaded entry need not have its connection flag set.

The [state tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.Abstractions.L0Tests/State/ProjectionsFeatureStateTests.cs) cover empty entries, typed lookup, and immutable updates. The [reducer tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/Reducers/ProjectionsReducerTests.cs) cover loading/error flags, null loaded/updated data, separate entity entries, and original-state preservation. The full retention rules above come from the reducer assignments, rather than assertions for every retained property.

## Summary

Projection entries keep the latest assigned data alongside independent loading, connection, and error observations. Use the individual fields to decide what to render; null data or a connection flag alone does not describe the whole entry.

## Next Steps

- Read [Inlet Reference](./reference.md) for client registration surfaces.
- Read [Reservoir State Flow](../../reservoir/concepts/state-flow.md) for action-driven state updates.
