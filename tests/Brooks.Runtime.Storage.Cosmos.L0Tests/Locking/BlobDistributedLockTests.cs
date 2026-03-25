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
///     Tests for <see cref="BlobDistributedLock" /> covering Locking/BlobDistributedLock plan items.
/// </summary>
public sealed class BlobDistributedLockTests
{
    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     DisposeAsync should release the lease and ignore failures during disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DisposeAsyncReleasesLeaseIgnoresFailuresAsync()
    {
        // Arrange
        Mock<IBlobLeaseClient> leaseClient = new();

        // First call throws, subsequent call (if any) would succeed
        leaseClient.Setup(l => l.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "not found"));
        await using BlobDistributedLock sut = new(
            leaseClient.Object,
            "lock-id",
            1,
            2,
            "test-lock-key",
            Stopwatch.StartNew());

        // Act - should not throw
        await sut.DisposeAsync();

        // Assert - release attempted at most once due to disposed flag
        leaseClient.Verify(
            l => l.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     RenewAsync should call BlobLeaseClient.RenewAsync when elapsed time exceeds the threshold.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncCallsLeaseAfterThresholdAsync()
    {
        // Arrange
        Mock<IBlobLeaseClient> leaseClient = new();
        FakeTimeProvider timeProvider = new(BaseTime);
        leaseClient.Setup(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobLease>>());
        await using BlobDistributedLock sut = new(
            leaseClient.Object,
            "lock-id",
            5,
            15,
            "test-lock-key",
            Stopwatch.StartNew(),
            timeProvider);
        timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        await sut.RenewAsync();

        // Assert
        leaseClient.Verify(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     RenewAsync should be a no-op (no lease call) when elapsed time is below the renewal threshold.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncNoOpBeforeThresholdAsync()
    {
        // Arrange
        Mock<IBlobLeaseClient> leaseClient = new();
        FakeTimeProvider timeProvider = new(BaseTime);

        // leaseDurationSeconds=15, thresholdSeconds=5
        await using BlobDistributedLock sut = new(
            leaseClient.Object,
            "lock-id",
            5,
            15,
            "test-lock-key",
            Stopwatch.StartNew(),
            timeProvider);

        // Act
        await sut.RenewAsync();

        // Assert
        leaseClient.Verify(
            l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     RenewAsync should throw InvalidOperationException when the lease is lost (409/404).
    /// </summary>
    /// <param name="status">The HTTP status code to simulate (409 or 404).</param>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Theory]
    [InlineData(409)]
    [InlineData(404)]
    public async Task RenewAsyncThrowsOnLeaseLostAsync(
        int status
    )
    {
        // Arrange
        Mock<IBlobLeaseClient> leaseClient = new();
        FakeTimeProvider timeProvider = new(BaseTime);
        leaseClient.Setup(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(status, "conflict"));
        await using BlobDistributedLock sut = new(
            leaseClient.Object,
            "lock-id",
            1,
            2,
            "test-lock-key",
            Stopwatch.StartNew(),
            timeProvider);
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenewAsync());
    }
}