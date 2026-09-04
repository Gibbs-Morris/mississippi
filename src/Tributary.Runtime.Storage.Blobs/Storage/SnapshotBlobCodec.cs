using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Owns the Blob snapshot document format, validation, and payload compression.
/// </summary>
internal sealed class SnapshotBlobCodec : ISnapshotBlobCodec
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobCodec" /> class.
    /// </summary>
    /// <param name="options">The document and payload storage options.</param>
    public SnapshotBlobCodec(
        IOptions<SnapshotBlobStorageOptions> options
    ) =>
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));

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
        if (snapshot.Data.IsDefault)
        {
            throw new ArgumentException("Snapshot envelope Data must be initialized.", nameof(snapshot));
        }

        if (snapshot.DataContentType is null)
        {
            throw new ArgumentException("Snapshot envelope DataContentType must not be null.", nameof(snapshot));
        }

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
    public SnapshotEnvelope Decode(
        SnapshotKey snapshotKey,
        BinaryData document
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        if (document.ToMemory().Length > Options.MaximumSnapshotDocumentSizeBytes)
        {
            throw new InvalidDataException(
                $"Blob snapshot '{blobName}' document size exceeds the configured maximum '{Options.MaximumSnapshotDocumentSizeBytes}'.");
        }

        SnapshotBlobDocument snapshotDocument = SnapshotBlobDocumentSerializer.Deserialize(document);
        ValidateDocument(snapshotKey, blobName, snapshotDocument);
        ValidateDeclaredPayloadSize(blobName, snapshotDocument.DataSizeBytes, Options.MaximumSnapshotPayloadSizeBytes);
        byte[] storedBytes = DecodeStoredBytes(blobName, snapshotDocument.Data);
        if (storedBytes.LongLength != snapshotDocument.StoredSizeBytes)
        {
            throw new InvalidDataException($"Stored payload size mismatch for Blob '{blobName}'.");
        }

        byte[] payload = SnapshotBlobCompression.Decompress(
            snapshotDocument.Compression,
            storedBytes,
            Options.MaximumSnapshotPayloadSizeBytes);
        if (payload.LongLength != snapshotDocument.DataSizeBytes)
        {
            throw new InvalidDataException($"Uncompressed payload size mismatch for Blob '{blobName}'.");
        }

        return new()
        {
            Data = ImmutableArray.CreateRange(payload),
            DataContentType = snapshotDocument.DataContentType,
            DataSizeBytes = snapshotDocument.DataSizeBytes,
            ReducerHash = snapshotDocument.ReducersHash,
        };
    }

    /// <inheritdoc />
    public BinaryData Encode(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot
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
        BinaryData documentPayload = SnapshotBlobDocumentSerializer.Serialize(document);
        if (documentPayload.ToMemory().Length > Options.MaximumSnapshotDocumentSizeBytes)
        {
            throw new ArgumentException(
                $"Snapshot document size exceeds configured maximum '{Options.MaximumSnapshotDocumentSizeBytes}'.",
                nameof(snapshot));
        }

        return documentPayload;
    }
}