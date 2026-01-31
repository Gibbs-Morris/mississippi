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
}
