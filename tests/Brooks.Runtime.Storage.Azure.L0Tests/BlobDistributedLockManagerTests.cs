using System;
using System.Linq;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Deterministic transport-backed tests for <see cref="BlobDistributedLockManager" />.
/// </summary>
public sealed class BlobDistributedLockManagerTests
{
    /// <summary>
    ///     Lock acquisition creates the lock blob on demand before taking the Azure lease.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncCreatesMissingLockBlobAndAcquiresLease()
    {
        using AzureBlobTransportTestContext context = new();
        Sha256StreamPathEncoder encoder = new();
        BlobDistributedLockManager manager = new(
            context.BlobServiceClient,
            context.CreateOptions(),
            encoder);
        BrookKey brookId = new("orders", "123");
        string expectedLockPath = $"/{context.StorageOptions.LockContainerName}/{encoder.GetLockPath(brookId)}";

        await using IDistributedLock distributedLock = await manager.AcquireLockAsync(brookId, TimeSpan.FromSeconds(60));

        Assert.False(string.IsNullOrWhiteSpace(distributedLock.LockId));
        Assert.Equal(1, context.Handler.Requests.Count(request => request == $"HEAD {expectedLockPath}"));
        Assert.Equal(1, context.Handler.Requests.Count(request => request == $"PUT {expectedLockPath}"));
        Assert.Equal(1, context.Handler.Requests.Count(request => request == $"PUT {expectedLockPath}?comp=lease"));
    }

    /// <summary>
    ///     Lock acquisition retries on lease conflicts and eventually succeeds when the lease becomes available.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AcquireLockAsyncRetriesAfterLeaseConflict()
    {
        using AzureBlobTransportTestContext context = new();
        Sha256StreamPathEncoder encoder = new();
        BlobDistributedLockManager manager = new(
            context.BlobServiceClient,
            context.CreateOptions(),
            encoder);
        BrookKey brookId = new("orders", "123");
        string lockPath = encoder.GetLockPath(brookId);

        context.Transport.SeedBlob(context.StorageOptions.LockContainerName, lockPath, BinaryData.FromString("lock"));
        context.Transport.SetLeaseAcquireConflicts(context.StorageOptions.LockContainerName, lockPath, 1);

        await using IDistributedLock distributedLock = await manager.AcquireLockAsync(brookId, TimeSpan.FromSeconds(60));

        Assert.False(string.IsNullOrWhiteSpace(distributedLock.LockId));
        Assert.Equal(2, context.Handler.Requests.Count(request => request.EndsWith($"/{context.StorageOptions.LockContainerName}/{lockPath}?comp=lease", StringComparison.Ordinal)));
    }
}