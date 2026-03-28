namespace Mississippi.Tributary.Runtime.Storage.Azure;

/// <summary>
///     Defines the startup validation behavior for the Tributary Azure snapshot container.
/// </summary>
public enum SnapshotStorageInitializationMode
{
    /// <summary>
    ///     Validates the configured container and creates it when it does not already exist.
    /// </summary>
    ValidateOrCreate,

    /// <summary>
    ///     Validates that the configured container already exists and fails startup otherwise.
    /// </summary>
    ValidateOnly,
}
