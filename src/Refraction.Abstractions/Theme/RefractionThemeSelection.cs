namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Represents the host-owned Refraction runtime theme selection.
/// </summary>
public sealed class RefractionThemeSelection
{
    /// <summary>
    ///     Gets the preferred brand identifier.
    /// </summary>
    public RefractionBrandId? BrandId { get; init; }

    /// <summary>
    ///     Gets the requested contrast mode.
    /// </summary>
    public RefractionContrastMode Contrast { get; init; } = RefractionContrastMode.System;

    /// <summary>
    ///     Gets the requested layout density.
    /// </summary>
    public RefractionDensity Density { get; init; } = RefractionDensity.Comfortable;

    /// <summary>
    ///     Gets the requested motion mode.
    /// </summary>
    public RefractionMotionMode Motion { get; init; } = RefractionMotionMode.System;
}