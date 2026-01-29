---
id: domain-definitions
title: Domain Definitions
sidebar_label: Domain Definitions
sidebar_position: 2
description: How to define aggregates, commands, and projections for Inlet code generation.
---

# Domain Definitions

## Overview

Inlet generates infrastructure code from aggregates, commands, and projections defined in a shared domain project. The Spring sample provides a concrete reference for how these definitions are organized and attributed.

## Project structure (Spring sample)

The Spring sample organizes its domain definitions as follows:

```text
Spring.Domain/
├── Aggregates/
│   └── BankAccount/
│       ├── BankAccountAggregate.cs
│       ├── Commands/
│       │   ├── OpenAccount.cs
│       │   ├── DepositFunds.cs
│       │   └── WithdrawFunds.cs
│       ├── Events/
│       │   ├── AccountOpened.cs
│       │   ├── FundsDeposited.cs
│       │   └── FundsWithdrawn.cs
│       ├── Handlers/
│       │   └── BankAccountHandlers.cs
│       └── Reducers/
│           ├── AccountOpenedReducer.cs
│           ├── FundsDepositedReducer.cs
│           └── FundsWithdrawnReducer.cs
├── Projections/
│   ├── BankAccountBalance/
│   │   ├── BankAccountBalanceProjection.cs
│   │   └── Reducers/
│   │       ├── AccountOpenedBalanceReducer.cs
│   │       ├── FundsDepositedBalanceReducer.cs
│   │       └── FundsWithdrawnBalanceReducer.cs
│   ├── BankAccountLedger/
│   └── FlaggedTransactions/
└── Spring.Domain.csproj
```

([Spring.Domain](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Domain))

## Aggregates

Aggregates represent write-side state for event-sourced entities. Inlet generators look for `[GenerateAggregateEndpoints]` on aggregate types.

### Basic Aggregate Definition

```csharp
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Aggregates.BankAccount;

[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.BankAccountAggregate")]
public sealed record BankAccountAggregate
{
    [Id(0)]
    public decimal Balance { get; init; }

    [Id(1)]
    public bool IsOpen { get; init; }

    [Id(2)]
    public string HolderName { get; init; } = string.Empty;

    [Id(3)]
    public int DepositCount { get; init; }

    [Id(4)]
    public int WithdrawalCount { get; init; }
}
```

([BankAccountAggregate.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs))

The aggregate attributes used in the Spring sample include:

- [BrookNameAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs)
- [SnapshotStorageNameAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameAttribute.cs)
- [GenerateAggregateEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs)

## Commands

Commands represent intentions to change aggregate state. Generators look for commands in a `Commands` sub-namespace of an aggregate decorated with `[GenerateAggregateEndpoints]`.

### Basic Command Definition

```csharp
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Aggregates.BankAccount.Commands;

[GenerateCommand(Route = "deposit")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DepositFunds")]
public sealed record DepositFunds
{
    [Id(0)]
    public decimal Amount { get; init; }
}
```

([DepositFunds.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Commands/DepositFunds.cs))

## Projections

Projections are read-optimized views for UI consumption. Inlet uses `[ProjectionPath]` and `[GenerateProjectionEndpoints]` to drive projection endpoint and DTO generation.

### Basic Projection Definition

```csharp
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;
using Orleans;

namespace Spring.Domain.Projections.BankAccountBalance;

[ProjectionPath("bank-account-balance")]
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTBALANCE")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection")]
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

([BankAccountBalanceProjection.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/BankAccountBalanceProjection.cs))

## Reducers

Reducers transform state when events occur. The Spring sample uses `EventReducerBase<TEvent, TState>` to implement reducers for both aggregates and projections.

### Aggregate Reducer

```csharp
using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

namespace Spring.Domain.Aggregates.BankAccount.Reducers;

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

([FundsDepositedReducer.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Reducers/FundsDepositedReducer.cs))

### Projection Reducer

```csharp
using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

namespace Spring.Domain.Projections.BankAccountBalance.Reducers;

internal sealed class FundsDepositedBalanceReducer : EventReducerBase<FundsDeposited, BankAccountBalanceProjection>
{
    protected override BankAccountBalanceProjection ReduceCore(
        BankAccountBalanceProjection state,
        FundsDeposited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Balance = state.Balance + eventData.Amount,
        };
    }
}
```

([FundsDepositedBalanceReducer.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/Reducers/FundsDepositedBalanceReducer.cs))

## Projection path rules

`ProjectionPathAttribute` defines the path used for HTTP routes and SignalR subscriptions. The attribute documents a `{feature}/{module}` path convention and provides helpers to form entity paths.

```csharp
[ProjectionPath("bank-account-balance")]
public sealed record BankAccountBalanceProjection { }
```

([ProjectionPathAttribute.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Abstractions/ProjectionPathAttribute.cs))

## Domain project references

Spring.Domain references the Event Sourcing abstraction projects and Inlet abstractions via project references:

([Spring.Domain.csproj](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Spring.Domain.csproj))

## Summary

- Define aggregates, commands, and projections in a shared domain project.
- Attribute aggregates and projections to drive code generation.
- Use reducers to transform state from events in both aggregates and projections.

## Next Steps

- [Attributes](./attributes.md) — Complete reference for all Inlet attributes
- [Source Generation](./source-generation.md) — What code gets generated from these definitions

