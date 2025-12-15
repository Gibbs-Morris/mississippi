using Allure.Xunit.Attributes;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class SnapshotStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Ensures the registrations wire dependencies when a CosmosClient is already in DI.
    /// </summary>
    [AllureEpic("Snapshots")]
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
        Assert.NotNull(provider.GetRequiredService<ISnapshotCosmosRepository>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.NotNull(provider.GetRequiredService<IRetryPolicy>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotDocument, SnapshotStorageModel>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotStorageModel, SnapshotEnvelope>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotWriteModel, SnapshotStorageModel>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotStorageModel, SnapshotDocument>>());
        Assert.NotNull(provider.GetRequiredService<IMapper<SnapshotDocument, SnapshotEnvelope>>());
        Container resolved = provider.GetRequiredService<Container>();
        Assert.Same(container.Object, resolved);
    }

    /// <summary>
    ///     Ensures the overload that creates a CosmosClient applies option configuration.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void AddCosmosSnapshotStorageProviderWithConnectionStringShouldBindOptions()
    {
        ServiceCollection services = new();
        services.AddCosmosSnapshotStorageProvider("UseDevelopmentStorage=true", o => o.DatabaseId = "db2");
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("db2", options.DatabaseId);
    }
}