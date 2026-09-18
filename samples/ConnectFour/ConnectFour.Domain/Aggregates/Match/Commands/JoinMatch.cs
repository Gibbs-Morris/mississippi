using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Claims the vacant yellow seat of a waiting match.
/// </summary>
/// <param name="ActorId">The fixed local-demo player identity joining the match.</param>
[GenerateCommand(Route = "join")]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.JoinMatch")]
public sealed record JoinMatch([property: Id(0)] string ActorId);