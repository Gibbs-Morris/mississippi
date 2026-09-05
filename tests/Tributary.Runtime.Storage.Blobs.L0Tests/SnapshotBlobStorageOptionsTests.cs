using System;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Blobs;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

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
    ///     Verifies the default limit for serialized JSON snapshot documents.
    /// </summary>
    [Fact]
    public void MaximumSnapshotDocumentSizeBytesShouldReturnDefaultValue()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.Equal(
            SnapshotBlobDefaults.DefaultMaximumSnapshotDocumentSizeBytes,
            options.MaximumSnapshotDocumentSizeBytes);
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
        Assert.Equal(201326592L, SnapshotBlobDefaults.DefaultMaximumSnapshotDocumentSizeBytes);
    }

    /// <summary>
    ///     Verifies size limits allow the complete range that the materialized byte arrays support.
    /// </summary>
    [Fact]
    public void ValidatorShouldAcceptSizeLimitBoundaries()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        long[] limits = [1, Array.MaxLength];
        foreach (ValidateOptionsResult result in limits.Select(limit => validator.Validate(
                     Options.DefaultName,
                     new()
                     {
                         MaximumSnapshotPayloadSizeBytes = limit,
                         MaximumSnapshotDocumentSizeBytes = limit,
                     })))
        {
            Assert.True(result.Succeeded);
        }
    }

    /// <summary>
    ///     Verifies valid container names at the service length boundaries and with separated dashes.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("snapshots-v1-123")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ValidatorShouldAcceptValidContainerNames(
        string containerName
    )
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new()
            {
                ContainerName = containerName,
            });
        Assert.True(result.Succeeded);
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
    /// <param name="serviceKey">The invalid client service key.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidatorShouldRejectBlankBlobServiceClientServiceKey(
        string? serviceKey
    )
    {
        SnapshotBlobStorageOptions options = new()
        {
            BlobServiceClientServiceKey = serviceKey!,
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("BlobServiceClientServiceKey", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies configured JSON documents must fit the materialized byte array representation.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectDocumentSizeAboveArrayLimit()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new()
            {
                MaximumSnapshotDocumentSizeBytes = (long)Array.MaxLength + 1,
            });
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                nameof(SnapshotBlobStorageOptions.MaximumSnapshotDocumentSizeBytes),
                StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies options validation rejects an invalid container name.
    /// </summary>
    /// <param name="containerName">The invalid container name.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("INVALID_CONTAINER")]
    [InlineData("snapshots_test")]
    [InlineData("snapshots--test")]
    [InlineData("-snapshots")]
    [InlineData("snapshots-")]
    [InlineData("snapshoté")]
    public void ValidatorShouldRejectInvalidContainerName(
        string? containerName
    )
    {
        SnapshotBlobStorageOptions options = new()
        {
            ContainerName = containerName!,
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("ContainerName", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies serialized document limits must be positive.
    /// </summary>
    /// <param name="maximumDocumentSize">The invalid maximum document size.</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void ValidatorShouldRejectNonPositiveDocumentSizeLimit(
        long maximumDocumentSize
    )
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new()
            {
                MaximumSnapshotDocumentSizeBytes = maximumDocumentSize,
            });
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                nameof(SnapshotBlobStorageOptions.MaximumSnapshotDocumentSizeBytes),
                StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies options validation rejects non-positive payload size limits.
    /// </summary>
    /// <param name="maximumPayloadSize">The invalid maximum payload size.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidatorShouldRejectNonPositivePayloadSizeLimit(
        long maximumPayloadSize
    )
    {
        SnapshotBlobStorageOptions options = new()
        {
            MaximumSnapshotPayloadSizeBytes = maximumPayloadSize,
        };
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("MaximumSnapshotPayloadSizeBytes", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies null options are rejected at the validation boundary.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectNullOptions()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(Options.DefaultName, null!));
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>
    ///     Verifies configured payloads must fit the materialized byte array representation.
    /// </summary>
    [Fact]
    public void ValidatorShouldRejectPayloadSizeAboveArrayLimit()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new()
            {
                MaximumSnapshotPayloadSizeBytes = (long)Array.MaxLength + 1,
            });
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                nameof(SnapshotBlobStorageOptions.MaximumSnapshotPayloadSizeBytes),
                StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies all invalid values are reported together before any storage work starts.
    /// </summary>
    [Fact]
    public void ValidatorShouldReportAllInvalidOptions()
    {
        SnapshotBlobStorageOptionsValidator validator = new();
        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new()
            {
                BlobServiceClientServiceKey = string.Empty,
                ContainerName = string.Empty,
                MaximumSnapshotPayloadSizeBytes = 0,
                MaximumSnapshotDocumentSizeBytes = 0,
            });
        Assert.True(result.Failed);
        Assert.Equal(4, result.Failures.Count());
    }
}