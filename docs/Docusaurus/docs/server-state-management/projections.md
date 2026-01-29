---
id: projections
title: Projections
sidebar_label: Projections
sidebar_position: 8
description: Projections are read-optimized records updated by projection reducers.
---

# Projections

## Overview

Projections are read-optimized records updated by projection reducers that use
[`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).
See the Spring sample projection:
[`BankAccountBalanceProjection`](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/BankAccountBalanceProjection.cs#L10-L43).

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57) | Base class for reducers applied to projection state. |
| [`BrookNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs#L7-L73) | Brook naming with validation. |
| [`SnapshotStorageNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameAttribute.cs#L7-L105) | Snapshot storage naming with validation. |

## Example

Example from the Spring sample: [BankAccountBalanceProjection](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/BankAccountBalanceProjection.cs#L10-L43).

```csharp
using Orleans;

public sealed record BankAccountBalanceProjection
{
    [Id(0)]
    public decimal Balance { get; init; }

    [Id(1)]
    public string HolderName { get; init; } = string.Empty;

    [Id(2)]
    public bool IsOpen { get; init; }
}
```

## Summary

- Projections provide read-optimized state derived from events as shown by
    [`BankAccountBalanceProjection`](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/BankAccountBalanceProjection.cs#L10-L43).
- Projection reducers apply events with
    [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).

## Next Steps

- [Effects](./effects.md)
- [Projection Reducers](./projection-reducers.md)
- [Snapshots](./snapshots.md)
```
