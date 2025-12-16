using System.Collections.Generic;
using System.Threading;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;

/// <summary>
///     Defines query operations for reading snapshot identifiers from Cosmos.
/// </summary>
internal interface ISnapshotQueryService
{
    /// <summary>
    ///     Reads snapshot identifiers and versions for a stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of snapshot identifiers and versions.</returns>
    IAsyncEnumerable<SnapshotIdVersion> ReadIdsAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );
}