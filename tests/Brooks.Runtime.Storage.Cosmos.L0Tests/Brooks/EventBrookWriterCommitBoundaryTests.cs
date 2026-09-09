using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

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
///     Verifies that a cursor commit failure cannot delete the events it may already cover.
/// </summary>
public sealed class EventBrookWriterCommitBoundaryTests
{
    /// <summary>
    ///     Preserves event and pending-cursor evidence when a commit attempt reports failure.
    /// </summary>
    /// <param name="isCursorCommitted">Whether the simulated cursor write completed before its acknowledgement failed.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitFailurePreservesEventsAndPendingEvidence(
        bool isCursorCommitted
    )
    {
        BrookKey key = new("test", "commit-boundary");
        Mock<ICosmosRepository> repository = new();
        Mock<IDistributedLockManager> locks = new();
        Mock<IDistributedLock> lease = new();
        locks.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease.Object);
        Mock<IRetryPolicy> retry = new();
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>((operation, _) => operation());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>())).Returns(new EventStorageModel());
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        List<long> retainedPositions = [0];
        bool hasPendingEvidence = false;
        long cursor = 0;
        repository.Setup(r => r.CreatePendingCursorAsync(key, new(0), 2, It.IsAny<CancellationToken>()))
            .Callback(() => hasPendingEvidence = true)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback<BrookKey, IReadOnlyList<EventStorageModel>, long, CancellationToken>((_, _, position, _) =>
                retainedPositions.Add(position))
            .Returns(Task.CompletedTask);
        InvalidOperationException failure = new("Cursor commit acknowledgement or pending cleanup failed.");
        repository.Setup(r => r.CommitCursorPositionAsync(key, 2, It.IsAny<CancellationToken>()))
            .Callback(() => cursor = isCursorCommitted ? 2 : 0)
            .ThrowsAsync(failure);
        repository.Setup(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<BrookKey, long, CancellationToken>((_, position, _) => retainedPositions.Remove(position))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingCursorAsync(key, It.IsAny<CancellationToken>()))
            .Callback(() => hasPendingEvidence = false)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrookKey _, long position, CancellationToken _) => retainedPositions.Contains(position));
        EventBrookWriter writer = new(
            repository.Object,
            locks.Object,
            new BatchSizeEstimator(),
            retry.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 1,
                }),
            mapper.Object,
            recovery.Object,
            NullLogger<EventBrookWriter>.Instance,
            new FakeTimeProvider());
        ImmutableArray<BrookEvent> events =
        [
            new()
            {
                Id = "first",
            },
            new()
            {
                Id = "second",
            },
        ];
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.AppendEventsAsync(key, events, new BrookPosition(0)));
        Assert.Same(failure, thrown);
        Assert.Equal(new long[] { 0, 1, 2 }, retainedPositions);
        Assert.True(hasPendingEvidence);
        Assert.Equal(isCursorCommitted ? 2 : 0, cursor);
        repository.Verify(r => r.CommitCursorPositionAsync(key, 2, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.DeletePendingCursorAsync(key, It.IsAny<CancellationToken>()), Times.Never);
    }
}