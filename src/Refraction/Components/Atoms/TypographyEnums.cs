namespace Refraction.Components.Atoms;

/// <summary>
///     Defines the typographic variants available in the Refraction design system.
/// </summary>
public enum TypographyVariant
{
    /// <summary>
    ///     Display text - largest size, Space Grotesk 700, 32/40.
    /// </summary>
    Display,

    /// <summary>
    ///     Heading 1 - Space Grotesk 700, 24/32.
    /// </summary>
    H1,

    /// <summary>
    ///     Heading 2 - Space Grotesk 600, 20/28.
    /// </summary>
    H2,

    /// <summary>
    ///     Body text - Inter 400, 16/24.
    /// </summary>
    Body,

    /// <summary>
    ///     Small text - Inter 400, 13/18.
    /// </summary>
    Small,

    /// <summary>
    ///     Micro text - Inter 600, 11/16.
    /// </summary>
    Micro,

    /// <summary>
    ///     Instrument label - Inter 600, 11/16, uppercase with wide tracking.
    /// </summary>
    Instrument,

    /// <summary>
    ///     Code/numeric text - JetBrains Mono 400-500, 13/18.
    /// </summary>
    Code,
}

/// <summary>
///     Defines the semantic color options for typography.
/// </summary>
public enum TypographyColor
{
    /// <summary>
    ///     Default text color (spectrum.text).
    /// </summary>
    Default,

    /// <summary>
    ///     Muted text color (spectrum.textMuted).
    /// </summary>
    Muted,

    /// <summary>
    ///     Ghost text color (spectrum.ghost).
    /// </summary>
    Ghost,

    /// <summary>
    ///     Accent color (spectrum.cyan).
    /// </summary>
    Accent,

    /// <summary>
    ///     Success/OK color (signal.ok).
    /// </summary>
    Ok,

    /// <summary>
    ///     Warning/attention color (signal.attn).
    /// </summary>
    Attention,

    /// <summary>
    ///     Critical/error color (signal.critical).
    /// </summary>
    Critical,
}