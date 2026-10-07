using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Verifies the append-only forced renewal overload without weakening ordinary renewal controls.
/// </summary>
public sealed class BlobDistributedLockForcedRenewalTests
{
    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Cancels the real caller source after service dispatch and retains its exact cancellation cause.
    /// </summary>
    /// <param name="caller">The cancellation source owned by the calling test.</param>
    /// <param name="failure">The exact provider cancellation failure.</param>
    /// <returns>The controlled service lease client.</returns>
    private static Mock<IBlobLeaseClient> CreateCancelingClient(
        CancellationTokenSource caller,
        OperationCanceledException failure
    )
    {
        Mock<IBlobLeaseClient> client = new();
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await caller.CancelAsync();
                throw failure;
            });
        return client;
    }

    /// <summary>
    ///     Forced renewal contacts the provider even while the ordinary overload skips a young lease.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ForcedRenewalContactsProviderForYoungLeaseAsync()
    {
        Mock<IBlobLeaseClient> client = new(MockBehavior.Strict);
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobLease>>());
        client.Setup(c => c.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<ReleasedObjectInfo>>());
        FakeTimeProvider clock = new(BaseTime);
        await using BlobDistributedLock lease = new(
            client.Object,
            "lease",
            20,
            60,
            "brook",
            Stopwatch.StartNew(),
            clock);
        await lease.RenewAsync(TestContext.Current.CancellationToken);
        client.Verify(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        await lease.RenewAsync(true, TestContext.Current.CancellationToken);
        client.Verify(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Expected cancellation of an actual renewal retains the original token and exception.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ForcedRenewalPreservesExpectedCancellationAsync()
    {
        using CancellationTokenSource caller =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        OperationCanceledException failure = new(caller.Token);
        Mock<IBlobLeaseClient> client = CreateCancelingClient(caller, failure);
        await using BlobDistributedLock lease = new(
            client.Object,
            "lease",
            20,
            60,
            "brook",
            Stopwatch.StartNew(),
            new FakeTimeProvider(BaseTime));
        OperationCanceledException actual =
            await Assert.ThrowsAsync<OperationCanceledException>(() => lease.RenewAsync(true, caller.Token));
        Assert.Same(failure, actual);
        Assert.Equal(caller.Token, actual.CancellationToken);
    }

    /// <summary>
    ///     Forced renewal retains the provider failure as the cause for confirmed and uncertain loss.
    /// </summary>
    /// <param name="status">The provider status code.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task ForcedRenewalRetainsProviderFailureAsync(
        int status
    )
    {
        RequestFailedException failure = new(status, "Renewal failed");
        Mock<IBlobLeaseClient> client = new();
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        await using BlobDistributedLock lease = new(
            client.Object,
            "lease",
            20,
            60,
            "brook",
            Stopwatch.StartNew(),
            new FakeTimeProvider(BaseTime));
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            lease.RenewAsync(true, TestContext.Current.CancellationToken));
        Assert.Same(failure, actual.InnerException);
    }

    /// <summary>
    ///     Elapsed bookkeeping begins at request start rather than its delayed successful response.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task SuccessfulRenewalUsesRequestStartTimestampAsync()
    {
        FakeTimeProvider clock = new(BaseTime);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IBlobLeaseClient> client = new();
        int calls = 0;
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(CancellationToken.None);
                }

                return Mock.Of<Response<BlobLease>>();
            });
        await using BlobDistributedLock lease = new(
            client.Object,
            "lease",
            5,
            15,
            "brook",
            Stopwatch.StartNew(),
            clock);
        try
        {
            Task renewal = lease.RenewAsync(true, TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            clock.Advance(TimeSpan.FromSeconds(8));
            release.TrySetResult();
            await renewal.WaitAsync(CancellationToken.None);
            clock.Advance(TimeSpan.FromSeconds(1));
            await lease.RenewAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, calls);
        }
        finally
        {
            release.TrySetResult();
        }
    }
}