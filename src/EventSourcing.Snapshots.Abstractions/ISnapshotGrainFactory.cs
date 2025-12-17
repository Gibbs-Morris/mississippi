namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Defines a factory for resolving Orleans snapshot grains by key.
/// </summary>
public interface ISnapshotGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="ISnapshotCacheGrain{TState}" /> for the specified snapshot key.
    /// </summary>
    /// <typeparam name="TState">The type of state stored in the snapshot.</typeparam>
    /// <param name="snapshotKey">The key identifying the snapshot (projection type, id, reducer hash, and version).</param>
    /// <returns>An <see cref="ISnapshotCacheGrain{TState}" /> instance for the snapshot.</returns>
    ISnapshotCacheGrain<TState> GetSnapshotCacheGrain<TState>(
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