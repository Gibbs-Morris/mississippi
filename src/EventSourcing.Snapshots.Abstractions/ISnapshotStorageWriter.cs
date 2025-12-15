using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Provides write, delete, and pruning operations for projection snapshots.
/// </summary>
/// <typeparam name="TProjection">The projection snapshot model.</typeparam>
public interface ISnapshotStorageWriter<TProjection>
{
    /// <summary>
    ///     Deletes all snapshots for a projection stream, optionally scoped by reducers hash.
    /// </summary>
    /// <param name="streamKey">The stream key identifying projection type, projection id, and reducers hash.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when all snapshots for the stream are removed.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying projection type, projection id, reducers hash, and version.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when the snapshot is removed.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Prunes snapshots for a projection stream, retaining versions divisible by any provided modulus and always the most
    ///     recent version.
    /// </summary>
    /// <param name="streamKey">The stream key identifying projection type, projection id, and reducers hash.</param>
    /// <param name="retainModuli">
    ///     One or more modulus values; snapshots whose version is divisible by any value are retained. Implementations
    ///     should also retain the highest version even when it does not match a modulus.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when pruning finishes.</returns>
    Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes a snapshot for the specified projection stream and version.
    ///     Implementations must store one snapshot per version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying projection type, projection id, reducers hash, and version.</param>
    /// <param name="projection">The projection snapshot to persist.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when the snapshot is persisted.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        TProjection projection,
        CancellationToken cancellationToken = default
    );
}