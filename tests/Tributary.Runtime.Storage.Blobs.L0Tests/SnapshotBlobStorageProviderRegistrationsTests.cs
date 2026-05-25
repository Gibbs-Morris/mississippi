using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Abstractions;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

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
    ///     Verifies the hosted initializer creates the container on host start.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SnapshotBlobContainerInitializerShouldCreateContainerOnStart()
    {
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.CreateContainerIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SnapshotBlobContainerInitializer initializer = new(
            operations.Object,
            NullLogger<SnapshotBlobContainerInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);
        operations.Verify(o => o.CreateContainerIfNotExistsAsync(CancellationToken.None), Times.Once);
    }
}