---
id: commands
title: Commands
sidebar_label: Commands
sidebar_position: 3
description: Commands are request records that command handlers validate and translate into events.
---

# Commands

## Overview

Commands are request records that command handlers validate and translate into events using
[`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65)
and return results via
[`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195).

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65) | Base class that validates commands in `HandleCore`. |
| [`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195) | Result container for success values or error details. |

## Example

Example from the Spring sample: [FlagTransaction](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/TransactionInvestigationQueue/Commands/FlagTransaction.cs#L1-L41).

```csharp
using System;
using Orleans;

[GenerateSerializer]
[Alias("Spring.Domain.TransactionInvestigationQueue.Commands.FlagTransaction")]
public sealed record FlagTransaction
{
    [Id(0)]
    public string AccountId { get; init; } = string.Empty;

    [Id(1)]
    public decimal Amount { get; init; }

    [Id(2)]
    public DateTimeOffset Timestamp { get; init; }
}
```

## Summary

- Commands are request records consumed by command handlers defined by
    [`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65).
- Command handlers return results via
    [`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195).

## Next Steps

- [Aggregates](./aggregates.md)
- [Command Handlers](./command-handlers.md)
- [Events](./events.md)
