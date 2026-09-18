using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Creates a new match and assigns the caller the red seat.
/// </summary>
/// <param name="ActorId">The fixed local-demo player identity creating the match.</param>
[GenerateCommand(Route = "create")]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.CreateMatch")]
public sealed record CreateMatch([property: Id(0)] string ActorId);