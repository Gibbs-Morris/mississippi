---
id: projection-reducers
title: Projection Reducers
sidebar_label: Projection Reducers
sidebar_position: 9
description: Projection reducers apply events to projection state using EventReducerBase.
---

# Projection Reducers

## Overview

Projection reducers apply events to projection state using
[`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57) | Base class for reducers with `Reduce`, `TryReduce`, and `ReduceCore`. |

## Example

Example from the Spring sample: [FundsDepositedLedgerReducer](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountLedger/Reducers/FundsDepositedLedgerReducer.cs#L1-L40).

```csharp
using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

internal sealed class FundsDepositedLedgerReducer : EventReducerBase<FundsDeposited, BankAccountLedgerProjection>
{
    protected override BankAccountLedgerProjection ReduceCore(
        BankAccountLedgerProjection state,
        FundsDeposited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        long newSequence = state.CurrentSequence + 1;
        LedgerEntry entry = new()
        {
            EntryType = LedgerEntryType.Deposit,
            Amount = eventData.Amount,
            Sequence = newSequence,
        };
        ImmutableArray<LedgerEntry> entries = state.Entries.Prepend(entry)
            .Take(BankAccountLedgerProjection.MaxEntries)
            .ToImmutableArray();
        return state with
        {
            Entries = entries,
            CurrentSequence = newSequence,
        };
    }
}
```

## Summary

- Projection reducers apply events using `ReduceCore` as defined by
    [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).
- `TryReduce` returns false when the event type does not match the reducer generic parameter per
    [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).

## Next Steps

- [Projections](./projections.md)
- [Snapshots](./snapshots.md)
- [Domain Modeling](./domain-modeling.md)
