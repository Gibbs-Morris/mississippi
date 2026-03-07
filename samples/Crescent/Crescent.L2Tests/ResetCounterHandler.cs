using Mississippi.DomainModeling.Abstractions;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     Command handler for resetting a counter.
/// </summary>
internal sealed class ResetCounterHandler : CommandHandlerBase<ResetCounter, CounterAggregate>
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