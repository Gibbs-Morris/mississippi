namespace Mississippi.Tributary.Runtime.Storage.Azure;

/// <summary>
///     Default constants for Tributary Azure storage configuration and keyed service registrations.
/// </summary>
public static class SnapshotAzureDefaults
{
    /// <summary>
    ///     The default keyed DI service key for the Tributary Azure <c>BlobServiceClient</c>.
    /// </summary>
    public const string BlobServiceClientServiceKey = "mississippi-blob-snapshots";

    /// <summary>
    ///     The default snapshot container name.
    /// </summary>
    public const string ContainerName = "snapshots";

    /// <summary>
    ///     The reserved Brooks event container name used by the shared-account Azure topology.
    /// </summary>
    public const string ReservedBrooksContainerName = "brooks";

    /// <summary>
    ///     The reserved Brooks lock container name used by the shared-account Azure topology.
    /// </summary>
    public const string ReservedLockContainerName = "locks";
}
