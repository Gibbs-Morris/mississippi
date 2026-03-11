using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Mapping;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobRepository" />.
/// </summary>
public sealed class SnapshotBlobRepositoryTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 3);

    private static SnapshotBlobRepository CreateRepository(
        Mock<ISnapshotBlobContainerOperations> operations,
        SnapshotBlobStorageOptions? options = null,
        IMapper<SnapshotBlobStorageModel, SnapshotEnvelope>? storageToEnvelopeMapper = null,
        IMapper<SnapshotWriteModel, SnapshotBlobStorageModel>? writeModelToStorageMapper = null
    )
    {
        storageToEnvelopeMapper ??= new SnapshotStorageToEnvelopeMapper();
        writeModelToStorageMapper ??= new SnapshotWriteModelToStorageMapper();
        return new(
            operations.Object,
            storageToEnvelopeMapper,
            writeModelToStorageMapper,
            Options.Create(options ?? new()),
            NullLogger<SnapshotBlobRepository>.Instance);
    }

    private static async IAsyncEnumerable<SnapshotBlobListItem> ToAsyncEnumerableAsync(
        IEnumerable<SnapshotBlobListItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        foreach (SnapshotBlobListItem item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Ensures DeleteAllAsync deletes all blobs returned by the query.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDeleteAllSnapshotsFromQuery()
    {
        List<string> deleted = [];
        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.ListBlobsAsync("TEST.BROOK/type/id/hash/", It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(
            [
                new("TEST.BROOK/type/id/hash/1.snapshot"),
                new("TEST.BROOK/type/id/hash/2.snapshot"),
            ]));
        operations.Setup(o => o.DeleteBlobIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((name, _) => deleted.Add(name))
            .ReturnsAsync(true);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        Assert.Equal(
            [
                "TEST.BROOK/type/id/hash/1.snapshot",
                "TEST.BROOK/type/id/hash/2.snapshot",
            ],
            deleted);
    }

    /// <summary>
    ///     Ensures PruneAsync retains modulus matches and the latest snapshot.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldAlwaysRetainMaxVersion()
    {
        List<string> deleted = [];
        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.ListBlobsAsync("TEST.BROOK/type/id/hash/", It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(
            [
                new("TEST.BROOK/type/id/hash/3.snapshot"),
                new("TEST.BROOK/type/id/hash/5.snapshot"),
            ]));
        operations.Setup(o => o.DeleteBlobIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((name, _) => deleted.Add(name))
            .ReturnsAsync(true);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await repository.PruneAsync(StreamKey, [10], CancellationToken.None);
        Assert.Equal(["TEST.BROOK/type/id/hash/3.snapshot"], deleted);
    }

    /// <summary>
    ///     Ensures reads decompress payloads when the stored blob is compressed.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldDecompressCompressedSnapshots()
    {
        byte[] original = new byte[] { 1, 2, 3 };
        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.DownloadBlobAsync("TEST.BROOK/type/id/hash/3.snapshot", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnapshotBlobDownloadResult(
                SnapshotCompression.Compress(original),
                "application/json",
                original.Length,
                true));
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotEnvelope? envelope = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.NotNull(envelope);
        Assert.Equal(original, envelope!.Data.ToArray());
        Assert.Equal("hash", envelope.ReducerHash);
    }

    /// <summary>
    ///     Ensures reads return null when the blob is missing.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenBlobMissing()
    {
        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.DownloadBlobAsync("TEST.BROOK/type/id/hash/3.snapshot", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotBlobDownloadResult?)null);
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotEnvelope? envelope = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(envelope);
    }

    /// <summary>
    ///     Ensures writes honor compression when enabled.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldCompressPayloadWhenEnabled()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)1, (byte)2, (byte)3),
            DataContentType = "application/octet-stream",
            DataSizeBytes = 3,
        };
        SnapshotBlobWriteRequest? capturedRequest = null;
        Mock<ISnapshotBlobContainerOperations> operations = new();
        operations.Setup(o => o.UploadBlobAsync(
                "TEST.BROOK/type/id/hash/3.snapshot",
                It.IsAny<SnapshotBlobWriteRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, SnapshotBlobWriteRequest, CancellationToken>((_, request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        SnapshotBlobRepository repository = CreateRepository(
            operations,
            new()
            {
                CompressionEnabled = true,
            });
        await repository.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.IsCompressed);
        Assert.NotEqual(envelope.Data.ToArray(), capturedRequest.Data);
        Assert.Equal(3, capturedRequest.DataSizeBytes);
    }
}
