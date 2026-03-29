namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Captures deterministic preference inputs for Refraction runtime theme resolution.
/// </summary>
public sealed class RefractionPreferenceSnapshot
{
    /// <summary>
    ///     Gets a value indicating whether high contrast is preferred.
    /// </summary>
    public bool PrefersHighContrast { get; init; }

    /// <summary>
    ///     Gets a value indicating whether reduced motion is preferred.
    /// </summary>
    public bool PrefersReducedMotion { get; init; }
}