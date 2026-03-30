using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Reads projection source state for replica latest-state processing.
/// </summary>
internal interface IReplicaSinkSourceStateAccessor
{
    /// <summary>
    ///     Reads projection source state for a specific projection/entity/source-position triple.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="sourcePosition">The requested source position.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tri-state source read result.</returns>
    ValueTask<ReplicaSinkSourceState> ReadAsync(
        Type projectionType,
        string entityId,
        long sourcePosition,
        CancellationToken cancellationToken = default
    );
}