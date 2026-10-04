using System;
using System.Linq;

using Bunit;

using MississippiSamples.LightSpeed.Client.Components.Molecules.Progress;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Molecules.Progress;

/// <summary>Verifies the demo emits intent and renders only parent-supplied completion.</summary>
public sealed class ProgressDemoTests : BunitContext
{
    /// <summary>Numeric and unknown choices leave state ownership with the parent.</summary>
    [Fact]
    public void ChoicesEmitIntentWithoutChangingParameters()
    {
        int? selected = -1;
        using IRenderedComponent<ProgressDemo> cut = Render<ProgressDemo>(p => p
            .Add(c => c.Percent, 25)
            .Add(c => c.PercentChanged, value => selected = value));
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "100%").Click();
        Assert.Equal(100, selected);
        Assert.Equal(25, cut.Instance.Percent);
        Assert.Equal("25", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Unknown duration").Click();
        Assert.Null(selected);
        cut.Render(p => p.Add(c => c.Percent, null));
        Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
        Assert.Equal("Unknown duration", cut.Find("[aria-pressed=true]").TextContent.Trim());
        Assert.Contains("Completion unknown", cut.Markup, StringComparison.Ordinal);
        Assert.All(cut.FindAll("button"), button => Assert.Equal("button", button.GetAttribute("type")));
    }
}