namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Options controlling Blob snapshot persistence.
/// </summary>
public sealed class SnapshotBlobStorageOptions
{
    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>BlobServiceClient</c> from DI.
    /// </summary>
    public string BlobServiceClientServiceKey { get; set; } = SnapshotBlobDefaults.BlobServiceClientServiceKey;

    /// <summary>
    ///     Gets or sets the Blob container name used for snapshot storage.
    /// </summary>
    public string ContainerName { get; set; } = SnapshotBlobDefaults.ContainerName;

    /// <summary>
    ///     Gets or sets a value indicating whether stored snapshot payload bytes should be gzip-compressed.
    /// </summary>
    public bool EnableCompression { get; set; }

    /// <summary>
    ///     Gets or sets the maximum allowed uncompressed snapshot payload size in bytes.
    /// </summary>
    public long MaximumSnapshotPayloadSizeBytes { get; set; } =
        SnapshotBlobDefaults.DefaultMaximumSnapshotPayloadSizeBytes;
}