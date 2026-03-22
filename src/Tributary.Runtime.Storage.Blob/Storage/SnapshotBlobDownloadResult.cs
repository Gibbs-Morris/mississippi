namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents the downloaded Blob payload and metadata for a snapshot.
/// </summary>
internal sealed record SnapshotBlobDownloadResult(
    byte[] Data,
    string DataContentType,
    long DataSizeBytes,
    bool IsCompressed
);
