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

    /// <summary>Each demo retains unique title and description relationships across parameter updates.</summary>
    [Fact]
    public void DemoInstancesUseDistinctStableAriaTargets()
    {
        using IRenderedComponent<EmitterDemo> first = Render<EmitterDemo>();
        using IRenderedComponent<EmitterDemo> second = Render<EmitterDemo>();
        string? firstTitleId = first.Find("h2").Id;
        string? secondTitleId = second.Find("h2").Id;
        string? firstDescriptionId = first.Find(".emitter-preview p.note").Id;
        string? secondDescriptionId = second.Find(".emitter-preview p.note").Id;
        Assert.False(string.IsNullOrWhiteSpace(firstTitleId));
        Assert.False(string.IsNullOrWhiteSpace(firstDescriptionId));
        Assert.False(string.IsNullOrWhiteSpace(secondTitleId));
        Assert.False(string.IsNullOrWhiteSpace(secondDescriptionId));
        Assert.NotEqual(firstTitleId, secondTitleId);
        Assert.NotEqual(firstDescriptionId, secondDescriptionId);
        Assert.Equal(firstTitleId, first.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal(secondTitleId, second.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal(firstDescriptionId, first.Find("button.rf-emitter").GetAttribute("aria-describedby"));
        Assert.Equal(secondDescriptionId, second.Find("button.rf-emitter").GetAttribute("aria-describedby"));
        first.Render(p => p.Add(c => c.ActivationCount, 3).Add(c => c.IsDisabled, true));
        Assert.Equal(firstTitleId, first.Find("h2").Id);
        Assert.Equal(firstDescriptionId, first.Find(".emitter-preview p.note").Id);
        Assert.Equal(firstTitleId, first.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal(firstDescriptionId, first.Find("button.rf-emitter").GetAttribute("aria-describedby"));
        Assert.Equal(secondTitleId, second.Find("h2").Id);
        Assert.Equal(secondDescriptionId, second.Find(".emitter-preview p.note").Id);
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