using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
            compression: (SnapshotBlobCompression)compression);
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

    /// <summary>
    ///     Verifies duplicate-version write conflicts surface actionable diagnostics through the provider.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldSurfaceActionableDuplicateConflictDiagnostics()
    {
        StubSnapshotBlobOperations operations = new()
        {
            CreateIfAbsentResult = false,
        };
        TestLogger<SnapshotBlobStorageProvider> logger = new();
        SnapshotBlobStorageProvider provider = CreateProvider(operations, logger);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        SnapshotEnvelope snapshot = new()
        {
            Data = ImmutableArray.Create((byte)1, (byte)2, (byte)3),
            DataContentType = "application/json",
            DataSizeBytes = 3,
            ReducerHash = snapshotKey.Stream.ReducersHash,
        };

        SnapshotBlobDuplicateVersionException exception = await Assert.ThrowsAsync<SnapshotBlobDuplicateVersionException>(
            () => provider.WriteAsync(snapshotKey, snapshot, CancellationToken.None));

        TestLogEntry conflictLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2411);

        Assert.Equal(snapshotKey, exception.SnapshotKey);
        Assert.Contains("does not overwrite", exception.Message, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Warning, conflictLog.Level);
        Assert.Equal(snapshotKey, Assert.IsType<SnapshotKey>(conflictLog.State["snapshotKey"]));
        Assert.Same(exception, conflictLog.Exception);
    }

    /// <summary>
    ///     Verifies unreadable stored blobs fail closed through the provider boundary.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldSurfaceUnreadableBlobDiagnosticsWhenStoredFrameIsCorrupt()
    {
        StubSnapshotBlobOperations operations = new();
        TestLogger<SnapshotBlobStorageProvider> logger = new();
        SnapshotBlobStorageProvider provider = CreateProvider(operations, logger);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        SnapshotEnvelope snapshot = new()
        {
            Data = ImmutableArray.Create((byte)1, (byte)2, (byte)3),
            DataContentType = "application/json",
            DataSizeBytes = 3,
            ReducerHash = snapshotKey.Stream.ReducersHash,
        };

        await provider.WriteAsync(snapshotKey, snapshot, CancellationToken.None);
        string blobName = operations.Blobs.Keys.Single();
        operations.Blobs[blobName][^1] ^= 0x5A;

        SnapshotBlobUnreadableFrameException exception = await Assert.ThrowsAsync<SnapshotBlobUnreadableFrameException>(
            () => provider.ReadAsync(snapshotKey, CancellationToken.None));

        TestLogEntry unreadableLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2412);

        Assert.Equal(SnapshotBlobUnreadableFrameReason.PayloadChecksumMismatch, exception.Reason);
        Assert.Equal(snapshotKey, exception.SnapshotKey);
        Assert.Contains("unreadable", exception.Message, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Error, unreadableLog.Level);
        Assert.Equal(snapshotKey, Assert.IsType<SnapshotKey>(unreadableLog.State["snapshotKey"]));
        Assert.Equal(
            SnapshotBlobUnreadableFrameReason.PayloadChecksumMismatch,
            Assert.IsType<SnapshotBlobUnreadableFrameReason>(unreadableLog.State["reason"]));
        Assert.Same(exception, unreadableLog.Exception);
    }

    private static SnapshotBlobStorageProvider CreateProvider(
        StubSnapshotBlobOperations operations,
        ILogger<SnapshotBlobStorageProvider>? logger = null,
        SnapshotBlobCompression compression = SnapshotBlobCompression.Off
    ) =>
        new(
            SnapshotBlobRepositoryTests.CreateRepository(operations, compression: compression),
            logger ?? new TestLogger<SnapshotBlobStorageProvider>());
}