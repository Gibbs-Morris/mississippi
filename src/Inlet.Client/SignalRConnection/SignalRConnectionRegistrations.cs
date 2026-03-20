using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Extension methods for registering the SignalR connection feature.
/// </summary>
public static class SignalRConnectionRegistrations
{
    /// <summary>
    ///     Adds the SignalR connection feature to the Reservoir builder.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddSignalRConnectionFeature(
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        reservoir.AddFeature<SignalRConnectionState>(feature => feature
            .AddReducer<SignalRConnectionState, SignalRConnectingAction>(SignalRConnectionReducers.OnConnecting)
            .AddReducer<SignalRConnectionState, SignalRConnectedAction>(SignalRConnectionReducers.OnConnected)
            .AddReducer<SignalRConnectionState, SignalRReconnectingAction>(SignalRConnectionReducers.OnReconnecting)
            .AddReducer<SignalRConnectionState, SignalRReconnectedAction>(SignalRConnectionReducers.OnReconnected)
            .AddReducer<SignalRConnectionState, SignalRDisconnectedAction>(SignalRConnectionReducers.OnDisconnected)
            .AddReducer<SignalRConnectionState, SignalRMessageReceivedAction>(
                SignalRConnectionReducers.OnMessageReceived));
        return reservoir;
    }

    /// <summary>
    ///     Adds the SignalR connection feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
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
    public static IServiceCollection AddSignalRConnectionFeature(
        this IServiceCollection services
    )
    {
        ReservoirBuilder reservoirBuilder = new(services);
        reservoirBuilder.AddSignalRConnectionFeature();
        return services;
    }
}