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

    private static ReplicaTargetDescriptor CreateTarget(
        ReplicaProvisioningMode provisioningMode
    ) =>
        new(new(TestClientKey, TestTargetName), provisioningMode);

    private sealed class FakeFeedIterator<TDocument>
        : FeedIterator<TDocument>,
          IDisposable
    {
        private readonly Queue<IReadOnlyList<TDocument>> pages;

        public FakeFeedIterator(
            IEnumerable<IReadOnlyList<TDocument>> pages
        ) =>
            this.pages = new(pages);

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
        ) =>
            operation();
    }

    private sealed class TestCosmosEnvironment
    {
        private readonly Dictionary<string, CosmosReplicaSinkTargetDeliveryDocument> deliveries =
            new(StringComparer.Ordinal);

        private readonly Queue<IReadOnlyList<CosmosReplicaSinkDeliveryStateDocument>> deliveryStateQueryPages = new();

        private readonly Dictionary<string, CosmosReplicaSinkDeliveryStateDocument> deliveryStates =
            new(StringComparer.Ordinal);

        private readonly Queue<IReadOnlyList<CosmosReplicaSinkTargetDeliveryDocument>> targetDeliveryQueryPages = new();

        private readonly Dictionary<string, CosmosReplicaSinkTargetMarkerDocument>
            targets = new(StringComparer.Ordinal);

        public TestCosmosEnvironment()
        {
            TimeProvider = new(new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
            Client = new();
            Database = new();
            Container = new();
            ContainerExists = true;
            Setup();
        }

        public Mock<Container> Container { get; }

        public bool ContainerExists { get; set; }

        public string ContainerPartitionKeyPath { get; set; } = ReplicaSinkCosmosDefaults.PartitionKeyPath;

        private Mock<CosmosClient> Client { get; }

        private Mock<Database> Database { get; }

        private FakeTimeProvider TimeProvider { get; }

        private static CosmosException CreateConflict() => new("conflict", HttpStatusCode.Conflict, 0, string.Empty, 0);

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

        private static ItemResponse<TDocument> CreateItemResponse<TDocument>(
            TDocument document
        )
            where TDocument : class
        {
            Mock<ItemResponse<TDocument>> response = new();
            response.SetupGet(r => r.Resource).Returns(document);
            return response.Object;
        }

        private static CosmosException CreateNotFound() =>
            new("not-found", HttpStatusCode.NotFound, 0, string.Empty, 0);

        public CosmosReplicaSinkProvider CreateProvider(
            CosmosReplicaSinkOptions? options = null
        )
        {
            options ??= new()
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

        public CosmosReplicaSinkContainerOperations CreateContainerOperations(
            CosmosReplicaSinkOptions? options = null
        )
        {
            options ??= new()
            {
                ClientKey = TestClientKey,
                DatabaseId = ReplicaSinkCosmosDefaults.DatabaseId,
                ContainerId = ReplicaSinkCosmosDefaults.ContainerId,
                ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing,
            };

            return new(
                TestSinkKey,
                options,
                Client.Object,
                Container.Object,
                new PassThroughRetryPolicy(),
                TimeProvider,
                NullLogger<CosmosReplicaSinkContainerOperations>.Instance);
        }

        public void QueueDeliveryStateQueryPage(
            params CosmosReplicaSinkDeliveryStateDocument[] documents
        ) => deliveryStateQueryPages.Enqueue(documents);

        public void QueueTargetDeliveryQueryPage(
            params CosmosReplicaSinkTargetDeliveryDocument[] documents
        ) => targetDeliveryQueryPages.Enqueue(documents);

        public void SeedTarget(
            string targetName
        ) => targets[targetName] = CosmosReplicaSinkTargetMarkerDocument.Create(targetName, TimeProvider.GetUtcNow());

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
                .ReturnsAsync((
                    ContainerProperties properties,
                    int? _,
                    RequestOptions? _,
                    CancellationToken _
                ) => CreateContainerResponse(new(properties.Id, ContainerPartitionKeyPath)));
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
                        CreateContainerResponse(
                            new(ReplicaSinkCosmosDefaults.ContainerId, ContainerPartitionKeyPath)));
                });
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkTargetMarkerDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    string id,
                    PartitionKey partitionKey,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
                {
                    CosmosReplicaSinkTargetMarkerDocument? document = targets.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkTargetMarkerDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.CreateItemAsync(
                    It.IsAny<CosmosReplicaSinkTargetMarkerDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    CosmosReplicaSinkTargetMarkerDocument document,
                    PartitionKey? _,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
                {
                    if (targets.ContainsKey(document.TargetName))
                    {
                        return Task.FromException<ItemResponse<CosmosReplicaSinkTargetMarkerDocument>>(
                            CreateConflict());
                    }

                    targets[document.TargetName] = document;
                    return Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkDeliveryStateDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    string id,
                    PartitionKey partitionKey,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
                {
                    CosmosReplicaSinkDeliveryStateDocument? document = deliveryStates.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkDeliveryStateDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.UpsertItemAsync(
                    It.IsAny<CosmosReplicaSinkDeliveryStateDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    CosmosReplicaSinkDeliveryStateDocument document,
                    PartitionKey? _,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
                {
                    deliveryStates[document.DeliveryKey] = document;
                    return Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.ReadItemAsync<CosmosReplicaSinkTargetDeliveryDocument>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    string id,
                    PartitionKey partitionKey,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
                {
                    CosmosReplicaSinkTargetDeliveryDocument? document = deliveries.Values.SingleOrDefault(doc =>
                        string.Equals(doc.Id, id, StringComparison.Ordinal) &&
                        partitionKey.Equals(new(doc.ReplicaPartitionKey)));
                    return document is null
                        ? Task.FromException<ItemResponse<CosmosReplicaSinkTargetDeliveryDocument>>(CreateNotFound())
                        : Task.FromResult(CreateItemResponse(document));
                });
            Container.Setup(c => c.UpsertItemAsync(
                    It.IsAny<CosmosReplicaSinkTargetDeliveryDocument>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((
                    CosmosReplicaSinkTargetDeliveryDocument document,
                    PartitionKey? _,
                    ItemRequestOptions? _,
                    CancellationToken _
                ) =>
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
                    IReadOnlyList<CosmosReplicaSinkTargetDeliveryDocument> documents =
                        targetDeliveryQueryPages.Count > 0
                            ? targetDeliveryQueryPages.Dequeue()
                            : deliveries.Values
                                .OrderByDescending(doc => doc.LatestSourcePosition)
                                .ThenByDescending(doc => doc.LastUpdatedAtUtc, StringComparer.Ordinal)
                                .ThenBy(doc => doc.DeliveryKey, StringComparer.Ordinal)
                                .ToArray();

                    return new FakeFeedIterator<CosmosReplicaSinkTargetDeliveryDocument>([documents]);
                });
            Container.Setup(c => c.GetItemQueryIterator<CosmosReplicaSinkDeliveryStateDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(() =>
                {
                    IReadOnlyList<CosmosReplicaSinkDeliveryStateDocument> documents =
                        deliveryStateQueryPages.Count > 0
                            ? deliveryStateQueryPages.Dequeue()
                            : deliveryStates.Values.ToArray();

                    return new FakeFeedIterator<CosmosReplicaSinkDeliveryStateDocument>([documents]);
                });
        }
    }

    /// <summary>
    ///     Ensures the provider exposes the stable Cosmos format and forwards container provisioning.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldExposeFormatAndEnsureContainer()
    {
        TestCosmosEnvironment environment = new()
        {
            ContainerExists = false,
        };
        CosmosReplicaSinkProvider provider = environment.CreateProvider();

        await provider.EnsureContainerAsync(CancellationToken.None);

        Assert.Equal(ReplicaSinkCosmosDefaults.FormatName, provider.Format);
        Assert.True(environment.ContainerExists);
    }

    /// <summary>
    ///     Ensures missing inspections report an absent target rather than throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldInspectMissingTargetsAsAbsent()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);

        ReplicaTargetInspection inspection = await provider.InspectAsync(target, CancellationToken.None);

        Assert.False(inspection.TargetExists);
        Assert.Equal(0, inspection.WriteCount);
        Assert.Null(inspection.LatestSourcePosition);
        Assert.Null(inspection.LatestPayload);
    }

    /// <summary>
    ///     Ensures already-provisioned targets validate successfully without requiring another create path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldValidateExistingProvisionedTargets()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor createIfMissingTarget = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);
        ReplicaTargetDescriptor validateOnlyTarget = CreateTarget(ReplicaProvisioningMode.ValidateOnly);

        await provider.EnsureTargetAsync(createIfMissingTarget, CancellationToken.None);
        await provider.EnsureTargetAsync(validateOnlyTarget, CancellationToken.None);
        ReplicaTargetInspection inspection = await provider.InspectAsync(validateOnlyTarget, CancellationToken.None);

        Assert.True(inspection.TargetExists);
        Assert.Equal(0, inspection.WriteCount);
        Assert.Null(inspection.LatestSourcePosition);
        Assert.Null(inspection.LatestPayload);
    }

    /// <summary>
    ///     Ensures validate-only container checks fail when the configured container is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldRejectValidateOnlyWhenContainerIsMissing()
    {
        TestCosmosEnvironment environment = new()
        {
            ContainerExists = false,
        };
        CosmosReplicaSinkOptions options = new()
        {
            ClientKey = TestClientKey,
            ProvisioningMode = ReplicaProvisioningMode.ValidateOnly,
        };
        CosmosReplicaSinkProvider provider = environment.CreateProvider(options);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.EnsureContainerAsync(CancellationToken.None));

        Assert.Contains(TestSinkKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ReplicaProvisioningMode.ValidateOnly), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures provider-level dead-letter and due-retry reads forward to the durable delivery-state partition.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldReadDeadLettersAndDueRetries()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaSinkDeliveryState newerDeadLetter = new(
            "delivery-dead-b",
            11,
            deadLetter: new(11, 1, "dead-b", "Dead B", new(2026, 3, 29, 12, 1, 0, TimeSpan.Zero)));
        ReplicaSinkDeliveryState olderDeadLetter = new(
            "delivery-dead-a",
            10,
            deadLetter: new(10, 1, "dead-a", "Dead A", new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero)));
        ReplicaSinkDeliveryState dueRetryA = new(
            "delivery-retry-a",
            20,
            null,
            null,
            new(
                20,
                1,
                "retry-a",
                "Retry A",
                new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero),
                new(2026, 3, 29, 12, 4, 0, TimeSpan.Zero)));
        ReplicaSinkDeliveryState dueRetryB = new(
            "delivery-retry-b",
            21,
            null,
            null,
            new(
                21,
                1,
                "retry-b",
                "Retry B",
                new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero),
                new(2026, 3, 29, 12, 5, 0, TimeSpan.Zero)));

        environment.QueueDeliveryStateQueryPage(
            CosmosReplicaSinkDeliveryStateDocument.FromDomain(newerDeadLetter),
            CosmosReplicaSinkDeliveryStateDocument.FromDomain(olderDeadLetter));
        environment.QueueDeliveryStateQueryPage(
            CosmosReplicaSinkDeliveryStateDocument.FromDomain(dueRetryA),
            CosmosReplicaSinkDeliveryStateDocument.FromDomain(dueRetryB));

        IReadOnlyList<ReplicaSinkDeliveryState> deadLetters = await provider.ReadDeadLettersAsync(
            2,
            CancellationToken.None);
        IReadOnlyList<ReplicaSinkDeliveryState> dueRetries = await provider.ReadDueRetriesAsync(
            new(2026, 3, 29, 12, 5, 0, TimeSpan.Zero),
            2,
            CancellationToken.None);

        Assert.Equal(["delivery-dead-b", "delivery-dead-a"], deadLetters.Select(static state => state.DeliveryKey));
        Assert.Equal(["delivery-retry-a", "delivery-retry-b"], dueRetries.Select(static state => state.DeliveryKey));
    }

    /// <summary>
    ///     Ensures zero-count state queries short-circuit to empty results.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldReturnEmptyCollectionsForZeroCountStateReads()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();

        IReadOnlyList<ReplicaSinkDeliveryState> deadLetters = await provider.ReadDeadLettersAsync(
            0,
            CancellationToken.None);
        IReadOnlyList<ReplicaSinkDeliveryState> dueRetries = await provider.ReadDueRetriesAsync(
            new(2026, 3, 29, 12, 5, 0, TimeSpan.Zero),
            0,
            CancellationToken.None);

        Assert.Empty(deadLetters);
        Assert.Empty(dueRetries);
    }

    /// <summary>
    ///     Ensures missing durable delivery-state documents round-trip as null.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldReadMissingDeliveryStateAsNull()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();

        ReplicaSinkDeliveryState? state = await provider.ReadStateAsync("missing-delivery", CancellationToken.None);

        Assert.Null(state);
    }

    /// <summary>
    ///     Ensures target creation tolerates idempotent conflicts that indicate the marker already exists.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkContainerOperationsShouldIgnoreTargetCreationConflicts()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkContainerOperations operations = environment.CreateContainerOperations();

        await operations.CreateTargetAsync(TestTargetName, CancellationToken.None);
        await operations.CreateTargetAsync(TestTargetName, CancellationToken.None);

        Assert.True(await operations.TargetExistsAsync(TestTargetName, CancellationToken.None));
    }

    /// <summary>
    ///     Ensures invalid container partition keys are rejected during container validation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkContainerOperationsShouldRejectInvalidPartitionKeys()
    {
        TestCosmosEnvironment environment = new()
        {
            ContainerPartitionKeyPath = "/wrong-partition",
        };
        CosmosReplicaSinkContainerOperations operations = environment.CreateContainerOperations();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await operations.EnsureContainerAsync(ReplicaProvisioningMode.CreateIfMissing, CancellationToken.None));

        Assert.Contains(ReplicaSinkCosmosDefaults.PartitionKeyPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("/wrong-partition", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures inspection chooses the newest delivery snapshot using source position, timestamp, and delivery-key tiebreakers.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkContainerOperationsShouldSelectNewestDeliveryAcrossTieBreakers()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkContainerOperations operations = environment.CreateContainerOperations();
        ReplicaTargetDescriptor target = CreateTarget(ReplicaProvisioningMode.CreateIfMissing);
        CosmosReplicaSinkTargetDeliveryDocument baseline = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-b", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "baseline"),
            1,
            new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument lowerSource = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-low", 9, ReplicaWriteMode.LatestState, TestContractIdentity, "lower-source"),
            2,
            new(2026, 3, 29, 12, 5, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument olderTimestamp = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-old-time", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "older-time"),
            3,
            new(2026, 3, 29, 11, 59, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument greaterKey = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-c", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "greater-key"),
            4,
            new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument smallerKey = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-a", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "smaller-key"),
            5,
            new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument newerTimestamp = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-new-time", 10, ReplicaWriteMode.LatestState, TestContractIdentity, "newer-time"),
            6,
            new(2026, 3, 29, 12, 1, 0, TimeSpan.Zero));
        CosmosReplicaSinkTargetDeliveryDocument higherSource = CosmosReplicaSinkTargetDeliveryDocument.Create(
            new(target, "order-high", 11, ReplicaWriteMode.LatestState, TestContractIdentity, "higher-source"),
            7,
            new(2026, 3, 29, 11, 0, 0, TimeSpan.Zero));

        environment.SeedTarget(TestTargetName);
        environment.QueueTargetDeliveryQueryPage(
            baseline,
            lowerSource,
            olderTimestamp,
            greaterKey,
            smallerKey,
            newerTimestamp,
            higherSource);

        CosmosReplicaSinkTargetInspectionSnapshot inspection = await operations.InspectTargetAsync(
            TestTargetName,
            CancellationToken.None);

        Assert.True(inspection.TargetExists);
        Assert.Equal(28, inspection.WriteCount);
        Assert.Equal(11, inspection.LatestSourcePosition);
        Assert.Equal("higher-source", inspection.LatestPayload);
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
    ///     Ensures all provider operations reject mismatched client keys.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkProviderShouldRejectClientKeyMismatchAcrossOperations()
    {
        TestCosmosEnvironment environment = new();
        CosmosReplicaSinkProvider provider = environment.CreateProvider();
        ReplicaTargetDescriptor mismatchedTarget = new(
            new("other-client", TestTargetName),
            ReplicaProvisioningMode.CreateIfMissing);
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
}