using System.Linq;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.LightSpeed.Client.Components.Templates.Showcase;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Templates.Showcase;

/// <summary>Verifies shell theme intent and content focus.</summary>
public sealed class ShowcaseShellTests : BunitContext
{
    /// <summary>The content link targets the page's main landmark through focus interop.</summary>
    [Fact]
    public void SkipLinkFocusesMainContent()
    {
        using IRenderedComponent<ShowcaseShell> cut = Render<ShowcaseShell>(p => p
            .Add(c => c.PagePath, "/kitchen-sink")
            .Add(c => c.Title, "Kitchen sink")
            .AddChildContent("<p>Example controls</p>"));
        Assert.Equal("/kitchen-sink#main-content", cut.Find(".skip-link").GetAttribute("href"));
        Assert.Equal("-1", cut.Find("main").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find("h1").GetAttribute("tabindex"));
        string? mainReference = cut.Find("main").GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(mainReference), cut.Find("main").OuterHtml);
        cut.Find(".skip-link").Click();
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(mainReference, focused.Id);
        Assert.Equal("Example controls", cut.Find("main > p").TextContent);
    }

    /// <summary>Each theme button emits its mode and reflects the parent's selected mode.</summary>
    /// <param name="label">The theme button's label.</param>
    /// <param name="mode">The selected theme.</param>
    [Theory]
    [InlineData("Dark", RefractionThemeMode.Dark)]
    [InlineData("Light", RefractionThemeMode.Light)]
    [InlineData("High contrast", RefractionThemeMode.HighContrast)]
    public void ThemeButtonsEmitControlledIntent(
        string label,
        RefractionThemeMode mode
    )
    {
        RefractionThemeMode? selected = null;
        RefractionThemeMode initial =
            mode == RefractionThemeMode.Dark ? RefractionThemeMode.Light : RefractionThemeMode.Dark;
        using IRenderedComponent<ShowcaseShell> cut = Render<ShowcaseShell>(p => p
            .Add(c => c.ThemeMode, initial)
            .Add(c => c.ThemeChanged, value => selected = value));
        Assert.Equal(
            "false",
            cut.FindAll(".theme-options button")
                .Single(button => button.TextContent.Trim() == label)
                .GetAttribute("aria-pressed"));
        cut.FindAll(".theme-options button").Single(button => button.TextContent.Trim() == label).Click();
        Assert.Equal(mode, selected);
        Assert.Equal(initial, cut.Instance.ThemeMode);
        cut.Render(p => p.Add(c => c.ThemeMode, mode));
        Assert.Equal(mode, cut.FindComponent<CascadingRefractionProvider>().Instance.ThemeMode);
        Assert.Equal(
            "true",
            cut.FindAll(".theme-options button")
                .Single(button => button.TextContent.Trim() == label)
                .GetAttribute("aria-pressed"));
    }
}