using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Verifies that appends stop when pending creation does not return a usable attempt identity.
/// </summary>
public sealed class EventBrookWriterMissingPendingETagTests
{
    private sealed class DirectRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     Retains the created pending document without dispatching events or cursor publication.
    /// </summary>
    /// <param name="large">Whether the events require separate batches.</param>
    /// <param name="pendingETag">The unusable entity tag returned by successful pending creation.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    public async Task MissingPendingETagStopsAppendAndRetainsEvidenceAsync(
        bool large,
        string? pendingETag
    )
    {
        using CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(30));
        BrookKey key = new("test", "missing-pending-etag");
        CursorDocument? storedPending = null;
        List<string> createdEvents = new();
        Mock<ItemResponse<CursorDocument>> pendingResponse = new();
        pendingResponse.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.Created);
        pendingResponse.SetupGet(r => r.ETag).Returns(pendingETag!);
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                new PartitionKey(key.ToString()),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<CursorDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>((document, _, _, _) =>
                storedPending = document)
            .ReturnsAsync(pendingResponse.Object);
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                new PartitionKey(key.ToString()),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<EventDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>((document, _, _, _) =>
                createdEvents.Add(document.EventId))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        DirectRetryPolicy retry = new();
        CosmosRepository repository = new(
            container.Object,
            retry,
            Mock.Of<IMapper<CursorDocument, CursorStorageModel>>(),
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupGet(l => l.LockId).Returns("append-lease");
        lease.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.RenewAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease.Object);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(10));
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns((BrookEvent e) => new()
            {
                EventId = e.Id,
            });
        EventBrookWriter writer = new(
            repository,
            locks.Object,
            new BatchSizeEstimator(),
            retry,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = large ? 1 : 100,
                }),
            mapper.Object,
            recovery.Object,
            NullLogger<EventBrookWriter>.Instance,
            new FakeTimeProvider());
        BrookEvent[] events =
        [
            new()
            {
                Id = "event-11",
            },
            new()
            {
                Id = "event-12",
            },
        ];
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await writer.AppendEventsAsync(key, events, new(10), watchdog.Token));
        Assert.Contains("entity tag", failure.Message, StringComparison.Ordinal);
        Assert.NotNull(storedPending);
        Assert.Equal("cursor-pending", storedPending.Id);
        Assert.Equal(10, storedPending.OriginalPosition);
        Assert.Equal(12, storedPending.Position);
        Assert.Empty(createdEvents);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                new PartitionKey(key.ToString()),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>()), Times.Never);
        container.Verify(
            c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.DeleteItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.VerifyNoOtherCalls();
        lease.Verify(l => l.DisposeAsync(), Times.Once);
    }
}