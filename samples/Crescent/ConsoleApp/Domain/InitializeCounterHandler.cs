using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for initializing a counter.
/// </summary>
internal sealed class InitializeCounterHandler : ICommandHandler<InitializeCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        InitializeCounter command,
        CounterState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        // Prevent re-initialization
        if (state?.IsInitialized == true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.AlreadyExists,
                "Counter is already initialized.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterInitialized
                {
                    InitialValue = command.InitialValue,
                },
            });
    }
}
