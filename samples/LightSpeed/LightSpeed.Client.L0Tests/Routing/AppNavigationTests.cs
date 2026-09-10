using System.Threading.Tasks;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Routing;

/// <summary>Verifies route changes request focus for the destination heading.</summary>
public sealed class AppNavigationTests : BunitContext
{
    /// <summary>The actual router renders each destination and requests focus for its heading.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RouteChangesFocusDestinationHeadingAsync()
    {
        Services.AddReservoir()
            .AddShowcaseFeature()
            .AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Off);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        using IRenderedComponent<App> cut = Render<App>();
        Assert.Equal("A clear view. A predictable state.", cut.Find("h1").TextContent);
        Assert.Equal("h1", JSInterop.VerifyFocusOnNavigateInvoke().Arguments[0]);
        await cut.InvokeAsync(() => navigation.NavigateTo("/kitchen-sink"));
        Assert.Equal("Controls, with nothing hidden.", cut.Find("h1").TextContent);
        Assert.Equal("-1", cut.Find("h1").GetAttribute("tabindex"));
        Assert.Equal("h1", JSInterop.VerifyFocusOnNavigateInvoke(2)[1].Arguments[0]);
        await cut.InvokeAsync(() => navigation.NavigateTo("/"));
        Assert.Equal("A clear view. A predictable state.", cut.Find("h1").TextContent);
        Assert.Equal("-1", cut.Find("h1").GetAttribute("tabindex"));
        Assert.Equal("h1", JSInterop.VerifyFocusOnNavigateInvoke(3)[2].Arguments[0]);
        await cut.InvokeAsync(() => navigation.NavigateTo("/missing"));
        Assert.Equal("Sorry, there's nothing at this address.", cut.Find("p").TextContent);
        JSInterop.VerifyFocusOnNavigateInvoke(3);
    }
}