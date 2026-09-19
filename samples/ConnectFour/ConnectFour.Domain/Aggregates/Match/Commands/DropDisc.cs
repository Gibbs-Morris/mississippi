using Orleans;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands;

/// <summary>
///     Drops a disc into a selected column when the caller has the current turn.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.ConnectFour.Domain.Aggregates.Match.Commands.DropDisc")]
internal sealed record DropDisc
{
    /// <summary>
    ///     Gets the fixed local-demo player identity making the move.
    /// </summary>
    [Id(0)]
    public required string ActorId { get; init; }

    /// <summary>
    ///     Gets the zero-based column receiving the disc.
    /// </summary>
    [Id(1)]
    public required int Column { get; init; }

    /// <summary>
    ///     Gets the number of accepted moves observed before this request.
    /// </summary>
    [Id(2)]
    public required int ExpectedMoveNumber { get; init; }
}