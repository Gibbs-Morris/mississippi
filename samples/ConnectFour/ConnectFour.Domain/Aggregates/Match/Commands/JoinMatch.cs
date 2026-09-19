using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Claims the vacant yellow seat of a waiting match.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.JoinMatch")]
internal sealed record JoinMatch
{
    /// <summary>
    ///     Gets the fixed local-demo player identity joining the match.
    /// </summary>
    [Id(0)]
    public required string ActorId { get; init; }
}