using System;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;

using Orleans;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Default saga orchestrator implementation.
/// </summary>
/// <remarks>
///     This orchestrator provides the high-level API for starting, monitoring, and
///     controlling saga instances. It delegates to the underlying saga grains.
/// </remarks>
internal sealed class SagaOrchestrator : ISagaOrchestrator
{
    private IGrainFactory GrainFactory { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaOrchestrator" /> class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory.</param>
    public SagaOrchestrator(
        IGrainFactory grainFactory
    )
    {
        GrainFactory = grainFactory;
    }

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

        ISagaGrain<TInput> grain = GrainFactory.GetGrain<ISagaGrain<TInput>>(sagaId);
        await grain.StartAsync(input, correlationId);
    }

    /// <inheritdoc />
    public async Task<SagaStatusProjection?> GetStatusAsync(
        Guid sagaId
    )
    {
        // Placeholder: Saga status projection requires grain implementation.
        // This will be implemented when SagaGrain is complete.
        await Task.CompletedTask;
        return null;
    }

    /// <inheritdoc />
    public Task ResumeAsync(
        Guid sagaId
    )
    {
        // Placeholder: Resume requires grain implementation.
        throw new NotSupportedException("Saga resume is not yet implemented.");
    }

    /// <inheritdoc />
    public Task CancelAsync(
        Guid sagaId,
        string reason
    )
    {
        // Placeholder: Cancel requires grain implementation.
        throw new NotSupportedException("Saga cancellation is not yet implemented.");
    }
}
