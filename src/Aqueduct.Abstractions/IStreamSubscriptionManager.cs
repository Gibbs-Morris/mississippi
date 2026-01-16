using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Aqueduct.Abstractions.Messages;


namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Defines a service for managing Orleans stream subscriptions for SignalR messaging.
/// </summary>
/// <remarks>
///     <para>
///         The stream subscription manager handles initialization and lifecycle
///         of Orleans streams used by the Aqueduct backplane. It manages subscriptions
///         to server-specific and hub-wide broadcast streams.
///     </para>
///     <para>
///         This abstraction isolates Orleans streaming concerns from the hub lifetime
///         manager, enabling easier testing and separation of concerns.
///     </para>
/// </remarks>
public interface IStreamSubscriptionManager
{
    /// <summary>
    ///     Gets a value indicating whether the stream subscriptions have been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    ///     Gets the unique identifier for this server instance.
    /// </summary>
    string ServerId { get; }

    /// <summary>
    ///     Ensures that stream subscriptions are set up for the hub.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="onServerMessage">Callback invoked when a server-targeted message is received.</param>
    /// <param name="onAllMessage">Callback invoked when a broadcast message is received.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task EnsureInitializedAsync(
        string hubName,
        Func<ServerMessage, Task> onServerMessage,
        Func<AllMessage, Task> onAllMessage,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Publishes a message to all clients connected to the hub across all servers.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishToAllAsync(
        AllMessage message
    );
}
