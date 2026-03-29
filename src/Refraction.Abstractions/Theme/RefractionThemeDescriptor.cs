namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Describes a host-selectable Refraction theme brand.
/// </summary>
public sealed class RefractionThemeDescriptor
{
    /// <summary>
    ///     Gets the brand identifier for the theme.
    /// </summary>
    public required RefractionBrandId BrandId { get; init; }

    /// <summary>
    ///     Gets the CSS scope name used for runtime theme application.
    /// </summary>
    public required string CssScopeName { get; init; }

    /// <summary>
    ///     Gets the human-readable display name for the theme.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this theme is the catalog default.
    /// </summary>
    public bool IsDefault { get; init; }
}