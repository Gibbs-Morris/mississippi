using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

using Moq;


namespace MississippiTests.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Tests lock blob initialization when first callers race to create it.
/// </summary>
public sealed class BlobDistributedLockCreationTests
{
    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Response<BlobLease> CreateLeaseResponse(
        string leaseId
    ) =>
        Response.FromValue(BlobsModelFactory.BlobLease(new("\"etag\""), BaseTime, leaseId), Mock.Of<Response>());

    private static BlobDistributedLockManager CreateManager(
        Mock<BlobClient> blob,
        Mock<IBlobLeaseClient> lease,
        ILogger<BlobDistributedLockManager>? logger = null
    )
    {
        Mock<BlobServiceClient> service = new();
        Mock<BlobContainerClient> container = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        service.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        factory.Setup(f => f.Create(blob.Object, It.IsAny<string?>())).Returns(lease.Object);
        return new(
            service.Object,
            Options.Create(new BrookStorageOptions()),
            factory.Object,
            logger ?? NullLogger<BlobDistributedLockManager>.Instance);
    }

    /// <summary>
    ///     Preserves the five-attempt limit after another caller creates the lock blob.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncBoundsLeaseContentionAfterLosingCreationAsync()
    {
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Already created", "BlobAlreadyExists", null));
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Lease held", "LeaseAlreadyPresent", null));
        BlobDistributedLockManager sut = CreateManager(blob, lease);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AcquireLockAsync(
                nameof(AcquireLockAsyncBoundsLeaseContentionAfterLosingCreationAsync),
                TimeSpan.FromSeconds(15),
                TestContext.Current.CancellationToken));
        Assert.Equal("Failed to acquire blob lease for distributed lock after retries.", exception.Message);
        lease.Verify(
            l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                TestContext.Current.CancellationToken),
            Times.Exactly(5));
        blob.Verify(b => b.UploadAsync(It.IsAny<BinaryData>(), TestContext.Current.CancellationToken), Times.Once);
    }

    /// <summary>
    ///     Makes both first callers observe absence and lets the losing creator acquire after the winner releases.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncContinuesAfterConcurrentFirstCreationAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TaskCompletionSource<Response<bool>> absent = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource created = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource contended = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<BlobClient> winnerBlob = new();
        Mock<BlobClient> loserBlob = new();
        Mock<IBlobLeaseClient> winnerLease = new();
        Mock<IBlobLeaseClient> loserLease = new();
        Mock<ILogger<BlobDistributedLockManager>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        RequestFailedException creationConflict = new(409, "Already created", "BlobAlreadyExists", null);
        winnerBlob.Setup(b => b.ExistsAsync(cancellationToken)).Returns(absent.Task);
        loserBlob.Setup(b => b.ExistsAsync(cancellationToken)).Returns(absent.Task);
        winnerBlob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), cancellationToken))
            .Returns(() =>
            {
                created.SetResult();
                return Task.FromResult(Mock.Of<Response<BlobContentInfo>>());
            });
        loserBlob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), cancellationToken))
            .Returns(async () =>
            {
                await created.Task.WaitAsync(cancellationToken);
                throw creationConflict;
            });
        winnerLease.Setup(l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), cancellationToken))
            .Returns(() =>
            {
                held.SetResult();
                return Task.FromResult(CreateLeaseResponse("winner"));
            });
        winnerLease.Setup(l => l.ReleaseAsync(It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                released.SetResult();
                return Task.FromResult(Mock.Of<Response<ReleasedObjectInfo>>());
            });
        loserLease.SetupSequence(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                cancellationToken))
            .Returns(async () =>
            {
                await held.Task.WaitAsync(cancellationToken);
                contended.SetResult();
                throw new RequestFailedException(409, "Lease held", "LeaseAlreadyPresent", null);
            })
            .Returns(async () =>
            {
                await released.Task.WaitAsync(cancellationToken);
                return CreateLeaseResponse("loser");
            });
        BlobDistributedLockManager winner = CreateManager(winnerBlob, winnerLease);
        BlobDistributedLockManager loser = CreateManager(loserBlob, loserLease, logger.Object);
        string lockKey = nameof(AcquireLockAsyncContinuesAfterConcurrentFirstCreationAsync);
        Task<IDistributedLock> winnerTask = winner.AcquireLockAsync(
            lockKey,
            TimeSpan.FromSeconds(15),
            cancellationToken);
        Task<IDistributedLock> loserTask = loser.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(15), cancellationToken);
        winnerBlob.Verify(b => b.ExistsAsync(cancellationToken), Times.Once);
        loserBlob.Verify(b => b.ExistsAsync(cancellationToken), Times.Once);
        winnerBlob.Verify(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()), Times.Never);
        loserBlob.Verify(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()), Times.Never);
        absent.SetResult(Response.FromValue(false, Mock.Of<Response>()));
        await using IDistributedLock winnerHandle = await winnerTask;
        Task firstOutcome = await Task.WhenAny(loserTask, contended.Task).WaitAsync(cancellationToken);
        if (firstOutcome == loserTask)
        {
            await using IDistributedLock earlyHandle = await loserTask;
        }

        Assert.Same(contended.Task, firstOutcome);
        Assert.False(released.Task.IsCompleted);
        Assert.Equal("winner", winnerHandle.LockId);
        await winnerHandle.DisposeAsync();
        await using IDistributedLock loserHandle = await loserTask;
        Assert.Equal("loser", loserHandle.LockId);
        winnerLease.Verify(
            l => l.AcquireAsync(TimeSpan.FromSeconds(15), It.IsAny<RequestConditions?>(), cancellationToken),
            Times.Once);
        loserLease.Verify(
            l => l.AcquireAsync(TimeSpan.FromSeconds(15), It.IsAny<RequestConditions?>(), cancellationToken),
            Times.Exactly(2));
        winnerBlob.Verify(
            b => b.UploadAsync(It.Is<BinaryData>(data => data.ToString() == "lock"), cancellationToken),
            Times.Once);
        loserBlob.Verify(
            b => b.UploadAsync(It.Is<BinaryData>(data => data.ToString() == "lock"), cancellationToken),
            Times.Once);
        winnerBlob.VerifyNoOtherCalls();
        loserBlob.VerifyNoOtherCalls();
        IInvocation raceLog = Assert.Single(
            logger.Invocations,
            invocation => (invocation.Method.Name == "Log") &&
                          (Assert.IsType<EventId>(invocation.Arguments[1]).Id == 6));
        Assert.Equal(LogLevel.Debug, raceLog.Arguments[0]);
        Assert.Equal("LockBlobAlreadyExists", Assert.IsType<EventId>(raceLog.Arguments[1]).Name);
        Assert.Same(creationConflict, raceLog.Arguments[3]);
        IReadOnlyList<KeyValuePair<string, object?>> state =
            Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(raceLog.Arguments[2], false);
        Assert.Collection(
            state,
            field => Assert.Equal(new("LockKey", lockKey), field),
            field => Assert.Equal(
                new("{OriginalFormat}", "Lock blob for key '{LockKey}' was created by another caller"),
                field));
    }

    /// <summary>
    ///     Acquires the lease after a create race when debug logging is disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncIgnoresDisabledCreationRaceLoggingAsync()
    {
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<ILogger<BlobDistributedLockManager>> logger = new();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Already created", "BlobAlreadyExists", null));
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeaseResponse("successful"));
        BlobDistributedLockManager sut = CreateManager(blob, lease, logger.Object);
        await using IDistributedLock handle = await sut.AcquireLockAsync(
            nameof(AcquireLockAsyncIgnoresDisabledCreationRaceLoggingAsync),
            TimeSpan.FromSeconds(15),
            TestContext.Current.CancellationToken);
        Assert.Equal("successful", handle.LockId);
        Assert.DoesNotContain(logger.Invocations, invocation => invocation.Method.Name == "Log");
    }

    /// <summary>
    ///     Preserves cancellation during lease retry after the create race.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncPreservesCancellationAfterLosingCreationAsync()
    {
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        blob.Setup(b => b.ExistsAsync(cancellation.Token)).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), cancellation.Token))
            .ThrowsAsync(new RequestFailedException(409, "Already created", "BlobAlreadyExists", null));
        lease.Setup(l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), cancellation.Token))
            .Returns(async () =>
            {
                await cancellation.CancelAsync();
                throw new RequestFailedException(409, "Lease held", "LeaseAlreadyPresent", null);
            });
        BlobDistributedLockManager sut = CreateManager(blob, lease);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.AcquireLockAsync(
                nameof(AcquireLockAsyncPreservesCancellationAfterLosingCreationAsync),
                TimeSpan.FromSeconds(15),
                cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        lease.Verify(
            l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), cancellation.Token),
            Times.Once);
    }

    /// <summary>
    ///     Propagates every upload failure except the exact documented already-exists conflict.
    /// </summary>
    /// <param name="status">The response status.</param>
    /// <param name="errorCode">The provider error code.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(409, "ContainerBeingDeleted")]
    [InlineData(409, null)]
    [InlineData(412, "ConditionNotMet")]
    [InlineData(403, "AuthorizationPermissionMismatch")]
    [InlineData(500, "BlobAlreadyExists")]
    [InlineData(404, "BlobNotFound")]
    [InlineData(409, "blobAlreadyExists")]
    public async Task AcquireLockAsyncPropagatesUnrelatedUploadFailuresAsync(
        int status,
        string? errorCode
    )
    {
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        RequestFailedException uploadFailure = new(status, "Upload failed", errorCode, null);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(uploadFailure);
        BlobDistributedLockManager sut = CreateManager(blob, lease);
        RequestFailedException exception = await Assert.ThrowsAsync<RequestFailedException>(() => sut.AcquireLockAsync(
            nameof(AcquireLockAsyncPropagatesUnrelatedUploadFailuresAsync),
            TimeSpan.FromSeconds(15),
            TestContext.Current.CancellationToken));
        Assert.Same(uploadFailure, exception);
        lease.Verify(
            l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Propagates cancellation of the upload without attempting to acquire a lease.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncPropagatesUploadCancellationAsync()
    {
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        blob.Setup(b => b.ExistsAsync(cancellation.Token)).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), cancellation.Token))
            .Returns(async () =>
            {
                await cancellation.CancelAsync();
                return await Task.FromCanceled<Response<BlobContentInfo>>(cancellation.Token);
            });
        BlobDistributedLockManager sut = CreateManager(blob, lease);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.AcquireLockAsync(
                nameof(AcquireLockAsyncPropagatesUploadCancellationAsync),
                TimeSpan.FromSeconds(15),
                cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        lease.Verify(
            l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Keeps the existing-blob and successful first-create paths on the create-only overload.
    /// </summary>
    /// <param name="exists">Whether the blob already exists.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AcquireLockAsyncUsesExistingOrSuccessfullyCreatedBlobAsync(
        bool exists
    )
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        blob.Setup(b => b.ExistsAsync(cancellationToken)).ReturnsAsync(Response.FromValue(exists, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), cancellationToken))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        lease.Setup(l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), cancellationToken))
            .ReturnsAsync(CreateLeaseResponse("successful"));
        BlobDistributedLockManager sut = CreateManager(blob, lease);
        await using IDistributedLock handle = await sut.AcquireLockAsync(
            nameof(AcquireLockAsyncUsesExistingOrSuccessfullyCreatedBlobAsync),
            TimeSpan.FromSeconds(15),
            cancellationToken);
        Assert.Equal("successful", handle.LockId);
        blob.Verify(b => b.ExistsAsync(cancellationToken), Times.Once);
        blob.Verify(
            b => b.UploadAsync(It.Is<BinaryData>(data => data.ToString() == "lock"), cancellationToken),
            exists ? Times.Never() : Times.Once());
        blob.VerifyNoOtherCalls();
        lease.Verify(
            l => l.AcquireAsync(TimeSpan.FromSeconds(15), It.IsAny<RequestConditions?>(), cancellationToken),
            Times.Once);
    }
}