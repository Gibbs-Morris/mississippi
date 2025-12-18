using System.Collections.Generic;

using Crescent.ConsoleApp.Counter.Commands;
using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Counter.Handlers;

/// <summary>
///     Command handler for decrementing a counter.
/// </summary>
internal sealed class DecrementCounterHandler : CommandHandler<DecrementCounter, CounterState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DecrementCounter command,
        CounterState? state
    )
    {
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