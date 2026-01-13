using Crescent.Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Handlers;

/// <summary>
///     Command handler for initializing a counter.
/// </summary>
internal sealed class InitializeCounterHandler : CommandHandlerBase<InitializeCounter, CounterAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        InitializeCounter command,
        CounterAggregate? state
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