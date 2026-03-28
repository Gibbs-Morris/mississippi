namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Default constants for Brooks Azure storage configuration and keyed service registrations.
/// </summary>
public static class BrookAzureDefaults
{
    /// <summary>
    ///     The default keyed DI service key for the Brooks Azure <c>BlobServiceClient</c>.
    /// </summary>
    public const string BlobServiceClientServiceKey = "mississippi-blob-brooks";

    /// <summary>
    ///     The default Brooks event container name.
    /// </summary>
    public const string ContainerName = "brooks";

    /// <summary>
    ///     The default lock container name.
    /// </summary>
    public const string LockContainerName = "locks";

    /// <summary>
    ///     The reserved snapshot container name used by the shared-account Azure topology.
    /// </summary>
    public const string SnapshotContainerName = "snapshots";
}