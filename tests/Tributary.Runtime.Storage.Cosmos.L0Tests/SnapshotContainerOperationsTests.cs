using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotContainerOperations" />.
/// </summary>
/// <remarks>
///     This class is the single point of contact with the Cosmos SDK, so tests here
///     verify correct SDK interaction patterns including retry, exception handling,
///     and document operations.
/// </remarks>
public sealed class SnapshotContainerOperationsTests
{
    private const string TestDocumentId = "test-doc-id";

    private const string TestPartitionKey = "test-partition";

    private static CosmosException CreateCosmosNotFound() =>
        new("not-found", HttpStatusCode.NotFound, 0, string.Empty, 0);

    private static SnapshotContainerOperations CreateOperations(
        Mock<Container> container,
        IRetryPolicy? retryPolicy = null,
        SnapshotStorageOptions? options = null
    )
    {
        retryPolicy ??= new PassThroughRetryPolicy();
        options ??= new();
        return new(
            container.Object,
            Options.Create(options),
            retryPolicy,
            NullLogger<SnapshotContainerOperations>.Instance);
    }

    /// <summary>
    ///     Verifies that constructor throws when container is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenContainerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotContainerOperations(
            null!,
            Options.Create(new SnapshotStorageOptions()),
            new PassThroughRetryPolicy(),
            NullLogger<SnapshotContainerOperations>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when options is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenOptionsIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotContainerOperations(
            container.Object,
            null!,
            new PassThroughRetryPolicy(),
            NullLogger<SnapshotContainerOperations>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when retryPolicy is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenRetryPolicyIsNull()
    {
        Mock<Container> container = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotContainerOperations(
            container.Object,
            Options.Create(new SnapshotStorageOptions()),
            null!,
            NullLogger<SnapshotContainerOperations>.Instance));
    }

    /// <summary>
    ///     Ensures DeleteDocumentAsync returns false when document not found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteDocumentAsyncShouldReturnFalseWhenNotFound()
    {
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                TestDocumentId,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosNotFound());
        SnapshotContainerOperations ops = CreateOperations(container);
        bool result = await ops.DeleteDocumentAsync(TestPartitionKey, TestDocumentId, CancellationToken.None);
        Assert.False(result);
    }

    /// <summary>
    ///     Ensures DeleteDocumentAsync returns true when document is deleted.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteDocumentAsyncShouldReturnTrueWhenDeleted()
    {
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<SnapshotDocument>(
                TestDocumentId,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        SnapshotContainerOperations ops = CreateOperations(container);
        bool result = await ops.DeleteDocumentAsync(TestPartitionKey, TestDocumentId, CancellationToken.None);
        Assert.True(result);
    }

    /// <summary>
    ///     Ensures QuerySnapshotIdsAsync forwards dynamic page sizing and drains an empty intermediate page.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task QuerySnapshotIdsAsyncShouldForwardDynamicBatchSizeAndDrainEmptyPage()
    {
        Mock<Container> container = new();
        Type dtoType =
            typeof(SnapshotContainerOperations).GetNestedType("SnapshotIdVersionDto", BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Snapshot query DTO type was not found.");
        MethodInfo queryMethod = typeof(SnapshotQueryTestAdapter).GetMethod(
                                     nameof(SnapshotQueryTestAdapter.RunAsync),
                                     BindingFlags.Static | BindingFlags.NonPublic) ??
                                 throw new InvalidOperationException("Snapshot query test method was not found.");
        List<IEnumerable<(string Id, long Version)>> pages =
        [
            new List<(string Id, long Version)>
            {
                ("snapshot-1", 1),
            },
            new List<(string Id, long Version)>(),
            new List<(string Id, long Version)>
            {
                ("snapshot-2", 2),
            },
        ];
        Func<Mock<Container>, SnapshotStorageOptions, SnapshotContainerOperations> operationsFactory =
            (services, options) => CreateOperations(services, options: options);
        Task<(int CapturedBatchSize, bool HasMoreResults, IReadOnlyList<(string Id, long Version)> Items)> queryTask =
            (Task<(int CapturedBatchSize, bool HasMoreResults, IReadOnlyList<(string Id, long Version)> Items)>)(
                queryMethod.MakeGenericMethod(dtoType)
                    .Invoke(
                        null,
                        new object?[]
                        {
                            container,
                            pages,
                            operationsFactory,
                            TestPartitionKey,
                            TestContext.Current.CancellationToken,
                        }) ??
                throw new InvalidOperationException("Snapshot query test could not be started."));
        (int CapturedBatchSize, bool HasMoreResults, IReadOnlyList<(string Id, long Version)> Items) result =
            await queryTask;
        Assert.Equal(-1, result.CapturedBatchSize);
        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal("snapshot-1", item.Id);
                Assert.Equal(1, item.Version);
            },
            item =>
            {
                Assert.Equal("snapshot-2", item.Id);
                Assert.Equal(2, item.Version);
            });
        Assert.False(result.HasMoreResults);
    }

    /// <summary>
    ///     Ensures ReadDocumentAsync returns document when found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadDocumentAsyncShouldReturnDocumentWhenFound()
    {
        SnapshotDocument expectedDoc = new()
        {
            Id = TestDocumentId,
            Data = new byte[] { 1, 2, 3 },
        };
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<SnapshotDocument>(
                TestDocumentId,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>(r => r.Resource == expectedDoc));
        SnapshotContainerOperations ops = CreateOperations(container);
        SnapshotDocument? result = await ops.ReadDocumentAsync(
            TestPartitionKey,
            TestDocumentId,
            CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(TestDocumentId, result.Id);
        Assert.Equal(expectedDoc.Data, result.Data);
    }

    /// <summary>
    ///     Ensures ReadDocumentAsync returns null when not found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadDocumentAsyncShouldReturnNullWhenNotFound()
    {
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<SnapshotDocument>(
                TestDocumentId,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosNotFound());
        SnapshotContainerOperations ops = CreateOperations(container);
        SnapshotDocument? result = await ops.ReadDocumentAsync(
            TestPartitionKey,
            TestDocumentId,
            CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Ensures UpsertDocumentAsync calls container with correct parameters.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task UpsertDocumentAsyncShouldCallContainer()
    {
        SnapshotDocument doc = new()
        {
            Id = TestDocumentId,
            SnapshotPartitionKey = TestPartitionKey,
        };
        Mock<Container> container = new();
        container.Setup(c => c.UpsertItemAsync(
                doc,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<SnapshotDocument>>());
        SnapshotContainerOperations ops = CreateOperations(container);
        await ops.UpsertDocumentAsync(TestPartitionKey, doc, CancellationToken.None);
        container.Verify(
            c => c.UpsertItemAsync(
                doc,
                It.Is<PartitionKey>(pk => pk.Equals(new(TestPartitionKey))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures UpsertDocumentAsync throws when document is null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task UpsertDocumentAsyncShouldThrowWhenDocumentIsNull()
    {
        Mock<Container> container = new();
        SnapshotContainerOperations ops = CreateOperations(container);
        await Assert.ThrowsAsync<ArgumentNullException>(() => ops.UpsertDocumentAsync(
            TestPartitionKey,
            null!,
            CancellationToken.None));
    }
}