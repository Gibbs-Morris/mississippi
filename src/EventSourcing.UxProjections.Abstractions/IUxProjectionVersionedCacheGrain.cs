using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     A stateless worker grain that caches a specific version of a UX projection.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         Versioned cache grains are stateless workers keyed by
///         <see cref="UxProjectionVersionedKey" /> in the format
///         "projectionTypeName|brookType|brookId|version".
///     </para>
///     <para>
///         Each grain instance caches a specific version of a projection in memory.
///         This allows clients requesting the same version to share a cached instance,
///         reducing load on the underlying snapshot cache grains.
///     </para>
///     <para>
///         The grain fetches the projection from a snapshot cache grain
///         on activation and caches it for the lifetime of the grain instance.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.IUxProjectionVersionedCacheGrain`1")]
public interface IUxProjectionVersionedCacheGrain<TProjection> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the projection state at the version specified by this grain's key.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The projection state at the specified version, or <c>default</c> if not found.
    /// </returns>
    /// <remarks>
    ///     The projection is loaded from the snapshot cache grain during activation
    ///     and cached in memory for all subsequent <see cref="GetAsync" /> requests.
    /// </remarks>
    [ReadOnly]
    [Alias("GetAsync")]
    ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    );
}