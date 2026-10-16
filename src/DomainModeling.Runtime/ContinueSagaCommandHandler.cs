using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

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
        if (command.SagaId == Guid.Empty)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Saga identifier must not be empty.");
        }

        if (state is null || (state.Phase == SagaPhase.NotStarted))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                $"Saga '{typeof(TSaga).Name}' has not started.");
        }

        if (state.SagaId != command.SagaId)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "The resume command does not identify the current saga instance.");
        }

        if (state.Phase == SagaPhase.Failed)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "A failed saga cannot continue without durable recovery direction and progress.");
        }

        if (state.Phase == SagaPhase.Compensating)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "A compensating saga cannot continue without a durable compensation cursor.");
        }

        if (state.Phase != SagaPhase.Running)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Only a running saga can continue.");
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