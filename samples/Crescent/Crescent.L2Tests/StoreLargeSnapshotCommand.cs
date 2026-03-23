using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Command that replaces the large aggregate snapshot payload.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.StoreLargeSnapshotCommand")]
internal sealed record StoreLargeSnapshotCommand
{
    /// <summary>
    ///     Gets the marker that must survive restart.
    /// </summary>
    [Id(0)]
    public string Marker { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the large payload to persist into the snapshot.
    /// </summary>
    [Id(1)]
    public string Payload { get; init; } = string.Empty;
}