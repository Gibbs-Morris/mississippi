using System.Collections.Generic;

using Crescent.ConsoleApp.Counter.Commands;
using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Counter.Handlers;

/// <summary>
///     Command handler for initializing a counter.
/// </summary>
internal sealed class InitializeCounterHandler
    : CommandHandler<InitializeCounter, CounterState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        InitializeCounter command,
        CounterState? state
    )
    {
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
