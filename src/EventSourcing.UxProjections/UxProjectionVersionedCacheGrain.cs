using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Versioned UX projection cache grain that provides cached access
///     to a specific version of a projection.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         Versioned cache grains are stateless workers that cache a specific version
///         of a projection in memory. They complement the main UX projection grain
///         by handling requests for historical versions.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionVersionedCacheKey" /> in the format
///         "brookName|entityId|version".
///     </para>
///     <para>
///         Read path:
///         <list type="number">
///             <item>If cached, return cached projection (fast path).</item>
///             <item>Otherwise, fetch from <see cref="ISnapshotCacheGrain{TSnapshot}" /> (slow path).</item>
///             <item>Cache and return.</item>
///         </list>
///     </para>
///     <para>
///         The brook name is read from the grain key, eliminating the need for custom derived
///         grain classes with <c>[BrookName]</c> attributes.
///     </para>
/// </remarks>
[StatelessWorker]
internal sealed class UxProjectionVersionedCacheGrain<TProjection>
    : IUxProjectionVersionedCacheGrain<TProjection>,
      IGrainBase
    where TProjection : class
{
    private TProjection? cachedProjection;

    private UxProjectionVersionedCacheKey versionedKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionVersionedCacheGrain{TProjection}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for computing the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public UxProjectionVersionedCacheGrain(
        IGrainContext grainContext,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<TProjection> rootReducer,
        ILogger<UxProjectionVersionedCacheGrain<TProjection>> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger Logger { get; }

    private IRootReducer<TProjection> RootReducer { get; }

    private ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <inheritdoc />
    public ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Cache is populated on activation; this is now a pure read
        Logger.VersionedCacheHit(versionedKey);
        return new(cachedProjection);
    }

    /// <summary>
    ///     Called when the grain is activated. Initializes the versioned key and loads the projection from snapshot.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        try
        {
            versionedKey = UxProjectionVersionedCacheKey.Parse(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.VersionedCacheGrainInvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        Logger.VersionedCacheGrainActivated(
            primaryKey,
            versionedKey.BrookName,
            versionedKey.EntityId,
            versionedKey.Version);

        // Load projection from snapshot cache on activation (versioned = immutable)
        // Brook name and entity ID are extracted directly from the key
        string reducersHash = RootReducer.GetReducerHash();
        SnapshotStreamKey snapshotStreamKey = new(
            versionedKey.BrookName,
            SnapshotStorageNameHelper.GetStorageName<TProjection>(),
            versionedKey.EntityId,
            reducersHash);
        SnapshotKey snapshotKey = new(snapshotStreamKey, versionedKey.Version.Value);
        ISnapshotCacheGrain<TProjection> snapshotCacheGrain =
            SnapshotGrainFactory.GetSnapshotCacheGrain<TProjection>(snapshotKey);
        cachedProjection = await snapshotCacheGrain.GetStateAsync(token);
        Logger.VersionedCacheLoaded(versionedKey);
    }
}