using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.Spring.Client.Features.ThemePreferences;

/// <summary>
///     Registers Spring client theme preferences with Reservoir.
/// </summary>
internal static class ThemePreferencesFeatureRegistration
{
    /// <summary>Adds the theme preferences feature.</summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddThemePreferencesFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<ThemePreferencesState>(feature => feature
            .AddReducer<SetThemeModeAction>(ThemePreferencesReducers.SetThemeMode));
        return builder;
    }
}
