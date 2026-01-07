// <copyright file="ISignalRGroupGrain.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Aqueduct.Abstractions.Grains;

/// <summary>
///     Orleans grain interface that tracks group membership for a SignalR group.
/// </summary>
/// <remarks>
///     <para>
///         This grain persists the set of connections belonging to a group
///         and supports broadcasting messages to all group members. The grain key
///         is formatted as "{HubName}:{GroupName}".
///     </para>
///     <para>
///         When sending to a group, this grain fans out to individual
///         <see cref="ISignalRClientGrain" /> instances for each connection.
///     </para>
/// </remarks>
[Alias("Mississippi.Aqueduct.ISignalRGroupGrain")]
public interface ISignalRGroupGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Adds a connection to this group.
    /// </summary>
    /// <param name="connectionId">The SignalR connection identifier to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("AddConnectionAsync")]
    Task AddConnectionAsync(
        string connectionId
    );

    /// <summary>
    ///     Gets all connection identifiers in this group.
    /// </summary>
    /// <returns>An immutable set of connection identifiers.</returns>
    [Alias("GetConnectionsAsync")]
    Task<ImmutableHashSet<string>> GetConnectionsAsync();

    /// <summary>
    ///     Removes a connection from this group.
    /// </summary>
    /// <param name="connectionId">The SignalR connection identifier to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("RemoveConnectionAsync")]
    Task RemoveConnectionAsync(
        string connectionId
    );

    /// <summary>
    ///     Sends a message to all connections in this group.
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