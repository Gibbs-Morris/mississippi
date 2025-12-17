using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for resetting a counter.
/// </summary>
internal sealed class ResetCounterHandler : CommandHandler<ResetCounter, CounterState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        ResetCounter command,
        CounterState? state
    )
    {
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