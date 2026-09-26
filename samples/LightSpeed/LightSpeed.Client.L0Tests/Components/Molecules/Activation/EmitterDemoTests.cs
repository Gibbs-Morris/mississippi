using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;

using MississippiSamples.LightSpeed.Client.Components.Molecules.Activation;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Molecules.Activation;

/// <summary>Verifies controlled emitter presentation and parent callback ownership.</summary>
public sealed class EmitterDemoTests : BunitContext
{
    /// <summary>The activation count is an atomic status region that updates in place.</summary>
    [Fact]
    public void ActivationCountUsesAtomicStatusRegion()
    {
        using IRenderedComponent<EmitterDemo> cut = Render<EmitterDemo>(p => p.Add(c => c.ActivationCount, 1));
        IElement status = cut.Find("[data-testid=emitter-activation-count]");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-atomic"));
        Assert.Equal("1 activation", status.TextContent);
        cut.Render(p => p.Add(c => c.ActivationCount, 2));
        IElement updatedStatus = cut.Find("[data-testid=emitter-activation-count]");
        Assert.Single(cut.FindAll("[data-testid=emitter-activation-count]"));
        Assert.Equal("status", updatedStatus.GetAttribute("role"));
        Assert.Equal("true", updatedStatus.GetAttribute("aria-atomic"));
        Assert.Equal("2 activations", updatedStatus.TextContent);
    }

    /// <summary>Activation emits typed intent without mutating the parent's count parameter.</summary>
    [Fact]
    public void ActivationEmitsIntentWithoutChangingParameters()
    {
        MouseEventArgs? receivedArgs = null;
        using IRenderedComponent<EmitterDemo> cut = Render<EmitterDemo>(p => p
            .Add(c => c.ActivationCount, 2)
            .Add(c => c.Activated, args => receivedArgs = args));
        cut.Find("button.rf-emitter").Click();
        Assert.NotNull(receivedArgs);
        Assert.Equal(2, cut.Instance.ActivationCount);
    }

    /// <summary>The molecule renders a visible label, count and controlled checkbox.</summary>
    [Fact]
    public void DemoRendersLabeledEmitterAndCount()
    {
        using IRenderedComponent<EmitterDemo> cut = Render<EmitterDemo>(p => p.Add(c => c.ActivationCount, 2));
        Assert.Equal("Emit signal", cut.Find(".rf-emitter__label").TextContent);
        Assert.Equal("2 activations", cut.Find("[data-testid=emitter-activation-count]").TextContent);
        Assert.Equal("emitter-disabled", cut.Find("input[type=checkbox]").GetAttribute("name"));
    }

    /// <summary>Changing the checkbox emits disabled intent without changing the controlled parameter.</summary>
    [Fact]
    public void DisabledCheckboxEmitsIntentWithoutChangingParameters()
    {
        bool? selected = null;
        using IRenderedComponent<EmitterDemo> cut = Render<EmitterDemo>(p => p
            .Add(c => c.IsDisabled, false)
            .Add(c => c.DisabledChanged, value => selected = value));
        cut.Find("input[type=checkbox]").Change(true);
        Assert.Equal(true, selected);
        Assert.False(cut.Instance.IsDisabled);
        Assert.False(cut.Find("button.rf-emitter").HasAttribute("disabled"));
    }

    /// <summary>Parent state replacement updates the emitter and checkbox together.</summary>
    [Fact]
    public void ParentStateControlsDisabledPresentation()
    {
        using IRenderedComponent<EmitterDemo> cut = Render<EmitterDemo>();
        cut.Render(p => p.Add(c => c.IsDisabled, true).Add(c => c.ActivationCount, 3));
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("disabled"));
        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
        Assert.Equal("3 activations", cut.Find("[data-testid=emitter-activation-count]").TextContent);
    }
}