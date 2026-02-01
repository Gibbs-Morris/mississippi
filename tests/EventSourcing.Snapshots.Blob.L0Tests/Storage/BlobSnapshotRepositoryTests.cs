using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Compression;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests.Storage;

/// <summary>
///     Tests for <see cref="BlobSnapshotRepository" />.
/// </summary>
public sealed class BlobSnapshotRepositoryTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 5);

    /// <summary>
    ///     Creates a repository with default mocks.
    /// </summary>
    private static BlobSnapshotRepository CreateRepository(
        Mock<IBlobSnapshotOperations>? operations = null,
        ISnapshotCompressor? compressor = null,
        BlobSnapshotStorageOptions? options = null
    )
    {
        operations ??= new();
        compressor ??= new NoCompressionCompressor();
        options ??= new();
        return new(operations.Object, compressor, Options.Create(options), NullLogger<BlobSnapshotRepository>.Instance);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<string> ToAsyncEnumerableAsync(
        IEnumerable<string> items
    )
    {
        foreach (string item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998

    /// <summary>
    ///     Ensures constructor throws when compressor is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenCompressorIsNull()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotRepository(
            ops.Object,
            null!,
            Options.Create(new BlobSnapshotStorageOptions()),
            NullLogger<BlobSnapshotRepository>.Instance));
    }

    /// <summary>
    ///     Ensures constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotRepository(
            ops.Object,
            new NoCompressionCompressor(),
            Options.Create(new BlobSnapshotStorageOptions()),
            null!));
    }

    /// <summary>
    ///     Ensures constructor throws when operations is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenOperationsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotRepository(
            null!,
            new NoCompressionCompressor(),
            Options.Create(new BlobSnapshotStorageOptions()),
            NullLogger<BlobSnapshotRepository>.Instance));
    }

    /// <summary>
    ///     Ensures constructor throws when options is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenOptionsIsNull()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new BlobSnapshotRepository(
            ops.Object,
            new NoCompressionCompressor(),
            null!,
            NullLogger<BlobSnapshotRepository>.Instance));
    }

    /// <summary>
    ///     Ensures DeleteAllAsync lists and deletes all snapshots for stream.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldListAndDeleteAll()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        string[] blobs = ["1.snapshot", "2.snapshot"];
        ops.Setup(o => o.ListBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(blobs));
        BlobSnapshotRepository repository = CreateRepository(ops);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        ops.Verify(o => o.ListBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        ops.Verify(o => o.DeleteBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Ensures DeleteAsync calls operations.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAsyncShouldDelegate()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        BlobSnapshotRepository repository = CreateRepository(ops);
        await repository.DeleteAsync(SnapshotKey, CancellationToken.None);
        ops.Verify(
            o => o.DeleteAsync(It.Is<string>(s => s.EndsWith("5.snapshot")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures PruneAsync retains max version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldRetainMaxVersion()
    {
        Mock<IBlobSnapshotOperations> ops = new();

        // Snapshots: versions 1, 2, 3, 4, 5
        string[] blobs = ["1.snapshot", "2.snapshot", "3.snapshot", "4.snapshot", "5.snapshot"];
        ops.Setup(o => o.ListBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(blobs));
        List<string> deletedPaths = [];
        ops.Setup(o => o.DeleteBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((
                paths,
                _
            ) => deletedPaths.AddRange(paths))
            .Returns(Task.CompletedTask);
        BlobSnapshotRepository repository = CreateRepository(ops);

        // Retain only every 3rd version (3)
        await repository.PruneAsync(StreamKey, [3], CancellationToken.None);

        // Version 5 is max (always retained), version 3 matches modulus
        // Versions 1, 2, 4 should be deleted
        Assert.Contains(deletedPaths, p => p.EndsWith("1.snapshot", StringComparison.Ordinal));
        Assert.Contains(deletedPaths, p => p.EndsWith("2.snapshot", StringComparison.Ordinal));
        Assert.Contains(deletedPaths, p => p.EndsWith("4.snapshot", StringComparison.Ordinal));
        Assert.DoesNotContain(deletedPaths, p => p.EndsWith("3.snapshot", StringComparison.Ordinal));
        Assert.DoesNotContain(deletedPaths, p => p.EndsWith("5.snapshot", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Ensures PruneAsync handles empty moduli list.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncWithEmptyModuliShouldRetainOnlyMax()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        string[] blobs = ["1.snapshot", "2.snapshot", "3.snapshot"];
        ops.Setup(o => o.ListBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(blobs));
        List<string> deletedPaths = [];
        ops.Setup(o => o.DeleteBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((
                paths,
                _
            ) => deletedPaths.AddRange(paths))
            .Returns(Task.CompletedTask);
        BlobSnapshotRepository repository = CreateRepository(ops);

        // Empty moduli - only max version retained
        await repository.PruneAsync(StreamKey, [], CancellationToken.None);
        Assert.Contains(deletedPaths, p => p.EndsWith("1.snapshot", StringComparison.Ordinal));
        Assert.Contains(deletedPaths, p => p.EndsWith("2.snapshot", StringComparison.Ordinal));
        Assert.DoesNotContain(deletedPaths, p => p.EndsWith("3.snapshot", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Ensures PruneAsync handles no snapshots gracefully.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncWithNoSnapshotsShouldNotThrow()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        ops.Setup(o => o.ListBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync([]));
        BlobSnapshotRepository repository = CreateRepository(ops);
        await repository.PruneAsync(StreamKey, [5], CancellationToken.None);
        ops.Verify(
            o => o.DeleteBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Ensures ReadAsync returns null when blob not found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenNotFound()
    {
        Mock<IBlobSnapshotOperations> ops = new();
        ops.Setup(o => o.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobDownloadResult?)null);
        BlobSnapshotRepository repository = CreateRepository(ops);
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(result);
    }
}