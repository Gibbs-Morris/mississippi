using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Cosmos.Locking;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Locking;

/// <summary>
///     Tests for <see cref="BlobDistributedLockManager" /> lease acquisition paths.
/// </summary>
public sealed class BlobDistributedLockManagerTests
{
    /// <summary>
    ///     Validates initial acquire path creates container and blob when missing.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AcquireLockAsyncCreatesContainerAndBlobOnFirstAcquireAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        svc.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(lease.Object);
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), DateTimeOffset.UtcNow, "lease-1"),
                    Mock.Of<Response>()))
            .Verifiable();
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts), factory.Object);

        // Act
        await using IDistributedLock l = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15));

        // Assert
        lease.Verify(
            lc => lc.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        Assert.NotNull(l);
    }

    /// <summary>
    ///     Ensures the manager does not perform extra retries once the first acquire succeeds (kills statement-mutation
    ///     removing the loop break).
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncStopsAfterFirstSuccessfulAttemptAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        svc.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(lease.Object);
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), DateTimeOffset.UtcNow, "lease-first"),
                    Mock.Of<Response>()))
            .Verifiable();
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts), factory.Object);

        // Act
        await using IDistributedLock handle = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15));

        // Assert
        Assert.NotNull(handle);
        lease.Verify(
            l => l.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "A successful first acquire should break out of the retry loop (mutant removing break would cause >1 calls).");
    }

    /// <summary>
    ///     Validates retry behavior when lease acquire encounters a 409 conflict.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AcquireLockAsyncRetriesOnLeaseConflictAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        svc.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(lease.Object);
        int calls = 0;
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                if (calls == 1)
                {
                    throw new RequestFailedException(409, "conflict");
                }

                return Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), DateTimeOffset.UtcNow, "lease-2"),
                    Mock.Of<Response>());
            });
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts), factory.Object);

        // Act
        await using IDistributedLock l = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15));

        // Assert
        Assert.NotNull(l);
        Assert.True(calls >= 2);
    }

    /// <summary>
    ///     Validates that acquire throws when unable to obtain a lease.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AcquireLockAsyncThrowsWhenUnableToAcquireAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        svc.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(lease.Object);
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "conflict"));
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts), factory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15)));
    }

    /// <summary>
    ///     Captures requested duration and ensures it flows into the lease acquire call.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AcquireLockAsyncCreatesLeaseWithRequestedDurationAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> lease = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        svc.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(lease.Object);
        TimeSpan captured = TimeSpan.Zero;
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                TimeSpan d,
                RequestConditions? conditions,
                CancellationToken ct
            ) => captured = d)
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), DateTimeOffset.UtcNow, "lease-3"),
                    Mock.Of<Response>()));
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts), factory.Object);

        // Act
        await using IDistributedLock lockHandle = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(13));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(13), captured);
    }
}
