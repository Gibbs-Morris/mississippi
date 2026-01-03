using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Orleans grain interface that tracks group membership for a SignalR group.
/// </summary>
/// <remarks>
///     <para>
///         This aggregate grain persists the set of connections belonging to a group
///         and supports broadcasting messages to all group members. The grain key
///         is formatted as "{HubName}:{GroupName}".
///     </para>
///     <para>
///         When sending to a group, this grain fans out to individual
///         <see cref="IUxClientGrain" /> instances for each connection.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.IUxGroupGrain")]
public interface IUxGroupGrain : IGrainWithStringKey
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
    /// <param name="methodName">The SignalR hub method name to invoke.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("SendAllAsync")]
    Task SendAllAsync(
        string methodName,
        object?[] args
    );
}