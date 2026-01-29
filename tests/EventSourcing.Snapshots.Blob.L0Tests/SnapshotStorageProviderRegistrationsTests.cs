using System.Linq;

using Azure.Storage.Blobs;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

public sealed class SnapshotStorageProviderRegistrationsTests
{
    [Fact]
    public void AddBlobSnapshotStorageProvider_RegistersProvider()
    {
        // Arrange
        ServiceCollection services = new();
        // Act
        services.AddBlobSnapshotStorageProvider();
        // Assert
        ServiceDescriptor? providerDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ISnapshotStorageProvider));
        providerDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddBlobSnapshotStorageProvider_WithConnectionString_RegistersKeyedClient()
    {
        // Arrange
        ServiceCollection services = new();
        // Act
        services.AddBlobSnapshotStorageProvider("UseDevelopmentStorage=true");
        // Assert
        ServiceDescriptor? clientDescriptor =
            services.FirstOrDefault(sd => (sd.ServiceType == typeof(BlobServiceClient)) && sd.IsKeyedService);
        clientDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddBlobSnapshotStorageProvider_WithOptionsAction_ConfiguresOptions()
    {
        // Arrange
        ServiceCollection services = new();
        // Act
        services.AddBlobSnapshotStorageProvider(opts => opts.ContainerId = "custom");
        ServiceProvider provider = services.BuildServiceProvider();
        // Assert
        IOptions<SnapshotStorageOptions>? options = provider.GetService<IOptions<SnapshotStorageOptions>>();
        options.Should().NotBeNull();
        options!.Value.ContainerId.Should().Be("custom");
    }
}