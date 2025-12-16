using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for incrementing a counter.
/// </summary>
internal sealed class IncrementCounterHandler : ICommandHandler<IncrementCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        IncrementCounter command,
        CounterState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before incrementing.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Increment amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterIncremented
                {
                    Amount = command.Amount,
                },
            });
    }
}
