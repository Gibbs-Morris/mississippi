using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Blob;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageOptions" /> and related defaults.
/// </summary>
public sealed class SnapshotBlobStorageOptionsTests
{
    /// <summary>
    ///     Ensures options default values match the published defaults.
    /// </summary>
    [Fact]
    public void DefaultsShouldMatchSnapshotBlobDefaults()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.Equal(SnapshotBlobDefaults.BlobServiceClientServiceKey, options.BlobServiceClientServiceKey);
        Assert.Equal(SnapshotBlobDefaults.ContainerName, options.ContainerName);
        Assert.False(options.CompressionEnabled);
    }

    /// <summary>
    ///     Ensures validator rejects a blank container name.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectBlankContainerName()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            null,
            new()
            {
                ContainerName = string.Empty,
            });
        Assert.True(result.Failed);
    }

    /// <summary>
    ///     Ensures validator rejects a blank service key.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectBlankServiceKey()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            null,
            new()
            {
                BlobServiceClientServiceKey = " ",
            });
        Assert.True(result.Failed);
    }

    /// <summary>
    ///     Ensures validator accepts valid settings.
    /// </summary>
    [Fact]
    public void ValidatorShouldAcceptValidOptions()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(null, new SnapshotBlobStorageOptions());
        Assert.True(result.Succeeded);
    }
}
