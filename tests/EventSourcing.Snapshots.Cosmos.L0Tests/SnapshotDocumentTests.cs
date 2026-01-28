
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotDocument" />.
/// </summary>
public sealed class SnapshotDocumentTests
{
    /// <summary>
    ///     Verifies default property values.
    /// </summary>
    [Fact]
    public void DefaultValuesShouldBeEmptyAndZeroed()
    {
        SnapshotDocument document = new();
        Assert.Empty(document.Data);
        Assert.Equal(string.Empty, document.DataContentType);
        Assert.Equal(string.Empty, document.Id);
        Assert.Equal(string.Empty, document.ProjectionId);
        Assert.Equal(string.Empty, document.ProjectionType);
        Assert.Equal(string.Empty, document.ReducersHash);
        Assert.Equal(string.Empty, document.SnapshotPartitionKey);
        Assert.Equal("snapshot", document.Type);
        Assert.Equal(0, document.Version);
    }

    /// <summary>
    ///     Verifies properties can be set.
    /// </summary>
    [Fact]
    public void PropertiesShouldBeSettable()
    {
        byte[] data = new byte[] { 1, 2, 3 };
        SnapshotDocument document = new()
        {
            Data = data,
            DataContentType = "application/json",
            Id = "42",
            ProjectionId = "proj-id",
            ProjectionType = "proj-type",
            ReducersHash = "hash123",
            SnapshotPartitionKey = "pk-value",
            Type = "custom-type",
            Version = 100,
        };
        Assert.Same(data, document.Data);
        Assert.Equal("application/json", document.DataContentType);
        Assert.Equal("42", document.Id);
        Assert.Equal("proj-id", document.ProjectionId);
        Assert.Equal("proj-type", document.ProjectionType);
        Assert.Equal("hash123", document.ReducersHash);
        Assert.Equal("pk-value", document.SnapshotPartitionKey);
        Assert.Equal("custom-type", document.Type);
        Assert.Equal(100, document.Version);
    }
}