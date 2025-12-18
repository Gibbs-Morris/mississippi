using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     A stateless worker grain that provides read access to a UX projection.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         UX projection grains are stateless workers that cache the last returned projection
///         in memory. On each request, they check the cursor position and only fetch a new
///         snapshot if the brook has advanced since the last read.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionKey" /> in the format
///         "projectionTypeName|brookType|brookId".
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.IUxProjectionGrain`1")]
public interface IUxProjectionGrain<TProjection> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current projection state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The current projection state, or <c>default</c> if the brook has no events yet.
    /// </returns>
    /// <remarks>
    ///     This method checks the cursor position and returns the cached projection if still current.
    ///     If the brook has advanced, it fetches the latest snapshot from the snapshot cache grain.
    /// </remarks>
    [ReadOnly]
    [Alias("GetAsync")]
    ValueTask<TProjection?> GetAsync(
        CancellationToken cancellationToken = default
    );
}
