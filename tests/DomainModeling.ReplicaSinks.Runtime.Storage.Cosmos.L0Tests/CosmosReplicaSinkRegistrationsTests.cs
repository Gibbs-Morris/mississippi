using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

using Moq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests the Cosmos replica sink registration extensions.
/// </summary>
public sealed class CosmosReplicaSinkRegistrationsTests
{
    /// <summary>
    ///     Ensures provider registration contributes a keyed provider, a state store, a descriptor, and one initializer.
    /// </summary>
    [Fact]
    public void AddCosmosReplicaSinkShouldRegisterProviderDescriptorStateStoreAndInitializer()
    {
        StateContainerHarness harness = new("orders-db", "orders-container");
        ServiceCollection services = [];
        services.AddKeyedSingleton<CosmosClient>("orders-client", (_, _) => harness.Client.Object);
        services.AddCosmosReplicaSink(
            "orders",
            "orders-client",
            options =>
            {
                options.DatabaseId = "orders-db";
                options.ContainerId = "orders-container";
                options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("orders");
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        ReplicaSinkRegistrationDescriptor descriptor = provider.GetServices<ReplicaSinkRegistrationDescriptor>().Single();
        CosmosReplicaSinkContainerInitializer initializer = provider.GetServices<IHostedService>()
            .OfType<CosmosReplicaSinkContainerInitializer>()
            .Single();

        Assert.IsType<CosmosReplicaSinkProvider>(sinkProvider);
        Assert.IsType<CosmosReplicaSinkDeliveryStateStore>(stateStore);
        Assert.NotNull(initializer);
        Assert.Equal("orders", descriptor.SinkKey);
        Assert.Equal("orders-client", descriptor.ClientKey);
        Assert.Equal(ReplicaSinkCosmosDefaults.FormatName, descriptor.Format);
        Assert.Equal(typeof(CosmosReplicaSinkProvider), descriptor.ProviderType);
        Assert.Equal(ReplicaProvisioningMode.CreateIfMissing, descriptor.ProvisioningMode);
    }

    /// <summary>
    ///     Ensures configuration binding uses named options while preserving the explicit client key parameter.
    /// </summary>
    [Fact]
    public void AddCosmosReplicaSinkWithConfigurationShouldBindNamedOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ProvisioningMode"] = nameof(ReplicaProvisioningMode.CreateIfMissing),
                    ["DatabaseId"] = "configured-db",
                    ["ContainerId"] = "configured-container",
                    ["ClientKey"] = "ignored-config-client",
                    ["QueryBatchSize"] = "25",
                })
            .Build();
        StateContainerHarness harness = new("configured-db", "configured-container");
        ServiceCollection services = [];
        services.AddKeyedSingleton<CosmosClient>("orders-client", (_, _) => harness.Client.Object);
        services.AddCosmosReplicaSink("orders", "orders-client", configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<CosmosReplicaSinkOptions> optionsMonitor = provider.GetRequiredService<IOptionsMonitor<CosmosReplicaSinkOptions>>();
        CosmosReplicaSinkOptions options = optionsMonitor.Get("orders");

        Assert.Equal("orders-client", options.ClientKey);
        Assert.Equal(ReplicaProvisioningMode.CreateIfMissing, options.ProvisioningMode);
        Assert.Equal("configured-db", options.DatabaseId);
        Assert.Equal("configured-container", options.ContainerId);
        Assert.Equal(25, options.QueryBatchSize);
    }

    /// <summary>
    ///     Ensures multiple same-kind Cosmos registrations keep durable state isolated by sink key.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AddCosmosReplicaSinkShouldKeepSameKindRegistrationsIsolated()
    {
        StateContainerHarness east = new("east-db", "east-container");
        StateContainerHarness west = new("west-db", "west-container");
        ServiceCollection services = [];
        services.AddKeyedSingleton<CosmosClient>("east-client", (_, _) => east.Client.Object);
        services.AddKeyedSingleton<CosmosClient>("west-client", (_, _) => west.Client.Object);
        services.AddCosmosReplicaSink(
            "east",
            "east-client",
            options =>
            {
                options.DatabaseId = "east-db";
                options.ContainerId = "east-container";
            });
        services.AddCosmosReplicaSink(
            "west",
            "west-client",
            options =>
            {
                options.DatabaseId = "west-db";
                options.ContainerId = "west-container";
            });

        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        IReplicaSinkProvider eastProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("east");
        IReplicaSinkProvider westProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("west");
        ReplicaSinkDeliveryState eastState = new("Projection::east::orders-read::1", 10, null, 9);
        ReplicaSinkDeliveryState westState = new("Projection::west::orders-read::1", 20, null, 19);

        await stateStore.WriteAsync(eastState, CancellationToken.None);
        await stateStore.WriteAsync(westState, CancellationToken.None);
        ReplicaSinkDeliveryState? roundTrippedEast = await stateStore.ReadAsync(eastState.DeliveryKey, CancellationToken.None);
        ReplicaSinkDeliveryState? roundTrippedWest = await stateStore.ReadAsync(westState.DeliveryKey, CancellationToken.None);

        Assert.NotSame(eastProvider, westProvider);
        Assert.True(east.StateDocuments.ContainsKey(eastState.DeliveryKey));
        Assert.False(east.StateDocuments.ContainsKey(westState.DeliveryKey));
        Assert.True(west.StateDocuments.ContainsKey(westState.DeliveryKey));
        Assert.False(west.StateDocuments.ContainsKey(eastState.DeliveryKey));
        Assert.Equal(9, roundTrippedEast?.CommittedSourcePosition);
        Assert.Equal(19, roundTrippedWest?.CommittedSourcePosition);
    }

    private sealed class StateContainerHarness
    {
        public StateContainerHarness(
            string databaseId,
            string containerId
        )
        {
            Client = new();
            Database = new();
            Container = new();
            DatabaseId = databaseId;
            ContainerId = containerId;
            Setup();
        }

        public Mock<Container> Container { get; }

        public Mock<CosmosClient> Client { get; }

        public string ContainerId { get; }

        public Mock<Database> Database { get; }

        public string DatabaseId { get; }

        public Dictionary<string, CosmosReplicaSinkDeliveryStateDocument> StateDocuments { get; } = new(StringComparer.Ordinal);

        private static CosmosException CreateNotFound() =>
            new("not-found", HttpStatusCode.NotFound, 0, string.Empty, 0);

        private static ItemResponse<CosmosReplicaSinkDeliveryStateDocument> CreateItemResponse(
            CosmosReplicaSinkDeliveryStateDocument document
        )
        {
            Mock<ItemResponse<CosmosReplicaSinkDeliveryStateDocument>> response = new();
            response.SetupGet(r => r.Resource).Returns(document);
            return response.Object;
        }

        private void Setup()
        {
            Client.Setup(c => c.GetDatabase(DatabaseId)).Returns(Database.Object);
            Database.Setup(d => d.GetContainer(ContainerId)).Returns(Container.Object);
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkDeliveryStateDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string id, PartitionKey partitionKey, ItemRequestOptions? _, CancellationToken _) =>
                {
                    CosmosReplicaSinkDeliveryStateDocument? document = StateDocuments.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new PartitionKey(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkDeliveryStateDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.UpsertItemAsync(
                    It.IsAny<CosmosReplicaSinkDeliveryStateDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CosmosReplicaSinkDeliveryStateDocument document, PartitionKey? _, ItemRequestOptions? _, CancellationToken _) =>
                {
                    StateDocuments[document.DeliveryKey] = document;
                    return Task.FromResult(CreateItemResponse(document));
                });
        }
    }
}
