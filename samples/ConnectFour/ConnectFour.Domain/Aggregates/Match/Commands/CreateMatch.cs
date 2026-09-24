using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Creates a new match and assigns the caller the red seat.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.CreateMatch")]
internal sealed record CreateMatch
{
    /// <summary>
    ///     Gets the fixed local-demo player identity creating the match.
    /// </summary>
    [Id(0)]
    public required string ActorId { get; init; }
}