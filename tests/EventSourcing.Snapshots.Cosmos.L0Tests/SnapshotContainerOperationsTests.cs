using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotContainerOperations" />.
/// </summary>
/// <remarks>
///     This class is the single point of contact with the Cosmos SDK, so tests here
///     verify correct SDK interaction patterns including retry, exception handling,
///     and document operations.
/// </remarks>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Container Operations")]
public sealed class SnapshotContainerOperationsTests
{
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

    private const string TestDocumentId = "test-doc-id";

    private const string TestPartitionKey = "test-partition";

    private sealed class PassThroughRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
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
        Assert.Equal(TestDocumentId, result!.Id);
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