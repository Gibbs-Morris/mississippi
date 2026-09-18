using System;
using System.Linq;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.Spring.Client.Components.Templates.SpringShell;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Tests for the Spring shell's presentational theme controls.
/// </summary>
public sealed class SpringApplicationShellTests : BunitContext
{
    /// <summary>
    ///     The skip link focuses the shell's content container through JS interop.
    /// </summary>
    [Fact]
    public void SkipLinkFocusesMainContent()
    {
        using IRenderedComponent<SpringApplicationShell> cut = Render<SpringApplicationShell>(parameters => parameters
            .Add(component => component.ThemeMode, RefractionThemeMode.Dark)
            .AddChildContent("<p>Example controls</p>"));
        string? mainReference = cut.Find("#main-content").GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(mainReference), cut.Find("#main-content").OuterHtml);

        cut.Find(".skip-link").Click();

        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(mainReference, focused.Id);
        Assert.Equal("Example controls", cut.Find("#main-content > p").TextContent);
    }

    /// <summary>
    ///     Selecting a theme invokes the callback with the selected mode.
    /// </summary>
    [Fact]
    public void ThemeButtonInvokesCallback()
    {
        RefractionThemeMode? selectedMode = null;
        using IRenderedComponent<SpringApplicationShell> cut = Render<SpringApplicationShell>(parameters => parameters
            .Add(component => component.ThemeMode, RefractionThemeMode.Dark)
            .Add(
                component => component.ThemeChanged,
                EventCallback.Factory.Create<RefractionThemeMode>(this, mode => selectedMode = mode)));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Light", StringComparison.Ordinal)).Click();
        Assert.Equal(RefractionThemeMode.Light, selectedMode);
    }
}
