using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Azure.Cosmos;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotCosmosRepository" />.
/// </summary>
public sealed class SnapshotCosmosRepositoryTests
{
    private static readonly SnapshotStreamKey StreamKey = new("type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 3);

    private static CosmosException CreateCosmosNotFound() =>
        new("not-found", HttpStatusCode.NotFound, 0, string.Empty, 0);

    private static CosmosException CreateCosmosServiceUnavailable() =>
        new("service-unavailable", HttpStatusCode.ServiceUnavailable, 0, string.Empty, 0);

    private static SnapshotCosmosRepository CreateRepository(
        Mock<Container> container,
        ISnapshotQueryService? queryService = null,
        IMapper<SnapshotDocument, SnapshotStorageModel>? documentToStorageMapper = null,
        IMapper<SnapshotStorageModel, SnapshotEnvelope>? storageToEnvelopeMapper = null,
        IMapper<SnapshotWriteModel, SnapshotStorageModel>? writeModelToStorageMapper = null,
        IMapper<SnapshotStorageModel, SnapshotDocument>? storageToDocumentMapper = null
    )
    {
        queryService ??= new EmptyQueryService();
        documentToStorageMapper ??= new StubMapper<SnapshotDocument, SnapshotStorageModel>(new());
        storageToEnvelopeMapper ??= new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new());
        writeModelToStorageMapper ??= new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new());
        storageToDocumentMapper ??= new StubMapper<SnapshotStorageModel, SnapshotDocument>(new());
        return new(
            container.Object,
            queryService,
            documentToStorageMapper,
            storageToEnvelopeMapper,
            writeModelToStorageMapper,
            storageToDocumentMapper,
            new PassThroughRetryPolicy());
    }

    private sealed class EmptyQueryService : ISnapshotQueryService
    {
        public async IAsyncEnumerable<SnapshotIdVersion> ReadIdsAsync(
            SnapshotStreamKey streamKey,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            yield break;
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

    private sealed class StubMapper<TInput, TOutput> : IMapper<TInput, TOutput>
    {
        private readonly TOutput output;

        public StubMapper(
            TOutput output
        ) =>
            this.output = output;

        public TOutput Map(
            TInput input
        ) =>
            output;
    }

    /// <summary>
    ///     Test query service that returns a predefined list of snapshot IDs.
    /// </summary>
    private sealed class TestQueryService(IEnumerable<SnapshotIdVersion> items) : ISnapshotQueryService
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<SnapshotIdVersion> ReadIdsAsync(
            SnapshotStreamKey streamKey,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            foreach (SnapshotIdVersion item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Verifies that constructor throws when container is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenContainerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            null!,
            new EmptyQueryService(),
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Verifies that constructor throws when documentToStorageMapper is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenDocumentToStorageMapperIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            new EmptyQueryService(),
            null!,
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Verifies that constructor throws when queryService is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenQueryServiceIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            null!,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Verifies that constructor throws when retryPolicy is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenRetryPolicyIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            new EmptyQueryService(),
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            null!));
    }

    /// <summary>
    ///     Verifies that constructor throws when storageToDocumentMapper is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenStorageToDocumentMapperIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            new EmptyQueryService(),
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            null!,
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Verifies that constructor throws when storageToEnvelopeMapper is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenStorageToEnvelopeMapperIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            new EmptyQueryService(),
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            null!,
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Verifies that constructor throws when writeModelToStorageMapper is null.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void ConstructorShouldThrowWhenWriteModelToStorageMapperIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            container.Object,
            new EmptyQueryService(),
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            null!,
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            new PassThroughRetryPolicy()));
    }

    /// <summary>
    ///     Ensures DeleteAllAsync deletes all snapshots returned by the query service.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task DeleteAllAsyncShouldDeleteAllSnapshotsFromQueryService()
    {
        List<string> deletedIds = [];
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                It.IsAny<string>(),
                It.Is<PartitionKey>(pk => pk.Equals(new(StreamKey.ToString()))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, ItemRequestOptions, CancellationToken>((
                id,
                _,
                _,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        ISnapshotQueryService queryService = new TestQueryService(
        [
            new("snap-1", 1),
            new("snap-2", 2),
            new("snap-3", 3),
        ]);
        SnapshotCosmosRepository repository = CreateRepository(container, queryService);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        Assert.Equal(["snap-1", "snap-2", "snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures DeleteAllAsync handles empty query results gracefully.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task DeleteAllAsyncShouldHandleEmptyQueryResults()
    {
        Mock<Container> container = new();
        SnapshotCosmosRepository repository = CreateRepository(container, new EmptyQueryService());
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        container.Verify(
            c => c.DeleteItemAsync<SnapshotDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Ensures delete ignores not-found responses.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task DeleteAsyncShouldIgnoreNotFound()
    {
        bool deleteCalled = false;
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => deleteCalled = true)
            .ThrowsAsync(CreateCosmosNotFound());
        SnapshotCosmosRepository repository = CreateRepository(container);
        await repository.DeleteAsync(SnapshotKey, CancellationToken.None);
        Assert.True(deleteCalled);
    }

    /// <summary>
    ///     Ensures delete rethrows non-NotFound Cosmos exceptions.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task DeleteAsyncShouldRethrowNonNotFoundExceptions()
    {
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosServiceUnavailable());
        SnapshotCosmosRepository repository = CreateRepository(container);
        await Assert.ThrowsAsync<CosmosException>(() => repository.DeleteAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Ensures delete succeeds when item exists.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task DeleteAsyncShouldSucceedWhenItemExists()
    {
        bool deleteCalled = false;
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => deleteCalled = true)
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        SnapshotCosmosRepository repository = CreateRepository(container);
        await repository.DeleteAsync(SnapshotKey, CancellationToken.None);
        Assert.True(deleteCalled);
    }

    /// <summary>
    ///     Ensures PruneAsync always retains the maximum version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task PruneAsyncShouldAlwaysRetainMaxVersion()
    {
        List<string> deletedIds = [];
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                It.IsAny<string>(),
                It.Is<PartitionKey>(pk => pk.Equals(new(StreamKey.ToString()))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, ItemRequestOptions, CancellationToken>((
                id,
                _,
                _,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        ISnapshotQueryService queryService = new TestQueryService(
        [
            new("snap-3", 3),
            new("snap-5", 5),
        ]);
        SnapshotCosmosRepository repository = CreateRepository(container, queryService);

        // No modulus matches, but max (5) should be retained
        await repository.PruneAsync(StreamKey, [10], CancellationToken.None);

        // Only snap-3 deleted; snap-5 is max so retained
        Assert.Equal(["snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures PruneAsync deletes snapshots that don't match the modulus rules.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task PruneAsyncShouldDeleteSnapshotsNotMatchingModulus()
    {
        List<string> deletedIds = [];
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                It.IsAny<string>(),
                It.Is<PartitionKey>(pk => pk.Equals(new(StreamKey.ToString()))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, ItemRequestOptions, CancellationToken>((
                id,
                _,
                _,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        ISnapshotQueryService queryService = new TestQueryService(
        [
            new("snap-1", 1),
            new("snap-2", 2),
            new("snap-3", 3),
            new("snap-4", 4),
            new("snap-5", 5),
        ]);
        SnapshotCosmosRepository repository = CreateRepository(container, queryService);

        // Retain versions divisible by 2 (2, 4) and the max (5)
        await repository.PruneAsync(StreamKey, [2], CancellationToken.None);

        // snap-1 and snap-3 should be deleted (not divisible by 2, not max)
        Assert.Equal(["snap-1", "snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures PruneAsync handles empty query results gracefully.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task PruneAsyncShouldHandleEmptyQueryResults()
    {
        Mock<Container> container = new();
        SnapshotCosmosRepository repository = CreateRepository(container, new EmptyQueryService());
        await repository.PruneAsync(StreamKey, [1], CancellationToken.None);
        container.Verify(
            c => c.DeleteItemAsync<SnapshotDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Ensures read rethrows non-NotFound Cosmos exceptions.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task ReadAsyncShouldRethrowNonNotFoundExceptions()
    {
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosServiceUnavailable());
        SnapshotCosmosRepository repository = CreateRepository(container);
        await Assert.ThrowsAsync<CosmosException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Ensures reads return mapped envelopes.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task ReadAsyncShouldReturnEnvelope()
    {
        SnapshotDocument document = new()
        {
            Data = new byte[] { 1 },
            DataContentType = "ct",
        };
        SnapshotStorageModel storage = new()
        {
            Data = document.Data,
            DataContentType = document.DataContentType,
            StreamKey = StreamKey,
            Version = SnapshotKey.Version,
        };
        SnapshotEnvelope envelope = new()
        {
            Data = document.Data.ToImmutableArray(),
            DataContentType = document.DataContentType,
        };
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>(r => r.Resource == document));
        SnapshotCosmosRepository repository = CreateRepository(
            container,
            null,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(envelope),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(document));
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(envelope.Data, result!.Data);
        Assert.Equal(envelope.DataContentType, result.DataContentType);
    }

    /// <summary>
    ///     Ensures reads return null on not found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenNotFound()
    {
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<SnapshotDocument>(
                "3",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosNotFound());
        SnapshotCosmosRepository repository = CreateRepository(container);
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Ensures writes map and upsert documents.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AllureEpic("Snapshots")]
    [Fact]
    public async Task WriteAsyncShouldUpsertDocument()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)7),
            DataContentType = "bin",
        };
        SnapshotStorageModel storage = new()
        {
            StreamKey = StreamKey,
            Version = SnapshotKey.Version,
            Data = [.. envelope.Data],
            DataContentType = envelope.DataContentType,
        };
        SnapshotDocument document = new()
        {
            Id = "3",
            SnapshotPartitionKey = StreamKey.ToString(),
        };
        bool upsertCalled = false;
        Mock<Container> container = new();
        container.Setup(c => c.UpsertItemAsync(
                document,
                It.Is<PartitionKey>(pk => pk.Equals(new(StreamKey.ToString()))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => upsertCalled = true)
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        SnapshotCosmosRepository repository = CreateRepository(
            container,
            null,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(envelope),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(document));
        await repository.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        Assert.True(upsertCalled);
    }
}