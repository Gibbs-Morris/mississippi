using Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.L2Tests.Domain.Counter.Handlers;

/// <summary>
///     Command handler for resetting a counter.
/// </summary>
internal sealed class ResetCounterHandler : CommandHandler<ResetCounter, CounterAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        ResetCounter command,
        CounterAggregate? state
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