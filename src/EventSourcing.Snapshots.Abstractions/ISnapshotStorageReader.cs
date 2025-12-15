using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Provides read access to projection snapshots stored for a specific projection type.
/// </summary>
/// <typeparam name="TProjection">The projection snapshot model.</typeparam>
public interface ISnapshotStorageReader<TProjection>
{
    /// <summary>
    ///     Reads a specific snapshot version for a projection stream.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying projection type, projection id, reducers hash, and version.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The snapshot projection, or <c>null</c> when the snapshot does not exist.</returns>
    Task<TProjection?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );
}