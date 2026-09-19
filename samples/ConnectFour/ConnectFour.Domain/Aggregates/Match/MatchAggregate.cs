using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;

using MississippiSamples.ConnectFour.Domain.Aggregates.Match.Board;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match;

/// <summary>
///     Event-sourced state for one Connect Four match.
/// </summary>
[BrookName("CONNECTFOUR", "GAME", "MATCH")]
[SnapshotStorageName("CONNECTFOUR", "GAME", "MATCHSTATE")]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.MatchAggregate")]
internal sealed record MatchAggregate
{
    /// <summary>
    ///     Gets the board cells in row-major order, with row zero at the bottom.
    /// </summary>
    [Id(3)]
    public ImmutableArray<DiscColor> Board { get; init; } = ConnectFourBoard.Empty;

    /// <summary>
    ///     Gets the time when the match reached a terminal phase.
    /// </summary>
    [Id(11)]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the time when the first player created the match.
    /// </summary>
    [Id(9)]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    ///     Gets the current player color while the match is in progress.
    /// </summary>
    [Id(5)]
    public DiscColor CurrentTurn { get; init; }

    /// <summary>
    ///     Gets the reason the match ended, if it is terminal.
    /// </summary>
    [Id(8)]
    public MatchEndReason EndReason { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a match has been created for this identity.
    /// </summary>
    [Id(0)]
    public bool IsCreated { get; init; }

    /// <summary>
    ///     Gets the number of accepted disc drops.
    /// </summary>
    [Id(4)]
    public int MoveCount { get; init; }

    /// <summary>
    ///     Gets the current lifecycle phase.
    /// </summary>
    [Id(6)]
    public MatchPhase Phase { get; init; }

    /// <summary>
    ///     Gets the player occupying the red seat.
    /// </summary>
    [Id(1)]
    public string? RedPlayerId { get; init; }

    /// <summary>
    ///     Gets the time when the second player joined the match.
    /// </summary>
    [Id(10)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets the player who won, if the match has a winner.
    /// </summary>
    [Id(7)]
    public string? WinnerPlayerId { get; init; }

    /// <summary>
    ///     Gets the winning cell indexes, when the match was won.
    /// </summary>
    [Id(12)]
    public ImmutableArray<int> WinningCells { get; init; } = [];

    /// <summary>
    ///     Gets the player occupying the yellow seat.
    /// </summary>
    [Id(2)]
    public string? YellowPlayerId { get; init; }
}