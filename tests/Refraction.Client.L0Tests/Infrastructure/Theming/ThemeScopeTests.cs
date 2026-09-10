using System;
using System.Collections.Generic;

using Bunit;

using Mississippi.Refraction.Client.Components.Atoms.Input;
using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure.Theming;

/// <summary>Verifies scoped theme selection and motion propagation.</summary>
public sealed class ThemeScopeTests : BunitContext
{
    /// <summary>Blank classes do not remove the base styling hook.</summary>
    [Fact]
    public void BlankClassRetainsBaseClass()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut =
            Render<CascadingRefractionProvider>(p => p.Add(c => c.Class, " "));
        Assert.Equal("rf-theme", cut.Find("div").ClassName);
    }

    /// <summary>Branding attributes reach the wrapper while owned selectors remain authoritative.</summary>
    [Fact]
    public void BrandingPreservesScopeIdentity()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut = Render<CascadingRefractionProvider>(p => p
            .Add(c => c.ThemeMode, RefractionThemeMode.Light)
            .Add(c => c.Class, "customer-brand")
            .Add(
                c => c.AdditionalAttributes,
                new Dictionary<string, object>
                {
                    ["style"] = "--rf-color-action-primary: rebeccapurple",
                    ["data-testid"] = "brand",
                    ["data-rf-theme"] = "dark",
                    ["class"] = "replacement",
                }));
        Assert.Contains("rf-theme", cut.Find("[data-testid=brand]").ClassList);
        Assert.Contains("customer-brand", cut.Find("[data-testid=brand]").ClassList);
        Assert.DoesNotContain("replacement", cut.Find("[data-testid=brand]").ClassList);
        Assert.Equal("light", cut.Find(".rf-theme").GetAttribute("data-rf-theme"));
        Assert.Equal("--rf-color-action-primary: rebeccapurple", cut.Find(".rf-theme").GetAttribute("style"));
    }

    /// <summary>The default scope preserves its class and renders child content.</summary>
    [Fact]
    public void DefaultScopeRendersContent()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut =
            Render<CascadingRefractionProvider>(p => p.AddChildContent("<span>Content</span>"));
        Assert.Equal("dark", cut.Find(".rf-theme").GetAttribute("data-rf-theme"));
        Assert.Equal("Content", cut.Find("span").TextContent);
    }

    /// <summary>Motion preferences propagate and update through the established cascade name.</summary>
    /// <param name="isReduced">The initial preference.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotionPreferencesUpdate(
        bool isReduced
    )
    {
        using IRenderedComponent<CascadingRefractionProvider> cut = Render<CascadingRefractionProvider>(p =>
            p.Add(c => c.IsReducedMotion, isReduced).AddChildContent<ReducedMotionProbe>());
        Assert.Equal(isReduced, cut.FindComponent<ReducedMotionProbe>().Instance.IsReducedMotion);
        cut.Render(p => p.Add(c => c.IsReducedMotion, !isReduced));
        Assert.Equal(!isReduced, cut.FindComponent<ReducedMotionProbe>().Instance.IsReducedMotion);
    }

    /// <summary>Nested providers retain their own mode when their parent changes.</summary>
    [Fact]
    public void NestedScopesRemainIndependent()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut = Render<CascadingRefractionProvider>(p =>
            p.Add(c => c.IsReducedMotion, true)
                .AddChildContent<CascadingRefractionProvider>(inner => inner
                    .Add(c => c.ThemeMode, RefractionThemeMode.Light)
                    .AddChildContent<ReducedMotionProbe>()));
        cut.Render(p => p.Add(c => c.ThemeMode, RefractionThemeMode.HighContrast));
        Assert.Equal("high-contrast", cut.FindAll(".rf-theme")[0].GetAttribute("data-rf-theme"));
        Assert.Equal("light", cut.FindAll(".rf-theme")[1].GetAttribute("data-rf-theme"));
        Assert.False(cut.FindComponent<ReducedMotionProbe>().Instance.IsReducedMotion);
    }

    /// <summary>Theme changes preserve the child component and its identity.</summary>
    [Fact]
    public void ThemeChangesPreserveInputIdentity()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut =
            Render<CascadingRefractionProvider>(p =>
                p.AddChildContent<InputField>(input => input.Add(c => c.Label, "Email")));
        string? id = cut.Find("input").Id;
        Assert.False(string.IsNullOrWhiteSpace(id));
        cut.Render(p => p.Add(c => c.ThemeMode, RefractionThemeMode.Light));
        Assert.Equal("light", cut.Find(".rf-theme").GetAttribute("data-rf-theme"));
        Assert.Equal(id, cut.Find("input").Id);
        Assert.Equal(id, cut.Find("label").GetAttribute("for"));
    }

    /// <summary>Each mode produces its public scope selector.</summary>
    /// <param name="mode">The selected theme.</param>
    /// <param name="name">The expected selector value.</param>
    [Theory]
    [InlineData(RefractionThemeMode.Dark, "dark")]
    [InlineData(RefractionThemeMode.Light, "light")]
    [InlineData(RefractionThemeMode.HighContrast, "high-contrast")]
    public void ThemeModeSelectsScope(
        RefractionThemeMode mode,
        string name
    )
    {
        using IRenderedComponent<CascadingRefractionProvider> cut =
            Render<CascadingRefractionProvider>(p => p.Add(c => c.ThemeMode, mode));
        Assert.Equal(name, cut.Find(".rf-theme").GetAttribute("data-rf-theme"));
    }

    /// <summary>Unsupported enum values fail with a diagnostic exception.</summary>
    [Fact]
    public void UnsupportedModeIsRejected()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using IRenderedComponent<CascadingRefractionProvider> cut =
                Render<CascadingRefractionProvider>(p => p.Add(c => c.ThemeMode, (RefractionThemeMode)99));
        });
        Assert.Equal((RefractionThemeMode)99, exception.ActualValue);
    }
}