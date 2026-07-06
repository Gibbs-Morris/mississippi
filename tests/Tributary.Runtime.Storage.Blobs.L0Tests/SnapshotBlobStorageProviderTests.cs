using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Abstractions;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageProvider" />.
/// </summary>
public sealed class SnapshotBlobStorageProviderTests
{
    private static readonly SnapshotStreamKey StreamKey = new(
        "TEST.BROOK",
        "BankAccountBalance",
        "acct-123",
        "reducers-hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 5);

    /// <summary>
    ///     Verifies constructor argument validation.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotBlobStorageProvider(
            null!,
            NullLogger<SnapshotBlobStorageProvider>.Instance));
    }

    /// <summary>
    ///     Verifies delete-all delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.DeleteAllAsync(StreamKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAllAsync(StreamKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies delete delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.DeleteAsync(SnapshotKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies the provider format identifier is stable.
    /// </summary>
    [Fact]
    public void FormatShouldReturnAzureBlob()
    {
        SnapshotBlobStorageProvider provider = new(
            Mock.Of<ISnapshotBlobRepository>(),
            NullLogger<SnapshotBlobStorageProvider>.Instance);
        Assert.Equal("azure-blob", provider.Format);
    }

    /// <summary>
    ///     Verifies prune validates arguments and delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldDelegate()
    {
        List<int> retainModuli =
        [
            2,
        ];
        Mock<ISnapshotBlobRepository> repository = new();
        repository.Setup(r => r.PruneAsync(StreamKey, retainModuli, CancellationToken.None)).ReturnsAsync(0);
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.PruneAsync(StreamKey, retainModuli, CancellationToken.None);
        repository.Verify(r => r.PruneAsync(StreamKey, retainModuli, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies prune rejects null retain sets.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldThrowWhenRetainModuliNull()
    {
        SnapshotBlobStorageProvider provider = new(
            Mock.Of<ISnapshotBlobRepository>(),
            NullLogger<SnapshotBlobStorageProvider>.Instance);
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.PruneAsync(
            StreamKey,
            null!,
            CancellationToken.None));
    }

    /// <summary>
    ///     Verifies read delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        repository.Setup(r => r.ReadAsync(SnapshotKey, CancellationToken.None)).ReturnsAsync(envelope);
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Same(envelope, result);
        repository.Verify(r => r.ReadAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies write delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        repository.Verify(r => r.WriteAsync(SnapshotKey, envelope, CancellationToken.None), Times.Once);
    }
}