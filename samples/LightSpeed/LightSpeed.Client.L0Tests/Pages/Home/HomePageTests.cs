using System.Linq;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.Showcase;
using MississippiSamples.LightSpeed.Client.Pages.Home;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Pages.Home;

/// <summary>Verifies overview theme intent through the application's layout.</summary>
public sealed class HomePageTests : BunitContext
{
    /// <summary>The overview dispatches a theme choice and renders the selected state.</summary>
    [Fact]
    public void ThemeChoiceUpdatesOverview()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<LayoutView> cut = Render<LayoutView>(p => p
            .Add(c => c.Layout, typeof(MainLayout))
            .AddChildContent<HomePage>());
        Assert.Equal("dark", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
        cut.FindAll(".theme-options button").Single(button => button.TextContent.Trim() == "Light").Click();
        Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
    }
}