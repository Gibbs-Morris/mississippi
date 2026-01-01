using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Test brook definition for snapshot cache grain tests.
/// </summary>
[BrookName("TEST", "SNAPSHOTS", "TESTBROOK")]
internal sealed class SnapshotCacheGrainTestBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "TEST.SNAPSHOTS.TESTBROOK";
}