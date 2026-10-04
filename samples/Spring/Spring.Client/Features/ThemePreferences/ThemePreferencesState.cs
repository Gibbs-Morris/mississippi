using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.State;


namespace MississippiSamples.Spring.Client.Features.ThemePreferences;

/// <summary>
///     Stores the Spring client's selected Refraction theme.
/// </summary>
internal sealed record ThemePreferencesState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "theme-preferences";

    /// <summary>Gets the selected theme mode.</summary>
    public RefractionThemeMode ThemeMode { get; init; } = RefractionThemeMode.Dark;
}