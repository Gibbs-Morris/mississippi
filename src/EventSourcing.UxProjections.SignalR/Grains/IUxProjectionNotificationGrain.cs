using System.Threading.Tasks;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Orleans grain interface that bridges projection cursor updates to SignalR clients.
/// </summary>
/// <remarks>
///     <para>
///         This grain subscribes to brook cursor update streams and pushes notifications
///         to SignalR groups. Each grain instance handles notifications for a specific
///         projection type and entity combination.
///     </para>
///     <para>
///         The grain is keyed by the projection key in the format
///         "projectionType|brookType|entityId". When the cursor moves, it notifies
///         all SignalR clients subscribed to the corresponding projection group.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.IUxProjectionNotificationGrain")]
public interface IUxProjectionNotificationGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Activates notification delivery for this projection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     Call this when the first client subscribes to this projection.
    ///     The grain will subscribe to the brook cursor stream and start
    ///     forwarding updates to SignalR.
    /// </remarks>
    [Alias("ActivateAsync")]
    Task ActivateAsync();

    /// <summary>
    ///     Gets the current cursor position for this projection.
    /// </summary>
    /// <returns>The current brook cursor position.</returns>
    [Alias("GetPositionAsync")]
    Task<long> GetPositionAsync();
}