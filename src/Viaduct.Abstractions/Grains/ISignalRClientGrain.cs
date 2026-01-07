// <copyright file="ISignalRClientGrain.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Viaduct.Abstractions.Grains;

/// <summary>
///     Orleans grain interface that tracks a single SignalR connection.
/// </summary>
/// <remarks>
///     <para>
///         This grain persists connection state and routes messages
///         to the appropriate SignalR client. The grain key is formatted as
///         "{HubName}:{ConnectionId}".
///     </para>
///     <para>
///         When a grain's server host becomes unavailable, the connection
///         state helps with graceful cleanup and recovery.
///     </para>
/// </remarks>
[Alias("Mississippi.Viaduct.ISignalRClientGrain")]
public interface ISignalRClientGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Registers the connection with the specified hub and server.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="serverId">The unique identifier of the hosting server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("ConnectAsync")]
    Task ConnectAsync(
        string hubName,
        string serverId
    );

    /// <summary>
    ///     Disconnects and cleans up the client state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("DisconnectAsync")]
    Task DisconnectAsync();

    /// <summary>
    ///     Gets the server identifier hosting this connection.
    /// </summary>
    /// <returns>
    ///     The server identifier, or <c>null</c> if not connected.
    /// </returns>
    [Alias("GetServerIdAsync")]
    Task<string?> GetServerIdAsync();

    /// <summary>
    ///     Sends a message to this client via the Orleans stream backplane.
    /// </summary>
    /// <param name="method">The SignalR hub method name.</param>
    /// <param name="args">The arguments to pass to the hub method.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("SendMessageAsync")]
    Task SendMessageAsync(
        string method,
        ImmutableArray<object?> args
    );
}