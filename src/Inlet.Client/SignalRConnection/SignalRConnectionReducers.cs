namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Contains reducer functions for the SignalR connection feature state.
/// </summary>
/// <remarks>
///     <para>
///         These reducers handle connection lifecycle actions and update the
///         <see cref="SignalRConnectionState" /> accordingly.
///     </para>
///     <para>
///         All reducers are pure functions that return a new state instance.
///     </para>
/// </remarks>
internal static class SignalRConnectionReducers
{
    /// <summary>
    ///     Reducer for the <see cref="SignalRConnectedAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The connected action.</param>
    /// <returns>The new connection state with connected status.</returns>
    public static SignalRConnectionState OnConnected(
        SignalRConnectionState state,
        SignalRConnectedAction action
    ) =>
        state with
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = action.ConnectionId,
            LastConnectedAt = action.Timestamp,
            ReconnectAttemptCount = 0,
            LastError = null,
        };

    /// <summary>
    ///     Reducer for the <see cref="SignalRConnectingAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The connecting action.</param>
    /// <returns>The new connection state with connecting status.</returns>
    public static SignalRConnectionState OnConnecting(
        SignalRConnectionState state,
        SignalRConnectingAction action
    )
    {
        _ = action;
        return state with
        {
            Status = SignalRConnectionStatus.Connecting,
            LastError = null,
        };
    }

    /// <summary>
    ///     Reducer for the <see cref="SignalRDisconnectedAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The disconnected action.</param>
    /// <returns>The new connection state with disconnected status.</returns>
    public static SignalRConnectionState OnDisconnected(
        SignalRConnectionState state,
        SignalRDisconnectedAction action
    ) =>
        state with
        {
            Status = SignalRConnectionStatus.Disconnected,
            LastDisconnectedAt = action.Timestamp,
            LastError = action.Error,
            ConnectionId = null,
        };

    /// <summary>
    ///     Reducer for the <see cref="SignalRMessageReceivedAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The message received action.</param>
    /// <returns>The new connection state with updated message timestamp.</returns>
    public static SignalRConnectionState OnMessageReceived(
        SignalRConnectionState state,
        SignalRMessageReceivedAction action
    ) =>
        state with
        {
            LastMessageReceivedAt = action.Timestamp,
        };

    /// <summary>
    ///     Reducer for the <see cref="SignalRReconnectedAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The reconnected action.</param>
    /// <returns>The new connection state with connected status after reconnection.</returns>
    public static SignalRConnectionState OnReconnected(
        SignalRConnectionState state,
        SignalRReconnectedAction action
    ) =>
        state with
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = action.ConnectionId,
            LastConnectedAt = action.Timestamp,
            ReconnectAttemptCount = 0,
            LastError = null,
        };

    /// <summary>
    ///     Reducer for the <see cref="SignalRReconnectingAction" />.
    /// </summary>
    /// <param name="state">The current connection state.</param>
    /// <param name="action">The reconnecting action.</param>
    /// <returns>The new connection state with reconnecting status.</returns>
    public static SignalRConnectionState OnReconnecting(
        SignalRConnectionState state,
        SignalRReconnectingAction action
    ) =>
        state with
        {
            Status = SignalRConnectionStatus.Reconnecting,
            ReconnectAttemptCount = action.AttemptNumber,
            LastError = action.Error,
        };
}