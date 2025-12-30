using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     A stateless worker grain that provides read access to a UX projection.
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
///         All projection reads are served via versioned cache grains to maintain single
///         responsibility and maximize cache reuse.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.IUxProjectionGrain`1")]
public interface IUxProjectionGrain<TProjection> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current projection state at the latest version.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The current projection state, or <c>default</c> if the brook has no events yet.
    /// </returns>
    /// <remarks>
    ///     This method gets the latest version from the cursor grain, then delegates to
    ///     <see cref="GetAtVersionAsync" /> to retrieve the projection from a versioned cache grain.
    /// </remarks>
    [ReadOnly]
    [Alias("GetAsync")]
    ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the projection state at a specific version.
    /// </summary>
    /// <param name="version">The specific version to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The projection state at the specified version, or <c>default</c> if the version is invalid.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method routes to a versioned cache grain that caches the projection
    ///         at the specified version. Multiple requests for the same version are served
    ///         from the same stateless worker grain, avoiding repeated snapshot reads.
    ///     </para>
    ///     <para>
    ///         Use this method when clients need a specific historical version of the projection
    ///         rather than the latest state.
    ///     </para>
    /// </remarks>
    [ReadOnly]
    [Alias("GetAtVersionAsync")]
    ValueTask<TProjection?> GetAtVersionAsync(
        BrookPosition version,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the latest known version of the projection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The latest version position, or <see cref="BrookPosition.NotSet" /> if no events exist.
    /// </returns>
    /// <remarks>
    ///     This method queries the cursor grain to get the current brook position without
    ///     fetching the projection state. Use this when you only need to know the version
    ///     number, not the actual projection data.
    /// </remarks>
    [ReadOnly]
    [Alias("GetLatestVersionAsync")]
    ValueTask<BrookPosition> GetLatestVersionAsync(
        CancellationToken cancellationToken = default
    );
}