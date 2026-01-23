namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Provides design tokens for the Refraction holographic HUD theme.
/// </summary>
/// <remarks>
///     Implementations supply color, spacing, typography, and motion tokens
///     that components consume at render time.
/// </remarks>
public interface IRefractionTheme
{
    /// <summary>
    ///     Gets the default border radius in pixels.
    /// </summary>
    int BorderRadius { get; }

    /// <summary>
    ///     Gets the background color for panels.
    /// </summary>
    string PanelBackground { get; }

    /// <summary>
    ///     Gets the primary accent color (hex format, e.g., "#00ffcc").
    /// </summary>
    string PrimaryAccent { get; }

    /// <summary>
    ///     Gets the secondary accent color.
    /// </summary>
    string SecondaryAccent { get; }

    /// <summary>
    ///     Gets the base spacing unit in pixels.
    /// </summary>
    int SpacingUnit { get; }

    /// <summary>
    ///     Gets the text color for primary content.
    /// </summary>
    string TextPrimary { get; }

    /// <summary>
    ///     Gets the text color for secondary/muted content.
    /// </summary>
    string TextSecondary { get; }

    /// <summary>
    ///     Gets the default transition duration in milliseconds.
    /// </summary>
    int TransitionDurationMs { get; }
}