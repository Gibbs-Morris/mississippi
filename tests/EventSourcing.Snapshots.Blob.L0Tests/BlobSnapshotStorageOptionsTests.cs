using Azure.Storage.Blobs.Models;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="BlobSnapshotStorageOptions" />.
/// </summary>
public sealed class BlobSnapshotStorageOptionsTests
{
    /// <summary>
    ///     Verifies default BlobServiceClientKey.
    /// </summary>
    [Fact]
    public void BlobServiceClientKeyShouldDefaultToExpectedKey()
    {
        BlobSnapshotStorageOptions options = new();
        Assert.Equal("mississippi-blob-snapshots-client", options.BlobServiceClientKey);
    }

    /// <summary>
    ///     Verifies default container name.
    /// </summary>
    [Fact]
    public void ContainerNameShouldDefaultToSnapshots()
    {
        BlobSnapshotStorageOptions options = new();
        Assert.Equal("snapshots", options.ContainerName);
    }

    /// <summary>
    ///     Verifies default access tier is Hot.
    /// </summary>
    [Fact]
    public void DefaultAccessTierShouldDefaultToHot()
    {
        BlobSnapshotStorageOptions options = new();
        Assert.Equal(AccessTier.Hot, options.DefaultAccessTier);
    }

    /// <summary>
    ///     Verifies default max concurrency.
    /// </summary>
    [Fact]
    public void MaxConcurrencyShouldDefaultTo10()
    {
        BlobSnapshotStorageOptions options = new();
        Assert.Equal(10, options.MaxConcurrency);
    }

    /// <summary>
    ///     Verifies options can be modified.
    /// </summary>
    [Fact]
    public void OptionsShouldBeMutable()
    {
        BlobSnapshotStorageOptions options = new()
        {
            ContainerName = "custom-container",
            BlobServiceClientKey = "custom-key",
            WriteCompression = SnapshotCompression.GZip,
            DefaultAccessTier = AccessTier.Cool,
            MaxConcurrency = 20,
        };
        Assert.Equal("custom-container", options.ContainerName);
        Assert.Equal("custom-key", options.BlobServiceClientKey);
        Assert.Equal(SnapshotCompression.GZip, options.WriteCompression);
        Assert.Equal(AccessTier.Cool, options.DefaultAccessTier);
        Assert.Equal(20, options.MaxConcurrency);
    }

    /// <summary>
    ///     Verifies default compression is Brotli.
    /// </summary>
    [Fact]
    public void WriteCompressionShouldDefaultToBrotli()
    {
        BlobSnapshotStorageOptions options = new();
        Assert.Equal(SnapshotCompression.Brotli, options.WriteCompression);
    }
}