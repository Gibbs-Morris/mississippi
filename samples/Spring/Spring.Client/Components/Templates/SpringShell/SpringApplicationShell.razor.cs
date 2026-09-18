using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace MississippiSamples.Spring.Client.Components.Templates.SpringShell;

/// <summary>
///     Provides the presentational shell for the Spring sample.
/// </summary>
public sealed partial class SpringApplicationShell : ComponentBase
{
    private static IReadOnlyList<RefractionThemeMode> ThemeModes { get; } =
    [
        RefractionThemeMode.Dark,
        RefractionThemeMode.Light,
        RefractionThemeMode.HighContrast,
    ];

    /// <summary>Gets or sets the content rendered inside the shell.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the callback raised when the theme changes.</summary>
    [Parameter]
    public EventCallback<RefractionThemeMode> ThemeChanged { get; set; }

    /// <summary>Gets or sets the selected theme.</summary>
    [Parameter]
    public RefractionThemeMode ThemeMode { get; set; }

    private ElementReference MainContent { get; set; }

    private static string GetModeLabel(
        RefractionThemeMode mode
    ) =>
        mode == RefractionThemeMode.HighContrast ? "High contrast" : mode.ToString();

    private Task FocusContentAsync() =>
        MainContent.FocusAsync().AsTask();
}
