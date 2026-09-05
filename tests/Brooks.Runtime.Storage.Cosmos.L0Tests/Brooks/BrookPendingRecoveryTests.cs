using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Verifies committed, incomplete, and unavailable evidence at recovery boundaries.
/// </summary>
public sealed class BrookPendingRecoveryTests
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookPendingRecoveryTests" /> class.
    /// </summary>
    public BrookPendingRecoveryTests()
    {
        WriterLock.Setup(value => value.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Repository.Setup(value => value.GetCursorDocumentAsync(BrookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        Repository.Setup(value => value.GetPendingCursorDocumentAsync(BrookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        RetryPolicy.Setup(value => value.ExecuteAsync(
                It.IsAny<Func<Task<CursorStorageModel?>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<CursorStorageModel?>> operation,
                CancellationToken _
            ) => operation());
        RetryPolicy.Setup(value => value.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> operation,
                CancellationToken _
            ) => operation());
    }

    private static BrookKey BrookId => new("test", "pending-recovery");

    private Mock<IDistributedLockManager> LockManager { get; } = new(MockBehavior.Strict);

    private Mock<ICosmosRepository> Repository { get; } = new(MockBehavior.Strict);

    private Mock<IRetryPolicy> RetryPolicy { get; } = new(MockBehavior.Strict);

    private Mock<IDistributedLock> WriterLock { get; } = new(MockBehavior.Strict);

    private BrookRecoveryService CreateService() =>
        new(
            Repository.Object,
            RetryPolicy.Object,
            LockManager.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);

    private void SetPending(
        long originalPosition,
        long targetPosition
    ) =>
        Repository.Setup(value => value.GetPendingCursorDocumentAsync(BrookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    OriginalPosition = new(originalPosition),
                    Position = new(targetPosition),
                });

    /// <summary>
    ///     Preserves committed events after a commit acknowledgement or pending cleanup was lost.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CommittedPendingBatchOnlyRemovesPendingMetadata()
    {
        Repository.Setup(value => value.GetCursorDocumentAsync(BrookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(4),
                });
        SetPending(1, 4);
        Repository.Setup(value => value.DeletePendingCursorAsync(BrookId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        BrookPosition result = await CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object);
        Assert.Equal(4, result.Value);
        Repository.Verify(value => value.DeletePendingCursorAsync(BrookId, It.IsAny<CancellationToken>()), Times.Once);
        Repository.Verify(
            value => value.DeleteEventAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Repository.Verify(
            value => value.EventExistsAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Leaves ownership of an existing writer lease with its caller.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task HeldWriterLockIsNotReacquiredOrDisposed()
    {
        BrookPosition result = await CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object);
        Assert.True(result.NotSet);
        LockManager.VerifyNoOtherCalls();
        WriterLock.Verify(value => value.DisposeAsync(), Times.Never);
    }

    /// <summary>
    ///     Rejects pending evidence that cannot be related to committed history.
    /// </summary>
    /// <param name="originalPosition">The position preceding the pending operation.</param>
    /// <param name="targetPosition">The intended final position.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(1, 4)]
    [InlineData(-1, -1)]
    public async Task InconsistentPendingEvidenceDoesNotMutateHistory(
        long originalPosition,
        long targetPosition
    )
    {
        SetPending(originalPosition, targetPosition);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object));
        Repository.Verify(value => value.DeletePendingCursorAsync(BrookId, It.IsAny<CancellationToken>()), Times.Never);
        Repository.Verify(
            value => value.DeleteEventAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Uses complete range evidence to commit a large pending operation or remove all its partial writes.
    /// </summary>
    /// <param name="isComplete">Whether every intended event exists.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LargePendingRangeRequiresEveryEvent(
        bool isComplete
    )
    {
        CursorStorageModel? cursor = null;
        Repository.Setup(value => value.GetCursorDocumentAsync(BrookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => cursor);
        SetPending(-1, 10);
        ISet<long> positions = Enumerable.Range(0, isComplete ? 11 : 10).Select(value => (long)value).ToHashSet();
        Repository.Setup(value => value.GetExistingEventPositionsAsync(BrookId, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);
        Repository.Setup(value => value.CommitCursorPositionAsync(BrookId, 10, It.IsAny<CancellationToken>()))
            .Callback(() => cursor = new()
            {
                Position = new(10),
            })
            .Returns(Task.CompletedTask);
        Repository.Setup(value => value.DeleteEventAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Repository.Setup(value => value.DeletePendingCursorAsync(BrookId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        BrookPosition result = await CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object);
        Assert.Equal(isComplete ? 10 : -1, result.Value);
        Repository.Verify(
            value => value.CommitCursorPositionAsync(BrookId, 10, It.IsAny<CancellationToken>()),
            isComplete ? Times.Once : Times.Never);
        Repository.Verify(
            value => value.DeleteEventAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Exactly(isComplete ? 0 : 11));
    }

    /// <summary>
    ///     Prevents committing pending history when recovery loses its lease.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task LostLeasePreventsRecoveryCommit()
    {
        SetPending(-1, 0);
        Repository.Setup(value => value.EventExistsAsync(BrookId, 0, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        WriterLock.SetupSequence(value => value.RenewAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new InvalidOperationException("Lease lost."));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object));
        Repository.Verify(
            value => value.CommitCursorPositionAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Waits for the writer's lease before inspecting or recovering storage.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task RecoveryWaitsForWriterBeforeReadingStorage()
    {
        TaskCompletionSource<IDistributedLock> lease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LockManager.Setup(value => value.AcquireLockAsync(
                BrookId.ToString(),
                TimeSpan.FromSeconds(60),
                It.IsAny<CancellationToken>()))
            .Returns(lease.Task);
        WriterLock.Setup(value => value.DisposeAsync()).Returns(default(ValueTask));
        Task<BrookPosition> recovery = CreateService().GetOrRecoverCursorPositionAsync(BrookId);
        Repository.VerifyNoOtherCalls();
        lease.SetResult(WriterLock.Object);
        BrookPosition result = await recovery;
        Assert.True(result.NotSet);
        WriterLock.Verify(value => value.DisposeAsync(), Times.Once);
    }

    /// <summary>
    ///     Preserves pending history when event existence cannot be established.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task UncertainEventReadDoesNotRollBack()
    {
        SetPending(-1, 0);
        Repository.Setup(value => value.EventExistsAsync(BrookId, 0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Event read acknowledgement was lost."));
        await Assert.ThrowsAsync<TimeoutException>(() =>
            CreateService().GetOrRecoverCursorPositionAsync(BrookId, WriterLock.Object));
        Repository.Verify(value => value.DeletePendingCursorAsync(BrookId, It.IsAny<CancellationToken>()), Times.Never);
        Repository.Verify(
            value => value.DeleteEventAsync(BrookId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}