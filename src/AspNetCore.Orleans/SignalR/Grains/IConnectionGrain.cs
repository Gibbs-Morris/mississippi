namespace Mississippi.AspNetCore.Orleans.SignalR.Grains;

using System.Threading.Tasks;
using global::Orleans;

/// <summary>
/// Grain interface for managing SignalR connection state.
/// </summary>
public interface IConnectionGrain : IGrainWithStringKey
{
    /// <summary>
    /// Registers a connection with user and groups.
    /// </summary>
    /// <param name="userId">The user identifier, if any.</param>
    /// <param name="groups">The groups the connection belongs to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterAsync(string? userId, string[] groups);

    /// <summary>
    /// Gets the connection information.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation containing connection data,
    /// or null if not found.
    /// </returns>
    Task<ConnectionData?> GetAsync();

    /// <summary>
    /// Adds the connection to a group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddToGroupAsync(string groupName);

    /// <summary>
    /// Removes the connection from a group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveFromGroupAsync(string groupName);

    /// <summary>
    /// Unregisters the connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterAsync();
}
