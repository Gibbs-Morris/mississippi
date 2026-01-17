using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for snapshot mapping utilities.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Snapshot Mapping")]
public sealed class SnapshotMappingTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    /// <summary>
    ///     Ensures documents map to envelopes.
    /// </summary>
    [Fact]
    public void SnapshotDocumentToEnvelopeMapperShouldMapFields()
    {
        SnapshotDocument document = new()
        {
            Data = new byte[] { 1, 2 },
            DataContentType = "ct",
            DataSizeBytes = 2,
        };
        SnapshotEnvelope envelope = new SnapshotDocumentToEnvelopeMapper().Map(document);
        Assert.Equal(new byte[] { 1, 2 }, envelope.Data.ToArray());
        Assert.Equal("ct", envelope.DataContentType);
        Assert.Equal(2, envelope.DataSizeBytes);
    }

    /// <summary>
    ///     Ensures documents map to storage models with composed stream keys.
    /// </summary>
    [Fact]
    public void SnapshotDocumentToStorageMapperShouldMapFields()
    {
        SnapshotDocument document = new()
        {
            Data = new byte[] { 3 },
            DataContentType = "json",
            DataSizeBytes = 1,
            BrookName = StreamKey.BrookName,
            ProjectionType = StreamKey.SnapshotStorageName,
            ProjectionId = StreamKey.EntityId,
            ReducersHash = StreamKey.ReducersHash,
            Version = 5,
        };
        SnapshotStorageModel storage = new SnapshotDocumentToStorageMapper().Map(document);
        Assert.Equal(document.Data, storage.Data);
        Assert.Equal(document.DataContentType, storage.DataContentType);
        Assert.Equal(1, storage.DataSizeBytes);
        Assert.Equal(StreamKey, storage.StreamKey);
        Assert.Equal(5, storage.Version);
    }

    /// <summary>
    ///     Ensures storage models map to documents and populate identifiers.
    /// </summary>
    [Fact]
    public void SnapshotStorageToDocumentMapperShouldMapFields()
    {
        SnapshotStorageModel storage = new()
        {
            StreamKey = StreamKey,
            Version = 7,
            Data = new byte[] { 9 },
            DataContentType = "xml",
            DataSizeBytes = 1,
        };
        SnapshotDocument document = new SnapshotStorageToDocumentMapper().Map(storage);
        Assert.Equal("7", document.Id);
        Assert.Equal("snapshot", document.Type);
        Assert.Equal(StreamKey.ToString(), document.SnapshotPartitionKey);
        Assert.Equal(StreamKey.BrookName, document.BrookName);
        Assert.Equal(StreamKey.SnapshotStorageName, document.ProjectionType);
        Assert.Equal(StreamKey.EntityId, document.ProjectionId);
        Assert.Equal(StreamKey.ReducersHash, document.ReducersHash);
        Assert.Equal(storage.Data, document.Data);
        Assert.Equal(storage.DataContentType, document.DataContentType);
        Assert.Equal(1, document.DataSizeBytes);
        Assert.Equal(7, document.Version);
    }

    /// <summary>
    ///     Ensures storage models map to envelopes.
    /// </summary>
    [Fact]
    public void SnapshotStorageToEnvelopeMapperShouldMapFields()
    {
        SnapshotStorageModel storage = new()
        {
            Data = new byte[] { 4, 5 },
            DataContentType = "bin",
            DataSizeBytes = 2,
            StreamKey = StreamKey,
            Version = 2,
        };
        SnapshotEnvelope envelope = new SnapshotStorageToEnvelopeMapper().Map(storage);
        Assert.Equal(new byte[] { 4, 5 }, envelope.Data.ToArray());
        Assert.Equal("bin", envelope.DataContentType);
        Assert.Equal(2, envelope.DataSizeBytes);
    }

    /// <summary>
    ///     Ensures write models map to documents.
    /// </summary>
    [Fact]
    public void SnapshotWriteModelToDocumentMapperShouldMapFields()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)7),
            DataContentType = "text",
            DataSizeBytes = 1,
        };
        SnapshotKey key = new(StreamKey, 11);
        SnapshotWriteModel writeModel = new(key, envelope);
        SnapshotDocument document = new SnapshotWriteModelToDocumentMapper().Map(writeModel);
        Assert.Equal("11", document.Id);
        Assert.Equal(StreamKey.ToString(), document.SnapshotPartitionKey);
        Assert.Equal(StreamKey.SnapshotStorageName, document.ProjectionType);
        Assert.Equal(StreamKey.EntityId, document.ProjectionId);
        Assert.Equal(StreamKey.ReducersHash, document.ReducersHash);
        Assert.Equal(11, document.Version);
        Assert.Equal(new byte[] { 7 }, document.Data);
        Assert.Equal("text", document.DataContentType);
        Assert.Equal(1, document.DataSizeBytes);
    }

    /// <summary>
    ///     Ensures write models map to storage models.
    /// </summary>
    [Fact]
    public void SnapshotWriteModelToStorageMapperShouldMapFields()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)8, (byte)9),
            DataContentType = "bytes",
            DataSizeBytes = 2,
        };
        SnapshotKey key = new(StreamKey, 3);
        SnapshotWriteModel writeModel = new(key, envelope);
        SnapshotStorageModel storage = new SnapshotWriteModelToStorageMapper().Map(writeModel);
        Assert.Equal(StreamKey, storage.StreamKey);
        Assert.Equal(3, storage.Version);
        Assert.Equal(new byte[] { 8, 9 }, storage.Data);
        Assert.Equal("bytes", storage.DataContentType);
        Assert.Equal(2, storage.DataSizeBytes);
    }
}