using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Infrastructure.Theming;

/// <summary>Provides a bounded theme scope and reduced-motion context.</summary>
/// <remarks>Public so applications can compose themed component subtrees.</remarks>
public sealed partial class CascadingRefractionProvider : ComponentBase
{
    /// <summary>Gets or sets native attributes for the theme wrapper.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the content rendered inside the theme scope.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets additional CSS classes for the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets a value indicating whether descendants should reduce motion.</summary>
    [Parameter]
    public bool IsReducedMotion { get; set; }

    /// <summary>Gets or sets the built-in theme mode.</summary>
    [Parameter]
    public RefractionThemeMode ThemeMode { get; set; } = RefractionThemeMode.Dark;

    private string CssClass => string.IsNullOrWhiteSpace(Class) ? "rf-theme" : $"rf-theme {Class}";

    private string ThemeName => GetThemeName(ThemeMode);

    private static string GetThemeName(
        RefractionThemeMode mode
    ) =>
        mode switch
        {
            RefractionThemeMode.Dark => "dark",
            RefractionThemeMode.Light => "light",
            RefractionThemeMode.HighContrast => "high-contrast",
            var _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Refraction theme mode."),
        };
}