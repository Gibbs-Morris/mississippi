using System;

using Bunit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Abstractions.Theme;
using Mississippi.Refraction.Client.Infrastructure;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionRoot" />.
/// </summary>
public sealed class RefractionRootTests : BunitContext
{
    /// <summary>
    ///     RefractionRoot applies the resolved theme selection as scoped runtime attributes.
    /// </summary>
    [Fact]
    public void RefractionRootAppliesScopedRuntimeAttributes()
    {
        // Arrange
        Services.AddLogging();
        Services.AddSingleton<IRefractionThemeCatalog>(new TestRefractionThemeCatalog());

        // Act
        using IRenderedComponent<RefractionRoot> cut = Render<RefractionRoot>(parameters => parameters.Add(
                component => component.Selection,
                new()
                {
                    BrandId = new RefractionBrandId("signal"),
                    Contrast = RefractionContrastMode.High,
                    Density = RefractionDensity.Compact,
                    Motion = RefractionMotionMode.Reduced,
                })
            .AddChildContent("<span class=\"rf-test-child\">slice-1</span>"));

        // Assert
        Assert.Equal("signal", cut.Find(".rf-root").GetAttribute("data-rf-brand"));
        Assert.Equal("compact", cut.Find(".rf-root").GetAttribute("data-rf-density"));
        Assert.Equal("high", cut.Find(".rf-root").GetAttribute("data-rf-contrast"));
        Assert.Equal("reduced", cut.Find(".rf-root").GetAttribute("data-rf-motion"));
        Assert.Single(cut.FindAll(".rf-test-child"));
    }

    /// <summary>
    ///     RefractionRoot emits the theme stylesheet into head content by default.
    /// </summary>
    [Fact]
    public void RefractionRootEmitsThemeStylesheetIntoHeadContent()
    {
        // Arrange
        Services.AddLogging();
        Services.AddSingleton<IRefractionThemeCatalog>(new TestRefractionThemeCatalog());
        JSInterop.Mode = JSRuntimeMode.Loose;
        using IRenderedComponent<HeadOutlet> headOutlet = Render<HeadOutlet>();

        // Act
        using IRenderedComponent<RefractionRoot> cut = Render<RefractionRoot>();

        // Assert
        Assert.Contains(
            "_content/Mississippi.Refraction.Client/themes/refraction.css",
            headOutlet.Markup,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     RefractionRoot falls back atomically to the default brand when the selection is unknown.
    /// </summary>
    [Fact]
    public void RefractionRootFallsBackToDefaultBrandWhenThemeUnknown()
    {
        // Arrange
        Services.AddLogging();
        Services.AddSingleton<IRefractionThemeCatalog>(new TestRefractionThemeCatalog());

        // Act
        using IRenderedComponent<RefractionRoot> cut = Render<RefractionRoot>(parameters => parameters.Add(
            component => component.Selection,
            new()
            {
                BrandId = new RefractionBrandId("missing-brand"),
                Density = RefractionDensity.Compact,
            }));

        // Assert
        Assert.Equal("horizon", cut.Find(".rf-root").GetAttribute("data-rf-brand"));
        Assert.Equal("compact", cut.Find(".rf-root").GetAttribute("data-rf-density"));
        Assert.Equal("standard", cut.Find(".rf-root").GetAttribute("data-rf-contrast"));
        Assert.Equal("standard", cut.Find(".rf-root").GetAttribute("data-rf-motion"));
    }

    /// <summary>
    ///     RefractionRoot resolves system preference seams deterministically.
    /// </summary>
    [Fact]
    public void RefractionRootResolvesSystemPreferenceSeams()
    {
        // Arrange
        Services.AddLogging();
        Services.AddSingleton<IRefractionThemeCatalog>(new TestRefractionThemeCatalog());

        // Act
        using IRenderedComponent<RefractionRoot> cut = Render<RefractionRoot>(parameters => parameters.Add(
                component => component.Preferences,
                new()
                {
                    PrefersHighContrast = true,
                    PrefersReducedMotion = true,
                })
            .Add(
                component => component.Selection,
                new()
                {
                    Contrast = RefractionContrastMode.System,
                    Motion = RefractionMotionMode.System,
                }));

        // Assert
        Assert.Equal("horizon", cut.Find(".rf-root").GetAttribute("data-rf-brand"));
        Assert.Equal("high", cut.Find(".rf-root").GetAttribute("data-rf-contrast"));
        Assert.Equal("reduced", cut.Find(".rf-root").GetAttribute("data-rf-motion"));
    }
}