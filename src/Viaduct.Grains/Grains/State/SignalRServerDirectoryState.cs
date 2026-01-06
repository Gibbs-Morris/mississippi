using System.Collections.Immutable;

using Orleans;


namespace Mississippi.Viaduct.Grains.State;

/// <summary>
///     Persisted state for a server directory grain implementing <see cref="ISignalRServerDirectoryGrain" />.
/// </summary>
/// <remarks>
///     Tracks all active SignalR servers and their heartbeat information.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Viaduct.SignalRServerDirectoryState")]
public sealed record SignalRServerDirectoryState
{
    /// <summary>
    ///     Gets the dictionary of active servers keyed by server identifier.
    /// </summary>
    [Id(0)]
    public ImmutableDictionary<string, SignalRServerInfo> ActiveServers { get; init; } =
        ImmutableDictionary<string, SignalRServerInfo>.Empty;
}