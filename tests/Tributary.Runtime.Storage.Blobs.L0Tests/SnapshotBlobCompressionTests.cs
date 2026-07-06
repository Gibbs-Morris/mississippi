using System;
using System.IO;
using System.Text;

using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for Blob snapshot compression helpers.
/// </summary>
public sealed class SnapshotBlobCompressionTests
{
    /// <summary>
    ///     Verifies disabled compression preserves the original bytes and metadata.
    /// </summary>
    [Fact]
    public void CompressShouldLeavePayloadUnchangedWhenCompressionIsDisabled()
    {
        byte[] payload = Encoding.UTF8.GetBytes("plain-text-payload");
        SnapshotBlobCompressionResult result = SnapshotBlobCompression.Compress(payload, false);
        Assert.Equal(SnapshotBlobCompression.None, result.Compression);
        Assert.Equal(payload, result.StoredBytes);
        Assert.Equal(payload.LongLength, result.StoredSizeBytes);
    }

    /// <summary>
    ///     Verifies decompression fails closed for unsupported compression metadata.
    /// </summary>
    [Fact]
    public void DecompressShouldThrowWhenCompressionValueIsUnsupported()
    {
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => SnapshotBlobCompression.Decompress(
            "lz4",
            [1, 2, 3, 4]));
        Assert.Contains("Unsupported", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies gzip decompression stops before exceeding the configured payload limit.
    /// </summary>
    [Fact]
    public void DecompressShouldThrowWhenPayloadExceedsMaximum()
    {
        byte[] payload = Encoding.UTF8.GetBytes(new string('x', 512));
        SnapshotBlobCompressionResult result = SnapshotBlobCompression.Compress(payload, true);
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => SnapshotBlobCompression.Decompress(
            result.Compression,
            result.StoredBytes,
            1));
        Assert.NotNull(exception);
    }

    /// <summary>
    ///     Verifies corrupt gzip payloads fail closed.
    /// </summary>
    [Fact]
    public void DecompressShouldThrowWhenPayloadIsCorrupt()
    {
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => SnapshotBlobCompression.Decompress(
            SnapshotBlobCompression.Gzip,
            [1, 2, 3, 4]));
        Assert.NotNull(exception);
    }

    /// <summary>
    ///     Verifies gzip compression round-trips payload bytes and records stored size before Base64 encoding.
    /// </summary>
    [Fact]
    public void GzipShouldRoundTripPayloadAndRecordStoredSize()
    {
        byte[] payload = Encoding.UTF8.GetBytes(new string('x', 512));
        SnapshotBlobCompressionResult result = SnapshotBlobCompression.Compress(payload, true);
        byte[] roundTripped = SnapshotBlobCompression.Decompress(result.Compression, result.StoredBytes);
        Assert.Equal(SnapshotBlobCompression.Gzip, result.Compression);
        Assert.Equal(payload, roundTripped);
        Assert.Equal(result.StoredBytes.LongLength, result.StoredSizeBytes);
    }
}