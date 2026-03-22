using System;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Extension methods for registering the SignalR connection feature.
/// </summary>
public static class SignalRConnectionRegistrations
{
    /// <summary>
    ///     Adds the SignalR connection feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This registers the <see cref="SignalRConnectionState" /> feature state
    ///         and all reducers for connection lifecycle actions.
    ///     </para>
    ///     <para>
    ///         Connection state actions are dispatched directly by the
    ///         <c>HubConnectionProvider</c> when SignalR events occur.
    ///     </para>
    ///     <para>
    ///         This method is called automatically by <c>AddInletBlazorSignalR</c>.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddSignalRConnectionFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeatureState<SignalRConnectionState>(feature => feature
            .AddReducer<SignalRConnectingAction>(SignalRConnectionReducers.OnConnecting)
            .AddReducer<SignalRConnectedAction>(SignalRConnectionReducers.OnConnected)
            .AddReducer<SignalRReconnectingAction>(SignalRConnectionReducers.OnReconnecting)
            .AddReducer<SignalRReconnectedAction>(SignalRConnectionReducers.OnReconnected)
            .AddReducer<SignalRDisconnectedAction>(SignalRConnectionReducers.OnDisconnected)
            .AddReducer<SignalRMessageReceivedAction>(SignalRConnectionReducers.OnMessageReceived));
        return builder;
    }
}