using System;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests.Storage;

/// <summary>
///     Tests for <see cref="BlobPathBuilder" />.
/// </summary>
public sealed class BlobPathBuilderTests
{
    /// <summary>
    ///     Verifies that BuildPath for SnapshotKey produces correct path.
    /// </summary>
    [Fact]
    public void BuildPathForSnapshotKeyShouldReturnCorrectFormat()
    {
        SnapshotStreamKey streamKey = new("MyBrook", "OrderSnapshot", "order-123", "abc123hash");
        SnapshotKey snapshotKey = new(streamKey, 42);
        string result = BlobPathBuilder.BuildPath(snapshotKey);
        Assert.Equal("MyBrook/OrderSnapshot/order-123/abc123hash/42.snapshot", result);
    }

    /// <summary>
    ///     Verifies that BuildPath for SnapshotStreamKey and version produces correct path.
    /// </summary>
    [Fact]
    public void BuildPathForStreamKeyAndVersionShouldReturnCorrectFormat()
    {
        SnapshotStreamKey streamKey = new("MyBrook", "OrderSnapshot", "order-123", "abc123hash");
        string result = BlobPathBuilder.BuildPath(streamKey, 42);
        Assert.Equal("MyBrook/OrderSnapshot/order-123/abc123hash/42.snapshot", result);
    }

    /// <summary>
    ///     Verifies that path components are URL-encoded to handle special characters.
    /// </summary>
    [Fact]
    public void BuildPathShouldEncodeSpecialCharacters()
    {
        SnapshotStreamKey streamKey = new("My Brook", "Order/Snapshot", "order#123", "abc=hash");
        SnapshotKey snapshotKey = new(streamKey, 1);
        string result = BlobPathBuilder.BuildPath(snapshotKey);

        // Verify special characters are encoded
        Assert.Contains("My%20Brook", result, StringComparison.Ordinal);
        Assert.Contains("Order%2FSnapshot", result, StringComparison.Ordinal);
        Assert.Contains("order%23123", result, StringComparison.Ordinal);
        Assert.Contains("abc%3Dhash", result, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that BuildPrefix returns correct path for listing.
    /// </summary>
    [Fact]
    public void BuildPrefixShouldReturnCorrectFormat()
    {
        SnapshotStreamKey streamKey = new("MyBrook", "OrderSnapshot", "order-123", "abc123hash");
        string result = BlobPathBuilder.BuildPrefix(streamKey);
        Assert.Equal("MyBrook/OrderSnapshot/order-123/abc123hash/", result);
    }

    /// <summary>
    ///     Verifies that ExtractVersion handles full path correctly.
    /// </summary>
    [Fact]
    public void ExtractVersionShouldHandleFullPath()
    {
        long? result = BlobPathBuilder.ExtractVersion("MyBrook/OrderSnapshot/order-123/abc123hash/99.snapshot");
        Assert.Equal(99L, result);
    }

    /// <summary>
    ///     Verifies that large version numbers are handled correctly.
    /// </summary>
    [Fact]
    public void ExtractVersionShouldHandleLargeVersionNumbers()
    {
        long? result = BlobPathBuilder.ExtractVersion("9223372036854775806.snapshot");
        Assert.Equal(9223372036854775806L, result);
    }

    /// <summary>
    ///     Verifies that ExtractVersion correctly parses version from blob name.
    /// </summary>
    [Fact]
    public void ExtractVersionShouldReturnCorrectVersion()
    {
        long? result = BlobPathBuilder.ExtractVersion("42.snapshot");
        Assert.Equal(42L, result);
    }

    /// <summary>
    ///     Verifies that ExtractVersion returns null for invalid names.
    /// </summary>
    /// <param name="blobName">The blob name to test.</param>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("abc.snapshot")]
    [InlineData("42.txt")]
    [InlineData(".snapshot")]
    public void ExtractVersionShouldReturnNullForInvalidNames(
        string blobName
    )
    {
        long? result = BlobPathBuilder.ExtractVersion(blobName);
        Assert.Null(result);
    }
}