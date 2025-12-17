namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Test state for snapshot cache grain tests.
/// </summary>
internal sealed record SnapshotCacheGrainTestState
{
    /// <summary>
    ///     Gets the value.
    /// </summary>
    public int Value { get; init; }
}