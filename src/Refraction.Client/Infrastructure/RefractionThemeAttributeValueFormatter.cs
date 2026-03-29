using System;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Client.Infrastructure;

/// <summary>
///     Formats resolved Refraction theme values for DOM runtime attributes.
/// </summary>
internal static class RefractionThemeAttributeValueFormatter
{
    /// <summary>
    ///     Formats the contrast mode for DOM attribute output.
    /// </summary>
    /// <param name="contrastMode">The resolved contrast mode.</param>
    /// <returns>The DOM attribute value for the contrast mode.</returns>
    internal static string Format(
        RefractionContrastMode contrastMode
    ) =>
        contrastMode switch
        {
            RefractionContrastMode.High => "high",
            RefractionContrastMode.Standard => "standard",
            RefractionContrastMode.System => "system",
            var _ => throw new ArgumentOutOfRangeException(
                nameof(contrastMode),
                contrastMode,
                "Unsupported Refraction contrast mode."),
        };

    /// <summary>
    ///     Formats the density for DOM attribute output.
    /// </summary>
    /// <param name="density">The resolved density.</param>
    /// <returns>The DOM attribute value for the density.</returns>
    internal static string Format(
        RefractionDensity density
    ) =>
        density switch
        {
            RefractionDensity.Compact => "compact",
            RefractionDensity.Comfortable => "comfortable",
            var _ => throw new ArgumentOutOfRangeException(nameof(density), density, "Unsupported Refraction density."),
        };

    /// <summary>
    ///     Formats the motion mode for DOM attribute output.
    /// </summary>
    /// <param name="motionMode">The resolved motion mode.</param>
    /// <returns>The DOM attribute value for the motion mode.</returns>
    internal static string Format(
        RefractionMotionMode motionMode
    ) =>
        motionMode switch
        {
            RefractionMotionMode.Reduced => "reduced",
            RefractionMotionMode.Standard => "standard",
            RefractionMotionMode.System => "system",
            var _ => throw new ArgumentOutOfRangeException(
                nameof(motionMode),
                motionMode,
                "Unsupported Refraction motion mode."),
        };
}