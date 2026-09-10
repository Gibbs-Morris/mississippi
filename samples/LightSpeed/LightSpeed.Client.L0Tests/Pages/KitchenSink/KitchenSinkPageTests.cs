using Bunit;

using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.Showcase;
using MississippiSamples.LightSpeed.Client.Pages.KitchenSink;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Pages.KitchenSink;

/// <summary>Verifies the page connects rendered callbacks to the real Reservoir store.</summary>
public sealed class KitchenSinkPageTests : BunitContext
{
    /// <summary>The rendered form validates and resets through selected store state.</summary>
    [Fact]
    public void FormActionsUpdateSelectedState()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.Find("input[name=work-email]").Input(string.Empty);
        Assert.Empty(cut.FindAll("[role=alert]"));
        cut.Find("form").Submit();
        Assert.Equal("ValidateProfileAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("true", cut.Find("input[name=work-email]").GetAttribute("aria-invalid"));
        cut.FindAll(".theme-options button")[1].Click();
        cut.Find("form button[type=button]").Click();
        Assert.Equal("alex@contoso.example", cut.Find("input[name=work-email]").GetAttribute("value"));
        Assert.Equal("4", cut.Find("[data-testid=action-count]").TextContent);
        Assert.Equal("ResetProfileAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
        Assert.Empty(cut.FindAll("[role=alert]"));
    }
}