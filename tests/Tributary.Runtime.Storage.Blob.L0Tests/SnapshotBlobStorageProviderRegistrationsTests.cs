using System;
using System.Collections.Generic;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests staged Blob snapshot runtime composition.
/// </summary>
public sealed class SnapshotBlobStorageProviderRegistrationsTests
{
    private static ISiloBuilder CreateSilo(
        IServiceCollection services
    )
    {
        Mock<ISiloBuilder> silo = new();
        silo.SetupGet(builder => builder.Services).Returns(services);
        silo.SetupGet(builder => builder.Configuration).Returns(new ConfigurationBuilder().Build());
        return silo.Object;
    }

    /// <summary>Verifies configuration is bound before staged registration.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderBindsConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(SnapshotBlobStorageOptions.ContainerName)] = "configured-snapshots",
                    [nameof(SnapshotBlobStorageOptions.EnableCompression)] = "true",
                })
            .Build();
        ServiceCollection services = new();
        CreateSilo(services).UseMississippi(runtime => runtime.AddBlobSnapshotStorageProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("configured-snapshots", options.ContainerName);
        Assert.True(options.EnableCompression);
    }

    /// <summary>Verifies host-owned Blob composition and the shared snapshot aliases.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderRegistersProviderRoles()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        CreateSilo(services)
            .UseMississippi(runtime =>
                runtime.AddBlobSnapshotStorageProvider(options => options.ContainerName = "snapshots-test"));
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotStorageProvider storage = provider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.Same(storage, provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(storage, provider.GetRequiredService<ISnapshotStorageWriter>());
        Assert.Equal("azure-blob", storage.Format);
        BlobContainerClient registeredContainer = provider.GetRequiredKeyedService<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey);
        Assert.Equal("snapshots-test", registeredContainer.Name);
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is SnapshotBlobContainerInitializer);
        Assert.False(provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value.EnableCompression);
    }

    /// <summary>Verifies a pre-registered scoped provider keeps scoped reader and writer aliases.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderPreservesCustomProviderLifetime()
    {
        ServiceCollection services = new();
        services.AddScoped<ISnapshotStorageProvider>(_ => Mock.Of<ISnapshotStorageProvider>());
        CreateSilo(services).UseMississippi(runtime => runtime.AddBlobSnapshotStorageProvider());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        ISnapshotStorageProvider first = firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageWriter>());
        Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService<ISnapshotStorageProvider>());
    }

    /// <summary>Verifies invalid connection strings fail before runtime staging.</summary>
    /// <param name="connectionString">The invalid connection string.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]

    public void AddBlobSnapshotStorageProviderRejectsBlankConnectionString(
        string? connectionString
    )
    {
        Mock<IRuntimeBuilder> builder = new();
        Assert.ThrowsAny<ArgumentException>(() => builder.Object.AddBlobSnapshotStorageProvider(connectionString!));
    }

    /// <summary>Verifies a connection string cannot silently ignore a custom client key.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderRejectsConnectionStringKeyConflict()
    {
        ServiceCollection services = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            CreateSilo(services)
                .UseMississippi(runtime => runtime.AddBlobSnapshotStorageProvider(
                    "UseDevelopmentStorage=true",
                    options => options.BlobServiceClientServiceKey = "custom-blobs")));
        Assert.Contains("custom", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies duplicate provider composition is rejected.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderRejectsDuplicateRegistration()
    {
        ServiceCollection services = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddBlobSnapshotStorageProvider();
                runtime.AddBlobSnapshotStorageProvider();
            }));
        Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies invalid options are rejected before provider services are registered.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderRejectsInvalidOptions()
    {
        ServiceCollection services = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            CreateSilo(services)
                .UseMississippi(runtime =>
                    runtime.AddBlobSnapshotStorageProvider(options => options.ContainerName = "INVALID_CONTAINER")));
        Assert.Contains(nameof(SnapshotBlobStorageOptions.ContainerName), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISnapshotStorageProvider));
    }

    /// <summary>Verifies an externally registered client key selects the intended account.</summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderUsesConfiguredClientKey()
    {
        Mock<BlobServiceClient> client = new(MockBehavior.Strict);
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        client.Setup(value => value.GetBlobContainerClient("custom-snapshots")).Returns(container.Object);
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton("custom-blobs", client.Object);
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddBlobSnapshotStorageProvider(options =>
            {
                options.BlobServiceClientServiceKey = "custom-blobs";
                options.ContainerName = "custom-snapshots";
            }));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(
            container.Object,
            provider.GetRequiredKeyedService<BlobContainerClient>(SnapshotBlobDefaults.BlobContainerClientServiceKey));
        client.Verify(value => value.GetBlobContainerClient("custom-snapshots"), Times.Once);
    }
}