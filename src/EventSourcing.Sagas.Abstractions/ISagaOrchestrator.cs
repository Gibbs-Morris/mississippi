using System;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Orchestrates the execution of sagas, managing step progression and compensation.
/// </summary>
public interface ISagaOrchestrator
{
    /// <summary>
    ///     Starts a new saga instance with the specified input data.
    /// </summary>
    /// <typeparam name="TSaga">The saga definition type.</typeparam>
    /// <typeparam name="TInput">The type of input data for the saga.</typeparam>
    /// <param name="sagaId">The unique identifier for this saga instance.</param>
    /// <param name="input">The input data to start the saga with.</param>
    /// <param name="correlationId">Optional correlation identifier for tracking.</param>
    /// <returns>A task representing the saga start operation.</returns>
    Task StartAsync<TSaga, TInput>(Guid sagaId, TInput input, string? correlationId = null)
        where TSaga : class, ISagaDefinition
        where TInput : class;

    /// <summary>
    ///     Gets the current status of a saga instance.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <returns>The current saga status projection, or null if not found.</returns>
    Task<Projections.SagaStatusProjection?> GetStatusAsync(Guid sagaId);

    /// <summary>
    ///     Resumes a saga that is awaiting intervention or manual retry.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <returns>A task representing the resume operation.</returns>
    Task ResumeAsync(Guid sagaId);

    /// <summary>
    ///     Cancels a running saga, triggering compensation for completed steps.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <returns>A task representing the cancellation operation.</returns>
    Task CancelAsync(Guid sagaId, string reason);
}
