using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     A grain that tracks the latest known brook position for a specific UX projection type.
/// </summary>
/// <remarks>
///     <para>
///         Each UX projection type maintains its own cursor grain to track the brook position.
///         The cursor grain subscribes implicitly to brook cursor update events and caches
///         the latest position in memory for fast reads.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionKey" /> in the format
///         "projectionTypeName|brookType|brookId".
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.IUxProjectionCursorGrain")]
public interface IUxProjectionCursorGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Deactivates the grain, releasing resources and unsubscribing from streams.
    /// </summary>
    /// <returns>A task representing the deactivation operation.</returns>
    [Alias("DeactivateAsync")]
    Task DeactivateAsync();

    /// <summary>
    ///     Gets the latest known brook position for this projection.
    /// </summary>
    /// <returns>
    ///     The latest known brook position, or <see cref="BrookPosition.NotSet" /> if no events
    ///     have been received yet.
    /// </returns>
    /// <remarks>
    ///     This method returns the cached position from memory and does not perform any I/O.
    ///     The position is updated reactively via stream subscription to brook cursor events.
    /// </remarks>
    [ReadOnly]
    [Alias("GetPositionAsync")]
    Task<BrookPosition> GetPositionAsync();
}