namespace Mississippi.Tributary.Runtime.Storage.Blob;

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
    ///     Gets or sets a value indicating whether snapshot payloads are compressed with gzip before upload.
    /// </summary>
    public bool CompressionEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the Blob container name used for snapshots.
    /// </summary>
    public string ContainerName { get; set; } = SnapshotBlobDefaults.ContainerName;
}
