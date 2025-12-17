namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Defines a factory for resolving Orleans snapshot grains by key.
/// </summary>
public interface ISnapshotGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="ISnapshotCacheGrain{TSnapshot}" /> for the specified snapshot key.
    /// </summary>
    /// <typeparam name="TSnapshot">The type of state stored in the snapshot.</typeparam>
    /// <param name="snapshotKey">The key identifying the snapshot (projection type, id, reducer hash, and version).</param>
    /// <returns>An <see cref="ISnapshotCacheGrain{TSnapshot}" /> instance for the snapshot.</returns>
    ISnapshotCacheGrain<TSnapshot> GetSnapshotCacheGrain<TSnapshot>(
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Retrieves an <see cref="ISnapshotPersisterGrain" /> for the specified snapshot key.
    /// </summary>
    /// <param name="snapshotKey">The key identifying the snapshot (projection type, id, reducer hash, and version).</param>
    /// <returns>An <see cref="ISnapshotPersisterGrain" /> instance for persisting the snapshot.</returns>
    ISnapshotPersisterGrain GetSnapshotPersisterGrain(
        SnapshotKey snapshotKey
    );
}