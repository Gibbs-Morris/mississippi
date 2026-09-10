using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Supplies the selected presentation data to the showcase components.</summary>
/// <remarks>Public because it is a parameter type on Razor components.</remarks>
/// <param name="ThemeMode">The selected theme.</param>
/// <param name="Email">The form value.</param>
/// <param name="InputState">The visual and accessible input state.</param>
/// <param name="ErrorText">The validation message.</param>
/// <param name="StatusText">The form status.</param>
/// <param name="ActionCount">The demonstrated action count.</param>
/// <param name="LastAction">The latest action name.</param>
public sealed record ShowcaseView(
    RefractionThemeMode ThemeMode,
    string Email,
    string InputState,
    string? ErrorText,
    string StatusText,
    int ActionCount,
    string LastAction
);