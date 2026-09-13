using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace MississippiSamples.LightSpeed.Client.Components.Templates.Showcase;

/// <summary>Provides the presentational navigation and theme shell.</summary>
/// <remarks>Public so pages can compose it in Razor.</remarks>
public sealed partial class ShowcaseShell : ComponentBase
{
    private static IReadOnlyList<RefractionThemeMode> Modes { get; } =
    [
        RefractionThemeMode.Dark,
        RefractionThemeMode.Light,
        RefractionThemeMode.HighContrast,
    ];

    /// <summary>Gets or sets page content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the current page path for its content anchor.</summary>
    [Parameter]
    public string PagePath { get; set; } = "/";

    /// <summary>Gets or sets the page introduction.</summary>
    [Parameter]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme selection callback.</summary>
    [Parameter]
    public EventCallback<RefractionThemeMode> ThemeChanged { get; set; }

    /// <summary>Gets or sets the selected mode.</summary>
    [Parameter]
    public RefractionThemeMode ThemeMode { get; set; }

    /// <summary>Gets or sets the page title.</summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    private ElementReference MainContent { get; set; }

    private static string GetModeLabel(
        RefractionThemeMode mode
    ) =>
        mode == RefractionThemeMode.HighContrast ? "High contrast" : mode.ToString();

    private Task FocusContentAsync() => MainContent.FocusAsync().AsTask();
}