using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match;

/// <summary>
///     Identifies the occupant of a Connect Four board cell.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.DiscColor")]
internal enum DiscColor
{
    /// <summary>
    ///     The cell is empty.
    /// </summary>
    Empty,

    /// <summary>
    ///     The cell contains a red player's disc.
    /// </summary>
    Red,

    /// <summary>
    ///     The cell contains a yellow player's disc.
    /// </summary>
    Yellow,
}