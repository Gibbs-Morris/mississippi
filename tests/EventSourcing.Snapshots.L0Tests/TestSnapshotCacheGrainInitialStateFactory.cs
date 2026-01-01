using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Test initial state factory for <see cref="SnapshotCacheGrainTestState" />.
/// </summary>
internal sealed class TestSnapshotCacheGrainInitialStateFactory : IInitialStateFactory<SnapshotCacheGrainTestState>
{
    /// <inheritdoc />
    public SnapshotCacheGrainTestState Create() =>
        new()
        {
            Value = 0,
        };
}