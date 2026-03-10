namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents the Blob upload payload and metadata for a snapshot write.
/// </summary>
internal sealed record SnapshotBlobWriteRequest(
    byte[] Data,
    string DataContentType,
    long DataSizeBytes,
    bool IsCompressed
);
