using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

using Moq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests the Cosmos-backed replica sink provider against mocked Cosmos SDK behavior.
/// </summary>
public sealed class CosmosReplicaSinkProviderTests
{
    private const string TestClientKey = "orders-client";

    private const string TestContractIdentity = "TestApp.Orders.MappedReplica.V1";

    private const string TestSinkKey = "orders";

    private const string TestTargetName = "orders-read";

    /// <summary>
    ///     Ensures the provider supports the happy-path onboarding flow.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldProvisionWriteAndInspect()
    {
        TestCosmosEnvironment environment = new();
        environment.ContainerExists = false;
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);

        await provider.EnsureTargetAsync(target, CancellationToken.None);
        ReplicaWriteResult result = await provider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-1"),
            CancellationToken.None);
        ReplicaTargetInspection inspection = await provider.InspectAsync(target, CancellationToken.None);

        Assert.True(environment.ContainerExists);
        Assert.Equal(ReplicaWriteOutcome.Applied, result.Outcome);
        Assert.True(inspection.TargetExists);
        Assert.Equal(10, inspection.LatestSourcePosition);
        Assert.Equal("payload-1", inspection.LatestPayload);
        Assert.Equal(1, inspection.WriteCount);
    }

    /// <summary>
    ///     Ensures delete-style writes clear the latest payload while preserving monotonic write fences.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldApplyDeleteWritesAsLatestState()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);

        await provider.EnsureTargetAsync(target, CancellationToken.None);
        await provider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-1"),
            CancellationToken.None);
        ReplicaWriteResult deleteResult = await provider.WriteAsync(
            new(target, "order-1", 11, ReplicaWriteMode.LatestState, TestContractIdentity, null)
            {
                IsDeleted = true,
            },
            CancellationToken.None);
        ReplicaTargetInspection inspection = await provider.InspectAsync(target, CancellationToken.None);

        Assert.Equal(ReplicaWriteOutcome.Applied, deleteResult.Outcome);
        Assert.Equal(11, inspection.LatestSourcePosition);
        Assert.Null(inspection.LatestPayload);
        Assert.Equal(2, inspection.WriteCount);
    }

    /// <summary>
    ///     Ensures duplicate and superseded writes are ignored.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldIgnoreDuplicateAndSupersededWrites()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);

        await provider.EnsureTargetAsync(target, CancellationToken.None);
        await provider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-1"),
            CancellationToken.None);

        ReplicaWriteResult duplicate = await provider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-1"),
            CancellationToken.None);
        ReplicaWriteResult superseded = await provider.WriteAsync(
            new(target, "order-1", 9, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-0"),
            CancellationToken.None);

        Assert.Equal(ReplicaWriteOutcome.DuplicateIgnored, duplicate.Outcome);
        Assert.Equal(ReplicaWriteOutcome.SupersededIgnored, superseded.Outcome);
    }

    /// <summary>
    ///     Ensures validation-only onboarding fails when the target does not already exist.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldRejectValidateOnlyWhenTargetIsMissing()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkOptions options = new()
        {
            ClientKey = TestClientKey,
            ProvisioningMode = ReplicaProvisioningMode.ValidateOnly,
        };
        CosmosReplicaSinkProvider provider = environment.CreateProvider(options);
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.ValidateOnly);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.EnsureTargetAsync(target, CancellationToken.None));

        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ReplicaProvisioningMode.ValidateOnly), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures writes fail until the target has been provisioned.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldRejectWritesBeforeProvisioning()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.WriteAsync(
                new(target, "order-1", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "payload-1"),
                CancellationToken.None));

        Assert.Contains("has not been provisioned", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures all provider operations reject mismatched client keys.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldRejectClientKeyMismatchAcrossOperations()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor mismatchedTarget = new(new("other-client", TestTargetName), ReplicaProvisioningMode.CreateIfMissing);
        ReplicaWriteRequest mismatchedRequest = new(
            mismatchedTarget,
            "order-1",
            10,
            ReplicaWriteMode.LatestState,
            TestContractIdentity,
            "payload-1");

        InvalidOperationException ensureException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.EnsureTargetAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException inspectException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.InspectAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException writeException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.WriteAsync(mismatchedRequest, CancellationToken.None));

        Assert.Contains(TestClientKey, ensureException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", ensureException.Message, StringComparison.Ordinal);
        Assert.Contains(TestClientKey, inspectException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", inspectException.Message, StringComparison.Ordinal);
        Assert.Contains(TestClientKey, writeException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", writeException.Message, StringComparison.Ordinal);
    }

    private static ReplicaTargetDescriptor CreateTarget(
        ReplicaProvisioningMode provisioningMode
    ) => new(new(TestClientKey, TestTargetName), provisioningMode);

    private sealed class FakeFeedIterator<TDocument> : FeedIterator<TDocument>, IDisposable
    {
        private readonly Queue<IReadOnlyList<TDocument>> pages;

        public FakeFeedIterator(
            IEnumerable<IReadOnlyList<TDocument>> pages
        ) => this.pages = new(pages);

        public override bool HasMoreResults => pages.Count > 0;

        public new void Dispose()
        {
            // nothing to dispose
        }

        public override Task<FeedResponse<TDocument>> ReadNextAsync(
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<TDocument> next = pages.Dequeue();
            Mock<FeedResponse<TDocument>> response = new();
            response.As<IEnumerable<TDocument>>().Setup(r => r.GetEnumerator()).Returns(() => next.GetEnumerator());
            return Task.FromResult(response.Object);
        }
    }

    private sealed class PassThroughRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) => operation();
    }

    private sealed class TestCosmosEnvironment
    {
        private readonly Dictionary<string, CosmosReplicaSinkTargetDeliveryDocument> deliveries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CosmosReplicaSinkTargetMarkerDocument> targets = new(StringComparer.Ordinal);

        public TestCosmosEnvironment()
        {
            TimeProvider = new(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
            Client = new();
            Database = new();
            Container = new();
            ContainerExists = true;
            Setup();
        }

        public Mock<Container> Container { get; }

        public bool ContainerExists { get; set; }

        private static CosmosException CreateConflict() =>
            new("conflict", HttpStatusCode.Conflict, 0, string.Empty, 0);

        private static CosmosException CreateNotFound() =>
            new("not-found", HttpStatusCode.NotFound, 0, string.Empty, 0);

        private static ItemResponse<TDocument> CreateItemResponse<TDocument>(
            TDocument document
        )
            where TDocument : class
        {
            Mock<ItemResponse<TDocument>> response = new();
            response.SetupGet(r => r.Resource).Returns(document);
            return response.Object;
        }

        private static ContainerResponse CreateContainerResponse(
            ContainerProperties properties
        )
        {
            Mock<ContainerResponse> response = new();
            response.SetupGet(r => r.Resource).Returns(properties);
            return response.Object;
        }

        private static DatabaseResponse CreateDatabaseResponse(
            Database database
        )
        {
            Mock<DatabaseResponse> response = new();
            response.SetupGet(r => r.Database).Returns(database);
            return response.Object;
        }

        private Mock<CosmosClient> Client { get; }

        private Mock<Database> Database { get; }

        private FakeTimeProvider TimeProvider { get; }

        public CosmosReplicaSinkProvider CreateProvider(
            CosmosReplicaSinkOptions? options = null
        )
        {
            options ??= new CosmosReplicaSinkOptions
            {
                ClientKey = TestClientKey,
                DatabaseId = ReplicaSinkCosmosDefaults.DatabaseId,
                ContainerId = ReplicaSinkCosmosDefaults.ContainerId,
                ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing,
            };
            CosmosReplicaSinkContainerOperations operations = new(
                TestSinkKey,
                options,
                Client.Object,
                Container.Object,
                new PassThroughRetryPolicy(),
                TimeProvider,
                NullLogger<CosmosReplicaSinkContainerOperations>.Instance);
            return new(TestSinkKey, options, operations, NullLogger<CosmosReplicaSinkProvider>.Instance);
        }

        private void Setup()
        {
            Client.Setup(c => c.GetDatabase(It.IsAny<string>())).Returns(Database.Object);
            Client.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<RequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => ContainerExists = true)
                .ReturnsAsync(() => CreateDatabaseResponse(Database.Object));
            Database.Setup(d => d.GetContainer(It.IsAny<string>())).Returns(Container.Object);
            Database.Setup(d => d.CreateContainerIfNotExistsAsync(
                    It.IsAny<ContainerProperties>(),
                    It.IsAny<int?>(),
                    It.IsAny<RequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => ContainerExists = true)
                .ReturnsAsync((ContainerProperties properties, int? _, RequestOptions? _, CancellationToken _) =>
                    CreateContainerResponse(new(properties.Id, properties.PartitionKeyPath)));
            Container.Setup(c => c.ReadContainerAsync(
                    It.IsAny<ContainerRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (!ContainerExists)
                    {
                        throw CreateNotFound();
                    }

                    return Task.FromResult(
                        CreateContainerResponse(new(ReplicaSinkCosmosDefaults.ContainerId, ReplicaSinkCosmosDefaults.PartitionKeyPath)));
                });
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkTargetMarkerDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string id, PartitionKey partitionKey, ItemRequestOptions? _, CancellationToken _) =>
                {
                    CosmosReplicaSinkTargetMarkerDocument? document = targets.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new PartitionKey(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkTargetMarkerDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.CreateItemAsync(
                    It.IsAny<CosmosReplicaSinkTargetMarkerDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CosmosReplicaSinkTargetMarkerDocument document, PartitionKey? _, ItemRequestOptions? _, CancellationToken _) =>
                {
                    if (targets.ContainsKey(document.TargetName))
                    {
                        return Task.FromException<ItemResponse<CosmosReplicaSinkTargetMarkerDocument>>(CreateConflict());
                    }

                    targets[document.TargetName] = document;
                    return Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkTargetDeliveryDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string id, PartitionKey partitionKey, ItemRequestOptions? _, CancellationToken _) =>
                {
                    CosmosReplicaSinkTargetDeliveryDocument? document = deliveries.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new PartitionKey(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkTargetDeliveryDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.UpsertItemAsync(
                    It.IsAny<CosmosReplicaSinkTargetDeliveryDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CosmosReplicaSinkTargetDeliveryDocument document, PartitionKey? _, ItemRequestOptions? _, CancellationToken _) =>
                {
                    deliveries[$"{document.TargetName}::{document.DeliveryKey}"] = document;
                    return Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.GetItemQueryIterator<CosmosReplicaSinkTargetDeliveryDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(() =>
                {
                    IReadOnlyList<CosmosReplicaSinkTargetDeliveryDocument> documents = deliveries.Values
                        .OrderByDescending(doc => doc.LatestSourcePosition)
                        .ThenByDescending(doc => doc.LastUpdatedAtUtc, StringComparer.Ordinal)
                        .ThenBy(doc => doc.DeliveryKey, StringComparer.Ordinal)
                        .ToArray();
                    return new FakeFeedIterator<CosmosReplicaSinkTargetDeliveryDocument>([documents]);
                });
        }
    }
}
