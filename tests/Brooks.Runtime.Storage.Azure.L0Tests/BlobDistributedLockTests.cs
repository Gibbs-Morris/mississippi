using System;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Deterministic transport-backed tests for <see cref="BlobDistributedLock" />.
/// </summary>
public sealed class BlobDistributedLockTests
{
    /// <summary>
    ///     Lease renewals are skipped before the threshold and emitted once the lease is close enough to expiry.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenewAsyncSkipsBeforeThresholdAndRenewsAfterThreshold()
    {
        using AzureBlobTransportTestContext context = new();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero));
        Sha256StreamPathEncoder encoder = new();
        BrookKey brookId = new("orders", "123");
        string lockPath = encoder.GetLockPath(brookId);

        context.Transport.SeedBlob(context.StorageOptions.LockContainerName, lockPath, BinaryData.FromString("lock"), "lease-1");

        await using BlobDistributedLock distributedLock = CreateLock(context, lockPath, "lease-1", timeProvider);

        await distributedLock.RenewAsync();
        Assert.DoesNotContain(context.Handler.Requests, request => request.EndsWith($"/{context.StorageOptions.LockContainerName}/{lockPath}?comp=lease", StringComparison.Ordinal));

        timeProvider.Advance(TimeSpan.FromSeconds(45));
        await distributedLock.RenewAsync();

        Assert.Contains(context.Handler.Requests, request => request.EndsWith($"/{context.StorageOptions.LockContainerName}/{lockPath}?comp=lease", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Lease renewal failures are translated into retryable Brooks Azure exceptions.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenewAsyncThrowsRetryableExceptionWhenLeaseLost()
    {
        using AzureBlobTransportTestContext context = new();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero));
        Sha256StreamPathEncoder encoder = new();
        BrookKey brookId = new("orders", "123");
        string lockPath = encoder.GetLockPath(brookId);

        context.Transport.SeedBlob(context.StorageOptions.LockContainerName, lockPath, BinaryData.FromString("lock"), "different-lease");

        await using BlobDistributedLock distributedLock = CreateLock(context, lockPath, "lease-1", timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(45));

        BrookStorageRetryableException exception = await Assert.ThrowsAsync<BrookStorageRetryableException>(() => distributedLock.RenewAsync());

        Assert.Contains("lease was lost", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Lease disposal tolerates already-lost leases.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task DisposeAsyncSwallowsAlreadyLostLease()
    {
        using AzureBlobTransportTestContext context = new();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero));
        Sha256StreamPathEncoder encoder = new();
        BrookKey brookId = new("orders", "123");
        string lockPath = encoder.GetLockPath(brookId);

        context.Transport.SeedBlob(context.StorageOptions.LockContainerName, lockPath, BinaryData.FromString("lock"), "different-lease");

        BlobDistributedLock distributedLock = CreateLock(context, lockPath, "lease-1", timeProvider);

        await distributedLock.DisposeAsync();

        Assert.Single(
            context.Handler.Requests,
            request => request.EndsWith($"/{context.StorageOptions.LockContainerName}/{lockPath}?comp=lease", StringComparison.Ordinal));
    }

    private static BlobDistributedLock CreateLock(
        AzureBlobTransportTestContext context,
        string lockPath,
        string lockId,
        FakeTimeProvider timeProvider
    )
    {
        BlobLeaseClient leaseClient = context.BlobServiceClient
            .GetBlobContainerClient(context.StorageOptions.LockContainerName)
            .GetBlobClient(lockPath)
            .GetBlobLeaseClient(lockId);

        return new BlobDistributedLock(
            leaseClient,
            lockId,
            context.StorageOptions.LeaseDurationSeconds,
            context.StorageOptions.LeaseRenewalThresholdSeconds,
            timeProvider);
    }
}