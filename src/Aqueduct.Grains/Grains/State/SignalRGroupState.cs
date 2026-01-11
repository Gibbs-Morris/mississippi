using System.Collections.Immutable;

using Mississippi.Aqueduct.Abstractions.Grains;

using Orleans;


namespace Mississippi.Aqueduct.Grains.Grains.State;

/// <summary>
///     Persisted state for a group grain implementing <see cref="ISignalRGroupGrain" />.
/// </summary>
/// <remarks>
///     Tracks group membership including the hub name and set of connection identifiers.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Aqueduct.SignalRGroupState")]
public sealed record SignalRGroupState
{
    /// <summary>
    ///     Gets the set of connection identifiers in this group.
    /// </summary>
    [Id(2)]
    public ImmutableHashSet<string> ConnectionIds { get; init; } = [];

    /// <summary>
    ///     Gets the name of the group.
    /// </summary>
    [Id(0)]
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the SignalR hub.
    /// </summary>
    [Id(1)]
    public string HubName { get; init; } = string.Empty;
}