using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Default saga orchestrator implementation.
/// </summary>
/// <remarks>
///     This orchestrator provides the high-level API for starting, monitoring, and
///     controlling saga instances. Since sagas ARE aggregates, this delegates to
///     the standard <see cref="IAggregateGrainFactory" />.
/// </remarks>
internal sealed class SagaOrchestrator : ISagaOrchestrator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaOrchestrator" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The aggregate grain factory.</param>
    public SagaOrchestrator(
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
        AggregateGrainFactory = aggregateGrainFactory;

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <inheritdoc />
    public Task CancelAsync(
        Guid sagaId,
        string reason
    ) =>

        // Cancel would dispatch CancelSagaCommand to the aggregate grain.
        throw new NotSupportedException("Saga cancellation is not yet implemented.");

    /// <inheritdoc />
    public async Task<SagaStatusProjection?> GetStatusAsync(
        Guid sagaId
    )
    {
        // Saga status is available via the SagaStatusProjection - requires projection infrastructure.
        // This would typically use a projection grain or query endpoint.
        await Task.CompletedTask;
        return null;
    }

    /// <inheritdoc />
    public Task ResumeAsync(
        Guid sagaId
    ) =>

        // Resume requires Orleans reminders (future enhancement).
        throw new NotSupportedException("Saga resume is not yet implemented.");

    /// <inheritdoc />
    public async Task StartAsync<TSaga, TInput>(
        Guid sagaId,
        TInput input,
        string? correlationId = null
    )
        where TSaga : class, ISagaDefinition
        where TInput : class
    {
        ArgumentNullException.ThrowIfNull(input);

        // Sagas are aggregates - use the standard aggregate grain
        IGenericAggregateGrain<TSaga> grain = AggregateGrainFactory.GetGenericAggregate<TSaga>(sagaId.ToString());
        StartSagaCommand<TInput> command = new(input, correlationId);
        OperationResult result = await grain.ExecuteAsync(command, CancellationToken.None);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to start saga: {result.ErrorCode} - {result.ErrorMessage}");
        }
    }
}