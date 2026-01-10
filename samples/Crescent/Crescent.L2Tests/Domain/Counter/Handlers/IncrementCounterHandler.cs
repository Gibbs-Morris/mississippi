using Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.L2Tests.Domain.Counter.Handlers;

/// <summary>
///     Command handler for incrementing a counter.
/// </summary>
internal sealed class IncrementCounterHandler : CommandHandler<IncrementCounter, CounterAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        IncrementCounter command,
        CounterAggregate? state
    )
    {
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