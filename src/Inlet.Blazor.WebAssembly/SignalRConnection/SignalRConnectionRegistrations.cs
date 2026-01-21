using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;
using Mississippi.Reservoir;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Extension methods for registering the SignalR connection feature.
/// </summary>
public static class SignalRConnectionRegistrations
{
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
        // Register reducers for each action
        services.AddReducer<SignalRConnectingAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnConnecting);

        services.AddReducer<SignalRConnectedAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnConnected);

        services.AddReducer<SignalRReconnectingAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnReconnecting);

        services.AddReducer<SignalRReconnectedAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnReconnected);

        services.AddReducer<SignalRDisconnectedAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnDisconnected);

        services.AddReducer<SignalRMessageReceivedAction, SignalRConnectionState>(
            SignalRConnectionReducers.OnMessageReceived);

        return services;
    }
}
