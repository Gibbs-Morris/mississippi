using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageProvider" />.
/// </summary>
public sealed class SnapshotStorageProviderTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 5);

    /// <summary>
    ///     Ensures constructor throws when repository is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotStorageProvider(
            null!,
            NullLogger<SnapshotStorageProvider>.Instance));
    }

    /// <summary>
    ///     Ensures the provider exposes the expected format.
    /// </summary>
    [Fact]
    public void FormatShouldReturnAzureBlobStorage()
    {
        SnapshotStorageProvider provider = new(
            Mock.Of<ISnapshotBlobRepository>(),
            NullLogger<SnapshotStorageProvider>.Instance);
        Assert.Equal("azure-blob-storage", provider.Format);
    }

    /// <summary>
    ///     Ensures delete-all forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        await provider.DeleteAllAsync(StreamKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAllAsync(StreamKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures delete forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        await provider.DeleteAsync(SnapshotKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures prune validates arguments and delegates to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        List<int> retain = [2];
        await provider.PruneAsync(StreamKey, retain, CancellationToken.None);
        repository.Verify(r => r.PruneAsync(StreamKey, retain, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures reads return repository results.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        repository.Setup(r => r.ReadAsync(SnapshotKey, CancellationToken.None)).ReturnsAsync(envelope);
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Same(envelope, result);
    }

    /// <summary>
    ///     Ensures writes forward to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        await provider.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        repository.Verify(r => r.WriteAsync(SnapshotKey, envelope, CancellationToken.None), Times.Once);
    }
}
