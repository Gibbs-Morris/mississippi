---
id: aggregate-reducers
title: Aggregate Reducers
sidebar_label: Aggregate Reducers
sidebar_position: 6
description: Aggregate reducers apply events to aggregate state using EventReducerBase.
---

# Aggregate Reducers

## Overview

Aggregate reducers apply events to aggregate state using
[`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).

```mermaid
flowchart LR
    E[Event] --> R[Reducer]
    R --> S[(Aggregate State)]

    style E fill:#50c878,color:#fff
    style R fill:#6c5ce7,color:#fff
    style S fill:#9b59b6,color:#fff
```

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57) | Base class for reducers and immutability enforcement. |

## Example

Example from the Spring sample: [FundsDepositedReducer](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Reducers/FundsDepositedReducer.cs#L1-L27).

```csharp
using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

internal sealed class FundsDepositedReducer : EventReducerBase<FundsDeposited, BankAccountAggregate>
{
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsDeposited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) + @event.Amount,
            DepositCount = (state?.DepositCount ?? 0) + 1,
        };
    }
}
```

## Summary

- Aggregate reducers apply events using `ReduceCore` as defined by
    [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).
- Reducers must return a new reference-type instance per
    [`EventReducerBase<TEvent, TProjection>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Reducers.Abstractions/EventReducerBase.cs#L6-L57).

## Next Steps

- [Events](./events.md)
- [Effects](./effects.md)
- [Projections](./projections.md)
