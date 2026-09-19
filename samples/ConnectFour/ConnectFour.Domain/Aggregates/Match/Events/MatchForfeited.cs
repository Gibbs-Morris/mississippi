using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events;

/// <summary>
///     Records a seated player forfeiting an in-progress match.
/// </summary>
[EventStorageName("CONNECTFOUR", "GAME", "MATCHFORFEITED", MatchStorageVersions.V1)]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events.MatchForfeited")]
internal sealed record MatchForfeited
{
    /// <summary>
    ///     Gets the identity of the player who forfeited.
    /// </summary>
    [Id(0)]
    public required string ForfeitingPlayerId { get; init; }

    /// <summary>
    ///     Gets the recorded forfeit time.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    ///     Gets the identity of the player who wins by forfeit.
    /// </summary>
    [Id(1)]
    public required string WinnerPlayerId { get; init; }
}