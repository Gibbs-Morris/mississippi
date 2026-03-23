using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Event raised when the large snapshot payload is stored.
/// </summary>
[EventStorageName("CRESCENT", "SAMPLE", "LARGESNAPSHOTSTORED")]
[GenerateSerializer]
[Alias("MississippiSamples.Crescent.L2Tests.LargeSnapshotStored")]
internal sealed record LargeSnapshotStored
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