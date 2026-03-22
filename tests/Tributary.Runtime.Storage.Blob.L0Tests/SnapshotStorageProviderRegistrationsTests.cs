using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for Blob snapshot storage provider DI registrations.
/// </summary>
public sealed class SnapshotStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Ensures the registrations wire dependencies when a BlobServiceClient is already in DI.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldRegisterServices()
    {
        Mock<BlobContainerClient> containerClient = new();
        Mock<BlobServiceClient> blobServiceClient = new();
        blobServiceClient.Setup(c => c.GetBlobContainerClient("snapshots")).Returns(containerClient.Object);
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(SnapshotBlobDefaults.BlobServiceClientServiceKey, blobServiceClient.Object);
        services.AddBlobSnapshotStorageProvider();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISnapshotBlobContainerOperations>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotBlobRepository>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotBlobStorageModel, SnapshotEnvelope>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotWriteModel, SnapshotBlobStorageModel>>());
        BlobContainerClient resolved =
            provider.GetRequiredKeyedService<BlobContainerClient>(SnapshotBlobDefaults.BlobContainerServiceKey);
        Assert.Same(containerClient.Object, resolved);
    }

    /// <summary>
    ///     Ensures the overload with IConfiguration binds options from configuration.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConfigurationShouldBindOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "ContainerName", "config-snapshots" },
                    { "CompressionEnabled", bool.TrueString },
                })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(SnapshotBlobDefaults.BlobServiceClientServiceKey, Mock.Of<BlobServiceClient>());
        services.AddBlobSnapshotStorageProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("config-snapshots", options.ContainerName);
        Assert.True(options.CompressionEnabled);
    }

    /// <summary>
    ///     Ensures the overload with a configuration action applies option configuration.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConfigureActionShouldBindOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(SnapshotBlobDefaults.BlobServiceClientServiceKey, Mock.Of<BlobServiceClient>());
        services.AddBlobSnapshotStorageProvider(options => options.ContainerName = "custom");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("custom", options.ContainerName);
    }

    /// <summary>
    ///     Ensures the connection string overload registers options without requiring external client registration.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldBindOptions()
    {
        ServiceCollection services = new();
        services.AddBlobSnapshotStorageProvider("UseDevelopmentStorage=true", options => options.ContainerName = "blob-tests");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("blob-tests", options.ContainerName);
    }

    /// <summary>
    ///     Ensures hosted initialization uses the real provider registration path on host startup.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldEnsureContainerExists()
    {
        Mock<BlobServiceClient> blobServiceClient = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(SnapshotBlobDefaults.BlobServiceClientServiceKey, blobServiceClient.Object);
        services.AddBlobSnapshotStorageProvider();

        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.EnsureContainerExistsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        services.Replace(ServiceDescriptor.Singleton(operations.Object));

        using IHost host = Host.CreateDefaultBuilder().ConfigureServices(serviceCollection =>
        {
            foreach (ServiceDescriptor descriptor in services)
            {
                serviceCollection.Add(descriptor);
            }
        }).Build();

        await host.StartAsync();

        operations.Verify(o => o.EnsureContainerExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
        await host.StopAsync();
    }
}
