using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events;

/// <summary>
///     Records a player claiming the yellow seat.
/// </summary>

// ReSharper disable once RedundantArgumentDefaultValue
[EventStorageName("CONNECTFOUR", "GAME", "PLAYERJOINED", MatchStorageVersions.V1)]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events.PlayerJoined")]
internal sealed record PlayerJoined
{
    /// <summary>
    ///     Gets the recorded join time.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    ///     Gets the identity of the player assigned to yellow.
    /// </summary>
    [Id(0)]
    public required string PlayerId { get; init; }
}