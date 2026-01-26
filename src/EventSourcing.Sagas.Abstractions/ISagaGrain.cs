using System.Threading.Tasks;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Orleans grain interface for saga orchestration.
///     Each saga instance is a grain identified by its saga ID.
/// </summary>
/// <typeparam name="TInput">The saga input type.</typeparam>
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.ISagaGrain`1")]
public interface ISagaGrain<TInput> : IGrainWithGuidKey
    where TInput : class
{
    /// <summary>
    ///     Starts the saga with the specified input.
    /// </summary>
    /// <param name="input">The input data to start the saga.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <returns>A task representing the saga start operation.</returns>
    [Alias("StartAsync")]
    Task StartAsync(TInput input, string? correlationId = null);

    /// <summary>
    ///     Gets the current status of the saga.
    /// </summary>
    /// <returns>The saga status projection.</returns>
    [Alias("GetStatusAsync")]
    Task<Projections.SagaStatusProjection> GetStatusAsync();

    /// <summary>
    ///     Resumes a saga that requires manual intervention.
    /// </summary>
    /// <returns>A task representing the resume operation.</returns>
    [Alias("ResumeAsync")]
    Task ResumeAsync();

    /// <summary>
    ///     Cancels the saga, triggering compensation.
    /// </summary>
    /// <param name="reason">The cancellation reason.</param>
    /// <returns>A task representing the cancellation.</returns>
    [Alias("CancelAsync")]
    Task CancelAsync(string reason);
}
