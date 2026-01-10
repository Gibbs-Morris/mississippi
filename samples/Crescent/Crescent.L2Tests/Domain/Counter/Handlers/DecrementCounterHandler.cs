using Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.L2Tests.Domain.Counter.Handlers;

/// <summary>
///     Command handler for decrementing a counter.
/// </summary>
internal sealed class DecrementCounterHandler : CommandHandler<DecrementCounter, CounterAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DecrementCounter command,
        CounterAggregate? state
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