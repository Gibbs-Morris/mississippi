using System;


namespace MississippiSamples.Spring.Client.Features.ThemePreferences;

/// <summary>
///     Applies Spring client theme preference actions.
/// </summary>
internal static class ThemePreferencesReducers
{
    /// <summary>Changes to a supported Refraction theme.</summary>
    /// <param name="state">The current theme preferences.</param>
    /// <param name="action">The requested theme.</param>
    /// <returns>The updated state, or the original state for an unsupported mode.</returns>
    public static ThemePreferencesState SetThemeMode(
        ThemePreferencesState state,
        SetThemeModeAction action
    ) =>
        Enum.IsDefined(action.Mode)
            ? state with
            {
                ThemeMode = action.Mode,
            }
            : state;
}