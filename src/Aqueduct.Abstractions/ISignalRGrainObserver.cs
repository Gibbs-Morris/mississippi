using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Aqueduct.Abstractions.Grains;


namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Provides an abstraction for ASP.NET-hosted services to send messages to SignalR clients.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Deployment boundary:</strong> This interface is intended for use on ASP.NET pods
///         that have access to <c>IHubContext</c>. It should <strong>not</strong> be injected into
///         Orleans grains running on silo pods, as the implementation requires SignalR infrastructure.
///     </para>
///     <para>
///         <strong>For grains:</strong> Grains that need to send SignalR messages should call
///         <see cref="ISignalRGroupGrain.SendMessageAsync" />
///         directly. The group grain publishes to an Orleans stream that ASP.NET pods subscribe to,
///         properly bridging the silo-to-web deployment boundary.
///     </para>
///     <para>
///         The hub name is used to route messages to the correct SignalR hub
///         when multiple hubs are registered in the application.
///     </para>
/// </remarks>
public interface ISignalRGrainObserver
{
    /// <summary>
    ///     Sends a message to all connected clients of a hub.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="method">The hub method name to invoke on clients.</param>
    /// <param name="args">The arguments to pass to the hub method.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendToAllAsync(
        string hubName,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Sends a message to a specific SignalR connection.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="connectionId">The SignalR connection identifier.</param>
    /// <param name="method">The hub method name to invoke on the client.</param>
    /// <param name="args">The arguments to pass to the hub method.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendToConnectionAsync(
        string hubName,
        string connectionId,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Sends a message to all connections in a SignalR group.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="groupName">The name of the group to send to.</param>
    /// <param name="method">The hub method name to invoke on clients.</param>
    /// <param name="args">The arguments to pass to the hub method.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendToGroupAsync(
        string hubName,
        string groupName,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    );
}