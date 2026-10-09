using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match;

/// <summary>
///     Identifies why a match reached a terminal phase.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.MatchEndReason")]
internal enum MatchEndReason
{
    /// <summary>
    ///     The match has no terminal result.
    /// </summary>
    None,

    /// <summary>
    ///     A player completed a winning line.
    /// </summary>
    WinningLine,

    /// <summary>
    ///     The board became full without a winner.
    /// </summary>
    BoardFull,

    /// <summary>
    ///     A player forfeited the match.
    /// </summary>
    Forfeit,
}