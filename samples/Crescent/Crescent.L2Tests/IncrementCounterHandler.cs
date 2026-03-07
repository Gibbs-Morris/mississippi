using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Command handler for incrementing a counter.
/// </summary>
internal sealed class IncrementCounterHandler : CommandHandlerBase<IncrementCounter, CounterAggregate>
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