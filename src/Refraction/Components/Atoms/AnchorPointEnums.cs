namespace Refraction.Components.Atoms;

/// <summary>
///     Defines the size variants for an AnchorPoint.
/// </summary>
public enum AnchorPointSize
{
    /// <summary>
    ///     Small anchor point - 4px diameter.
    /// </summary>
    Small,

    /// <summary>
    ///     Default anchor point - 6px diameter.
    /// </summary>
    Default,

    /// <summary>
    ///     Large anchor point - 8px diameter.
    /// </summary>
    Large,
}

/// <summary>
///     Defines the color variants for an AnchorPoint.
/// </summary>
public enum AnchorPointColor
{
    /// <summary>
    ///     Default cyan accent color.
    /// </summary>
    Default,

    /// <summary>
    ///     Success/OK state (green).
    /// </summary>
    Ok,

    /// <summary>
    ///     Warning/attention state (amber).
    /// </summary>
    Attention,

    /// <summary>
    ///     Critical/error state (red).
    /// </summary>
    Critical,
}