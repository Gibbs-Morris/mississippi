namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Options controlling Blob-backed snapshot persistence.
/// </summary>
internal sealed class SnapshotBlobStorageOptions
{
    /// <summary>
    ///     Gets or sets the configured Blob container initialization behavior.
    /// </summary>
    public SnapshotBlobContainerInitializationMode ContainerInitializationMode { get; set; } =
        SnapshotBlobContainerInitializationMode.CreateIfMissing;

    /// <summary>
    ///     Gets or sets the Blob container name used for snapshot persistence.
    /// </summary>
    public string ContainerName { get; set; } = SnapshotBlobDefaults.ContainerName;

    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>BlobServiceClient</c> from DI.
    /// </summary>
    public string BlobServiceClientServiceKey { get; set; } = SnapshotBlobDefaults.BlobServiceClientServiceKey;

    /// <summary>
    ///     Gets or sets the configured snapshot payload serializer format.
    /// </summary>
    public string PayloadSerializerFormat { get; set; } = SnapshotBlobDefaults.PayloadSerializerFormat;
}