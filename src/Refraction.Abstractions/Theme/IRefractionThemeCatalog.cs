using System.Collections.Generic;


namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Provides the known Refraction theme descriptors available to a host.
/// </summary>
public interface IRefractionThemeCatalog
{
    /// <summary>
    ///     Gets the default theme descriptor.
    /// </summary>
    RefractionThemeDescriptor DefaultTheme { get; }

    /// <summary>
    ///     Gets the known theme descriptors.
    /// </summary>
    IReadOnlyList<RefractionThemeDescriptor> Themes { get; }

    /// <summary>
    ///     Gets a theme descriptor for the requested brand identifier.
    /// </summary>
    /// <param name="brandId">The brand identifier to resolve.</param>
    /// <returns>The matching theme descriptor, or <see langword="null" /> when the brand is unknown.</returns>
    RefractionThemeDescriptor? GetTheme(
        RefractionBrandId brandId
    );
}