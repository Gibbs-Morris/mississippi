using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobPathBuilder" />.
/// </summary>
public sealed class SnapshotBlobPathBuilderTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "Projection Type", "entity/id", "hash==");

    /// <summary>
    ///     Ensures the blob name is deterministic and escapes individual path segments.
    /// </summary>
    [Fact]
    public void BuildBlobNameShouldEscapePathSegments()
    {
        SnapshotKey snapshotKey = new(StreamKey, 12);
        string blobName = SnapshotBlobPathBuilder.BuildBlobName(snapshotKey);
        Assert.Equal("TEST.BROOK/Projection%20Type/entity%2Fid/hash%3D%3D/12.snapshot", blobName);
    }

    /// <summary>
    ///     Ensures the stream prefix is stable for all versions within the stream.
    /// </summary>
    [Fact]
    public void BuildStreamPrefixShouldOmitVersion()
    {
        Assert.Equal(
            "TEST.BROOK/Projection%20Type/entity%2Fid/hash%3D%3D/",
            SnapshotBlobPathBuilder.BuildStreamPrefix(StreamKey));
    }

    /// <summary>
    ///     Ensures versions can be parsed from names that belong to the stream.
    /// </summary>
    [Fact]
    public void TryParseVersionShouldReturnVersionForMatchingBlob()
    {
        bool parsed = SnapshotBlobPathBuilder.TryParseVersion(
            StreamKey,
            "TEST.BROOK/Projection%20Type/entity%2Fid/hash%3D%3D/42.snapshot",
            out long version);
        Assert.True(parsed);
        Assert.Equal(42, version);
    }

    /// <summary>
    ///     Ensures non-matching names are rejected.
    /// </summary>
    [Fact]
    public void TryParseVersionShouldRejectNonMatchingBlob()
    {
        bool parsed = SnapshotBlobPathBuilder.TryParseVersion(
            StreamKey,
            "TEST.BROOK/Projection%20Type/entity%2Fid/hash%3D%3D/subdir/42.snapshot",
            out long version);
        Assert.False(parsed);
        Assert.Equal(0, version);
    }
}
