using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents a snapshot in storage form prior to Blob serialization.
/// </summary>
internal sealed record SnapshotBlobStorageModel(
    SnapshotStreamKey StreamKey,
    long Version,
    byte[] Data,
    string DataContentType,
    long DataSizeBytes,
    bool IsCompressed = false
);
