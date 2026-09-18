using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Forfeits an in-progress match for a seated player.
/// </summary>
/// <param name="ActorId">The seated player identity forfeiting the match.</param>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.ForfeitMatch")]
internal sealed record ForfeitMatch([property: Id(0)] string ActorId);
