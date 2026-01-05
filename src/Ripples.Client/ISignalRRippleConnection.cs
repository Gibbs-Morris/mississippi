using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Represents a SignalR connection for receiving projection updates.
/// </summary>
/// <remarks>
///     <para>
///         This interface abstracts the SignalR connection lifecycle, allowing
///         components to subscribe to projection updates without managing the
///         underlying connection details.
///     </para>
///     <para>
///         The connection implements automatic reconnection with exponential backoff
///         based on the configured <see cref="RipplesClientOptions" />.
///     </para>
/// </remarks>
public interface ISignalRRippleConnection : IAsyncDisposable
{
    /// <summary>
    ///     Occurs when the connection state changes.
    /// </summary>
    event EventHandler<SignalRConnectionStateChangeEventArgs>? StateChanged;

    /// <summary>
    ///     Gets the current connection state.
    /// </summary>
    SignalRConnectionState State { get; }

    /// <summary>
    ///     Starts the SignalR connection if not already connected.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Stops the SignalR connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Subscribes to updates for a specific projection entity.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="callback">The callback to invoke when updates are received.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A disposable that unsubscribes when disposed.</returns>
    Task<IDisposable> SubscribeAsync(
        string projectionType,
        string entityId,
        Action<long> callback,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Unsubscribes from updates for a specific projection entity.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(
        string projectionType,
        string entityId,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
///     Represents the state of a SignalR connection.
/// </summary>
public enum SignalRConnectionState
{
    /// <summary>
    ///     The connection has not been started.
    /// </summary>
    Disconnected,

    /// <summary>
    ///     The connection is being established.
    /// </summary>
    Connecting,

    /// <summary>
    ///     The connection is established and active.
    /// </summary>
    Connected,

    /// <summary>
    ///     The connection is attempting to reconnect.
    /// </summary>
    Reconnecting,
}

/// <summary>
///     Event arguments for <see cref="ISignalRRippleConnection.StateChanged" />.
/// </summary>
public sealed class SignalRConnectionStateChangeEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRConnectionStateChangeEventArgs" /> class.
    /// </summary>
    /// <param name="previousState">The previous connection state.</param>
    /// <param name="currentState">The current connection state.</param>
    /// <param name="exception">An exception if the state change was due to an error; otherwise, null.</param>
    public SignalRConnectionStateChangeEventArgs(
        SignalRConnectionState previousState,
        SignalRConnectionState currentState,
        Exception? exception = null
    )
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Exception = exception;
    }

    /// <summary>
    ///     Gets the current connection state.
    /// </summary>
    public SignalRConnectionState CurrentState { get; }

    /// <summary>
    ///     Gets the exception if the state change was due to an error; otherwise, null.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    ///     Gets the previous connection state.
    /// </summary>
    public SignalRConnectionState PreviousState { get; }
}