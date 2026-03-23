using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Blob.Startup;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageOptions" />.
/// </summary>
public sealed class SnapshotBlobStorageOptionsTests
{
    /// <summary>
    ///     Verifies the public Blob defaults remain stable.
    /// </summary>
    [Fact]
    public void SnapshotBlobDefaultsShouldMatchExpectedContractValues()
    {
        Assert.Equal("snapshots", SnapshotBlobDefaults.ContainerName);
        Assert.Equal("snapshots/", SnapshotBlobDefaults.BlobPrefix);
        Assert.Equal("mississippi-blob-snapshots", SnapshotBlobDefaults.BlobContainerServiceKey);
        Assert.Equal("mississippi-blob-snapshots-client", SnapshotBlobDefaults.BlobServiceClientServiceKey);
        Assert.Equal(SnapshotBlobCompression.Off, SnapshotBlobDefaults.Compression);
        Assert.Equal(500, SnapshotBlobDefaults.ListPageSizeHint);
        Assert.Equal(64 * 1024, SnapshotBlobDefaults.MaxHeaderBytes);
        Assert.Equal("System.Text.Json", SnapshotBlobDefaults.PayloadSerializerFormat);
    }

    /// <summary>
    ///     Verifies options expose the expected defaults.
    /// </summary>
    [Fact]
    public void SnapshotBlobStorageOptionsShouldReturnDefaultValues()
    {
        SnapshotBlobStorageOptions options = new();
        Assert.Equal(SnapshotBlobDefaults.BlobPrefix, options.BlobPrefix);
        Assert.Equal(SnapshotBlobDefaults.ContainerName, options.ContainerName);
        Assert.Equal(SnapshotBlobDefaults.BlobServiceClientServiceKey, options.BlobServiceClientServiceKey);
        Assert.Equal(SnapshotBlobDefaults.Compression, options.Compression);
        Assert.Equal(SnapshotBlobContainerInitializationMode.CreateIfMissing, options.ContainerInitializationMode);
        Assert.Equal(SnapshotBlobDefaults.ListPageSizeHint, options.ListPageSizeHint);
        Assert.Equal(SnapshotBlobDefaults.MaxHeaderBytes, options.MaxHeaderBytes);
        Assert.Equal(SnapshotBlobDefaults.PayloadSerializerFormat, options.PayloadSerializerFormat);
    }

    /// <summary>
    ///     Verifies invalid options fail validation.
    /// </summary>
    [Fact]
    public void SnapshotBlobStorageOptionsValidationShouldFailForInvalidOptions()
    {
        ServiceCollection services = new();
        services.AddOptions<SnapshotBlobStorageOptions>()
            .Configure(options =>
            {
                options.ContainerName = string.Empty;
                options.BlobServiceClientServiceKey = string.Empty;
                options.Compression = (SnapshotBlobCompression)99;
                options.ListPageSizeHint = 0;
                options.MaxHeaderBytes = 0;
                options.PayloadSerializerFormat = string.Empty;
                options.ContainerInitializationMode = (SnapshotBlobContainerInitializationMode)99;
            });
        services.AddSingleton<IValidateOptions<SnapshotBlobStorageOptions>, SnapshotBlobStorageOptionsValidator>();
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value);
        Assert.Contains("ContainerName", exception.Message, StringComparison.Ordinal);
        Assert.Contains("BlobServiceClientServiceKey", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Compression", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ListPageSizeHint", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MaxHeaderBytes", exception.Message, StringComparison.Ordinal);
        Assert.Contains("PayloadSerializerFormat", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ContainerInitializationMode", exception.Message, StringComparison.Ordinal);
    }
}