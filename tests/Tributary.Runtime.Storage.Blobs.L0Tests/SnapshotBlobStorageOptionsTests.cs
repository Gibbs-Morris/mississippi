using System;

using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageOptions" /> and <see cref="SnapshotBlobDefaults" />.
/// </summary>
public sealed class SnapshotBlobStorageOptionsTests
{
    /// <summary>
    ///     Verifies the default Blob container name.
    /// </summary>
    [Fact]
    public void ContainerNameShouldReturnDefaultValue()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.Equal(SnapshotBlobDefaults.ContainerName, options.ContainerName);
    }

    /// <summary>
    ///     Verifies compression defaults to disabled.
    /// </summary>
    [Fact]
    public void EnableCompressionShouldDefaultToFalse()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.False(options.EnableCompression);
    }

    /// <summary>
    ///     Verifies the default uncompressed payload size limit.
    /// </summary>
    [Fact]
    public void MaximumSnapshotPayloadSizeBytesShouldReturnDefaultValue()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.Equal(
            SnapshotBlobDefaults.DefaultMaximumSnapshotPayloadSizeBytes,
            options.MaximumSnapshotPayloadSizeBytes);
    }

    /// <summary>
    ///     Verifies public defaults retain the expected contract values.
    /// </summary>
    [Fact]
    public void SnapshotBlobDefaultsShouldMatchExpectedContractValues()
    {
        Assert.Equal("snapshots", SnapshotBlobDefaults.ContainerName);
        Assert.Equal("mississippi-blob-snapshots", SnapshotBlobDefaults.BlobServiceClientServiceKey);
        Assert.Equal("mississippi-blob-snapshots-container", SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Equal(134217728L, SnapshotBlobDefaults.DefaultMaximumSnapshotPayloadSizeBytes);
    }

    /// <summary>
    ///     Verifies options validation accepts valid default options.
    /// </summary>
    [Fact]
    public void ValidatorShouldAcceptValidOptions()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, new());
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Verifies options validation rejects a blank Blob service client key.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectBlankBlobServiceClientServiceKey()
    {
        SnapshotBlobStorageOptions options = new()
        {
            BlobServiceClientServiceKey = " ",
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("BlobServiceClientServiceKey", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies options validation rejects an invalid container name.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectInvalidContainerName()
    {
        SnapshotBlobStorageOptions options = new()
        {
            ContainerName = "INVALID_CONTAINER",
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("ContainerName", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies options validation rejects non-positive payload size limits.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectNonPositivePayloadSizeLimit()
    {
        SnapshotBlobStorageOptions options = new()
        {
            MaximumSnapshotPayloadSizeBytes = 0,
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("MaximumSnapshotPayloadSizeBytes", StringComparison.Ordinal));
    }
}