using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

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
    [Fact(
        Skip = "Moq cannot setup extension method GetBlobLeaseClient; needs seam/wrapper. Skipping pending refactor.")]
    public async Task AcquireLockAsyncCreatesContainerAndBlobOnFirstAcquireAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<BlobLeaseClient> lease = new();
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
        blob.Setup(b => b.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        blob.Setup(b => b.GetBlobLeaseClient(It.IsAny<string?>())).Returns(lease.Object);
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(default(BlobLease), Mock.Of<Response>()))
            .Verifiable();
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts));

        // Act
        await using IDistributedLock l = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15));

        // Assert
        lease.Verify(
            lc => lc.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        Assert.NotNull(l);
    }

    /// <summary>
    ///     Validates retry behavior when lease acquire encounters a 409 conflict.
    /// </summary>
    [Fact(
        Skip = "Moq cannot setup extension method GetBlobLeaseClient; needs seam/wrapper. Skipping pending refactor.")]
    public async Task AcquireLockAsyncRetriesOnLeaseConflictAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<BlobLeaseClient> lease = new();
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
        blob.Setup(b => b.GetBlobLeaseClient(It.IsAny<string?>())).Returns(lease.Object);
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

                return Response.FromValue(default(BlobLease), Mock.Of<Response>());
            });
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts));

        // Act
        await using IDistributedLock l = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15));

        // Assert
        Assert.NotNull(l);
        Assert.True(calls >= 2);
    }

    /// <summary>
    ///     Validates that acquire throws when unable to obtain a lease.
    /// </summary>
    [Fact(
        Skip = "Moq cannot setup extension method GetBlobLeaseClient; needs seam/wrapper. Skipping pending refactor.")]
    public async Task AcquireLockAsyncThrowsWhenUnableToAcquireAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<BlobLeaseClient> lease = new();
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
        blob.Setup(b => b.GetBlobLeaseClient(It.IsAny<string?>())).Returns(lease.Object);
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "conflict"));
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AcquireLockAsync("k", TimeSpan.FromSeconds(15)));
    }

    /// <summary>
    ///     Captures requested duration and ensures it flows into the lease acquire call.
    /// </summary>
    [Fact(
        Skip = "Moq cannot setup extension method GetBlobLeaseClient; needs seam/wrapper. Skipping pending refactor.")]
    public async Task AcquireLockAsyncCreatesLeaseWithRequestedDurationAsync()
    {
        // Arrange
        BrookStorageOptions opts = new();
        Mock<BlobServiceClient> svc = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<BlobLeaseClient> lease = new();
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
        blob.Setup(b => b.GetBlobLeaseClient(It.IsAny<string?>())).Returns(lease.Object);
        TimeSpan captured = TimeSpan.Zero;
        lease.Setup(l => l.AcquireAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                TimeSpan d,
                RequestConditions? _,
                CancellationToken __
            ) => captured = d)
            .ReturnsAsync(Response.FromValue(default(BlobLease), Mock.Of<Response>()));
        BlobDistributedLockManager sut = new(svc.Object, Options.Create(opts));

        // Act
        await using IDistributedLock _lock = await sut.AcquireLockAsync("k", TimeSpan.FromSeconds(13));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(13), captured);
    }
}