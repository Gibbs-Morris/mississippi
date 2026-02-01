namespace Refraction.Components.Organisms;

/// <summary>
///     Defines the semantic state of a tide ribbon notification.
/// </summary>
public enum TideRibbonState
{
    /// <summary>
    ///     Informational notification (accent/blue).
    /// </summary>
    Info,

    /// <summary>
    ///     Success notification (green).
    /// </summary>
    Success,

    /// <summary>
    ///     Warning notification (amber).
    /// </summary>
    Warning,

    /// <summary>
    ///     Critical/error notification (red).
    /// </summary>
    Critical,
}