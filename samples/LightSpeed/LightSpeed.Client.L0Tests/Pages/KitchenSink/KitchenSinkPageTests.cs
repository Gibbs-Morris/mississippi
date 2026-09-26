using System.Linq;

using Bunit;

using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.Showcase;
using MississippiSamples.LightSpeed.Client.Pages.KitchenSink;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Pages.KitchenSink;

/// <summary>Verifies the page connects rendered callbacks to the real Reservoir store.</summary>
public sealed class KitchenSinkPageTests : BunitContext
{
    /// <summary>Emitter activation and disabled intent use the registered page store and survive reset.</summary>
    [Fact]
    public void EmitterActionsUpdateSelectedStateAndSurviveReset()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.Find("button.rf-emitter").Click();
        Assert.Equal("1", cut.Find("[data-testid=state-emitter-count]").TextContent);
        Assert.Equal(nameof(ActivateEmitterAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("input[name=emitter-disabled]").Change(true);
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("[data-testid=state-emitter-disabled]").TextContent);
        Assert.Equal(nameof(ChangeEmitterDisabledAction), cut.Find("[data-testid=last-action]").TextContent);
        cut.Find("form button[type=button]").Click();
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("disabled"));
        Assert.Equal("1", cut.Find("[data-testid=state-emitter-count]").TextContent);
        Assert.Equal(nameof(ResetProfileAction), cut.Find("[data-testid=last-action]").TextContent);
    }

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
        cut.FindAll(".theme-options button").Single(button => button.TextContent.Trim() == "Light").Click();
        cut.Find("form button[type=button]").Click();
        Assert.Equal("alex@contoso.example", cut.Find("input[name=work-email]").GetAttribute("value"));
        Assert.Equal("4", cut.Find("[data-testid=action-count]").TextContent);
        Assert.Equal("ResetProfileAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    /// <summary>Progress choices dispatch through Reservoir and survive form resets.</summary>
    [Fact]
    public void ProgressChoicesUpdateSelectedState()
    {
        Services.AddReservoir().AddShowcaseFeature();
        using IRenderedComponent<KitchenSinkPage> cut = Render<KitchenSinkPage>();
        cut.FindAll(".progress-options button").Single(button => button.TextContent.Trim() == "75%").Click();
        Assert.Equal("75", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Equal("ChangeProgressAction", cut.Find("[data-testid=last-action]").TextContent);
        Assert.Equal("75%", cut.Find("[data-testid=state-progress]").TextContent);
        cut.Find("form button[type=button]").Click();
        Assert.Equal("75", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        cut.FindAll(".progress-options button")
            .Single(button => button.TextContent.Trim() == "Unknown duration")
            .Click();
        Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
        Assert.Equal("Unknown", cut.Find("[data-testid=state-progress]").TextContent);
        Assert.Equal("3", cut.Find("[data-testid=action-count]").TextContent);
    }
}