using System.Text.Json.Serialization;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Blob JSON document representation of a snapshot.
/// </summary>
internal sealed class SnapshotBlobDocument
{
    /// <summary>
    ///     The current JSON schema version.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    ///     Gets the brook name identifying the event stream.
    /// </summary>
    [JsonPropertyName("brookName")]
    public string BrookName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the compression metadata value.
    /// </summary>
    [JsonPropertyName("compression")]
    public string Compression { get; init; } = SnapshotBlobCompression.None;

    /// <summary>
    ///     Gets the Base64-encoded stored payload bytes.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the MIME type describing the payload bytes.
    /// </summary>
    [JsonPropertyName("dataContentType")]
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the uncompressed payload size in bytes.
    /// </summary>
    [JsonPropertyName("dataSizeBytes")]
    public long DataSizeBytes { get; init; }

    /// <summary>
    ///     Gets the entity identifier for the snapshot stream.
    /// </summary>
    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the reducers hash for the snapshot stream.
    /// </summary>
    [JsonPropertyName("reducersHash")]
    public string ReducersHash { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the JSON schema version.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    ///     Gets the snapshot storage name for the snapshot stream.
    /// </summary>
    [JsonPropertyName("snapshotStorageName")]
    public string SnapshotStorageName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the stored payload size in bytes before Base64 encoding.
    /// </summary>
    [JsonPropertyName("storedSizeBytes")]
    public long StoredSizeBytes { get; init; }

    /// <summary>
    ///     Gets the snapshot version.
    /// </summary>
    [JsonPropertyName("version")]
    public long Version { get; init; }
}