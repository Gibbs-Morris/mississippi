using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Base class for UX projection grains that provide cached, read-optimized access to projection state.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         UX projection grains are stateless workers that serve as the entry point to the
///         UX projection grain family. They coordinate access to cursor grains and versioned
///         cache grains.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionKey" /> in the format
///         "projectionTypeName|brookType|brookId".
///     </para>
///     <para>
///         Derived classes MUST be decorated with <see cref="BrookNameAttribute" />
///         directly on the final sealed class. If the attribute is missing, the grain will
///         fail to activate with an exception.
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
public abstract class UxProjectionGrainBase<TProjection>
    : IUxProjectionGrain<TProjection>,
      IGrainBase
    where TProjection : class
{
    private UxProjectionKey projectionKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionGrainBase{TProjection}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    protected UxProjectionGrainBase(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        UxProjectionGrainFactory = uxProjectionGrainFactory ??
                                   throw new ArgumentNullException(nameof(uxProjectionGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the brook name from the <see cref="BrookNameAttribute" /> on this grain type.
    /// </summary>
    /// <remarks>
    ///     This property reads the attribute from the concrete grain type at runtime.
    ///     If the attribute is missing, an <see cref="InvalidOperationException" /> is thrown.
    /// </remarks>
    protected string BrookName => BrookNameHelper.GetBrookNameFromGrain(GetType());

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Gets the factory for resolving UX projection grains and cursors.
    /// </summary>
    protected IUxProjectionGrainFactory UxProjectionGrainFactory { get; }

    /// <inheritdoc />
    public async ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Get current brook position from the projection cursor grain
        BrookPosition latestVersion = await GetLatestVersionAsync(cancellationToken);

        // Handle case where brook has no events yet
        if (latestVersion.NotSet)
        {
            Logger.NoEventsYet(projectionKey);
            return default;
        }

        // Delegate to versioned cache grain for single responsibility
        Logger.GetAsyncDelegatingToVersion(projectionKey, latestVersion);
        return await GetAtVersionAsync(latestVersion, cancellationToken);
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
            Logger.VersionedRequestInvalidVersion(projectionKey, version);
            return default;
        }

        // Route to versioned cache grain for the specific version
        Logger.VersionedRequestRouting(projectionKey, version);
        UxProjectionVersionedKey versionedKey = new(projectionKey, version);
        IUxProjectionVersionedCacheGrain<TProjection> versionedCacheGrain =
            UxProjectionGrainFactory.GetUxProjectionVersionedCacheGrain<TProjection>(versionedKey);
        TProjection? result = await versionedCacheGrain.GetAsync(cancellationToken);
        Logger.VersionedRequestCompleted(projectionKey, version);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<BrookPosition> GetLatestVersionAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken; // Reserved for future use
        IUxProjectionCursorGrain cursorGrain = UxProjectionGrainFactory.GetUxProjectionCursorGrain(projectionKey);
        BrookPosition position = await cursorGrain.GetPositionAsync();
        Logger.LatestVersionRetrieved(projectionKey, position);
        return position;
    }

    /// <summary>
    ///     Called when the grain is activated. Validates the attribute and initializes the projection key.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <see cref="BrookNameAttribute" /> is missing from the concrete grain type.
    /// </exception>
    public virtual Task OnActivateAsync(
        CancellationToken token
    )
    {
        // Validate that the attribute is present on the concrete grain type (fail-fast)
        // This call will throw InvalidOperationException if the attribute is missing
        _ = BrookNameHelper.GetDefinition(GetType());
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