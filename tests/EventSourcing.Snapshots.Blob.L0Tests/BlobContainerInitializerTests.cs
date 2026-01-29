using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Snapshots.Blob.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="BlobContainerInitializer" />.
/// </summary>
public sealed class BlobContainerInitializerTests
{
    /// <summary>
    ///     Ensures constructor throws when blobOperations is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenBlobOperationsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlobContainerInitializer(
            null!,
            NullLogger<BlobContainerInitializer>.Instance));
    }

    /// <summary>
    ///     Ensures constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        Mock<IBlobSnapshotOperations> operations = new();
        Assert.Throws<ArgumentNullException>(() => new BlobContainerInitializer(operations.Object, null!));
    }

    /// <summary>
    ///     Ensures StartAsync calls EnsureContainerExistsAsync.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task StartAsyncShouldEnsureContainerExists()
    {
        Mock<IBlobSnapshotOperations> operations = new();
        BlobContainerInitializer initializer = new(operations.Object, NullLogger<BlobContainerInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);
        operations.Verify(o => o.EnsureContainerExistsAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures cancellation is respected.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task StartAsyncShouldPassCancellationToken()
    {
        Mock<IBlobSnapshotOperations> operations = new();
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        BlobContainerInitializer initializer = new(operations.Object, NullLogger<BlobContainerInitializer>.Instance);
        await initializer.StartAsync(token);
        operations.Verify(o => o.EnsureContainerExistsAsync(token), Times.Once);
    }

    /// <summary>
    ///     Ensures StopAsync completes without error.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task StopAsyncShouldCompleteSuccessfully()
    {
        Mock<IBlobSnapshotOperations> operations = new();
        BlobContainerInitializer initializer = new(operations.Object, NullLogger<BlobContainerInitializer>.Instance);
        await initializer.StopAsync(CancellationToken.None);

        // Verify StopAsync completed without throwing
        Assert.True(true);
    }
}