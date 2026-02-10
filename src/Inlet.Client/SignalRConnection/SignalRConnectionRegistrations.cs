using System;

using Mississippi.Reservoir.Abstractions.Builders;


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
    /// <returns>The Reservoir builder for chaining.</returns>
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
        builder.AddFeature<SignalRConnectionState>(featureBuilder =>
        {
            // Register reducers for each action
            featureBuilder.AddReducer<SignalRConnectingAction>(SignalRConnectionReducers.OnConnecting);
            featureBuilder.AddReducer<SignalRConnectedAction>(SignalRConnectionReducers.OnConnected);
            featureBuilder.AddReducer<SignalRReconnectingAction>(SignalRConnectionReducers.OnReconnecting);
            featureBuilder.AddReducer<SignalRReconnectedAction>(SignalRConnectionReducers.OnReconnected);
            featureBuilder.AddReducer<SignalRDisconnectedAction>(SignalRConnectionReducers.OnDisconnected);
            featureBuilder.AddReducer<SignalRMessageReceivedAction>(SignalRConnectionReducers.OnMessageReceived);
        });
        return builder;
    }
}