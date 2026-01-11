using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;


namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Defines a factory for resolving Orleans SignalR grains by strongly-typed keys.
/// </summary>
/// <remarks>
///     <para>
///         This factory encapsulates the grain key formatting, so consumers
///         don't need to know the internal key structure. Use this factory
///         instead of calling <c>IGrainFactory.GetGrain&lt;T&gt;(string key)</c> directly.
///     </para>
/// </remarks>
public interface ISignalRGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="ISignalRClientGrain" /> for the specified client key.
    /// </summary>
    /// <param name="clientKey">The key identifying the SignalR client (hub name and connection ID).</param>
    /// <returns>An <see cref="ISignalRClientGrain" /> instance for the client.</returns>
    ISignalRClientGrain GetClientGrain(
        SignalRClientKey clientKey
    );

    /// <summary>
    ///     Retrieves an <see cref="ISignalRClientGrain" /> for the specified hub and connection.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="connectionId">The SignalR connection identifier.</param>
    /// <returns>An <see cref="ISignalRClientGrain" /> instance for the client.</returns>
    ISignalRClientGrain GetClientGrain(
        string hubName,
        string connectionId
    );

    /// <summary>
    ///     Retrieves an <see cref="ISignalRGroupGrain" /> for the specified group key.
    /// </summary>
    /// <param name="groupKey">The key identifying the SignalR group (hub name and group name).</param>
    /// <returns>An <see cref="ISignalRGroupGrain" /> instance for the group.</returns>
    ISignalRGroupGrain GetGroupGrain(
        SignalRGroupKey groupKey
    );

    /// <summary>
    ///     Retrieves an <see cref="ISignalRGroupGrain" /> for the specified hub and group.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="groupName">The name of the SignalR group.</param>
    /// <returns>An <see cref="ISignalRGroupGrain" /> instance for the group.</returns>
    ISignalRGroupGrain GetGroupGrain(
        string hubName,
        string groupName
    );

    /// <summary>
    ///     Retrieves an <see cref="ISignalRServerDirectoryGrain" /> for the specified key.
    /// </summary>
    /// <param name="directoryKey">The key identifying the server directory (typically "default").</param>
    /// <returns>An <see cref="ISignalRServerDirectoryGrain" /> instance for the directory.</returns>
    ISignalRServerDirectoryGrain GetServerDirectoryGrain(
        SignalRServerDirectoryKey directoryKey
    );

    /// <summary>
    ///     Retrieves the default <see cref="ISignalRServerDirectoryGrain" /> singleton.
    /// </summary>
    /// <returns>An <see cref="ISignalRServerDirectoryGrain" /> instance for the default directory.</returns>
    ISignalRServerDirectoryGrain GetServerDirectoryGrain();
}
