using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match;

/// <summary>
///     Describes the lifecycle phase of a match.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.MatchPhase")]
internal enum MatchPhase
{
    /// <summary>
    ///     No match has been created for the aggregate identity.
    /// </summary>
    NotCreated,

    /// <summary>
    ///     The first player is waiting for an opponent.
    /// </summary>
    WaitingForOpponent,

    /// <summary>
    ///     Both player seats are occupied and moves are accepted.
    /// </summary>
    InProgress,

    /// <summary>
    ///     A player has completed a winning line.
    /// </summary>
    Won,

    /// <summary>
    ///     The board is full without a winning line.
    /// </summary>
    Draw,

    /// <summary>
    ///     A player forfeited an in-progress match.
    /// </summary>
    Forfeited,
}