using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Drops a disc into a selected column when the caller has the current turn.
/// </summary>
/// <param name="ActorId">The fixed local-demo player identity making the move.</param>
/// <param name="Column">The zero-based column receiving the disc.</param>
/// <param name="ExpectedMoveNumber">The number of accepted moves observed before this request.</param>
[GenerateCommand(Route = "drop-disc")]
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.DropDisc")]
public sealed record DropDisc(
    [property: Id(0)] string ActorId,
    [property: Id(1)] int Column,
    [property: Id(2)] int ExpectedMoveNumber
);