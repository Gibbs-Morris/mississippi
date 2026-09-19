using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Forfeits an in-progress match for a seated player.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.ForfeitMatch")]
internal sealed record ForfeitMatch
{
    /// <summary>
    ///     Gets the seated player identity forfeiting the match.
    /// </summary>
    [Id(0)]
    public required string ActorId { get; init; }
}