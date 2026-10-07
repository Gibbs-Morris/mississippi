using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Composes the real manager, Blob lock, writer, recovery and repository around controlled SDK boundaries.
/// </summary>
public sealed class EventBrookWriterManagerCompositionTests
{
    /// <summary>
    ///     The shared clock drives actual service renewals through held pending deletion in either batch path.
    /// </summary>
    /// <param name="large">Whether the two events use separate batches.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealManagerClockCoversPendingDeletionAsync(
        bool large
    )
    {
        DateTimeOffset initial = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        LeaseTestTimeProvider clock = new(initial);
        Mock<BlobServiceClient> service = new();
        Mock<BlobContainerClient> blobContainer = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> client = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        service.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(blobContainer.Object);
        blobContainer.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response<BlobContainerInfo>>());
        blobContainer.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(client.Object);
        client.Setup(c => c.AcquireAsync(
                TimeSpan.FromSeconds(60),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), initial, "composed-lease"),
                    Mock.Of<Response>()));
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response<BlobLease>>());
        Mock<Container> container = new(MockBehavior.Strict);
        CursorDocument cursor = new()
        {
            Id = "cursor",
            Position = 0,
        };
        Mock<ItemResponse<CursorDocument>> cursorResponse = new();
        cursorResponse.SetupGet(r => r.Resource).Returns(cursor);
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorResponse.Object);
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor-pending",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("No pending cursor", HttpStatusCode.NotFound, 0, "pending-read", 0));
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<CursorDocument>>());
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        container.Setup(c => c.UpsertItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<CursorDocument>>());
        TaskCompletionSource deleteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishDelete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        container.Setup(c => c.DeleteItemAsync<CursorDocument>(
                "cursor-pending",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                deleteEntered.TrySetResult();
                await finishDelete.Task.WaitAsync(CancellationToken.None);
                return Mock.Of<ItemResponse<CursorDocument>>();
            });
        Mock<IRetryPolicy> retry = new();
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> operation, CancellationToken _) => operation());
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<CursorStorageModel?>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CursorStorageModel?>> operation, CancellationToken _) => operation());
        retry.Setup(r => r.ExecuteAsync(
                It.IsAny<Func<Task<ItemResponse<CursorDocument>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<ItemResponse<CursorDocument>>> operation, CancellationToken _) => operation());
        Mock<IMapper<CursorDocument, CursorStorageModel>> cursorMapper = new();
        cursorMapper.Setup(m => m.Map(cursor))
            .Returns(
                new CursorStorageModel
                {
                    Position = new(0),
                });
        Mock<IMapper<BrookEvent, EventStorageModel>> eventMapper = new();
        eventMapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns((BrookEvent e) => new()
            {
                EventId = e.Id,
            });
        IOptions<BrookStorageOptions> options = Options.Create(
            new BrookStorageOptions
            {
                MaxEventsPerBatch = large ? 1 : 100,
            });
        BlobDistributedLockManager locks = new(
            service.Object,
            options,
            factory.Object,
            NullLogger<BlobDistributedLockManager>.Instance,
            clock);
        CosmosRepository repository = new(
            container.Object,
            retry.Object,
            cursorMapper.Object,
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        BrookRecoveryService recovery = new(
            repository,
            retry.Object,
            locks,
            options,
            NullLogger<BrookRecoveryService>.Instance);
        EventBrookWriter writer = new(
            repository,
            locks,
            new BatchSizeEstimator(),
            retry.Object,
            options,
            eventMapper.Object,
            recovery,
            NullLogger<EventBrookWriter>.Instance,
            clock);
        BrookKey key = new("test", "real-manager");
        using CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        Task<BrookPosition> append = writer.AppendEventsAsync(
            key,
            [
                new()
                {
                    Id = "event1",
                },
                new()
                {
                    Id = "event2",
                },
            ],
            new(0),
            watchdog.Token);
        try
        {
            await deleteEntered.Task.WaitAsync(watchdog.Token);
            client.Verify(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
            Task registration = clock.NextRegistration;
            clock.Advance(TimeSpan.FromSeconds(20));
            await registration.WaitAsync(watchdog.Token);
            client.Verify(
                c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            Assert.False(append.IsCompleted);
            finishDelete.TrySetResult();
            Assert.Equal(2, (await append.WaitAsync(watchdog.Token)).Value);
        }
        finally
        {
            finishDelete.TrySetResult();
            await append.WaitAsync(CancellationToken.None);
        }

        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        container.Verify(
            c => c.UpsertItemAsync(
                It.Is<CursorDocument>(d => d.Position == 2),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(c => c.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, clock.ActiveTimers);
    }
}