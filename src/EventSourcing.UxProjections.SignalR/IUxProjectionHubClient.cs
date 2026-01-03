using System.Threading.Tasks;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Client callback interface for receiving projection change notifications.
/// </summary>
/// <remarks>
///     <para>
///         This interface defines the SignalR methods that clients must implement
///         to receive real-time notifications about projection changes.
///     </para>
///     <para>
///         When a projection's version changes, the client should fetch updated
///         data via HTTP GET using the ETag mechanism for optimistic concurrency.
///     </para>
/// </remarks>
public interface IUxProjectionHubClient
{
    /// <summary>
    ///     Called when a subscribed projection's version changes.
    /// </summary>
    /// <param name="projectionType">The type of projection that changed.</param>
    /// <param name="entityId">The identifier of the entity whose projection changed.</param>
    /// <param name="newVersion">The new version number of the projection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     The client should fetch updated data via HTTP GET when this is received.
    ///     Use the newVersion value to track whether a refresh is needed.
    /// </remarks>
    Task OnProjectionChangedAsync(
        string projectionType,
        string entityId,
        long newVersion
    );
}