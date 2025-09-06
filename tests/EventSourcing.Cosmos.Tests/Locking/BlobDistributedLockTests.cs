using System.Reflection;

using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Mississippi.EventSourcing.Cosmos.Locking;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Locking;

/// <summary>
///     Tests for <see cref="BlobDistributedLock" /> covering Locking/BlobDistributedLock plan items.
/// </summary>
public sealed class BlobDistributedLockTests
{
    /// <summary>
    ///     RenewAsync should be a no-op (no lease call) when elapsed time is below the renewal threshold.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncNoOpBeforeThresholdAsync()
    {
        // Arrange
        Mock<BlobLeaseClient> leaseClient = new();

        // leaseDurationSeconds=15, thresholdSeconds=5
        await using BlobDistributedLock sut = new(leaseClient.Object, "lock-id", 5, 15);

        // Act
        await sut.RenewAsync();

        // Assert
        leaseClient.Verify(
            l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     RenewAsync should call BlobLeaseClient.RenewAsync when elapsed time exceeds the threshold.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncCallsLeaseAfterThresholdAsync()
    {
        // Arrange
        Mock<BlobLeaseClient> leaseClient = new();
        leaseClient.Setup(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobLease>>());
        await using BlobDistributedLock sut = new(leaseClient.Object, "lock-id", 5, 15);

        // Force lastRenewalTime far in the past to exceed threshold
        SetPrivateField(sut, "lastRenewalTime", DateTimeOffset.UtcNow - TimeSpan.FromHours(1));

        // Act
        await sut.RenewAsync();

        // Assert
        leaseClient.Verify(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
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
        Mock<BlobLeaseClient> leaseClient = new();
        leaseClient.Setup(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(status, "conflict"));
        await using BlobDistributedLock sut = new(leaseClient.Object, "lock-id", 1, 2);
        SetPrivateField(sut, "lastRenewalTime", DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenewAsync());
    }

    /// <summary>
    ///     DisposeAsync should release the lease and ignore failures during disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DisposeAsyncReleasesLeaseIgnoresFailuresAsync()
    {
        // Arrange
        Mock<BlobLeaseClient> leaseClient = new();

        // First call throws, subsequent call (if any) would succeed
        leaseClient.Setup(l => l.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "not found"));
        await using BlobDistributedLock sut = new(leaseClient.Object, "lock-id", 1, 2);

        // Act - should not throw
        await sut.DisposeAsync();

        // Assert - release attempted at most once due to disposed flag
        leaseClient.Verify(
            l => l.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static void SetPrivateField<T>(
        object instance,
        string fieldName,
        T value
    )
    {
        FieldInfo? field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }
}