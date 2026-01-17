using System.Collections.Generic;

using Microsoft.AspNetCore.SignalR;


namespace Mississippi.Aqueduct;

/// <summary>
///     Defines a registry for tracking local SignalR hub connections.
/// </summary>
/// <remarks>
///     <para>
///         This registry maintains an in-memory collection of active
///         <see cref="HubConnectionContext" /> instances for the current server.
///         It enables efficient lookup and enumeration of local connections
///         without coupling to the Orleans backplane.
///     </para>
///     <para>
///         The registry is intended to be scoped per hub type and is used
///         by the <see cref="AqueductHubLifetimeManager{THub}" /> to manage
///         connections local to this server instance.
///     </para>
/// </remarks>
public interface IConnectionRegistry
{
    /// <summary>
    ///     Gets the current number of active connections in the registry.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Gets all active connections currently in the registry.
    /// </summary>
    /// <returns>An enumerable of all active <see cref="HubConnectionContext" /> instances.</returns>
    IEnumerable<HubConnectionContext> GetAll();

    /// <summary>
    ///     Attempts to retrieve a connection by its identifier.
    /// </summary>
    /// <param name="connectionId">The SignalR connection identifier.</param>
    /// <returns>
    ///     The <see cref="HubConnectionContext" /> if found; otherwise, <c>null</c>.
    /// </returns>
    HubConnectionContext? GetConnection(
        string connectionId
    );

    /// <summary>
    ///     Attempts to add a connection to the registry.
    /// </summary>
    /// <param name="connectionId">The SignalR connection identifier.</param>
    /// <param name="connection">The hub connection context to add.</param>
    /// <returns>
    ///     <c>true</c> if the connection was added successfully;
    ///     <c>false</c> if a connection with the same identifier already exists.
    /// </returns>
    bool TryAdd(
        string connectionId,
        HubConnectionContext connection
    );

    /// <summary>
    ///     Attempts to remove a connection from the registry.
    /// </summary>
    /// <param name="connectionId">The SignalR connection identifier to remove.</param>
    /// <returns>
    ///     <c>true</c> if the connection was removed successfully;
    ///     <c>false</c> if no connection with the specified identifier exists.
    /// </returns>
    bool TryRemove(
        string connectionId
    );
}