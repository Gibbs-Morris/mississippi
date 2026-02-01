using System;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Selectors for deriving values from <see cref="SignalRConnectionState" />.
/// </summary>
/// <remarks>
///     <para>
///         These selectors provide a consistent, reusable way to derive connection
///         status values across components.
///     </para>
/// </remarks>
public static class SignalRConnectionSelectors
{
    /// <summary>
    ///     Selects the current connection identifier.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The connection ID, or null if not connected.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetConnectionId(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ConnectionId;
    }

    /// <summary>
    ///     Selects the timestamp when the connection was last successfully established.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The last connected timestamp, or null if never connected.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static DateTimeOffset? GetLastConnectedAt(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LastConnectedAt;
    }

    /// <summary>
    ///     Selects the timestamp when the connection was last disconnected.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The last disconnected timestamp, or null if never disconnected.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static DateTimeOffset? GetLastDisconnectedAt(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LastDisconnectedAt;
    }

    /// <summary>
    ///     Selects the last error message that occurred.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The last error message, or null if no error.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetLastError(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LastError;
    }

    /// <summary>
    ///     Selects the timestamp when the last message was received.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The last message timestamp, or null if no message received.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static DateTimeOffset? GetLastMessageReceivedAt(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LastMessageReceivedAt;
    }

    /// <summary>
    ///     Selects the current reconnection attempt count.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The number of reconnection attempts.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static int GetReconnectAttemptCount(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ReconnectAttemptCount;
    }

    /// <summary>
    ///     Selects the current connection status.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>The current connection status.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static SignalRConnectionStatus GetStatus(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Status;
    }

    /// <summary>
    ///     Selects whether the SignalR connection is currently connected.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>True if connected; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool IsConnected(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Status == SignalRConnectionStatus.Connected;
    }

    /// <summary>
    ///     Selects whether the SignalR connection is currently disconnected.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>True if not connected; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool IsDisconnected(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Status != SignalRConnectionStatus.Connected;
    }

    /// <summary>
    ///     Selects whether the SignalR connection is currently reconnecting.
    /// </summary>
    /// <param name="state">The SignalR connection state.</param>
    /// <returns>True if reconnecting; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool IsReconnecting(
        SignalRConnectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Status == SignalRConnectionStatus.Reconnecting;
    }
}