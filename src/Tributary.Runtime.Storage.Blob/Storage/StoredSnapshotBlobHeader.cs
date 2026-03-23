using System;
using System.Text.Json.Serialization;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents the authoritative JSON header persisted inside the stored Blob frame.
/// </summary>
internal sealed record StoredSnapshotBlobHeader
{
    /// <summary>
    ///     Gets the canonical persisted stream identity.
    /// </summary>
    [JsonPropertyName("canonicalStreamIdentity")]
    public string CanonicalStreamIdentity { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted payload compression algorithm name.
    /// </summary>
    [JsonPropertyName("compressionAlgorithm")]
    public string CompressionAlgorithm { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted payload content type.
    /// </summary>
    [JsonPropertyName("dataContentType")]
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted payload serializer identity.
    /// </summary>
    [JsonPropertyName("payloadSerializerId")]
    public string PayloadSerializerId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the SHA-256 checksum over the uncompressed payload bytes.
    /// </summary>
    [JsonPropertyName("payloadSha256")]
    public string PayloadSha256 { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted reducer hash.
    /// </summary>
    [JsonPropertyName("reducerHash")]
    public string ReducerHash { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted snapshot storage name.
    /// </summary>
    [JsonPropertyName("snapshotStorageName")]
    public string SnapshotStorageName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the storage format version for the provider-owned frame.
    /// </summary>
    [JsonPropertyName("storageFormatVersion")]
    public int StorageFormatVersion { get; init; }

    /// <summary>
    ///     Gets the stored payload segment size in bytes.
    /// </summary>
    [JsonPropertyName("storedPayloadBytes")]
    public long StoredPayloadBytes { get; init; }

    /// <summary>
    ///     Gets the uncompressed payload size in bytes.
    /// </summary>
    [JsonPropertyName("uncompressedPayloadBytes")]
    public long UncompressedPayloadBytes { get; init; }

    /// <summary>
    ///     Gets the snapshot version.
    /// </summary>
    [JsonPropertyName("version")]
    public long Version { get; init; }

    /// <summary>
    ///     Gets the write timestamp captured when the frame was encoded.
    /// </summary>
    [JsonPropertyName("writtenUtc")]
    public DateTimeOffset WrittenUtc { get; init; }
}