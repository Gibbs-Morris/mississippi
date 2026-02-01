namespace Refraction.Components.Molecules;

/// <summary>
///     Defines the stroke style of a mooring line.
/// </summary>
public enum MooringLineStyle
{
    /// <summary>
    ///     Thin continuous line (1px stroke).
    /// </summary>
    Fine,

    /// <summary>
    ///     Standard continuous line (2px stroke).
    /// </summary>
    Solid,

    /// <summary>
    ///     Thick continuous line (3px stroke).
    /// </summary>
    Bold,

    /// <summary>
    ///     Dashed line pattern.
    /// </summary>
    Dashed,

    /// <summary>
    ///     Dotted line pattern.
    /// </summary>
    Dotted,
}

/// <summary>
///     Defines the semantic color of a mooring line.
/// </summary>
public enum MooringLineColor
{
    /// <summary>
    ///     Default foreground color.
    /// </summary>
    Default,

    /// <summary>
    ///     Muted/secondary color for subtle connections.
    /// </summary>
    Muted,

    /// <summary>
    ///     Accent color for highlighted connections.
    /// </summary>
    Accent,

    /// <summary>
    ///     Ok/success color for positive connections.
    /// </summary>
    Ok,

    /// <summary>
    ///     Attention/warning color for cautionary connections.
    /// </summary>
    Attention,

    /// <summary>
    ///     Critical/error color for problem connections.
    /// </summary>
    Critical,
}