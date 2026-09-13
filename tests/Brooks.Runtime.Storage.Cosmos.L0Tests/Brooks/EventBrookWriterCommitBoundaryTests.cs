using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
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
    ///     Does not compensate for a failed commit, including when the commit already removed pending metadata.
    /// </summary>
    /// <param name="isCursorCommitted">Whether the simulated cursor write completed before its acknowledgement failed.</param>
    /// <param name="isPendingDeleted">Whether the commit removed pending metadata before reporting failure.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CommitFailureDoesNotTriggerCompensatingDeletes(
        bool isCursorCommitted,
        bool isPendingDeleted
    )
    {
        const long finalPosition = 2;
        BrookPosition originalPosition = new(0);
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
            .ReturnsAsync(originalPosition);
        List<long> retainedPositions = [originalPosition.Value];
        bool hasPendingEvidence = false;
        long cursor = originalPosition.Value;
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                It.Is<BrookPosition>(p => p == originalPosition),
                finalPosition,
                It.IsAny<CancellationToken>()))
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
        repository.Setup(r => r.CommitCursorPositionAsync(key, finalPosition, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                cursor = isCursorCommitted ? finalPosition : originalPosition.Value;
                hasPendingEvidence = !isPendingDeleted;
            })
            .ThrowsAsync(failure);
        repository.Setup(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<BrookKey, long, CancellationToken>((_, position, _) => retainedPositions.Remove(position))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingCursorAsync(key, It.IsAny<CancellationToken>()))
            .Callback(() => hasPendingEvidence = false)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrookKey _, long position, CancellationToken _) => retainedPositions.Contains(position));
        Mock<ILogger<EventBrookWriter>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
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
            logger.Object,
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
            writer.AppendEventsAsync(key, events, originalPosition, TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 1013),
                It.Is<It.IsAnyType>((state, _) =>
                    ((IReadOnlyList<KeyValuePair<string, object?>>)state).Contains(new("BrookId", key)) &&
                    ((IReadOnlyList<KeyValuePair<string, object?>>)state).Contains(
                        new("FinalPosition", finalPosition))),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.Equal(new long[] { 0, 1, 2 }, retainedPositions.Order());
        Assert.Equal(!isPendingDeleted, hasPendingEvidence);
        Assert.Equal(isCursorCommitted ? finalPosition : originalPosition.Value, cursor);
        repository.Verify(
            r => r.CommitCursorPositionAsync(key, finalPosition, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.DeletePendingCursorAsync(key, It.IsAny<CancellationToken>()), Times.Never);
    }
}