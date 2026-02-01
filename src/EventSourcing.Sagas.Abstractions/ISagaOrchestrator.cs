using System;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions.Projections;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Orchestrates the execution of sagas, managing step progression and status retrieval.
/// </summary>
/// <remarks>
///     <para>
///         This interface provides the core saga operations: starting new saga instances and
///         querying their status. Saga cancellation and resume operations may be added in a
///         future release via a separate interface.
///     </para>
/// </remarks>
public interface ISagaOrchestrator
{
    /// <summary>
    ///     Gets the current status of a saga instance.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <returns>The current saga status projection, or null if not found.</returns>
    Task<SagaStatusProjection?> GetStatusAsync(
        Guid sagaId
    );

    /// <summary>
    ///     Starts a new saga instance with the specified input data.
    /// </summary>
    /// <typeparam name="TSaga">The saga definition type.</typeparam>
    /// <typeparam name="TInput">The type of input data for the saga.</typeparam>
    /// <param name="sagaId">The unique identifier for this saga instance.</param>
    /// <param name="input">The input data to start the saga with.</param>
    /// <param name="correlationId">Optional correlation identifier for tracking.</param>
    /// <returns>A task representing the saga start operation.</returns>
    Task StartAsync<TSaga, TInput>(
        Guid sagaId,
        TInput input,
        string? correlationId = null
    )
        where TSaga : class, ISagaDefinition
        where TInput : class;
}