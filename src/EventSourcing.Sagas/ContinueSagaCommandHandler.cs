using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Handles manual saga resume commands by emitting <see cref="SagaResumeRequested" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class ContinueSagaCommandHandler<TSaga> : CommandHandlerBase<ContinueSagaCommand, TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ContinueSagaCommandHandler{TSaga}" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    public ContinueSagaCommandHandler(
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        TimeProvider = timeProvider;
    }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        ContinueSagaCommand command,
        TSaga? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (state is null || state.Phase == SagaPhase.NotStarted)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                $"Saga '{typeof(TSaga).Name}' has not started.");
        }

        SagaResumeRequested resumeRequested = new()
        {
            SagaId = command.SagaId,
            CorrelationId = command.CorrelationId,
            RequestedAt = TimeProvider.GetUtcNow(),
        };

        return OperationResult.Ok<IReadOnlyList<object>>([resumeRequested]);
    }
}