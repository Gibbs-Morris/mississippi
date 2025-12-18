using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     An Orleans grain that handles background persistence of snapshots to storage.
/// </summary>
/// <remarks>
///     <para>
///         This grain is designed to receive fire-and-forget calls from <see cref="ISnapshotCacheGrain{TSnapshot}" />,
///         allowing the cache grain to return immediately after building state
///         while persistence happens asynchronously.
///     </para>
///     <para>
///         The grain is keyed by <see cref="SnapshotKey" /> in the format
///         "projectionType|projectionId|reducersHash|version",
///         matching the cache grain's key for one-to-one correspondence.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Snapshots.Abstractions.ISnapshotPersisterGrain")]
public interface ISnapshotPersisterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Persists a snapshot envelope to storage.
    /// </summary>
    /// <param name="envelope">The snapshot envelope containing the serialized state and metadata.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous persistence operation.</returns>
    /// <remarks>
    ///     This method is marked with <see cref="OneWayAttribute" /> for fire-and-forget semantics.
    ///     The caller does not wait for persistence to complete.
    /// </remarks>
    [Alias("PersistAsync")]
    [OneWay]
    Task PersistAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken = default
    );
}