using System;
using System.Collections.Generic;
using System.Linq;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs;

using Moq;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for Blob snapshot storage DI registrations.
/// </summary>
public sealed class SnapshotBlobStorageProviderRegistrationsTests
{
    private static BlobServiceClient CreateBlobServiceClient() => new("UseDevelopmentStorage=true");

    /// <summary>
    ///     Verifies the hosted initializer is registered with DI.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldRegisterHostedInitializer()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            CreateBlobServiceClient());
        services.AddBlobSnapshotStorageProvider();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReadOnlyList<IHostedService> hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, hostedService => hostedService is SnapshotBlobContainerInitializer);
    }

    /// <summary>
    ///     Verifies the main registration method wires provider services and the keyed container client.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldRegisterServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            CreateBlobServiceClient());
        services.AddBlobSnapshotStorageProvider(options => options.ContainerName = "snapshots-test");
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISnapshotBlobOperations>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotBlobRepository>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageWriter>());
        Assert.NotNull(provider.GetRequiredService<IValidateOptions<SnapshotBlobStorageOptions>>());
        BlobContainerClient containerClient =
            provider.GetRequiredKeyedService<BlobContainerClient>(SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Equal("snapshots-test", containerClient.Name);
    }

    /// <summary>
    ///     Verifies both self-created client overloads reject blank connection strings during registration.
    /// </summary>
    /// <param name="connectionString">The invalid connection string.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddBlobSnapshotStorageProviderShouldRejectBlankConnectionStrings(
        string? connectionString
    )
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        ArgumentException actionException = Assert.ThrowsAny<ArgumentException>(() =>
            services.AddBlobSnapshotStorageProvider(connectionString!));
        ArgumentException configurationException = Assert.ThrowsAny<ArgumentException>(() =>
            services.AddBlobSnapshotStorageProvider(connectionString!, configuration));
        Assert.Equal("blobConnectionString", actionException.ParamName);
        Assert.Equal("blobConnectionString", configurationException.ParamName);
    }

    /// <summary>
    ///     Verifies an externally registered custom client key selects the intended storage account.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldResolveConfiguredExternalClient()
    {
        Mock<BlobServiceClient> defaultClient = new(MockBehavior.Strict);
        Mock<BlobServiceClient> customClient = new(MockBehavior.Strict);
        Mock<BlobContainerClient> containerClient = new(MockBehavior.Strict);
        customClient.Setup(client => client.GetBlobContainerClient("custom-snapshots")).Returns(containerClient.Object);
        ServiceCollection services = new();
        services.AddKeyedSingleton(SnapshotBlobDefaults.BlobServiceClientServiceKey, defaultClient.Object);
        services.AddKeyedSingleton("custom-blobs", customClient.Object);
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.BlobServiceClientServiceKey = "custom-blobs";
            options.ContainerName = "custom-snapshots";
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        BlobContainerClient resolved = provider.GetRequiredKeyedService<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Same(containerClient.Object, resolved);
        customClient.Verify(client => client.GetBlobContainerClient("custom-snapshots"), Times.Once);
        defaultClient.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies invalid options are caught through the host startup validation hook before SDK access.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldValidateOptionsOnStart()
    {
        ServiceCollection services = new();
        services.AddBlobSnapshotStorageProvider(options => options.ContainerName = "INVALID_CONTAINER");
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains(nameof(SnapshotBlobStorageOptions.ContainerName), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the configuration overload binds options from configuration.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConfigurationShouldBindOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ContainerName"] = "configured-snapshots",
                    ["EnableCompression"] = "true",
                })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            CreateBlobServiceClient());
        services.AddBlobSnapshotStorageProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("configured-snapshots", options.ContainerName);
        Assert.True(options.EnableCompression);
    }

    /// <summary>
    ///     Verifies the connection-string and configuration overload registers a usable container client.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringAndConfigurationShouldResolveContainer()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(SnapshotBlobStorageOptions.ContainerName)] = "configured-snapshots",
                    [nameof(SnapshotBlobStorageOptions.EnableCompression)] = "true",
                })
            .Build();
        ServiceCollection services = new();
        services.AddBlobSnapshotStorageProvider("UseDevelopmentStorage=true", configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        BlobContainerClient containerClient = provider.GetRequiredKeyedService<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Equal("configured-snapshots", containerClient.Name);
        Assert.True(provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value.EnableCompression);
    }

    /// <summary>
    ///     Verifies the connection-string overload registers the keyed Blob service client.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldRegisterBlobServiceClient()
    {
        ServiceCollection services = new();
        services.AddBlobSnapshotStorageProvider(
            "UseDevelopmentStorage=true",
            options => options.ContainerName = "connection-string-snapshots");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        BlobServiceClient blobServiceClient =
            provider.GetRequiredKeyedService<BlobServiceClient>(SnapshotBlobDefaults.BlobServiceClientServiceKey);
        Assert.NotNull(blobServiceClient);
        Assert.Equal("connection-string-snapshots", options.ContainerName);
    }

    /// <summary>
    ///     Verifies connection strings cannot be silently ignored in favor of a separately keyed client.
    /// </summary>
    /// <param name="bindConfiguration">Whether to use the configuration overload.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldRejectCustomClientKey(
        bool bindConfiguration
    )
    {
        ServiceCollection services = new();
        if (bindConfiguration)
        {
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [nameof(SnapshotBlobStorageOptions.BlobServiceClientServiceKey)] = "custom-blobs",
                    })
                .Build();
            services.AddBlobSnapshotStorageProvider("UseDevelopmentStorage=true", configuration);
        }
        else
        {
            services.AddBlobSnapshotStorageProvider(
                "UseDevelopmentStorage=true",
                options => options.BlobServiceClientServiceKey = "custom-blobs");
        }

        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains(
            nameof(SnapshotBlobStorageOptions.BlobServiceClientServiceKey),
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("connection string", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies connection-string registration works with default options.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldUseDefaultOptions()
    {
        ServiceCollection services = new();
        services.AddBlobSnapshotStorageProvider("UseDevelopmentStorage=true");
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        BlobContainerClient containerClient = provider.GetRequiredKeyedService<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Equal(SnapshotBlobDefaults.ContainerName, containerClient.Name);
    }
}