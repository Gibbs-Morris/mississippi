using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;
using Mississippi.Tributary.Runtime.Storage.Blob.Startup;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Verifies increment-2 repository primitives built on Blob naming and listing.
/// </summary>
public sealed class SnapshotBlobRepositoryTests
{
    /// <summary>
    ///     Verifies conditional create succeeds for a new Blob name.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteIfAbsentAsyncShouldUseConditionalCreateAndSucceedWhenBlobIsNew()
    {
        StubSnapshotBlobOperations operations = new()
        {
            CreateIfAbsentResult = true,
        };
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        using MemoryStream content = new([1, 2, 3]);

        await repository.WriteIfAbsentAsync(snapshotKey, content, CancellationToken.None);

        Assert.Single(operations.CreatedBlobNames);
        Assert.EndsWith("v00000000000000000012.snapshot", operations.CreatedBlobNames[0], System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies duplicate writes surface the internal duplicate-version conflict.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteIfAbsentAsyncShouldThrowDuplicateVersionConflictWhenBlobAlreadyExists()
    {
        StubSnapshotBlobOperations operations = new()
        {
            CreateIfAbsentResult = false,
        };
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        using MemoryStream content = new([1, 2, 3]);

        SnapshotBlobDuplicateVersionException exception = await Assert.ThrowsAsync<SnapshotBlobDuplicateVersionException>(
            () => repository.WriteIfAbsentAsync(snapshotKey, content, CancellationToken.None));

        Assert.Equal(snapshotKey, exception.SnapshotKey);
    }

    /// <summary>
    ///     Verifies stream-local listing only yields versions for the requested stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListVersionsAsyncShouldReturnOnlyVersionsForTheRequestedStream()
    {
        SnapshotStreamKey targetStream = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy nameStrategy = CreateNameStrategy();
        StubSnapshotBlobOperations operations = new();
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(targetStream, 9)),
            nameStrategy.GetBlobName(new(targetStream, 10)),
            nameStrategy.GetBlobName(new(new SnapshotStreamKey("brook-a", "projection-a", "entity-43", "reducers-v1"), 999)),
            "not-a-snapshot-name",
        ]));

        SnapshotBlobRepository repository = CreateRepository(operations, nameStrategy);

        List<IReadOnlyList<long>> pages = [];
        await foreach (IReadOnlyList<long> page in repository.ListVersionsAsync(targetStream, CancellationToken.None))
        {
            pages.Add(page);
        }

        Assert.Single(pages);
        Assert.Equal([9L, 10L], pages[0]);
        Assert.Single(operations.ListPrefixes);
        Assert.Equal(nameStrategy.GetStreamPrefix(targetStream), operations.ListPrefixes[0]);
        Assert.Single(operations.ListPageSizeHints);
        Assert.Equal(SnapshotBlobDefaults.ListPageSizeHint, operations.ListPageSizeHints[0]);
    }

    /// <summary>
    ///     Verifies latest-version selection uses parsed names across multiple pages.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetLatestVersionAsyncShouldSelectHighestVersionAcrossPages()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy nameStrategy = CreateNameStrategy();
        StubSnapshotBlobOperations operations = new();
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(streamKey, 9)),
            nameStrategy.GetBlobName(new(streamKey, 10)),
        ]));
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(streamKey, 11)),
            nameStrategy.GetBlobName(new(streamKey, 100)),
        ]));

        SnapshotBlobRepository repository = CreateRepository(operations, nameStrategy);

        long? latestVersion = await repository.GetLatestVersionAsync(streamKey, CancellationToken.None);

        Assert.Equal(100, latestVersion);
    }

    /// <summary>
    ///     Verifies missing exact-version reads return null.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenBlobDoesNotExist()
    {
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);

        SnapshotEnvelope? snapshot = await repository.ReadAsync(snapshotKey, CancellationToken.None);

        Assert.Null(snapshot);
        Assert.Single(operations.DownloadedBlobNames);
    }

    /// <summary>
    ///     Verifies latest reads return null when the stream has no snapshots.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadLatestAsyncShouldReturnNullWhenTheStreamHasNoSnapshots()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        SnapshotBlobRepository repository = CreateRepository(new StubSnapshotBlobOperations());

        SnapshotEnvelope? snapshot = await repository.ReadLatestAsync(streamKey, CancellationToken.None);

        Assert.Null(snapshot);
    }

    /// <summary>
    ///     Verifies exact write and read round-trip through the stored Blob frame.
    /// </summary>
    /// <param name="compression">The configured compression mode.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData((int)SnapshotBlobCompression.Off)]
    [InlineData((int)SnapshotBlobCompression.Gzip)]
    public async Task WriteAsyncAndReadAsyncShouldRoundTripThroughTheBlobFrame(
        int compression
    )
    {
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobRepository repository = CreateRepository(
            operations,
            compression: (SnapshotBlobCompression)compression);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope([1, 2, 3, 4], snapshotKey.Stream.ReducersHash, "application/octet-stream");

        await repository.WriteAsync(snapshotKey, snapshot, CancellationToken.None);
        SnapshotEnvelope? roundTripped = await repository.ReadAsync(snapshotKey, CancellationToken.None);

        Assert.NotNull(roundTripped);
        Assert.Equal([.. snapshot.Data], [.. roundTripped!.Data]);
        Assert.Equal(snapshot.DataContentType, roundTripped.DataContentType);
        Assert.Equal(snapshot.ReducerHash, roundTripped.ReducerHash);
        Assert.Equal(snapshot.DataSizeBytes, roundTripped.DataSizeBytes);
    }

    /// <summary>
    ///     Verifies latest-read selects the highest version and downloads only the selected candidate body.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadLatestAsyncShouldDownloadOnlyTheSelectedLatestBlob()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);

        await repository.WriteAsync(new(streamKey, 9), CreateSnapshotEnvelope([9], streamKey.ReducersHash), CancellationToken.None);
        await repository.WriteAsync(new(streamKey, 10), CreateSnapshotEnvelope([10], streamKey.ReducersHash), CancellationToken.None);
        await repository.WriteAsync(new(streamKey, 100), CreateSnapshotEnvelope([100], streamKey.ReducersHash), CancellationToken.None);

        SnapshotEnvelope? latest = await repository.ReadLatestAsync(streamKey, CancellationToken.None);

        Assert.NotNull(latest);
        string downloadedBlobName = Assert.Single(operations.DownloadedBlobNames);
        Assert.EndsWith("v00000000000000000100.snapshot", downloadedBlobName, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies delete-all stays list-driven and does not download Blob bodies.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDeleteOnlyTheTargetStreamWithoutDownloadingBodies()
    {
        SnapshotStreamKey targetStream = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        SnapshotStreamKey otherStream = new("brook-a", "projection-a", "entity-43", "reducers-v1");
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);

        await repository.WriteAsync(new(targetStream, 9), CreateSnapshotEnvelope([9], targetStream.ReducersHash), CancellationToken.None);
        await repository.WriteAsync(new(targetStream, 10), CreateSnapshotEnvelope([10], targetStream.ReducersHash), CancellationToken.None);
        await repository.WriteAsync(new(otherStream, 77), CreateSnapshotEnvelope([77], otherStream.ReducersHash), CancellationToken.None);

        await repository.DeleteAllAsync(targetStream, CancellationToken.None);

        Assert.Empty(operations.DownloadedBlobNames);
        Assert.Collection(
            operations.DeletedBlobNames,
            static _ => { },
            static _ => { });
        Assert.Single(operations.Blobs);
        string remainingBlobName = Assert.Single(operations.Blobs.Keys);
        Assert.Contains("v00000000000000000077.snapshot", remainingBlobName, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies prune retains modulus matches plus the latest version and does not download candidate bodies.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldRetainModulusMatchesAndLatestWithoutDownloadingBodies()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobRepository repository = CreateRepository(operations);

        foreach (long version in new long[] { 1, 2, 3, 4, 5 })
        {
            await repository.WriteAsync(new(streamKey, version), CreateSnapshotEnvelope([(byte)version], streamKey.ReducersHash), CancellationToken.None);
        }

        await repository.PruneAsync(streamKey, [2], CancellationToken.None);

        Assert.Empty(operations.DownloadedBlobNames);
        Assert.Equal(2, operations.DeletedBlobNames.Count);
        Assert.DoesNotContain(operations.Blobs.Keys, blobName => blobName.Contains("v00000000000000000001.snapshot", System.StringComparison.Ordinal));
        Assert.DoesNotContain(operations.Blobs.Keys, blobName => blobName.Contains("v00000000000000000003.snapshot", System.StringComparison.Ordinal));
        Assert.Contains(operations.Blobs.Keys, blobName => blobName.Contains("v00000000000000000002.snapshot", System.StringComparison.Ordinal));
        Assert.Contains(operations.Blobs.Keys, blobName => blobName.Contains("v00000000000000000004.snapshot", System.StringComparison.Ordinal));
        Assert.Contains(operations.Blobs.Keys, blobName => blobName.Contains("v00000000000000000005.snapshot", System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies persisted non-default serializer identities round-trip through the repository path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncAndReadAsyncShouldSupportNonDefaultConfiguredSerializer()
    {
        StubSnapshotBlobOperations operations = new();
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope([1, 2, 3], snapshotKey.Stream.ReducersHash);
        SnapshotBlobRepository writerRepository = CreateRepository(
            operations,
            payloadSerializerFormat: "custom-json",
            useAlternateSerializerType: true);

        await writerRepository.WriteAsync(snapshotKey, snapshot, CancellationToken.None);

        SnapshotBlobRepository readerRepository = CreateRepository(
            operations,
            payloadSerializerFormat: "custom-json",
            useAlternateSerializerType: true);
        SnapshotEnvelope? roundTripped = await readerRepository.ReadAsync(snapshotKey, CancellationToken.None);

        Assert.NotNull(roundTripped);
        Assert.Equal([.. snapshot.Data], [.. roundTripped!.Data]);
        Assert.Equal(snapshot.ReducerHash, roundTripped.ReducerHash);
    }

    /// <summary>
    ///     Creates the canonical Blob name strategy for tests.
    /// </summary>
    /// <returns>The configured Blob name strategy.</returns>
    internal static BlobNameStrategy CreateNameStrategy() =>
        new(Options.Create(new SnapshotBlobStorageOptions()));

    /// <summary>
    ///     Creates a snapshot envelope for repository and provider tests.
    /// </summary>
    /// <param name="data">The payload bytes.</param>
    /// <param name="reducerHash">The reducer hash.</param>
    /// <param name="dataContentType">The payload content type.</param>
    /// <returns>The configured snapshot envelope.</returns>
    private static SnapshotEnvelope CreateSnapshotEnvelope(
        IEnumerable<byte> data,
        string reducerHash,
        string dataContentType = "application/json"
    )
    {
        ImmutableArray<byte> payload = [.. data];
        return new SnapshotEnvelope
        {
            Data = payload,
            DataContentType = dataContentType,
            DataSizeBytes = payload.Length,
            ReducerHash = reducerHash,
        };
    }

    /// <summary>
    ///     Creates the Blob frame codec used by repository tests.
    /// </summary>
    /// <param name="nameStrategy">The Blob name strategy.</param>
    /// <param name="compression">The configured compression mode.</param>
    /// <param name="payloadSerializerFormat">The configured serializer format.</param>
    /// <param name="useAlternateSerializerType">Whether to use the alternate serializer test type.</param>
    /// <returns>The configured Blob frame codec.</returns>
    private static BlobEnvelopeCodec CreateCodec(
        BlobNameStrategy nameStrategy,
        SnapshotBlobCompression compression,
        string payloadSerializerFormat,
        bool useAlternateSerializerType
    )
    {
        SnapshotBlobStorageOptions options = new()
        {
            Compression = compression,
            PayloadSerializerFormat = payloadSerializerFormat,
        };
        ISerializationProvider serializer = useAlternateSerializerType
            ? new AlternateTestSerializationProvider(payloadSerializerFormat)
            : new TestSerializationProvider(payloadSerializerFormat);
        SnapshotPayloadSerializerResolver resolver = new([serializer], Options.Create(options));
        return new BlobEnvelopeCodec(nameStrategy, resolver, [serializer], Options.Create(options), new FakeTimeProvider());
    }

    /// <summary>
    ///     Creates a Blob repository with the supplied test seams.
    /// </summary>
    /// <param name="operations">The low-level Blob operations test double.</param>
    /// <param name="nameStrategy">The optional Blob naming strategy.</param>
    /// <param name="compression">The configured compression mode.</param>
    /// <param name="payloadSerializerFormat">The configured serializer format.</param>
    /// <param name="useAlternateSerializerType">Whether to use the alternate serializer test type.</param>
    /// <returns>The configured Blob repository.</returns>
    internal static SnapshotBlobRepository CreateRepository(
        StubSnapshotBlobOperations operations,
        BlobNameStrategy? nameStrategy = null,
        SnapshotBlobCompression compression = SnapshotBlobCompression.Off,
        string payloadSerializerFormat = SnapshotBlobDefaults.PayloadSerializerFormat,
        bool useAlternateSerializerType = false
    )
    {
        BlobNameStrategy resolvedNameStrategy = nameStrategy ?? CreateNameStrategy();
        SnapshotBlobStorageOptions options = new()
        {
            Compression = compression,
            PayloadSerializerFormat = payloadSerializerFormat,
        };

        return new(
            resolvedNameStrategy,
            operations,
            CreateCodec(resolvedNameStrategy, compression, payloadSerializerFormat, useAlternateSerializerType),
            Options.Create(options));
    }
}