using System;

using Bunit;

using MississippiSamples.Spring.Client.Components.Molecules;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Molecules;

/// <summary>Protects safe rendering of arbitrary snapshot values.</summary>
public sealed class SnapshotTableTests : BunitContext
{
    /// <summary>Unavailable snapshots cannot look like a loaded empty table.</summary>
    [Fact]
    public void MissingSnapshotShowsExplicitUnavailableState()
    {
        using IRenderedComponent<SnapshotTable> cut = Render<SnapshotTable>();
        Assert.Empty(cut.FindAll("table"));
        Assert.Contains("No snapshot data available.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Snapshot values are text, not executable markup.</summary>
    [Fact]
    public void SnapshotValuesAreEscapedAndRegionIsKeyboardAccessible()
    {
        using IRenderedComponent<SnapshotTable> cut = Render<SnapshotTable>(parameters => parameters
            .Add(component => component.Title, "Projection snapshot")
            .Add(component => component.Rows, [("Value", "<script>alert('value')</script>")]));
        Assert.Empty(cut.FindAll("script"));
        Assert.Equal("<script>alert('value')</script>", cut.Find("td").TextContent);
        Assert.Equal("Projection snapshot", cut.Find("[role='region']").GetAttribute("aria-label"));
        Assert.Equal("0", cut.Find("[role='region']").GetAttribute("tabindex"));
        Assert.Equal("row", cut.Find("tbody th").GetAttribute("scope"));
    }
}