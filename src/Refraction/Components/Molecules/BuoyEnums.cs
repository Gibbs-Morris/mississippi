namespace Refraction.Components.Molecules;

/// <summary>
///     Defines the semantic state of a buoy indicator.
/// </summary>
public enum BuoyState
{
    /// <summary>
    ///     Informational state (accent/blue).
    /// </summary>
    Info,

    /// <summary>
    ///     Success/ok state (green).
    /// </summary>
    Success,

    /// <summary>
    ///     Warning/attention state (amber).
    /// </summary>
    Warning,

    /// <summary>
    ///     Critical/error state (red).
    /// </summary>
    Critical,
}

/// <summary>
///     Defines the size variant of a buoy indicator.
/// </summary>
public enum BuoySize
{
    /// <summary>
    ///     Small buoy (12px indicator).
    /// </summary>
    Small,

    /// <summary>
    ///     Default buoy (16px indicator).
    /// </summary>
    Default,

    /// <summary>
    ///     Large buoy (24px indicator).
    /// </summary>
    Large,
}