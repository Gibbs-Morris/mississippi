using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Mapping;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for Blob snapshot mapping utilities.
/// </summary>
public sealed class SnapshotMappingTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    /// <summary>
    ///     Ensures storage models map to envelopes including reducer hash.
    /// </summary>
    [Fact]
    public void SnapshotStorageToEnvelopeMapperShouldMapFields()
    {
        SnapshotBlobStorageModel storage = new(
            StreamKey,
            2,
            new byte[] { 4, 5 },
            "bin",
            2);
        SnapshotEnvelope envelope = new SnapshotStorageToEnvelopeMapper().Map(storage);
        Assert.Equal(new byte[] { 4, 5 }, envelope.Data.ToArray());
        Assert.Equal("bin", envelope.DataContentType);
        Assert.Equal(2, envelope.DataSizeBytes);
        Assert.Equal("hash", envelope.ReducerHash);
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
            ReducerHash = "hash",
        };
        SnapshotKey key = new(StreamKey, 3);
        SnapshotWriteModel writeModel = new(key, envelope);
        SnapshotBlobStorageModel storage = new SnapshotWriteModelToStorageMapper().Map(writeModel);
        Assert.Equal(StreamKey, storage.StreamKey);
        Assert.Equal(3, storage.Version);
        Assert.Equal(new byte[] { 8, 9 }, storage.Data);
        Assert.Equal("bytes", storage.DataContentType);
        Assert.Equal(2, storage.DataSizeBytes);
    }
}
