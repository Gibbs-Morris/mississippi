using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Aggregate used by the Blob L2 trust slice to persist a materially large snapshot payload.
/// </summary>
[BrookName("CRESCENT", "SAMPLE", "LARGESNAPSHOT")]
[SnapshotStorageName("CRESCENT", "SAMPLE", "LARGESNAPSHOTSTATE")]
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.LargeSnapshotAggregate")]
internal sealed record LargeSnapshotAggregate
{
    /// <summary>
    ///     Gets the large snapshot payload.
    /// </summary>
    [Id(0)]
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the logical marker used to prove restart survival.
    /// </summary>
    [Id(1)]
    public string Marker { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the logical payload size recorded by the reducer.
    /// </summary>
    [Id(2)]
    public int PayloadLength { get; init; }
}