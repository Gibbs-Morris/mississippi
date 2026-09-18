using System;
using System.Linq;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Tests the Spring layout's store integration.
/// </summary>
public sealed class MainLayoutTests : BunitContext
{
    /// <summary>
    ///     The layout dispatches connection setup, rerenders from theme state, and disposes its subscription.
    /// </summary>
    [Fact]
    public void LayoutIntegratesWithThemeStore()
    {
        using TrackingInletStore store = new();
        Services.AddSingleton<IInletStore>(store);
        using (IRenderedComponent<MainLayout> cut = Render<MainLayout>())
        {
            Assert.Contains(store.Actions, action => action is RequestSignalRConnectionAction);
            Assert.Equal(1, store.ActiveSubscriptions);
            Assert.Equal("dark", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
            cut.FindAll("button")
                .Single(button => button.TextContent.Contains("Light", StringComparison.Ordinal))
                .Click();
            Assert.Contains(store.Actions, action => action is SetThemeModeAction { Mode: RefractionThemeMode.Light });
            Assert.Equal(RefractionThemeMode.Light, store.ThemeMode);
            Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
            cut.Instance.Dispose();
        }

        Assert.Equal(0, store.ActiveSubscriptions);
    }
}
