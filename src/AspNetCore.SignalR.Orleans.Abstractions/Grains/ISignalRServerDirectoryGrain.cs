// <copyright file="ISignalRServerDirectoryGrain.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.AspNetCore.SignalR.Orleans.Grains;

/// <summary>
///     Orleans grain interface that tracks active SignalR servers for failure detection.
/// </summary>
/// <remarks>
///     <para>
///         This grain maintains a registry of active SignalR servers and their
///         heartbeat status. It enables detection of dead servers so that
///         orphaned connections can be cleaned up.
///     </para>
///     <para>
///         There is typically one instance of this grain per deployment, keyed
///         by a constant value (e.g., "default").
///     </para>
/// </remarks>
[Alias("Mississippi.AspNetCore.SignalR.Orleans.ISignalRServerDirectoryGrain")]
public interface ISignalRServerDirectoryGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Gets a list of servers that have not sent a heartbeat within the timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration for considering a server dead.</param>
    /// <returns>An immutable list of dead server identifiers.</returns>
    [Alias("GetDeadServersAsync")]
    Task<ImmutableList<string>> GetDeadServersAsync(
        TimeSpan timeout
    );

    /// <summary>
    ///     Updates the heartbeat timestamp and connection count for a server.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server.</param>
    /// <param name="connectionCount">The current number of active connections.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("HeartbeatAsync")]
    Task HeartbeatAsync(
        string serverId,
        int connectionCount
    );

    /// <summary>
    ///     Registers a server as active in the directory.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("RegisterServerAsync")]
    Task RegisterServerAsync(
        string serverId
    );

    /// <summary>
    ///     Unregisters a server from the directory.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("UnregisterServerAsync")]
    Task UnregisterServerAsync(
        string serverId
    );
}