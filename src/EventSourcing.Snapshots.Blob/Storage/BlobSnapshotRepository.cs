using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Compression;
using Mississippi.EventSourcing.Snapshots.Blob.Diagnostics;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Domain-level blob snapshot repository implementation.
/// </summary>
internal sealed class BlobSnapshotRepository : IBlobSnapshotRepository
{
    private const string CompressionMetadataKey = "SnapshotCompression";

    private const string ContentTypeMetadataKey = "SnapshotContentType";

    private const string ReducerHashMetadataKey = "SnapshotReducerHash";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobSnapshotRepository" /> class.
    /// </summary>
    /// <param name="operations">The blob operations abstraction.</param>
    /// <param name="writeCompressor">The compressor for writes.</param>
    /// <param name="options">The storage options.</param>
    /// <param name="logger">The logger.</param>
    public BlobSnapshotRepository(
        IBlobSnapshotOperations operations,
        ISnapshotCompressor writeCompressor,
        IOptions<BlobSnapshotStorageOptions> options,
        ILogger<BlobSnapshotRepository> logger
    )
    {
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        WriteCompressor = writeCompressor ?? throw new ArgumentNullException(nameof(writeCompressor));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<BlobSnapshotRepository> Logger { get; }

    private IBlobSnapshotOperations Operations { get; }

    private BlobSnapshotStorageOptions Options { get; }

    private ISnapshotCompressor WriteCompressor { get; }

    private static ISnapshotCompressor GetCompressorForEncoding(
        string encoding
    ) =>
        encoding switch
        {
            "gzip" => new GZipSnapshotCompressor(),
            "br" => new BrotliSnapshotCompressor(),
            var _ => new NoCompressionCompressor(),
        };

    private static string GetCompressionMetricTag(
        string encoding
    ) =>
        encoding switch
        {
            "gzip" => "gzip",
            "br" => "brotli",
            var _ => "none",
        };

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        string prefix = BlobPathBuilder.BuildPrefix(streamKey);
        List<string> blobsToDelete = [];
        await foreach (string blobPath in Operations.ListBlobsAsync(prefix, cancellationToken).ConfigureAwait(false))
        {
            blobsToDelete.Add(blobPath);
        }

        if (blobsToDelete.Count > 0)
        {
            await Operations.DeleteBatchAsync(blobsToDelete, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        await Operations.DeleteAsync(blobPath, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        string prefix = BlobPathBuilder.BuildPrefix(streamKey);
        List<(string Path, long Version)> allSnapshots = [];
        await foreach (string blobPath in Operations.ListBlobsAsync(prefix, cancellationToken).ConfigureAwait(false))
        {
            long? version = BlobPathBuilder.ExtractVersion(blobPath);
            if (version.HasValue)
            {
                allSnapshots.Add((blobPath, version.Value));
            }
        }

        if (allSnapshots.Count == 0)
        {
            return;
        }

        // Find the highest version (always retained)
        long maxVersion = allSnapshots.Max(s => s.Version);

        // Determine which snapshots to delete
        List<string> toDelete = [];
        foreach ((string path, long version) in allSnapshots)
        {
            // Always keep the highest version
            if (version == maxVersion)
            {
                continue;
            }

            // Keep if divisible by any modulus
            bool shouldRetain = retainModuli.Any(m => (m > 0) && ((version % m) == 0));
            if (!shouldRetain)
            {
                toDelete.Add(path);
            }
        }

        if (toDelete.Count > 0)
        {
            await Operations.DeleteBatchAsync(toDelete, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        BlobDownloadResult? result = await Operations.DownloadAsync(blobPath, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        byte[] data = result.Content.ToArray();
        IDictionary<string, string> metadata = result.Details.Metadata;

        // Detect and apply decompression
        string compressionEncoding = metadata.TryGetValue(CompressionMetadataKey, out string? encoding)
            ? encoding
            : string.Empty;
        ISnapshotCompressor compressor = GetCompressorForEncoding(compressionEncoding);
        byte[] decompressedData = await compressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(compressionEncoding))
        {
            Logger.DecompressedSnapshot(data.Length, decompressedData.Length, compressionEncoding);
        }

        string contentType = metadata.TryGetValue(ContentTypeMetadataKey, out string? ct) ? ct : string.Empty;
        string reducerHash = metadata.TryGetValue(ReducerHashMetadataKey, out string? rh) ? rh : string.Empty;
        return new()
        {
            Data = decompressedData.ToImmutableArray(),
            DataContentType = contentType,
            DataSizeBytes = decompressedData.Length,
            ReducerHash = reducerHash,
        };
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        byte[] originalData = envelope.Data.ToArray();

        // Apply compression using the injected compressor
        byte[] compressedData =
            await WriteCompressor.CompressAsync(originalData, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(WriteCompressor.ContentEncoding))
        {
            Logger.CompressedSnapshot(originalData.Length, compressedData.Length, WriteCompressor.ContentEncoding);
        }

        // Build metadata
        Dictionary<string, string> metadata = new()
        {
            [CompressionMetadataKey] = WriteCompressor.ContentEncoding ?? string.Empty,
            [ContentTypeMetadataKey] = envelope.DataContentType ?? string.Empty,
            [ReducerHashMetadataKey] = envelope.ReducerHash ?? string.Empty,
        };
        await Operations.UploadAsync(blobPath, compressedData, metadata, Options.DefaultAccessTier, cancellationToken)
            .ConfigureAwait(false);
        BlobSnapshotStorageMetrics.RecordCompressionRatio(
            snapshotKey.Stream.SnapshotStorageName,
            GetCompressionMetricTag(WriteCompressor.ContentEncoding ?? string.Empty),
            originalData.Length,
            compressedData.Length);
    }
}