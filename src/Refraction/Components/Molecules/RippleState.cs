namespace Refraction.Components.Molecules;

/// <summary>
///     Defines the semantic state of a ripple effect.
/// </summary>
public enum RippleState
{
    /// <summary>
    ///     Success/confirmation ripple (green spectrum).
    /// </summary>
    Success,

    /// <summary>
    ///     Warning/caution ripple (amber spectrum).
    /// </summary>
    Warning,

    /// <summary>
    ///     Failure/error ripple (red spectrum).
    /// </summary>
    Failure,
}