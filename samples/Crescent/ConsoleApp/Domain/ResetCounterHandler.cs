using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for resetting a counter.
/// </summary>
internal sealed class ResetCounterHandler : ICommandHandler<ResetCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        ResetCounter command,
        CounterState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before resetting.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterReset
                {
                    NewValue = command.NewValue,
                    PreviousValue = state.Count,
                },
            });
    }
}
