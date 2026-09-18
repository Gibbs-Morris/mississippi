using System;
using System.Threading.Tasks;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;
using Mississippi.Reservoir.Core;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client.L0Tests.Routing;

/// <summary>
///     Tests route-change focus behavior for the Spring application.
/// </summary>
public sealed class AppNavigationTests : BunitContext
{
    /// <summary>
    ///     Route changes focus the destination page heading through Blazor's focus interop.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RouteChangesFocusDestinationHeadingAsync()
    {
        Services.AddReservoir()
            .AddThemePreferencesFeature()
            .AddReservoirBlazorBuiltIns()
            .AddInletClient()
            .AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Off);
        using IDisposable documentThemeInterop = JSInterop.SetupVoid(
            "document.documentElement.setAttribute",
            _ => true);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        using IRenderedComponent<App> cut = Render<App>();
        Assert.Equal("Bank Account Demo", cut.Find("h1").TextContent);
        Assert.Equal("h1", JSInterop.VerifyFocusOnNavigateInvoke().Arguments[0]);
        await cut.InvokeAsync(() => navigation.NavigateTo("/investigations"));
        Assert.Equal("Transaction Investigations", cut.Find("h1").TextContent);
        Assert.Equal("h1", JSInterop.VerifyFocusOnNavigateInvoke(2)[1].Arguments[0]);
    }
}