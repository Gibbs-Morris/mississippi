namespace Mississippi.Tributary.Runtime.Storage.Azure;

/// <summary>
///     Configuration options for the Azure Blob Storage Tributary snapshot provider.
/// </summary>
public sealed class SnapshotStorageOptions
{
    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>BlobServiceClient</c> from DI.
    /// </summary>
    public string BlobServiceClientServiceKey { get; set; } = SnapshotAzureDefaults.BlobServiceClientServiceKey;

    /// <summary>
    ///     Gets or sets the snapshot container name.
    /// </summary>
    public string ContainerName { get; set; } = SnapshotAzureDefaults.ContainerName;

    /// <summary>
    ///     Gets or sets the startup initialization behavior.
    /// </summary>
    public SnapshotStorageInitializationMode InitializationMode { get; set; } = SnapshotStorageInitializationMode.ValidateOrCreate;

    /// <summary>
    ///     Gets or sets the page size hint used for delete-all and prune listings.
    /// </summary>
    public int ListPageSize { get; set; } = 100;
}
