using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Verifies the optional manager clock seam without changing requested duration or recovery decisions.
/// </summary>
public sealed class BlobDistributedLockManagerClockTests
{
    /// <summary>
    ///     A manager-created lock observes the supplied monotonic clock and retains the configured duration.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task AcquiredLockUsesSuppliedClockAsync()
    {
        DateTimeOffset initial = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(initial);
        Mock<BlobServiceClient> service = new();
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        Mock<IBlobLeaseClient> client = new();
        Mock<IBlobLeaseClientFactory> factory = new();
        service.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);
        container.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<BlobContainerEncryptionScopeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        factory.Setup(f => f.Create(It.IsAny<BlobClient>(), It.IsAny<string?>())).Returns(client.Object);
        client.Setup(c => c.AcquireAsync(
                TimeSpan.FromSeconds(60),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobLease(new("\"etag\""), initial, "clock-lease"),
                    Mock.Of<Response>()));
        client.Setup(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobLease>>());
        BlobDistributedLockManager manager = new(
            service.Object,
            Options.Create(new BrookStorageOptions()),
            factory.Object,
            NullLogger<BlobDistributedLockManager>.Instance,
            clock);
        await using IDistributedLock lease = await manager.AcquireLockAsync(
            "clock-brook",
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken);
        await lease.RenewAsync(TestContext.Current.CancellationToken);
        client.Verify(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        clock.Advance(TimeSpan.FromSeconds(39));
        await lease.RenewAsync(TestContext.Current.CancellationToken);
        client.Verify(c => c.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(
            c => c.AcquireAsync(
                TimeSpan.FromSeconds(60),
                It.IsAny<RequestConditions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}