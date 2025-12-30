using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Test brook definition for snapshot cache grain tests.
/// </summary>
internal sealed class SnapshotCacheGrainTestBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "TEST.SNAPSHOTS.TestBrook";
}