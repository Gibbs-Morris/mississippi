using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Blob-backed implementation of <see cref="ISnapshotBlobRepository" />.
/// </summary>
internal sealed class SnapshotBlobRepository : ISnapshotBlobRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobRepository" /> class.
    /// </summary>
    /// <param name="operations">The Blob operations abstraction.</param>
    /// <param name="options">The Blob snapshot storage options.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobRepository(
        ISnapshotBlobOperations operations,
        IOptions<SnapshotBlobStorageOptions> options,
        ILogger<SnapshotBlobRepository> logger
    )
    {
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<SnapshotBlobRepository> Logger { get; }

    private ISnapshotBlobOperations Operations { get; }

    private SnapshotBlobStorageOptions Options { get; }

    private static byte[] DecodeStoredBytes(
        string blobName,
        string base64Payload
    )
    {
        try
        {
            return Convert.FromBase64String(base64Payload);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException(
                $"Blob snapshot '{blobName}' contains invalid Base64 payload data.",
                exception);
        }
    }

    private static void ValidateDeclaredPayloadSize(
        string blobName,
        long dataSizeBytes,
        long maximumSnapshotPayloadSizeBytes
    )
    {
        if ((dataSizeBytes < 0) || (dataSizeBytes > maximumSnapshotPayloadSizeBytes))
        {
            throw new InvalidDataException(
                $"Blob snapshot '{blobName}' declares an invalid DataSizeBytes value '{dataSizeBytes}'.");
        }
    }

    private static void ValidateDocument(
        SnapshotKey snapshotKey,
        string blobName,
        SnapshotBlobDocument document
    )
    {
        if (document.SchemaVersion != SnapshotBlobDocument.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Blob snapshot '{blobName}' uses unsupported schema version '{document.SchemaVersion}'.");
        }

        if (!string.Equals(document.BrookName, snapshotKey.Stream.BrookName, StringComparison.Ordinal) ||
            !string.Equals(
                document.SnapshotStorageName,
                snapshotKey.Stream.SnapshotStorageName,
                StringComparison.Ordinal) ||
            !string.Equals(document.EntityId, snapshotKey.Stream.EntityId, StringComparison.Ordinal) ||
            !string.Equals(document.ReducersHash, snapshotKey.Stream.ReducersHash, StringComparison.Ordinal) ||
            (document.Version != snapshotKey.Version))
        {
            throw new InvalidDataException($"Blob snapshot '{blobName}' does not match the requested snapshot key.");
        }
    }

    private static void ValidateReducerHash(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot
    )
    {
        if (!string.Equals(snapshot.ReducerHash, snapshotKey.Stream.ReducersHash, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Snapshot envelope ReducerHash '{snapshot.ReducerHash}' does not match snapshot key reducers hash '{snapshotKey.Stream.ReducersHash}'.",
                nameof(snapshot));
        }
    }

    private static void ValidateSnapshotEnvelope(
        SnapshotEnvelope snapshot
    )
    {
        long actualLength = snapshot.Data.Length;
        if (snapshot.DataSizeBytes != actualLength)
        {
            throw new ArgumentException(
                $"Snapshot envelope DataSizeBytes '{snapshot.DataSizeBytes}' does not match payload length '{actualLength}'.",
                nameof(snapshot));
        }
    }

    private static void ValidateSnapshotPayloadLimit(
        SnapshotEnvelope snapshot,
        long maximumSnapshotPayloadSizeBytes
    )
    {
        if (snapshot.DataSizeBytes > maximumSnapshotPayloadSizeBytes)
        {
            throw new ArgumentException(
                $"Snapshot envelope DataSizeBytes '{snapshot.DataSizeBytes}' exceeds configured maximum '{maximumSnapshotPayloadSizeBytes}'.",
                nameof(snapshot));
        }
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        string prefix = SnapshotBlobPath.BuildStreamPrefix(streamKey);
        Logger.DeletingAllSnapshots(prefix);
        await foreach (string blobName in Operations.ListBlobNamesAsync(prefix, cancellationToken))
        {
            if (!SnapshotBlobPath.TryParseVersionFromBlobName(blobName, streamKey, out long _))
            {
                continue;
            }

            await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false);
            Logger.SnapshotDeleted(blobName);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false);
        Logger.SnapshotDeleted(blobName);
    }

    /// <inheritdoc />
    public async Task<int> PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        string prefix = SnapshotBlobPath.BuildStreamPrefix(streamKey);
        Logger.PruningSnapshots(prefix, retainModuli.Count);
        List<(string BlobName, long Version)> versions = [];
        await foreach (string blobName in Operations.ListBlobNamesAsync(prefix, cancellationToken))
        {
            if (SnapshotBlobPath.TryParseVersionFromBlobName(blobName, streamKey, out long version))
            {
                versions.Add((blobName, version));
            }
        }

        if (versions.Count == 0)
        {
            return 0;
        }

        long maxVersion = versions.Max(item => item.Version);
        HashSet<long> retainedVersions = new(
            versions.Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                .Select(item => item.Version));
        retainedVersions.Add(maxVersion);
        int deletedCount = 0;
        foreach ((string blobName, long version) in versions)
        {
            if (retainedVersions.Contains(version))
            {
                continue;
            }

            if (await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false))
            {
                deletedCount++;
                Logger.SnapshotDeleted(blobName);
            }
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        BinaryData? documentPayload = await Operations.DownloadAsync(blobName, cancellationToken).ConfigureAwait(false);
        if (documentPayload is null)
        {
            Logger.SnapshotNotFound(blobName);
            return null;
        }

        try
        {
            SnapshotBlobDocument document = SnapshotBlobDocumentSerializer.Deserialize(documentPayload);
            ValidateDocument(snapshotKey, blobName, document);
            byte[] storedBytes = DecodeStoredBytes(blobName, document.Data);
            if (storedBytes.LongLength != document.StoredSizeBytes)
            {
                throw new InvalidDataException($"Stored payload size mismatch for Blob '{blobName}'.");
            }

            ValidateDeclaredPayloadSize(blobName, document.DataSizeBytes, Options.MaximumSnapshotPayloadSizeBytes);
            byte[] payload = SnapshotBlobCompression.Decompress(
                document.Compression,
                storedBytes,
                Options.MaximumSnapshotPayloadSizeBytes);
            if (payload.LongLength != document.DataSizeBytes)
            {
                throw new InvalidDataException($"Uncompressed payload size mismatch for Blob '{blobName}'.");
            }

            Logger.SnapshotRead(blobName);
            return new()
            {
                Data = ImmutableArray.CreateRange(payload),
                DataContentType = document.DataContentType,
                DataSizeBytes = document.DataSizeBytes,
                ReducerHash = document.ReducersHash,
            };
        }
        catch (InvalidDataException exception)
        {
            Logger.InvalidSnapshotDocument(blobName, exception.Message, exception);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshotEnvelope(snapshot);
        ValidateReducerHash(snapshotKey, snapshot);
        ValidateSnapshotPayloadLimit(snapshot, Options.MaximumSnapshotPayloadSizeBytes);
        byte[] payload = snapshot.Data.ToArray();
        SnapshotBlobCompressionResult compression = SnapshotBlobCompression.Compress(
            payload,
            Options.EnableCompression);
        SnapshotBlobDocument document = new()
        {
            SchemaVersion = SnapshotBlobDocument.CurrentSchemaVersion,
            BrookName = snapshotKey.Stream.BrookName,
            SnapshotStorageName = snapshotKey.Stream.SnapshotStorageName,
            EntityId = snapshotKey.Stream.EntityId,
            ReducersHash = snapshot.ReducerHash,
            Version = snapshotKey.Version,
            DataContentType = snapshot.DataContentType,
            DataSizeBytes = snapshot.DataSizeBytes,
            Compression = compression.Compression,
            StoredSizeBytes = compression.StoredSizeBytes,
            Data = Convert.ToBase64String(compression.StoredBytes),
        };
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        await Operations.UploadAsync(blobName, SnapshotBlobDocumentSerializer.Serialize(document), cancellationToken)
            .ConfigureAwait(false);
        Logger.SnapshotUploaded(blobName);
    }
}