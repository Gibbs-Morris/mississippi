using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace MississippiSamples.Spring.Client.Features.ThemePreferences;

/// <summary>
///     Selects Spring client theme preferences for presentation.
/// </summary>
internal static class ThemePreferencesSelectors
{
    /// <summary>Gets the selected Refraction theme.</summary>
    /// <param name="state">The current theme preferences.</param>
    /// <returns>The selected theme mode.</returns>
    public static RefractionThemeMode GetThemeMode(
        ThemePreferencesState state
    ) =>
        state.ThemeMode;
}
