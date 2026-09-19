using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events;

/// <summary>
///     Records creation of a match by its first player.
/// </summary>

// ReSharper disable once RedundantArgumentDefaultValue
[EventStorageName("CONNECTFOUR", "GAME", "MATCHCREATED", MatchStorageVersions.V1)]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events.MatchCreated")]
internal sealed record MatchCreated
{
    /// <summary>
    ///     Gets the recorded creation time.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the identity of the player assigned to red.
    /// </summary>
    [Id(0)]
    public required string PlayerId { get; init; }
}