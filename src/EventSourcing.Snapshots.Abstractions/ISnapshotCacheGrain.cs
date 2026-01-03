using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     An Orleans grain that acts as an immutable, in-memory cache for a snapshot at a specific version.
/// </summary>
/// <typeparam name="TSnapshot">The type of state stored in the snapshot.</typeparam>
/// <remarks>
///     <para>
///         This grain is keyed by <see cref="SnapshotKey" /> in the format
///         "brookName|entityId|version|snapshotStorageName|reducersHash".
///         Once activated and hydrated, the state is immutable and cached in memory for fast read access.
///     </para>
///     <para>
///         On activation, the grain first attempts to load the snapshot from storage.
///         If the snapshot does not exist or has a stale reducer hash, the grain reads events from
///         the underlying brook and rebuilds the state using the registered reducers.
///     </para>
///     <para>
///         After the state is built, a one-way call is made to an <see cref="ISnapshotPersisterGrain" />
///         to persist the snapshot asynchronously without blocking the caller.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Snapshots.Abstractions.ISnapshotCacheGrain`1")]
public interface ISnapshotCacheGrain<TSnapshot> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the cached state for this snapshot version.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The state for this snapshot version.</returns>
    /// <remarks>
    ///     The state is hydrated on grain activation. This method returns the already-cached state
    ///     and does not perform any I/O after activation completes.
    /// </remarks>
    [ReadOnly]
    [Alias("GetStateAsync")]
    ValueTask<TSnapshot> GetStateAsync(
        CancellationToken cancellationToken = default
    );
}