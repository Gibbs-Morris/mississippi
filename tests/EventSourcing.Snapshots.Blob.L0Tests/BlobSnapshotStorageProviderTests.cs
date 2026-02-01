using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="BlobSnapshotStorageProvider" />.
/// </summary>
public sealed class BlobSnapshotStorageProviderTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 5);

    /// <summary>
    ///     Creates a provider with default mocks.
    /// </summary>
    private static BlobSnapshotStorageProvider CreateProvider(
        Mock<IBlobSnapshotRepository>? repository = null
    )
    {
        repository ??= new();
        BlobSnapshotStorageOptions options = new();
        return new(repository.Object, Options.Create(options), NullLogger<BlobSnapshotStorageProvider>.Instance);
    }

    /// <summary>
    ///     Ensures archive access tier is rejected.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenAccessTierIsArchive()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        BlobSnapshotStorageOptions options = new()
        {
            DefaultAccessTier = AccessTier.Archive,
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlobSnapshotStorageProvider(
            repo.Object,
            Options.Create(options),
            NullLogger<BlobSnapshotStorageProvider>.Instance));
    }

    /// <summary>
    ///     Ensures constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotStorageProvider(
            repo.Object,
            Options.Create(new BlobSnapshotStorageOptions()),
            null!));
    }

    /// <summary>
    ///     Ensures constructor throws when options is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenOptionsIsNull()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotStorageProvider(
            repo.Object,
            null!,
            NullLogger<BlobSnapshotStorageProvider>.Instance));
    }

    /// <summary>
    ///     Ensures constructor throws when repository is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotStorageProvider(
            null!,
            Options.Create(new BlobSnapshotStorageOptions()),
            NullLogger<BlobSnapshotStorageProvider>.Instance));
    }

    /// <summary>
    ///     Ensures delete-all forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDelegate()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        await provider.DeleteAllAsync(StreamKey, CancellationToken.None);
        repo.Verify(r => r.DeleteAllAsync(StreamKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures delete forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAsyncShouldDelegate()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        await provider.DeleteAsync(SnapshotKey, CancellationToken.None);
        repo.Verify(r => r.DeleteAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures the provider exposes the expected format.
    /// </summary>
    [Fact]
    public void FormatShouldReturnAzureBlob()
    {
        BlobSnapshotStorageProvider provider = CreateProvider();
        Assert.Equal("azure-blob", provider.Format);
    }

    /// <summary>
    ///     Ensures prune validates arguments and delegates to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldDelegate()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        List<int> retain = [2];
        await provider.PruneAsync(StreamKey, retain, CancellationToken.None);
        repo.Verify(r => r.PruneAsync(StreamKey, retain, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures prune rejects null retain sets.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldThrowWhenRetainModuliNull()
    {
        BlobSnapshotStorageProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.PruneAsync(
            StreamKey,
            null!,
            CancellationToken.None));
    }

    /// <summary>
    ///     Ensures read forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<IBlobSnapshotRepository> repo = new();
        repo.Setup(r => r.ReadAsync(SnapshotKey, CancellationToken.None)).ReturnsAsync(envelope);
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Same(envelope, result);
        repo.Verify(r => r.ReadAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures read returns null when repository returns null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenNotFound()
    {
        Mock<IBlobSnapshotRepository> repo = new();
        repo.Setup(r => r.ReadAsync(SnapshotKey, It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotEnvelope?)null);
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Ensures write forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<IBlobSnapshotRepository> repo = new();
        BlobSnapshotStorageProvider provider = CreateProvider(repo);
        await provider.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        repo.Verify(r => r.WriteAsync(SnapshotKey, envelope, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures write rejects null snapshot.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldThrowWhenSnapshotNull()
    {
        BlobSnapshotStorageProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.WriteAsync(
            SnapshotKey,
            null!,
            CancellationToken.None));
    }
}