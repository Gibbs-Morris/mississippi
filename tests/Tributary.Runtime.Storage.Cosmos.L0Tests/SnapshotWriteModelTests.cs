using System.Collections.Immutable;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotWriteModel" />.
/// </summary>
public sealed class SnapshotWriteModelTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    /// <summary>
    ///     Ensures two models with different keys are not equal.
    /// </summary>
    [Fact]
    public void SnapshotWriteModelShouldNotBeEqualWithDifferentKey()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)4),
            DataContentType = "binary",
        };
        SnapshotKey key1 = new(StreamKey, 5);
        SnapshotKey key2 = new(StreamKey, 6);
        SnapshotWriteModel model1 = new(key1, envelope);
        SnapshotWriteModel model2 = new(key2, envelope);
        Assert.NotEqual(model1, model2);
    }

    /// <summary>
    ///     Ensures the record stores key and snapshot correctly.
    /// </summary>
    [Fact]
    public void SnapshotWriteModelShouldStoreKeyAndSnapshot()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)1, (byte)2),
            DataContentType = "application/json",
        };
        SnapshotKey key = new(StreamKey, 42);
        SnapshotWriteModel model = new(key, envelope);
        Assert.Equal(key, model.Key);
        Assert.Same(envelope, model.Snapshot);
    }

    /// <summary>
    ///     Ensures two models with same values are equal.
    /// </summary>
    [Fact]
    public void SnapshotWriteModelShouldSupportValueEquality()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)3),
            DataContentType = "text/plain",
        };
        SnapshotKey key = new(StreamKey, 10);
        SnapshotWriteModel model1 = new(key, envelope);
        SnapshotWriteModel model2 = new(key, envelope);
        Assert.Equal(model1, model2);
        Assert.Equal(model1.GetHashCode(), model2.GetHashCode());
    }
}