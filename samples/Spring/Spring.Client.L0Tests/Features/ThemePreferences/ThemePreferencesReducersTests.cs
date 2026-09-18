using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client.L0Tests.Features.ThemePreferences;

/// <summary>
///     Tests for Spring client theme preference reducers.
/// </summary>
public sealed class ThemePreferencesReducersTests
{
    /// <summary>
    ///     A supported theme replaces the selected mode.
    /// </summary>
    [Fact]
    public void SetThemeModeAcceptsSupportedMode()
    {
        ThemePreferencesState state = new();

        ThemePreferencesState result = ThemePreferencesReducers.SetThemeMode(
            state,
            new SetThemeModeAction(RefractionThemeMode.HighContrast));

        Assert.Equal(RefractionThemeMode.HighContrast, result.ThemeMode);
    }

    /// <summary>
    ///     An unsupported theme leaves the state unchanged.
    /// </summary>
    [Fact]
    public void SetThemeModeRejectsUnsupportedMode()
    {
        ThemePreferencesState state = new();

        ThemePreferencesState result = ThemePreferencesReducers.SetThemeMode(
            state,
            new SetThemeModeAction((RefractionThemeMode)int.MaxValue));

        Assert.Same(state, result);
    }
}
