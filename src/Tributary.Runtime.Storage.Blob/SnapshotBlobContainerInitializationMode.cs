namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Controls container initialization behavior during host startup.
/// </summary>
public enum SnapshotBlobContainerInitializationMode
{
    /// <summary>
    ///     Creates the configured container when it does not already exist.
    /// </summary>
    CreateIfMissing = 0,

    /// <summary>
    ///     Fails startup when the configured container does not already exist.
    /// </summary>
    ValidateExists = 1,
}