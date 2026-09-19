using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events;

/// <summary>
///     Records one accepted disc placement.
/// </summary>
[EventStorageName("CONNECTFOUR", "GAME", "DISCDROPPED", version: MatchStorageVersions.V1)]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Events.DiscDropped")]
internal sealed record DiscDropped
{
    /// <summary>
    ///     Gets the color assigned to the player for this move.
    /// </summary>
    [Id(0)]
    public required DiscColor Color { get; init; }

    /// <summary>
    ///     Gets the zero-based column receiving the disc.
    /// </summary>
    [Id(2)]
    public required int Column { get; init; }

    /// <summary>
    ///     Gets the accepted one-based move number.
    /// </summary>
    [Id(4)]
    public required int MoveNumber { get; init; }

    /// <summary>
    ///     Gets the recorded move time.
    /// </summary>
    [Id(6)]
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    ///     Gets the identity of the player making the move.
    /// </summary>
    [Id(1)]
    public required string PlayerId { get; init; }

    /// <summary>
    ///     Gets the zero-based row selected by gravity.
    /// </summary>
    [Id(3)]
    public required int Row { get; init; }

    /// <summary>
    ///     Gets the winning cell indexes created by this move, if any.
    /// </summary>
    [Id(5)]
    public required ImmutableArray<int> WinningCells { get; init; }
}
