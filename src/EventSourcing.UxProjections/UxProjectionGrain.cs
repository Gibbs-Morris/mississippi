using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Base class for UX projection grains that provide cached, read-optimized access to projection state.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <typeparam name="TBrook">The brook definition type that identifies the event stream.</typeparam>
/// <remarks>
///     <para>
///         UX projection grains are stateless workers that cache the last returned projection in memory.
///         On each request, they check the cursor position and only fetch a new snapshot if the brook
///         has advanced since the last read.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionKey" /> in the format
///         "projectionTypeName|brookType|brookId".
///     </para>
///     <para>
///         Read path:
///         <list type="number">
///             <item>Get current brook position from <see cref="IUxProjectionCursorGrain" />.</item>
///             <item>If position matches cached version, return cached projection (fast path).</item>
///             <item>If position is newer, fetch from <see cref="ISnapshotCacheGrain{TSnapshot}" /> (slow path).</item>
///             <item>Update cache and return.</item>
///         </list>
///     </para>
/// </remarks>
[StatelessWorker]
public abstract class UxProjectionGrain<TProjection, TBrook>
    : IUxProjectionGrain<TProjection>,
      IGrainBase
    where TProjection : class
    where TBrook : IBrookDefinition
{
    private TProjection? cachedProjection;

    private BrookPosition cachedVersion = -1;

    private UxProjectionKey projectionKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionGrain{TProjection, TBrook}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="reducersHash">The hash of the reducers for snapshot key construction.</param>
    /// <param name="logger">Logger instance.</param>
    protected UxProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ISnapshotGrainFactory snapshotGrainFactory,
        string reducersHash,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        UxProjectionGrainFactory =
            uxProjectionGrainFactory ?? throw new ArgumentNullException(nameof(uxProjectionGrainFactory));
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

    /// <summary>
    ///     Gets the factory for resolving UX projection grains.
    /// </summary>
    protected IUxProjectionGrainFactory UxProjectionGrainFactory { get; }

    /// <inheritdoc />
    public async ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Get current brook position from cursor grain
        IUxProjectionCursorGrain cursorGrain = UxProjectionGrainFactory.GetUxProjectionCursorGrain(projectionKey);
        BrookPosition currentPosition = await cursorGrain.GetPositionAsync();

        // Fast path: return cached projection if still current
        if (!currentPosition.IsNewerThan(cachedVersion))
        {
            Logger.CacheHit(projectionKey, cachedVersion);
            return cachedProjection;
        }

        // Handle case where brook has no events yet
        if (currentPosition.NotSet)
        {
            Logger.NoEventsYet(projectionKey);
            cachedProjection = default;
            cachedVersion = currentPosition;
            return cachedProjection;
        }

        // Slow path: fetch from snapshot cache grain
        Logger.CacheMiss(projectionKey, cachedVersion, currentPosition);
        SnapshotStreamKey snapshotStreamKey = new(
            SnapshotNameHelper.GetSnapshotName<TProjection>(),
            projectionKey.BrookKey.Id,
            ReducersHash);
        SnapshotKey snapshotKey = new(snapshotStreamKey, currentPosition.Value);

        ISnapshotCacheGrain<TProjection> snapshotCacheGrain =
            SnapshotGrainFactory.GetSnapshotCacheGrain<TProjection>(snapshotKey);
        cachedProjection = await snapshotCacheGrain.GetStateAsync(cancellationToken);
        cachedVersion = currentPosition;

        Logger.CacheUpdated(projectionKey, cachedVersion);
        return cachedProjection;
    }

    /// <summary>
    ///     Called when the grain is activated. Initializes the projection key.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public virtual Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        try
        {
            projectionKey = UxProjectionKey.FromString(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.ProjectionGrainInvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        Logger.ProjectionGrainActivated(primaryKey, projectionKey.ProjectionTypeName, projectionKey.BrookKey);
        return Task.CompletedTask;
    }
}
