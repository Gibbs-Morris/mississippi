namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Default constants for Blob snapshot storage configuration and keyed service registrations.
/// </summary>
public static class SnapshotBlobDefaults
{
    /// <summary>
    ///     The keyed DI service key for the Blob container client used by the snapshot provider.
    /// </summary>
    public const string BlobContainerClientServiceKey = "mississippi-blob-snapshots-container";

    /// <summary>
    ///     The keyed DI service key for the Blob service client used by the snapshot provider.
    /// </summary>
    public const string BlobServiceClientServiceKey = "mississippi-blob-snapshots";

    /// <summary>
    ///     The default Blob container name for snapshots.
    /// </summary>
    public const string ContainerName = "snapshots";

    /// <summary>
    ///     The default maximum uncompressed snapshot payload size in bytes.
    /// </summary>
    public const long DefaultMaximumSnapshotPayloadSizeBytes = 128L * 1024L * 1024L;
}