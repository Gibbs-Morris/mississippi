using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Diagnostics;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     UX projection grain that provides cached, read-optimized access to projection state.
/// </summary>
/// <typeparam name="TProjection">
///     The projection state type. Must be decorated with
///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
/// </typeparam>
/// <remarks>
///     <para>
///         UX projection grains are stateless workers that serve as the entry point to the
///         UX projection grain family. They coordinate access to cursor grains and versioned
///         cache grains.
///     </para>
///     <para>
///         The grain is keyed by just the entity ID. The brook name is obtained from
///         the <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         on the <typeparamref name="TProjection" /> type itself.
///     </para>
///     <para>
///         The projection type <typeparamref name="TProjection" /> MUST be decorated with
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
///         If the attribute is missing, the grain will fail to activate with an exception.
///     </para>
///     <para>
///         Read path for latest projection:
///         <list type="number">
///             <item>Get current brook position from <see cref="IUxProjectionCursorGrain" />.</item>
///             <item>
///                 Delegate to <see cref="GetAtVersionAsync" /> which routes to
///                 <see cref="IUxProjectionVersionedCacheGrain{TProjection}" />.
///             </item>
///         </list>
///     </para>
///     <para>
///         Read path for specific version:
///         <list type="number">
///             <item>
///                 Route directly to <see cref="IUxProjectionVersionedCacheGrain{TProjection}" />
///                 which caches that specific version.
///             </item>
///         </list>
///     </para>
/// </remarks>
[StatelessWorker]
internal sealed class UxProjectionGrain<TProjection>
    : IUxProjectionGrain<TProjection>,
      IGrainBase
    where TProjection : class
{
    private string entityId = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionGrain{TProjection}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public UxProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UxProjectionGrain<TProjection>> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        UxProjectionGrainFactory = uxProjectionGrainFactory ??
                                   throw new ArgumentNullException(nameof(uxProjectionGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the brook name from the
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     on the <typeparamref name="TProjection" /> type.
    /// </summary>
    /// <remarks>
    ///     This property reads the attribute from the projection type at runtime.
    ///     If the attribute is missing, an <see cref="InvalidOperationException" /> is thrown.
    ///     The value is cached in <see cref="BrookNameHelper" /> so repeated calls are efficient.
    /// </remarks>
    private static string BrookName => BrookNameHelper.GetBrookName<TProjection>();

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxProjectionGrain<TProjection>> Logger { get; }

    private IUxProjectionGrainFactory UxProjectionGrainFactory { get; }

    /// <inheritdoc />
    public async ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    )
    {
        string projectionTypeName = typeof(TProjection).Name;
        Stopwatch sw = Stopwatch.StartNew();

        // Get current brook position from the projection cursor grain
        BrookPosition latestVersion = await GetLatestVersionAsync(cancellationToken);

        // Handle case where brook has no events yet
        if (latestVersion.NotSet)
        {
            sw.Stop();
            UxProjectionMetrics.RecordQuery(projectionTypeName, "latest", sw.Elapsed.TotalMilliseconds, false);
            Logger.NoEventsYet(entityId);
            return default;
        }

        // Delegate to versioned cache grain for single responsibility
        Logger.GetAsyncDelegatingToVersion(entityId, latestVersion);
        TProjection? result = await GetAtVersionAsync(latestVersion, cancellationToken);
        sw.Stop();
        UxProjectionMetrics.RecordQuery(projectionTypeName, "latest", sw.Elapsed.TotalMilliseconds, result is not null);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<TProjection?> GetAtVersionAsync(
        BrookPosition version,
        CancellationToken cancellationToken = default
    )
    {
        // Validate version
        if (version.NotSet)
        {
            Logger.VersionedRequestInvalidVersion(entityId, version);
            return default;
        }

        // Route to versioned cache grain for the specific version
        Logger.VersionedRequestRouting(entityId, version);
        UxProjectionVersionedCacheKey versionedCacheKey = new(BrookName, entityId, version);
        IUxProjectionVersionedCacheGrain<TProjection> versionedCacheGrain =
            UxProjectionGrainFactory.GetUxProjectionVersionedCacheGrain<TProjection>(versionedCacheKey);
        TProjection? result = await versionedCacheGrain.GetAsync(cancellationToken);
        Logger.VersionedRequestCompleted(entityId, version);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<BrookPosition> GetLatestVersionAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken; // Reserved for future use
        UxProjectionCursorKey cursorKey = new(BrookName, entityId);
        IUxProjectionCursorGrain cursorGrain = UxProjectionGrainFactory.GetUxProjectionCursorGrain(cursorKey);
        BrookPosition position = await cursorGrain.GetPositionAsync();
        Logger.LatestVersionRetrieved(entityId, position);
        return position;
    }

    /// <summary>
    ///     Called when the grain is activated. Validates the projection type has
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     and initializes the entity ID.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <typeparamref name="TProjection" /> type is missing the
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </exception>
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        // Validate that the attribute is present on the projection type (fail-fast)
        // This call will throw InvalidOperationException if the attribute is missing
        _ = BrookNameHelper.GetBrookName<TProjection>();
        entityId = this.GetPrimaryKeyString();
        Logger.ProjectionGrainActivated(entityId, BrookName);
        return Task.CompletedTask;
    }
}