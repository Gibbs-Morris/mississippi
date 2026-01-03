using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageProvider" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Storage Provider")]
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
    ///     Ensures delete-all forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDelegate()
    {
        Mock<ISnapshotCosmosRepository> repo = new();
        SnapshotStorageProvider provider = new(repo.Object, NullLogger<SnapshotStorageProvider>.Instance);
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
        Mock<ISnapshotCosmosRepository> repo = new();
        SnapshotStorageProvider provider = new(repo.Object, NullLogger<SnapshotStorageProvider>.Instance);
        await provider.DeleteAsync(SnapshotKey, CancellationToken.None);
        repo.Verify(r => r.DeleteAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures the provider exposes the expected format.
    /// </summary>
    [Fact]
    public void FormatShouldReturnCosmosDb()
    {
        SnapshotStorageProvider provider = new(
            Mock.Of<ISnapshotCosmosRepository>(),
            NullLogger<SnapshotStorageProvider>.Instance);
        Assert.Equal("cosmos-db", provider.Format);
    }

    /// <summary>
    ///     Ensures prune validates arguments and delegates to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldDelegate()
    {
        Mock<ISnapshotCosmosRepository> repo = new();
        SnapshotStorageProvider provider = new(repo.Object, NullLogger<SnapshotStorageProvider>.Instance);
        List<int> retain = new()
        {
            2,
        };
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
        SnapshotStorageProvider provider = new(
            Mock.Of<ISnapshotCosmosRepository>(),
            NullLogger<SnapshotStorageProvider>.Instance);
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
        Mock<ISnapshotCosmosRepository> repo = new();
        repo.Setup(r => r.ReadAsync(SnapshotKey, CancellationToken.None)).ReturnsAsync(envelope);
        SnapshotStorageProvider provider = new(repo.Object, NullLogger<SnapshotStorageProvider>.Instance);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Same(envelope, result);
        repo.Verify(r => r.ReadAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Ensures write forwards to the repository.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotCosmosRepository> repo = new();
        SnapshotStorageProvider provider = new(repo.Object, NullLogger<SnapshotStorageProvider>.Instance);
        await provider.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        repo.Verify(r => r.WriteAsync(SnapshotKey, envelope, CancellationToken.None), Times.Once);
    }
}