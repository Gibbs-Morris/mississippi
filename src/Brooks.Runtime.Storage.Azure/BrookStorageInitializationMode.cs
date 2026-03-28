namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Controls how the Brooks Azure provider validates or provisions required blob containers on startup.
/// </summary>
public enum BrookStorageInitializationMode
{
    /// <summary>
    ///     Validate the configured containers and create them when missing.
    /// </summary>
    ValidateOrCreate,

    /// <summary>
    ///     Validate that the configured containers already exist and fail fast when they do not.
    /// </summary>
    ValidateOnly,
}