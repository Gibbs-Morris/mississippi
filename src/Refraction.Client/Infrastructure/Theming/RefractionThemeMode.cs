namespace Mississippi.Refraction.Client.Infrastructure.Theming;

/// <summary>Specifies the built-in palette for a Refraction theme scope.</summary>
/// <remarks>Public so applications can select a provider's presentation mode.</remarks>
public enum RefractionThemeMode
{
    /// <summary>The default Neo Blue palette on dark surfaces.</summary>
    Dark = 0,

    /// <summary>A dark-ink palette on light surfaces.</summary>
    Light = 1,

    /// <summary>A high-contrast palette on black surfaces.</summary>
    HighContrast = 2,
}