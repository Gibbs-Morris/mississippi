using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobRepository" />.
/// </summary>
public sealed class SnapshotBlobRepositoryTests
{
    private static readonly SnapshotStreamKey StreamKey = new(
        "TEST.BROOK",
        "BankAccountBalance",
        "acct-123",
        "reducers-hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 7);

    private static SnapshotBlobDocument CreateDocument(
        byte[] payload,
        int schemaVersion = SnapshotBlobDocument.CurrentSchemaVersion,
        string? entityId = null,
        string? compressionValue = null,
        long? dataSizeBytes = null,
        long? storedSizeBytes = null,
        bool enableCompression = false
    )
    {
        SnapshotBlobCompressionResult compression = SnapshotBlobCompression.Compress(payload, enableCompression);
        return new()
        {
            SchemaVersion = schemaVersion,
            BrookName = SnapshotKey.Stream.BrookName,
            SnapshotStorageName = SnapshotKey.Stream.SnapshotStorageName,
            EntityId = entityId ?? SnapshotKey.Stream.EntityId,
            ReducersHash = SnapshotKey.Stream.ReducersHash,
            Version = SnapshotKey.Version,
            DataContentType = "application/x-test",
            DataSizeBytes = dataSizeBytes ?? payload.LongLength,
            Compression = compressionValue ?? compression.Compression,
            StoredSizeBytes = storedSizeBytes ?? compression.StoredSizeBytes,
            Data = Convert.ToBase64String(compression.StoredBytes),
        };
    }

    private static SnapshotBlobRepository CreateRepository(
        Mock<ISnapshotBlobOperations> operations,
        Action<SnapshotBlobStorageOptions>? configure = null
    )
    {
        SnapshotBlobStorageOptions options = new();
        configure?.Invoke(options);
        return new(operations.Object, Options.Create(options), NullLogger<SnapshotBlobRepository>.Instance);
    }

    private static void SetupDownload(
        Mock<ISnapshotBlobOperations> operations,
        SnapshotBlobDocument document
    ) =>
        operations.Setup(o => o.DownloadAsync(
                SnapshotBlobPath.BuildSnapshotBlobName(SnapshotKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SnapshotBlobDocumentSerializer.Serialize(document));

    private static async IAsyncEnumerable<string> ToAsyncEnumerableAsync(
        IEnumerable<string> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        foreach (string item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Verifies delete-all only removes well-formed snapshot version blobs for the requested stream prefix.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDeleteOnlyMatchingSnapshotBlobsForStream()
    {
        string prefix = SnapshotBlobPath.BuildStreamPrefix(StreamKey);
        List<string> deleted = [];
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.ListBlobNamesAsync(prefix, It.IsAny<CancellationToken>()))
            .Returns(
                ToAsyncEnumerableAsync(
                [
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 1)),
                    $"{prefix}notes.txt",
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 2)),
                ]));
        operations.Setup(o => o.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((
                blobName,
                _
            ) => deleted.Add(blobName))
            .ReturnsAsync(true);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        Assert.Equal(
            [
                SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 1)),
                SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 2)),
            ],
            deleted);
    }

    /// <summary>
    ///     Verifies deleting a specific snapshot uses the expected hashed Blob path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsyncShouldCallDeleteOnExpectedBlobName()
    {
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await repository.DeleteAsync(SnapshotKey, CancellationToken.None);
        operations.Verify(
            o => o.DeleteIfExistsAsync(
                SnapshotBlobPath.BuildSnapshotBlobName(SnapshotKey),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies prune always retains the highest version and versions matching retention moduli.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldRetainMaxVersionAndMatchingModuli()
    {
        string prefix = SnapshotBlobPath.BuildStreamPrefix(StreamKey);
        List<string> deleted = [];
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.ListBlobNamesAsync(prefix, It.IsAny<CancellationToken>()))
            .Returns(
                ToAsyncEnumerableAsync(
                [
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 1)),
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 2)),
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 3)),
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 4)),
                    SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 5)),
                    $"{prefix}not-a-version.json",
                ]));
        operations.Setup(o => o.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((
                blobName,
                _
            ) => deleted.Add(blobName))
            .ReturnsAsync(true);
        SnapshotBlobRepository repository = CreateRepository(operations);
        int deletedCount = await repository.PruneAsync(StreamKey, [2], CancellationToken.None);
        Assert.Equal(2, deletedCount);
        Assert.Equal(
            [
                SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 1)),
                SnapshotBlobPath.BuildSnapshotBlobName(new(StreamKey, 3)),
            ],
            deleted);
    }

    /// <summary>
    ///     Verifies reads preserve all envelope metadata, including reducer hash.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnEnvelopeWithAllMetadata()
    {
        byte[] payload = Encoding.UTF8.GetBytes("compressed snapshot payload");
        SnapshotBlobCompressionResult compression = SnapshotBlobCompression.Compress(payload, true);
        SnapshotBlobDocument document = new()
        {
            SchemaVersion = SnapshotBlobDocument.CurrentSchemaVersion,
            BrookName = SnapshotKey.Stream.BrookName,
            SnapshotStorageName = SnapshotKey.Stream.SnapshotStorageName,
            EntityId = SnapshotKey.Stream.EntityId,
            ReducersHash = SnapshotKey.Stream.ReducersHash,
            Version = SnapshotKey.Version,
            DataContentType = "application/x-test",
            DataSizeBytes = payload.LongLength,
            Compression = compression.Compression,
            StoredSizeBytes = compression.StoredSizeBytes,
            Data = Convert.ToBase64String(compression.StoredBytes),
        };
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.DownloadAsync(
                SnapshotBlobPath.BuildSnapshotBlobName(SnapshotKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SnapshotBlobDocumentSerializer.Serialize(document));
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(payload, result!.Data.AsSpan().ToArray());
        Assert.Equal("application/x-test", result.DataContentType);
        Assert.Equal(payload.LongLength, result.DataSizeBytes);
        Assert.Equal(SnapshotKey.Stream.ReducersHash, result.ReducerHash);
    }

    /// <summary>
    ///     Verifies a missing Blob document returns <c>null</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenBlobIsMissing()
    {
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.DownloadAsync(
                SnapshotBlobPath.BuildSnapshotBlobName(SnapshotKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinaryData?)null);
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Verifies reads reject documents with mismatched uncompressed payload sizes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenDataSizeDoesNotMatchDocument()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, dataSizeBytes: payload.LongLength + 1);
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies reads reject documents whose declared uncompressed size exceeds the configured limit.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenDeclaredPayloadSizeExceedsConfiguredMaximum()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, dataSizeBytes: 2);
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(
            operations,
            options => options.MaximumSnapshotPayloadSizeBytes = 1);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies reads reject documents with unsupported compression values.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenDocumentCompressionIsUnsupported()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, compressionValue: "lz4");
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies reads reject documents whose key fields do not match the requested snapshot key.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenDocumentDoesNotMatchRequestedKey()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, entityId: "other-account");
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies reads reject documents with unsupported schema versions.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenDocumentSchemaVersionIsUnsupported()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, SnapshotBlobDocument.CurrentSchemaVersion + 1);
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies reads reject documents whose stored payload size metadata does not match the encoded bytes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowWhenStoredSizeDoesNotMatchDocument()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotBlobDocument document = CreateDocument(payload, storedSizeBytes: payload.LongLength + 1);
        Mock<ISnapshotBlobOperations> operations = new();
        SetupDownload(operations, document);
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadAsync(SnapshotKey, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies writes reject envelopes whose declared size does not match the payload length.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldThrowWhenDataSizeBytesDoesNotMatchPayloadLength()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create(payload),
            DataContentType = "application/x-test",
            DataSizeBytes = payload.LongLength + 1,
            ReducerHash = SnapshotKey.Stream.ReducersHash,
        };
        Mock<ISnapshotBlobOperations> operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<ArgumentException>(() => repository.WriteAsync(
            SnapshotKey,
            envelope,
            CancellationToken.None));
        operations.Verify(
            o => o.UploadAsync(It.IsAny<string>(), It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies writes reject envelopes whose payload exceeds the configured maximum size.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldThrowWhenPayloadExceedsConfiguredMaximum()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create(payload),
            DataContentType = "application/x-test",
            DataSizeBytes = payload.LongLength,
            ReducerHash = SnapshotKey.Stream.ReducersHash,
        };
        Mock<ISnapshotBlobOperations> operations = new();
        SnapshotBlobRepository repository = CreateRepository(
            operations,
            options => options.MaximumSnapshotPayloadSizeBytes = payload.LongLength - 1);
        await Assert.ThrowsAsync<ArgumentException>(() => repository.WriteAsync(
            SnapshotKey,
            envelope,
            CancellationToken.None));
        operations.Verify(
            o => o.UploadAsync(It.IsAny<string>(), It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies writes reject envelopes whose reducer hash does not match the snapshot key stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldThrowWhenReducerHashDoesNotMatchSnapshotKey()
    {
        byte[] payload = Encoding.UTF8.GetBytes("payload");
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create(payload),
            DataContentType = "application/x-test",
            DataSizeBytes = payload.LongLength,
            ReducerHash = "different-reducers-hash",
        };
        Mock<ISnapshotBlobOperations> operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);
        await Assert.ThrowsAsync<ArgumentException>(() => repository.WriteAsync(
            SnapshotKey,
            envelope,
            CancellationToken.None));
        operations.Verify(
            o => o.UploadAsync(It.IsAny<string>(), It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies writes upload a JSON document to the expected hashed Blob path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldUploadDocumentToExpectedBlobName()
    {
        byte[] payload = Encoding.UTF8.GetBytes(new string('x', 256));
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create(payload),
            DataContentType = "application/x-test",
            DataSizeBytes = payload.LongLength,
            ReducerHash = SnapshotKey.Stream.ReducersHash,
        };
        string? uploadedBlobName = null;
        BinaryData? uploadedDocument = null;
        Mock<ISnapshotBlobOperations> operations = new();
        operations.Setup(o => o.UploadAsync(It.IsAny<string>(), It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .Callback<string, BinaryData, CancellationToken>((
                blobName,
                document,
                _
            ) =>
            {
                uploadedBlobName = blobName;
                uploadedDocument = document;
            })
            .Returns(Task.CompletedTask);
        SnapshotBlobRepository repository = CreateRepository(operations, options => options.EnableCompression = true);
        await repository.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        Assert.Equal(SnapshotBlobPath.BuildSnapshotBlobName(SnapshotKey), uploadedBlobName);
        Assert.NotNull(uploadedDocument);
        SnapshotBlobDocument document = SnapshotBlobDocumentSerializer.Deserialize(uploadedDocument!);
        Assert.Equal(SnapshotBlobCompression.Gzip, document.Compression);
        Assert.Equal(payload.LongLength, document.DataSizeBytes);
        Assert.Equal("application/x-test", document.DataContentType);
        Assert.Equal(SnapshotKey.Stream.ReducersHash, document.ReducersHash);
        Assert.Equal(SnapshotKey.Stream.SnapshotStorageName, document.SnapshotStorageName);
        Assert.Equal(SnapshotKey.Version, document.Version);
    }
}