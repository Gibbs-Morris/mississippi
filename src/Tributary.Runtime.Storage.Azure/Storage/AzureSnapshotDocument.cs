namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage;

/// <summary>
///     Represents a serialized Tributary snapshot document stored in Azure Blob Storage.
/// </summary>
internal sealed record AzureSnapshotDocument
{
    /// <summary>
    ///     Gets the snapshot payload content type.
    /// </summary>
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the serialized snapshot payload bytes.
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     Gets the size of the payload in bytes.
    /// </summary>
    public long DataSizeBytes { get; init; }

    /// <summary>
    ///     Gets the reducers hash that scopes this snapshot stream.
    /// </summary>
    public string ReducerHash { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the persisted document schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    ///     Gets the string representation of the snapshot stream key.
    /// </summary>
    public string StreamKey { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the snapshot version.
    /// </summary>
    public long Version { get; init; }
}
