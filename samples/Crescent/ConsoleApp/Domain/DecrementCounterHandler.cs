using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for decrementing a counter.
/// </summary>
internal sealed class DecrementCounterHandler : ICommandHandler<DecrementCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        DecrementCounter command,
        CounterState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before decrementing.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Decrement amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterDecremented
                {
                    Amount = command.Amount,
                },
            });
    }
}