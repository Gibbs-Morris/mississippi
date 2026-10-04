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
///     Verifies single-batch commit diagnostics without discarding uncertain storage evidence.
/// </summary>
public sealed class EventBrookWriterSingleBatchCommitTests
{
    /// <summary>
    ///     Reports a failed commit while preserving the original exception and all remaining evidence.
    /// </summary>
    /// <param name="isCursorCommitted">Whether the cursor advanced before the acknowledgement failed.</param>
    /// <param name="isPendingDeleted">Whether the commit removed the pending attempt.</param>
    /// <param name="isCanceled">Whether the commit reports cancellation.</param>
    /// <param name="isLoggingEnabled">Whether the error diagnostic is enabled.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    public async Task CommitFailurePreservesEvidenceAndReportsAttemptAsync(
        bool isCursorCommitted,
        bool isPendingDeleted,
        bool isCanceled,
        bool isLoggingEnabled
    )
    {
        BrookKey key = new("test", "single-batch-commit");
        BrookPosition originalPosition = new(0);
        const long finalPosition = 2;
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
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, lease.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalPosition);
        List<long> retainedPositions = [originalPosition.Value];
        bool hasPendingEvidence = false;
        long cursor = originalPosition.Value;
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                originalPosition,
                finalPosition,
                It.IsAny<CancellationToken>()))
            .Callback(() => hasPendingEvidence = true)
            .ReturnsAsync("single-attempt");
        repository.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                1,
                It.IsAny<CancellationToken>()))
            .Callback<BrookKey, IReadOnlyList<EventStorageModel>, long, CancellationToken>((_, events, start, _) =>
                retainedPositions.AddRange(Enumerable.Range(0, events.Count).Select(offset => start + offset)))
            .Returns(Task.CompletedTask);
        Exception failure = isCanceled
            ? new OperationCanceledException("Cursor commit acknowledgement was canceled.")
            : new InvalidOperationException("Cursor commit acknowledgement failed.");
        repository.Setup(r => r.CommitCursorPositionAsync(
                key,
                finalPosition,
                "single-attempt",
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                cursor = isCursorCommitted ? finalPosition : originalPosition.Value;
                hasPendingEvidence = !isPendingDeleted;
            })
            .ThrowsAsync(failure);
        repository.Setup(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<BrookKey, long, CancellationToken>((_, position, _) => retainedPositions.Remove(position))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingCursorAsync(key, "single-attempt", It.IsAny<CancellationToken>()))
            .Callback(() => hasPendingEvidence = false)
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookWriter>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(isLoggingEnabled);
        EventBrookWriter writer = new(
            repository.Object,
            locks.Object,
            new BatchSizeEstimator(),
            retry.Object,
            Options.Create(new BrookStorageOptions()),
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
        Exception? thrown = await Record.ExceptionAsync(() => writer.AppendEventsAsync(
            key,
            events,
            originalPosition,
            TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        Assert.Equal(new long[] { 0, 1, 2 }, retainedPositions);
        Assert.Equal(!isPendingDeleted, hasPendingEvidence);
        Assert.Equal(isCursorCommitted ? finalPosition : originalPosition.Value, cursor);
        repository.Verify(
            r => r.AppendEventBatchAsync(
                key,
                It.Is<IReadOnlyList<EventStorageModel>>(batch => batch.Count == 2),
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            r => r.CommitCursorPositionAsync(key, finalPosition, "single-attempt", It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(
            r => r.DeletePendingCursorAsync(key, "single-attempt", It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id =>
                    (id.Id == 1013) && (id.Name == nameof(EventBrookWriterLoggerExtensions.CursorCommitFailed))),
                It.Is<It.IsAnyType>((state, _) =>
                    ((IReadOnlyList<KeyValuePair<string, object?>>)state).Contains(new("BrookId", key)) &&
                    ((IReadOnlyList<KeyValuePair<string, object?>>)state).Contains(
                        new("FinalPosition", finalPosition)) &&
                    ((IReadOnlyList<KeyValuePair<string, object?>>)state).Contains(
                        new(
                            "{OriginalFormat}",
                            "Cursor commit failed for brook '{BrookId}' at position {FinalPosition}; appended events were retained"))),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(isLoggingEnabled ? 1 : 0));
    }
}