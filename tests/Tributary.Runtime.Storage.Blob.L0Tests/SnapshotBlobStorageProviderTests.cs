using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageProvider" />.
/// </summary>
public sealed class SnapshotBlobStorageProviderTests
{
    /// <summary>
    ///     Verifies the provider returns null for missing snapshots.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenSnapshotDoesNotExist()
    {
        SnapshotBlobStorageProvider provider = CreateProvider(new StubSnapshotBlobOperations());
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);

        SnapshotEnvelope? snapshot = await provider.ReadAsync(snapshotKey, CancellationToken.None);

        Assert.Null(snapshot);
    }

    /// <summary>
    ///     Verifies the provider round-trips snapshots through the repository integration.
    /// </summary>
    /// <param name="compression">The configured compression mode.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData((int)SnapshotBlobCompression.Off)]
    [InlineData((int)SnapshotBlobCompression.Gzip)]
    public async Task WriteAsyncAndReadAsyncShouldRoundTripSnapshots(
        int compression
    )
    {
        SnapshotBlobStorageProvider provider = CreateProvider(
            new StubSnapshotBlobOperations(),
            (SnapshotBlobCompression)compression);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        SnapshotEnvelope snapshot = new()
        {
            Data = ImmutableArray.Create((byte)1, (byte)2, (byte)3),
            DataContentType = "application/json",
            DataSizeBytes = 3,
            ReducerHash = snapshotKey.Stream.ReducersHash,
        };

        await provider.WriteAsync(snapshotKey, snapshot, CancellationToken.None);
        SnapshotEnvelope? roundTripped = await provider.ReadAsync(snapshotKey, CancellationToken.None);

        Assert.NotNull(roundTripped);
        Assert.Equal([.. snapshot.Data], [.. roundTripped!.Data]);
        Assert.Equal(snapshot.DataContentType, roundTripped.DataContentType);
        Assert.Equal(snapshot.ReducerHash, roundTripped.ReducerHash);
    }

    /// <summary>
    ///     Verifies delete of a missing snapshot remains idempotent and non-throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsyncShouldBeIdempotentWhenSnapshotDoesNotExist()
    {
        StubSnapshotBlobOperations operations = new();
        SnapshotBlobStorageProvider provider = CreateProvider(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);

        await provider.DeleteAsync(snapshotKey, CancellationToken.None);

        Assert.Single(operations.DeletedBlobNames);
    }

    private static SnapshotBlobStorageProvider CreateProvider(
        StubSnapshotBlobOperations operations,
        SnapshotBlobCompression compression = SnapshotBlobCompression.Off
    ) =>
        new(
            SnapshotBlobRepositoryTests.CreateRepository(operations, compression: compression),
            NullLogger<SnapshotBlobStorageProvider>.Instance);
}