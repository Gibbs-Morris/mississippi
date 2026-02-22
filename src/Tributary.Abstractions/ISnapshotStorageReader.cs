using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Provides read access to projection snapshots as raw envelopes so storage remains serialization-agnostic.
/// </summary>
public interface ISnapshotStorageReader
{
    /// <summary>
    ///     Reads a specific snapshot version for a projection stream.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying projection type, projection id, reducers hash, and version.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The snapshot envelope, or <c>null</c> when the snapshot does not exist.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );
}