using System;
using System.Security.Cryptography;
using System.Text;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for Blob snapshot naming and stream prefixes.
/// </summary>
public sealed class SnapshotBlobNameTests
{
    private static readonly SnapshotStreamKey StreamKey = new(
        "TEST.BROOK",
        "BankAccountBalance",
        "acct/../123 with spaces",
        "reducers-hash");

    /// <summary>
    ///     Verifies the durable Blob path format for a concrete snapshot version.
    /// </summary>
    [Fact]
    public void BuildSnapshotBlobNameShouldUseResolvedStoragePath()
    {
        SnapshotKey snapshotKey = new(StreamKey, 42);
        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(StreamKey.ToString())));
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        Assert.Equal($"v1/streams/{expectedHash}/versions/00000000000000000042.json", blobName);
        Assert.DoesNotContain(StreamKey.BrookName, blobName, StringComparison.Ordinal);
        Assert.DoesNotContain(StreamKey.EntityId, blobName, StringComparison.Ordinal);
        Assert.DoesNotContain(snapshotKey.ToString(), blobName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the stream prefix is derived from the SHA-256 hash of <see cref="SnapshotStreamKey.ToString" />.
    /// </summary>
    [Fact]
    public void BuildStreamPrefixShouldHashSnapshotStreamKeyToString()
    {
        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(StreamKey.ToString())));
        string prefix = SnapshotBlobPath.BuildStreamPrefix(StreamKey);
        Assert.Equal($"v1/streams/{expectedHash}/versions/", prefix);
        Assert.DoesNotContain(StreamKey.ToString(), prefix, StringComparison.Ordinal);
    }
}