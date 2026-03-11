namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Default constants for Blob snapshot storage configuration and keyed service registrations.
/// </summary>
public static class SnapshotBlobDefaults
{
    /// <summary>
    ///     The keyed DI service key for the snapshot Blob container client.
    /// </summary>
    public const string BlobContainerServiceKey = "mississippi-blob-snapshots";

    /// <summary>
    ///     The keyed DI service key for the snapshot Blob service client.
    /// </summary>
    public const string BlobServiceClientServiceKey = "mississippi-blob-snapshots-client";

    /// <summary>
    ///     The default Blob container name for snapshots.
    /// </summary>
    public const string ContainerName = "snapshots";
}
