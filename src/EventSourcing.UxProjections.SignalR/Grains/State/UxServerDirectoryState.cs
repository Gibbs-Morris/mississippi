using System.Collections.Immutable;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;

/// <summary>
///     Persisted state for a server directory grain implementing <see cref="IUxServerDirectoryGrain" />.
/// </summary>
/// <remarks>
///     Tracks all active SignalR servers and their heartbeat information.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxServerDirectoryState")]
public sealed record UxServerDirectoryState
{
    /// <summary>
    ///     Gets the dictionary of active servers keyed by server identifier.
    /// </summary>
    [Id(0)]
    public ImmutableDictionary<string, ServerInfo> ActiveServers { get; init; } =
        ImmutableDictionary<string, ServerInfo>.Empty;
}