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