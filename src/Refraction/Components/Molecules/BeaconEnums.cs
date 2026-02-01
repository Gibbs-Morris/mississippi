namespace Refraction.Components.Molecules;

/// <summary>
///     Defines the semantic states for a Beacon.
/// </summary>
public enum BeaconState
{
    /// <summary>
    ///     Informational state - default cyan color.
    /// </summary>
    Info,

    /// <summary>
    ///     Warning state - amber color.
    /// </summary>
    Warning,

    /// <summary>
    ///     Critical state - red color.
    /// </summary>
    Critical,
}

/// <summary>
///     Defines the size variants for a Beacon.
/// </summary>
public enum BeaconSize
{
    /// <summary>
    ///     Small beacon - 8px diameter.
    /// </summary>
    Small,

    /// <summary>
    ///     Default beacon - 12px diameter.
    /// </summary>
    Default,

    /// <summary>
    ///     Large beacon - 16px diameter.
    /// </summary>
    Large,
}