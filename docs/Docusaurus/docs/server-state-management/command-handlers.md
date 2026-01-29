---
id: command-handlers
title: Command Handlers
sidebar_label: Command Handlers
sidebar_position: 4
description: Command handlers validate commands against state and return event results.
---

# Command Handlers

## Overview

Command handlers process commands against aggregate state using
[`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65)
and return results via
[`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195).

## Key Contracts

| Contract | Purpose |
| --- | --- |
| [`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65) | Provides `Handle`, `TryHandle`, and the `HandleCore` override point. |
| [`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195) | Carries success values or error information. |
| [`AggregateErrorCodes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/AggregateErrorCodes.cs#L1-L40) | Standard error codes for aggregate failures. |

## Example

Example from the Spring sample: [DepositFundsHandler](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/Handlers/DepositFundsHandler.cs#L1-L46).

```csharp
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;

internal sealed class DepositFundsHandler : CommandHandlerBase<DepositFunds, BankAccountAggregate>
{
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DepositFunds command,
        BankAccountAggregate? state
    )
    {
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open before depositing funds.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Deposit amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new FundsDeposited
                {
                    Amount = command.Amount,
                },
            });
    }
}
```

## Error codes

Values are defined in
[`AggregateErrorCodes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/AggregateErrorCodes.cs#L1-L40).

| Code | Value |
| --- | --- |
| `AlreadyExists` | `AGGREGATE_ALREADY_EXISTS` |
| `CommandHandlerNotFound` | `COMMAND_HANDLER_NOT_FOUND` |
| `ConcurrencyConflict` | `CONCURRENCY_CONFLICT` |
| `InvalidCommand` | `INVALID_COMMAND` |
| `InvalidState` | `INVALID_STATE` |
| `NotFound` | `AGGREGATE_NOT_FOUND` |

## Summary

- Command handlers translate commands into events or error results via
    [`CommandHandlerBase<TCommand, TSnapshot>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/CommandHandlerBase.cs#L7-L65) and
    [`OperationResult<T>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/OperationResult.cs#L9-L195).
- Aggregate error codes are defined by
    [`AggregateErrorCodes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates.Abstractions/AggregateErrorCodes.cs#L1-L40).

## Next Steps

- [Commands](./commands.md)
- [Events](./events.md)
- [Aggregate Reducers](./aggregate-reducers.md)
