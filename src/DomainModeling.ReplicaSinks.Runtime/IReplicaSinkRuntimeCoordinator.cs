using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Coordinates bounded live/bootstrap wake-ups and retry execution for replica sink delivery work.
/// </summary>
public interface IReplicaSinkRuntimeCoordinator
{
    /// <summary>
    ///     Executes a bounded batch of currently eligible replica sink work.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of projection/entity work items processed in the batch.</returns>
    Task<int> ExecuteBatchAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Registers bootstrap work for a single binding/entity lane and issues a best-effort execution nudge.
    /// </summary>
    /// <param name="projectionType">The projection type declaring the binding.</param>
    /// <param name="entityId">The replicated entity identifier.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="targetName">The provider-neutral target name.</param>
    /// <param name="bootstrapUpperBoundSourcePosition">The bootstrap cutover fence for the binding.</param>
    /// <param name="desiredSourcePosition">The highest desired source position currently known for the entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RegisterBootstrapAsync(
        Type projectionType,
        string entityId,
        string sinkKey,
        string targetName,
        long bootstrapUpperBoundSourcePosition,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Registers bootstrap work for a single binding/entity lane and issues a best-effort execution nudge.
    /// </summary>
    Task RegisterBootstrapAsync<TProjection>(
        string entityId,
        string sinkKey,
        string targetName,
        long bootstrapUpperBoundSourcePosition,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;

    /// <summary>
    ///     Raises the desired live watermark for all bindings of the specified projection/entity pair and issues a
    ///     best-effort execution nudge.
    /// </summary>
    /// <param name="projectionType">The projection type whose bindings should advance.</param>
    /// <param name="entityId">The replicated entity identifier.</param>
    /// <param name="desiredSourcePosition">The highest desired source position.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task NotifyLiveAsync(
        Type projectionType,
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Raises the desired live watermark for all bindings of the specified projection/entity pair and issues a
    ///     best-effort execution nudge.
    /// </summary>
    Task NotifyLiveAsync<TProjection>(
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class;
}
