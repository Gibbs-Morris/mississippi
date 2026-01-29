---
id: events
title: Events
sidebar_label: Events
sidebar_position: 5
description: Events use stable storage names to represent what happened in a brook.
---

# Events

## Overview

Events use stable storage names via
[`EventStorageNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs#L7-L105).

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`EventStorageNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs#L7-L105) | Defines storage names in the `{AppName}.{ModuleName}.{Name}.V{Version}` format with validation. |

## Example

Example from the Spring sample: [FundsDeposited](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Events/FundsDeposited.cs#L1-L20).

```csharp
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Orleans;

[EventStorageName("SPRING", "BANKING", "FUNDSDEPOSITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsDeposited")]
internal sealed record FundsDeposited
{
    [Id(0)]
    public decimal Amount { get; init; }
}
```

## Summary

- Events use
    [`EventStorageNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs#L7-L105)
    for stable storage naming.
- Storage names use uppercase alphanumeric components and versioned naming as validated by
    [`EventStorageNameAttribute`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs#L7-L105).

## Next Steps

- [Command Handlers](./command-handlers.md)
- [Aggregate Reducers](./aggregate-reducers.md)
- [Effects](./effects.md)
