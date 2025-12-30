using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Base class for versioned UX projection cache grains that provide cached access
///     to a specific version of a projection.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <typeparam name="TBrook">The brook definition type that identifies the event stream.</typeparam>
/// <remarks>
///     <para>
///         Versioned cache grains are stateless workers that cache a specific version
///         of a projection in memory. They complement the main UX projection grain
///         by handling requests for historical versions.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionVersionedKey" /> in the format
///         "projectionTypeName|brookType|brookId|version".
///     </para>
///     <para>
///         Read path:
///         <list type="number">
///             <item>If cached, return cached projection (fast path).</item>
///             <item>Otherwise, fetch from <see cref="ISnapshotCacheGrain{TSnapshot}" /> (slow path).</item>
///             <item>Cache and return.</item>
///         </list>
///     </para>
/// </remarks>
[StatelessWorker]
public abstract class UxProjectionVersionedCacheGrainBase<TProjection, TBrook>
    : IUxProjectionVersionedCacheGrain<TProjection>,
      IGrainBase
    where TProjection : class
    where TBrook : IBrookDefinition
{
    private TProjection? cachedProjection;

    private UxProjectionVersionedKey versionedKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionVersionedCacheGrainBase{TProjection, TBrook}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="reducersHash">The hash of the reducers for snapshot key construction.</param>
    /// <param name="logger">Logger instance.</param>
    protected UxProjectionVersionedCacheGrainBase(
        IGrainContext grainContext,
        ISnapshotGrainFactory snapshotGrainFactory,
        string reducersHash,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        ReducersHash = reducersHash ?? throw new ArgumentNullException(nameof(reducersHash));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the brook name from the <typeparamref name="TBrook" /> definition.
    /// </summary>
    protected static string BrookName => TBrook.BrookName;

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Gets the hash of the reducers for snapshot key construction.
    /// </summary>
    protected string ReducersHash { get; }

    /// <summary>
    ///     Gets the factory for resolving snapshot grains.
    /// </summary>
    protected ISnapshotGrainFactory SnapshotGrainFactory { get; }

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
    public virtual async Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        try
        {
            versionedKey = UxProjectionVersionedKey.FromString(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.VersionedCacheGrainInvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        Logger.VersionedCacheGrainActivated(
            primaryKey,
            versionedKey.ProjectionKey.ProjectionTypeName,
            versionedKey.ProjectionKey.BrookKey,
            versionedKey.Version);

        // Load projection from snapshot cache on activation (versioned = immutable)
        SnapshotStreamKey snapshotStreamKey = new(
            SnapshotNameHelper.GetSnapshotName<TProjection>(),
            versionedKey.ProjectionKey.BrookKey.Id,
            ReducersHash);
        SnapshotKey snapshotKey = new(snapshotStreamKey, versionedKey.Version.Value);
        ISnapshotCacheGrain<TProjection> snapshotCacheGrain =
            SnapshotGrainFactory.GetSnapshotCacheGrain<TProjection>(snapshotKey);
        cachedProjection = await snapshotCacheGrain.GetStateAsync(token);
        Logger.VersionedCacheLoaded(versionedKey);
    }
}