using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for snapshot storage provider DI registrations.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Storage Provider Registrations")]
public sealed class SnapshotStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Ensures the registrations wire dependencies when a CosmosClient is already in DI.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageProviderShouldRegisterServices()
    {
        Mock<Container> container = new();
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("snapshots")).Returns(container.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.GetDatabase("db")).Returns(database.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClient.Object);
        services.Configure<SnapshotStorageOptions>(o => o.DatabaseId = "db");
        services.AddCosmosSnapshotStorageProvider();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISnapshotContainerOperations>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotCosmosRepository>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.NotNull(provider.GetRequiredService<IRetryPolicy>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotDocument, SnapshotStorageModel>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotStorageModel, SnapshotEnvelope>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotWriteModel, SnapshotStorageModel>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotStorageModel, SnapshotDocument>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotDocument, SnapshotEnvelope>>());
        Container resolved = provider.GetRequiredKeyedService<Container>(CosmosContainerKeys.Snapshots);
        Assert.Same(container.Object, resolved);
    }

    /// <summary>
    ///     Ensures the overload with IConfiguration binds options from configuration.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageProviderWithConfigurationShouldBindOptions()
    {
        Mock<Container> container = new();
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("snapshots")).Returns(container.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.GetDatabase("config-db")).Returns(database.Object);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "DatabaseId", "config-db" },
                })
            .Build();
        ServiceCollection services = new();
        services.AddSingleton(cosmosClient.Object);
        services.AddCosmosSnapshotStorageProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("config-db", options.DatabaseId);
        Assert.NotNull(provider.GetRequiredService<ISnapshotCosmosRepository>());
    }

    /// <summary>
    ///     Ensures the overload with a configuration action applies option configuration.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageProviderWithConfigureActionShouldBindOptions()
    {
        Mock<Container> container = new();
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("snapshots")).Returns(container.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.GetDatabase("custom-db")).Returns(database.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClient.Object);
        services.AddCosmosSnapshotStorageProvider(o => o.DatabaseId = "custom-db");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("custom-db", options.DatabaseId);
        Assert.NotNull(provider.GetRequiredService<ISnapshotCosmosRepository>());
    }

    /// <summary>
    ///     Ensures the overload that creates a CosmosClient applies option configuration.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageProviderWithConnectionStringShouldBindOptions()
    {
        ServiceCollection services = new();
        services.AddCosmosSnapshotStorageProvider("UseDevelopmentStorage=true", o => o.DatabaseId = "db2");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("db2", options.DatabaseId);
    }

    /// <summary>
    ///     CosmosContainerInitializer should create container when it does not exist (NotFound exception).
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerShouldCreateContainerWhenNotFound()
    {
        // Arrange
        Mock<ContainerResponse> containerResponseMock = new();
        containerResponseMock.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/snapshotPartitionKey",
                });
        Mock<Container> containerMock = new();
        containerMock
            .Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));
        Mock<Database> databaseMock = new();
        databaseMock.Setup(d => d.GetContainer("new-container")).Returns(containerMock.Object);
        databaseMock.Setup(d => d.CreateContainerIfNotExistsAsync(
                "new-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<DatabaseResponse> databaseResponseMock = new();
        databaseResponseMock.Setup(r => r.Database).Returns(databaseMock.Object);
        Mock<CosmosClient> cosmosClientMock = new();
        cosmosClientMock.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "new-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponseMock.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClientMock.Object);
        services.Configure<SnapshotStorageOptions>(o =>
        {
            o.DatabaseId = "new-db";
            o.ContainerId = "new-container";
        });
        services.AddCosmosSnapshotStorageProvider();
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IHostedService hostedService = provider.GetServices<IHostedService>()
            .First(s => s.GetType().Name == "CosmosContainerInitializer");
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        databaseMock.Verify(
            d => d.CreateContainerIfNotExistsAsync(
                "new-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     CosmosContainerInitializer should throw when existing container has wrong partition key path.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerShouldThrowWhenPartitionKeyMismatch()
    {
        // Arrange
        Mock<ContainerResponse> containerResponseMock = new();
        containerResponseMock.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/wrongPartitionKey",
                });
        Mock<Container> containerMock = new();
        containerMock
            .Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<Database> databaseMock = new();
        databaseMock.Setup(d => d.GetContainer("existing-container")).Returns(containerMock.Object);
        Mock<DatabaseResponse> databaseResponseMock = new();
        databaseResponseMock.Setup(r => r.Database).Returns(databaseMock.Object);
        Mock<CosmosClient> cosmosClientMock = new();
        cosmosClientMock.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "existing-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponseMock.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClientMock.Object);
        services.Configure<SnapshotStorageOptions>(o =>
        {
            o.DatabaseId = "existing-db";
            o.ContainerId = "existing-container";
        });
        services.AddCosmosSnapshotStorageProvider();
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IHostedService hostedService = provider.GetServices<IHostedService>()
            .First(s => s.GetType().Name == "CosmosContainerInitializer");
        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("/wrongPartitionKey", ex.Message, StringComparison.Ordinal);
        Assert.Contains("/snapshotPartitionKey", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     CosmosContainerInitializer StartAsync should create database and container.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerStartAsyncShouldCreateDatabaseAndContainer()
    {
        // Arrange
        Mock<ContainerResponse> containerResponseMock = new();
        containerResponseMock.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/snapshotPartitionKey",
                });
        Mock<Container> containerMock = new();
        containerMock
            .Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<Database> databaseMock = new();
        databaseMock.Setup(d => d.GetContainer("test-container")).Returns(containerMock.Object);
        databaseMock.Setup(d => d.CreateContainerIfNotExistsAsync(
                "test-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<DatabaseResponse> databaseResponseMock = new();
        databaseResponseMock.Setup(r => r.Database).Returns(databaseMock.Object);
        Mock<CosmosClient> cosmosClientMock = new();
        cosmosClientMock.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "test-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponseMock.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClientMock.Object);
        services.Configure<SnapshotStorageOptions>(o =>
        {
            o.DatabaseId = "test-db";
            o.ContainerId = "test-container";
        });
        services.AddCosmosSnapshotStorageProvider();
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IHostedService hostedService = provider.GetServices<IHostedService>()
            .First(s => s.GetType().Name == "CosmosContainerInitializer");
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        cosmosClientMock.Verify(
            c => c.CreateDatabaseIfNotExistsAsync(
                "test-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        databaseMock.Verify(
            d => d.CreateContainerIfNotExistsAsync(
                "test-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     CosmosContainerInitializer StopAsync should complete successfully.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerStopAsyncShouldComplete()
    {
        // Arrange
        Mock<ContainerResponse> containerResponseMock = new();
        containerResponseMock.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/snapshotPartitionKey",
                });
        Mock<Container> containerMock = new();
        containerMock
            .Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<Database> databaseMock = new();
        databaseMock.Setup(d => d.GetContainer("snapshots")).Returns(containerMock.Object);
        databaseMock.Setup(d => d.CreateContainerIfNotExistsAsync(
                "snapshots",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponseMock.Object);
        Mock<DatabaseResponse> databaseResponseMock = new();
        databaseResponseMock.Setup(r => r.Database).Returns(databaseMock.Object);
        Mock<CosmosClient> cosmosClientMock = new();
        cosmosClientMock.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponseMock.Object);
        ServiceCollection services = new();
        services.AddSingleton(cosmosClientMock.Object);
        services.Configure<SnapshotStorageOptions>(o => o.DatabaseId = "db");
        services.AddCosmosSnapshotStorageProvider();
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IHostedService hostedService = provider.GetServices<IHostedService>()
            .First(s => s.GetType().Name == "CosmosContainerInitializer");
        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);

        // Assert - StopAsync completes without throwing
        Assert.True(true, "StopAsync completed successfully without throwing.");
    }
}