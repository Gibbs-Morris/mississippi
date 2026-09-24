using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.Spring.Client.Features.ThemePreferences;

/// <summary>
///     Selects a Refraction theme for the Spring client.
/// </summary>
internal sealed record SetThemeModeAction(RefractionThemeMode Mode) : IAction;