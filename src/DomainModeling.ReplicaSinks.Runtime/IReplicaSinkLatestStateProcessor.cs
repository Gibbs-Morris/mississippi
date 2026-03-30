using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Advances and flushes durable latest-state replica work for a projection/entity pair.
/// </summary>
internal interface IReplicaSinkLatestStateProcessor
{
    /// <summary>
    ///     Advances the desired source-position watermark for all bindings of the specified projection/entity pair.
    /// </summary>
    /// <param name="projectionType">The projection type whose bindings should advance.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="desiredSourcePosition">The highest desired source position.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous update.</returns>
    Task AdvanceDesiredPositionAsync(
        Type projectionType,
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Advances the desired source-position watermark for all bindings of the specified projection/entity pair.
    /// </summary>
    /// <typeparam name="TProjection">The projection type whose bindings should advance.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="desiredSourcePosition">The highest desired source position.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous update.</returns>
    Task AdvanceDesiredPositionAsync<TProjection>(
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;

    /// <summary>
    ///     Flushes the highest currently pending latest-state work for the specified projection/entity pair.
    /// </summary>
    /// <param name="projectionType">The projection type whose bindings should flush.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous flush.</returns>
    Task FlushAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Flushes the highest currently pending latest-state work for the specified projection/entity pair.
    /// </summary>
    /// <typeparam name="TProjection">The projection type whose bindings should flush.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous flush.</returns>
    Task FlushAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;
}
